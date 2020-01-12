//using System.Collections.Generic;
//using System.Linq;
//using DotNetify;
//using SWLOR.Game.Server.Enumeration;
//using SWLOR.Game.Server.Extension;
//using SWLOR.Game.Server.Service;
//using SWLOR.Web.Models;

//namespace SWLOR.Web.ViewModels
//{
//    public class SkillsViewModel: BaseVM
//    {
//        public string SkillList_itemkey => nameof(SkillAttribute.Name);
//        public Dictionary<Skill, List<SkillAttribute>> SkillList
//        {
//            get => Get<Dictionary<Skill, List<SkillAttribute>>>();
//            set => Set(value);
//        }

//        public string SkillCollectionList_itemkey => nameof(SkillCategoryAttribute.Name);
//        public SkillCollection SkillCollectionList
//        {
//            get => Get<SkillCollection>();
//            set => Set(value);
//        }

//        public SkillCategory SelectedCategoryID
//        {
//            get => Get<SkillCategory>();
//            set
//            {
//                Set(value);
//                LoadSkillList();
//            }
//        }

//        public Skill SelectedSkillID
//        {
//            get => Get<Skill>();
//            set
//            {
//                Set(value); 
//                LoadSkill();
//            }
//        }

//        public SkillAttribute SelectedSkill
//        {
//            get => Get<SkillAttribute>();
//            set => Set(value);
//        }

//        public SkillsViewModel(SkillCollection skills)
//        {
//            SkillCollectionList = skills;

//            SelectedCategoryID = SkillCollectionList.Keys.First();
//            SelectedSkillID = SkillCollectionList.Values.First().First().Key;
//        }


//        private void LoadSkillList()
//        {
//            SkillList.Clear();

//            foreach (var skill in SkillCollectionList[SelectedCategoryID])
//            {
//                if (!skill.Value.IsActive) continue;



//                SkillList[skill.Key] = 
//            }

//            SkillList = SkillCollectionList[SelectedCategoryID]
//                .Where(x => x.Value.IsActive)
//                .OrderBy(o => o.Key)
//                .Select(o => new
//                {
//                    SkillID = o.Key, 
//                    SkillCategoryID = SelectedCategoryID, 
//                    Name = o.Value.Name,
//                    Description = o.Value.Description,
//                    MaxRank = o.Value.MaxRank,
//                    PrimaryName = o.Value.Primary.GetDescriptionAttribute(),
//                    SecondaryName = o.Value.Secondary.GetDescriptionAttribute(),
//                    TertiaryName = o.Value.Tertiary.GetDescriptionAttribute()
//                })
//                .ToList();

//            SelectedSkillID = SkillList.First().Key;
//        }

//        private void LoadSkill()
//        {
//            if (SelectedSkillID == Skill.Unknown) return;

//            SelectedSkill = SkillList[SelectedSkillID];
//        }
//    }
//}
