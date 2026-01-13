using CommunityToolkit.Mvvm.ComponentModel;

namespace PromptRunner.Models
{
    public partial class ChecklistItem : ObservableObject
    {
        [ObservableProperty]
        private string id = string.Empty;

        [ObservableProperty]
        private string title = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private ChecklistCategory category;

        [ObservableProperty]
        private string promptTemplate = string.Empty;

        [ObservableProperty]
        private bool isEnabled;

        [ObservableProperty]
        private double confidence;

        [ObservableProperty]
        private string reason = string.Empty;

        [ObservableProperty]
        private int order;

        [ObservableProperty]
        private string userNote = string.Empty;

        [ObservableProperty]
        private bool showPrompt;

        [ObservableProperty]
        private bool isFavorite;

        public ChecklistItem()
        {
        }

        public ChecklistItem(string id, string title, string description, ChecklistCategory category, string promptTemplate)
        {
            Id = id;
            Title = title;
            Description = description;
            Category = category;
            PromptTemplate = promptTemplate;
            IsEnabled = false;
            Confidence = 0.0;
            Reason = string.Empty;
            Order = 0;
            UserNote = string.Empty;
            IsFavorite = false;
        }

        public string GetActualPrompt(string inputText, string? facts = null)
        {
            return PromptTemplate
                .Replace("{InputText}", inputText ?? "[入力テキスト]")
                .Replace("{UserNote}", UserNote ?? "[補足条件なし]")
                .Replace("{Facts}", facts ?? "[事実抽出なし]");
        }
    }
}
