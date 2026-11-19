using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.GuiDefinition.DroidAssemblyWindow;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DroidAssemblyViewModel : GuiViewModelBase<DroidAssemblyViewModel, GuiPayloadBase>, IDroidAssemblyHost
    {
        private DroidAssemblyActionService DroidAssemblyActionService { get; }

        public DroidAssemblyViewModel(DroidAssemblyActionService droidAssemblyActionService)
        {
            DroidAssemblyActionService = droidAssemblyActionService;
        }

        private readonly DroidAssemblySession _session = new();

        public string Error
        {
            get => Get<string>();
            set => Set(value);
        }

        public bool ProcessNotStarted
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsBuildInProgress
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsCPUSelected
        {
            get => Get<bool>();
            set => Set(value);
        }

        public string Name
        {
            get => Get<string>();
            set => Set(value);
        }

        public int PersonalityIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public string Tier
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Level
        {
            get => Get<string>();
            set => Set(value);
        }

        public string AISlots
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HP
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Stamina
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Might
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Perception
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Vitality
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Willpower
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Agility
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Social
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Vibroblade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Vibroknife
        {
            get => Get<string>();
            set => Set(value);
        }

        public string HeavyVibroblade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Spear
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TwinBlade
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Katar
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Staff
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Pistol
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Rifle
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Throwing
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Unarmed
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TwinPistol
        {
            get => Get<string>();
            set => Set(value);
        }

        public string Armor
        {
            get => Get<string>();
            set => Set(value);
        }

        public string FireResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string PoisonResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string ElectricalResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string IceResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MindResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string MobilityResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string TraumaResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string DisruptionResistance
        {
            get => Get<string>();
            set => Set(value);
        }

        public string CPUResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string HeadResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string BodyResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string ArmsResref
        {
            get => Get<string>();
            set => Set(value);
        }
        public string LegsResref
        {
            get => Get<string>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            DroidAssemblyActionService.Reset(this);

            WatchOnClient(model => model.Name);
            WatchOnClient(model => model.PersonalityIndex);
        }

        /// <summary>Runs whenever the window closes, whether the player closes it or the server does.</summary>
        public override Action OnWindowClosed() => () =>
        {
            if (!DroidAssemblyActionService.Reset(this))
                SendMessageToPC(Player, ItemService.ItemsHeldMessage);
        };

        public Action OnClickCPU() => () => DroidAssemblyActionService.OnClickCPU(this);

        public Action OnClickHead() => () => DroidAssemblyActionService.OnClickHead(this);

        public Action OnClickBody() => () => DroidAssemblyActionService.OnClickBody(this);

        public Action OnClickArms() => () => DroidAssemblyActionService.OnClickArms(this);

        public Action OnClickLegs() => () => DroidAssemblyActionService.OnClickLegs(this);

        public Action OnClickNewDroid() => () => DroidAssemblyActionService.OnClickNewDroid(this);

        public Action OnClickReset() => () => DroidAssemblyActionService.OnClickReset(this);

        public Action OnClickConstruct() => () => DroidAssemblyActionService.OnClickConstruct(this);

        DroidAssemblySession IDroidAssemblyHost.Session => _session;
        uint IDroidAssemblyHost.Player => Player;
        void IDroidAssemblyHost.ShowModal(string prompt, Action confirmAction) => ShowModal(prompt, confirmAction);
        string IDroidAssemblyHost.Error { get => Error; set => Error = value; }
        string IDroidAssemblyHost.Name { get => Name; set => Name = value; }
        int IDroidAssemblyHost.PersonalityIndex { get => PersonalityIndex; set => PersonalityIndex = value; }
        bool IDroidAssemblyHost.ProcessNotStarted { get => ProcessNotStarted; set => ProcessNotStarted = value; }
        bool IDroidAssemblyHost.IsBuildInProgress { get => IsBuildInProgress; set => IsBuildInProgress = value; }
        bool IDroidAssemblyHost.IsCPUSelected { get => IsCPUSelected; set => IsCPUSelected = value; }
        string IDroidAssemblyHost.Tier { get => Tier; set => Tier = value; }
        string IDroidAssemblyHost.Level { get => Level; set => Level = value; }
        string IDroidAssemblyHost.AISlots { get => AISlots; set => AISlots = value; }
        string IDroidAssemblyHost.HP { get => HP; set => HP = value; }
        string IDroidAssemblyHost.Stamina { get => Stamina; set => Stamina = value; }
        string IDroidAssemblyHost.Might { get => Might; set => Might = value; }
        string IDroidAssemblyHost.Perception { get => Perception; set => Perception = value; }
        string IDroidAssemblyHost.Vitality { get => Vitality; set => Vitality = value; }
        string IDroidAssemblyHost.Willpower { get => Willpower; set => Willpower = value; }
        string IDroidAssemblyHost.Agility { get => Agility; set => Agility = value; }
        string IDroidAssemblyHost.Social { get => Social; set => Social = value; }
        string IDroidAssemblyHost.Vibroblade { get => Vibroblade; set => Vibroblade = value; }
        string IDroidAssemblyHost.Vibroknife { get => Vibroknife; set => Vibroknife = value; }
        string IDroidAssemblyHost.HeavyVibroblade { get => HeavyVibroblade; set => HeavyVibroblade = value; }
        string IDroidAssemblyHost.Spear { get => Spear; set => Spear = value; }
        string IDroidAssemblyHost.TwinBlade { get => TwinBlade; set => TwinBlade = value; }
        string IDroidAssemblyHost.Katar { get => Katar; set => Katar = value; }
        string IDroidAssemblyHost.Staff { get => Staff; set => Staff = value; }
        string IDroidAssemblyHost.Pistol { get => Pistol; set => Pistol = value; }
        string IDroidAssemblyHost.Rifle { get => Rifle; set => Rifle = value; }
        string IDroidAssemblyHost.Throwing { get => Throwing; set => Throwing = value; }
        string IDroidAssemblyHost.Unarmed { get => Unarmed; set => Unarmed = value; }
        string IDroidAssemblyHost.TwinPistol { get => TwinPistol; set => TwinPistol = value; }
        string IDroidAssemblyHost.Armor { get => Armor; set => Armor = value; }
        string IDroidAssemblyHost.FireResistance { get => FireResistance; set => FireResistance = value; }
        string IDroidAssemblyHost.PoisonResistance { get => PoisonResistance; set => PoisonResistance = value; }
        string IDroidAssemblyHost.ElectricalResistance { get => ElectricalResistance; set => ElectricalResistance = value; }
        string IDroidAssemblyHost.IceResistance { get => IceResistance; set => IceResistance = value; }
        string IDroidAssemblyHost.MindResistance { get => MindResistance; set => MindResistance = value; }
        string IDroidAssemblyHost.MobilityResistance { get => MobilityResistance; set => MobilityResistance = value; }
        string IDroidAssemblyHost.TraumaResistance { get => TraumaResistance; set => TraumaResistance = value; }
        string IDroidAssemblyHost.DisruptionResistance { get => DisruptionResistance; set => DisruptionResistance = value; }
        string IDroidAssemblyHost.CPUResref { get => CPUResref; set => CPUResref = value; }
        string IDroidAssemblyHost.HeadResref { get => HeadResref; set => HeadResref = value; }
        string IDroidAssemblyHost.BodyResref { get => BodyResref; set => BodyResref = value; }
        string IDroidAssemblyHost.ArmsResref { get => ArmsResref; set => ArmsResref = value; }
        string IDroidAssemblyHost.LegsResref { get => LegsResref; set => LegsResref = value; }
    }
}
