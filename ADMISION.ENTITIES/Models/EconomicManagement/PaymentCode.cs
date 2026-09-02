using ADMISION.ENTITIES.Models.Modality;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ADMISION.ENTITIES.Models.EconomicManagement
{
    [Table("PaymentCode", Schema = "EconomicManagement")]
    public class PaymentCode
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción es obligatoria")]
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Guid TermId { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        [ForeignKey("TermId")]
        public virtual Term? Term { get; set; }
        public virtual ICollection<PaymentCodeModality> PaymentCodeModalities { get; set; } = new List<PaymentCodeModality>();
    }
}
