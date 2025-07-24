namespace INC_Care_App.Models.DTOs
{
    public class ClaimInfo
    {
        public string SourceKey { get; set; }
        public string Source { get; set; } 
        public string Proposer { get; set; }
        public string Patient { get; set; }
        public string ICardNumber { get; set; }
        public string PolicyNumber { get; set; }
        public string ClaimNumber { get; set; }
        public string DOA { get; set; }
        public string AmountClaimed { get; set; }
        public string AmountSettled { get; set; }
        public string Difference { get; set; }
        public string Deductions { get; set; }
        public string AmountAuthorized { get; set; }
        public string FinalAmount {  get; set; }
    }
}
