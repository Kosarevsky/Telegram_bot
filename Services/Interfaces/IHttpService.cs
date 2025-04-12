using Services.Models;

namespace Services.Interfaces
{
    public  interface IHttpService
    {
        //Task<ApiResult<T>> ProcessHttpResponse<T>(HttpResponseMessage response) where T : class;
        Task<ApiResult<T>> SendGetRequest<T>(string url, bool useProxy = false, Func<HttpResponseMessage, bool> additionalSuccessPredicate = null) where T : class;
        Task<ApiResult<T>> SendPostRequest<T>(string url, object payload, bool useProxy = false) where T : class;
        Task<ApiResult<T>> SendMultipartPostRequest<T>(string url, MultipartFormDataContent payload, bool useProxy = false, Dictionary<string, string>? headers = null) where T : class;
        Task<ApiResult<T>> SendFormPostRequest<T>(string url, Dictionary<string, string> formData, bool useProxy = false) where T : class;
    }
}
