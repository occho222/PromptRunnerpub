namespace PromptRunner.Models
{
    public class ExecutionResult
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemTitle { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
