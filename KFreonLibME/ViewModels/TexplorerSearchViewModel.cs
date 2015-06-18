using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFreonLibME.ViewModels
{
    public class TexplorerSearchViewModel<T> : MESearchViewModel<T> where T : IToolEntry, new()
    {
        public TexplorerSearchViewModel(ICollection<T> collection)
            : base(collection)
        {

        }

        public override void Search(string val, string Searcher = null, ICollection<T> collection = null)
        {
            Results.Clear();
            var results = searchEngine.Search(val, Searcher, collection);

            if (results.Count != searchEngine.SearchableCollection.Count)
                Results.AddRange(results);
        }
    }
}
