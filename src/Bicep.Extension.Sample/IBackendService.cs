namespace Bicep.Extension.Sample
{
    public interface IBackendService
    {
        Task<bool> CreateOrUpdate(string json);
        Task<bool> Delete(string json);
        Task<bool> Get(string json);
        Task<bool> Preview(string json);
    }
}
