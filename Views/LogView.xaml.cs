using System.Windows.Controls;
using PromptRunner.ViewModels;
using System.ComponentModel;

namespace PromptRunner.Views
{
    public partial class LogView : UserControl
    {
        private bool _isWebViewInitialized = false;

        public LogView()
        {
            InitializeComponent();
            Loaded += LogView_Loaded;
        }

        private async void LogView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // WebView2の初期化
            await MarkdownWebView.EnsureCoreWebView2Async(null);
            _isWebViewInitialized = true;

            // ViewModelのプロパティ変更を監視
            if (DataContext is LogViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isWebViewInitialized) return;

            if (e.PropertyName == nameof(LogViewModel.MarkdownHtml))
            {
                var viewModel = DataContext as LogViewModel;
                if (viewModel != null && !string.IsNullOrEmpty(viewModel.MarkdownHtml))
                {
                    MarkdownWebView.NavigateToString(viewModel.MarkdownHtml);
                }
            }
        }
    }
}
