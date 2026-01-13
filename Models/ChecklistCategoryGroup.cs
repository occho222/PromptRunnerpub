using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace PromptRunner.Models
{
    public partial class ChecklistCategoryGroup : ObservableObject
    {
        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private ChecklistCategory category;

        [ObservableProperty]
        private ObservableCollection<ChecklistItem> items = new();

        [ObservableProperty]
        private bool isExpanded = true;

        [ObservableProperty]
        private int enabledCount;

        public ChecklistCategoryGroup()
        {
        }

        public ChecklistCategoryGroup(string name, ChecklistCategory category, List<ChecklistItem> items)
        {
            Name = name;
            Category = category;
            Items = new ObservableCollection<ChecklistItem>(items);
            UpdateEnabledCount();
        }

        public void UpdateEnabledCount()
        {
            EnabledCount = Items.Count(x => x.IsEnabled);
        }
    }
}
