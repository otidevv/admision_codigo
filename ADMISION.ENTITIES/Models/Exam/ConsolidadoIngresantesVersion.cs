using System.ComponentModel.DataAnnotations.Schema;
using ADMISION.ENTITIES.Models.Modality;

namespace ADMISION.ENTITIES.Models.Exam
{
    [Table("ConsolidadoIngresantesVersion", Schema = "Exam")]
    public class ConsolidadoIngresantesVersion
    {
        public Guid Id { get; set; }
        public Guid TermId { get; set; }
        public int VersionNumber { get; set; }
        public bool IsLatest { get; set; } = true;
        public int RecordCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = "";

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }
    }
}
