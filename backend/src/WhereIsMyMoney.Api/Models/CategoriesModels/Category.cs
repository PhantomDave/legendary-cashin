namespace WhereIsMyMoney.Api.Models.CategoryModels
{
    public class Category
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public decimal Budget { get; set; }
        public long AccountId { get; set; }
    }
}
