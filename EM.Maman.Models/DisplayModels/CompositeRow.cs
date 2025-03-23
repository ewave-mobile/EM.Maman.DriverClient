using EM.Maman.Models.LocalDbModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.DisplayModels
{
    public class CompositeRow
    {
        public int Position { get; set; }

        // Cells
        public Cell? LeftOuterCell { get; set; }
        public Cell? LeftInnerCell { get; set; }
        public Cell? RightOuterCell { get; set; }
        public Cell? RightInnerCell { get; set; }

        // Fingers
        public Finger? LeftFinger { get; set; }
        public Finger? RightFinger { get; set; }

        // Pallets
        public Pallet? LeftOuterPallet { get; set; }
        public Pallet? LeftInnerPallet { get; set; }
        public Pallet? RightOuterPallet { get; set; }
        public Pallet? RightInnerPallet { get; set; }

        // Helper properties
        public bool HasLeftOuterPallet => LeftOuterPallet != null;
        public bool HasLeftInnerPallet => LeftInnerPallet != null;
        public bool HasRightOuterPallet => RightOuterPallet != null;
        public bool HasRightInnerPallet => RightInnerPallet != null;
    }

}
