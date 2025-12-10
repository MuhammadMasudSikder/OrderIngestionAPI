namespace OrderIngestionAPI.Validators
{
    public class ValidationErrorResponse
    {
        public string Status => "ValidationFailed";
        public List<ErrorDetail> Errors { get; set; } = new();
    }

    public class ErrorDetail
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
