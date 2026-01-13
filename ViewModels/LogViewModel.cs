using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptRunner.Models;
using PromptRunner.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace PromptRunner.ViewModels
{
    public partial class LogViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private readonly MarkdownService _markdownService;

        [ObservableProperty]
        private ObservableCollection<ExecutionLog> logs = new();

        [ObservableProperty]
        private ExecutionLog? selectedLog;

        [ObservableProperty]
        private ExecutionLogItem? selectedLogItem;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private bool isMarkdownView = false;

        [ObservableProperty]
        private string markdownHtml = string.Empty;

        public LogViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _markdownService = new MarkdownService();
        }

        public void LoadLogs()
        {
            var logService = _mainViewModel.GetLogService();
            var allLogs = logService.GetAllLogs();

            Logs.Clear();
            foreach (var log in allLogs)
            {
                Logs.Add(log);
            }

            StatusMessage = $"{allLogs.Count}件のログが見つかりました";

            if (Logs.Count > 0)
            {
                SelectedLog = Logs[0];
            }
        }

        [RelayCommand]
        private void Back()
        {
            _mainViewModel.NavigateToInput();
        }

        [RelayCommand]
        private void DeleteLog(ExecutionLog log)
        {
            var result = System.Windows.MessageBox.Show(
                $"このログを削除しますか？\n実行日時: {log.ExecutedAt:yyyy/MM/dd HH:mm:ss}",
                "確認",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var logService = _mainViewModel.GetLogService();
                logService.DeleteLog(log.Id);
                LoadLogs();
            }
        }

        [RelayCommand]
        private void ClearAllLogs()
        {
            var result = System.Windows.MessageBox.Show(
                "すべてのログを削除しますか？この操作は元に戻せません。",
                "確認",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning
            );

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var logService = _mainViewModel.GetLogService();
                logService.ClearAllLogs();
                LoadLogs();
            }
        }

        [RelayCommand]
        private async Task ExportLogAsync(ExecutionLog log)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "テキストファイル (*.txt)|*.txt|Markdownファイル (*.md)|*.md",
                Title = "ログをエクスポート",
                FileName = $"PromptRunner_Log_{log.ExecutedAt:yyyyMMdd_HHmmss}"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("# PromptRunner 実行ログ");
                    sb.AppendLine($"実行日時: {log.ExecutedAt:yyyy年MM月dd日 HH:mm:ss}");
                    sb.AppendLine();
                    sb.AppendLine("## 入力テキスト");
                    sb.AppendLine(log.InputText);
                    sb.AppendLine();

                    if (!string.IsNullOrWhiteSpace(log.ExtractedFacts))
                    {
                        sb.AppendLine("## 抽出された事実");
                        sb.AppendLine(log.ExtractedFacts);
                        sb.AppendLine();
                    }

                    sb.AppendLine("## 実行結果");
                    sb.AppendLine();

                    foreach (var item in log.Items)
                    {
                        sb.AppendLine($"### {item.ItemTitle}");
                        sb.AppendLine();
                        sb.AppendLine("**使用したプロンプトテンプレート:**");
                        sb.AppendLine("```");
                        sb.AppendLine(item.PromptTemplate);
                        sb.AppendLine("```");
                        sb.AppendLine();

                        if (!string.IsNullOrWhiteSpace(item.UserNote))
                        {
                            sb.AppendLine($"**補足条件:** {item.UserNote}");
                            sb.AppendLine();
                        }

                        if (item.IsSuccess)
                        {
                            sb.AppendLine("**結果:**");
                            sb.AppendLine(item.ResultContent);
                        }
                        else
                        {
                            sb.AppendLine($"**エラー:** {item.ErrorMessage}");
                        }
                        sb.AppendLine();
                        sb.AppendLine("---");
                        sb.AppendLine();
                    }

                    await File.WriteAllTextAsync(dialog.FileName, sb.ToString());

                    System.Windows.MessageBox.Show("ログをエクスポートしました。", "成功",
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
        private void CopyPrompt(ExecutionLogItem logItem)
        {
            try
            {
                System.Windows.Clipboard.SetText(logItem.PromptTemplate);
                System.Windows.MessageBox.Show("プロンプトをクリップボードにコピーしました。", "成功",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"コピー中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void ToggleMarkdownView()
        {
            IsMarkdownView = !IsMarkdownView;
        }

        partial void OnSelectedLogChanged(ExecutionLog? value)
        {
            UpdateMarkdownHtml();
        }

        partial void OnIsMarkdownViewChanged(bool value)
        {
            UpdateMarkdownHtml();
        }

        private void UpdateMarkdownHtml()
        {
            if (SelectedLog != null && IsMarkdownView)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# PromptRunner 実行ログ");
                sb.AppendLine();
                sb.AppendLine($"**実行日時:** {SelectedLog.ExecutedAt:yyyy年MM月dd日 HH:mm:ss}");
                sb.AppendLine();
                sb.AppendLine("## 入力テキスト");
                sb.AppendLine();
                sb.AppendLine(SelectedLog.InputText);
                sb.AppendLine();

                if (!string.IsNullOrWhiteSpace(SelectedLog.ExtractedFacts))
                {
                    sb.AppendLine("## 抽出された事実");
                    sb.AppendLine();
                    sb.AppendLine(SelectedLog.ExtractedFacts);
                    sb.AppendLine();
                }

                sb.AppendLine("## 実行結果");
                sb.AppendLine();

                foreach (var item in SelectedLog.Items)
                {
                    sb.AppendLine($"### {item.ItemTitle}");
                    sb.AppendLine();
                    sb.AppendLine("**使用したプロンプトテンプレート:**");
                    sb.AppendLine();
                    sb.AppendLine("```");
                    sb.AppendLine(item.PromptTemplate);
                    sb.AppendLine("```");
                    sb.AppendLine();

                    if (!string.IsNullOrWhiteSpace(item.UserNote))
                    {
                        sb.AppendLine($"**補足条件:** {item.UserNote}");
                        sb.AppendLine();
                    }

                    if (item.IsSuccess)
                    {
                        sb.AppendLine("**結果:**");
                        sb.AppendLine();
                        sb.AppendLine(item.ResultContent);
                    }
                    else
                    {
                        sb.AppendLine($"**エラー:** {item.ErrorMessage}");
                    }
                    sb.AppendLine();
                    sb.AppendLine("---");
                    sb.AppendLine();
                }

                MarkdownHtml = _markdownService.ConvertToHtml(sb.ToString());
            }
        }
    }
}
