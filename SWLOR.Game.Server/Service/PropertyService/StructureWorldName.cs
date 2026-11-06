namespace SWLOR.Game.Server.Service.PropertyService
{
    public static class StructureWorldName
    {
        /// <summary>
        /// The name to give a structure's placeable. An empty name makes SetName go back to the template's name, which is what a
        /// structure still using its catalog name should show.
        /// </summary>
        public static string Resolve(string customName, string catalogName)
        {
            return string.IsNullOrEmpty(customName) || customName == catalogName
                ? string.Empty
                : customName;
        }
    }
}
