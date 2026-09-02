using ADMISION.ENTITIES.Data;
using ADMISION.ENTITIES.Models.Modality;
using ADMISION.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ADMISION.Services.Implementations
{
    public class ExamService : IExamService
    {
        private readonly AppDbContext _context;

        public ExamService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Modality>> GetActiveExamsAsync()
        {
            // Logic to get modalities that are active and part of the current term
            // For now returning active modalities
            return await _context.Modalities
                .Include(m => m.Term)
                .Where(m => m.IsActive && m.Term.IsActive)
                .ToListAsync();
        }
    }
}
