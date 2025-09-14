using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        ///   Gets a value from a 2DA file on the server and returns it as a string
        ///   avoid using this function in loops
        ///   - s2DA: the name of the 2da file, 16 chars max
        ///   - sColumn: the name of the column in the 2da
        ///   - nRow: the row in the 2da
        ///   * returns an empty string if file, row, or column not found
        /// </summary>
        public static string Get2DAString(string s2DA, string sColumn, int nRow)
        {
            return global::NWN.Core.NWScript.Get2DAString(s2DA, sColumn, nRow);
        }

        /// <summary>
        ///  Returns the column name of s2DA at nColumn index (starting at 0).
        /// Returns "" if column nColumn doesn't exist (at end).
        /// </summary>
        public static string Get2DAColumn(string s2DA, int nColumnIdx)
        {
            return global::NWN.Core.NWScript.Get2DAColumn(s2DA, nColumnIdx);
        }

        /// <summary>
        /// Returns the number of defined rows in the 2da s2DA.
        /// </summary>
        public static int Get2DARowCount(string s2DA)
        {
            return global::NWN.Core.NWScript.Get2DARowCount(s2DA);
        }
    }
}
