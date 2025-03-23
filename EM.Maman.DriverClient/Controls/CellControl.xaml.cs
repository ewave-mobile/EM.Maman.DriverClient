using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EM.Maman.DriverClient.Controls
{
    /// <summary>
    /// Interaction logic for CellControl.xaml
    /// </summary>

    public partial class CellControl : UserControl
    {
        public static readonly DependencyProperty CellInfoProperty =
            DependencyProperty.Register("CellInfo", typeof(Cell), typeof(CellControl),
                new PropertyMetadata(null, OnCellInfoChanged));

        public static readonly DependencyProperty PalletInfoProperty =
            DependencyProperty.Register("PalletInfo", typeof(Pallet), typeof(CellControl),
                new PropertyMetadata(null, OnPalletInfoChanged));

        public static readonly DependencyProperty HasPalletProperty =
            DependencyProperty.Register("HasPallet", typeof(bool), typeof(CellControl),
                new PropertyMetadata(false));

        public Cell CellInfo
        {
            get { return (Cell)GetValue(CellInfoProperty); }
            set { SetValue(CellInfoProperty, value); }
        }

        public Pallet PalletInfo
        {
            get { return (Pallet)GetValue(PalletInfoProperty); }
            set { SetValue(PalletInfoProperty, value); }
        }

        public bool HasPallet
        {
            get { return (bool)GetValue(HasPalletProperty); }
            set { SetValue(HasPalletProperty, value); }
        }

        public CellControl()
        {
            InitializeComponent();
        }

        private static void OnCellInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CellControl;
            if (control != null)
            {
                control.UpdateHasPallet();
            }
        }

        private static void OnPalletInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as CellControl;
            if (control != null)
            {
                control.UpdateHasPallet();
            }
        }

        private void UpdateHasPallet()
        {
            HasPallet = PalletInfo != null;
        }
    }
}
