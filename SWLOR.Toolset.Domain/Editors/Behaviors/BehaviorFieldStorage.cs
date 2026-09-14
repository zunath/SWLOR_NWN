namespace SWLOR.Toolset.Domain.Editors.Behaviors
{
    /// <summary>Where a behavior keeps one of its values.</summary>
    public enum BehaviorFieldStorage
    {
        /// <summary>A field on the object struct itself (blueprint root, or the instance struct).</summary>
        Field,

        /// <summary>An entry in the object's VarTable.</summary>
        Local
    }
}
