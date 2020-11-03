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
                .AnimationAction(Animation.FireForgetPauseBored);
            builder.Create("bow")
                .Description("Plays a bored animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetBow);
            builder.Create("cower")
                .Description("Plays a cower animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingCustom3);
            builder.Create("crossarms")
                .Description("Plays a cross arms animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingCustom4);
            builder.Create("crouch")
                .Description("Plays a crouch animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingCustom2);
            builder.Create("deadback")
                .Description("Plays a dead back animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadBack);
            builder.Create("deadfront")
                .Description("Plays a dead front animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingDeadFront);
            builder.Create("drink")
                .Description("Plays a drinking animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDrink);
            builder.Create("drunk")
                .Description("Plays a drunk animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseDrunk);
            builder.Create("duck")
                .Description("Plays a duck animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeDuck);
            builder.Create("greet")
                .Description("Plays a greet animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetGreeting);
            builder.Create("interact")
                .Description("Plays an interact animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetMid);
            builder.Create("meditate")
                .Description("Plays a meditate animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingMeditate);
            builder.Create("laughing")
                .Description("Plays a laughing animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingTalkLaughing);
            builder.Create("listen")
                .Description("Plays a listen animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingListen);
            builder.Create("look")
                .Description("Plays a look far animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingLookFar);
            builder.Create("pickup")
                .Description("Plays a pickup animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingGetLow);
            builder.Create("point")
                .Description("Plays a point animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingCustom1);
            builder.Create("read")
                .Description("Plays a read animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetRead);
            builder.Create("salute")
                .Description("Plays a salute animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetSalute);
            builder.Create("scratchhead")
                .Description("Plays a scratch head animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetPauseScratchHead);
            builder.Create("sidestep")
                .Description("Plays a side-step animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetDodgeSide);
            builder.Create("sit")
                .Description("Makes your character sit down.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSitCross);
            builder.Create("spasm")
                .Description("Plays a spasm animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingSpasm);
            builder.Create("taunt")
                .Description("Plays a taunt animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetTaunt);
            builder.Create("tired")
                .Description("Plays a tired animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationLoopingAction(Animation.LoopingPauseTired);
            builder.Create("victory1")
                .Description("Plays a victory 1 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory1);
            builder.Create("victory2")
                .Description("Plays a victory 2 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory2);
            builder.Create("victory3")
                .Description("Plays a victory 3 animation.")
                .Permissions(AuthorizationLevel.All)
                .AnimationAction(Animation.FireForgetVictory3);


            return builder.Build();
        }
    }
}
