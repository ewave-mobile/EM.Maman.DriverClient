using EM.Maman.Models.LocalDbModels;

namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing trolley movement methods for MainViewModel
    public partial class MainViewModel
    {
        // These methods are now implemented in the main class as TrolleyMethods_MoveTrolleyUp and TrolleyMethods_MoveTrolleyDown
        
        // Method to add a pallet to the trolley's left cell
        public void AddPalletToTrolleyLeftCell(Pallet pallet)
        {
            if (TrolleyVM != null)
            {
                TrolleyVM.LoadPalletIntoLeftCell(pallet);
            }
        }

        // Method to add a pallet to the trolley's right cell
        public void AddPalletToTrolleyRightCell(Pallet pallet)
        {
            if (TrolleyVM != null)
            {
                TrolleyVM.LoadPalletIntoRightCell(pallet);
            }
        }

        // Method to remove a pallet from the trolley's left cell
        public Pallet RemovePalletFromTrolleyLeftCell()
        {
            if (TrolleyVM != null)
            {
                return TrolleyVM.RemovePalletFromLeftCell();
            }
            return null;
        }

        // Method to remove a pallet from the trolley's right cell
        public Pallet RemovePalletFromTrolleyRightCell()
        {
            if (TrolleyVM != null)
            {
                return TrolleyVM.RemovePalletFromRightCell();
            }
            return null;
        }
    }
}
