using Application.ViewModels.OrderModel.ExcelDoc;
using Application.ViewModels.OrderModel;

namespace Application.DataAccessLayer.Interface.CalculationService
{
    public interface ICalculationService
    {
        Task<ExportCalculationViewModel> BuildCalculationViewModelAsync(ExportOrderViewModel model, string currentUser);
    }
}
