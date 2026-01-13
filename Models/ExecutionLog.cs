namespace PromptRunner.Models
{
    public class ExecutionLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime ExecutedAt { get; set; } = DateTime.Now;
        public string InputText { get; set; } = string.Empty;
        public string? ExtractedFacts { get; set; }
        public List<ExecutionLogItem> Items { get; set; } = new();
    }

    public class ExecutionLogItem
    {
        public string ItemId { get; set; } = string.Empty;
        public string ItemTitle { get; set; } = string.Empty;
        public string PromptTemplate { get; set; } = string.Empty;
        public string UserNote { get; set; } = string.Empty;
        public string ResultContent { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
