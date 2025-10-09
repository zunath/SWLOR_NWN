using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Events.Events.Player;

namespace SWLOR.Component.Character.Service
{
    internal class CharacterResourceService: ICharacterResourceService
    {
        private const string FPLocalVar = "FP";
        private const string StaminaLocalVar = "STAMINA";
        
        private readonly IPlayerRepository _playerRepository;
        private readonly IStatCalculationService _statCalculationService;
        private readonly IEventAggregator _eventAggregator;

        public CharacterResourceService(
            IPlayerRepository playerRepository,
            IStatCalculationService statCalculationService,
            IEventAggregator eventAggregator)
        {
            _playerRepository = playerRepository;
            _statCalculationService = statCalculationService;
            _eventAggregator = eventAggregator;
        }
        public void RestoreHP(uint creature, int amount)
        {
            var effect = EffectHeal(amount);
            ApplyEffectToObject(DurationType.Instant, effect, creature);
        }

        public void RestoreFP(uint creature, int amount)
        {
            if (amount <= 0) return;

            var maxFP = _statCalculationService.CalculateMaxFP(creature);
            
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepository.GetById(playerId);
                
                dbPlayer.FP += amount;

                if (dbPlayer.FP > maxFP)
                    dbPlayer.FP = maxFP;
                
                _playerRepository.Save(dbPlayer);
            }
            // NPCs
            else
            {
                var fp = GetLocalInt(creature, FPLocalVar);
                fp += amount;

                if (fp > maxFP)
                    fp = maxFP;

                SetLocalInt(creature, FPLocalVar, fp);
            }

            _eventAggregator.Publish(new OnPlayerFPAdjusted(), creature);
        }

        public void RestoreSTM(uint creature, int amount)
        {
            if (amount <= 0) return;

            var maxSTM = _statCalculationService.CalculateMaxSTM(creature);

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepository.GetById(playerId);

                dbPlayer.Stamina += amount;

                if (dbPlayer.Stamina > maxSTM)
                    dbPlayer.Stamina = maxSTM;

                _playerRepository.Save(dbPlayer);
            }
            // NPCs
            else
            {
                var stamina = GetLocalInt(creature, StaminaLocalVar);
                stamina += amount;

                if (stamina > maxSTM)
                    stamina = maxSTM;

                SetLocalInt(creature, StaminaLocalVar, stamina);
            }

            _eventAggregator.Publish(new OnPlayerStaminaAdjusted(), creature);
        }

        public void ReduceFP(uint creature, int reduceBy, Player dbPlayer = null)
        {
            if (reduceBy <= 0) return;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = _playerRepository.GetById(playerId);
                }

                dbPlayer.FP -= reduceBy;

                if (dbPlayer.FP < 0)
                    dbPlayer.FP = 0;

                _playerRepository.Save(dbPlayer);
            }
            else
            {
                var fp = GetLocalInt(creature, FPLocalVar);
                fp -= reduceBy;
                if (fp < 0)
                    fp = 0;

                SetLocalInt(creature, FPLocalVar, fp);
            }

            _eventAggregator.Publish(new OnPlayerFPAdjusted(), creature);
        }

        public void ReduceStamina(uint creature, int reduceBy, Player dbPlayer = null)
        {
            if (reduceBy <= 0) return;

            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                if (dbPlayer == null)
                {
                    dbPlayer = _playerRepository.GetById(playerId);
                }

                dbPlayer.Stamina -= reduceBy;

                if (dbPlayer.Stamina < 0)
                    dbPlayer.Stamina = 0;

                _playerRepository.Save(dbPlayer);
            }
            else
            {
                var stamina = GetLocalInt(creature, StaminaLocalVar);
                stamina -= reduceBy;
                if (stamina < 0)
                    stamina = 0;

                SetLocalInt(creature, StaminaLocalVar, stamina);
            }

            _eventAggregator.Publish(new OnPlayerStaminaAdjusted(), creature);
        }

        public void SetCurrentFP(uint creature, int amount)
        {
            var maxFP = _statCalculationService.CalculateMaxFP(creature);

            if (amount < 0) amount = 0;
            if (amount > maxFP) amount = maxFP;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepository.GetById(playerId);

                dbPlayer.FP = amount;

                _playerRepository.Save(dbPlayer);
            }
            // NPCs
            else
            {
                SetLocalInt(creature, FPLocalVar, amount);
            }

            _eventAggregator.Publish(new OnPlayerFPAdjusted(), creature);
        }

        public void SetCurrentSTM(uint creature, int amount)
        {
            var maxSTM = _statCalculationService.CalculateMaxSTM(creature);

            if (amount < 0) amount = 0;
            if (amount > maxSTM) amount = maxSTM;

            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepository.GetById(playerId);

                dbPlayer.Stamina = amount;

                _playerRepository.Save(dbPlayer);
            }
            // NPCs
            else
            {
                SetLocalInt(creature, StaminaLocalVar, amount);
            }

            _eventAggregator.Publish(new OnPlayerStaminaAdjusted(), creature);
        }

        public int GetCurrentHP(uint creature)
        {
            return GetCurrentHitPoints(creature);
        }

        public int GetCurrentFP(uint creature)
        {
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepository.GetById(playerId);
                return dbPlayer.FP;
            }
            // NPCs
            else
            {
                return GetLocalInt(creature, FPLocalVar);
            }
        }

        public int GetCurrentSTM(uint creature)
        {
            // Players
            if (GetIsPC(creature) && !GetIsDM(creature))
            {
                var playerId = GetObjectUUID(creature);
                var dbPlayer = _playerRepository.GetById(playerId);
                return dbPlayer.Stamina;
            }
            // NPCs
            else
            {
                return GetLocalInt(creature, StaminaLocalVar);
            }
        }

        public void NPCNaturalRegen()
        {
            var self = OBJECT_SELF;
            var maxFP = _statCalculationService.CalculateMaxFP(self);
            var maxSTM = _statCalculationService.CalculateMaxSTM(self);
            var fp = GetLocalInt(self, FPLocalVar) + 1;
            var stm = GetLocalInt(self, StaminaLocalVar) + 1;

            if (fp > maxFP)
                fp = maxFP;
            if (stm > maxSTM)
                stm = maxSTM;

            SetLocalInt(self, FPLocalVar, fp);
            SetLocalInt(self, StaminaLocalVar, stm);

            // If out of combat - restore HP at 10% per tick.
            if (!GetIsInCombat(self) &&
                GetCurrentHP(self) < GetMaxHitPoints(self))
            {
                var hpToHeal = GetMaxHitPoints(self) * 0.1f;
                ApplyEffectToObject(DurationType.Instant, EffectHeal((int)hpToHeal), self);
            }
        }
    }
}
