using System;

namespace EM.Maman.DriverClient.EventArgs
{
    /// <summary>
    /// Event arguments for dialog result events
    /// </summary>
    public class DialogResultEventArgs : System.EventArgs
    {
        /// <summary>
        /// Gets the dialog result
        /// </summary>
        public bool Result { get; }

        /// <summary>
        /// Initializes a new instance of the DialogResultEventArgs class
        /// </summary>
        /// <param name="result">The dialog result</param>
        public DialogResultEventArgs(bool result)
        {
            Result = result;
        }
    }
}
