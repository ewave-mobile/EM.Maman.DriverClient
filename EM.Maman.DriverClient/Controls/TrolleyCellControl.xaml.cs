using EM.Maman.Models.DisplayModels;
using EM.Maman.Models.LocalDbModels;
using System.Windows;
using System.Windows.Controls;

namespace EM.Maman.DriverClient.Controls
{
    public partial class TrolleyCellControl : UserControl
    {
        public static readonly DependencyProperty TrolleyCellProperty =
            DependencyProperty.Register("TrolleyCell", typeof(Models.DisplayModels.TrolleyCell), typeof(TrolleyCellControl),
                new PropertyMetadata(null, OnTrolleyCellChanged));

        public static readonly DependencyProperty IsOccupiedProperty =
            DependencyProperty.Register("IsOccupied", typeof(bool), typeof(TrolleyCellControl),
                new PropertyMetadata(false));

        public static readonly DependencyProperty IsNotOccupiedProperty =
            DependencyProperty.Register("IsNotOccupied", typeof(bool), typeof(TrolleyCellControl),
                new PropertyMetadata(true));

        public static readonly DependencyProperty PalletDisplayNameProperty =
            DependencyProperty.Register("PalletDisplayName", typeof(string), typeof(TrolleyCellControl),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty CellPositionProperty =
            DependencyProperty.Register("CellPosition", typeof(string), typeof(TrolleyCellControl),
                new PropertyMetadata(string.Empty));

        public Models.DisplayModels.TrolleyCell TrolleyCell
        {
            get { return (Models.DisplayModels.TrolleyCell)GetValue(TrolleyCellProperty); }
            set { SetValue(TrolleyCellProperty, value); }
        }

        public bool IsOccupied
        {
            get { return (bool)GetValue(IsOccupiedProperty); }
            private set { SetValue(IsOccupiedProperty, value); }
        }

        public bool IsNotOccupied
        {
            get { return (bool)GetValue(IsNotOccupiedProperty); }
            private set { SetValue(IsNotOccupiedProperty, value); }
        }

        public string PalletDisplayName
        {
            get { return (string)GetValue(PalletDisplayNameProperty); }
            private set { SetValue(PalletDisplayNameProperty, value); }
        }

        public string CellPosition
        {
            get { return (string)GetValue(CellPositionProperty); }
            set { SetValue(CellPositionProperty, value); }
        }

        public TrolleyCellControl()
        {
            InitializeComponent();
        }

        private static void OnTrolleyCellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TrolleyCellControl;
            if (control != null)
            {
                var cell = e.NewValue as Models.DisplayModels.TrolleyCell;
                if (cell != null)
                {
                    control.UpdateProperties(cell);

                    // Subscribe to property changes on the TrolleyCell
                    cell.PropertyChanged -= control.Cell_PropertyChanged;  // Avoid double subscription
                    cell.PropertyChanged += control.Cell_PropertyChanged;
                }
                else
                {
                    control.ClearProperties();
                }
            }
        }

        private void Cell_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Update properties when the TrolleyCell changes
            if (sender is Models.DisplayModels.TrolleyCell cell)
            {
                if (e.PropertyName == nameof(TrolleyCell.Pallet) ||
                    e.PropertyName == nameof(TrolleyCell.IsOccupied))
                {
                    UpdateProperties(cell);
                }
                else if (e.PropertyName == nameof(TrolleyCell.CellPosition))
                {
                    CellPosition = cell.CellPosition;
                }
            }
        }

        private void UpdateProperties(Models.DisplayModels.TrolleyCell cell)
        {
            IsOccupied = cell.IsOccupied;
            IsNotOccupied = !cell.IsOccupied;
            PalletDisplayName = cell.Pallet?.DisplayName ?? string.Empty;
            CellPosition = cell.CellPosition;
        }

        private void ClearProperties()
        {
            IsOccupied = false;
            IsNotOccupied = true;
            PalletDisplayName = string.Empty;
            CellPosition = string.Empty;
        }
    }
}
