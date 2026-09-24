using Application.DataAccessLayer.Interface.Common;
using Application.ViewModels.CityModel;
using DB.Entity;

namespace Application.DataAccessLayer.Interface.Entities
{
    public interface ICityService : IGenericService<City, CityDto>
    {
        Task DeleteCityWithRelatedDataAsync(int id);
        Task<bool> CityExistsAsync(int id);
        Task<int> GetRelatedCompaniesCountAsync(int value);
    }
}
