using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace SWLOR.Game.Server.Extension
{
    public static class EnumerationExtension
    {
        /// <summary>
        /// Retrieves the value of a DescriptionAttribute found on an enumeration value.
        /// </summary>
        /// <typeparam name="T">The type of enumeration</typeparam>
        /// <param name="source">The enumeration value</param>
        /// <returns>The value of the description attribute, or the name of the enumeration value if no attribute exists.</returns>
        public static string GetDescriptionAttribute<T>(this T source)
            where T : Enum
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute), false);

            if (attributes.Length > 0) return attributes[0].Description;
            else return source.ToString();
        }

        /// <summary>
        /// Retrieves an attribute instance of a specified type from an enumeration value.
        /// </summary>
        /// <typeparam name="TEnum">The type of enumeration</typeparam>
        /// <typeparam name="TAttribute">The attribute type to retrieve.</typeparam>
        /// <param name="source">The enumeration value</param>
        /// <returns>The first instance of an attribute found on the enumeration value</returns>
        public static TAttribute GetAttribute<TEnum, TAttribute>(this TEnum source)
            where TEnum : Enum
            where TAttribute : Attribute
        {
            var fieldInfo = source.GetType().GetField(source.ToString());
            var attributes = (TAttribute[])fieldInfo.GetCustomAttributes(typeof(TAttribute), false);

            if (attributes.Length > 0) return attributes[0];
            else throw new Exception($"Could not find attribute '{typeof(TAttribute)}' on enumeration type {typeof(TEnum)}. Value = {source}");
        }

        /// <summary>
        /// Retrieves a list of attributes of a specified type from an enumeration value.
        /// </summary>
        /// <typeparam name="TEnum">The type of enumeration</typeparam>
        /// <typeparam name="TAttribute">The enumeration value</typeparam>
        /// <param name="source">All of the instances of an attribute found on the enumeration value</param>
        /// <returns></returns>
        public static IEnumerable<TAttribute> GetAttributes<TEnum, TAttribute>(this TEnum source)
            where TEnum : Enum
            where TAttribute : Attribute
        {
            var fieldInfo = source.GetType().GetField(source.ToString());
            var attributes = (TAttribute[])fieldInfo.GetCustomAttributes(typeof(TAttribute), false);

            return attributes.Length > 0 ?
                attributes :
                new List<TAttribute>().ToArray();
        }
    }
}
