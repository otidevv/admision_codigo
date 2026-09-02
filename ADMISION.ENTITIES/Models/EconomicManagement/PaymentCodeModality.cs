using ADMISION.ENTITIES.Models.Modality;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.EconomicManagement
{
    [Table("PaymentCodeModality", Schema = "EconomicManagement")]
    public class PaymentCodeModality
    {
        public Guid Id { get; set; }
        public Guid PaymentCodeId { get; set; }
        public Guid? ModalityId { get; set; }
        public Guid? TypeModalityId { get; set; }
        public decimal Amount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        [ForeignKey("PaymentCodeId")]
        public virtual PaymentCode? PaymentCode { get; set; }
        
        [ForeignKey("ModalityId")]
        public virtual Modality.Modality? Modality { get; set; }
        
        [ForeignKey("TypeModalityId")]
        public virtual TypeModality? TypeModality { get; set; }
    }
}
