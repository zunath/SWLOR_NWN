using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    public sealed record SpaceXPReceipt(double ActiveClock, int BaseXP);
    public sealed class SpaceExperienceLedger
    {
        public DateTime LastWorkEnd { get; set; }
        public double ActiveClock { get; set; }
        public List<SpaceXPReceipt> HourlyReceipts { get; set; } = new();
        public string ExpeditionId { get; set; }
        public int ExpeditionIndustryXP { get; set; }
        public int ExpeditionSurveyXP { get; set; }

        public void Touch(DateTime start, DateTime end)
        {
            if (end < start) throw new ArgumentException("Expected a legitimate active interval.");
            var unionStart = LastWorkEnd > start ? LastWorkEnd : start;
            if (end > unionStart) ActiveClock += (end - unionStart).TotalSeconds;
            if (end > LastWorkEnd) LastWorkEnd = end;
            HourlyReceipts.RemoveAll(x => x.ActiveClock <= ActiveClock - 3600);
        }
        public int ClaimOperating(int requested)
        {
            if (requested < 0) throw new ArgumentOutOfRangeException(nameof(requested));
            HourlyReceipts.RemoveAll(x => x.ActiveClock <= ActiveClock - 3600);
            var allowed = Math.Max(0, Math.Min(requested, 18000 - HourlyReceipts.Sum(x => x.BaseXP)));
            if (allowed > 0) HourlyReceipts.Add(new(ActiveClock, allowed));
            return allowed;
        }

        public int Claim(string expedition, ShipModuleAction action, int requested, DateTime start, DateTime end)
        {
            if (string.IsNullOrWhiteSpace(expedition) || requested < 0 || end < start) throw new ArgumentException("Expected a completed finite work interval.");
            Touch(start, end);
            if (ExpeditionId != expedition)
            { ExpeditionId = expedition; ExpeditionIndustryXP = 0; ExpeditionSurveyXP = 0; }
            var survey = action == ShipModuleAction.Survey;
            var expeditionRemaining = survey ? 1200 - ExpeditionSurveyXP : 6000 - ExpeditionIndustryXP;
            var allowed = Math.Max(0, Math.Min(requested, Math.Min(expeditionRemaining, 18000 - HourlyReceipts.Sum(x => x.BaseXP))));
            if (allowed > 0)
            {
                HourlyReceipts.Add(new(ActiveClock, allowed));
                if (survey) ExpeditionSurveyXP += allowed; else ExpeditionIndustryXP += allowed;
            }
            return allowed;
        }
    }
}
