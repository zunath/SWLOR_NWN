using System;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DroidAssemblyViewModel : GuiViewModelBase<DroidAssemblyViewModel, GuiPayloadBase>
    {
        public bool IsPartsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsDetailsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsInstructionsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }
        public bool IsGambitsSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public int HP
        {
            get => Get<int>();
            set => Set(value);
        }

        public int STM
        {
            get => Get<int>();
            set => Set(value);
        }

        public int MGT
        {
            get => Get<int>();
            set => Set(value);
        }

        public int PER
        {
            get => Get<int>();
            set => Set(value);
        }

        public int VIT
        {
            get => Get<int>();
            set => Set(value);
        }

        public int WIL
        {
            get => Get<int>();
            set => Set(value);
        }

        public int AGI
        {
            get => Get<int>();
            set => Set(value);
        }

        public int SOC
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Attack
        {
            get => Get<int>();
            set => Set(value);
        }

        public int Defense
        {
            get => Get<int>();
            set => Set(value);
        }
        public int ForceDefense
        {
            get => Get<int>();
            set => Set(value);
        }
        public int RecastReduction
        {
            get => Get<int>();
            set => Set(value);
        }
        public int InstructionSlots
        {
            get => Get<int>();
            set => Set(value);
        }
        public int GambitSlots
        {
            get => Get<int>();
            set => Set(value);
        }
        public int Tier
        {
            get => Get<int>();
            set => Set(value);
        }

        public string ChassisIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string CPUIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HeadIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LeftHandIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LeftArmIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LeftLegIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string RightHandIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string RightArmIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string RightLegIcon
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BodyIcon
        {
            get => Get<string>();
            set => Set(value);
        }


        public const string CombatPartsView = "CombatPartsView";
        public const string AstromechPartsView = "AstromechPartsView";
        public const string PartsSelectionView = "PartsSelectionView";
        public const string DetailsView = "DetailsView";
        public const string InstructionsView = "InstructionsView";
        public const string GambitsView = "GambitsView";

        public const string StatsView = "StatsView";

        public const string StatsPartialName = "StatsPartial";
        public const string ActivePartialName = "ActivePartial";
        public const string PartsPartialName = "PartsPartial";

        public const string BlankChassisIcon = "Blank";
        public const string BlankCPUIcon = "Blank";
        public const string BlankHeadIcon = "Blank";
        public const string BlankBodyIcon = "Blank";
        public const string BlankArmIcon = "Blank";
        public const string BlankLegIcon = "Blank";
        public const string BlankHandIcon = "Blank";

        private string _chassisItem;
        private string _cpuItem;
        private string _headItem;
        private string _bodyItem;
        private string _leftArmItem;
        private string _rightArmItem;
        private string _leftHandItem;
        private string _rightHandItem;
        private string _leftLegItem;
        private string _rightLegItem;

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ChangePartialView(ActivePartialName, PartsSelectionView);

            ClearItemsStored();
            IsPartsSelected = true;
            IsDetailsSelected = false;
            IsInstructionsSelected = false;
            IsGambitsSelected = false;
            Name = string.Empty;

            WatchOnClient(model => model.Name);
        }

        private void ClearItemsStored()
        {
            _chassisItem = string.Empty;
            _cpuItem = string.Empty;
            _headItem = string.Empty;
            _bodyItem = string.Empty;
            _leftArmItem = string.Empty;
            _rightArmItem = string.Empty;
            _leftHandItem = string.Empty;
            _rightHandItem = string.Empty;
            _leftLegItem = string.Empty;
            _rightLegItem = string.Empty;

            ChassisIcon = BlankChassisIcon;
            CPUIcon = BlankCPUIcon;
            HeadIcon = BlankHeadIcon;
            LeftHandIcon = BlankHandIcon;
            RightHandIcon = BlankHandIcon;
            LeftArmIcon = BlankArmIcon;
            RightArmIcon = BlankArmIcon;
            BodyIcon = BlankBodyIcon;
            LeftLegIcon = BlankLegIcon;
            RightLegIcon = BlankLegIcon;
        }

        public Action OnCloseWindow() => () =>
        {
            void ReturnItem(string serialized)
            {
                if (!string.IsNullOrWhiteSpace(serialized))
                {
                    var item = ObjectPlugin.Deserialize(serialized);
                    ObjectPlugin.AcquireItem(Player, item);
                }
            }

            ReturnItem(_chassisItem);
            ReturnItem(_cpuItem);
            ReturnItem(_headItem);
            ReturnItem(_bodyItem);
            ReturnItem(_leftArmItem);
            ReturnItem(_rightArmItem);
            ReturnItem(_leftHandItem);
            ReturnItem(_rightHandItem);
            ReturnItem(_leftLegItem);
            ReturnItem(_rightLegItem);

            ClearItemsStored();
        };

        public Action OnClickParts() => () =>
        {
            ChangePartialView(ActivePartialName, PartsSelectionView);
            IsPartsSelected = true;
            IsDetailsSelected = false;
            IsInstructionsSelected = false;
            IsGambitsSelected = false;
        };
        public Action OnClickDetails() => () =>
        {
            ChangePartialView(ActivePartialName, DetailsView);
            ChangePartialView(StatsPartialName, StatsView);
            IsPartsSelected = false;
            IsDetailsSelected = true;
            IsInstructionsSelected = false;
            IsGambitsSelected = false;
        };
        public Action OnClickInstructions() => () =>
        {
            ChangePartialView(ActivePartialName, InstructionsView);
            IsPartsSelected = false;
            IsDetailsSelected = false;
            IsInstructionsSelected = true;
            IsGambitsSelected = false;
        };
        public Action OnClickGambits() => () =>
        {
            ChangePartialView(ActivePartialName, GambitsView);
            IsPartsSelected = false;
            IsDetailsSelected = false;
            IsInstructionsSelected = false;
            IsGambitsSelected = true;
        };

        public Action OnClickChassis() => () =>
        {
            Targeting.EnterTargetingMode(Player, ObjectType.Item, "Select as Chassis item to use for this droid.", item =>
            {
                ChassisIcon = Item.GetIconResref(item);
                _chassisItem = ObjectPlugin.Serialize(item);
                DestroyObject(item);

                ChangePartialView(PartsPartialName, CombatPartsView);
            });
        };
        public Action OnClickCPU() => () =>
        {

        };

        public Action OnClickHead() => () =>
        {

        };
        public Action OnClickBody() => () =>
        {

        };
        public Action OnClickRightArm() => () =>
        {

        };
        public Action OnClickLeftArm() => () =>
        {

        };
        public Action OnClickRightLeg() => () =>
        {

        };
        public Action OnClickLeftLeg() => () =>
        {

        };
        public Action OnClickRightHand() => () =>
        {

        };
        public Action OnClickLeftHand() => () =>
        {

        };
    }
}
