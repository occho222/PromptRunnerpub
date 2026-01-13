using Markdig;
using System.Text;

namespace PromptRunner.Services
{
    /// <summary>
    /// マークダウンをHTMLに変換するサービス
    /// </summary>
    public class MarkdownService
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownService()
        {
            // Markdigのパイプラインを設定（拡張機能を有効化）
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions() // テーブル、タスクリスト、絵文字など
                .Build();
        }

        /// <summary>
        /// マークダウンテキストをHTMLに変換
        /// </summary>
        public string ConvertToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return string.Empty;

            var htmlBody = Markdown.ToHtml(markdown, _pipeline);
            return CreateFullHtmlDocument(htmlBody);
        }

        /// <summary>
        /// 完全なHTMLドキュメントを生成（スタイル付き）
        /// </summary>
        private string CreateFullHtmlDocument(string bodyHtml)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='utf-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1'>");
            sb.AppendLine("    <style>");
            sb.AppendLine(GetCssStyles());
            sb.AppendLine("    </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine(bodyHtml);
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }

        /// <summary>
        /// GitHub風のCSSスタイルを取得
        /// </summary>
        private string GetCssStyles()
        {
            return @"
                body {
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif, 'Apple Color Emoji', 'Segoe UI Emoji';
                    font-size: 16px;
                    line-height: 1.6;
                    color: #24292f;
                    background-color: #ffffff;
                    padding: 20px;
                    max-width: 100%;
                    margin: 0 auto;
                }

                h1, h2, h3, h4, h5, h6 {
                    margin-top: 24px;
                    margin-bottom: 16px;
                    font-weight: 600;
                    line-height: 1.25;
                    border-bottom: 1px solid #d0d7de;
                    padding-bottom: 8px;
                }

                h1 { font-size: 2em; }
                h2 { font-size: 1.5em; }
                h3 { font-size: 1.25em; }
                h4 { font-size: 1em; }
                h5 { font-size: 0.875em; }
                h6 { font-size: 0.85em; color: #57606a; }

                p {
                    margin-top: 0;
                    margin-bottom: 16px;
                }

                a {
                    color: #0969da;
                    text-decoration: none;
                }

                a:hover {
                    text-decoration: underline;
                }

                ul, ol {
                    padding-left: 2em;
                    margin-top: 0;
                    margin-bottom: 16px;
                }

                li {
                    margin-top: 0.25em;
                }

                blockquote {
                    margin: 0;
                    padding: 0 1em;
                    color: #57606a;
                    border-left: 0.25em solid #d0d7de;
                }

                code {
                    padding: 0.2em 0.4em;
                    margin: 0;
                    font-size: 85%;
                    background-color: rgba(175, 184, 193, 0.2);
                    border-radius: 6px;
                    font-family: 'Courier New', monospace;
                }

                pre {
                    padding: 16px;
                    overflow: auto;
                    font-size: 85%;
                    line-height: 1.45;
                    background-color: #f6f8fa;
                    border-radius: 6px;
                    margin-top: 0;
                    margin-bottom: 16px;
                }

                pre code {
                    display: block;
                    padding: 0;
                    margin: 0;
                    overflow: visible;
                    line-height: inherit;
                    background-color: transparent;
                    border: 0;
                }

                table {
                    border-spacing: 0;
                    border-collapse: collapse;
                    margin-top: 0;
                    margin-bottom: 16px;
                    width: 100%;
                }

                table th {
                    font-weight: 600;
                    padding: 6px 13px;
                    border: 1px solid #d0d7de;
                    background-color: #f6f8fa;
                }

                table td {
                    padding: 6px 13px;
                    border: 1px solid #d0d7de;
                }

                table tr {
                    background-color: #ffffff;
                    border-top: 1px solid #d0d7de;
                }

                table tr:nth-child(2n) {
                    background-color: #f6f8fa;
                }

                hr {
                    height: 0.25em;
                    padding: 0;
                    margin: 24px 0;
                    background-color: #d0d7de;
                    border: 0;
                }

                img {
                    max-width: 100%;
                    box-sizing: content-box;
                }

                /* タスクリスト */
                input[type='checkbox'] {
                    margin-right: 0.5em;
                }

                /* 強調 */
                strong {
                    font-weight: 600;
                }

                em {
                    font-style: italic;
                }

                /* 削除線 */
                del {
                    text-decoration: line-through;
                }
            ";
        }
    }
}
