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
        public Finger? LeftFinger { get; set; }
        public Cell? LeftOuterCell { get; set; }
        public Cell? LeftInnerCell { get; set; }
        public Cell? RightOuterCell { get; set; }
        public Cell? RightInnerCell { get; set; }
        public Finger? RightFinger { get; set; }
    }

}
