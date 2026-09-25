using DB.Entity.Enum;

namespace Application.ViewModels.ConsolidationModel
{
    public class ConsolidationBoardDto
    {
        public int WeekSpan { get; set; } = 1;
        public List<ConsolidationColumnDto> Columns { get; set; } = new();
        public List<ConsolidationSuggestionDto> Suggestions { get; set; } = new();
        public List<ConsolidationPoolDto> Pools { get; set; } = new();
        public decimal? SelectedWeightLimit { get; set; }
        public List<ConsolidationWeightLimitDto> AvailableWeightLimits { get; set; } = new();
    }

    public class ConsolidationWeightLimitDto
    {
        public int Id { get; set; }
        public decimal Value { get; set; }
        public bool IsSystem { get; set; }
    }

    public class ConsolidationColumnDto
    {
        public int Week { get; set; }
        public List<ConsolidationOrderCardDto> Orders { get; set; } = new();
        public int OrderCount => Orders.Count;
        public decimal TotalWeight => Orders.Sum(o => o.TotalWeight);
    }

    public class ConsolidationOrderCardDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public decimal TotalWeight { get; set; }
        public int LeadTimeWeeks { get; set; }
        public bool IsInPool { get; set; }
        public bool IsInactive { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string? LockedByUserId { get; set; }
        public DateTime? LockedAt { get; set; }
    }

    public class ConsolidationSuggestionDto
    {
        public int FromWeek { get; set; }
        public int ToWeek { get; set; }
        public List<int> OrderIds { get; set; } = new();
        public decimal TotalWeight { get; set; }
        public int OrderCount => OrderIds.Count;
        public int? PoolId { get; set; }
    }

    public class ConsolidationPoolDto
    {
        public int PoolId { get; set; }
        public int TargetWeek { get; set; }
        public decimal TotalWeight { get; set; }
        public string Color { get; set; } = "#e0e0e0";
        public Status Status { get; set; } = Status.Paid;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? LockedByUserId { get; set; }
        public DateTime? LockedAt { get; set; }
        public List<ConsolidationOrderCardDto> Orders { get; set; } = new();
    }
}