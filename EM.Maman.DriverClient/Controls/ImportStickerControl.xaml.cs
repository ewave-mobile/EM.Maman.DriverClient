using System.Windows;
using System.Windows.Controls;

namespace EM.Maman.DriverClient.Controls
{
    public partial class ImportStickerControl : UserControl
    {
        public ImportStickerControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ManifestProperty =
            DependencyProperty.Register("Manifest", typeof(string), typeof(ImportStickerControl), new PropertyMetadata(string.Empty));

        public string Manifest
        {
            get { return (string)GetValue(ManifestProperty); }
            set { SetValue(ManifestProperty, value); }
        }

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register("Unit", typeof(string), typeof(ImportStickerControl), new PropertyMetadata(string.Empty));

        public string Unit
        {
            get { return (string)GetValue(UnitProperty); }
            set { SetValue(UnitProperty, value); }
        }

        public static readonly DependencyProperty AppearanceProperty =
            DependencyProperty.Register("Appearance", typeof(string), typeof(ImportStickerControl), new PropertyMetadata(string.Empty));

        public string Appearance
        {
            get { return (string)GetValue(AppearanceProperty); }
            set { SetValue(AppearanceProperty, value); }
        }
    }
}
