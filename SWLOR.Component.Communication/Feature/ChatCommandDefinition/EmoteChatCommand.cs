using SWLOR.Component.Communication.Service;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Common.Enums;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Communication.ValueObjects;

namespace SWLOR.Component.Communication.Feature.ChatCommandDefinition
{
    public class EmoteChatCommand: IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("bored")
                .Description("Plays a bored animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetPauseBored)
                .IsEmote();
            builder.Create("bow")
                .Description("Plays a bow animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetBow)
                .IsEmote(); 
            builder.Create("cower")
                .Description("Plays a cower animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.HoldHead)
                .IsEmote();
            builder.Create("crossarms")
                .Description("Plays a cross arms animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.CrossArms)
                .IsEmote(); 
            builder.Create("crouch")
                .Description("Plays a crouch animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Crouch)
                .IsEmote(); 
            builder.Create("deadback")
                .Description("Plays a dead back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingDeadBack)
                .IsEmote();
            builder.Create("deadfront")
                .Description("Plays a dead front animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingDeadFront)
                .IsEmote();
            builder.Create("drink")
                .Description("Plays a drinking animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.FireForgetDrink)
                .IsEmote();
            builder.Create("drunk")
                .Description("Plays a drunk animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingPauseDrunk)
                .IsEmote();
            builder.Create("duck")
                .Description("Plays a duck animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetDodgeDuck)
                .IsEmote();
            builder.Create("greet")
                .Description("Plays a greet animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetGreeting)
                .IsEmote();
            builder.Create("interact")
                .Description("Plays an interact animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingGetMid)
                .IsEmote();
            builder.Create("meditate")
                .Description("Plays a meditate animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingMeditate)
                .IsEmote();
            builder.Create("laughing")
                .Description("Plays a laughing animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingTalkLaughing)
                .IsEmote();
            builder.Create("listen")
                .Description("Plays a listen animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingListen)
                .IsEmote();
            builder.Create("look")
                .Description("Plays a look far animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingLookFar)
                .IsEmote();
            builder.Create("pickup")
                .Description("Plays a pickup animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingGetLow)
                .IsEmote();
            builder.Create("point")
                .Description("Plays a point animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.PointForward)
                .IsEmote();
            builder.Create("read")
                .Description("Plays a read animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetRead)
                .IsEmote();
            builder.Create("salute")
                .Description("Plays a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetSalute)
                .IsEmote();
            builder.Create("scratchhead")
                .Description("Plays a scratch head animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetPauseScratchHead)
                .IsEmote();
            builder.Create("sidestep")
                .Description("Plays a side-step animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetDodgeSide)
                .IsEmote();
            builder.Create("sit")
                .Description("Makes your character sit down.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingSitCross)
                .IsEmote();
            builder.Create("spasm")
                .Description("Plays a spasm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingSpasm)
                .IsEmote();
            builder.Create("taunt")
                .Description("Plays a taunt animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetTaunt)
                .IsEmote();
            builder.Create("tired")
                .Description("Plays a tired animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LoopingPauseTired)
                .IsEmote();
            builder.Create("victory1")
                .Description("Plays a victory 1 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetVictory1)
                .IsEmote();
            builder.Create("victory2")
                .Description("Plays a victory 2 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetVictory2)
                .IsEmote();
            builder.Create("victory3")
                .Description("Plays a victory 3 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.FireForgetVictory3)
                .IsEmote();

            builder.Create("think")
                .Description("Plays a think animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.ThinkingMan)
                .IsEmote();
            builder.Create("jumpforward")
                .Description("Plays a jump animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.JumpForward)
                .IsEmote();
            builder.Create("followme")
                .Description("Plays a follow-me animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.FollowMe)
                .IsEmote();
            builder.Create("hangbyhands")
                .Description("Plays a hanging by hands animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.HangByHands)
                .IsEmote();
            builder.Create("dig")
                .Description("Plays a dig animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Dig);
            builder.Create("layonside")
                .Description("Plays a lay on side animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LayOnSide)
                .IsEmote();
            builder.Create("kneel")
                .Description("Plays a kneel animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Kneel)
                .IsEmote();
            builder.Create("layback")
                .Description("Plays a laying animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LayBackHandsOnStomach)
                .IsEmote();
            builder.Create("laysitup")
                .Description("Plays a lay on back partially upright animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LayOnBackUpright)
                .IsEmote();
            builder.Create("praystanding")
                .Description("Plays a praying animation standing up.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.PrayStanding)
                .IsEmote();
            builder.Create("ypose")
                .Description("Hold arms up in a Y shape.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.YPose)
                .IsEmote();
            builder.Create("disagree")
                .Description("Plays a disagree animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Disagree)
                .IsEmote();
            builder.Create("pushup")
                .Description("Play a push up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.PushUp)
                .IsEmote();
            builder.Create("lounge")
                .Description("Play a lounge animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LayBackHandsBehindHead)
                .IsEmote();
            builder.Create("situp")
                .Description("Play a sit-up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LayBackWithHandsBehindHeadFeetUp)
                .IsEmote();
            builder.Create("jumpingjacks")
                .Description("Play a jumping jack animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(AnimationType.JumpingJacks)
                .IsEmote();
            builder.Create("squat")
                .Description("Play a squat animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Squat)
                .IsEmote();
            builder.Create("clap")
                .Description("Play a clap animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Clap)
                .IsEmote();
            builder.Create("salute")
                .Description("Play a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Salute)
                .IsEmote();
            builder.Create("facepalm")
                .Description("Play a facepalm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Facepalm)
                .IsEmote();
            builder.Create("wallfoot")
                .Description("Play a lean back on wall, foot up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LeanBackOnWallFootUp)
                .IsEmote();
            builder.Create("prisoner")
                .Description("Play a prisoner with hands behind back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Prisoner)
                .IsEmote();
            builder.Create("flex")
                .Description("Play a flex animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Flex)
                .IsEmote();
            builder.Create("dejectedkneel")
                .Description("Kneel dejectedly.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.DejectedKneel)
                .IsEmote();
            builder.Create("jedihandsonback")
                .Description("Place hands behind back like a Jedi.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.JediHandsBehindBack)
                .IsEmote();
            builder.Create("laydownfaceforward")
                .Description("Lay down, face forward.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.LayDownFaceForward)
                .IsEmote();
            builder.Create("shrug")
                .Description("Shrug")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(AnimationType.Shrug)
                .IsEmote();
            builder.Create("kneeup")
               .Description("Play a knee up animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.KneeUp)
               .IsEmote();
            builder.Create("fetal")
               .Description("Play a fetal position animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.HoldHeadLow)
               .IsEmote();
            builder.Create("layheadonside")
               .Description("Lay to the side.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.LayToTheSide)
               .IsEmote();
            builder.Create("sitspread")
               .Description("Sit, legs spread.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.SitLegsSpread)
               .IsEmote();
            builder.Create("sitcrossed")
               .Description("Sit, legs crossed.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.SitLegsCrossed)
               .IsEmote();
            builder.Create("layback")
               .Description("Lay partially back, hands on stomach.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.LayPartiallyBackHandsOnStomach)
               .IsEmote();
            builder.Create("cheerloud")
               .Description("Cheer loudly.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.CheerLoudly)
               .IsEmote();
            builder.Create("shieldwall")
               .Description("Take a shield stance.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.ShieldWall)
               .IsEmote();
            builder.Create("dancehandsup")
               .Description("Dance with your hands held high.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.DanceHandsUp)
               .IsEmote();
            builder.Create("smoke")
               .Description("Smoke it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.Smoke)
               .IsEmote();
            builder.Create("drink")
               .Description("Drink it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.Drink)
               .IsEmote();
            builder.Create("kiss")
               .Description("Play a male/female kiss dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.MaleFemaleKiss)
               .IsEmote();
            builder.Create("hug")
               .Description("Play a male/female hug dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.MaleFemaleHug)
               .IsEmote();
            builder.Create("waltz")
               .Description("Play a male/female waltz dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.MaleFemaleWaltz)
               .IsEmote();
            builder.Create("push")
               .Description("Play a push animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.Push)
               .IsEmote();
            builder.Create("paraderest")
               .Description("Play a parade rest animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.ParadeRest)
               .IsEmote();
            builder.Create("bootdance")
               .Description("Play a boot dance animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.BootDance)
               .IsEmote();
            builder.Create("playflute")
               .Description("Play a flute animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.PlayFlute)
               .IsEmote();
            builder.Create("playguitar")
               .Description("Play a guitar animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.PlayGuitar)
               .IsEmote();
            builder.Create("pointpistol")
               .Description("Point your pistol.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(AnimationType.PointPistol)
               .IsEmote();
            builder.Create("doublelsstance")
              .Description("Hold your lightsaber behind you.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(AnimationType.DoubleLSStance)
              .IsEmote();
            builder.Create("classicjedistance")
              .Description("Get in the classic Jedi stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(AnimationType.ClassicJediStance)
              .IsEmote();
            builder.Create("onehandedstance")
              .Description("One handed melee stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(AnimationType.OneHandedStance)
              .IsEmote();
            builder.Create("dualwieldingstance")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(AnimationType.DualWieldingStance)
              .IsEmote();
            builder.Create("dualwieldingstance2")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(AnimationType.DualWieldingStance2)
              .IsEmote();

            return builder.Build();
        }
    }
}
