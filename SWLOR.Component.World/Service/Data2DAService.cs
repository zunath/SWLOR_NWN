using SWLOR.Shared.Domain.Contracts;
using SWLOR.NWN.API.Service;

namespace SWLOR.Component.World.Service
{
    /// <summary>
    /// Service for reading data from 2DA files
    /// </summary>
    public class Data2DAService : IData2DAService
    {
        public Data2DAService()
        {
        }

        /// <summary>
        /// Gets the number of rows in a 2DA file
        /// </summary>
        /// <param name="fileName">The name of the 2DA file</param>
        /// <returns>The number of rows</returns>
        public int GetRowCount(string fileName)
        {
            return Get2DARowCount(fileName);
        }

        /// <summary>
        /// Gets a string value from a 2DA file
        /// </summary>
        /// <param name="fileName">The name of the 2DA file</param>
        /// <param name="columnName">The name of the column</param>
        /// <param name="row">The row number</param>
        /// <returns>The string value</returns>
        public string GetStringValue(string fileName, string columnName, int row)
        {
            return Get2DAString(fileName, columnName, row);
        }

        /// <summary>
        /// Gets a string reference value and converts it to the actual string
        /// </summary>
        /// <param name="strRef">The string reference number</param>
        /// <returns>The actual string value</returns>
        public string GetStringByStrRef(int strRef)
        {
            return NWScript.GetStringByStrRef(strRef);
        }
    }
}
