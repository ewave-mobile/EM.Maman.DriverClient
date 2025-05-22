using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Models.Enums
{
    public enum ActiveTaskStatus
    {
        New = 0, // Added New status
        retrieval = 1,
        transit = 2,
        storing = 3,
        finished = 4,
        pending = 5,
        pending_authentication = 6,
        authentication = 7,
        arrived_at_destination = 8,
        navigating_to_source = 9 // Added for retrieval task navigating to source cell
    }
}
