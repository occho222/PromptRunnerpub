using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptRunner.Models;
using PromptRunner.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PromptRunner.ViewModels
{
    public partial class ExecutionViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly MarkdownService _markdownService;

        [ObservableProperty]
        private ObservableCollection<ExecutionResult> results = new();

        [ObservableProperty]
        private bool isExecuting;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private int currentItemIndex;

        [ObservableProperty]
        private int totalItems;

        [ObservableProperty]
        private ExecutionResult? selectedResult;

        [ObservableProperty]
        private bool isMarkdownView = false;

        [ObservableProperty]
        private string markdownHtml = string.Empty;

        private InputData? _inputData;
        private List<ChecklistItem>? _items;

        public ExecutionViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _markdownService = new MarkdownService();
        }

        partial void OnSelectedResultChanged(ExecutionResult? value)
        {
            UpdateMarkdownHtml();
        }

        partial void OnIsMarkdownViewChanged(bool value)
        {
            UpdateMarkdownHtml();
        }

        private void UpdateMarkdownHtml()
        {
            if (SelectedResult != null && IsMarkdownView)
            {
                MarkdownHtml = _markdownService.ConvertToHtml(SelectedResult.Content);
            }
        }

        public async void StartExecution(InputData inputData, List<ChecklistItem> items)
        {
            _inputData = inputData;
            _items = items;
            Results.Clear();
            TotalItems = items.Count;
            CurrentItemIndex = 0;

            IsExecuting = true;

            var llmService = _mainViewModel.GetLLMService();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                CurrentItemIndex = i + 1;
                StatusMessage = $"実行中: {item.Title} ({CurrentItemIndex}/{TotalItems})";

                try
                {
                    var result = await llmService.ExecuteItemAsync(item, inputData);
                    Results.Add(result);
                }
                catch (Exception ex)
                {
                    var errorResult = new ExecutionResult
                    {
                        ItemId = item.Id,
                        ItemTitle = item.Title,
                        Content = string.Empty,
                        CompletedAt = DateTime.Now,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    };
                    Results.Add(errorResult);
                }
            }

            IsExecuting = false;
            StatusMessage = $"完了: {Results.Count(r => r.IsSuccess)}個の項目が正常に実行されました";

            // ログを保存
            SaveLog();

            // 最初の結果を選択
            if (Results.Count > 0)
            {
                SelectedResult = Results[0];
            }
        }

        private void SaveLog()
        {
            if (_inputData == null || _items == null) return;

            var logService = _mainViewModel.GetLogService();

            var log = new ExecutionLog
            {
                ExecutedAt = DateTime.Now,
                InputText = _inputData.RawText,
                ExtractedFacts = _inputData.ExtractedFacts,
                Items = new List<ExecutionLogItem>()
            };

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                var result = Results.FirstOrDefault(r => r.ItemId == item.Id);

                if (result != null)
                {
                    log.Items.Add(new ExecutionLogItem
                    {
                        ItemId = item.Id,
                        ItemTitle = item.Title,
                        PromptTemplate = item.PromptTemplate,
                        UserNote = item.UserNote,
                        ResultContent = result.Content,
                        IsSuccess = result.IsSuccess,
                        ErrorMessage = result.ErrorMessage
                    });
                }
            }

            logService.SaveLog(log);
        }

        [RelayCommand]
        private void Back()
        {
            _mainViewModel.NavigateToInput();
        }

        [RelayCommand]
        private async Task ExportAllAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "テキストファイル (*.txt)|*.txt|Markdownファイル (*.md)|*.md",
                Title = "エクスポート先を選択",
                FileName = $"PromptRunner_結果_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("# PromptRunner 実行結果");
                    sb.AppendLine($"実行日時: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("## 入力テキスト");
                    sb.AppendLine(_inputData?.RawText);
                    sb.AppendLine();

                    if (!string.IsNullOrWhiteSpace(_inputData?.ExtractedFacts))
                    {
                        sb.AppendLine("## 抽出された事実");
                        sb.AppendLine(_inputData.ExtractedFacts);
                        sb.AppendLine();
                    }

                    sb.AppendLine("## 実行結果");
                    sb.AppendLine();

                    foreach (var result in Results)
                    {
                        sb.AppendLine($"### {result.ItemTitle}");
                        sb.AppendLine();
                        if (result.IsSuccess)
                        {
                            sb.AppendLine(result.Content);
                        }
                        else
                        {
                            sb.AppendLine($"**エラー**: {result.ErrorMessage}");
                        }
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();
                    }

                    await File.WriteAllTextAsync(dialog.FileName, sb.ToString());

                    System.Windows.MessageBox.Show("エクスポートが完了しました。", "成功",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"エクスポート中にエラーが発生しました: {ex.Message}", "エラー",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task ExportSelectedAsync()
        {
            if (SelectedResult == null)
            {
                System.Windows.MessageBox.Show("エクスポートする結果を選択してください。", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "テキストファイル (*.txt)|*.txt|Markdownファイル (*.md)|*.md",
                Title = "エクスポート先を選択",
                FileName = $"{SelectedResult.ItemTitle}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"# {SelectedResult.ItemTitle}");
                    sb.AppendLine($"実行日時: {SelectedResult.CompletedAt:yyyy年MM月dd日 HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine(SelectedResult.Content);

                    await File.WriteAllTextAsync(dialog.FileName, sb.ToString());

                    System.Windows.MessageBox.Show("エクスポートが完了しました。", "成功",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"エクスポート中にエラーが発生しました: {ex.Message}", "エラー",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void ToggleViewMode()
        {
            IsMarkdownView = !IsMarkdownView;
        }

        [RelayCommand]
        private void CopyToClipboard()
        {
            if (SelectedResult == null)
            {
                System.Windows.MessageBox.Show("コピーする結果を選択してください。", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                System.Windows.Clipboard.SetText(SelectedResult.Content);
                System.Windows.MessageBox.Show("クリップボードにコピーしました。", "成功",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"コピー中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
