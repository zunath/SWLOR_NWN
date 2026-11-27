namespace SWLOR.Game.Server.Service.PropertyService
{
    /// <summary>
    /// Rules about who may hand out a property permission, shared by the permission window and its tests.
    /// </summary>
    public static class PropertyPermissionRules
    {
        /// <summary>
        /// Whether the right to grant the permission is held only by the owner and can never be given. Manage Edicts shows
        /// in the permission window, but only the mayor delegates it, so a delegate who holds it cannot pass it on.
        /// </summary>
        public static bool IsGrantRightOwnerOnly(PropertyPermissionType type)
        {
            return type == PropertyPermissionType.ManageEdicts;
        }
    }
}
