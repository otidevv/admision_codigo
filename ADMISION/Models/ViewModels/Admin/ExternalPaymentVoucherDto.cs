namespace admision.Models.ViewModels.Admin
{
    public class ExternalPaymentVoucherDto
    {
        public string SerialVoucher { get; set; }
        public string FullName { get; set; }
        public DateTimeOffset QueriedAt { get; set; }
        public List<ExternalPaymentDetailDto> Payments { get; set; }
    }

    public class ExternalPaymentDetailDto
    {
        public string Description { get; set; }
        public string TermName { get; set; }
        public decimal Total { get; set; }
        public decimal Discount { get; set; }
    }
}
