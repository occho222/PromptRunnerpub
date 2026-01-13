using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PromptRunner.Models
{
    /// <summary>
    /// アプリケーション設定クラス
    /// </summary>
    public class AppSettings : INotifyPropertyChanged
    {
        private string _apiKey = string.Empty;
        private string _modelName = "gemini-2.5-flash";
        private int _maxOutputTokens = 8192;
        private double _temperature = 0.7;

        /// <summary>
        /// Google AI API キー
        /// </summary>
        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        /// <summary>
        /// 使用するモデル名
        /// </summary>
        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        /// <summary>
        /// 最大出力トークン数
        /// </summary>
        public int MaxOutputTokens
        {
            get => _maxOutputTokens;
            set => SetProperty(ref _maxOutputTokens, value);
        }

        /// <summary>
        /// Temperature（0.0〜1.0）
        /// </summary>
        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
