using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.PlcModels
{
    public class RegisterChangedEventArgs : EventArgs
    {
        public string RegisterName { get; }
        public object NewValue { get; }

        public RegisterChangedEventArgs(string registerName, object newValue)
        {
            RegisterName = registerName;
            NewValue = newValue;
        }
    }
}
