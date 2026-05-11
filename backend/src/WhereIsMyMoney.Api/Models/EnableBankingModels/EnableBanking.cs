namespace WhereIsMyMoney.Api.Models.EnableBankingModels
{
    public class EnableBanking
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public string? Asps { get; set; } // Comma-separated list of selected Account Servicing Payment Service Providers
        public DateTime CreatedAtUtc { get; set; }
    }
}
