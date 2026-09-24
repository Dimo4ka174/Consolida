using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CompanyModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DB.Entity.Enum;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class CompanyService : GenericService<Company, CompanyDto>, ICompanyService
    {
        private readonly ICacheService<City> _cityCacheService;
        private readonly ICascadeSoftDeleteService _cascadeDeleteService;

        public CompanyService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<Company> filterService,
            ICacheService<Company> companyCacheService,
            ICacheService<City> cityCacheService,
            ICascadeSoftDeleteService cascadeDeleteService)
            : base(unitOfWork, mapper, filterService, companyCacheService)
        {
            _cityCacheService = cityCacheService;
            _cascadeDeleteService = cascadeDeleteService;
        }

        // Переопределяем маппинг списка, чтобы добавить CitiesList
        protected override async Task<List<CompanyDto>> MapToDtoListAsync(List<Company> entities, CancellationToken ct)
        {
            var cities = await _cityCacheService.GetCachedDataAsync(ct);
            var citySelectList = cities.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();

            var models = Mapper.Map<List<CompanyDto>>(entities);
            foreach (var model in models)
            {
                model.CitiesList = citySelectList;
            }
            return models;
        }

        protected override IQueryable<Company> GetPagedQuery()
        {
            return UnitOfWork.GetRepository<Company>().GetQueryableWithIncludes(c => c.City);
        }

        public override async Task<PagedResult<CompanyDto>> GetPagedAsync(FilterParams parameters, CancellationToken ct = default)
        {
            var query = GetPagedQuery();
            var result = FilterService.ApplyFilters(query, parameters);

            return new PagedResult<CompanyDto>
            {
                Items = await MapToDtoListAsync(result.Data, ct),
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            };
        }

        public async Task<CompanyDto> GetCreateModelAsync()
        {
            var cities = await _cityCacheService.GetCachedDataAsync();
            var model = new CompanyDto
            {
                CitiesList = cities.Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }).ToList(),
                CountriesList = LoadCountries()
            };
            return model;
        }

        public async Task<CompanyDto> GetEditModelAsync(int id)
        {
            var company = await Repository.GetById(id);
            if (company == null) return null;

            var model = Mapper.Map<CompanyDto>(company);
            var cities = await _cityCacheService.GetCachedDataAsync();
            model.CitiesList = cities.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();
            model.CountriesList = LoadCountries();
            return model;
        }

        public async Task<int> GetRelatedCustomersCountAsync(int companyId)
        {
            var customerRepo = UnitOfWork.GetRepository<Customer>();
            return await customerRepo.GetQueryable()
                .CountAsync(c => c.CompanyId == companyId && !c.IsDeleted);
        }

        public async Task CreateCompanyAsync(CompanyDto model)
        {
            var company = Mapper.Map<Company>(model);
            await Repository.Create(company);
            await UnitOfWork.SaveChangesAsync();

            // Создание стандартных клиентов
            var defaultCustomers = new[]
            {
                new { FirstName = "", LastName = "Заинтересованные лица" },
                new { FirstName = "", LastName = "–" }
            };

            var customerRepo = UnitOfWork.GetRepository<Customer>();
            foreach (var dc in defaultCustomers)
            {
                var exists = await customerRepo.FindFirstOrDefault(
                    c => c.CompanyId == company.Id && c.LastName == dc.LastName && c.FirstName == dc.FirstName && !c.IsDeleted);
                if (exists == null)
                {
                    var customer = new Customer
                    {
                        CompanyId = company.Id,
                        FirstName = dc.FirstName,
                        LastName = dc.LastName,
                        IsDeleted = false
                    };
                    await customerRepo.Create(customer);
                }
            }
            await UnitOfWork.SaveChangesAsync();

            await RefreshCacheAsync(CancellationToken.None);
        }

        public async Task UpdateCompanyAsync(CompanyDto model)
        {
            var company = await Repository.GetById(model.Id);
            if (company == null)
                throw new KeyNotFoundException($"Компания с ID {model.Id} не найдена");

            Mapper.Map(model, company);
            Repository.Update(company);
            await UnitOfWork.SaveChangesAsync();

            await RefreshCacheAsync(CancellationToken.None);
        }

        public async Task DeleteCompanyAsync(int id)
        {
            await _cascadeDeleteService.DeleteCompany(id);
            await RefreshCacheAsync(CancellationToken.None);
        }

        private List<SelectListItem> LoadCountries()
        {
            return Enum.GetValues(typeof(Country))
                .Cast<Country>()
                .Select(country => new SelectListItem
                {
                    Text = country.GetDisplayName(),
                    Value = ((int)country).ToString()
                })
                .ToList();
        }
    }
}