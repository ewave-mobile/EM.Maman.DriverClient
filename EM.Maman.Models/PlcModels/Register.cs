using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.PlcModels
{
    public class Register : INotifyPropertyChanged
    {
        private string _id;
        private string _nodeId;
        private string _name;
        private string _value;
        private bool _isSubscribed;
        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id));  }
        }
        public string NodeId
        {
            get => _nodeId;
            set { _nodeId = value; OnPropertyChanged(nameof(NodeId)); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(nameof(Name));}
        }

        public string Value 
        {
            get => _value;
            set { _value = value; OnPropertyChanged(nameof(Value));}
        }

        public bool IsSubscribed
        {
            get => _isSubscribed;
            set { _isSubscribed = value; OnPropertyChanged(nameof(IsSubscribed)); }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
