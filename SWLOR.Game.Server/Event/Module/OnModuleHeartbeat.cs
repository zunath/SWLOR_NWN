using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Event.Module
{
    internal class OnModuleHeartbeat : IRegisteredEvent
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly IItemService _item;
        private readonly IAbilityService _ability;
        private readonly IPerkService _perk;
        private readonly IBaseService _base;
        private readonly IPlayerStatService _playerStat;
        private readonly IChatTextService _chatText;
        
        public OnModuleHeartbeat(INWScript script,
            IDataContext db,
            IItemService item,
            IAbilityService ability,
            IPerkService perk,
            IBaseService @base,
            IPlayerStatService playerStat,
            IChatTextService chatText)
        {
            _ = script;
            _db = db;
            _item = item;
            _ability = ability;
            _perk = perk;
            _base = @base;
            _playerStat = playerStat;
            _chatText = chatText;
        }

        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetFirstPC());

            while(player.IsValid)
            {
                if (!player.IsDM)
                {
                    PlayerCharacter entity = _db.PlayerCharacters.SingleOrDefault(x => x.PlayerID == player.GlobalID);

                    if (entity != null)
                    {
                        HandleRegenerationTick(player, entity);
                        HandleFPRegenerationTick(player, entity);

                        _db.SaveChanges();
                    }
                }

                player = (_.GetNextPC());
            }

            SaveCharacters();
            _item.OnModuleHeartbeat();
            _base.OnModuleHeartbeat();
            _chatText.OnModuleHeartbeat();

            return true;
        }
        
        // Export all characters every minute.
        private void SaveCharacters()
        {
            int currentTick = NWModule.Get().GetLocalInt("SAVE_CHARACTERS_TICK") + 1;

            if (currentTick >= 10)
            {
                _.ExportAllCharacters();
                currentTick = 0;
            }

            NWModule.Get().SetLocalInt("SAVE_CHARACTERS_TICK", currentTick);
        }

        private void HandleRegenerationTick(NWPlayer oPC, PlayerCharacter entity)
        {
            entity.RegenerationTick = entity.RegenerationTick - 1;
            int rate = 20;
            int amount = entity.HPRegenerationAmount;

            if (entity.RegenerationTick <= 0)
            {
                if (oPC.CurrentHP < oPC.MaxHP)
                {
                    // CON bonus
                    int con = oPC.ConstitutionModifier;
                    if (con > 0)
                    {
                        amount += con;
                    }
                    amount += _playerStat.EffectiveHPRegenBonus(oPC);
                    
                    if (oPC.Chest.CustomItemType == CustomItemType.HeavyArmor)
                    {
                        int sturdinessLevel = _perk.GetPCPerkLevel(oPC, PerkType.Sturdiness);
                        if (sturdinessLevel > 0)
                        {
                            amount += sturdinessLevel + 1;
                        }
                    }
                    _.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, _.EffectHeal(amount), oPC.Object);
                }

                entity.RegenerationTick = rate;
            }
        }

        private void HandleFPRegenerationTick(NWPlayer oPC, PlayerCharacter entity)
        {
            entity.CurrentFPTick = entity.CurrentFPTick - 1;
            int rate = 20;
            int amount = 1;

            if (entity.CurrentFPTick <= 0)
            {
                if (entity.CurrentFP < entity.MaxFP)
                {
                    // CHA bonus
                    int cha = oPC.CharismaModifier;
                    if (cha > 0)
                    {
                        amount += cha;
                    }
                    amount += _playerStat.EffectiveFPRegenBonus(oPC);

                    if (oPC.Chest.CustomItemType == CustomItemType.ForceArmor)
                    {
                        int clarityLevel = _perk.GetPCPerkLevel(oPC, PerkType.Clarity);
                        if (clarityLevel > 0)
                        {
                            amount += clarityLevel + 1;
                        }
                    }

                    entity = _ability.RestoreFP(oPC, amount, entity);
                }

                entity.CurrentFPTick = rate;
            }
        }

    }
}
