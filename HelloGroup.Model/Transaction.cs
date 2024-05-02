namespace HelloGroup.Model
{
    public class Transaction
    {
        public long TransactionId { get; set; }
        public long? LineNumber { get; set; }
        public double? FCDebit { get; set; }
        public double? FCCredit { get; set; }
        public double? Debit { get; set; }
        public double? Credit { get; set; }
        public DateOnly? PostDate { get; set; }
        public string? Currency { get; set; }

        public string ToString()
        {
            return $"TransactionId: {this.TransactionId}, LineNumber: {LineNumber}, FCDebit: {this.FCCredit}, FCCredit: {this.FCCredit}, Debit: {this.Debit}, PostDate: {this.PostDate}, Currency: {this.Currency}";
        }
    }
}
