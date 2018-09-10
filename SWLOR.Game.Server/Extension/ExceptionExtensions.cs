using System;
using System.Data.Entity.Validation;
using System.Text;

namespace SWLOR.Game.Server.Extension
{
    public static class ExceptionExtensions
    {
        public static string ToMessageAndCompleteStacktrace(this Exception exception)
        {
            Exception e = exception;
            StringBuilder s = new StringBuilder();
            while (e != null)
            {
                s.AppendLine("Exception type: " + e.GetType().FullName);
                s.AppendLine("Message       : " + (e.Message ?? string.Empty));
                s.AppendLine("Stacktrace:");
                s.AppendLine(e.StackTrace);
                
                if (e is DbEntityValidationException valEx)
                {
                    s.AppendLine("Entity validation errors: ");
                    
                    foreach (var error in valEx.EntityValidationErrors)
                    {
                        if (error.ValidationErrors == null) continue;
                        foreach (var val in error.ValidationErrors)
                        {
                            s.AppendLine(val.PropertyName + ": " + val.ErrorMessage);
                        }

                    }

                    s.AppendLine();
                }

                s.AppendLine();
                e = e.InnerException;
            }

            

            return s.ToString();
        }
    }
}
