using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class EchaniTempleMonks
    {
        private static readonly string[] Tags = { "es_monk_master", "es_monk_ren", "es_monk_sai" };
        private const string FirstStop = "es_monk_path_000";
        private const string StopKey = "ECHANI_MONK_STOP";
        private const string BeatKey = "ECHANI_MONK_BEAT";
        private const string MovingKey = "ECHANI_MONK_MOVING";

        [NWNEventHandler(ScriptName.OnCreatureHeartbeatAfter)]
        public static void Heartbeat()
        {
            var leader = OBJECT_SELF;
            if (!IsMember(leader, Tags[0])) return;
            var monks = Tags.Select(GetUniqueObject).ToArray();
            if (monks.Where((monk, index) => !IsMember(monk, Tags[index])).Any()) return;
            if (monks.Any(monk => !Available(monk)))
            {
                foreach (var monk in monks) StopMovement(monk);
                return;
            }

            var tag = GetLocalString(leader, StopKey);
            var stop = GetUniqueObject(string.IsNullOrEmpty(tag) ? FirstStop : tag);
            if (!GetIsObjectValid(stop) || GetObjectType(stop) != ObjectType.Waypoint) return;
            if (monks.Any(monk => GetArea(monk) != GetArea(stop))) return;

            var arrived = true;
            foreach (var monk in monks)
            {
                if (GetDistanceBetween(monk, stop) <= 3f)
                {
                    StopMovement(monk);
                    continue;
                }
                arrived = false;
                if (GetCurrentAction(monk) != ActionType.Invalid) continue;
                SetLocalInt(monk, MovingKey, 1);
                AssignCommand(monk, () => ActionMoveToLocation(GetLocation(stop)));
            }
            if (!arrived) return;
            if (monks.Any(monk => GetCurrentAction(monk) != ActionType.Invalid)) return;

            var next = GetUniqueObject(GetLocalString(stop, "ECHANI_NEXT"));
            if (!GetIsObjectValid(next)) return;
            var transfer = GetLocalString(stop, "ECHANI_TRANSFER");
            if (!string.IsNullOrEmpty(transfer))
            {
                var destination = GetUniqueObject(transfer);
                if (!GetIsObjectValid(destination) || GetArea(destination) != GetArea(next)) return;
                foreach (var monk in monks)
                    AssignCommand(monk, () => JumpToLocation(GetLocation(destination)));
            }
            else
            {
                var scene = GetLocalString(stop, "ECHANI_SCENE");
                if (!string.IsNullOrEmpty(scene))
                {
                    var beat = GetLocalInt(leader, BeatKey);
                    var rest = scene == "spa" ? 30 : scene == "temple" ? 12 : 6;
                    if (beat < rest)
                    {
                        PlayBeat(monks, stop, scene, beat);
                        SetLocalInt(leader, BeatKey, beat + 1);
                        return;
                    }
                }
            }
            DeleteLocalInt(leader, BeatKey);
            SetLocalString(leader, StopKey, GetTag(next));
        }

        private static uint GetUniqueObject(string tag)
        {
            if (string.IsNullOrEmpty(tag) || GetIsObjectValid(GetObjectByTag(tag, 1))) return OBJECT_INVALID;
            return GetObjectByTag(tag);
        }

        private static bool IsMember(uint creature, string tag) =>
            GetIsObjectValid(creature) && GetObjectType(creature) == ObjectType.Creature &&
            GetTag(creature) == tag && GetResRef(creature) == tag && GetLocalInt(creature, "ECHANI_MONK") == 1 &&
            !GetIsPC(creature) && !GetIsDM(creature);

        private static bool Available(uint creature) =>
            !GetIsDMPossessed(creature) && !GetIsDead(creature) && GetCurrentHitPoints(creature) > 0 &&
            !GetIsInCombat(creature) && !GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)) &&
            AI.IsCreatureAIEnabled(creature) && GetCommandable(creature) &&
            !IsInConversation(creature) && !Activity.IsBusy(creature) &&
            !Area.GetPlayersInArea(GetArea(creature)).Any(player => Gui.HasOpenWindowTetheredTo(player, creature));

        private static void StopMovement(uint creature)
        {
            // Only clear movement owned by this controller; combat and DM commands retain ownership.
            if (!GetIsDMPossessed(creature) && !GetIsInCombat(creature) &&
                !GetIsObjectValid(Enmity.GetHighestEnmityTarget(creature)) &&
                GetLocalInt(creature, MovingKey) == 1 &&
                GetCurrentAction(creature) == ActionType.MoveToPoint)
                ClearAllActions(false, creature);
            DeleteLocalInt(creature, MovingKey);
        }

        private static void PlayBeat(uint[] monks, uint stop, string scene, int beat)
        {
            if (beat > 2)
            {
                if (beat == 3 && (scene == "temple" || scene == "spa"))
                    foreach (var monk in monks)
                        AssignCommand(monk, () => ActionPlayAnimation(Animation.LoopingMeditate, 1f, 6f));
                return;
            }

            var speaker = monks[beat];
            var listener = monks[(beat + 1) % monks.Length];
            var host = GetUniqueObject(GetLocalString(stop, "ECHANI_HOST"));
            if (GetIsObjectValid(host) && GetArea(host) == GetArea(stop) && GetDistanceBetween(host, stop) <= 8f)
            {
                listener = host;
                // Existing residents keep their movement and conversation queues.
                if (beat == 2 && Available(host) && GetCurrentAction(host) == ActionType.Invalid)
                {
                    AssignCommand(host, () => SpeakString(Reply(scene)));
                    return;
                }
            }
            SetFacingPoint(GetPosition(listener), speaker);
            AssignCommand(speaker, () =>
            {
                SpeakString(Line(scene, beat));
                ActionPlayAnimation(Animation.LoopingTalkNormal, 1f, 3f);
            });
        }

        private static string Reply(string scene) => scene switch
        {
            "errands" => "Thank you, Master. There is always another doorstep to reach.",
            "cloth" => "Bring the torn hems by tomorrow. I will have thread ready.",
            "teacher" => "And a patient teacher knows when to let the student answer.",
            _ => "Safe travels to you."
        };

        private static string Line(string scene, int beat) => (scene, beat) switch
        {
            ("temple", 0) => "Before we leave, settle your breathing. Carry that stillness into the city.",
            ("temple", 1) => "Even when the streets are crowded, Master?",
            ("temple", 2) => "Especially then. I will try to remember.",
            ("errands", 0) => "Good morning, Ilya. May your errands leave you a little time to rest.",
            ("cloth", 0) => "Cerin, your stitching has survived another week of practice.",
            ("teacher", 0) => "Toren. Have these two learned to listen with their feet yet?",
            ("river", 0) => "Listen to the river before you speak. We have time.",
            ("homes", 0) => "Keep your voices low here. These are people's homes.",
            ("market", 0) => "We will pause here. Check your straps before the climb.",
            ("gardens", 0) => "A quiet garden is a good place to notice an impatient mind.",
            ("ward", 0) => "The city is waking. Give the workers room to pass.",
            ("port", 0) => "Every arrival brings someone else's understanding of the world.",
            ("practice", 0) => "Remember this ground. A careful step matters as much as a strong strike.",
            ("heights", 0) => "Shorter steps on the climb. We arrive together.",
            ("peaks", 0) => "Feel the wind before you choose your footing.",
            ("spa", 0) => "We have arrived. Rest is part of discipline too.",
            ("spa", 1) => "That is a lesson I am ready to practice.",
            ("spa", 2) => "Slowly, Ren. Even now you are rushing.",
            (_, 1) => "I understand, Master. I will pay attention.",
            (_, 2) => "Then let us take the next stretch together.",
            _ => "A moment here, before we continue."
        };
    }
}
