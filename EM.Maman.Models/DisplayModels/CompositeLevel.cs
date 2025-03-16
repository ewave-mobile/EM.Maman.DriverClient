using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.DisplayModels
{
    public class CompositeLevel : INotifyPropertyChanged
    {
        private Level _level;
        private bool _isCurrentLevel;
        private ObservableCollection<CompositeRow> _rows;

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

        public ObservableCollection<CompositeRow> Rows
        {
            get => _rows;
            set
            {
                if (_rows != value)
                {
                    _rows = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsCurrentLevel
        {
            get => _isCurrentLevel;
            set
            {
                if (_isCurrentLevel != value)
                {
                    _isCurrentLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        public CompositeLevel()
        {
            Rows = new ObservableCollection<CompositeRow>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
