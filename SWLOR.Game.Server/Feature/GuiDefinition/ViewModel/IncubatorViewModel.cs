using System;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class IncubatorViewModel: GuiViewModelBase<IncubatorViewModel, GuiPayloadBase>
    {
        private const string _blank = "Blank";

        private string _dnaItem;
        private string _hydrolaseItem;
        private string _isomeraseItem;
        private string _lyaseItem;

        public string DNAItemResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HydrolaseItemResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string IsomeraseItemResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LyaseItemResref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsStartJobEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsErraticGeniusEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CurrentExperimentationStage
        {
            get => Get<string>();
            set => Set(value);
        }

        public string EstimatedTimeToCompletion
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsErraticGeniusChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string CurrentMutationChance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CurrentAttackPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentAccuracyPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentEvasionPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentLearningPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentPhysicalDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentForceDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentFireDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentPoisonDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentElectricalDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentIceDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentFortitudePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentReflexPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CurrentWillPurity
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CurrentXPPenalty
        {
            get => Get<string>();
            set => Set(value);
        }

        public string BonusAttackPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusAccuracyPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusEvasionPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusLearningPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusPhysicalDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusForceDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusFireDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusPoisonDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusElectricalDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusIceDefensePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusFortitudePurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusReflexPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BonusWillPurity
        {
            get => Get<string>();
            set => Set(value);
        }
        public string ExtraXPPenalty
        {
            get => Get<string>();
            set => Set(value);
        }

        public string BonusMutationChance
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            DNAItemResref = _blank;
            HydrolaseItemResref = _blank;
            IsomeraseItemResref = _blank;
            LyaseItemResref = _blank;

            WatchOnClient(model => model.IsErraticGeniusChecked);
        }

        public Action OnClickDNA() => () =>
        {

        };

        public Action OnClickHydrolase() => () =>
        {

        };

        public Action OnClickLyase() => () =>
        {

        };

        public Action OnClickIsomerase() => () =>
        {

        };

        public Action OnClickStartJob() => () =>
        {

        };

    }
}
