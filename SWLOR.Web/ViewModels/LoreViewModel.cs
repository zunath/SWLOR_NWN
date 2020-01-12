using System.Collections.Generic;
using System.Linq;
using DotNetify;
using SWLOR.Web.Models;

namespace SWLOR.Web.ViewModels
{
    public class LoreViewModel : BaseVM
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

        public LoreViewModel(GameTopicCollection collection)
        {
            TopicList = collection.Values.Where(x => x.GameTopicCategoryID == GameTopicCategory.Species).OrderBy(o => o.Sequence);
            SelectedTopicID = TopicList.First().ID;
        }

        private void LoadTopic()
        {
            SelectedTopic = TopicList.Single(x => x.ID == SelectedTopicID);
        }

    }
}
