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
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("bow")
                .Description("Plays a bow animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetBow)
                .IsEmote(EmoteCategoryType.Social); 
            builder.Create("cower")
                .Description("Plays a cower animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.HoldHead)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("crossarms")
                .Description("Plays a cross arms animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.CrossArms)
                .IsEmote(EmoteCategoryType.Social); 
            builder.Create("crouch")
                .Description("Plays a crouch animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Crouch)
                .IsEmote(EmoteCategoryType.Exploration); 
            builder.Create("deadback")
                .Description("Plays a dead back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadBack)
                .IsEmote(EmoteCategoryType.Combat);
            builder.Create("deadfront")
                .Description("Plays a dead front animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadFront)
                .IsEmote(EmoteCategoryType.Combat);
            builder.Create("drink")
                .Description("Plays a drinking animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.FireForgetDrink)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("drunk")
                .Description("Plays a drunk animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseDrunk)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("duck")
                .Description("Plays a duck animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeDuck)
                .IsEmote(EmoteCategoryType.Combat);
            builder.Create("greet")
                .Description("Plays a greet animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetGreeting)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("interact")
                .Description("Plays an interact animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetMid)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("meditate")
                .Description("Plays a meditate animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingMeditate)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("laughing")
                .Description("Plays a laughing animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingTalkLaughing)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("listen")
                .Description("Plays a listen animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingListen)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("look")
                .Description("Plays a look far animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingLookFar)
                .IsEmote(EmoteCategoryType.Exploration);
            builder.Create("pickup")
                .Description("Plays a pickup animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetLow)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("point")
                .Description("Plays a point animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PointForward)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("read")
                .Description("Plays a read animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetRead)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("salute")
                .Description("Plays a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetSalute)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("scratchhead")
                .Description("Plays a scratch head animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetPauseScratchHead)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("sidestep")
                .Description("Plays a side-step animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeSide)
                .IsEmote(EmoteCategoryType.Combat);
            builder.Create("sit")
                .Description("Makes your character sit down.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSitCross)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("spasm")
                .Description("Plays a spasm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSpasm)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("taunt")
                .Description("Plays a taunt animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetTaunt)
                .IsEmote(EmoteCategoryType.Combat);
            builder.Create("tired")
                .Description("Plays a tired animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseTired)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("victory1")
                .Description("Plays a victory 1 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory1)
                .IsEmote(EmoteCategoryType.Combat);
            builder.Create("victory2")
                .Description("Plays a victory 2 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory2)
                .IsEmote(EmoteCategoryType.Combat);
            builder.Create("victory3")
                .Description("Plays a victory 3 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory3)
                .IsEmote(EmoteCategoryType.Combat);

            builder.Create("think")
                .Description("Plays a think animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.ThinkingMan)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("jumpforward")
                .Description("Plays a jump animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.JumpForward)
                .IsEmote(EmoteCategoryType.Exploration);
            builder.Create("followme")
                .Description("Plays a follow-me animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.FollowMe)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("hangbyhands")
                .Description("Plays a hanging by hands animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.HangByHands)
                .IsEmote(EmoteCategoryType.Exploration);
            builder.Create("dig")
                .Description("Plays a dig animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Dig)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("layonside")
                .Description("Plays a lay on side animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayOnSide)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("kneel")
                .Description("Plays a kneel animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Kneel)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("layback")
                .Description("Plays a laying animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackHandsOnStomach)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("laysitup")
                .Description("Plays a lay on back partially upright animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayOnBackUpright)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("praystanding")
                .Description("Plays a praying animation standing up.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PrayStanding)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("ypose")
                .Description("Hold arms up in a Y shape.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.YPose)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("disagree")
                .Description("Plays a disagree animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Disagree)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("pushup")
                .Description("Play a push up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.PushUp)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("lounge")
                .Description("Play a lounge animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackHandsBehindHead)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("situp")
                .Description("Play a sit-up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayBackWithHandsBehindHeadFeetUp)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("jumpingjacks")
                .Description("Play a jumping jack animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.JumpingJacks)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("squat")
                .Description("Play a squat animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Squat)
                .IsEmote(EmoteCategoryType.Tasks);
            builder.Create("clap")
                .Description("Play a clap animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Clap)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("salute")
                .Description("Play a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Salute)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("facepalm")
                .Description("Play a facepalm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Facepalm)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("wallfoot")
                .Description("Play a lean back on wall, foot up animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LeanBackOnWallFootUp)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("prisoner")
                .Description("Play a prisoner with hands behind back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Prisoner)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("flex")
                .Description("Play a flex animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Flex)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("dejectedkneel")
                .Description("Kneel dejectedly.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.DejectedKneel)
                .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("jedihandsonback")
                .Description("Place hands behind back like a Jedi.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.JediHandsBehindBack)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("laydownfaceforward")
                .Description("Lay down, face forward.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LayDownFaceForward)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("shrug")
                .Description("Shrug")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.Shrug)
                .IsEmote(EmoteCategoryType.Social);
            builder.Create("kneeup")
               .Description("Play a knee up animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.KneeUp)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("fetal")
               .Description("Play a fetal position animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.HoldHeadLow)
               .IsEmote(EmoteCategoryType.Feelings);
            builder.Create("layheadonside")
               .Description("Lay to the side.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.LayToTheSide)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("sitspread")
               .Description("Sit, legs spread.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.SitLegsSpread)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("sitcrossed")
               .Description("Sit, legs crossed.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.SitLegsCrossed)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("layback")
               .Description("Lay partially back, hands on stomach.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.LayPartiallyBackHandsOnStomach)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("cheerloud")
               .Description("Cheer loudly.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.CheerLoudly)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("shieldwall")
               .Description("Take a shield stance.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.ShieldWall)
               .IsEmote(EmoteCategoryType.Combat);
            builder.Create("dancehandsup")
               .Description("Dance with your hands held high.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.DanceHandsUp)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("smoke")
               .Description("Smoke it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Smoke)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("drink")
               .Description("Drink it up.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Drink)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("kiss")
               .Description("Play a male/female kiss dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleKiss)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("hug")
               .Description("Play a male/female hug dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleHug)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("waltz")
               .Description("Play a male/female waltz dependent on gender.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.MaleFemaleWaltz)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("push")
               .Description("Play a push animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.Push)
               .IsEmote(EmoteCategoryType.Combat);
            builder.Create("paraderest")
               .Description("Play a parade rest animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.ParadeRest)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("bootdance")
               .Description("Play a boot dance animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.BootDance)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("playflute")
               .Description("Play a flute animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PlayFlute)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("playguitar")
               .Description("Play a guitar animation.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PlayGuitar)
               .IsEmote(EmoteCategoryType.Social);
            builder.Create("pointpistol")
               .Description("Point your pistol.")
               .Permissions(AuthorizationLevel.All)
               .AnimationLoopingAction(Animation.PointPistol)
               .IsEmote(EmoteCategoryType.Combat);
            builder.Create("doublelsstance")
              .Description("Hold your lightsaber behind you.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DoubleLSStance)
              .IsEmote(EmoteCategoryType.Combat);
            builder.Create("classicjedistance")
              .Description("Get in the classic Jedi stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.ClassicJediStance)
              .IsEmote(EmoteCategoryType.Combat);
            builder.Create("onehandedstance")
              .Description("One handed melee stance.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.OneHandedStance)
              .IsEmote(EmoteCategoryType.Combat);
            builder.Create("dualwieldingstance")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DualWieldingStance)
              .IsEmote(EmoteCategoryType.Combat);
            builder.Create("dualwieldingstance2")
              .Description("Take a combat stance with two weapons.")
              .Permissions(AuthorizationLevel.All)
              .AnimationLoopingAction(Animation.DualWieldingStance2)
              .IsEmote(EmoteCategoryType.Combat);

            return builder.Build();
        }
    }
}
