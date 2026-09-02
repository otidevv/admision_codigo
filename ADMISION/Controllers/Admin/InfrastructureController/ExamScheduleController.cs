using ADMISION.ENTITIES.Constants;
using ADMISION.ENTITIES.Data;
using ADMISION.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace admision.Controllers.Admin.InfrastructureController
{
    [Authorize(Roles = AppConstants.Roles.SuperAdmin + "," + AppConstants.Roles.Admin)]
    [Route("admin/infrastructure/exam-schedule")]
    public class ExamScheduleController : Controller
    {
        private readonly IExamScheduleService _service;
        private readonly AppDbContext _context;

        public ExamScheduleController(IExamScheduleService service, AppDbContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpGet("get-by-modality/{modalityId}")]
        public async Task<IActionResult> GetByModality(Guid modalityId, CancellationToken ct)
        {
            var schedule = await _service.GetByModalityAsync(modalityId, ct);
            if (schedule == null) return NotFound();
            return Json(schedule);
        }

        [HttpGet("get/{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var schedule = await _service.GetByIdAsync(id, ct);
            if (schedule == null) return NotFound();
            return Json(schedule);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { errors });
            }

            var user = User.Identity?.Name ?? "Admin";
            var result = await _service.CreateAsync(request.Name, request.ModalityId, request.TermId, request.Rooms, user, ct);
            if (result.NotFound) return NotFound();
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Message).ToList() });

            return Ok(new { success = true });
        }

        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update([FromBody] UpdateScheduleRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new { errors });
            }

            var user = User.Identity?.Name ?? "Admin";
            var result = await _service.UpdateAsync(request.Id, request.Rooms, user, ct);
            if (result.NotFound) return NotFound();
            if (!result.Succeeded)
                return BadRequest(new { errors = result.Errors.Select(e => e.Message).ToList() });

            return Ok(new { success = true });
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var ok = await _service.DeleteAsync(id, ct);
            if (!ok) return NotFound();
            return Ok(new { success = true });
        }

        [HttpGet("classrooms/{pavilionId?}")]
        public async Task<IActionResult> GetClassrooms(Guid? pavilionId, CancellationToken ct)
        {
            var query = _context.Classrooms
                .AsNoTracking()
                .Include(c => c.Pavilion)
                .Where(c => c.IsActive && c.Pavilion != null && c.Pavilion.IsActive);

            if (pavilionId.HasValue)
                query = query.Where(c => c.PavilionId == pavilionId.Value);

            var classrooms = await query
                .OrderBy(c => c.Pavilion!.Code)
                .ThenBy(c => c.Floor)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.Capacity, c.Floor, c.PavilionId, PavilionCode = c.Pavilion!.Code, PavilionName = c.Pavilion!.Name })
                .ToListAsync(ct);

            return Json(classrooms);
        }

        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers(CancellationToken ct)
        {
            var teachers = await _context.Teachers
                .AsNoTracking()
                .Include(t => t.User)
                .Where(t => t.IsActive)
                .OrderBy(t => t.User!.FullName)
                .Select(t => new { t.Id, FullName = t.User!.FullName, Specialization = t.Specialization })
                .ToListAsync(ct);

            return Json(teachers);
        }

        [HttpGet("tematic-areas/{termId}")]
        public async Task<IActionResult> GetTematicAreas(Guid termId, CancellationToken ct)
        {
            var areas = await _context.TematicAreas
                .AsNoTracking()
                .OrderBy(a => a.Code)
                .Select(a => new { a.Id, a.Code })
                .ToListAsync(ct);

            return Json(areas);
        }

        [HttpGet("pavilions")]
        public async Task<IActionResult> GetPavilions(CancellationToken ct)
        {
            var pavilions = await _context.Pavilions
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Code)
                .Select(p => new { p.Id, p.Name, p.Code })
                .ToListAsync(ct);

            return Json(pavilions);
        }

        public class CreateScheduleRequest
        {
            public string Name { get; set; } = string.Empty;
            public Guid ModalityId { get; set; }
            public Guid TermId { get; set; }
            public List<ExamScheduleRoomDto> Rooms { get; set; } = new();
        }

        public class UpdateScheduleRequest
        {
            public Guid Id { get; set; }
            public List<ExamScheduleRoomDto> Rooms { get; set; } = new();
        }
    }
}
