using Application.ViewModels.OrderModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Excel
{
    public interface IExportExcelService
    {
        Task<MemoryStream> GenerateTKPAsync(ExportOrderViewModel model, Order order, TkpSettings? tkpSettings = null);
    }
}
