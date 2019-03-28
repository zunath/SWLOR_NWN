using System;

namespace SWLOR.Tools.Editor.Messages
{
    public class DatabaseConnectionFailed
    {
        public Exception Exception { get; set; }

        public DatabaseConnectionFailed(Exception ex)
        {
            Exception = ex;
        }
    }
}
