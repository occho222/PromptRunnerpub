using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptRunner.Models;
using PromptRunner.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace PromptRunner.ViewModels
{
    public partial class ChecklistManagerViewModel : ObservableObject
    {
        private readonly IChecklistService _checklistService;

        [ObservableProperty]
        private ObservableCollection<ChecklistItem> items = new();

        [ObservableProperty]
        private ChecklistItem? selectedItem;

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private string editId = string.Empty;

        [ObservableProperty]
        private string editTitle = string.Empty;

        [ObservableProperty]
        private string editDescription = string.Empty;

        [ObservableProperty]
        private ChecklistCategory editCategory = ChecklistCategory.Summary;

        [ObservableProperty]
        private string editPromptTemplate = string.Empty;

        [ObservableProperty]
        private string searchText = string.Empty;

        public List<ChecklistCategory> AvailableCategories { get; } = new()
        {
            ChecklistCategory.Summary,
            ChecklistCategory.Analysis,
            ChecklistCategory.Ideation,
            ChecklistCategory.Writing,
            ChecklistCategory.Translation,
            ChecklistCategory.Learning,
            ChecklistCategory.Planning,
            ChecklistCategory.Execution
        };

        public ChecklistManagerViewModel(IChecklistService checklistService)
        {
            _checklistService = checklistService;
            LoadItems();
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterItems();
        }

        private void LoadItems()
        {
            var allItems = _checklistService.GetAllChecklistItems();
            Items.Clear();
            foreach (var item in allItems.OrderBy(i => i.Category).ThenBy(i => i.Title))
            {
                Items.Add(item);
            }
        }

        private void FilterItems()
        {
            var allItems = _checklistService.GetAllChecklistItems();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                Items.Clear();
                foreach (var item in allItems.OrderBy(i => i.Category).ThenBy(i => i.Title))
                {
                    Items.Add(item);
                }
                return;
            }

            var searchLower = SearchText.ToLower();
            var filtered = allItems.Where(i =>
                i.Title.ToLower().Contains(searchLower) ||
                i.Description.ToLower().Contains(searchLower) ||
                i.PromptTemplate.ToLower().Contains(searchLower) ||
                i.Id.ToLower().Contains(searchLower))
                .OrderBy(i => i.Category)
                .ThenBy(i => i.Title);

            Items.Clear();
            foreach (var item in filtered)
            {
                Items.Add(item);
            }
        }

        [RelayCommand]
        private void AddNew()
        {
            IsEditMode = true;
            EditId = $"custom_{Guid.NewGuid():N}";
            EditTitle = string.Empty;
            EditDescription = string.Empty;
            EditCategory = ChecklistCategory.Summary;
            EditPromptTemplate = "以下の内容について処理してください。\n\n{InputText}\n\n{UserNote}";
            SelectedItem = null;
        }

        [RelayCommand]
        private void Edit()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("編集する項目を選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsEditMode = true;
            EditId = SelectedItem.Id;
            EditTitle = SelectedItem.Title;
            EditDescription = SelectedItem.Description;
            EditCategory = SelectedItem.Category;
            EditPromptTemplate = SelectedItem.PromptTemplate;
        }

        [RelayCommand]
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditTitle))
            {
                MessageBox.Show("タイトルを入力してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditPromptTemplate))
            {
                MessageBox.Show("プロンプトテンプレートを入力してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var item = new ChecklistItem(EditId, EditTitle, EditDescription, EditCategory, EditPromptTemplate);

            // 既存項目の更新か新規追加かを判定
            if (_checklistService.GetAllChecklistItems().Any(i => i.Id == EditId))
            {
                _checklistService.UpdateItem(item);
            }
            else
            {
                _checklistService.AddItem(item);
            }

            IsEditMode = false;
            LoadItems();
            MessageBox.Show("保存しました。", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditMode = false;
        }

        [RelayCommand]
        private void Delete()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("削除する項目を選択してください。", "エラー",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"「{SelectedItem.Title}」を削除しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _checklistService.DeleteItem(SelectedItem.Id);
                LoadItems();
                MessageBox.Show("削除しました。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        [RelayCommand]
        private async Task ExportAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSONファイル (*.json)|*.json",
                Title = "チェックリストをエクスポート",
                FileName = $"checklist_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var items = _checklistService.ExportItems();
                    var json = JsonSerializer.Serialize(items, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    });
                    await File.WriteAllTextAsync(dialog.FileName, json);

                    MessageBox.Show("エクスポートが完了しました。", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"エクスポートに失敗しました: {ex.Message}", "エラー",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task ImportAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSONファイル (*.json)|*.json",
                Title = "チェックリストをインポート"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    "インポートすると現在のチェックリストがすべて置き換えられます。よろしいですか？",
                    "確認",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(dialog.FileName);
                        var items = JsonSerializer.Deserialize<List<ChecklistItem>>(json);

                        if (items == null || items.Count == 0)
                        {
                            MessageBox.Show("有効なチェックリストデータが見つかりませんでした。", "エラー",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        _checklistService.ImportItems(items);
                        LoadItems();
                        MessageBox.Show($"{items.Count}個の項目をインポートしました。", "成功",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"インポートに失敗しました: {ex.Message}", "エラー",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        [RelayCommand]
        private void ResetToDefault()
        {
            var result = MessageBox.Show(
                "チェックリストをデフォルトに戻しますか？カスタマイズした内容はすべて失われます。",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _checklistService.ResetToDefault();
                LoadItems();
                MessageBox.Show("デフォルトに戻しました。", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public string GetCategoryName(ChecklistCategory category)
        {
            return category switch
            {
                ChecklistCategory.Summary => "要約系",
                ChecklistCategory.Analysis => "分析系",
                ChecklistCategory.Ideation => "アイデア出し系",
                ChecklistCategory.Writing => "文章化系",
                ChecklistCategory.Translation => "翻訳系",
                ChecklistCategory.Learning => "学習系",
                ChecklistCategory.Planning => "企画・設計系",
                ChecklistCategory.Execution => "実行支援系",
                _ => category.ToString()
            };
        }
    }
}
