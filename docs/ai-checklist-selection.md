# AIチェックリスト選択機能 仕様書

## 概要

PromptRunnerのAIチェックリスト選択機能は、ユーザーが入力したテキストの内容を分析し、最も適切なチェックリスト項目を自動的に選択する機能です。

この機能により、ユーザーは約50個のチェックリスト項目から手動で選択する手間を省き、AIが推奨する3〜10個の項目にすぐに取り組むことができます。

## 処理フロー

```
[ユーザー入力]
    ↓
[事実抽出] (オプション)
    ↓
[全チェックリスト項目取得]
    ↓
[AIに項目選択依頼]
    ↓
[JSON応答をパース]
    ↓
[推奨項目を表示]
    ↓
[ユーザーが確認・調整]
    ↓
[実行]
```

## コンポーネント構成

### 1. ChecklistService
**責務**: チェックリスト項目のマスターデータ管理

**主要メソッド**:
- `GetAllChecklistItems()`: 全チェックリスト項目を取得

**チェックリスト項目の構成**:
- **8カテゴリ**: 要約系、分析系、アイデア出し系、文章化系、翻訳系、学習系、企画・設計系、実行支援系
- **合計約50項目**: 各カテゴリに複数の項目が登録されている

**項目の構造**:
```csharp
public class ChecklistItem
{
    public string Id { get; set; }              // 一意のID (例: "summary_full")
    public string Title { get; set; }           // 表示タイトル (例: "全体要約")
    public string Description { get; set; }     // 説明
    public ChecklistCategory Category { get; set; } // カテゴリ
    public string PromptTemplate { get; set; }  // プロンプトテンプレート
    public bool IsEnabled { get; set; }         // 選択状態
    public double Confidence { get; set; }      // AIの信頼度 (0.0〜1.0)
    public string Reason { get; set; }          // AIの選択理由
    public int Order { get; set; }              // 表示順序
    public string UserNote { get; set; }        // ユーザーの補足条件
}
```

### 2. LLMService
**責務**: Google Gemini APIを使用したAI処理

**主要メソッド**:
- `SelectItemsAsync(InputData inputData, List<ChecklistItem> allItems)`: チェックリスト項目の自動選択

#### SelectItemsAsyncの処理詳細

**入力**:
- `inputData`: ユーザー入力テキストと抽出された事実
- `allItems`: 全チェックリスト項目リスト（約50項目）

**処理ステップ**:

1. **項目情報の整形**
   ```
   - ID: summary_full, タイトル: 全体要約, カテゴリ: 要約系, 説明: 文章全体を簡潔に要約します
   - ID: summary_keypoints, タイトル: 要点抽出, カテゴリ: 要約系, 説明: 重要なポイントを箇条書きで抽出します
   ...（全項目）
   ```

2. **プロンプト生成**
   ```
   あなたは入力テキストを分析し、適切なチェックリスト項目を選択するアシスタントです。

   【入力テキスト】
   {ユーザーの入力テキスト}

   【利用可能なチェックリスト項目】
   {整形された全項目リスト}

   【タスク】
   入力テキストの内容に基づいて、最も適切なチェックリスト項目を3〜10個選択してください。
   各項目について、選択理由と信頼度（0.0〜1.0）を提供してください。

   【出力形式】
   以下のJSON形式で回答してください。それ以外の文章は一切含めないでください:
   ```json
   [
     {"id": "項目ID", "confidence": 0.8, "reason": "選択理由"},
     ...
   ]
   ```
   ```

3. **API呼び出し**
   - Google Gemini APIの`GenerateContent`メソッドを呼び出し
   - 設定で選択されたモデル（gemini-2.5-flash等）を使用

4. **JSON応答の抽出とパース**
   - 正規表現 `\[[\s\S]*?\]` でJSON配列を抽出
   - `System.Text.Json`でデシリアライズ
   - 内部クラス`SelectionResult`にマッピング

5. **チェックリスト項目の生成**
   - AIが選択した各IDに対応する元の項目を検索
   - 以下の情報を設定:
     - `IsEnabled = true`: 選択状態
     - `Confidence`: AIの信頼度
     - `Reason`: AIの選択理由
     - `Order`: 選択順序（0から順番に）

**出力**:
- AIが選択したチェックリスト項目のリスト（3〜10個）
- 各項目にConfidence（信頼度）とReason（選択理由）が含まれる

**エラーハンドリング**:
- API呼び出しエラー時: フォールバック処理で最初の3項目を返す
- JSON解析エラー時: 空リストを返す
- 選択項目が3個未満: フォールバック処理で最初の3項目を返す

#### フォールバック処理

AIによる選択が失敗した場合の安全機能:

```csharp
private Task<List<ChecklistItem>> FallbackSelectionAsync(List<ChecklistItem> allItems)
{
    var fallbackItems = allItems.Take(3).Select((item, index) => new ChecklistItem
    {
        // 元の項目情報をコピー
        Id = item.Id,
        Title = item.Title,
        Description = item.Description,
        Category = item.Category,
        PromptTemplate = item.PromptTemplate,

        // フォールバック用の値を設定
        IsEnabled = true,
        Confidence = 0.5,
        Reason = "デフォルト選択（API応答なし）",
        Order = index,
        UserNote = string.Empty
    }).ToList();

    return Task.FromResult(fallbackItems);
}
```

### 3. ChecklistViewModel
**責務**: チェックリスト画面のUI制御とデータ管理

**主要メソッド**:
- `LoadChecklistAsync(InputData inputData)`: AI分析ありでチェックリストを読み込み
- `LoadChecklistWithoutAnalysis(InputData inputData)`: AI分析なしで全項目を読み込み

#### LoadChecklistAsyncの処理詳細

1. **初期化**
   ```csharp
   IsLoading = true;
   StatusMessage = "AIがチェックリストを作成中...";
   ```

2. **チェックリスト項目取得**
   ```csharp
   var checklistService = _mainViewModel.GetChecklistService();
   var allItems = checklistService.GetAllChecklistItems();
   ```

3. **AI選択実行**
   ```csharp
   var llmService = _mainViewModel.GetLLMService();
   var recommendedItems = await llmService.SelectItemsAsync(inputData, allItems);
   ```

4. **カテゴリ別グループ化**
   - 8つのカテゴリごとにItemsControlを生成
   - 各カテゴリ内で、AIが推奨した項目は`IsEnabled = true`
   - 推奨されなかった項目は`IsEnabled = false`

5. **PropertyChanged監視**
   ```csharp
   item.PropertyChanged += (s, e) =>
   {
       if (e.PropertyName == nameof(ChecklistItem.IsEnabled))
       {
           UpdateAllGroupCounts();
       }
   };
   ```
   - ユーザーがチェックボックスを操作すると自動的にカウント更新

6. **ステータスメッセージ更新**
   ```
   "AIが{N}個の項目を推奨しました（全{M}項目から選択可能）"
   ```

## UI表示

### チェックリスト画面のレイアウト

```
+----------------------------------+------------------+
| カテゴリ別チェックリスト          | ✓ 選択済み       |
|                                  | 5 項目           |
| [要約系 (2/5 選択)]              +------------------+
|   ☑ 全体要約                     | ■ 全体要約       |
|     AI推奨 信頼度: 0.9           |   要約系         |
|     理由: 文章が長いため         |   AI推奨         |
|   ☐ 要点抽出                     +------------------+
|   ☐ 図解化                       | ■ SWOT分析      |
|                                  |   分析系         |
| [分析系 (3/12 選択)]             |   AI推奨         |
|   ☑ SWOT分析                     +------------------+
|     AI推奨 信頼度: 0.85          | ■ メリデメ整理   |
|   ☑ メリデメ整理                 |   分析系         |
|     AI推奨 信頼度: 0.8           |   AI推奨         |
|   ☐ 事実抽出                     +------------------+
|                                  |                  |
+----------------------------------+------------------+
```

### 視覚的特徴

**選択済み項目**:
- 背景色: 薄緑色 (#E8F8F5)
- AIが推奨した項目には緑色のバッジ表示

**右側の選択済みリスト**:
- ヘッダー: 緑色 (#27AE60)
- 選択項目数を大きく表示
- スクロール不要で一目で確認可能
- リアルタイムで自動更新

## AI選択のロジック

### AIの判断基準

Google Gemini APIは以下の観点で項目を選択します:

1. **テキストの性質**
   - 長文 → 要約系
   - 箇条書き → 文章化系
   - 質問形式 → 回答作成系

2. **目的の推測**
   - 意思決定が必要 → 分析系、企画系
   - アイデアが欲しい → アイデア出し系
   - 資料を作りたい → 文章化系

3. **内容の特性**
   - 専門用語が多い → 翻訳系、学習系
   - 行動が必要 → 実行支援系
   - 計画立案 → 企画・設計系

### 信頼度（Confidence）

- **0.9以上**: 非常に適切（強く推奨）
- **0.7〜0.9**: 適切（推奨）
- **0.5〜0.7**: まあまあ適切
- **0.5未満**: あまり適切でない（フォールバック）

### 選択理由（Reason）

AIは各項目について具体的な理由を提示:
- 例: "文章が長く全体像を把握しづらいため、要約が有効です"
- 例: "複数の選択肢があり、メリット・デメリットの整理が必要です"
- 例: "具体的な行動計画が必要な段階のため"

## 設定とカスタマイズ

### モデル選択

設定画面で以下のモデルを選択可能:
- gemini-2.5-flash（デフォルト）: 高速・コスト効率的
- gemini-2.5-pro: 高精度
- gemini-1.5-flash: 旧世代・高速
- gemini-1.5-pro: 旧世代・高精度
- gemma-3-27b-it: オープンモデル

### Temperature

- 設定値: 0.0〜1.0（デフォルト: 0.7）
- 高いほど創造的・多様な選択
- 低いほど確実・一貫した選択

### 最大出力トークン数

- 設定値: 1024〜32768（デフォルト: 8192）
- JSON応答を受け取るには最低1024は必要
- 大きくしても選択結果には影響が少ない

## エラーハンドリング

### API呼び出しエラー

**発生要因**:
- APIキー未設定
- ネットワークエラー
- API制限超過
- モデル名の誤り

**対処**:
1. エラーメッセージを表示
2. フォールバック処理で最初の3項目を選択
3. 理由を "デフォルト選択（API応答なし）" に設定

### JSON解析エラー

**発生要因**:
- AIが指示通りのJSON形式で応答しなかった
- 特殊文字によるJSON破損

**対処**:
1. 正規表現でJSON配列部分を抽出
2. デシリアライズ失敗時は空リストを返す
3. フォールバック処理に移行

### 選択項目不足

**発生要因**:
- AIが3個未満の項目しか選択しなかった
- JSON解析に成功したが内容が不十分

**対処**:
1. 選択項目数をチェック（< 3）
2. フォールバック処理で最初の3項目を選択

## パフォーマンス考慮事項

### 処理時間

- **事実抽出なし**: 2〜5秒
- **事実抽出あり**: 5〜10秒（2回のAPI呼び出し）

### API呼び出し回数

1. 事実抽出（オプション）: 1回
2. チェックリスト選択: 1回
3. 各項目の実行: N回（選択項目数）

### コスト最適化

- デフォルトでgemini-2.5-flashを使用（コスト効率的）
- 必要に応じてより高精度なモデルに切り替え可能
- 事実抽出はオプションで無効化可能

## セキュリティ

### APIキー管理

- 設定は `%AppData%\PromptRunner\user-settings.json` に保存
- `.gitignore`に追加済み（リポジトリに含まれない）
- パスワード形式の入力欄（表示/非表示切り替え可能）

### 入力データの扱い

- ユーザー入力はGoogle APIに送信される
- Google AI Studio利用規約に準拠
- ログには保存されない（実行結果のみ保存）

## 今後の拡張可能性

### 学習機能

- ユーザーの選択パターンを学習
- 頻繁に使用する項目の優先度を上げる
- ユーザー固有の推奨ロジックを構築

### カスタムチェックリスト

- ユーザー独自の項目を追加可能に
- 業界特化型のテンプレート提供
- チェックリストのインポート/エクスポート

### 多段階分析

1. 第1段階: カテゴリを選択（3〜5個）
2. 第2段階: カテゴリ内の項目を選択
3. より精密な推奨が可能

### A/Bテスト

- 複数のプロンプト戦略を試す
- 選択精度を定量的に評価
- より良いプロンプトに自動最適化

## 関連ファイル

| ファイルパス | 責務 |
|---|---|
| `Services/LLMService.cs` | AI処理の実装 |
| `Services/ChecklistService.cs` | チェックリスト項目のマスターデータ |
| `ViewModels/ChecklistViewModel.cs` | チェックリスト画面のロジック |
| `Views/ChecklistView.xaml` | チェックリスト画面のUI |
| `Models/ChecklistItem.cs` | チェックリスト項目のモデル |
| `Models/ChecklistCategory.cs` | カテゴリの定義 |
| `Models/InputData.cs` | 入力データのモデル |

## バージョン履歴

- **v1.0**: 初期実装
  - 8カテゴリ、約50項目
  - Google Gemini API統合
  - JSON形式の応答パース
  - フォールバック処理

---

最終更新日: 2026-01-12
