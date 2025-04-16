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

        // Cells - now supporting up to 4 cells on each side
        public Cell? LeftCell1 { get; set; } // Outermost cell (was LeftOuterCell)
        public Cell? LeftCell2 { get; set; } // (was LeftInnerCell)
        public Cell? LeftCell3 { get; set; } // New additional cell
        public Cell? LeftCell4 { get; set; } // New additional cell (innermost)
        
        public Cell? RightCell1 { get; set; } // Outermost cell (was RightOuterCell)
        public Cell? RightCell2 { get; set; } // (was RightInnerCell)
        public Cell? RightCell3 { get; set; } // New additional cell
        public Cell? RightCell4 { get; set; } // New additional cell (innermost)

        // For backward compatibility
        public Cell? LeftOuterCell { get => LeftCell1; set => LeftCell1 = value; }
        public Cell? LeftInnerCell { get => LeftCell2; set => LeftCell2 = value; }
        public Cell? RightOuterCell { get => RightCell1; set => RightCell1 = value; }
        public Cell? RightInnerCell { get => RightCell2; set => RightCell2 = value; }

        // Fingers
        public Finger? LeftFinger { get; set; }
        public Finger? RightFinger { get; set; }

        // Finger pallet counts
        public int LeftFingerPalletCount { get; set; }
        public int RightFingerPalletCount { get; set; }

        // Pallets - now supporting up to 4 cells on each side
        public Pallet? LeftCell1Pallet { get; set; } // Outermost cell pallet (was LeftOuterPallet)
        public Pallet? LeftCell2Pallet { get; set; } // (was LeftInnerPallet)
        public Pallet? LeftCell3Pallet { get; set; } // New additional cell pallet
        public Pallet? LeftCell4Pallet { get; set; } // New additional cell pallet (innermost)
        
        public Pallet? RightCell1Pallet { get; set; } // Outermost cell pallet (was RightOuterPallet)
        public Pallet? RightCell2Pallet { get; set; } // (was RightInnerPallet)
        public Pallet? RightCell3Pallet { get; set; } // New additional cell pallet
        public Pallet? RightCell4Pallet { get; set; } // New additional cell pallet (innermost)

        // For backward compatibility
        public Pallet? LeftOuterPallet { get => LeftCell1Pallet; set => LeftCell1Pallet = value; }
        public Pallet? LeftInnerPallet { get => LeftCell2Pallet; set => LeftCell2Pallet = value; }
        public Pallet? RightOuterPallet { get => RightCell1Pallet; set => RightCell1Pallet = value; }
        public Pallet? RightInnerPallet { get => RightCell2Pallet; set => RightCell2Pallet = value; }

        // Helper properties
        public bool HasLeftCell1Pallet => LeftCell1Pallet != null;
        public bool HasLeftCell2Pallet => LeftCell2Pallet != null;
        public bool HasLeftCell3Pallet => LeftCell3Pallet != null;
        public bool HasLeftCell4Pallet => LeftCell4Pallet != null;
        
        public bool HasRightCell1Pallet => RightCell1Pallet != null;
        public bool HasRightCell2Pallet => RightCell2Pallet != null;
        public bool HasRightCell3Pallet => RightCell3Pallet != null;
        public bool HasRightCell4Pallet => RightCell4Pallet != null;

        // For backward compatibility
        public bool HasLeftOuterPallet => HasLeftCell1Pallet;
        public bool HasLeftInnerPallet => HasLeftCell2Pallet;
        public bool HasRightOuterPallet => HasRightCell1Pallet;
        public bool HasRightInnerPallet => HasRightCell2Pallet;
    }
}
