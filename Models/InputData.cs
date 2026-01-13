namespace PromptRunner.Models
{
    public class InputData
    {
        public string RawText { get; set; } = string.Empty;
        public string? ExtractedFacts { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
