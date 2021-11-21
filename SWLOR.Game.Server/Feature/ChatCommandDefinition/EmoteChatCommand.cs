using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ChatCommandService;

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
                .IsEmote(true);
            builder.Create("bow")
                .Description("Plays a bored animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetBow)
                .IsEmote(true); 
            builder.Create("cower")
                .Description("Plays a cower animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.HoldHead)
                .IsEmote(true);
            builder.Create("crossarms")
                .Description("Plays a cross arms animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.CrossArms)
                .IsEmote(true); 
            builder.Create("crouch")
                .Description("Plays a crouch animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Crouch)
                .IsEmote(true); 
            builder.Create("deadback")
                .Description("Plays a dead back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadBack)
                .IsEmote(true);
            builder.Create("deadfront")
                .Description("Plays a dead front animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadFront)
                .IsEmote(true);
            builder.Create("drink")
                .Description("Plays a drinking animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.FireForgetDrink)
                .IsEmote(true);
            builder.Create("drunk")
                .Description("Plays a drunk animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseDrunk)
                .IsEmote(true);
            builder.Create("duck")
                .Description("Plays a duck animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeDuck)
                .IsEmote(true);
            builder.Create("greet")
                .Description("Plays a greet animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetGreeting)
                .IsEmote(true);
            builder.Create("interact")
                .Description("Plays an interact animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetMid)
                .IsEmote(true);
            builder.Create("meditate")
                .Description("Plays a meditate animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingMeditate)
                .IsEmote(true);
            builder.Create("laughing")
                .Description("Plays a laughing animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingTalkLaughing)
                .IsEmote(true);
            builder.Create("listen")
                .Description("Plays a listen animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingListen)
                .IsEmote(true);
            builder.Create("look")
                .Description("Plays a look far animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingLookFar)
                .IsEmote(true);
            builder.Create("pickup")
                .Description("Plays a pickup animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetLow)
                .IsEmote(true);
            builder.Create("point")
                .Description("Plays a point animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PointForward)
                .IsEmote(true);
            builder.Create("read")
                .Description("Plays a read animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetRead)
                .IsEmote(true);
            builder.Create("salute")
                .Description("Plays a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetSalute)
                .IsEmote(true);
            builder.Create("scratchhead")
                .Description("Plays a scratch head animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetPauseScratchHead)
                .IsEmote(true);
            builder.Create("sidestep")
                .Description("Plays a side-step animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeSide)
                .IsEmote(true);
            builder.Create("sit")
                .Description("Makes your character sit down.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSitCross)
                .IsEmote(true);
            builder.Create("spasm")
                .Description("Plays a spasm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSpasm)
                .IsEmote(true);
            builder.Create("taunt")
                .Description("Plays a taunt animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetTaunt)
                .IsEmote(true);
            builder.Create("tired")
                .Description("Plays a tired animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseTired)
                .IsEmote(true);
            builder.Create("victory1")
                .Description("Plays a victory 1 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory1)
                .IsEmote(true);
            builder.Create("victory2")
                .Description("Plays a victory 2 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory2)
                .IsEmote(true);
            builder.Create("victory3")
                .Description("Plays a victory 3 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory3)
                .IsEmote(true);
            builder.Create("think")
                .Description("Plays a think animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.ThinkingMan)
                .IsEmote(true);
            builder.Create("jumpfoward")
                .Description("Plays a jump animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.JumpForward)
                .IsEmote(true);
            builder.Create("followme")
                .Description("Plays a follow-me animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.FollowMe)
                .IsEmote(true);
            builder.Create("hangbyhands")
                .Description("Plays a hanging by hands animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.HangByhands)
                .IsEmote(true);
            builder.Create("dig")
                .Description("Plays a dig animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Dig);
            builder.Create("layonside")
                .Description("Plays a lay on side animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayOnSide)
                .IsEmote(true);
            builder.Create("kneel")
                .Description("Plays a kneel animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Kneel)
                .IsEmote(true);
            builder.Create("laybackhandsonstomach")
                .Description("Plays a laying animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackHandsOnStomach)
                .IsEmote(true);
            builder.Create("layonbackupright")
                .Description("Plays a lay on back partially upright animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayOnBackUpright)
                .IsEmote(true);
            builder.Create("praystanding")
                .Description("Plays a praying animation standing up.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PrayStanding)
                .IsEmote(true);
            builder.Create("ypose")
                .Description("Like a T pose, but a Y.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.YPose)
                .IsEmote(true);
            builder.Create("disagree")
                .Description("Like a T pose, but a Y.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Disagree)
                .IsEmote(true);
            builder.Create("pushup")
                .Description("Play a push up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PushUp)
                .IsEmote(true);
            builder.Create("laybackwithhandsbehindhead")
                .Description("Play a play animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackHandsBehindHead)
                .IsEmote(true);
            builder.Create("laybackwithhandsbehindheadfeetup")
                .Description("Play a lay animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackWithHandsBehindHeadFeetUp)
                .IsEmote(true);
            builder.Create("jumpingjacks")
                .Description("Play a jumping jack animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.JumpingJacks)
                .IsEmote(true);
            builder.Create("squat")
                .Description("Play a squat animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Squat)
                .IsEmote(true);
            builder.Create("clap")
                .Description("Play a clap animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Clap)
                .IsEmote(true);
            builder.Create("salute")
                .Description("Play a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Salute)
                .IsEmote(true);
            builder.Create("faceplam")
                .Description("Play a facepalm..")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Facepalm)
                .IsEmote(true);
            builder.Create("handonhip")
                .Description("Play a hand on hip animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.HandOnHip)
                .IsEmote(true);
            builder.Create("leanbackonwallfootup")
                .Description("Play a lean back on wall, foot up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LeanBackOnWallFootUp)
                .IsEmote(true);
            builder.Create("prisoner")
                .Description("Play a Prisoner, kneeling hands behind back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Prisoner)
                .IsEmote(true);
            builder.Create("flex")
                .Description("Play a flex animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Flex)
                .IsEmote(true);
            builder.Create("dejectedkneel")
                .Description("Kneel dejectedly.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.DejectedKneel)
                .IsEmote(true);
            builder.Create("jedihandsonback")
                .Description("Place hands behind back like a Jedi.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.JediHandsBehindBack)
                .IsEmote(true);
            builder.Create("laydownfaceforward")
                .Description("Lay down, face forward.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayDownFaceForward)
                .IsEmote(true);
            builder.Create("shrug")
                .Description("Shrug")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Shrug)
                .IsEmote(true);
            builder.Create("usecomputer")
                .Description("Play a Use Computer animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.UseComputer)
                .IsEmote(true);
            builder.Create("kneeup")
               .Description("Play a knee up animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.KneeUp)
               .IsEmote(true);
            builder.Create("holdheadlow")
               .Description("Play a hold head low animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.HoldHeadLow)
               .IsEmote(true);
            builder.Create("layheadonside")
               .Description("Lay to the side.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.LayToTheSide)
               .IsEmote(true);
            builder.Create("sitspread")
               .Description("Sit, legs spread.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.SitLegsSpread)
               .IsEmote(true);
            builder.Create("sitcrossed")
               .Description("Sit, legs crossed.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.SitLegsCrossed)
               .IsEmote(true);
            builder.Create("layback")
               .Description("Lay partially back, hands on stomach.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.LayPartiallyBackHandsOnStomach)
               .IsEmote(true);
            builder.Create("cheerloud")
               .Description("Cheer loudly.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.CheerLoudly)
               .IsEmote(true);
            builder.Create("shieldwall")
               .Description("Take a shield stance.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.ShieldWall)
               .IsEmote(true);
            builder.Create("dancehandsup")
               .Description("Dance with your hands held high.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.DanceHandsUp)
               .IsEmote(true);
            builder.Create("shakirashakira")
               .Description("Hips don't lie.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.ShakiraShakira)
               .IsEmote(true);
            builder.Create("smoke")
               .Description("Smoke it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Smoke)
               .IsEmote(true);
            builder.Create("drink")
               .Description("Drink it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Drink)
               .IsEmote(true);
            builder.Create("kiss")
               .Description("Play a male/female kiss dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleKiss)
               .IsEmote(true);
            builder.Create("hug")
               .Description("Play a male/female hug dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleHug)
               .IsEmote(true);
            builder.Create("waltz")
               .Description("Play a male/female waltz dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleWaltz)
               .IsEmote(true);
            builder.Create("push")
               .Description("Play a push animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Push)
               .IsEmote(true);
            builder.Create("paraderest")
               .Description("Play a parade rest animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.ParadeRest)
               .IsEmote(true);
            builder.Create("bootdance")
               .Description("Play a boot dance animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.BootDance)
               .IsEmote(true);
            builder.Create("playflute")
               .Description("Play a flute animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PlayFlute)
               .IsEmote(true);
            builder.Create("playguitar")
               .Description("Play a guitar animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PlayGuitar)
               .IsEmote(true);
            builder.Create("pointpistol")
               .Description("Point your pistol.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PointPistol)
               .IsEmote(true);
            builder.Create("doublelsstance")
              .Description("Hold your lightsaber behind you.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DoubleLSStance)
              .IsEmote(true);
            builder.Create("classicjedistance")
              .Description("Get in the classic Jedi stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.ClassicJediStance)
              .IsEmote(true);
            builder.Create("onehandedstance")
              .Description("One handed melee stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.OneHandedStance)
              .IsEmote(true);
            builder.Create("seethe")
              .Description("Seethe like Maul.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.Seethe)
              .IsEmote(true);
            builder.Create("dualwieldingstance")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DualWieldingStance)
              .IsEmote(true);
            builder.Create("dualwieldingstance2")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DualWieldingStance2);

            return builder.Build();
        }
    }
}
