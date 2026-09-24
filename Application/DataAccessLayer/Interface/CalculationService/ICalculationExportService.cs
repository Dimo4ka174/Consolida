using Application.ViewModels.OrderModel.ExcelDoc;

namespace Application.DataAccessLayer.Interface.CalculationService
{
    public interface ICalculationExportService
    {
        Task<MemoryStream> ExportCalculationAsync(ExportCalculationViewModel model);
    }
}
