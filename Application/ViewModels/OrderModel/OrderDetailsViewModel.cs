using Application.ViewModels.OrderModel.Products;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using DB.Entity.Enum;

namespace Application.ViewModels.OrderModel
{
    public class OrderDetailsViewModel
    {
        public int? Id { get; set; }
        public int SelectedStatusId { get; set; }


        [Display(Name = "Номер заказа")]
        public string OrderNumber { get; set; } = string.Empty;

        [Display(Name = "Клиент")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Дата создания заказа")]
        public DateTime OrderCreationDate { get; set; }

        [Display(Name = "Дата изменения заказа")]
        public DateTime LastChangeData { get; set; }

        [Display(Name = "Дата создания ТКП")]
        public DateTime DateCreationTKP { get; set; }

        [Display(Name = "Дата измененеия статуса заказа")]
        public DateTime LastStatusChangeDate { get; set; }

        [Display(Name = "Последний изменивший статус")]
        public string LastStatusChangedBy { get; set; } = string.Empty;

        [Display(Name = "Страна назначения")]
        public Country DestinationCountry { get; set; }

        [Display(Name = "Статус")]
        public Status Status { get; set; }

        [Display(Name = "Общий вес")]
        public decimal TotalWeight { get; set; }

        [Display(Name = "Общая стоимость")]
        public decimal TotalCost { get; set; }

        [Display(Name = "Максимальное кол-во недель доставки товаров до склада")]
        public int? TotalLeadTimeWeeks { get; set; }

        [Display(Name = "Цена за 1 кг")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal PricePerKg { get; set; }

        [Display(Name = "Цена за 1 м³")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal PricePerCubicMeter { get; set; }

        [Display(Name = "Курс Юаня")]
        public decimal ExchangeRate { get; set; } = 1;

        [Display(Name = "Комментарий")]
        public string Comment { get; set; } = string.Empty;

        public bool IsSaved { get; set; } = false;


        public MetrologicalInfoViewModel? MetrologicalInfo { get; set; }
        public List<OrderTaxViewModel> OrderTaxes { get; set; } = new();
        public List<OrderProductViewModel> Products { get; set; } = new();
        public List<SelectListItem> AvailableStatuses { get; set; } = new();
        public Dictionary<string, int> TaxTypeIds { get; set; } = new();
    }

    public class RecalculateRequest
    {
        public int OrderId { get; set; }
        public decimal ExchangeRate { get; set; }
    }

    public class OrderTaxViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string MeasureUnit { get; set; } = string.Empty;
        public bool IsCalculated { get; set; }
    }

    public class OrderCodeViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Rate { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public int StatusId { get; set; }
    }
}
