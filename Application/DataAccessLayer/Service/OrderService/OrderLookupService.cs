using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Interface.OrderService;
using Application.ViewModels.OrderModel.Api;
using DB.Entity;
using Microsoft.EntityFrameworkCore;

namespace Application.DataAccessLayer.Service.OrderService
{
    public class OrderLookupService : IOrderLookupService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderLookupService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<CodeLookupDto>> SearchCodesAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CodeLookupDto>();

            return await _unitOfWork.GetRepository<CodeTNVD>()
                .GetQueryable()
                .Where(c => c.Name.Contains(query))
                .OrderBy(c => c.Id)
                .Take(10)
                .Select(c => new CodeLookupDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Rate = c.Rate
                })
                .ToListAsync(ct);
        }

        public async Task<MetrologicalLookupDto> SearchMetrologicalInfoAsync(string number, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(number))
                return new MetrologicalLookupDto { Success = false };

            var trimmed = number.Trim();

            var info = await _unitOfWork.GetRepository<MetrologicalInfo>()
                .GetQueryable()
                .Where(m => m.RegistrationNumber == trimmed && !m.IsDeleted)
                .OrderByDescending(m => m.ExpiryDate)
                .Select(m => new
                {
                    m.Id,
                    m.RegistrationNumber,
                    m.ExpiryDate
                })
                .FirstOrDefaultAsync(ct);

            if (info == null)
                return new MetrologicalLookupDto { Success = false };

            return new MetrologicalLookupDto
            {
                Success = true,
                Id = info.Id,
                RegistrationNumber = info.RegistrationNumber,
                ExpiryDate = info.ExpiryDate.ToString("yyyy-MM-dd")
            };
        }

        public async Task<List<TaxTypeLookupDto>> SearchTaxTypesAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<TaxTypeLookupDto>();

            var lowerQuery = query.ToLower();

            return await _unitOfWork.GetRepository<TaxType>()
                .GetQueryable()
                .Include(tt => tt.MeasureUnit)
                .Where(tt => !tt.IsDeleted && tt.Name.ToLower().Contains(lowerQuery))
                .OrderBy(tt => tt.Id)
                .Take(10)
                .Select(tt => new TaxTypeLookupDto
                {
                    Id = tt.Id,
                    Name = tt.Name,
                    Cost = tt.Cost,
                    MeasureUnit = tt.MeasureUnit != null ? tt.MeasureUnit.Name : "₽",
                    IsPercentage = tt.MeasureUnit != null && tt.MeasureUnit.Name == "%"
                })
                .ToListAsync(ct);
        }

        public async Task<List<CompanyLookupDto>> SearchCompaniesAsync(string query, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<CompanyLookupDto>();

            var lowerQuery = query.ToLower();

            return await _unitOfWork.GetRepository<Company>()
                .GetQueryable()
                .Where(c => !c.IsDeleted && c.Name.ToLower().Contains(lowerQuery))
                .OrderBy(c => c.Id)
                .Take(10)
                .Select(c => new CompanyLookupDto
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync(ct);
        }

        public async Task<List<CustomerLookupDto>> GetCustomersByCompanyAsync(int companyId, CancellationToken ct = default)
        {
            return await _unitOfWork.GetRepository<Customer>()
                .GetQueryable()
                .Where(c => c.CompanyId == companyId && !c.IsDeleted)
                .Select(c => new CustomerLookupDto
                {
                    Id = c.Id,
                    LastName = c.LastName,
                    FirstName = c.FirstName
                })
                .ToListAsync(ct);
        }
    }
}
