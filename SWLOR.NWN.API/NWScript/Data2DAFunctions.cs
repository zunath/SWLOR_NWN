namespace SWLOR.NWN.API.NWScript
{
    public partial class NWScript
    {
        /// <summary>
        /// Gets a value from a 2DA file on the server and returns it as a string.
        /// </summary>
        /// <param name="s2DA">The name of the 2da file (16 chars max)</param>
        /// <param name="sColumn">The name of the column in the 2da</param>
        /// <param name="nRow">The row in the 2da</param>
        /// <returns>The value as a string. Returns an empty string if file, row, or column not found</returns>
        /// <remarks>Avoid using this function in loops.</remarks>
        public static string Get2DAString(string s2DA, string sColumn, int nRow)
        {
            return global::NWN.Core.NWScript.Get2DAString(s2DA, sColumn, nRow);
        }

        /// <summary>
        /// Returns the column name of the 2DA file at the specified column index.
        /// </summary>
        /// <param name="s2DA">The name of the 2da file</param>
        /// <param name="nColumnIdx">The column index (starting at 0)</param>
        /// <returns>The column name. Returns empty string if column doesn't exist (at end)</returns>
        public static string Get2DAColumn(string s2DA, int nColumnIdx)
        {
            return global::NWN.Core.NWScript.Get2DAColumn(s2DA, nColumnIdx);
        }

        /// <summary>
        /// Returns the number of defined rows in the specified 2DA file.
        /// </summary>
        /// <param name="s2DA">The name of the 2da file</param>
        /// <returns>The number of defined rows in the 2DA file</returns>
        public static int Get2DARowCount(string s2DA)
        {
            return global::NWN.Core.NWScript.Get2DARowCount(s2DA);
        }
    }
}
