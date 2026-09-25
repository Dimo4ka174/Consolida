namespace Application.ViewModels.ConsolidationModel
{
    public class ConsolidationPoolHistoryDto
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
    }
}