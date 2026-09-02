using System.ComponentModel.DataAnnotations.Schema;

namespace ADMISION.ENTITIES.Models.Integrations
{
    [Table("ExternalPaymentDetail", Schema = "Integrations")]
    public class ExternalPaymentDetail
    {
        public Guid Id { get; set; }

        public Guid VoucherId { get; set; }
        [ForeignKey("VoucherId")]
        public virtual ExternalPaymentVoucher? Voucher { get; set; }

        public string Description { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public string? TypeUser { get; set; }
        public decimal Quantity { get; set; }
        public int Status { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? CreatedBy { get; set; }
        public bool IsBankPayment { get; set; }
        public string? Name { get; set; }
        public bool ActiveDependency { get; set; }
        public string? Acronym { get; set; }
        public string? Cashier { get; set; }
        public string? TermName { get; set; }
        public string? AmountInWords { get; set; }
    }
}
