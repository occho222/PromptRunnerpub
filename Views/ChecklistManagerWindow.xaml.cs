using System.Windows;
using PromptRunner.ViewModels;

namespace PromptRunner.Views
{
    public partial class ChecklistManagerWindow : Window
    {
        public ChecklistManagerWindow(ChecklistManagerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
