using PromptRunner.Models;

namespace PromptRunner.Services
{
    public interface ILLMService
    {
        Task<List<ChecklistItem>> SelectItemsAsync(InputData inputData, List<ChecklistItem> allItems);
        Task<ExecutionResult> ExecuteItemAsync(ChecklistItem item, InputData inputData);
        Task<string> ExtractFactsAsync(string inputText);
    }
}
