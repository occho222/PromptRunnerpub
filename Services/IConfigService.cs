using PromptRunner.Models;

namespace PromptRunner.Services
{
    /// <summary>
    /// 設定サービスのインターフェース
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 設定を読み込む
        /// </summary>
        AppSettings LoadSettings();

        /// <summary>
        /// 設定を保存する
        /// </summary>
        void SaveSettings(AppSettings settings);

        /// <summary>
        /// 現在の設定を取得する
        /// </summary>
        AppSettings CurrentSettings { get; }
    }
}
