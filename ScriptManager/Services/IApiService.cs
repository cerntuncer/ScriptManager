namespace ScriptManager.Services
{
    public interface IApiService
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<List<T>> GetListAsync<T>(string endpoint);
        Task<TResult?> PostAsync<TRequest, TResult>(string endpoint, TRequest request);
        Task<TResult?> PutAsync<TRequest, TResult>(string endpoint, TRequest request);
        Task<TResult?> DeleteAsync<TRequest, TResult>(string endpoint, TRequest request);
    }
}