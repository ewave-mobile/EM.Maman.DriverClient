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
    /// Represents a tab in the level selection UI
    /// </summary>
    public class LevelTab : INotifyPropertyChanged
    {
        private Level _level;
        private bool _isSelected;
        private int _itemCount;
        private int _emptyCount;
        private string _displayName;

        public Level Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ItemCount
        {
            get => _itemCount;
            set
            {
                if (_itemCount != value)
                {
                    _itemCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public int EmptyCount
        {
            get => _emptyCount;
            set
            {
                if (_emptyCount != value)
                {
                    _emptyCount = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayName
        {
            get => _displayName ?? Level?.DisplayName ?? Level?.Number.ToString();
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged();
                }
            }
        }

        public LevelTab(Level level, bool isSelected = false)
        {
            Level = level;
            IsSelected = isSelected;

            // These values would typically come from your database
            // For now we'll set some example values
            CalculateItemCounts();
        }

        private void CalculateItemCounts()
        {
            // In a real implementation, you would query the database or use a cached value
            // to determine how many cells contain items and how many are empty

            Random random = new Random();
            // Just for demo purposes - in real app this would be calculated based on actual data
            ItemCount = random.Next(0, 8);
            EmptyCount = random.Next(5, 12);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
