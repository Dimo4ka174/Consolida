namespace Application.DataAccessLayer.Service.Common
{
    public class PagedResult<TDto>
    {
        public List<TDto> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
