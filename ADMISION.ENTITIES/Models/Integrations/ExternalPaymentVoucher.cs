using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Integrations
{
    [Table("ExternalPaymentVoucher", Schema = "Integrations")]
    public class ExternalPaymentVoucher
    {
        public Guid Id { get; set; }

        public Guid ExternalApiId { get; set; }
        [ForeignKey("ExternalApiId")]
        public virtual ExternalApi? ExternalApi { get; set; }

        public string SerialVoucher { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public Guid QueryLogId { get; set; }
        [ForeignKey("QueryLogId")]
        public virtual ApiQueryLog? QueryLog { get; set; }

        public DateTimeOffset QueriedAt { get; set; } = DateTimeOffset.UtcNow;

        public virtual ICollection<ExternalPaymentDetail> Payments { get; set; } = new List<ExternalPaymentDetail>();
    }
}
