using System;
using System.Collections.Generic;
using System.Linq;
using DotNetify;

namespace SWLOR.Web.ViewModels.BaseViewModels
{
    public abstract class PaginateBaseVM: BaseVM
    {
        private readonly int _recordsPerPage;

        private IEnumerable<dynamic> FullItems { get; }

        protected PaginateBaseVM(IEnumerable<dynamic> fullItems,
            string itemKeyName,
            int recordsPerPage = 15)
        {
            FullItems = fullItems;
            _recordsPerPage = recordsPerPage;
            PaginatedItems_itemkey = itemKeyName;
        }

        public IEnumerable<dynamic> PaginatedItems
        {
            get
            {
                var records = Paginate(FullItems);
                Pages = FullItems.Count() / _recordsPerPage;

                return records;
            }
        }

        public string PaginatedItems_itemkey { get; }

        public int SelectedPage
        {
            get => Get<int>();
            set
            {
                Set(value);
                Changed(nameof(PaginatedItems));
            }
        }

        public int[] Pagination
        {
            get => Get<int[]>();
            set
            {
                Set(value);
                SelectedPage = 1;
            }
        }

        public int Pages
        {
            get => Get<int>();
            set => Set(value);
        }

        public Action<int> ChangePage => page =>
        {
            SelectedPage = page;
        };


        private IEnumerable<dynamic> Paginate(IEnumerable<dynamic> items)
        {
            if (ChangedProperties.ContainsKey(nameof(SelectedPage)))
                return items.Skip(_recordsPerPage * (SelectedPage)).Take(_recordsPerPage);
            else
            {
                var enumerable = items as IList<dynamic> ?? items.ToList();
                var pageCount = (int)Math.Ceiling(enumerable.Count / (double)_recordsPerPage);
                Pagination = Enumerable.Range(1, pageCount).ToArray();
                return enumerable.Take(_recordsPerPage);
            }
        }
    }
}
