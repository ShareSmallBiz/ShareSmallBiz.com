namespace ApiTestGenerator.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}