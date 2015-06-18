using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KFreonLibME.ViewModels
{
    public class ModMakerSearchViewModel<T> : MESearchViewModel<T> where T : KFreonLibME.Scripting.ModMaker.ModJob, new()
    {
        public override string SearchBox4Text
        {
            get
            {
                return searchBox4Text;
            }
            set
            {
                SetProperty(ref searchBox4Text, value);
                Search(value, "ScriptSearcher");
            }
        }

        public ModMakerSearchViewModel(ICollection<T> collection, ICollectionView itemView)
            : base(collection, itemView)
        {
            Func<T, string, bool> ScriptSearcher = (toolentry, searchString) =>
            {
                return toolentry.Script.Contains(searchString);
            };

            SearchMethods.Add("ScriptSearcher", ScriptSearcher);
            Mappings.Add("ScriptSearcher", () => SearchBox4Text);
        }
    }
}
