# PromptRunner

あなたのアイデアを"ブレない形"にするWPFアプリケーション

## 概要

PromptRunnerは、入力されたテキスト（文章、メモ、議事録、URLなど）を分析し、AIが自動的にチェックリストを作成し、選択された項目を順に実行して成果物を生成するアプリケーションです。

## 特徴

### 3ステップのワークフロー

1. **入力** - 文章、メモ、議事録、URL等を入力
2. **チェックリスト** - AIが自動的に実行すべき項目を選択（理由・信頼度付き）
3. **実行** - 選択された項目を順に実行して結果を表示

### 豊富なチェックリスト項目（60種類以上）

#### 要約系
- 全体要約、要点抽出、図解化、1枚企画書、FAQ化

#### 分析系
- SWOT分析、事実抽出、メリデメ整理、反論視点、代替案提案
- 評価軸設計、スコアリング表、期待効果と副作用、リスク分析、追加情報の洗い出し

#### アイデア出し系
- ブレスト、切り口出し、類推・アナロジー、制約付きアイデア、逆張り案
- やらないことリスト、ネーミング案、タイトル案、企画の骨子、具体化

#### 文章化系
- 文章化、構成案、文章構成（文章を整える）、レポート化、ブログ記事化、ストーリー仕立て
- メール文案、プレゼン原稿、スピーチ原稿

#### 翻訳系
- 専門文書の翻訳、口語化、ローカライゼーション、英日併記、文言の意味を調べる

#### 学習系
- クイズ化、暗記カード、練習問題、誤答指摘、ステップ学習
- 例題→類題→応用、口頭試問

#### 企画・設計系
- 目的→課題→打ち手→KPI、ロジックツリー、優先度付け、要件定義
- 仕様の曖昧さ検出、決めるべき事項、タスク分解、スケジュール案

#### 実行支援系
- 最初の30分、具体的な手順、台本、想定質問と回答
- 反対意見への返し方、会議アジェンダ案、合意形成の落とし所

## プロジェクト構造

```
PromptRunner/
├── Models/                     # データモデル
│   ├── ChecklistCategory.cs   # チェックリストカテゴリ
│   ├── ChecklistItem.cs        # チェックリスト項目
│   ├── ChecklistCategoryGroup.cs # カテゴリ別グループ
│   ├── InputData.cs            # 入力データ
│   ├── ExecutionResult.cs      # 実行結果
│   └── ExecutionLog.cs         # 実行ログ
├── ViewModels/                 # ビューモデル（MVVMパターン）
│   ├── MainViewModel.cs        # メインビューモデル
│   ├── InputViewModel.cs       # 入力画面
│   ├── ChecklistViewModel.cs   # チェックリスト画面
│   ├── ExecutionViewModel.cs   # 実行・結果表示画面
│   └── LogViewModel.cs         # ログ履歴画面
├── Views/                      # ビュー（XAML）
│   ├── InputView.xaml          # 入力画面UI
│   ├── ChecklistView.xaml      # チェックリスト画面UI
│   ├── ExecutionView.xaml      # 実行・結果表示画面UI
│   └── LogView.xaml            # ログ履歴画面UI
├── Services/                   # サービスレイヤー
│   ├── ChecklistService.cs     # チェックリスト項目管理
│   ├── LLMService.cs           # LLM API呼び出し
│   └── LogService.cs           # ログ管理
├── Converters/                 # XAMLコンバーター
│   ├── InverseBooleanConverter.cs
│   ├── InverseBooleanToVisibilityConverter.cs
│   └── StringToVisibilityConverter.cs
├── App.xaml                    # アプリケーションリソース
└── MainWindow.xaml             # メインウィンドウ
```

## 技術スタック

- **.NET 8.0** - 最新の.NETフレームワーク
- **WPF (Windows Presentation Foundation)** - Windowsデスクトップアプリケーション
- **MVVM (Model-View-ViewModel)** - アーキテクチャパターン
- **CommunityToolkit.Mvvm** - MVVMヘルパーライブラリ
- **Newtonsoft.Json** - JSON処理

## ビルドと実行

### 前提条件

- .NET 8.0 SDK以降
- Windows OS

### ビルド

```bash
cd C:\github\PromptRunner
dotnet build
```

### 実行

```bash
dotnet run
```

または、Visual Studioでソリューションを開いて実行します。

## LLM APIの接続

現在、`Services/LLMService.cs`は仮実装となっており、ダミーレスポンスを返しています。

実際のLLM APIを接続するには、以下の手順に従ってください：

### 1. APIクライアントライブラリの追加

```bash
# OpenAI APIの場合
dotnet add package OpenAI

# Anthropic Claude APIの場合
dotnet add package Anthropic.SDK

# その他のLLM APIの場合は適切なパッケージを追加
```

### 2. LLMService.csの実装

`Services/LLMService.cs`の以下のメソッドを実装してください：

- `SelectItemsAsync()` - 入力テキストに基づいてチェックリスト項目を選択
- `ExecuteItemAsync()` - チェックリスト項目を実行して成果物を生成
- `ExtractFactsAsync()` - 入力テキストから事実を抽出（オプション）

### 3. APIキーの管理

APIキーは環境変数または設定ファイルで管理してください：

```csharp
// 環境変数から読み込む例
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

// appsettings.jsonから読み込む例（要実装）
// var apiKey = Configuration["OpenAI:ApiKey"];
```

### 実装例（OpenAI API）

```csharp
using OpenAI;
using OpenAI.Chat;

public class LLMService : ILLMService
{
    private readonly ChatClient _client;

    public LLMService()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _client = new ChatClient("gpt-4", apiKey);
    }

    public async Task<ExecutionResult> ExecuteItemAsync(ChecklistItem item, InputData inputData)
    {
        var prompt = item.PromptTemplate
            .Replace("{InputText}", inputData.RawText)
            .Replace("{UserNote}", item.UserNote)
            .Replace("{Facts}", inputData.ExtractedFacts ?? "");

        var response = await _client.CompleteChatAsync(prompt);

        return new ExecutionResult
        {
            ItemId = item.Id,
            ItemTitle = item.Title,
            Content = response.Content[0].Text,
            CompletedAt = DateTime.Now,
            IsSuccess = true
        };
    }
}
```

## 使い方

1. **入力画面**
   - テキストボックスに文章、メモ、議事録、URLなどを入力
   - または「ファイルから読み込み」ボタンでテキストファイルを読み込み
   - 「事実抽出を有効にする」にチェックを入れると精度が向上（処理時間が長くなります）
   - 「次へ（AIに分析させる）」ボタンをクリック

2. **チェックリスト画面**
   - **カテゴリ別階層表示**: 全60種類以上の項目がカテゴリ別に整理されて表示される
   - **AI推奨項目**: AIが推奨した項目には「AI推奨」バッジと信頼度、推奨理由が表示される
   - **展開/折りたたみ**: 各カテゴリを展開・折りたたんで見やすく整理
   - **選択カウント**: 各カテゴリの右側に「選択数/全項目数」が表示される
   - **チェックボックス**: 各項目のチェックボックスでON/OFFを切り替え
   - **補足条件**: 各項目に補足条件を入力可能（オプション）
   - **プロンプト参照機能**: 各項目の「プロンプトを表示/非表示」ボタンで実際に使用されるプロンプトテンプレートを確認可能
   - **便利なボタン**:
     - 「推奨項目を選択」- AIが推奨した項目のみをチェック
     - 「すべて選択」- 全項目をチェック
     - 「すべて解除」- 全項目のチェックを外す
     - 「すべて展開」- 全カテゴリを展開
     - 「すべて折りたたむ」- 全カテゴリを折りたたむ
   - 「実行」ボタンでチェックした項目を実行

3. **実行・結果表示画面**
   - 選択された項目が順に実行される
   - 左側のリストから結果を選択して表示
   - 「選択した結果をコピー」でクリップボードにコピー
   - 「選択した結果をエクスポート」で個別にファイル保存
   - 「すべてエクスポート」で全結果をまとめて保存
   - **実行後自動ログ保存**: 実行が完了すると自動的にログが保存される

4. **ログ履歴画面**
   - メインウィンドウのヘッダーにある「実行ログ履歴を表示」ボタンからアクセス
   - **過去の実行履歴を参照**: 日時、入力テキスト、実行項目、結果を確認可能
   - **プロンプト参照**: 各項目で使用されたプロンプトテンプレートを確認・コピー可能
   - **エクスポート機能**: ログを個別にエクスポート可能
   - **ログ管理**: 個別削除または全削除が可能
   - **保存場所**: `マイドキュメント\PromptRunner\Logs\execution_logs.json`
   - **自動保持**: 最新100件まで自動保持

## 新機能

### プロンプト参照機能
各チェックリスト項目で「プロンプトを表示/非表示」ボタンをクリックすると、実際にLLMに送信されるプロンプトテンプレートを確認できます。`{InputText}`, `{UserNote}`, `{Facts}` などのプレースホルダーが実行時にどのように置換されるかを理解できます。

### ログ管理機能
実行した内容は自動的にログとして保存されます。ログ履歴画面では：
- 過去の実行を時系列で確認
- 入力テキスト、使用したプロンプト、生成結果を振り返り
- プロンプトをコピーして再利用
- ログをエクスポートして保管

これにより、過去の成功パターンを再現したり、プロンプトを改善したりすることが容易になります。

## ライセンス

このプロジェクトはオープンソースです。

## 貢献

プルリクエストを歓迎します。大きな変更の場合は、まずissueを開いて変更内容を議論してください。

## サポート

問題や質問がある場合は、GitHubのissueを作成してください。