namespace WhereIsMyMoney.Api.Models.CategoryModels
{
    public class CreateCategoryRequest
    {
        public required string Name { get; set; }
        public decimal Budget { get; set; }
        public long AccountId { get; set; }
    }
}
