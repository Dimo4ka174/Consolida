using System.ComponentModel.DataAnnotations;

namespace DB.Entity.Enum
{
    public enum Status
    {
        [Display(Name = "Нет статуса")]
        None = 0,

        [Display(Name = "Зарегистрирован")]
        Registered = 1,

        [Display(Name = "Произведен расчёт")]
        Calculated = 2,

        [Display(Name = "Выставлено ТКП")]
        ExhibitTKP = 3,

        //Если через N кол-во времени статус не изменится на "Оплачен", то программа переведет заказ в статус "Требует оплаты"
        [Display(Name = "Передан в производство")]
        TransferredProduction = 4,

        //Уведомление через N кол-во времени, чтобы уточнить этап оплаты
        [Display(Name = "Требует оплаты")]
        RequiresPayment = 5,

        //Уведомление через N кол-во времени, чтобы уточнить этап изготовки
        [Display(Name = "Оплачен")]
        Paid = 6,

        //Фиксируется дата отправки, чтобы через N кол-во времени напомнить о том, что скоро товар приедет в РФ
        [Display(Name = "В пути")]
        OnTheWay = 7,

        [Display(Name = "Отгружен")]
        Shipped = 8,

        [Display(Name = "Заказ закрыт")]
        OrderClosed = 9,

        [Display(Name = "Архив")]
        Archive = 10,
    }
}
