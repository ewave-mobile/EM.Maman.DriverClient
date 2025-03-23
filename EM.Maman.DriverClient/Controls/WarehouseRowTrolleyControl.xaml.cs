using EM.Maman.Models.DisplayModels;
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
    /// Interaction logic for WarehouseRowTrolleyControl.xaml
    /// </summary>
    public partial class WarehouseRowTrolleyControl : UserControl
    {
        public static readonly DependencyProperty RowDataProperty =
            DependencyProperty.Register("RowData", typeof(CompositeRow), typeof(WarehouseRowTrolleyControl), new PropertyMetadata(null));

        public static readonly DependencyProperty HighestActiveRowProperty =
            DependencyProperty.Register("HighestActiveRow", typeof(int), typeof(WarehouseRowTrolleyControl), new PropertyMetadata(0));

        public static readonly DependencyProperty CurrentTrolleyPositionProperty =
            DependencyProperty.Register("CurrentTrolleyPosition", typeof(int), typeof(WarehouseRowTrolleyControl), new PropertyMetadata(0));

        public static readonly DependencyProperty CurrentTrolleyNameProperty =
            DependencyProperty.Register("CurrentTrolleyName", typeof(string), typeof(WarehouseRowTrolleyControl), new PropertyMetadata(string.Empty));

        public CompositeRow RowData
        {
            get { return (CompositeRow)GetValue(RowDataProperty); }
            set { SetValue(RowDataProperty, value); }
        }

        public int HighestActiveRow
        {
            get { return (int)GetValue(HighestActiveRowProperty); }
            set { SetValue(HighestActiveRowProperty, value); }
        }

        public int CurrentTrolleyPosition
        {
            get { return (int)GetValue(CurrentTrolleyPositionProperty); }
            set { SetValue(CurrentTrolleyPositionProperty, value); }
        }

        public string CurrentTrolleyName
        {
            get { return (string)GetValue(CurrentTrolleyNameProperty); }
            set { SetValue(CurrentTrolleyNameProperty, value); }
        }

        public WarehouseRowTrolleyControl()
        {
            InitializeComponent();
        }
    }
}
