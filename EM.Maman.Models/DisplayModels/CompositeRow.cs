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
    public class CompositeRow : INotifyPropertyChanged
    {
        private int _position;
        private Cell? _leftCell1;
        private Cell? _leftCell2;
        private Cell? _leftCell3;
        private Cell? _leftCell4;
        private Cell? _rightCell1;
        private Cell? _rightCell2;
        private Cell? _rightCell3;
        private Cell? _rightCell4;
        private Finger? _leftFinger;
        private Finger? _rightFinger;
        private int _leftFingerPalletCount;
        private int _rightFingerPalletCount;
        private Pallet? _leftCell1Pallet;
        private Pallet? _leftCell2Pallet;
        private Pallet? _leftCell3Pallet;
        private Pallet? _leftCell4Pallet;
        private Pallet? _rightCell1Pallet;
        private Pallet? _rightCell2Pallet;
        private Pallet? _rightCell3Pallet;
        private Pallet? _rightCell4Pallet;

        public int Position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    OnPropertyChanged();
                }
            }
        }

        // Cells - now supporting up to 4 cells on each side
        public Cell? LeftCell1 { get => _leftCell1; set { if (_leftCell1 != value) { _leftCell1 = value; OnPropertyChanged(); } } }
        public Cell? LeftCell2 { get => _leftCell2; set { if (_leftCell2 != value) { _leftCell2 = value; OnPropertyChanged(); } } }
        public Cell? LeftCell3 { get => _leftCell3; set { if (_leftCell3 != value) { _leftCell3 = value; OnPropertyChanged(); } } }
        public Cell? LeftCell4 { get => _leftCell4; set { if (_leftCell4 != value) { _leftCell4 = value; OnPropertyChanged(); } } }

        public Cell? RightCell1 { get => _rightCell1; set { if (_rightCell1 != value) { _rightCell1 = value; OnPropertyChanged(); } } }
        public Cell? RightCell2 { get => _rightCell2; set { if (_rightCell2 != value) { _rightCell2 = value; OnPropertyChanged(); } } }
        public Cell? RightCell3 { get => _rightCell3; set { if (_rightCell3 != value) { _rightCell3 = value; OnPropertyChanged(); } } }
        public Cell? RightCell4 { get => _rightCell4; set { if (_rightCell4 != value) { _rightCell4 = value; OnPropertyChanged(); } } }

        // For backward compatibility
        public Cell? LeftOuterCell { get => LeftCell1; set => LeftCell1 = value; }
        public Cell? LeftInnerCell { get => LeftCell2; set => LeftCell2 = value; }
        public Cell? RightOuterCell { get => RightCell1; set => RightCell1 = value; }
        public Cell? RightInnerCell { get => RightCell2; set => RightCell2 = value; }

        // Fingers
        public Finger? LeftFinger { get => _leftFinger; set { if (_leftFinger != value) { _leftFinger = value; OnPropertyChanged(); } } }
        public Finger? RightFinger { get => _rightFinger; set { if (_rightFinger != value) { _rightFinger = value; OnPropertyChanged(); } } }

        // Finger pallet counts
        public int LeftFingerPalletCount
        {
            get => _leftFingerPalletCount;
            set
            {
                if (_leftFingerPalletCount != value)
                {
                    _leftFingerPalletCount = value;
                    OnPropertyChanged();
                }
            }
        }
        public int RightFingerPalletCount
        {
            get => _rightFingerPalletCount;
            set
            {
                if (_rightFingerPalletCount != value)
                {
                    _rightFingerPalletCount = value;
                    OnPropertyChanged();
                }
            }
        }

        // Pallets - now supporting up to 4 cells on each side
        public Pallet? LeftCell1Pallet
        {
            get => _leftCell1Pallet;
            set
            {
                if (_leftCell1Pallet != value)
                {
                    _leftCell1Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasLeftCell1Pallet));
                }
            }
        }
        public Pallet? LeftCell2Pallet
        {
            get => _leftCell2Pallet;
            set
            {
                if (_leftCell2Pallet != value)
                {
                    _leftCell2Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasLeftCell2Pallet));
                }
            }
        }
        public Pallet? LeftCell3Pallet
        {
            get => _leftCell3Pallet;
            set
            {
                if (_leftCell3Pallet != value)
                {
                    _leftCell3Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasLeftCell3Pallet));
                }
            }
        }
        public Pallet? LeftCell4Pallet
        {
            get => _leftCell4Pallet;
            set
            {
                if (_leftCell4Pallet != value)
                {
                    _leftCell4Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasLeftCell4Pallet));
                }
            }
        }

        public Pallet? RightCell1Pallet
        {
            get => _rightCell1Pallet;
            set
            {
                if (_rightCell1Pallet != value)
                {
                    _rightCell1Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasRightCell1Pallet));
                }
            }
        }
        public Pallet? RightCell2Pallet
        {
            get => _rightCell2Pallet;
            set
            {
                if (_rightCell2Pallet != value)
                {
                    _rightCell2Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasRightCell2Pallet));
                }
            }
        }
        public Pallet? RightCell3Pallet
        {
            get => _rightCell3Pallet;
            set
            {
                if (_rightCell3Pallet != value)
                {
                    _rightCell3Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasRightCell3Pallet));
                }
            }
        }
        public Pallet? RightCell4Pallet
        {
            get => _rightCell4Pallet;
            set
            {
                if (_rightCell4Pallet != value)
                {
                    _rightCell4Pallet = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasRightCell4Pallet));
                }
            }
        }

        // For backward compatibility
        public Pallet? LeftOuterPallet { get => LeftCell1Pallet; set => LeftCell1Pallet = value; }
        public Pallet? LeftInnerPallet { get => LeftCell2Pallet; set => LeftCell2Pallet = value; }
        public Pallet? RightOuterPallet { get => RightCell1Pallet; set => RightCell1Pallet = value; }
        public Pallet? RightInnerPallet { get => RightCell2Pallet; set => RightCell2Pallet = value; }

        // Helper properties
        public bool HasLeftCell1Pallet => LeftCell1Pallet != null;
        public bool HasLeftCell2Pallet => LeftCell2Pallet != null;
        public bool HasLeftCell3Pallet => LeftCell3Pallet != null;
        public bool HasLeftCell4Pallet => LeftCell4Pallet != null;

        public bool HasRightCell1Pallet => RightCell1Pallet != null;
        public bool HasRightCell2Pallet => RightCell2Pallet != null;
        public bool HasRightCell3Pallet => RightCell3Pallet != null;
        public bool HasRightCell4Pallet => RightCell4Pallet != null;

        // For backward compatibility
        public bool HasLeftOuterPallet => HasLeftCell1Pallet;
        public bool HasLeftInnerPallet => HasLeftCell2Pallet;
        public bool HasRightOuterPallet => HasRightCell1Pallet;
        public bool HasRightInnerPallet => HasRightCell2Pallet;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
