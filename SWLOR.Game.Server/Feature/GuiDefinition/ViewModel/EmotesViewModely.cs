using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class EmotesViewModel: GuiViewModelBase<EmotesViewModel, GuiPayloadBase>
    {

        public GuiBindingList<string> EmoteNames
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<string> EmoteDescriptions
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        public GuiBindingList<int> EmoteAnimations
        {
            get => Get<GuiBindingList<int>>();
            set => Set(value);
        }
        public GuiBindingList<bool> IsEmoteLoopingAnimations
        {
            get => Get<GuiBindingList<bool>>();
            set => Set(value);
        }

        public int SelectedEmoteIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            var emoteNames = new GuiBindingList<string>();
            var emoteDescriptions = new GuiBindingList<string>();
            var emoteAnimations = new GuiBindingList<int>();
            var isEmoteLoopingAnimations = new GuiBindingList<bool>();

            SelectedEmoteIndex = -1;

            emoteNames.Add("bored");
            emoteDescriptions.Add("Plays a bored animation.");
            emoteAnimations.Add((int) Animation.FireForgetPauseBored);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("bow");
            emoteDescriptions.Add("Plays a bow animation.");
            emoteAnimations.Add((int) Animation.FireForgetBow);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("cower");
            emoteDescriptions.Add("Plays a cower animation.");
            emoteAnimations.Add((int) Animation.HoldHead);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("crossarms");
            emoteDescriptions.Add("Plays a cross arms animation.");
            emoteAnimations.Add((int) Animation.CrossArms);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("crouch");
            emoteDescriptions.Add("Plays a crouch animation.");
            emoteAnimations.Add((int) Animation.Crouch);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("deadback");
            emoteDescriptions.Add("Plays a dead back animation.");
            emoteAnimations.Add((int) Animation.LoopingDeadBack);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("deadfront");
            emoteDescriptions.Add("Plays a dead front animation.");
            emoteAnimations.Add((int) Animation.LoopingDeadFront);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("drink");
            emoteDescriptions.Add("Plays a drinking animation.");
            emoteAnimations.Add((int) Animation.FireForgetDrink);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("drunk");
            emoteDescriptions.Add("Plays a drunk animation.");
            emoteAnimations.Add((int) Animation.LoopingPauseDrunk);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("duck");
            emoteDescriptions.Add("Plays a duck animation.");
            emoteAnimations.Add((int) Animation.FireForgetDodgeDuck);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("greet");
            emoteDescriptions.Add("Plays a greet animation.");
            emoteAnimations.Add((int) Animation.FireForgetGreeting);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("interact");
            emoteDescriptions.Add("Plays an interact animation.");
            emoteAnimations.Add((int) Animation.LoopingGetMid);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("meditate");
            emoteDescriptions.Add("Plays a meditate animation.");
            emoteAnimations.Add((int) Animation.LoopingMeditate);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("laughing");
            emoteDescriptions.Add("Plays a laughing animation.");
            emoteAnimations.Add((int) Animation.LoopingTalkLaughing);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("listen");
            emoteDescriptions.Add("Plays a listen animation.");
            emoteAnimations.Add((int) Animation.LoopingListen);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("look");
            emoteDescriptions.Add("Plays a look far animation.");
            emoteAnimations.Add((int) Animation.LoopingLookFar);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("pickup");
            emoteDescriptions.Add("Plays a pickup animation.");
            emoteAnimations.Add((int) Animation.LoopingGetLow);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("point");
            emoteDescriptions.Add("Plays a point animation.");
            emoteAnimations.Add((int) Animation.PointForward);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("read");
            emoteDescriptions.Add("Plays a read animation.");
            emoteAnimations.Add((int) Animation.FireForgetRead);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("salute");
            emoteDescriptions.Add("Plays a salute animation.");
            emoteAnimations.Add((int) Animation.FireForgetSalute);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("scratchhead");
            emoteDescriptions.Add("Plays a scratch head animation.");
            emoteAnimations.Add((int) Animation.FireForgetPauseScratchHead);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("sidestep");
            emoteDescriptions.Add("Plays a side-step animation.");
            emoteAnimations.Add((int) Animation.FireForgetDodgeSide);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("sit");
            emoteDescriptions.Add("Makes your character sit down.");
            emoteAnimations.Add((int) Animation.LoopingSitCross);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("spasm");
            emoteDescriptions.Add("Plays a spasm animation.");
            emoteAnimations.Add((int) Animation.LoopingSpasm);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("taunt");
            emoteDescriptions.Add("Plays a taunt animation.");
            emoteAnimations.Add((int) Animation.FireForgetTaunt);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("tired");
            emoteDescriptions.Add("Plays a tired animation.");
            emoteAnimations.Add((int) Animation.LoopingPauseTired);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("victory1");
            emoteDescriptions.Add("Plays a victory 1 animation.");
            emoteAnimations.Add((int) Animation.FireForgetVictory1);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("victory2");
            emoteDescriptions.Add("Plays a victory 2 animation.");
            emoteAnimations.Add((int) Animation.FireForgetVictory2);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("victory3");
            emoteDescriptions.Add("Plays a victory 3 animation.");
            emoteAnimations.Add((int) Animation.FireForgetVictory3);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("think");
            emoteDescriptions.Add("Plays a think animation.");
            emoteAnimations.Add((int) Animation.ThinkingMan);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("jumpforward");
            emoteDescriptions.Add("Plays a jump animation.");
            emoteAnimations.Add((int) Animation.JumpForward);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("followme");
            emoteDescriptions.Add("Plays a follow-me animation.");
            emoteAnimations.Add((int) Animation.FollowMe);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("hangbyhands");
            emoteDescriptions.Add("Plays a hanging by hands animation.");
            emoteAnimations.Add((int) Animation.HangByHands);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("dig");
            emoteDescriptions.Add("Plays a dig animation.");
            emoteAnimations.Add((int) Animation.Dig); ;
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("layonside");
            emoteDescriptions.Add("Plays a lay on side animation.");
            emoteAnimations.Add((int) Animation.LayOnSide);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("kneel");
            emoteDescriptions.Add("Plays a kneel animation.");
            emoteAnimations.Add((int) Animation.Kneel);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("layback");
            emoteDescriptions.Add("Plays a laying animation.");
            emoteAnimations.Add((int) Animation.LayBackHandsOnStomach);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("laysitup");
            emoteDescriptions.Add("Plays a lay on back partially upright animation.");
            emoteAnimations.Add((int) Animation.LayOnBackUpright);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("praystanding");
            emoteDescriptions.Add("Plays a praying animation standing up.");
            emoteAnimations.Add((int) Animation.PrayStanding);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("ypose");
            emoteDescriptions.Add("Hold arms up in a Y shape.");
            emoteAnimations.Add((int) Animation.YPose);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("disagree");
            emoteDescriptions.Add("Plays a disagree animation.");
            emoteAnimations.Add((int) Animation.Disagree);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("pushup");
            emoteDescriptions.Add("Play a push up animation.");
            emoteAnimations.Add((int) Animation.PushUp);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("lounge");
            emoteDescriptions.Add("Play a lounge animation.");
            emoteAnimations.Add((int) Animation.LayBackHandsBehindHead);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("situp");
            emoteDescriptions.Add("Play a sit-up animation.");
            emoteAnimations.Add((int) Animation.LayBackWithHandsBehindHeadFeetUp);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("jumpingjacks");
            emoteDescriptions.Add("Play a jumping jack animation.");
            emoteAnimations.Add((int) Animation.JumpingJacks);
            isEmoteLoopingAnimations.Add(false);
            emoteNames.Add("squat");
            emoteDescriptions.Add("Play a squat animation.");
            emoteAnimations.Add((int) Animation.Squat);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("clap");
            emoteDescriptions.Add("Play a clap animation.");
            emoteAnimations.Add((int) Animation.Clap);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("salute");
            emoteDescriptions.Add("Play a salute animation.");
            emoteAnimations.Add((int) Animation.Salute);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("facepalm");
            emoteDescriptions.Add("Play a facepalm animation.");
            emoteAnimations.Add((int) Animation.Facepalm);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("wallfoot");
            emoteDescriptions.Add("Play a lean back on wall, foot up animation.");
            emoteAnimations.Add((int) Animation.LeanBackOnWallFootUp);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("prisoner");
            emoteDescriptions.Add("Play a prisoner with hands behind back animation.");
            emoteAnimations.Add((int) Animation.Prisoner);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("flex");
            emoteDescriptions.Add("Play a flex animation.");
            emoteAnimations.Add((int) Animation.Flex);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("dejectedkneel");
            emoteDescriptions.Add("Kneel dejectedly.");
            emoteAnimations.Add((int) Animation.DejectedKneel);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("jedihandsonback");
            emoteDescriptions.Add("Place hands behind back like a Jedi.");
            emoteAnimations.Add((int) Animation.JediHandsBehindBack);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("laydownfaceforward");
            emoteDescriptions.Add("Lay down, face forward.");
            emoteAnimations.Add((int) Animation.LayDownFaceForward);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("shrug");
            emoteDescriptions.Add("Shrug");
            emoteAnimations.Add((int) Animation.Shrug);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("kneeup");
            emoteDescriptions.Add("Play a knee up animation.");
            emoteAnimations.Add((int) Animation.KneeUp);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("fetal");
            emoteDescriptions.Add("Play a fetal position animation.");
            emoteAnimations.Add((int) Animation.HoldHeadLow);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("layheadonside");
            emoteDescriptions.Add("Lay to the side.");
            emoteAnimations.Add((int) Animation.LayToTheSide);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("sitspread");
            emoteDescriptions.Add("Sit, legs spread.");
            emoteAnimations.Add((int) Animation.SitLegsSpread);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("sitcrossed");
            emoteDescriptions.Add("Sit, legs crossed.");
            emoteAnimations.Add((int) Animation.SitLegsCrossed);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("layback");
            emoteDescriptions.Add("Lay partially back, hands on stomach.");
            emoteAnimations.Add((int) Animation.LayPartiallyBackHandsOnStomach);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("cheerloud");
            emoteDescriptions.Add("Cheer loudly.");
            emoteAnimations.Add((int) Animation.CheerLoudly);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("shieldwall");
            emoteDescriptions.Add("Take a shield stance.");
            emoteAnimations.Add((int) Animation.ShieldWall);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("dancehandsup");
            emoteDescriptions.Add("Dance with your hands held high.");
            emoteAnimations.Add((int) Animation.DanceHandsUp);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("smoke");
            emoteDescriptions.Add("Smoke it up.");
            emoteAnimations.Add((int) Animation.Smoke);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("drink");
            emoteDescriptions.Add("Drink it up.");
            emoteAnimations.Add((int) Animation.Drink);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("kiss");
            emoteDescriptions.Add("Play a male/female kiss dependent on gender.");
            emoteAnimations.Add((int) Animation.MaleFemaleKiss);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("hug");
            emoteDescriptions.Add("Play a male/female hug dependent on gender.");
            emoteAnimations.Add((int) Animation.MaleFemaleHug);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("waltz");
            emoteDescriptions.Add("Play a male/female waltz dependent on gender.");
            emoteAnimations.Add((int) Animation.MaleFemaleWaltz);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("push");
            emoteDescriptions.Add("Play a push animation.");
            emoteAnimations.Add((int) Animation.Push);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("paraderest");
            emoteDescriptions.Add("Play a parade rest animation.");
            emoteAnimations.Add((int) Animation.ParadeRest);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("bootdance");
            emoteDescriptions.Add("Play a boot dance animation.");
            emoteAnimations.Add((int) Animation.BootDance);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("playflute");
            emoteDescriptions.Add("Play a flute animation.");
            emoteAnimations.Add((int) Animation.PlayFlute);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("playguitar");
            emoteDescriptions.Add("Play a guitar animation.");
            emoteAnimations.Add((int) Animation.PlayGuitar);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("pointpistol");
            emoteDescriptions.Add("Point your pistol.");
            emoteAnimations.Add((int) Animation.PointPistol);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("doublelsstance");
            emoteDescriptions.Add("Hold your lightsaber behind you.");
            emoteAnimations.Add((int) Animation.DoubleLSStance);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("classicjedistance");
            emoteDescriptions.Add("Get in the classic Jedi stance.");
            emoteAnimations.Add((int) Animation.ClassicJediStance);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("onehandedstance");
            emoteDescriptions.Add("One handed melee stance.");
            emoteAnimations.Add((int) Animation.OneHandedStance);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("dualwieldingstance");
            emoteDescriptions.Add("Take a combat stance with two weapons.");
            emoteAnimations.Add((int) Animation.DualWieldingStance);
            isEmoteLoopingAnimations.Add(true);
            emoteNames.Add("dualwieldingstance2");
            emoteDescriptions.Add("Take a combat stance with two weapons.");
            emoteAnimations.Add((int) Animation.DualWieldingStance2);
            isEmoteLoopingAnimations.Add(true);

            EmoteNames = emoteNames;
            EmoteDescriptions = emoteDescriptions;
            EmoteAnimations = emoteAnimations;
            IsEmoteLoopingAnimations = isEmoteLoopingAnimations;
        }

        public Action OnSelectEmote() => () =>
        {
            var index = NuiGetEventArrayIndex();
            SelectedEmoteIndex = index;
            AssignCommand(Player, () => ClearAllActions());
            if (IsEmoteLoopingAnimations[SelectedEmoteIndex])
            {
                var duration = 9999.9f;
                AssignCommand(Player, () => ActionPlayAnimation((Animation) EmoteAnimations[SelectedEmoteIndex], 1f, duration));
            }
            else
            {
                AssignCommand(Player, () => ActionPlayAnimation((Animation) EmoteAnimations[SelectedEmoteIndex]));
            }

        };
    }
}
