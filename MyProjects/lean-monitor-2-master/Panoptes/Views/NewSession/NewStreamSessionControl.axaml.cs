using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Panoptes.Views.NewSession
{
    public partial class NewStreamSessionControl : UserControl
    {
        public NewStreamSessionControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
