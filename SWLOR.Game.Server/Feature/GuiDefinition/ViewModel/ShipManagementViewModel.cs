using System;
using SWLOR.Game.Server.Service.GuiService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class ShipManagementViewModel: GuiViewModelBase<ShipManagementViewModel, GuiPayloadBase>
    {
        private int SelectedShipIndex { get; set; }

        public GuiBindingList<string> ShipNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<bool> ShipToggles
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public bool IsRegisterEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsUnregisterEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string ShipName
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ShipType
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Shields
        {
            get => Get<float>();
            set => Set(value);
        }

        public string ShieldsTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Hull
        {
            get => Get<float>();
            set => Set(value);
        }

        public string HullTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public float Capacitor
        {
            get => Get<float>();
            set => Set(value);
        }

        public string CapacitorTooltip
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ShieldRechargeRate
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool HighPower1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool HighPower8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string HighPower1Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower2Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower3Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower4Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower5Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower6Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower7Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HighPower8Resref
        {
            get => Get<string>();
            set => Set(value);
        }


        public bool LowPower1Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower2Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower3Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower4Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower5Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower6Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower7Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool LowPower8Visible
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string LowPower1Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower2Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower3Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower4Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower5Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower6Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower7Resref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LowPower8Resref
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool IsMakeActiveEnabled
        {
            get => Get<bool>();
            set => Set(value);
        }


        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            
        }

        private void LoadShip()
        {

        }

        public Action OnClickShip() => () =>
        {
            if (SelectedShipIndex > -1)
                ShipToggles[SelectedShipIndex] = false;

            var index = NuiGetEventArrayIndex();
            SelectedShipIndex = index;
            ShipToggles[index] = true;

            LoadShip();
        };

        public Action OnClickRegisterShip() => () =>
        {

        };

        public Action OnClickUnregisterShip() => () =>
        {

        };

        private void ProcessHighPower(int slot)
        {

        }


        public Action OnClickHighPower1() => () =>
        {
            ProcessHighPower(1);
        };
        public Action OnClickHighPower2() => () =>
        {
            ProcessHighPower(2);
        };
        public Action OnClickHighPower3() => () =>
        {
            ProcessHighPower(3);
        };
        public Action OnClickHighPower4() => () =>
        {
            ProcessHighPower(4);
        };
        public Action OnClickHighPower5() => () =>
        {
            ProcessHighPower(5);
        };
        public Action OnClickHighPower6() => () =>
        {
            ProcessHighPower(6);
        };
        public Action OnClickHighPower7() => () =>
        {
            ProcessHighPower(7);
        };
        public Action OnClickHighPower8() => () =>
        {
            ProcessHighPower(8);
        };

        private void ProcessLowPower(int slot)
        {

        }

        public Action OnClickLowPower1() => () =>
        {
            ProcessLowPower(1);
        };
        public Action OnClickLowPower2() => () =>
        {
            ProcessLowPower(2);
        };
        public Action OnClickLowPower3() => () =>
        {
            ProcessLowPower(3);
        };
        public Action OnClickLowPower4() => () =>
        {
            ProcessLowPower(4);
        };
        public Action OnClickLowPower5() => () =>
        {
            ProcessLowPower(5);
        };
        public Action OnClickLowPower6() => () =>
        {
            ProcessLowPower(6);
        };
        public Action OnClickLowPower7() => () =>
        {
            ProcessLowPower(7);
        };
        public Action OnClickLowPower8() => () =>
        {
            ProcessLowPower(8);
        };

        public Action OnClickMakeActive() => () =>
        {

        };
    }
}
