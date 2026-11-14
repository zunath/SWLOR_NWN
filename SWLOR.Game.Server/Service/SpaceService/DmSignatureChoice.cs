namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>One entry of the DM Tools signature picker: a label and the kind of hidden signature it places.</summary>
    public class DmSignatureChoice
    {
        public string Label { get; init; }
        public SignatureType Type { get; init; }
    }
}
