using Application.ViewModels.OrderModel.Products;
using Application.ViewModels.ManufacturerModel;
using Application.ViewModels.MeasureUnitModel;
using Application.ViewModels.CodeTNVDModel;
using Application.ViewModels.CustomerModel;
using Application.ViewModels.ProductModel;
using Application.ViewModels.TaxTypeModel;
using Application.ViewModels.CompanyModel;
using Application.ViewModels.OrderModel;
using Application.ViewModels.CityModel;
using DB.Entity.Enum;
using AutoMapper;
using DB.Entity;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Application.DataAccessLayer.Service.Common
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // ============================================================
            // =================== БАЗОВЫЕ СПРАВОЧНИКИ =====================
            // ============================================================

            // --- City ---
            CreateMap<CityDto, City>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Companies, o => o.Ignore())
                .ReverseMap();

            // --- Company ---
            CreateMap<CompanyDto, Company>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CityId, o => o.MapFrom(s => s.CityId))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Country, o => o.MapFrom(s => s.Country))
                .ForMember(d => d.Address, o => o.MapFrom(s => s.Address))
                .ForMember(d => d.City, o => o.Ignore())
                .ForMember(d => d.Customers, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.CitiesList, o => o.Ignore())
                .ForMember(d => d.CountriesList, o => o.Ignore());

            // --- Customer ---
            CreateMap<CustomerDto, Customer>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CompanyId, o => o.MapFrom(s => s.CompanyId))
                .ForMember(d => d.FirstName, o => o.MapFrom(s => s.FirstName))
                .ForMember(d => d.LastName, o => o.MapFrom(s => s.LastName))
                .ForMember(d => d.MiddleName, o => o.MapFrom(s => s.MiddleName))
                .ForMember(d => d.PositionJob, o => o.MapFrom(s => s.PositionJob))
                .ForMember(d => d.Email, o => o.MapFrom(s => s.Email))
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.Phone))
                .ForMember(d => d.PreferredMethod, o => o.MapFrom(s => s.PreferredMethod))
                .ForMember(d => d.Company, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.CompanyName, o => o.Ignore())
                .ForMember(d => d.CompaniesList, o => o.Ignore())
                .ForMember(d => d.PreferredMethodsList, o => o.Ignore());

            // --- CodeTNVD ---
            CreateMap<CodeTNVD, CodeTNVDdto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Rate, o => o.MapFrom(s => s.Rate));

            CreateMap<CodeTNVDdto, CodeTNVD>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Rate, o => o.MapFrom(s => s.Rate))
                .ForMember(d => d.IsDeleted, o => o.Ignore())
                .ForMember(d => d.Products, o => o.Ignore());

            // --- Manufacturer ---
            CreateMap<Manufacturer, ManufacturerDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name));

            CreateMap<ManufacturerDto, Manufacturer>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.IsDeleted, o => o.Ignore())
                .ForMember(d => d.Products, o => o.Ignore());

            // --- Product ---
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.ManufacturerId, o => o.MapFrom(s => s.ManufacturerId))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Model, o => o.MapFrom(s => s.Model))
                .ForMember(d => d.Cost, o => o.MapFrom(s => s.Price))
                .ForMember(d => d.CreatedDate, o => o.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.ManufacturersList, o => o.Ignore());

            CreateMap<ProductDto, Product>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.ManufacturerId, o => o.MapFrom(s => s.ManufacturerId))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Model, o => o.MapFrom(s => s.Model))
                .ForMember(d => d.Price, o => o.MapFrom(s => s.Cost))
                .ForMember(d => d.CreatedDate, o => o.MapFrom(s => s.CreatedDate))
                .ForMember(d => d.CodeTNVDId, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore())
                .ForMember(d => d.CodeTNVD, o => o.Ignore())
                .ForMember(d => d.Manufacturer, o => o.Ignore())
                .ForMember(d => d.OrdersProducts, o => o.Ignore());

            // --- MeasureUnit ---
            CreateMap<MeasureUnit, MeasureUnitDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name));

            CreateMap<MeasureUnitDto, MeasureUnit>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.IsDeleted, o => o.Ignore())
                .ForMember(d => d.TaxTypes, o => o.Ignore());

            // --- TaxType ---
            CreateMap<TaxType, TaxTypeDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Cost, o => o.MapFrom(s => s.Cost))
                .ForMember(d => d.MeasureUnitId, o => o.MapFrom(s => s.MeasureUnitId))
                .ForMember(d => d.MeasureUnitsList, o => o.Ignore());

            CreateMap<TaxTypeDto, TaxType>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
                .ForMember(d => d.Cost, o => o.MapFrom(s => s.Cost))
                .ForMember(d => d.MeasureUnitId, o => o.MapFrom(s => s.MeasureUnitId))
                .ForMember(d => d.IsDeleted, o => o.Ignore())
                .ForMember(d => d.MeasureUnit, o => o.Ignore())
                .ForMember(d => d.OrdersTax, o => o.Ignore());

            // ============================================================
            // ========================== ORDER ===========================
            // ============================================================

            // --- Order → OrderListDto (для списка заказов) ---
            CreateMap<Order, OrderListDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.OrderNumber, o => o.MapFrom(s => s.OrderNumber))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.CompanyId, o => o.MapFrom(s => s.Customer != null ? s.Customer.CompanyId : null))
                .ForMember(d => d.CompanyName, o => o.MapFrom(s =>
                    s.Customer != null && s.Customer.Company != null ? s.Customer.Company.Name : string.Empty))
                .ForMember(d => d.TotalWeight, o => o.MapFrom(s => s.TotalWeight))
                .ForMember(d => d.TotalCost, o => o.MapFrom(s => s.TotalCost))
                .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.LastChangeDate, o => o.MapFrom(s => s.LastChangeDate));

            // --- Order → DeleteOrderViewModel ---
            // Products и StatusHistory заполняются вручную в OrderDeleteService.
            CreateMap<Order, DeleteOrderViewModel>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id ?? 0))
                .ForMember(d => d.OrderNumber, o => o.MapFrom(s => s.OrderNumber))
                .ForMember(d => d.CustomerName, o => o.MapFrom(s =>
                    s.Customer != null
                        ? (s.Customer.LastName + " " + s.Customer.FirstName).Trim()
                        : string.Empty))
                .ForMember(d => d.CreationDate, o => o.MapFrom(s => s.CreationDate))
                .ForMember(d => d.TotalCost, o => o.MapFrom(s => s.TotalCost))
                .ForMember(d => d.TotalWeight, o => o.MapFrom(s => s.TotalWeight))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.GetDisplayName()))
                .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment ?? string.Empty))
                .ForMember(d => d.Products, o => o.Ignore())
                .ForMember(d => d.StatusHistory, o => o.Ignore())
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId));

            // --- OrderFormDto → Order (создание заказа) ---
            CreateMap<OrderFormDto, Order>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.CustomerId, o => o.MapFrom(s => s.CustomerId))
                .ForMember(d => d.OrderNumber, o => o.MapFrom(s => s.OrderNumber))
                .ForMember(d => d.Priority, o => o.MapFrom(s => s.Priority))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status))
                .ForMember(d => d.TotalWeight, o => o.MapFrom(s => s.TotalWeight))
                .ForMember(d => d.TotalCost, o => o.MapFrom(s => s.TotalCost))
                .ForMember(d => d.CreationDate, o => o.MapFrom(s => s.CreationDate))
                .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment))
                .ForMember(d => d.Customer, o => o.Ignore())
                .ForMember(d => d.MetrologicalInfo, o => o.Ignore())
                .ForMember(d => d.OrderTaxProduct, o => o.Ignore())
                .ForMember(d => d.OrderTaxes, o => o.Ignore())
                .ForMember(d => d.OrdersProducts, o => o.Ignore())
                .ForMember(d => d.StatusHistory, o => o.Ignore())
                .ForMember(d => d.LastChangeDate, o => o.Ignore())
                .ForMember(d => d.DateCreationTKP, o => o.Ignore())
                .ForMember(d => d.LastStatusChangeDate, o => o.Ignore())
                .ForMember(d => d.ExchangeRate, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            // --- ExportOrderViewModel → Order (обновление заказа при сохранении) ---
            // Игнорируем поля, которых нет в DTO, и не перезаписываем существующие
            // поля нулями, если в модели их нет.
            CreateMap<ExportOrderViewModel, Order>()
                .ForMember(d => d.ExchangeRate, o => o.MapFrom(s => s.ExchangeRate))
                .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment))
                .ForMember(d => d.DateCreationTKP, o => o.MapFrom(s =>
                    s.DateCreationTKP ?? default))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.CustomerId, o => o.Ignore())
                .ForMember(d => d.Customer, o => o.Ignore())
                .ForMember(d => d.OrderNumber, o => o.Ignore())
                .ForMember(d => d.Priority, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.TotalWeight, o => o.Ignore())
                .ForMember(d => d.TotalCost, o => o.Ignore())
                .ForMember(d => d.CreationDate, o => o.Ignore())
                .ForMember(d => d.LastChangeDate, o => o.Ignore())
                .ForMember(d => d.LastStatusChangeDate, o => o.Ignore())
                .ForMember(d => d.MetrologicalInfo, o => o.Ignore())
                .ForMember(d => d.OrderTaxProduct, o => o.Ignore())
                .ForMember(d => d.OrderTaxes, o => o.Ignore())
                .ForMember(d => d.OrdersProducts, o => o.Ignore())
                .ForMember(d => d.StatusHistory, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            // --- ProductData → OrderProduct ---
            CreateMap<ProductData, OrderProduct>()
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.OrderId, o => o.Ignore())
                .ForMember(d => d.ProductId, o => o.MapFrom(s => s.ProductId))
                .ForMember(d => d.Quantity, o => o.MapFrom(s => s.Quantity))
                .ForMember(d => d.Price, o => o.MapFrom(s => s.Price))
                .ForMember(d => d.Weight, o => o.MapFrom(s => s.Weight))
                .ForMember(d => d.LeadTime, o => o.MapFrom(s => s.LeadTime ?? 0))
                .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment ?? string.Empty))
                .ForMember(d => d.DeliveryDate, o => o.MapFrom(s =>
                    s.DeliveryDate ?? DateTime.Now))
                .ForMember(d => d.TotalPrice, o => o.MapFrom(s => s.Price * s.Quantity))
                .ForMember(d => d.Order, o => o.Ignore())
                .ForMember(d => d.Product, o => o.Ignore())
                .ForMember(d => d.MetrologicalInfo, o => o.Ignore())
                .ForMember(d => d.OrdersTaxes, o => o.Ignore())
                .ForMember(d => d.IsDeleted, o => o.Ignore());

            // --- OrderTaxProduct → OrderTaxProductViewModel (для Details) ---
            CreateMap<OrderTaxProduct, OrderTaxProductViewModel>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Name, o => o.MapFrom(s =>
                    s.TaxType != null ? s.TaxType.Name : "Не указано"))
                .ForMember(d => d.Value, o => o.MapFrom(s => s.Cost))
                .ForMember(d => d.MeasureUnit, o => o.MapFrom(s =>
                    s.TaxType != null && s.TaxType.MeasureUnit != null
                        ? s.TaxType.MeasureUnit.Name
                        : "₽"))
                .ForMember(d => d.CalculationType, o => o.Ignore())
                .ForMember(d => d.Formula, o => o.Ignore())
                .ForMember(d => d.IsCalculated, o => o.MapFrom(s => s.IsCalculated));
        }
    }
}
