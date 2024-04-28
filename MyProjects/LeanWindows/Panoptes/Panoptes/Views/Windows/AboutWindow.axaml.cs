using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.Windows
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
