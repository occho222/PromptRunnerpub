using System.IO;
using System.Text.Json;
using PromptRunner.Models;

namespace PromptRunner.Services
{
    /// <summary>
    /// 設定サービスの実装
    /// </summary>
    public class ConfigService : IConfigService
    {
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;

        public ConfigService()
        {
            var appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PromptRunner");

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }

            _settingsFilePath = Path.Combine(appDataFolder, "user-settings.json");
            _currentSettings = LoadSettings();
        }

        public AppSettings CurrentSettings => _currentSettings;

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        _currentSettings = settings;
                        System.Diagnostics.Debug.WriteLine($"設定を読み込みました: {_settingsFilePath}");
                        System.Diagnostics.Debug.WriteLine($"APIキー: {(string.IsNullOrEmpty(settings.ApiKey) ? "未設定" : "設定済み")}");
                        System.Diagnostics.Debug.WriteLine($"モデル名: {settings.ModelName}");
                        return settings;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"設定ファイルが存在しません: {_settingsFilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"設定の読み込みエラー: {ex.Message}");
            }

            // デフォルト設定を返す
            System.Diagnostics.Debug.WriteLine("デフォルト設定を使用します");
            _currentSettings = new AppSettings();
            return _currentSettings;
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsFilePath, json);
                _currentSettings = settings;

                System.Diagnostics.Debug.WriteLine($"設定を保存しました: {_settingsFilePath}");
                System.Diagnostics.Debug.WriteLine($"APIキー: {(string.IsNullOrEmpty(settings.ApiKey) ? "未設定" : "設定済み")}");
                System.Diagnostics.Debug.WriteLine($"モデル名: {settings.ModelName}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"設定の保存に失敗しました: {ex.Message}", ex);
            }
        }
    }
}
