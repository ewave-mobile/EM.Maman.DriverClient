using System.Windows;
using System.Windows.Controls;

namespace EM.Maman.DriverClient.Controls
{
    public partial class ExportStickerControl : UserControl
    {
        public ExportStickerControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty AwbNumberProperty =
            DependencyProperty.Register("AwbNumber", typeof(string), typeof(ExportStickerControl), new PropertyMetadata(string.Empty));
        public string AwbNumber
        {
            get { return (string)GetValue(AwbNumberProperty); }
            set { SetValue(AwbNumberProperty, value); }
        }

        public static readonly DependencyProperty SwbPrefixProperty =
            DependencyProperty.Register("SwbPrefix", typeof(string), typeof(ExportStickerControl), new PropertyMetadata(string.Empty));
        public string SwbPrefix
        {
            get { return (string)GetValue(SwbPrefixProperty); }
            set { SetValue(SwbPrefixProperty, value); }
        }

        public static readonly DependencyProperty G080TextProperty =
            DependencyProperty.Register("G080Text", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("G080"));
        public string G080Text
        {
            get { return (string)GetValue(G080TextProperty); }
            set { SetValue(G080TextProperty, value); }
        }

        public static readonly DependencyProperty E1TextProperty =
            DependencyProperty.Register("E1Text", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("E1"));
        public string E1Text
        {
            get { return (string)GetValue(E1TextProperty); }
            set { SetValue(E1TextProperty, value); }
        }
        
        // AYYBText and AyybCode are not used in the current XAML for ExportSticker, 
        // but kept here if future design requires them.
        // public static readonly DependencyProperty AyybTextProperty =
        //    DependencyProperty.Register("AyybText", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("AYYB"));
        // public string AyybText
        // {
        //    get { return (string)GetValue(AyybTextProperty); }
        //    set { SetValue(AyybTextProperty, value); }
        // }

        // public static readonly DependencyProperty AyybCodeProperty =
        //    DependencyProperty.Register("AyybCode", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("488-20055803"));
        // public string AyybCode
        // {
        //    get { return (string)GetValue(AyybCodeProperty); }
        //    set { SetValue(AyybCodeProperty, value); }
        // }

        // HAYVBText and HayvbCode are not used in the current XAML for ExportSticker
        // public static readonly DependencyProperty HayvbTextProperty =
        //    DependencyProperty.Register("HayvbText", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("HAYVB"));
        // public string HayvbText
        // {
        //    get { return (string)GetValue(HayvbTextProperty); }
        //    set { SetValue(HayvbTextProperty, value); }
        // }

        // public static readonly DependencyProperty HayvbCodeProperty =
        //    DependencyProperty.Register("HayvbCode", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("488-200"));
        // public string HayvbCode
        // {
        //    get { return (string)GetValue(HayvbCodeProperty); }
        //    set { SetValue(HayvbCodeProperty, value); }
        // }

        public static readonly DependencyProperty LargeNumericDisplayProperty =
            DependencyProperty.Register("LargeNumericDisplay", typeof(string), typeof(ExportStickerControl), new PropertyMetadata(string.Empty));
        public string LargeNumericDisplay
        {
            get { return (string)GetValue(LargeNumericDisplayProperty); }
            set { SetValue(LargeNumericDisplayProperty, value); }
        }

        public static readonly DependencyProperty DestinationProperty =
            DependencyProperty.Register("Destination", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("TAS"));
        public string Destination
        {
            get { return (string)GetValue(DestinationProperty); }
            set { SetValue(DestinationProperty, value); }
        }

        public static readonly DependencyProperty AppearanceValueProperty =
            DependencyProperty.Register("AppearanceValue", typeof(string), typeof(ExportStickerControl), new PropertyMetadata(string.Empty));
        public string AppearanceValue
        {
            get { return (string)GetValue(AppearanceValueProperty); }
            set { SetValue(AppearanceValueProperty, value); }
        }

        public static readonly DependencyProperty StorageValueProperty =
            DependencyProperty.Register("StorageValue", typeof(string), typeof(ExportStickerControl), new PropertyMetadata(string.Empty));
        public string StorageValue
        {
            get { return (string)GetValue(StorageValueProperty); }
            set { SetValue(StorageValueProperty, value); }
        }

        public static readonly DependencyProperty AirlineTextProperty =
            DependencyProperty.Register("AirlineText", typeof(string), typeof(ExportStickerControl), new PropertyMetadata("C6"));
        public string AirlineText
        {
            get { return (string)GetValue(AirlineTextProperty); }
            set { SetValue(AirlineTextProperty, value); }
        }

        public static readonly DependencyProperty PiecesCountProperty =
            DependencyProperty.Register("PiecesCount", typeof(string), typeof(ExportStickerControl), new PropertyMetadata(string.Empty));
        public string PiecesCount
        {
            get { return (string)GetValue(PiecesCountProperty); }
            set { SetValue(PiecesCountProperty, value); }
        }

        public static readonly DependencyProperty TotalPiecesProperty =
            DependencyProperty.Register("TotalPieces", typeof(string), typeof(ExportStickerControl), new PropertyMetadata(string.Empty));
        public string TotalPieces
        {
            get { return (string)GetValue(TotalPiecesProperty); }
            set { SetValue(TotalPiecesProperty, value); }
        }
    }
}
