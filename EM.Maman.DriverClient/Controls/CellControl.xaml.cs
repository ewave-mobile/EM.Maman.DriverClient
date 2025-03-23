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
            DependencyProperty.Register("CellInfo", typeof(Cell), typeof(CellControl), new PropertyMetadata(null));

        public Cell CellInfo
        {
            get { return (Cell)GetValue(CellInfoProperty); }
            set { SetValue(CellInfoProperty, value); }
        }

        public CellControl()
        {
            InitializeComponent();
        }
    }
}
