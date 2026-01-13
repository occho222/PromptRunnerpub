using System.Windows;
using PromptRunner.ViewModels;
using PromptRunner.Views;
using PromptRunner.Services;

namespace PromptRunner
{
    public partial class MainWindow : Window
    {
        private readonly IConfigService _configService;
        private readonly MainViewModel _mainViewModel;

        public MainWindow()
        {
            InitializeComponent();
            _configService = new ConfigService();
            _mainViewModel = new MainViewModel(_configService);
            DataContext = _mainViewModel;
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new SettingsViewModel(_configService);
            var settingsWindow = new SettingsWindow(viewModel)
            {
                Owner = this
            };

            var result = settingsWindow.ShowDialog();
            if (result == true)
            {
                // 設定が保存された場合、LLMServiceを再初期化
                _mainViewModel.ResetLLMService();
            }
        }

        private void OpenChecklistManager_Click(object sender, RoutedEventArgs e)
        {
            var checklistService = _mainViewModel.GetChecklistService();
            var viewModel = new ChecklistManagerViewModel(checklistService);
            var managerWindow = new ChecklistManagerWindow(viewModel)
            {
                Owner = this
            };

            managerWindow.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}