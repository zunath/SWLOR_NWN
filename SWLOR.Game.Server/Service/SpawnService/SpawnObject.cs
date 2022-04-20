﻿using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.AnimationService;

namespace SWLOR.Game.Server.Service.SpawnService
{
    public class SpawnObject
    {
        public ObjectType Type { get; set; }
        public string Resref { get; set; }
        public int Weight { get; set; }
        public AIFlag AIFlags { get; set; }
        
        public List<DayOfWeek> RealWorldDayOfWeekRestriction { get; set; }
        public TimeSpan? RealWorldStartRestriction { get; set; }
        public TimeSpan? RealWorldEndRestriction { get; set; }

        public int GameHourStartRestriction { get; set; }
        public int GameHourEndRestriction { get; set; }

        public List<IAnimator> Animators { get; set; }

        public SpawnObject()
        {
            AIFlags = AIFlag.None;
            RealWorldDayOfWeekRestriction = new List<DayOfWeek>();
            GameHourStartRestriction = -1;
            GameHourEndRestriction = -1;
            Animators = new List<IAnimator>();
        }
    }
}
