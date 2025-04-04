using Services.Models;

namespace Services.Interfaces
{
    public  interface IHttpService
    {
        //Task<ApiResult<T>> ProcessHttpResponse<T>(HttpResponseMessage response) where T : class;
        Task<ApiResult<T>> SendGetRequest<T>(string url, bool useProxy = false) where T : class;
        Task<ApiResult<T>> SendPostRequest<T>(string url, object payload, bool useProxy = false) where T : class;
        Task<ApiResult<T>> SendMultipartPostRequest<T>(string url, MultipartFormDataContent payload, bool useProxy = false) where T : class;
    }
}
