using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.Users
{
    [Table("Teachers", Schema = "Users")]
    public class Teachers
    {
        public Guid Id { get; set; }
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public string Specialization { get; set; } = string.Empty; //especialidad
        public string Degree { get; set; } = string.Empty; //grado academico
        public string Type { get; set; } = string.Empty; //tipo de docente (Nombrado, Contratado, Auxiliar)
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; }

        public virtual Users? User { get; set; }
    }
}
