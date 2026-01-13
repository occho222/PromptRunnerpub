using System.Windows;
using System.Windows.Controls;
using PromptRunner.ViewModels;

namespace PromptRunner.Views
{
    /// <summary>
    /// SettingsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsWindow(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            // イベントハンドラーを設定
            _viewModel.SettingsSaved += (s, e) => DialogResult = true;
            _viewModel.Cancelled += (s, e) => DialogResult = false;

            // 初期値をPasswordBoxに設定
            PasswordBoxApiKey.Password = _viewModel.ApiKey;

            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ウィンドウが読み込まれたらAPIキーフィールドにフォーカス
            PasswordBoxApiKey.Focus();
        }

        private void PasswordBoxApiKey_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && DataContext is SettingsViewModel vm)
            {
                vm.ApiKey = passwordBox.Password;
            }
        }
    }
}
