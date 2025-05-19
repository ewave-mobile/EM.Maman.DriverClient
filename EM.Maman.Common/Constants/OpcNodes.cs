using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EM.Maman.Common.Constants
{
    public static class OpcNodes
    {
        /// <summary>
        /// Base namespace and path for Maman PLC nodes
        /// </summary>
        private const string MamanBase = "ns=2;s=s7.s7 300.maman.";

        /// <summary>
        /// Current position process value
        /// </summary>
        public const string PositionPV = MamanBase + "Position_PV";

        /// <summary>
        /// Requested/target position setpoint
        /// </summary>
        public const string PositionRequest = MamanBase + "PositionRequest";

        /// <summary>
        /// Control command register (1 = start movement, 0 = stop)
        /// </summary>
        public const string Control = MamanBase + "control";

        /// <summary>
        /// Status register ("Idle", "Moving", etc.)
        /// </summary>
        public const string Status = MamanBase + "status";

        /// <summary>
        /// Speed register (units per second)
        /// </summary>
        public const string Speed = MamanBase + "speed";

        /// <summary>
        /// Register for controlling In/Out operations of pallets from/to trolley cells.
        /// </summary>
        public const string InOutRegister = MamanBase + "In_Out";

        // Other system registers
        public const string Watchdog = "ns=2;s=Watchdog";
        public const string SystemStatus = "ns=2;s=SystemStatus";
        public const string ErrorCode = "ns=2;s=ErrorCode";
        public const string SafetyCircuit = "ns=2;s=SafetyCircuit";
        public const string EmergencyStop = "ns=2;s=EmergencyStop";
        public const string PowerOn = "ns=2;s=PowerOn";
    }
}
