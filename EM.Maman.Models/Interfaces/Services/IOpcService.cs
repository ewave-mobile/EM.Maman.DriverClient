using EM.Maman.Models.PlcModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Services
{
    public interface IOpcService : IDisposable
    {
        /// <summary>
        /// Raised when a monitored register's value changes.
        /// </summary>
        event EventHandler<RegisterChangedEventArgs> RegisterChanged;

        /// <summary>
        /// Raised when the connectivity status changes.
        /// The boolean parameter indicates whether the service is connected.
        /// </summary>
        event EventHandler<bool> ConnectivityChanged;

        /// <summary>
        /// Gets a value indicating whether the service is connected to the OPC UA server.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Asynchronously connects to the OPC UA server using the provided endpoint URL.
        /// </summary>
        /// <param name="serverUrl">The endpoint URL of the OPC UA server.</param>
        Task ConnectAsync(string serverUrl);

        /// <summary>
        /// Disconnects from the OPC UA server.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Asynchronously writes a value to the specified node.
        /// </summary>
        /// <param name="nodeId">The NodeId of the register as a string.</param>
        /// <param name="value">The value to write.</param>
        Task WriteRegisterAsync(string nodeId, object value);

        /// <summary>
        /// Asynchronously reads the value from the specified node.
        /// </summary>
        /// <param name="nodeId">The NodeId of the register as a string.</param>
        /// <returns>The value read from the node.</returns>
        Task<object> ReadRegisterAsync(string nodeId);

        /// <summary>
        /// Asynchronously reads the values from a list of registers.
        /// </summary>
        /// <param name="nodeIds">A list of NodeIds of the registers.</param>
        /// <returns>A list of registers with their values.</returns>
        Task<List<Register>> ReadRegistersAsync(List<string> nodeIds);

        /// <summary>
        /// Subscribes to live updates for a specific register.
        /// </summary>
        /// <param name="nodeId">The NodeId of the register as a string.</param>
        /// <param name="onValueChanged">Action callback to be invoked when the register value changes.</param>
        void SubscribeToRegister(string nodeId, Action<Register> onValueChanged);

        /// <summary>
        /// Unsubscribes from live updates for the specified register.
        /// </summary>
        /// <param name="nodeId">The NodeId of the register as a string.</param>
        void Unsubscribe(string nodeId);
    }
}
