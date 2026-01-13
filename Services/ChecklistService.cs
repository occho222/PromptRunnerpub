using PromptRunner.Models;
using System.Text.Json;
using System.IO;

namespace PromptRunner.Services
{
    public class ChecklistService : IChecklistService
    {
        private List<ChecklistItem> _allItems;
        private readonly string _customChecklistPath;

        public ChecklistService()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PromptRunner");
            Directory.CreateDirectory(appDataPath);
            _customChecklistPath = Path.Combine(appDataPath, "custom-checklist.json");

            _allItems = LoadChecklistItems();
        }

        public List<ChecklistItem> GetAllChecklistItems()
        {
            return _allItems.Select(item => new ChecklistItem
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Category = item.Category,
                PromptTemplate = item.PromptTemplate,
                IsEnabled = false,
                Confidence = 0.0,
                Reason = string.Empty,
                Order = 0,
                UserNote = string.Empty
            }).ToList();
        }

        public List<ChecklistItem> GetItemsByCategory(ChecklistCategory category)
        {
            return _allItems.Where(item => item.Category == category).ToList();
        }

        public void AddItem(ChecklistItem item)
        {
            _allItems.Add(item);
            SaveChecklistItems();
        }

        public void UpdateItem(ChecklistItem item)
        {
            var index = _allItems.FindIndex(i => i.Id == item.Id);
            if (index >= 0)
            {
                _allItems[index] = item;
                SaveChecklistItems();
            }
        }

        public void DeleteItem(string id)
        {
            var item = _allItems.FirstOrDefault(i => i.Id == id);
            if (item != null)
            {
                _allItems.Remove(item);
                SaveChecklistItems();
            }
        }

        public void ImportItems(List<ChecklistItem> items)
        {
            _allItems = items;
            SaveChecklistItems();
        }

        public List<ChecklistItem> ExportItems()
        {
            return _allItems.ToList();
        }

        private List<ChecklistItem> LoadChecklistItems()
        {
            // カスタムチェックリストがあればそれを使用
            if (File.Exists(_customChecklistPath))
            {
                try
                {
                    var json = File.ReadAllText(_customChecklistPath);
                    var items = JsonSerializer.Deserialize<List<ChecklistItem>>(json);
                    if (items != null && items.Count > 0)
                    {
                        return items;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"カスタムチェックリストの読み込みエラー: {ex.Message}");
                }
            }

            // なければデフォルトを使用
            return InitializeChecklistItems();
        }

        private void SaveChecklistItems()
        {
            try
            {
                var json = JsonSerializer.Serialize(_allItems, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText(_customChecklistPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"チェックリストの保存エラー: {ex.Message}");
            }
        }

        public void ResetToDefault()
        {
            _allItems = InitializeChecklistItems();
            SaveChecklistItems();
        }

        private List<ChecklistItem> InitializeChecklistItems()
        {
            var items = new List<ChecklistItem>();

            // 要約系
            items.Add(new ChecklistItem("summary_full", "全体要約", "文章全体を簡潔に要約します",
                ChecklistCategory.Summary,
                "以下の文章を簡潔に要約してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("summary_keypoints", "要点抽出", "重要なポイントを箇条書きで抽出します",
                ChecklistCategory.Summary,
                "以下の文章から重要な要点を箇条書きで抽出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("summary_diagram", "図解化", "内容を図やダイアグラムで表現します",
                ChecklistCategory.Summary,
                "以下の文章の内容を図解化してください（テキストベースの図で表現）。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("summary_onepage", "1枚企画書", "A4一枚に収まる企画書形式にまとめます",
                ChecklistCategory.Summary,
                "以下の内容をA4一枚に収まる企画書形式にまとめてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("summary_faq", "FAQ化", "よくある質問と回答の形式にします",
                ChecklistCategory.Summary,
                "以下の内容をFAQ（よくある質問と回答）の形式にしてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("summary_exec", "エグゼクティブ要約", "結論・根拠・次アクションを最短でまとめます",
                ChecklistCategory.Summary,
                "以下の内容を、忙しい意思決定者向けに「結論→根拠→次アクション→懸念点」の順で200〜400字で要約してください。曖昧な点は曖昧と明記。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("summary_timeline", "時系列整理", "出来事を時系列に並べて整理します",
                ChecklistCategory.Summary,
                "以下の内容を時系列（日時が不明なら推定せず『不明』）で整理し、最後に「現状」「未解決」「次の一手」を3行でまとめてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("summary_actionitems", "要約＋ToDo抽出", "要点とアクションアイテムを同時に抽出します",
                ChecklistCategory.Summary,
                "以下から(1)要約(5行以内) (2)ToDo（担当/期限/優先度/依存関係）(3)保留事項 を抽出してください。情報がなければ空欄で。\n\n{InputText}\n\n{UserNote}"));

            // 分析系
            items.Add(new ChecklistItem("analysis_swot", "SWOT分析", "強み・弱み・機会・脅威を分析します",
                ChecklistCategory.Analysis,
                "以下の内容についてSWOT分析（強み・弱み・機会・脅威）を行ってください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_facts", "事実抽出", "事実と意見を分離して抽出します",
                ChecklistCategory.Analysis,
                "以下の文章から事実と意見を分離して抽出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_proscons", "メリデメ整理", "利点と欠点を整理します",
                ChecklistCategory.Analysis,
                "以下の内容についてメリット・デメリット・リスクを整理してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_counterargument", "反論視点", "批判的視点から分析します",
                ChecklistCategory.Analysis,
                "以下の内容について、批判的視点（悪魔の代弁者）から反論・批判を提示してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_alternatives", "代替案提案", "別の選択肢を提案します",
                ChecklistCategory.Analysis,
                "以下の内容に対して、代替案（第3の選択肢）を提案してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_criteria", "評価軸設計", "何を基準に比べるべきか設計します",
                ChecklistCategory.Analysis,
                "以下の内容について、評価軸（何を基準に比べるべきか）を設計してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_scoring", "スコアリング表", "重み付き採点表を作成します",
                ChecklistCategory.Analysis,
                "以下の内容についてスコアリング表（重み付き採点）を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_impact", "期待効果と副作用", "短期・長期の影響を分析します",
                ChecklistCategory.Analysis,
                "以下の内容について、期待効果と副作用（短期／長期）を分析してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_risk", "リスク分析", "想定事故と予防策を分析します",
                ChecklistCategory.Analysis,
                "以下の内容についてリスク分析（想定事故、起きたときの影響、予防策）を行ってください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_info_needed", "追加情報の洗い出し", "意思決定に必要な追加情報を洗い出します",
                ChecklistCategory.Analysis,
                "以下の内容について、意思決定に必要な追加情報を洗い出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_structure", "主張根拠具体例手段の構造化", "論理構造を明確に整理します",
                ChecklistCategory.Analysis,
                "以下の内容を「主張→根拠→具体例→手段」の構造で整理してください。\n\n" +
                "【主張】何を言いたいのか\n" +
                "【根拠】なぜそう言えるのか（データ、理論、経験など）\n" +
                "【具体例】具体的にはどういうことか（事例、数字、比較など）\n" +
                "【手段】どうすれば実現できるのか（方法、ステップ、リソースなど）\n\n" +
                "{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_5whys", "5 Whys（なぜなぜ分析）", "問題の根本原因を掘り下げます",
                ChecklistCategory.Analysis,
                "以下の問題について5 Whysを作成し、(1)暫定根本原因 (2)確認すべき事実 (3)再発防止策（短期/中期）を出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_rootcause_fishbone", "特性要因図（魚骨図）", "原因候補を網羅的に分類します",
                ChecklistCategory.Analysis,
                "以下の問題を特性要因図（人/方法/機械/材料/環境/測定）で分類し、各カテゴリに原因候補を箇条書きしてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_bias_check", "バイアス検出", "思い込みや論理の飛躍を指摘します",
                ChecklistCategory.Analysis,
                "以下の内容に含まれる認知バイアスや論理の飛躍（例：相関と因果の混同、二分法、チェリーピッキング等）を指摘し、より検証可能な言い換え案を提示してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_premortem", "プレモーテム", "失敗した前提で原因と対策を先に出します",
                ChecklistCategory.Analysis,
                "この計画は6ヶ月後に失敗したと仮定します。失敗理由を10個挙げ、各理由に対して「予兆」「予防策」「発生時のリカバリ」をセットで出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("analysis_stakeholder", "ステークホルダー分析", "利害関係者と懸念・説得材料を整理します",
                ChecklistCategory.Analysis,
                "以下のテーマのステークホルダーを洗い出し、各ステークホルダーの関心/懸念/反対理由/刺さるメリット/必要な根拠を整理してください。\n\n{InputText}\n\n{UserNote}"));

            // アイデア出し系
            items.Add(new ChecklistItem("ideation_brainstorm", "ブレスト", "アイデアを大量生成します",
                ChecklistCategory.Ideation,
                "以下のテーマについてブレインストーミング（アイデアを大量生成）してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_perspectives", "切り口出し", "様々な視点からアイデアを出します",
                ChecklistCategory.Ideation,
                "以下のテーマについて、様々な切り口（ターゲット別・用途別・状況別）からアイデアを出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_analogy", "類推・アナロジー", "別業界のやり方を移植します",
                ChecklistCategory.Ideation,
                "以下のテーマについて、類推・アナロジー（別業界のやり方を移植）でアイデアを出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_constrained", "制約付きアイデア", "制約条件下でのアイデアを出します",
                ChecklistCategory.Ideation,
                "以下のテーマについて、制約付きアイデア（予算1万円、30分以内、ツール縛りなど）を出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_reverse", "逆張り案", "真逆の方向からアイデアを出します",
                ChecklistCategory.Ideation,
                "以下のテーマについて、逆張り案（わざと真逆の方向）を出してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_not_todo", "やらないことリスト", "あえてやらないことを提案します",
                ChecklistCategory.Ideation,
                "以下のテーマについて、「やらないことリスト」を提案してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_naming", "ネーミング案", "名前やキャッチコピーを提案します",
                ChecklistCategory.Ideation,
                "以下の内容について、ネーミング案／キャッチコピー案を複数提案してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_titles", "タイトル案", "SNS、ブログ、動画のタイトルを提案します",
                ChecklistCategory.Ideation,
                "以下の内容について、タイトル案（SNS、ブログ、動画）を複数提案してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_project_outline", "企画の骨子", "狙い・構成・オチを設計します",
                ChecklistCategory.Ideation,
                "以下の内容について、企画の骨子（狙い→構成→オチ）を設計してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_concrete", "具体化", "次の一歩に落とし込みます",
                ChecklistCategory.Ideation,
                "以下の内容を具体化（次の一歩に落とし込み）してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_scamper", "SCAMPER発想", "代替・拡張・縮小などの切り口で発想します",
                ChecklistCategory.Ideation,
                "以下のテーマをSCAMPER（代用/結合/応用/修正/転用/削除/逆転）で各3案ずつ出してください。実行しやすい順に。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_crazy8", "Crazy 8s", "短時間で8案を強制生成します",
                ChecklistCategory.Ideation,
                "以下のテーマについて、方向性が被らない8案を一気に出してください（現実性は一旦無視）。その後、現実的にする改造案を各1つ付けてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("ideation_experiments", "検証実験アイデア", "小さく検証する実験案を作ります",
                ChecklistCategory.Ideation,
                "以下の仮説を検証するための実験案を5つ。各案に「最小実装」「成功指標」「期間」「想定リスク」を付けてください。\n\n{InputText}\n\n{UserNote}"));

            // 文章化系
            items.Add(new ChecklistItem("writing_full", "文章化", "箇条書きを文章に展開します",
                ChecklistCategory.Writing,
                "以下の箇条書きを文章に展開してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_structure", "構成案", "序論・本論・結論に整理します",
                ChecklistCategory.Writing,
                "以下の内容を序論・本論・結論の構成に整理してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_refine", "文章構成", "文章を整えて読みやすくします",
                ChecklistCategory.Writing,
                "以下の文章を整えて読みやすくしてください。文法、表現、構成を改善してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_report", "レポート化", "正式なレポート形式にします",
                ChecklistCategory.Writing,
                "以下の内容を正式なレポート形式（目的・方法・結果・考察）にまとめてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_blog", "ブログ記事化", "読みやすいブログ記事にします",
                ChecklistCategory.Writing,
                "以下の内容を読みやすいブログ記事（見出し・本文・まとめ）にしてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_storytelling", "ストーリー仕立て", "物語形式で伝えます",
                ChecklistCategory.Writing,
                "以下の内容をストーリー仕立て（起承転結）で表現してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_email", "メール文案", "ビジネスメールを作成します",
                ChecklistCategory.Writing,
                "以下の内容をビジネスメール（件名・本文・署名）として作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_presentation", "プレゼン原稿", "発表用の原稿を作成します",
                ChecklistCategory.Writing,
                "以下の内容をプレゼン原稿（導入・本編・まとめ）として作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_speech", "スピーチ原稿", "スピーチ用の原稿を作成します",
                ChecklistCategory.Writing,
                "以下の内容をスピーチ原稿（冒頭・本編・結び）として作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("writing_minutes", "議事録化", "会話/メモを議事録形式にします",
                ChecklistCategory.Writing,
                "以下を議事録にしてください：参加者/目的/決定事項/論点/宿題（担当・期限）/保留/次回アジェンダ。未記載は『不明』。\n\n{InputText}\n\n{UserNote}"));

            // 翻訳系
            items.Add(new ChecklistItem("translation_formal", "専門文書の翻訳", "用語統一込みで翻訳します",
                ChecklistCategory.Translation,
                "以下の文章を翻訳してください（用語統一込み）。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("translation_colloquial", "口語化", "自然な言い回しに変換します",
                ChecklistCategory.Translation,
                "以下の文章を口語化（自然な言い回し）してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("translation_localization", "ローカライゼーション", "日本向け・海外向けに調整します",
                ChecklistCategory.Translation,
                "以下の文章をローカライゼーション（日本向け／海外向けに調整）してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("translation_bilingual", "英日併記", "英語と日本語を併記します",
                ChecklistCategory.Translation,
                "以下の内容を英日併記（同じ内容を英語と日本語で並べて表示）してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("translation_word_meaning", "文言の意味を調べる", "言葉や表現の意味を詳しく説明します",
                ChecklistCategory.Translation,
                "以下の文言・表現の意味を詳しく説明してください。語源、使い方、例文も含めてください。\n\n{InputText}\n\n{UserNote}"));

            // 学習系
            items.Add(new ChecklistItem("learning_quiz", "クイズ化", "選択式の問題を作成します",
                ChecklistCategory.Learning,
                "以下の内容からクイズ（選択式／穴埋め／一問一答）を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_flashcards", "暗記カード", "重要用語の暗記カードを作成します",
                ChecklistCategory.Learning,
                "以下の内容から重要用語の暗記カードを作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_exercises", "練習問題", "難易度別の練習問題を作成します",
                ChecklistCategory.Learning,
                "以下の内容から練習問題（難易度別）を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_common_mistakes", "誤答指摘", "間違えやすいポイントを指摘します",
                ChecklistCategory.Learning,
                "以下の内容について、誤答しやすい点を指摘してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_stepbystep", "ステップ学習", "段階的な学習計画を作成します",
                ChecklistCategory.Learning,
                "以下の内容をステップ学習（今日やる→明日やる）の形式にしてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_progressive", "例題→類題→応用", "段階的な問題セットを作成します",
                ChecklistCategory.Learning,
                "以下の内容から、例題→類題→応用の順に問題を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_interview", "口頭試問", "面接練習形式の質問を作成します",
                ChecklistCategory.Learning,
                "以下の内容について、口頭試問（面接練習）形式の質問を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_explain_kids", "子どもにもわかる説明", "比喩と例で噛み砕きます",
                ChecklistCategory.Learning,
                "以下の内容を、小学生にもわかるように説明してください。たとえ話を2つ、ミニクイズを3問付けてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("learning_cheatsheet", "チートシート", "要点を一枚に整理します",
                ChecklistCategory.Learning,
                "以下の内容をチートシート（見出し→要点3つ→よくあるミス→コツ）で1枚分にまとめてください。\n\n{InputText}\n\n{UserNote}"));

            // 企画・設計系
            items.Add(new ChecklistItem("planning_framework", "目的→課題→打ち手→KPI", "フレームワークに分解します",
                ChecklistCategory.Planning,
                "以下の内容を目的→課題→打ち手→KPIに分解してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_logic_tree", "ロジックツリー", "論理的に分解します",
                ChecklistCategory.Planning,
                "以下の内容をロジックツリーとして分解してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_prioritization", "優先度付け", "Impact×Effortで優先順位をつけます",
                ChecklistCategory.Planning,
                "以下の内容について、優先度付け（Impact×Effort）を行ってください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_requirements", "要件定義", "要件定義テンプレートに落とし込みます",
                ChecklistCategory.Planning,
                "以下の内容を要件定義のテンプレートに落とし込んでください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_ambiguity", "仕様の曖昧さ検出", "質問リストを作成します",
                ChecklistCategory.Planning,
                "以下の仕様について、曖昧さを検出し質問リストを作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_decisions", "決めるべき事項", "決定が必要な項目をチェックリスト化します",
                ChecklistCategory.Planning,
                "以下の内容について、「決めるべき事項」をチェックリストにしてください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_wbs", "タスク分解", "作業を分解します",
                ChecklistCategory.Planning,
                "以下の内容をタスク分解（WBS）してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_schedule", "スケジュール案", "週次計画を作成します",
                ChecklistCategory.Planning,
                "以下の内容からスケジュール案（週次計画）を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("planning_timebox", "タイムボックス化", "タスクをタイムボックスで分解し、各ブロックのゴールを設定します",
                ChecklistCategory.Planning,
                "以下をタイムボックス（25分×nなど）で進める手順に分解し、各ブロックのゴールと終了条件を定義してください。\n\n{InputText}\n\n{UserNote}"));

            // 実行支援系
            items.Add(new ChecklistItem("execution_first30min", "最初の30分", "最初にやることを提案します",
                ChecklistCategory.Execution,
                "以下のタスクについて、最初の30分でやることを提案してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("execution_steps", "具体的な手順", "ステップバイステップの手順を作成します",
                ChecklistCategory.Execution,
                "以下のタスクについて、具体的な手順（チェックリスト）を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("execution_script", "台本（セリフ付き）", "実際の台詞を含む台本を作成します",
                ChecklistCategory.Execution,
                "以下の場面について、台本（実際の台詞付き）を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("execution_qanda", "想定質問と回答", "Q&Aを作成します",
                ChecklistCategory.Execution,
                "以下の内容について、想定質問と回答（Q&A）を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("execution_objection", "反対意見への返し方", "反論への対応を準備します",
                ChecklistCategory.Execution,
                "以下の内容について、反対意見への返し方を準備してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("execution_meeting_agenda", "会議アジェンダ案", "会議の進行案を作成します",
                ChecklistCategory.Execution,
                "以下の内容について、会議アジェンダ案を作成してください。\n\n{InputText}\n\n{UserNote}"));

            items.Add(new ChecklistItem("execution_consensus", "合意形成の落とし所", "妥協点を提案します",
                ChecklistCategory.Execution,
                "以下の状況について、合意形成の落とし所（妥協点）を提案してください。\n\n{InputText}\n\n{UserNote}"));

            return items;
        }
    }
}
