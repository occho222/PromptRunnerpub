using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PromptRunner.Models;
using System.Collections.ObjectModel;

namespace PromptRunner.ViewModels
{
    public partial class ChecklistViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;
        private InputData? _currentInputData;
        private List<ChecklistItem> _allChecklistItems = new();

        [ObservableProperty]
        private ObservableCollection<ChecklistCategoryGroup> categoryGroups = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        [ObservableProperty]
        private ChecklistItem? selectedItem;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ChecklistItem> selectedItemsForDisplay = new();

        public ChecklistViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilterChecklistItems();
        }

        public async void LoadChecklistAsync(InputData inputData)
        {
            _currentInputData = inputData;
            IsLoading = true;
            StatusMessage = "AIがチェックリストを作成中...";

            try
            {
                var checklistService = _mainViewModel.GetChecklistService();
                var llmService = _mainViewModel.GetLLMService();

                // すべてのチェックリスト項目を取得
                var allItems = checklistService.GetAllChecklistItems();
                _allChecklistItems = allItems;

                // LLMに項目選定を依頼
                var recommendedItems = await llmService.SelectItemsAsync(inputData, allItems);
                var recommendedIds = new HashSet<string>(recommendedItems.Select(x => x.Id));

                // カテゴリ別にグループ化
                InitializeCategories(allItems, recommendedIds, recommendedItems);

                var totalRecommended = recommendedItems.Count;
                StatusMessage = $"AIが{totalRecommended}個の項目を推奨しました（全{allItems.Count}項目から選択可能）";
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                System.Windows.MessageBox.Show($"チェックリスト作成中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LoadChecklistWithoutAnalysis(InputData inputData)
        {
            _currentInputData = inputData;
            IsLoading = true;
            StatusMessage = "チェックリストを読み込み中...";

            try
            {
                var checklistService = _mainViewModel.GetChecklistService();

                // すべてのチェックリスト項目を取得
                var allItems = checklistService.GetAllChecklistItems();
                _allChecklistItems = allItems;

                // AI分析なしなので、推奨項目はなし
                var recommendedIds = new HashSet<string>();
                var recommendedItems = new List<ChecklistItem>();

                // カテゴリ別にグループ化（すべて未選択状態）
                InitializeCategories(allItems, recommendedIds, recommendedItems);

                StatusMessage = $"全{allItems.Count}項目から選択してください（AI分析なし）";
            }
            catch (Exception ex)
            {
                StatusMessage = $"エラー: {ex.Message}";
                System.Windows.MessageBox.Show($"チェックリスト読み込み中にエラーが発生しました: {ex.Message}", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void InitializeCategories(List<ChecklistItem> allItems, HashSet<string> recommendedIds, List<ChecklistItem> recommendedItems)
        {
            CategoryGroups.Clear();

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
                var categoryItems = allItems.Where(x => x.Category == category).ToList();

                // 推奨された項目にチェックを入れ、信頼度と理由を設定
                foreach (var item in categoryItems)
                {
                    if (recommendedIds.Contains(item.Id))
                    {
                        var recommended = recommendedItems.First(x => x.Id == item.Id);
                        item.IsEnabled = true;
                        item.Confidence = recommended.Confidence;
                        item.Reason = recommended.Reason;
                        item.Order = recommended.Order;
                    }
                    else
                    {
                        item.IsEnabled = false;
                        item.Confidence = 0.0;
                        item.Reason = string.Empty;
                        item.Order = 999;
                    }

                    // PropertyChangedイベントを購読してカウントを更新
                    item.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ChecklistItem.IsEnabled))
                        {
                            UpdateAllGroupCounts();
                        }
                    };
                }

                var group = new ChecklistCategoryGroup(name, category, categoryItems);
                CategoryGroups.Add(group);
            }

            // お気に入りを読み込む
            LoadFavorites();

            // 選択済み項目リストを初期化
            UpdateSelectedItemsForDisplay();
        }

        [RelayCommand]
        private void ToggleItem(ChecklistItem item)
        {
            item.IsEnabled = !item.IsEnabled;
        }

        [RelayCommand]
        private void TogglePrompt(ChecklistItem item)
        {
            item.ShowPrompt = !item.ShowPrompt;
        }

        [RelayCommand]
        private void Back()
        {
            _mainViewModel.NavigateToInput();
        }

        [RelayCommand]
        private void Execute()
        {
            var selectedItems = GetAllSelectedItems();

            if (selectedItems.Count == 0)
            {
                System.Windows.MessageBox.Show("実行する項目を選択してください。", "エラー",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 順序でソート
            selectedItems = selectedItems.OrderBy(x => x.Order).ThenBy(x => x.Title).ToList();

            // 順序を更新
            for (int i = 0; i < selectedItems.Count; i++)
            {
                selectedItems[i].Order = i;
            }

            _mainViewModel.NavigateToExecution(selectedItems);
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var group in CategoryGroups)
            {
                foreach (var item in group.Items)
                {
                    item.IsEnabled = true;
                }
                group.UpdateEnabledCount();
            }
        }

        [RelayCommand]
        private void DeselectAll()
        {
            foreach (var group in CategoryGroups)
            {
                foreach (var item in group.Items)
                {
                    item.IsEnabled = false;
                }
                group.UpdateEnabledCount();
            }
        }

        [RelayCommand]
        private void SelectRecommended()
        {
            foreach (var group in CategoryGroups)
            {
                foreach (var item in group.Items)
                {
                    item.IsEnabled = item.Confidence > 0;
                }
                group.UpdateEnabledCount();
            }
        }

        [RelayCommand]
        private void ExpandAll()
        {
            foreach (var group in CategoryGroups)
            {
                group.IsExpanded = true;
            }
        }

        [RelayCommand]
        private void CollapseAll()
        {
            foreach (var group in CategoryGroups)
            {
                group.IsExpanded = false;
            }
        }

        private List<ChecklistItem> GetAllSelectedItems()
        {
            var result = new List<ChecklistItem>();
            foreach (var group in CategoryGroups)
            {
                result.AddRange(group.Items.Where(x => x.IsEnabled));
            }
            return result;
        }

        private void UpdateAllGroupCounts()
        {
            foreach (var group in CategoryGroups)
            {
                group.UpdateEnabledCount();
            }

            // 選択済み項目リストを更新
            UpdateSelectedItemsForDisplay();
        }

        private void UpdateSelectedItemsForDisplay()
        {
            var selectedItems = GetAllSelectedItems()
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Title)
                .ToList();

            SelectedItemsForDisplay.Clear();
            foreach (var item in selectedItems)
            {
                SelectedItemsForDisplay.Add(item);
            }
        }

        private void FilterChecklistItems()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // 検索テキストが空の場合は全て表示
                foreach (var group in CategoryGroups)
                {
                    foreach (var item in group.Items)
                    {
                        // すべて表示状態にする（Visibilityは使わず、フィルタリングしない）
                    }
                }
                return;
            }

            var searchLower = SearchText.ToLower();

            // 各グループの各項目をフィルタリング
            foreach (var group in CategoryGroups)
            {
                var filteredItems = _allChecklistItems
                    .Where(x => x.Category == group.Category)
                    .Where(x =>
                        x.Title.ToLower().Contains(searchLower) ||
                        x.Description.ToLower().Contains(searchLower) ||
                        x.PromptTemplate.ToLower().Contains(searchLower))
                    .ToList();

                group.Items.Clear();
                foreach (var item in filteredItems)
                {
                    group.Items.Add(item);
                }
                group.UpdateEnabledCount();
            }
        }

        [RelayCommand]
        private void ToggleFavorite(ChecklistItem item)
        {
            item.IsFavorite = !item.IsFavorite;
            SaveFavorites();
        }

        private void SaveFavorites()
        {
            var favoriteIds = _allChecklistItems.Where(x => x.IsFavorite).Select(x => x.Id).ToList();
            var favoriteService = _mainViewModel.GetFavoriteService();
            favoriteService.SaveFavorites(favoriteIds);
        }

        public void LoadFavorites()
        {
            var favoriteService = _mainViewModel.GetFavoriteService();
            var favoriteIds = favoriteService.GetFavorites();

            foreach (var item in _allChecklistItems)
            {
                item.IsFavorite = favoriteIds.Contains(item.Id);
            }
        }

        public string GetPromptPreview(ChecklistItem item)
        {
            if (_currentInputData == null) return item.PromptTemplate;
            return item.GetActualPrompt(_currentInputData.RawText, _currentInputData.ExtractedFacts);
        }
    }
}
