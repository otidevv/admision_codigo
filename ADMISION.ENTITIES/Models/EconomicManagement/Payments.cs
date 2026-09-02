using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.EconomicManagement
{
    [Table("Payments", Schema = "EconomicManagement")]
    public class Payments
    {
        public Guid Id { get; set; }
        public Guid InscriptionId { get; set; }
        public string OperationCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? FilePath { get; set; }
        public Guid? MethodPaymentId { get; set; }
        public bool IsApproved { get; set; } = false;
        public string? Observation { get; set; } = string.Empty;
        public DateTimeOffset DatePayment { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? UpdatedBy { get; set; } = string.Empty;

        public Guid? ExternalPaymentVoucherId { get; set; }

        [ForeignKey("InscriptionId")]
        public virtual Models.Postulante.Inscription? Inscription { get; set; }

        [ForeignKey("MethodPaymentId")]
        public virtual MethodPayment? MethodPayment { get; set; }

        [ForeignKey("ExternalPaymentVoucherId")]
        public virtual Models.Integrations.ExternalPaymentVoucher? ExternalPaymentVoucher { get; set; }
    }
}
