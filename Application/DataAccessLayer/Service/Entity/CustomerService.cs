using Application.DataAccessLayer.Interface.Entities;
using Application.DataAccessLayer.Interface.Common;
using Application.DataAccessLayer.Service.Common;
using Application.ViewModels.CustomerModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using DB.Entity.Enum;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Entity
{
    public class CustomerService : GenericService<Customer, CustomerDto>, ICustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFilterService<Customer> _filterService;
        private readonly ICascadeSoftDeleteService _cascadeDeleteService;

        public CustomerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IFilterService<Customer> filterService,
            ICascadeSoftDeleteService cascadeDeleteService)
            : base(unitOfWork, mapper, filterService)
        {
            _unitOfWork = unitOfWork;
            _filterService = filterService;
            _cascadeDeleteService = cascadeDeleteService;
        }

        protected override IQueryable<Customer> GetPagedQuery()
        {
            return _unitOfWork.GetRepository<Customer>()
                .GetQueryableWithIncludes(c => c.Company);
        }

        public async Task<PagedResult<CustomerDto>> GetFilteredPagedAsync(FilterParams parameters)
        {
            var query = GetPagedQuery();
            var result = _filterService.ApplyFilters(query, parameters);

            var companies = await _unitOfWork.GetRepository<Company>().GetList();
            var companySelectList = companies.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();

            var models = result.Data.Select(customer => new CustomerDto
            {
                Id = customer.Id,
                CompanyId = customer.CompanyId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                MiddleName = customer.MiddleName,
                PositionJob = customer.PositionJob,
                Email = customer.Email,
                Phone = customer.Phone,
                PreferredMethod = customer.PreferredMethod,
                CompanyName = customer.Company?.Name,
                CompaniesList = companySelectList
            }).ToList();

            return new PagedResult<CustomerDto>
            {
                Items = models,
                TotalItems = result.TotalItems,
                ErrorMessage = result.ErrorMessage
            };
        }

        public async Task<CustomerDto> GetCreateModelAsync()
        {
            var companies = await _unitOfWork.GetRepository<Company>().GetList();
            return new CustomerDto
            {
                CompaniesList = companies.Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString()
                }).ToList(),
                PreferredMethodsList = LoadPreferredMethods()
            };
        }

        public async Task<CustomerDto> GetEditModelAsync(int id)
        {
            var customer = await Repository.GetById(id);
            if (customer == null) return null;

            var companies = await _unitOfWork.GetRepository<Company>().GetList();
            var model = Mapper.Map<CustomerDto>(customer);
            model.CompaniesList = companies.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList();
            model.PreferredMethodsList = LoadPreferredMethods();
            return model;
        }

        public override async Task DeleteAsync(int? id, CancellationToken ct = default)
        {
            if (id.HasValue)
                await _cascadeDeleteService.DeleteCustomer(id.Value);
            else
                await base.DeleteAsync(id, ct);
        }

        private List<SelectListItem> LoadPreferredMethods()
        {
            return Enum.GetValues(typeof(PreferredMethod))
                .Cast<PreferredMethod>()
                .Select(pm => new SelectListItem
                {
                    Text = pm.GetDisplayName(),
                    Value = ((int)pm).ToString()
                })
                .ToList();
        }
    }
}
