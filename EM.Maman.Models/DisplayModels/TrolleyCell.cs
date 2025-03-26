using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.DisplayModels
{
    /// <summary>
    /// Represents a cell on a trolley that can contain a pallet
    /// </summary>
    public class TrolleyCell : INotifyPropertyChanged
    {
        private Pallet _pallet;
        private bool _isOccupied;
        private string _cellPosition; // "Left" or "Right"

        public Pallet Pallet
        {
            get => _pallet;
            set
            {
                if (_pallet != value)
                {
                    _pallet = value;
                    IsOccupied = _pallet != null;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOccupied
        {
            get => _isOccupied;
            private set
            {
                if (_isOccupied != value)
                {
                    _isOccupied = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CellPosition
        {
            get => _cellPosition;
            set
            {
                if (_cellPosition != value)
                {
                    _cellPosition = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
