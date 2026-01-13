using Mscc.GenerativeAI;
using PromptRunner.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PromptRunner.Services
{
    public class LLMService : ILLMService
    {
        private readonly GenerativeModel _model;
        private readonly string _modelName;
        private readonly IConfigService _configService;

        public LLMService(IConfigService configService)
        {
            _configService = configService;
            var settings = _configService.CurrentSettings;

            System.Diagnostics.Debug.WriteLine("=== LLMService初期化 ===");
            System.Diagnostics.Debug.WriteLine($"APIキー: {(string.IsNullOrWhiteSpace(settings.ApiKey) ? "未設定" : $"設定済み({settings.ApiKey.Length}文字)")}");
            System.Diagnostics.Debug.WriteLine($"モデル名: {settings.ModelName}");

            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new InvalidOperationException("Google AI APIキーが設定されていません。設定画面からAPIキーを入力してください。");
            }

            _modelName = settings.ModelName;

            try
            {
                var googleAI = new GoogleAI(settings.ApiKey);
                _model = googleAI.GenerativeModel(model: _modelName);
                System.Diagnostics.Debug.WriteLine("LLMService初期化完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LLMService初期化エラー: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ChecklistItem>> SelectItemsAsync(InputData inputData, List<ChecklistItem> allItems)
        {
            var selectedItems = new List<ChecklistItem>();

            try
            {
                // チェックリスト項目の情報を整形
                var itemsInfo = new StringBuilder();
                foreach (var item in allItems)
                {
                    itemsInfo.AppendLine($"- ID: {item.Id}, タイトル: {item.Title}, カテゴリ: {GetCategoryName(item.Category)}, 説明: {item.Description}");
                }

                var prompt = $@"あなたは入力テキストを分析し、適切なチェックリスト項目を選択するアシスタントです。

【入力テキスト】
{inputData.RawText}

【利用可能なチェックリスト項目】
{itemsInfo}

【タスク】
入力テキストの内容に基づいて、最も適切なチェックリスト項目を3〜10個選択してください。
各項目について、選択理由と信頼度（0.0〜1.0）を提供してください。

【出力形式】
以下のJSON形式で回答してください。それ以外の文章は一切含めないでください:
```json
[
  {{""id"": ""項目ID"", ""confidence"": 0.8, ""reason"": ""選択理由""}},
  ...
]
```";

                var response = await _model.GenerateContent(prompt);
                var responseText = response.Text ?? "";

                // JSONを抽出してパース
                var jsonMatch = Regex.Match(responseText, @"\[[\s\S]*?\]");
                if (jsonMatch.Success)
                {
                    var jsonText = jsonMatch.Value;
                    var selections = JsonSerializer.Deserialize<List<SelectionResult>>(jsonText, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (selections != null)
                    {
                        int order = 0;
                        foreach (var selection in selections)
                        {
                            var originalItem = allItems.FirstOrDefault(x => x.Id == selection.Id);
                            if (originalItem != null)
                            {
                                selectedItems.Add(new ChecklistItem
                                {
                                    Id = originalItem.Id,
                                    Title = originalItem.Title,
                                    Description = originalItem.Description,
                                    Category = originalItem.Category,
                                    PromptTemplate = originalItem.PromptTemplate,
                                    IsEnabled = true,
                                    Confidence = selection.Confidence,
                                    Reason = selection.Reason ?? "AIによる選択",
                                    Order = order++,
                                    UserNote = string.Empty
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SelectItemsAsync エラー: {ex.Message}");
                // エラー時はフォールバックとして最初の3項目を選択
                return await FallbackSelectionAsync(allItems);
            }

            // 結果が少なすぎる場合はフォールバック
            if (selectedItems.Count < 3)
            {
                return await FallbackSelectionAsync(allItems);
            }

            return selectedItems;
        }

        public async Task<ExecutionResult> ExecuteItemAsync(ChecklistItem item, InputData inputData)
        {
            try
            {
                // プロンプトテンプレートに値を埋め込む
                var prompt = item.PromptTemplate
                    .Replace("{InputText}", inputData.RawText)
                    .Replace("{UserNote}", string.IsNullOrEmpty(item.UserNote) ? "なし" : item.UserNote)
                    .Replace("{Facts}", inputData.ExtractedFacts ?? "なし");

                var response = await _model.GenerateContent(prompt);
                var responseText = response.Text ?? "";

                return new ExecutionResult
                {
                    ItemId = item.Id,
                    ItemTitle = item.Title,
                    Content = responseText,
                    CompletedAt = DateTime.Now,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                return new ExecutionResult
                {
                    ItemId = item.Id,
                    ItemTitle = item.Title,
                    Content = string.Empty,
                    CompletedAt = DateTime.Now,
                    IsSuccess = false,
                    ErrorMessage = $"API呼び出しエラー: {ex.Message}"
                };
            }
        }

        public async Task<string> ExtractFactsAsync(string inputText)
        {
            try
            {
                var prompt = $@"以下のテキストから重要な事実・情報を抽出してください。

【入力テキスト】
{inputText}

【タスク】
以下の形式で、入力テキストに含まれる重要な事実や情報を箇条書きで抽出してください：
- 主要なトピックやテーマ
- 登場する人物、組織、製品名
- 日時、数値、統計情報
- 問題点や課題
- 提案やアイデア
- その他重要な情報

各事実は独立した行で、「・」で始めてください。";

                var response = await _model.GenerateContent(prompt);
                return response.Text ?? "事実を抽出できませんでした。";
            }
            catch (Exception ex)
            {
                return $"事実抽出エラー: {ex.Message}";
            }
        }

        private Task<List<ChecklistItem>> FallbackSelectionAsync(List<ChecklistItem> allItems)
        {
            var fallbackItems = allItems.Take(3).Select((item, index) => new ChecklistItem
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Category = item.Category,
                PromptTemplate = item.PromptTemplate,
                IsEnabled = true,
                Confidence = 0.5,
                Reason = "デフォルト選択（API応答なし）",
                Order = index,
                UserNote = string.Empty
            }).ToList();

            return Task.FromResult(fallbackItems);
        }

        private string GetCategoryName(ChecklistCategory category)
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
                _ => "不明"
            };
        }

        // JSON逆シリアル化用のヘルパークラス
        private class SelectionResult
        {
            public string Id { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public string? Reason { get; set; }
        }
    }
}
