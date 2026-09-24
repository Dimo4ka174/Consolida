using Application.ViewModels.CustomerModel;
using Application.ViewModels.CompanyModel;
using Application.ViewModels.CityModel;
using AutoMapper;
using DB.Entity;

namespace Application.DataAccessLayer.Service.Common
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // City
            CreateMap<CityDto, City>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Companies, opt => opt.Ignore())
                .ReverseMap();

            // Company
            CreateMap<CompanyDto, Company>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CityId, opt => opt.MapFrom(src => src.CityId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                .ForMember(dest => dest.City, opt => opt.Ignore())
                .ForMember(dest => dest.Customers, opt => opt.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.CitiesList, opt => opt.Ignore())
                .ForMember(dest => dest.CountriesList, opt => opt.Ignore());

            // Customer
            CreateMap<CustomerDto, Customer>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.CompanyId))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.PositionJob, opt => opt.MapFrom(src => src.PositionJob))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.PreferredMethod, opt => opt.MapFrom(src => src.PreferredMethod))
                .ForMember(dest => dest.Company, opt => opt.Ignore())
                .ReverseMap()
                .ForMember(dest => dest.CompanyName, opt => opt.Ignore())
                .ForMember(dest => dest.CompaniesList, opt => opt.Ignore())
                .ForMember(dest => dest.PreferredMethodsList, opt => opt.Ignore());
        }
    }
}
