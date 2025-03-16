using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.CustomModels
{
    public class SyncProgressEventArgs : EventArgs
    {
        public int Current { get; }
        public int Total { get; }
        public double ProgressPercentage => Total > 0 ? (double)Current / Total * 100 : 0;

        public SyncProgressEventArgs(int current, int total)
        {
            Current = current;
            Total = total;
        }
    }
}
