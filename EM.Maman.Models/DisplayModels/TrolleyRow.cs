using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.DisplayModels
{
    public class TrolleyRow
    {
        public int Position { get; set; }
        public Cell OuterCell { get; set; }
        public Cell InnerCell { get; set; }
        public Finger? Finger { get; set; }
    }
}
