using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.OrderService;
using Application.ViewModels.OrderModel.Api;
using DB.Entity;
using DB.Entity.Enum;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class QuickCreateService : IQuickCreateService
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuickCreateService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CreatedCompanyDto> CreateCompanyAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new CreatedCompanyDto { Success = false, Message = "Название компании обязательно" };

            name = name.Trim();

            var existing = await _unitOfWork.GetRepository<Company>()
                .FindFirstOrDefault(c => c.Name == name && !c.IsDeleted);

            if (existing != null)
                return new CreatedCompanyDto { Success = false, Message = "Компания с таким названием уже существует" };

            var company = new Company
            {
                Name = name,
                Country = Country.Russia,
                IsDeleted = false
            };
            await _unitOfWork.GetRepository<Company>().Create(company);
            await _unitOfWork.SaveChangesAsync(ct);

            // Автосоздание служебных клиентов (как при ручном создании компании)
            var defaultCustomers = new[]
            {
                new { FirstName = "", LastName = "Заинтересованные лица" },
                new { FirstName = "", LastName = "–" }
            };

            foreach (var dc in defaultCustomers)
            {
                var exists = await _unitOfWork.GetRepository<Customer>()
                    .FindFirstOrDefault(c => c.CompanyId == company.Id &&
                                             c.LastName == dc.LastName &&
                                             c.FirstName == dc.FirstName &&
                                             !c.IsDeleted);
                if (exists == null)
                {
                    var customer = new Customer
                    {
                        CompanyId = company.Id,
                        FirstName = dc.FirstName,
                        LastName = dc.LastName,
                        IsDeleted = false
                    };
                    await _unitOfWork.GetRepository<Customer>().Create(customer);
                }
            }
            await _unitOfWork.SaveChangesAsync(ct);

            return new CreatedCompanyDto
            {
                Success = true,
                Message = "Компания создана",
                Id = company.Id,
                Name = company.Name
            };
        }

        public async Task<CreatedCustomerDto> CreateCustomerAsync(int? companyId, string firstName, string lastName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                return new CreatedCustomerDto { Success = false, Message = "Имя и фамилия обязательны" };

            if (!companyId.HasValue || companyId <= 0)
                return new CreatedCustomerDto { Success = false, Message = "Компания не выбрана" };

            var company = await _unitOfWork.GetRepository<Company>().GetById(companyId);
            if (company == null)
                return new CreatedCustomerDto { Success = false, Message = "Компания не найдена" };

            var customer = new Customer
            {
                CompanyId = companyId,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                IsDeleted = false
            };
            await _unitOfWork.GetRepository<Customer>().Create(customer);
            await _unitOfWork.SaveChangesAsync(ct);

            return new CreatedCustomerDto
            {
                Success = true,
                Message = "Клиент создан",
                Id = customer.Id,
                FullName = $"{customer.LastName} {customer.FirstName}"
            };
        }

        public async Task<CreatedManufacturerDto> CreateManufacturerAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new CreatedManufacturerDto { Success = false, Message = "Название обязательно" };

            name = name.Trim();

            var existing = await _unitOfWork.GetRepository<Manufacturer>()
                .FindFirstOrDefault(m => m.Name == name && !m.IsDeleted);

            if (existing != null)
                return new CreatedManufacturerDto
                {
                    Success = true,
                    Message = "Производитель уже существует",
                    Id = existing.Id,
                    Name = existing.Name
                };

            var manufacturer = new Manufacturer { Name = name, IsDeleted = false };
            await _unitOfWork.GetRepository<Manufacturer>().Create(manufacturer);
            await _unitOfWork.SaveChangesAsync(ct);

            return new CreatedManufacturerDto
            {
                Success = true,
                Message = "Производитель создан",
                Id = manufacturer.Id,
                Name = manufacturer.Name
            };
        }
    }
}
