using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.CustomModels
{
    public class CellWithPalletInfo
    {
        public Cell Cell { get; set; }
        public Pallet Pallet { get; set; }
        public bool HasPallet => Pallet != null;
    }
}
