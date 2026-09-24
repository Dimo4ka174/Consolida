namespace Application.ViewModels.OrderModel.ExcelDoc
{
    public class DeliveryOptimizationInfo
    {
        public bool CanOptimize { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime OriginalDate { get; set; }
        public DateTime OptimizedDate { get; set; }
        public int WeeksToReduce { get; set; }
    }
}
