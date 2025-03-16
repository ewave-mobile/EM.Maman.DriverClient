using EM.Maman.Models.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EM.Maman.DriverClient.Services
{
    public class DispatcherService : IDispatcherService
    {
        public void Invoke(Action action)
        {


            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                // Optionally handle cases where Application.Current is null
                action();
            }
        }
    }
}
