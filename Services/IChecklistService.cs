using PromptRunner.Models;

namespace PromptRunner.Services
{
    public interface IChecklistService
    {
        List<ChecklistItem> GetAllChecklistItems();
        List<ChecklistItem> GetItemsByCategory(ChecklistCategory category);
        void AddItem(ChecklistItem item);
        void UpdateItem(ChecklistItem item);
        void DeleteItem(string id);
        void ImportItems(List<ChecklistItem> items);
        List<ChecklistItem> ExportItems();
        void ResetToDefault();
    }
}
