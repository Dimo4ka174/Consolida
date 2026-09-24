using Application.ViewModels.OrderModel.Products;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using DB.Entity.Enum;

namespace Application.ViewModels.OrderModel
{
    public class OrderFormDto
    {
        public int? Id { get; set; }

        [Display(Name = "Компания")]
        [Required(ErrorMessage = "Выберите компанию")]
        [Range(1, int.MaxValue, ErrorMessage = "Выберите компанию")]
        public int? CompanyId { get; set; }

        [Display(Name = "Клиент")]
        [Required(ErrorMessage = "Выберите клиента")]
        [Range(1, int.MaxValue, ErrorMessage = "Выберите клиента")]
        public int? CustomerId { get; set; }


        [Display(Name = "Номер заказа")]
        public string OrderNumber { get; set; } = string.Empty;

        [Display(Name = "Приоритет")]
        [Required(ErrorMessage = "Выберите приоритет")]
        public Priority Priority { get; set; }

        [Display(Name = "Статус")]
        [Required(ErrorMessage = "Выберите статус")]
        public Status Status { get; set; } = Status.Registered;

        [Display(Name = "Общий вес (кг)")]
        public decimal TotalWeight { get; set; }

        [Display(Name = "Общая стоимость (¥)")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Дата создания заказа")]
        public DateTime CreationDate { get; set; }

        [Display(Name = "Комментарий ")]
        public string Comment { get; set; } = string.Empty;


        public List<SelectListItem> PrioritiesList { get; set; } = new();
        public List<OrderProductViewModel> Products
        {
            get => _products;
            set
            {
                _products = value;
                CalculateTotals();
            }
        }

        private List<OrderProductViewModel> _products = new();

        public void CalculateTotals()
        {
            TotalWeight = Products.Sum(p => p.Weight * p.Quantity);
            TotalCost = Products.Sum(p => p.Price * p.Quantity);
        }
    }
}