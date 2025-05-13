using System.Windows;
using EM.Maman.Models.LocalDbModels;

namespace EM.Maman.DriverClient.ViewModels
{
    public partial class MainViewModel
    {
        #region Trolley Operations

        public void TrolleyMethods_MoveTrolleyUp()
        {
            if (CurrentTrolley.Position > 0)
            {
                CurrentTrolley.Position--;
                OnPropertyChanged(nameof(CurrentTrolley));

                (MoveTrolleyUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (MoveTrolleyDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public void TrolleyMethods_MoveTrolleyDown()
        {
            if (CurrentTrolley.Position < 23)
            {
                CurrentTrolley.Position++;
                OnPropertyChanged(nameof(CurrentTrolley));

                (MoveTrolleyUpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (MoveTrolleyDownCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public void PickPallet(Cell sourceCell, Pallet pallet)
        {
            if (TrolleyVM.LeftCell.IsOccupied && TrolleyVM.RightCell.IsOccupied)
            {
                MessageBox.Show("Trolley has no available cells. Please unload a cell first.");
                return;
            }

            if (!TrolleyVM.LeftCell.IsOccupied)
            {
                TrolleyVM.LoadPalletIntoLeftCell(pallet);
            }
            else
            {
                TrolleyVM.LoadPalletIntoRightCell(pallet);
            }
        }

        public void PutPallet(Cell destinationCell, string cellSide)
        {
            Pallet pallet = null;

            if (cellSide == "Left" && TrolleyVM.LeftCell.IsOccupied)
            {
                pallet = TrolleyVM.RemovePalletFromLeftCell();
            }
            else if (cellSide == "Right" && TrolleyVM.RightCell.IsOccupied)
            {
                pallet = TrolleyVM.RemovePalletFromRightCell();
            }

            if (pallet == null)
            {
                MessageBox.Show("No pallet in the selected trolley cell.");
                return;
            }

            // Update destination cell in database
        }

       

        #endregion
    }
}