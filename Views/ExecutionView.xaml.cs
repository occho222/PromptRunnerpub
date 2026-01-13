using System.Windows.Controls;
using PromptRunner.ViewModels;
using System.ComponentModel;

namespace PromptRunner.Views
{
    public partial class ExecutionView : UserControl
    {
        private bool _isWebViewInitialized = false;

        public ExecutionView()
        {
            InitializeComponent();
            Loaded += ExecutionView_Loaded;
        }

        private async void ExecutionView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // WebView2の初期化
            await MarkdownWebView.EnsureCoreWebView2Async(null);
            _isWebViewInitialized = true;

            // ViewModelのプロパティ変更を監視
            if (DataContext is ExecutionViewModel viewModel)
            {
                viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_isWebViewInitialized) return;

            if (e.PropertyName == nameof(ExecutionViewModel.MarkdownHtml))
            {
                var viewModel = DataContext as ExecutionViewModel;
                if (viewModel != null && !string.IsNullOrEmpty(viewModel.MarkdownHtml))
                {
                    MarkdownWebView.NavigateToString(viewModel.MarkdownHtml);
                }
            }
        }
    }
}
