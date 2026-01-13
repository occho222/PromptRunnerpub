using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using PromptRunner.Models;
using PromptRunner.Services;

namespace PromptRunner.ViewModels
{
    /// <summary>
    /// 設定画面のViewModel
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IConfigService _configService;
        private string _apiKey = string.Empty;
        private string _modelName = "gemini-2.5-flash";
        private int _maxOutputTokens = 8192;
        private double _temperature = 0.7;
        private bool _isApiKeyVisible = false;

        public SettingsViewModel(IConfigService configService)
        {
            _configService = configService;

            // 現在の設定を読み込み
            var currentSettings = _configService.CurrentSettings;
            _apiKey = currentSettings.ApiKey;
            _modelName = currentSettings.ModelName;
            _maxOutputTokens = currentSettings.MaxOutputTokens;
            _temperature = currentSettings.Temperature;

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ToggleApiKeyVisibilityCommand = new RelayCommand(ToggleApiKeyVisibility);
        }

        public List<string> AvailableModels { get; } = new List<string>
        {
            "gemini-2.5-flash",
            "gemini-2.5-pro",
            "gemini-1.5-flash",
            "gemini-1.5-pro",
            "gemma-3-27b-it"
        };

        public string ApiKey
        {
            get => _apiKey;
            set => SetProperty(ref _apiKey, value);
        }

        public string ModelName
        {
            get => _modelName;
            set => SetProperty(ref _modelName, value);
        }

        public int MaxOutputTokens
        {
            get => _maxOutputTokens;
            set => SetProperty(ref _maxOutputTokens, value);
        }

        public double Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, value);
        }

        public bool IsApiKeyVisible
        {
            get => _isApiKeyVisible;
            set => SetProperty(ref _isApiKeyVisible, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ToggleApiKeyVisibilityCommand { get; }

        public event EventHandler? SettingsSaved;
        public event EventHandler? Cancelled;

        private void Save()
        {
            try
            {
                var settings = new AppSettings
                {
                    ApiKey = ApiKey,
                    ModelName = ModelName,
                    MaxOutputTokens = MaxOutputTokens,
                    Temperature = Temperature
                };

                _configService.SaveSettings(settings);
                MessageBox.Show("設定を保存しました。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                SettingsSaved?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"設定の保存に失敗しました:\n{ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private void ToggleApiKeyVisibility()
        {
            IsApiKeyVisible = !IsApiKeyVisible;
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

    /// <summary>
    /// シンプルなRelayCommandの実装
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}
