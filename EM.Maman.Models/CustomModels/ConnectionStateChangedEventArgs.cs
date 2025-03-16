using EM.Maman.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.CustomModels
{
    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public ConnectionType ConnectionType { get; }
        public bool IsConnected { get; }
        public string Message { get; }

        public ConnectionStateChangedEventArgs(ConnectionType connectionType, bool isConnected, string message = null)
        {
            ConnectionType = connectionType;
            IsConnected = isConnected;
            Message = message;
        }
    }
}
