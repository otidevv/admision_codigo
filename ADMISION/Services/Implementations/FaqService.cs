using System.Globalization;
using System.Text;
using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Info;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class FaqService : IFaqService
    {
        private readonly AppDbContext _context;
        public FaqService(AppDbContext context) { _context = context; }

        public async Task<IReadOnlyList<FaqItem>> GetAllAsync(bool includeInactive, CancellationToken ct = default)
        {
            IQueryable<FaqItem> q = _context.FaqItems.AsNoTracking();
            if (!includeInactive) q = q.Where(f => f.IsActive);
            return await q
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<FaqItem>> GetPublicAsync(CancellationToken ct = default)
        {
            return await _context.FaqItems.AsNoTracking()
                .Where(f => f.IsActive)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .ToListAsync(ct);
        }

        public Task<FaqItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _context.FaqItems.FirstOrDefaultAsync(f => f.Id == id, ct);

        public async Task<FaqItem> CreateAsync(FaqItem item, string actor, CancellationToken ct = default)
        {
            item.Id = Guid.NewGuid();
            item.CreatedAt = DateTimeOffset.UtcNow;
            item.CreatedBy = actor;
            item.HitCount = 0;
            _context.FaqItems.Add(item);
            await _context.SaveChangesAsync(ct);
            return item;
        }

        public async Task<bool> UpdateAsync(FaqItem item, string actor, CancellationToken ct = default)
        {
            var existing = await _context.FaqItems.FirstOrDefaultAsync(f => f.Id == item.Id, ct);
            if (existing == null) return false;
            existing.Question = item.Question;
            existing.Answer = item.Answer;
            existing.Category = item.Category;
            existing.Keywords = item.Keywords;
            existing.DisplayOrder = item.DisplayOrder;
            existing.IsActive = item.IsActive;
            existing.ParentId = item.ParentId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            existing.UpdatedBy = actor;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var item = await _context.FaqItems.FirstOrDefaultAsync(f => f.Id == id, ct);
            if (item == null) return false;
            _context.FaqItems.Remove(item);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken ct = default)
        {
            return await _context.FaqItems.AsNoTracking()
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.Category))
                .Select(f => f.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(ct);
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Motor de matching simple (sin LLM): normaliza tildes y mayúsculas,
        //  tokeniza pregunta + keywords, calcula score por intersección de
        //  tokens (Jaccard ponderado por presencia en la pregunta).
        //  Suficiente para un catálogo curado de FAQs; queda explícito para
        //  poder cambiarlo a TF-IDF o similar más adelante sin tocar callers.
        // ═══════════════════════════════════════════════════════════════════
        public async Task<FaqAskResult> AskAsync(string query, int suggestionsTop = 3, CancellationToken ct = default)
        {
            var result = new FaqAskResult();
            if (string.IsNullOrWhiteSpace(query)) return result;

            var queryTokens = Tokenize(query);
            if (queryTokens.Count == 0) return result;

            var all = await _context.FaqItems.AsNoTracking()
                .Where(f => f.IsActive)
                .ToListAsync(ct);

            var scored = all.Select(item =>
            {
                var itemTokens = Tokenize(item.Question + " " + (item.Keywords ?? ""));
                return new { Item = item, Score = SimilarityScore(queryTokens, itemTokens) };
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.HitCount)
            .ToList();

            if (scored.Count == 0) return result;

            var best = scored[0];
            // Umbral: si el match es muy bajo (<0.35) lo tratamos como "no encontrado"
            // pero igual devolvemos las sugerencias.
            const double matchThreshold = 0.35;

            if (best.Score >= matchThreshold)
            {
                result.Matched = true;
                result.Score = best.Score;
                result.Best = best.Item;

                // Incrementar HitCount (fire-and-forget no — lo hacemos sincronía corta).
                var tracked = await _context.FaqItems.FirstOrDefaultAsync(f => f.Id == best.Item.Id, ct);
                if (tracked != null)
                {
                    tracked.HitCount++;
                    await _context.SaveChangesAsync(ct);
                }
            }

            // Sugerencias: si hay match, omitimos el best; si no, lo incluimos.
            var suggestionsSource = result.Matched ? scored.Skip(1) : scored;
            result.Suggestions = suggestionsSource
                .Take(suggestionsTop)
                .Select(x => new FaqSuggestion
                {
                    Id = x.Item.Id,
                    Question = x.Item.Question,
                    Category = x.Item.Category,
                    Score = x.Score
                })
                .ToList();

            return result;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Navegación jerárquica por opciones (chatbot tipo menú)
        // ═══════════════════════════════════════════════════════════════════
        public async Task<IReadOnlyList<FaqItem>> GetRootOptionsAsync(CancellationToken ct = default)
        {
            return await _context.FaqItems.AsNoTracking()
                .Where(f => f.IsActive && f.ParentId == null)
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.Question)
                .ToListAsync(ct);
        }

        public async Task<FaqItem?> GetOptionWithChildrenAsync(Guid id, CancellationToken ct = default)
        {
            return await _context.FaqItems.AsNoTracking()
                .Include(f => f.Children.Where(c => c.IsActive))
                .FirstOrDefaultAsync(f => f.Id == id && f.IsActive, ct);
        }

        // ── Helpers de matching ────────────────────────────────────────────
        private static readonly HashSet<string> _stopwords = new(StringComparer.OrdinalIgnoreCase)
        {
            "a","al","algo","algun","alguna","algunas","alguno","algunos","ante","antes",
            "como","con","contra","cual","cuando","de","del","desde","donde","durante",
            "e","el","ella","ellas","ellos","en","entre","era","erais","eran","eras","eres",
            "es","esa","esas","ese","eso","esos","esta","estaba","estabais","estaban","estabas",
            "estad","estada","estadas","estado","estados","estais","estamos","estan","estando",
            "estar","estara","estaran","estaras","estare","estareis","estaremos","estaria","estariais",
            "estariamos","estarian","estarias","estas","este","esto","estos","estoy","fue","fuera",
            "fuerais","fueran","fueras","fueron","fuese","fueseis","fuesen","fueses","fuesemos",
            "fui","fuimos","fuiste","fuisteis","ha","habeis","habia","habiais","habiamos","habian",
            "habias","han","has","hasta","hay","haya","hayais","hayamos","hayan","hayas","he",
            "hemos","la","las","le","les","lo","los","me","mi","mis","mucho","muchos","muy",
            "nada","ni","no","nos","nosotras","nosotros","nuestra","nuestras","nuestro","nuestros",
            "o","os","otra","otras","otro","otros","para","pero","poco","por","porque","que",
            "quien","quienes","se","sea","seais","seamos","sean","seas","sera","seran","seras",
            "sere","sereis","seremos","seria","seriais","seriamos","serian","serias","si","sido",
            "siendo","sin","sobre","sois","somos","son","soy","su","sus","suya","suyas","suyo","suyos",
            "tambien","tanto","te","tendra","tendran","tendras","tendre","tendreis","tendremos",
            "tendria","tendriais","tendriamos","tendrian","tendrias","tenemos","tener","tenga",
            "tengais","tengamos","tengan","tengas","tengo","tenia","teniais","teniamos","tenian",
            "tenias","tiene","tienen","tienes","todo","todos","tu","tus","tuya","tuyas","tuyo",
            "tuyos","un","una","uno","unas","unos","vosotras","vosotros","y","ya","yo"
        };

        private static List<string> Tokenize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            var normalized = StripAccents(raw).ToLowerInvariant();
            // Reemplaza cualquier no-alfanumérico por espacio.
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            }
            var tokens = sb.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length > 1 && !_stopwords.Contains(t))
                .ToList();
            return tokens;
        }

        private static string StripAccents(string s)
        {
            var normalized = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        // Score = (|intersección| / |query|) ponderado.
        // Si todos los tokens del query están en el item → 1.0
        // Si la mitad → 0.5, etc. Bonus pequeño si el item tiene tokens extra
        // que también coinciden (no penaliza).
        private static double SimilarityScore(List<string> queryTokens, List<string> itemTokens)
        {
            if (queryTokens.Count == 0 || itemTokens.Count == 0) return 0;
            var itemSet = new HashSet<string>(itemTokens, StringComparer.OrdinalIgnoreCase);
            var matches = queryTokens.Count(t => itemSet.Contains(t));
            return matches / (double)queryTokens.Count;
        }
    }
}
