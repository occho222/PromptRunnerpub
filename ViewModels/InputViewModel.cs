using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptRunner.Models;
using System.Collections.ObjectModel;
using System.IO;

namespace PromptRunner.ViewModels
{
    public partial class InputViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private string inputText = string.Empty;

        [ObservableProperty]
        private bool isExtractingFacts;

        [ObservableProperty]
        private bool enableFactExtraction;

        [ObservableProperty]
        private ObservableCollection<ChecklistItem> favoriteItems = new();

        [ObservableProperty]
        private ObservableCollection<ChecklistCategoryGroup> favoriteCategoryGroups = new();

        public InputViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            LoadFavorites();
        }

        private void LoadFavorites()
        {
            var checklistService = _mainViewModel.GetChecklistService();
            var favoriteService = _mainViewModel.GetFavoriteService();

            var allItems = checklistService.GetAllChecklistItems();
            var favoriteIds = favoriteService.GetFavorites();

            FavoriteItems.Clear();
            var favoriteItemsList = new List<ChecklistItem>();

            foreach (var id in favoriteIds)
            {
                var item = allItems.FirstOrDefault(x => x.Id == id);
                if (item != null)
                {
                    item.IsFavorite = true;
                    FavoriteItems.Add(item);
                    favoriteItemsList.Add(item);
                }
            }

            // カテゴリー別にグループ化
            FavoriteCategoryGroups.Clear();

            var categories = new[]
            {
                (ChecklistCategory.Summary, "要約系"),
                (ChecklistCategory.Analysis, "分析系"),
                (ChecklistCategory.Ideation, "アイデア出し系"),
                (ChecklistCategory.Writing, "文章化系"),
                (ChecklistCategory.Translation, "翻訳系"),
                (ChecklistCategory.Learning, "学習系"),
                (ChecklistCategory.Planning, "企画・設計系"),
                (ChecklistCategory.Execution, "実行支援系")
            };

            foreach (var (category, name) in categories)
            {
                var categoryItems = favoriteItemsList.Where(x => x.Category == category).ToList();
                if (categoryItems.Count > 0)
                {
                    var group = new ChecklistCategoryGroup(name, category, categoryItems);
                    FavoriteCategoryGroups.Add(group);
                }
            }
        }

        [RelayCommand]
        private async Task AnalyzeAsync()
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                System.Windows.MessageBox.Show("入力テキストを入力してください。", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var inputData = new InputData
            {
                RawText = InputText,
                CreatedAt = DateTime.Now
            };

            // 事実抽出が有効な場合
            if (EnableFactExtraction)
            {
                IsExtractingFacts = true;
                try
                {
                    var llmService = _mainViewModel.GetLLMService();
                    inputData.ExtractedFacts = await llmService.ExtractFactsAsync(InputText);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"事実抽出中にエラーが発生しました: {ex.Message}", "エラー",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
                finally
                {
                    IsExtractingFacts = false;
                }
            }

            // チェックリスト画面に遷移
            _mainViewModel.NavigateToChecklist(inputData);
        }

        [RelayCommand]
        private void NextWithoutAnalysis()
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                System.Windows.MessageBox.Show("入力テキストを入力してください。", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var inputData = new InputData
            {
                RawText = InputText,
                CreatedAt = DateTime.Now
            };

            // AI分析なしでチェックリスト画面に遷移（全項目を手動選択可能）
            _mainViewModel.NavigateToChecklistWithoutAnalysis(inputData);
        }

        [RelayCommand]
        private void Clear()
        {
            InputText = string.Empty;
        }

        [RelayCommand]
        private async Task LoadFromFileAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "テキストファイル (*.txt)|*.txt|すべてのファイル (*.*)|*.*",
                Title = "ファイルを開く"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    InputText = await File.ReadAllTextAsync(dialog.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"ファイル読み込み中にエラーが発生しました: {ex.Message}", "エラー",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task ExecuteFavoriteAsync(ChecklistItem favoriteItem)
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                System.Windows.MessageBox.Show("入力テキストを入力してください。", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var inputData = new InputData
            {
                RawText = InputText,
                CreatedAt = DateTime.Now
            };

            // 事実抽出が有効な場合
            if (EnableFactExtraction)
            {
                IsExtractingFacts = true;
                try
                {
                    var llmService = _mainViewModel.GetLLMService();
                    inputData.ExtractedFacts = await llmService.ExtractFactsAsync(InputText);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"事実抽出中にエラーが発生しました: {ex.Message}", "エラー",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                finally
                {
                    IsExtractingFacts = false;
                }
            }

            // お気に入り項目のコピーを作成して実行
            var executionItem = new ChecklistItem
            {
                Id = favoriteItem.Id,
                Title = favoriteItem.Title,
                Description = favoriteItem.Description,
                Category = favoriteItem.Category,
                PromptTemplate = favoriteItem.PromptTemplate,
                IsEnabled = true,
                IsFavorite = true,
                Order = 0,
                UserNote = string.Empty,
                Confidence = 1.0,
                Reason = "お気に入りから実行"
            };

            var items = new List<ChecklistItem> { executionItem };
            _mainViewModel.NavigateToExecutionWithInputData(inputData, items);
        }
    }
}
