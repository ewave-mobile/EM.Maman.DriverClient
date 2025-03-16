using EM.Maman.Models.Interfaces.Services;
using EM.Maman.Models.PlcModels;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using System.Xml.Linq;
using Azure.Core;
using System.Collections.Concurrent;

namespace EM.Maman.Services.PlcServices
{
    public class OpcUaService : IOpcService, IDisposable
    {
        // Event raised when a monitored register value changes.
        public event EventHandler<RegisterChangedEventArgs> RegisterChanged;
        // Event raised when connectivity changes.
        public event EventHandler<bool> ConnectivityChanged;

        private ApplicationConfiguration _config;
        private Session _session;
        private Subscription _subscription;
        private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
        private readonly TimeSpan WatchdogTimeout = TimeSpan.FromSeconds(5);
        private Timer _watchdogTimer;
        private const string WatchdogNodeId = "ns=2;s=Watchdog"; // Example node identifier
        private const string ExpectedWatchdogResetValue = "ResetValue"; // Define as needed
        public bool IsConnected { get; private set; } = false;

        /// <summary>
        /// Asynchronously connects to the OPC UA server using the provided endpoint URL.
        /// </summary>
        /// <param name="serverUrl">The OPC UA server endpoint URL.</param>
        public async Task ConnectAsync(string serverUrl)
        {
            // Configure the OPC UA application.
            _config = new ApplicationConfiguration()
            {
                ApplicationName = "MyOpcUaClient",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    // Update these settings to match your certificate store/environment.
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault",
                        SubjectName = "MyOpcUaClient"
                    },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
                TraceConfiguration = new TraceConfiguration()
            };

            // Validate configuration.
            await _config.Validate(ApplicationType.Client);

            if (_config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                _config.CertificateValidator.CertificateValidation += (s, e) =>
                {
                    e.Accept = true;
                };
            }

            // Select the endpoint.
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(_config, serverUrl, useSecurity: false);
            //var endpointConfiguration = EndpointConfiguration.Create(_config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint);

            // Create the session.
            _session = await Session.Create(_config, endpoint, false, "MyOpcUaSession", 60000, null, null);

            IsConnected = true;
            ConnectivityChanged?.Invoke(this, IsConnected);

            // Create a subscription for live updates.
            _subscription = new Subscription(_session.DefaultSubscription)
            {
                PublishingInterval = 1000 // 1 second publishing interval.
            };
            _session.AddSubscription(_subscription);
            _subscription.Create();
        }

        /// <summary>
        /// Disconnects the OPC UA session.
        /// </summary>
        public void Disconnect()
        {
            if (_session != null)
            {
                _session.Close();
                _session.Dispose();
                _session = null;
            }
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.RemoveItems(subscription.MonitoredItems);
                _session.RemoveSubscription(subscription);
                subscription.Dispose();
            }
            IsConnected = false;
            ConnectivityChanged?.Invoke(this, IsConnected);
        }

        /// <summary>
        /// Asynchronously writes a value to a given node.
        /// </summary>
        /// <param name="nodeId">The NodeId of the register as a string.</param>
        /// <param name="value">The value to write.</param>
        public async Task WriteRegisterAsync(string nodeId, object value)
        {
            if (_session == null)
                throw new InvalidOperationException("Session is not connected.");

            WriteValue writeValue = new WriteValue
            {
                NodeId = new NodeId(nodeId),
                AttributeId = Attributes.Value,
                Value = new DataValue(new Variant(value))
            };

            WriteValueCollection writeValues = new WriteValueCollection { writeValue };

            // WriteAsync returns the results and diagnostic info.
            StatusCodeCollection results;
            DiagnosticInfoCollection diagnosticInfos;
            var response = await _session.WriteAsync(null, writeValues.ToArray(), CancellationToken.None);
            //await _session.WriteAsync(
            //    null,
            //    writeValues,
            //    out results,
            //    out diagnosticInfos);

            // Optionally check results and throw an exception if the write failed.
            if (StatusCode.IsBad(response.Results[0]))
            {
                throw new Exception($"Write failed for node {nodeId}: {response.Results[0]}");
            }
        }

        /// <summary>
        /// Asynchronously reads a value from a given node.
        /// </summary>
        /// <param name="nodeId">The NodeId of the register as a string.</param>
        /// <returns>The value read from the node.</returns>
        public async Task<object> ReadRegisterAsync(string nodeId)
        {
            if (_session == null)
                throw new InvalidOperationException("Session is not connected.");

            DataValue value = await _session.ReadValueAsync(new NodeId(nodeId));
            return value.Value;
        }
        public async Task<List<Register>> ReadRegistersAsync(List<string> nodeIds)
        {
            var reads = new List<ReadValueId>();
            foreach (var nodeId in nodeIds)
            {
                reads.Add(new ReadValueId
                {
                    NodeId = new Opc.Ua.NodeId(nodeId),
                    AttributeId = Attributes.Value
                });
            }

            var response = await _session.ReadAsync(null, 0, TimestampsToReturn.Neither, reads.ToArray(), CancellationToken.None);


            var registers = new List<Register>();
            for (int i = 0; i < nodeIds.Count; i++)
            {
                registers.Add(new Register
                {
                    NodeId = nodeIds[i],
                    Value = response.Results[i].Value?.ToString(),
                    IsSubscribed = false
                });
            }

            return registers;
        }
        /// <summary>
        /// Subscribes to live updates for a specific node (register).
        /// </summary>
        /// <param name="nodeId">The NodeId of the register as a string.</param>

        public void SubscribeToRegister(string nodeId, Action<Register> onValueChanged)
        {
            if (_subscriptions.ContainsKey(nodeId))
                return;

            try
            {
                var subscription = new Subscription(_session.DefaultSubscription)
                {
                    PublishingInterval = 1000 // 1 second
                };

                var monitoredItem = new MonitoredItem(subscription.DefaultItem)
                {
                    StartNodeId = nodeId,
                    AttributeId = Attributes.Value,
                    SamplingInterval = 1000, // 1 second
                    QueueSize = 10,
                    DiscardOldest = true
                };

                monitoredItem.Notification += (sender, e) =>
                {
                    foreach (var value in sender.DequeueValues())
                    {
                        onValueChanged(new Register
                        {
                            NodeId = nodeId,
                            Value = value.Value?.ToString(),
                            IsSubscribed = true
                        });
                    }
                };

                subscription.AddItem(monitoredItem);
                _session.AddSubscription(subscription);
                subscription.Create();

                _subscriptions[nodeId] = subscription;
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                throw new Exception($"Failed to subscribe to register '{nodeId}': {ex.Message}", ex);
            }
        }
        public void Unsubscribe(string nodeId)
        {
            if (_subscriptions.TryRemove(nodeId, out var subscription))
            {
                //subscription.RemoveItems();
                _session.RemoveSubscription(subscription);
                subscription.Dispose();
            }
        }
        ///// <summary>
        ///// Handles notifications from monitored items and raises the RegisterChanged event.
        ///// </summary>
        //private void MonitoredItem_Notification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        //{
        //    foreach (var notification in item.DequeueValues())
        //    {
        //        // Raise an event for each value change.
        //        RegisterChanged?.Invoke(this, new RegisterChangedEventArgs(item.StartNodeId.Identifier.ToString(), notification.Value));
        //    }
        //}


        public void Dispose()
        {
            Disconnect();
        }
    }
}
