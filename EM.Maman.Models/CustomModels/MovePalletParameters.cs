using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.CustomModels
{
    public class MovePalletParameters
    {
        public int PalletId { get; set; }
        public int SourceCellId { get; set; }
        public int DestinationCellId { get; set; }
    }
}
