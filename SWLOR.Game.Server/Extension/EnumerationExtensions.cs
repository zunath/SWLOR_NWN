using System;
using System.ComponentModel;
using System.Reflection;

namespace SWLOR.Game.Server.Extension
{
    public static class EnumerationExtensions
    {
        public static string GetDescriptionAttribute<T>(this T source)
            where T: Enum
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes.Length > 0) return attributes[0].Description;
            else return source.ToString();
        }
    }
}
