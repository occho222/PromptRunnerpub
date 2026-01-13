using PromptRunner.Models;
using PromptRunner.ViewModels;
using System.Globalization;
using System.Windows.Data;

namespace PromptRunner.Converters
{
    public class PromptPreviewConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return string.Empty;

            var viewModel = values[0] as ChecklistViewModel;
            var item = values[1] as ChecklistItem;

            if (viewModel == null || item == null) return string.Empty;

            return viewModel.GetPromptPreview(item);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
