using System.Collections.Generic;
using System.Linq;
using DotNetify;
using SWLOR.Web.Models;

namespace SWLOR.Web.ViewModels.BaseViewModels
{
    public abstract class GameTopicBaseVM : BaseVM
    {
        public string TopicList_itemkey => nameof(GameTopic.ID);
        public IEnumerable<GameTopic> TopicList
        {
            get => Get<IEnumerable<GameTopic>>();
            set => Set(value);
        }

        public int SelectedTopicID
        {
            get => Get<int>();
            set
            {
                Set(value);
                LoadTopic();
            }
        }

        public GameTopic SelectedTopic
        {
            get => Get<GameTopic>();
            set => Set(value);
        }

        protected GameTopicBaseVM(GameTopicCollection collection, GameTopicCategory category)
        {
            TopicList = collection.Values.Where(x => x.GameTopicCategoryID == category && x.IsActive);
            
            var topic = TopicList.FirstOrDefault();

            if (topic != null)
            {
                SelectedTopicID = topic.ID;
            }

        }

        private void LoadTopic()
        {
            SelectedTopic = TopicList.Single(x => x.ID == SelectedTopicID);
        }

        protected abstract int CategoryID { get; }
    }
}
