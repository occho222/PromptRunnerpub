using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptRunner.Models;
using PromptRunner.Services;

namespace PromptRunner.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IChecklistService _checklistService;
        private ILLMService? _llmService;
        private readonly ILogService _logService;
        private readonly IFavoriteService _favoriteService;
        private readonly IConfigService _configService;

        [ObservableProperty]
        private object? currentView;

        [ObservableProperty]
        private string currentStep = "入力";

        [ObservableProperty]
        private InputData? inputData;

        [ObservableProperty]
        private List<ChecklistItem>? checklistItems;

        public InputViewModel InputViewModel { get; }
        public ChecklistViewModel ChecklistViewModel { get; }
        public ExecutionViewModel ExecutionViewModel { get; }
        public LogViewModel LogViewModel { get; }

        public MainViewModel(IConfigService? configService = null)
        {
            _checklistService = new ChecklistService();
            _configService = configService ?? new ConfigService();
            _logService = new LogService();
            _favoriteService = new FavoriteService();

            InputViewModel = new InputViewModel(this);
            ChecklistViewModel = new ChecklistViewModel(this);
            ExecutionViewModel = new ExecutionViewModel(this);
            LogViewModel = new LogViewModel(this);

            // 初期画面を入力画面に設定
            CurrentView = InputViewModel;
        }

        public void NavigateToInput()
        {
            CurrentView = InputViewModel;
            CurrentStep = "入力";
        }

        public void NavigateToChecklist(InputData inputData)
        {
            InputData = inputData;
            ChecklistViewModel.LoadChecklistAsync(inputData);
            CurrentView = ChecklistViewModel;
            CurrentStep = "チェックリスト";
        }

        public void NavigateToChecklistWithoutAnalysis(InputData inputData)
        {
            InputData = inputData;
            ChecklistViewModel.LoadChecklistWithoutAnalysis(inputData);
            CurrentView = ChecklistViewModel;
            CurrentStep = "チェックリスト（手動選択）";
        }

        public void NavigateToExecution(List<ChecklistItem> selectedItems)
        {
            ChecklistItems = selectedItems;
            ExecutionViewModel.StartExecution(InputData!, selectedItems);
            CurrentView = ExecutionViewModel;
            CurrentStep = "実行";
        }

        public void NavigateToExecutionWithInputData(InputData inputData, List<ChecklistItem> selectedItems)
        {
            InputData = inputData;
            ChecklistItems = selectedItems;
            ExecutionViewModel.StartExecution(inputData, selectedItems);
            CurrentView = ExecutionViewModel;
            CurrentStep = "実行";
        }

        public void NavigateToLog()
        {
            LogViewModel.LoadLogs();
            CurrentView = LogViewModel;
            CurrentStep = "ログ履歴";
        }

        [RelayCommand]
        private void ShowLog()
        {
            NavigateToLog();
        }

        public IChecklistService GetChecklistService() => _checklistService;

        public ILLMService GetLLMService()
        {
            if (_llmService == null)
            {
                try
                {
                    _llmService = new LLMService(_configService);
                }
                catch (InvalidOperationException ex)
                {
                    System.Windows.MessageBox.Show(
                        $"{ex.Message}\n\nメニューの「設定」→「API設定」からAPIキーを設定してください。",
                        "API設定エラー",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    throw;
                }
            }
            return _llmService;
        }

        public ILogService GetLogService() => _logService;
        public IFavoriteService GetFavoriteService() => _favoriteService;

        /// <summary>
        /// LLMServiceをリセットして、次回使用時に設定を再読み込みする
        /// </summary>
        public void ResetLLMService()
        {
            _llmService = null;
            System.Diagnostics.Debug.WriteLine("LLMServiceをリセットしました。次回使用時に設定が再読み込みされます。");
        }
    }
}
