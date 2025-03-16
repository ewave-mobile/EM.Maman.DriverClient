using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Interfaces.Services
{
    public interface IDispatcherService
    {
        void Invoke(Action action);
    }
}

