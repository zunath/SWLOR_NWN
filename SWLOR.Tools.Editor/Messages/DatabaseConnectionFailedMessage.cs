using System;
using System.Runtime.Serialization;

namespace SWLOR.Tools.Editor.Messages
{
    public class DatabaseConnectionFailedMessage
    {
        public Exception Exception { get; set; }

        public DatabaseConnectionFailedMessage(Exception ex)
        {
            Exception = ex;
        }
    }
}
