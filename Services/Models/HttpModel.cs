namespace Services.Models
{
        public class ApiResult<T>
        {
            public bool IsSuccess { get; set; }
            public T? Data { get; set; }
            public string? ErrorMessage { get; set; }
        }
        public class ApiErrorResponse
        {
            public string reason { get; set; } = string.Empty;
        }
    
}
