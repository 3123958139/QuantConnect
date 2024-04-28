using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.Windows
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
