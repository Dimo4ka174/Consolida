namespace Application.DataAccessLayer.Interface.Common
{
    public interface IDBInitializer
    {
        Task Initialize(CancellationToken cancellationToken = default);
    }
}
