namespace SWLOR.Toolset.Domain.Editors.Triggers
{
    /// <summary>Where a trigger behavior keeps one of its values.</summary>
    public enum TriggerFieldStorage
    {
        /// <summary>A field on the trigger struct itself (blueprint root, or the instance struct).</summary>
        Field,

        /// <summary>An entry in the trigger's VarTable — the SWLOR side of most behaviors.</summary>
        Local
    }
}
