using System.ComponentModel.DataAnnotations;

namespace Application.ViewModels.OrderNotificationModel
{
    public class OrderNotificationViewModel
    {
        public int? Id { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Укажите заголовок")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Укажите дату")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? CompletedBy { get; set; }

        public int DaysLeft => (DueDate.Date - DateTime.Today).Days;
        public bool IsOverdue => !IsCompleted && DaysLeft < 0;
        public bool IsUrgent => !IsCompleted && DaysLeft >= 0 && DaysLeft <= 7;
    }

    public class OrderNotificationCreateDto
    {
        public int OrderId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class OrderSearchResultDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
    }
}
