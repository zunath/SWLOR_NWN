namespace SWLOR.Game.Server.Service.MasteryService
{
    /// <summary>
    /// A single business-rule check result produced by MasteryRules.ValidateRequest.
    /// Every rule except OffLimit is a warning: staff may approve the request anyway,
    /// but the orchestration layer should require an override reason when doing so.
    /// </summary>
    public class MasteryRuleViolation
    {
        public MasteryRuleType RuleType { get; set; }
        public string Message { get; set; }
        public bool IsBlocking { get; set; }

        public MasteryRuleViolation(MasteryRuleType ruleType, string message, bool isBlocking)
        {
            RuleType = ruleType;
            Message = message;
            IsBlocking = isBlocking;
        }
    }
}
