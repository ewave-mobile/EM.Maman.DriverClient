using EM.Maman.Models.PlcModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EM.Maman.DriverClient.ViewModels
{
    // Partial class containing OPC and Register handling methods for MainViewModel
    public partial class MainViewModel
    {
        // These methods now delegate to the OpcViewModel

        /// <summary>
        /// Initializes the registers by delegating to OpcViewModel
        /// </summary>
        private void InitializeRegisters()
        {
            // This is now handled by the OpcViewModel constructor
        }

        /// <summary>
        /// Refreshes the registers by delegating to OpcViewModel
        /// </summary>
        private async Task RefreshRegistersAsync()
        {
            // This is now handled by the OpcViewModel
            if (OpcVM != null)
            {
                await Task.Run(() => OpcVM.RefreshRegistersCommand.Execute(null));
            }
        }

        /// <summary>
        /// Connects to the OPC server by delegating to OpcViewModel
        /// </summary>
        private void ConnectToOpc()
        {
            // This is now handled by the OpcViewModel
            OpcVM?.ConnectToOpcCommand.Execute(null);
        }

        /// <summary>
        /// Subscribes to a register by delegating to OpcViewModel
        /// </summary>
        private void SubscribeRegister(Register register)
        {
            // This is now handled by the OpcViewModel
            OpcVM?.SubscribeRegisterCommand.Execute(register);
        }

        /// <summary>
        /// Handles register changes from the OPC service
        /// This is now handled by the OpcViewModel's RegisterChanged event handler
        /// </summary>
        private void OpcService_RegisterChanged(object sender, RegisterChangedEventArgs e)
        {
            // This is now handled by the OpcViewModel
        }

        /// <summary>
        /// Determines whether a register can be written to
        /// </summary>
        private bool CanWriteRegister(object parameter)
        {
            // This is now handled by the OpcViewModel
            return true;
        }

        /// <summary>
        /// Writes to a register by delegating to OpcViewModel
        /// </summary>
        private void WriteRegister(object parameter)
        {
            // This is now handled by the OpcViewModel
            OpcVM?.WriteRegisterCommand.Execute(parameter);
        }

        /// <summary>
        /// Subscribes to cell changes
        /// </summary>
        private void SubscribeToCellChanges()
        {
            // In a real application, this would subscribe to cell changes
            // For now, it's just a placeholder
        }
    }
}
