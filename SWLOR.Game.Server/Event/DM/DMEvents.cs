using System;
using System.Collections.Generic;
using System.Text;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Event.DM
{
    public static class DMEvents
    {
        [NWNEventHandler("dm_appear_bef")]
        public static void Appears()
        {
            MessageHub.Instance.Publish(new OnDMAction(20));
        }
        [NWNEventHandler("dm_chgdiff_bef")]
        public static void ChangeDifficulty()
        {
            MessageHub.Instance.Publish(new OnDMAction(8));
        }
        [NWNEventHandler("dm_distrap_bef")]
        public static void DisableTrap()
        {
            MessageHub.Instance.Publish(new OnDMAction(19));
        }
        [NWNEventHandler("dm_disappear_bef")]
        public static void Disappears()
        {
            MessageHub.Instance.Publish(new OnDMAction(21));
        }
        [NWNEventHandler("dm_forcerest_bef")]
        public static void ForceRest()
        {
            MessageHub.Instance.Publish(new OnDMAction(15));
        }
        [NWNEventHandler("dm_getvar_bef")]
        public static void GetVariable()
        {
            MessageHub.Instance.Publish(new OnDMAction(30));
        }
        [NWNEventHandler("dm_givegold_bef")]
        public static void GiveGold()
        {
            MessageHub.Instance.Publish(new OnDMAction(24));
        }
        [NWNEventHandler("dm_giveitem_bef")]
        public static void GiveItem()
        {
            MessageHub.Instance.Publish(new OnDMAction(25));
        }
        [NWNEventHandler("dm_givelvl_bef")]
        public static void GiveLevel()
        {
            MessageHub.Instance.Publish(new OnDMAction(23));
        }
        [NWNEventHandler("dm_givexp_bef")]
        public static void GiveXP()
        {
            MessageHub.Instance.Publish(new OnDMAction(22));
        }
        [NWNEventHandler("dm_heal_bef")]
        public static void Heal()
        {
            MessageHub.Instance.Publish(new OnDMAction(10));
        }
        [NWNEventHandler("dm_jumppt_bef")]
        public static void Jump()
        {
            MessageHub.Instance.Publish(new OnDMAction(12));
        }
        [NWNEventHandler("dm_jumpall_bef")]
        public static void JumpAll()
        {
            MessageHub.Instance.Publish(new OnDMAction(28));
        }
        [NWNEventHandler("dm_jumptarg_bef")]
        public static void JumpTarget()
        {
            MessageHub.Instance.Publish(new OnDMAction(27));
        }
        [NWNEventHandler("dm_kill_bef")]
        public static void Kill()
        {
            MessageHub.Instance.Publish(new OnDMAction(11));
        }
        [NWNEventHandler("dm_limbo_bef")]
        public static void Limbo()
        {
            MessageHub.Instance.Publish(new OnDMAction(16));
        }
        [NWNEventHandler("dm_poss_bef")]
        public static void Possess()
        {
            MessageHub.Instance.Publish(new OnDMAction(13));
        }
        [NWNEventHandler("dm_setdate_bef")]
        public static void SetDate()
        {
            MessageHub.Instance.Publish(new OnDMAction(33));
        }
        [NWNEventHandler("dm_setstat_bef")]
        public static void SetStat()
        {
            MessageHub.Instance.Publish(new OnDMAction(29));
        }
        [NWNEventHandler("dm_settime_bef")]
        public static void SetTime()
        {
            MessageHub.Instance.Publish(new OnDMAction(32));
        }
        [NWNEventHandler("dm_setvar_bef")]
        public static void SetVar()
        {
            MessageHub.Instance.Publish(new OnDMAction(31));
        }
        [NWNEventHandler("dm_takeitem_bef")]
        public static void TakeItem()
        {
            MessageHub.Instance.Publish(new OnDMAction(26));
        }
        [NWNEventHandler("dm_immortal_bef")]
        public static void ToggleImmortality()
        {
            MessageHub.Instance.Publish(new OnDMAction(14));
        }
        [NWNEventHandler("dm_ai_bef")]
        public static void ToggleAI()
        {
            MessageHub.Instance.Publish(new OnDMAction(17));
        }
        [NWNEventHandler("dm_lock_bef")]
        public static void ToggleLock()
        {
            MessageHub.Instance.Publish(new OnDMAction(18));
        }
    }
}
