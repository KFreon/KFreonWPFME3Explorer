using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings.WPF;
using UsefulThings;
using System.ComponentModel;

namespace KFreonLibME.ViewModels
{
    public class FileEntrySearchViewModel<T> : MESearchViewModel<T> where T : FileEntryBase
    {
        ICollectionView ItemsView = null;
        public FileEntrySearchViewModel(ICollection<T> searchingCollection, ICollectionView itemview, params KeyValuePair<string, Func<T, string, bool>>[] Searchers) 
            : base(searchingCollection, itemview)
        {
            ItemsView = itemview;

            Func<FileEntryBase, string, bool> searcher = (item, searchString) =>
            {
                if (item.GetType() == typeof(DLCFileEntry))
                {
                    foreach (var file in ((DLCFileEntry)item).Files)
                        if (file.Info.FullName.Contains(searchString, StringComparison.CurrentCultureIgnoreCase))
                            return true;

                    return false;
                }
                else
                    return item.Info.FullName.Contains(searchString, StringComparison.CurrentCultureIgnoreCase);
            };
        }


        public override void Search(string val, string Searcher = null, bool SearchInParallel = false, ICollection<T> collection = null)
        {
            List<T> tempresults = new List<T>();
            
            bool defaultFound = val.Length == 0;

            if (typeof(T) == typeof(DLCFileEntry))
                Parallel.ForEach(SearchableCollection, item =>
                {
                    bool mainDLCVisible = CheckVisibility(item, tempresults, defaultFound);

                    foreach (var file in item.Files)
                    {
                        if (mainDLCVisible)
                            file.IsSearchVisible = file.Info.FullName.Contains(val, StringComparison.CurrentCultureIgnoreCase);
                        else
                            file.IsSearchVisible = false;
                    }

                    item.FilesView.Refresh();
                });
            else
                Parallel.ForEach(SearchableCollection, item =>
                {
                    CheckVisibility(item, tempresults, defaultFound);
                });

            ItemsView.Refresh();
        }

        private bool CheckVisibility(FileEntryBase item, List<T> tempresults, bool found)
        {
            if (tempresults.Contains(item))
                found = true;
            
            item.IsSearchVisible = found;
            return found;
        }
    }
}
