using EM.Maman.Models.DisplayModels;
using System.Windows;
using System.Windows.Controls;

namespace EM.Maman.DriverClient.Controls
{
    public partial class TrolleyWithCellsControl : UserControl
    {
        public static readonly DependencyProperty RowPositionProperty =
            DependencyProperty.Register("RowPosition", typeof(int), typeof(TrolleyWithCellsControl), new PropertyMetadata(0));

        public static readonly DependencyProperty TrolleyPositionProperty =
            DependencyProperty.Register("TrolleyPosition", typeof(int), typeof(TrolleyWithCellsControl), new PropertyMetadata(0));

        public static readonly DependencyProperty TrolleyNameProperty =
            DependencyProperty.Register("TrolleyName", typeof(string), typeof(TrolleyWithCellsControl), new PropertyMetadata("Trolley"));

        public static readonly DependencyProperty LeftCellProperty =
            DependencyProperty.Register("LeftCell", typeof(TrolleyCell), typeof(TrolleyWithCellsControl), new PropertyMetadata(null));

        public static readonly DependencyProperty RightCellProperty =
            DependencyProperty.Register("RightCell", typeof(TrolleyCell), typeof(TrolleyWithCellsControl), new PropertyMetadata(null));

        public int RowPosition
        {
            get { return (int)GetValue(RowPositionProperty); }
            set { SetValue(RowPositionProperty, value); }
        }

        public int TrolleyPosition
        {
            get { return (int)GetValue(TrolleyPositionProperty); }
            set { SetValue(TrolleyPositionProperty, value); }
        }

        public string TrolleyName
        {
            get { return (string)GetValue(TrolleyNameProperty); }
            set { SetValue(TrolleyNameProperty, value); }
        }

        public TrolleyCell LeftCell
        {
            get { return (TrolleyCell)GetValue(LeftCellProperty); }
            set { SetValue(LeftCellProperty, value); }
        }

        public TrolleyCell RightCell
        {
            get { return (TrolleyCell)GetValue(RightCellProperty); }
            set { SetValue(RightCellProperty, value); }
        }

        public TrolleyWithCellsControl()
        {
            InitializeComponent();
        }
    }
}