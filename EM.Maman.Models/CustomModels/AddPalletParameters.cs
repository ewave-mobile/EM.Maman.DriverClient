using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.CustomModels
{
    public class AddPalletParameters
    {
        public int? CellId { get; set; }
        public string DisplayName { get; set; }
        public string UldCode { get; set; }
        public string UldType { get; set; }
    }
}
