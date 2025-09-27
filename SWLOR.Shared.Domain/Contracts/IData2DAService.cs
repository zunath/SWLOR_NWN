namespace SWLOR.Shared.Domain.Contracts
{
    /// <summary>
    /// Interface for reading data from 2DA files
    /// </summary>
    public interface IData2DAService
    {
        /// <summary>
        /// Gets the number of rows in a 2DA file
        /// </summary>
        /// <param name="fileName">The name of the 2DA file</param>
        /// <returns>The number of rows</returns>
        int GetRowCount(string fileName);

        /// <summary>
        /// Gets a string value from a 2DA file
        /// </summary>
        /// <param name="fileName">The name of the 2DA file</param>
        /// <param name="columnName">The name of the column</param>
        /// <param name="row">The row number</param>
        /// <returns>The string value</returns>
        string GetStringValue(string fileName, string columnName, int row);

        /// <summary>
        /// Gets a string reference value and converts it to the actual string
        /// </summary>
        /// <param name="strRef">The string reference number</param>
        /// <returns>The actual string value</returns>
        string GetStringByStrRef(int strRef);
    }
}
