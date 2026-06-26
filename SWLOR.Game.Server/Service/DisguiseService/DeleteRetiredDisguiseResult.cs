namespace SWLOR.Game.Server.Service.DisguiseService
{
    public enum DeleteRetiredDisguiseResult
    {
        Success = 0,
        NotFound = 1,
        NotOwner = 2,
        NotRetired = 3,
        IsActive = 4,
        InsufficientCredits = 5,
        InsufficientRoleplayXP = 6,
        InvalidPaymentMethod = 7
    }
}
