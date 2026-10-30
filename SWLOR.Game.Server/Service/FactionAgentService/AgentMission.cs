using System;
using SWLOR.Game.Server.Service.FactionService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Service.FactionAgentService
{
    /// <summary>
    /// A faction agent mission (plan section 29): an offer on an agent's board, and once accepted the mission the player
    /// holds on their record. Its id is also the id of its runtime quest. The terms are kept with the held mission, so a
    /// tuning change never alters a mission already taken.
    /// </summary>
    public class AgentMission
    {
        /// <summary>The offer id: agent, board day and slot (<see cref="FactionAgentRules.OfferId"/>).</summary>
        public string Id { get; set; }

        public FactionAgentType Agent { get; set; }
        public FactionType Faction { get; set; }
        public AgentMissionSlot Slot { get; set; }
        public AgentMissionType Type { get; set; }
        public int Tier { get; set; }

        /// <summary>The standing with the agent's faction the offer needs (0 for none).</summary>
        public int MinimumStanding { get; set; }

        /// <summary>The space system the mission is flown in; empty for a courier run or a mining quota.</summary>
        public string OrbitResref { get; set; } = string.Empty;

        /// <summary>Starlane jumps from the agent's dock: to the mission's system, or a courier run's destination.</summary>
        public int Hops { get; set; }

        /// <summary>A kill mission's target role; Invalid for any ship of the tier.</summary>
        public ShipNpcRole Role { get; set; }

        /// <summary>Ships to destroy or ore to deliver; 1 for every other mission.</summary>
        public int Quantity { get; set; } = 1;

        /// <summary>A mining quota's ore.</summary>
        public string ItemResref { get; set; } = string.Empty;

        /// <summary>A courier run's destination exchange.</summary>
        public TradeExchangeType Destination { get; set; }

        /// <summary>A scan-down's site type (a data heist's is always a data site).</summary>
        public SignatureType Site { get; set; }

        /// <summary>A courier run's sealed volume.</summary>
        public int Volume { get; set; }

        /// <summary>A courier run's time to deliver, from accepting.</summary>
        public int DeadlineMinutes { get; set; }

        public int Credits { get; set; }
        public int Standing { get; set; }
        public int Tokens { get; set; }

        /// <summary>When the player accepted it; null on an offer.</summary>
        public DateTime? AcceptedAt { get; set; }

        /// <summary>The ship carrying a courier run's sealed load.</summary>
        public string ShipId { get; set; } = string.Empty;

        /// <summary>
        /// Whether the completed mission's terms were paid. A mission whose quest completed but was not paid (a restart
        /// in between) is paid at the next login.
        /// </summary>
        public bool Paid { get; set; }

        /// <summary>Whether the standing part of the pay was saved; a pay retried after a restart skips it.</summary>
        public bool PaidStanding { get; set; }

        /// <summary>Whether the token part of the pay was saved; a pay retried after a restart skips it.</summary>
        public bool PaidTokens { get; set; }

        /// <summary>Whether the credit part of the pay was saved with the character; a pay retried after a restart skips it.</summary>
        public bool PaidCredits { get; set; }

        public AgentMission Copy()
        {
            return (AgentMission)MemberwiseClone();
        }
    }
}
