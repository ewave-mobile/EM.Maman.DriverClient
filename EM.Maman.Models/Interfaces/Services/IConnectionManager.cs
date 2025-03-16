using EM.Maman.Models.CustomModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Services
{
    public interface IConnectionManager
    {
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        bool IsPlcConnected { get; }
        bool IsServerConnected { get; }
        bool IsNetworkAvailable { get; }
        bool IsOfflineModeEnabled { get; }

        Task ConnectToPlcAsync();
        void DisconnectFromPlc();
        Task CheckServerConnectionAsync();
        Task ToggleOfflineModeAsync(bool enable);
        void StartConnectionMonitoring();
        void StopConnectionMonitoring();
    }
}
