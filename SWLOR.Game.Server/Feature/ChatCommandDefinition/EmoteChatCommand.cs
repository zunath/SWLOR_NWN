using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ChatCommandDefinition
{
    public class EmoteChatCommand: IChatCommandListDefinition
    {
        public Dictionary<string, ChatCommandDetail> BuildChatCommands()
        {
            var builder = new ChatCommandBuilder();

            builder.Create("bored")
                .Description("Plays a bored animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetPauseBored)
                .IsEmote();
            builder.Create("bow")
                .Description("Plays a bow animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetBow)
                .IsEmote(); 
            builder.Create("cower")
                .Description("Plays a cower animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.HoldHead)
                .IsEmote();
            builder.Create("crossarms")
                .Description("Plays a cross arms animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.CrossArms)
                .IsEmote(); 
            builder.Create("crouch")
                .Description("Plays a crouch animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Crouch)
                .IsEmote(); 
            builder.Create("deadback")
                .Description("Plays a dead back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadBack)
                .IsEmote();
            builder.Create("deadfront")
                .Description("Plays a dead front animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadFront)
                .IsEmote();
            builder.Create("drink")
                .Description("Plays a drinking animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.FireForgetDrink)
                .IsEmote();
            builder.Create("drunk")
                .Description("Plays a drunk animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseDrunk)
                .IsEmote();
            builder.Create("duck")
                .Description("Plays a duck animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeDuck)
                .IsEmote();
            builder.Create("greet")
                .Description("Plays a greet animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetGreeting)
                .IsEmote();
            builder.Create("interact")
                .Description("Plays an interact animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetMid)
                .IsEmote();
            builder.Create("meditate")
                .Description("Plays a meditate animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingMeditate)
                .IsEmote();
            builder.Create("laughing")
                .Description("Plays a laughing animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingTalkLaughing)
                .IsEmote();
            builder.Create("listen")
                .Description("Plays a listen animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingListen)
                .IsEmote();
            builder.Create("look")
                .Description("Plays a look far animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingLookFar)
                .IsEmote();
            builder.Create("pickup")
                .Description("Plays a pickup animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetLow)
                .IsEmote();
            builder.Create("point")
                .Description("Plays a point animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PointForward)
                .IsEmote();
            builder.Create("read")
                .Description("Plays a read animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetRead)
                .IsEmote();
            builder.Create("salute")
                .Description("Plays a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetSalute)
                .IsEmote();
            builder.Create("scratchhead")
                .Description("Plays a scratch head animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetPauseScratchHead)
                .IsEmote();
            builder.Create("sidestep")
                .Description("Plays a side-step animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeSide)
                .IsEmote();
            builder.Create("sit")
                .Description("Makes your character sit down.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSitCross)
                .IsEmote();
            builder.Create("spasm")
                .Description("Plays a spasm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSpasm)
                .IsEmote();
            builder.Create("taunt")
                .Description("Plays a taunt animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetTaunt)
                .IsEmote();
            builder.Create("tired")
                .Description("Plays a tired animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseTired)
                .IsEmote();
            builder.Create("victory1")
                .Description("Plays a victory 1 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory1)
                .IsEmote();
            builder.Create("victory2")
                .Description("Plays a victory 2 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory2)
                .IsEmote();
            builder.Create("victory3")
                .Description("Plays a victory 3 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory3)
                .IsEmote();

            builder.Create("think")
                .Description("Plays a think animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.ThinkingMan)
                .IsEmote();
            builder.Create("jumpforward")
                .Description("Plays a jump animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.JumpForward)
                .IsEmote();
            builder.Create("followme")
                .Description("Plays a follow-me animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.FollowMe)
                .IsEmote();
            builder.Create("hangbyhands")
                .Description("Plays a hanging by hands animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.HangByHands)
                .IsEmote();
            builder.Create("dig")
                .Description("Plays a dig animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Dig);
            builder.Create("layonside")
                .Description("Plays a lay on side animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayOnSide)
                .IsEmote();
            builder.Create("kneel")
                .Description("Plays a kneel animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Kneel)
                .IsEmote();
            builder.Create("layback")
                .Description("Plays a laying animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackHandsOnStomach)
                .IsEmote();
            builder.Create("laysitup")
                .Description("Plays a lay on back partially upright animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayOnBackUpright)
                .IsEmote();
            builder.Create("praystanding")
                .Description("Plays a praying animation standing up.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PrayStanding)
                .IsEmote();
            builder.Create("ypose")
                .Description("Hold arms up in a Y shape.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.YPose)
                .IsEmote();
            builder.Create("disagree")
                .Description("Plays a disagree animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Disagree)
                .IsEmote();
            builder.Create("pushup")
                .Description("Play a push up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PushUp)
                .IsEmote();
            builder.Create("lounge")
                .Description("Play a lounge animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackHandsBehindHead)
                .IsEmote();
            builder.Create("situp")
                .Description("Play a sit-up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackWithHandsBehindHeadFeetUp)
                .IsEmote();
            builder.Create("jumpingjacks")
                .Description("Play a jumping jack animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.JumpingJacks)
                .IsEmote();
            builder.Create("squat")
                .Description("Play a squat animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Squat)
                .IsEmote();
            builder.Create("clap")
                .Description("Play a clap animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Clap)
                .IsEmote();
            builder.Create("salute")
                .Description("Play a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Salute)
                .IsEmote();
            builder.Create("facepalm")
                .Description("Play a facepalm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Facepalm)
                .IsEmote();
            builder.Create("wallfoot")
                .Description("Play a lean back on wall, foot up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LeanBackOnWallFootUp)
                .IsEmote();
            builder.Create("prisoner")
                .Description("Play a prisoner with hands behind back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Prisoner)
                .IsEmote();
            builder.Create("flex")
                .Description("Play a flex animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Flex)
                .IsEmote();
            builder.Create("dejectedkneel")
                .Description("Kneel dejectedly.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.DejectedKneel)
                .IsEmote();
            builder.Create("jedihandsonback")
                .Description("Place hands behind back like a Jedi.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.JediHandsBehindBack)
                .IsEmote();
            builder.Create("laydownfaceforward")
                .Description("Lay down, face forward.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayDownFaceForward)
                .IsEmote();
            builder.Create("shrug")
                .Description("Shrug")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Shrug)
                .IsEmote();
            builder.Create("kneeup")
               .Description("Play a knee up animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.KneeUp)
               .IsEmote();
            builder.Create("fetal")
               .Description("Play a fetal position animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.HoldHeadLow)
               .IsEmote();
            builder.Create("layheadonside")
               .Description("Lay to the side.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.LayToTheSide)
               .IsEmote();
            builder.Create("sitspread")
               .Description("Sit, legs spread.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.SitLegsSpread)
               .IsEmote();
            builder.Create("sitcrossed")
               .Description("Sit, legs crossed.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.SitLegsCrossed)
               .IsEmote();
            builder.Create("layback")
               .Description("Lay partially back, hands on stomach.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.LayPartiallyBackHandsOnStomach)
               .IsEmote();
            builder.Create("cheerloud")
               .Description("Cheer loudly.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.CheerLoudly)
               .IsEmote();
            builder.Create("shieldwall")
               .Description("Take a shield stance.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.ShieldWall)
               .IsEmote();
            builder.Create("dancehandsup")
               .Description("Dance with your hands held high.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.DanceHandsUp)
               .IsEmote();
            builder.Create("smoke")
               .Description("Smoke it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Smoke)
               .IsEmote();
            builder.Create("drink")
               .Description("Drink it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Drink)
               .IsEmote();
            builder.Create("kiss")
               .Description("Play a male/female kiss dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleKiss)
               .IsEmote();
            builder.Create("hug")
               .Description("Play a male/female hug dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleHug)
               .IsEmote();
            builder.Create("waltz")
               .Description("Play a male/female waltz dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleWaltz)
               .IsEmote();
            builder.Create("push")
               .Description("Play a push animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Push)
               .IsEmote();
            builder.Create("paraderest")
               .Description("Play a parade rest animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.ParadeRest)
               .IsEmote();
            builder.Create("bootdance")
               .Description("Play a boot dance animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.BootDance)
               .IsEmote();
            builder.Create("playflute")
               .Description("Play a flute animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PlayFlute)
               .IsEmote();
            builder.Create("playguitar")
               .Description("Play a guitar animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PlayGuitar)
               .IsEmote();
            builder.Create("pointpistol")
               .Description("Point your pistol.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PointPistol)
               .IsEmote();
            builder.Create("doublelsstance")
              .Description("Hold your lightsaber behind you.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DoubleLSStance)
              .IsEmote();
            builder.Create("classicjedistance")
              .Description("Get in the classic Jedi stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.ClassicJediStance)
              .IsEmote();
            builder.Create("onehandedstance")
              .Description("One handed melee stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.OneHandedStance)
              .IsEmote();
            builder.Create("dualwieldingstance")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DualWieldingStance)
              .IsEmote();
            builder.Create("dualwieldingstance2")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DualWieldingStance2)
              .IsEmote();

            return builder.Build();
        }
    }
}
