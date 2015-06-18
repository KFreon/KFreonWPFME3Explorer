using KFreonLibME.Textures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings.WPF;
using UsefulThings;
using System.Collections.Concurrent;
using System.ComponentModel;


namespace KFreonLibME.ViewModels
{
    public class MESearchViewModel<T> : ViewModelBase where T : ISearchable
    {
        bool UseResults = false;
        public ICollection<T> SearchableCollection { get; set; }
        public RangedObservableCollection<T> Results { get; set; }

        public Dictionary<string, Func<T, string, bool>> SearchMethods { get; set; } 

        public bool IsEmpty
        {
            get
            {
                return String.IsNullOrEmpty(SearchBoxText) && String.IsNullOrEmpty(SearchBox1Text) && String.IsNullOrEmpty(SearchBox2Text)
                    && String.IsNullOrEmpty(SearchBox3Text) && String.IsNullOrEmpty(SearchBox4Text);
            }
        }


        string searchboxtext = null;
        public string SearchBoxText
        {
            get
            {
                return searchboxtext;
            }
            set
            {
                SetProperty(ref searchboxtext, value);
                OnPropertyChanged("IsEmpty");
                Search(value, "AllSearcher", true);
            }
        }

        private string searchbox1text = null;
        public string SearchBox1Text
        {
            get
            {
                return searchbox1text;
            }
            set
            {
                SetProperty(ref searchbox1text, value);
                OnPropertyChanged("IsEmpty");
                Search(value, "NameSearcher");
            }
        }

        string searchBox2 = null;
        public string SearchBox2Text
        {
            get
            {
                return searchBox2;
            }
            set
            {
                SetProperty(ref searchBox2, value);
                OnPropertyChanged("IsEmpty");
                Search(value, "PCCSearcher");
            }
        }

        string searchBox3 = null;
        public string SearchBox3Text
        {
            get
            {
                return searchBox3;
            }
            set
            {
                SetProperty(ref searchBox3, value);
                OnPropertyChanged("IsEmpty");
                Search(value, "ExpIDSearcher");
            }
        }

        protected string searchBox4Text = null;
        public virtual string SearchBox4Text
        {
            get
            {
                return searchBox4Text;
            }
            set
            {
                SetProperty(ref searchBox4Text, value);
                OnPropertyChanged("IsEmpty");
                Search(value, "HashSearch");
            }
        }


        ICollectionView ItemView = null;

        public Dictionary<string, Func<string>> Mappings = new Dictionary<string, Func<string>>();

        public MESearchViewModel(ICollection<T> searchingCollection, ICollectionView itemView, bool useResults = false) 
            : this(searchingCollection, itemView, useResults, null, null)
        {

        }

        public MESearchViewModel(ICollection<T> searchingCollection, ICollectionView itemView, bool useResults, Dictionary<string, Func<string>> mappings, Dictionary<string, Func<T, string, bool>> methods)
            : base()
        {
            SearchMethods = new Dictionary<string, Func<T, string, bool>>();
            SearchableCollection = searchingCollection;

            UseResults = useResults;
            ItemView = itemView;

            if ((mappings == null && methods != null) || (mappings != null && methods == null))
                throw new InvalidOperationException("Must provide both or neither for Mappings and Methods parameters.");

            if (mappings == null && methods == null)
            {
                Func<T, string, bool> NameSearcher = (ToolEntry, searchString) =>
                {
                    var toolentry = ToolEntry as IToolEntry;

                    if (toolentry == null)
                        throw new InvalidCastException("Expected type IToolEntry. Got: " + toolentry.GetType());

                    return toolentry.EntryName.Contains(searchString, StringComparison.InvariantCultureIgnoreCase);
                };

                Func<T, string, bool> PCCSearcher = (ToolEntry, searchString) =>
                {
                    var toolentry = ToolEntry as IToolEntry;

                    if (toolentry == null)
                        throw new InvalidCastException("Expected type IToolEntry. Got: " + toolentry.GetType());


                    // KFreon: Don't care what the result is YET, only that it's there
                    foreach (PCCEntry entry in toolentry.PCCs)
                        if (entry.File.Contains(searchString, StringComparison.InvariantCultureIgnoreCase))
                            return true;

                    return false;
                };

                Func<T, string, bool> ExpIDSearcher = (ToolEntry, searchString) =>
                {
                    int ExpID;

                    var toolentry = ToolEntry as IToolEntry;

                    if (toolentry == null)
                        throw new InvalidCastException("Expected type IToolEntry. Got: " + toolentry.GetType());

                    // KFreon: Check it's a number
                    if (int.TryParse(searchString, out ExpID))
                    {
                        foreach (PCCEntry entry in toolentry.PCCs)
                            if (entry.ExpID.ToString().Contains(searchString))
                                return true;
                    }

                    return false;
                };


                Func<T, string, bool> HashSearch = (ToolEntry, searchString) =>
                {
                    var toolentry = ToolEntry as IToolEntry;

                    if (toolentry == null)
                        throw new InvalidCastException("Expected type IToolEntry. Got: " + toolentry.GetType());


                    return KFreonLibME.Textures.Methods.FormatTexmodHashAsString(toolentry.Hash).Contains(searchString, StringComparison.InvariantCultureIgnoreCase);
                };

                Mappings.Add("NameSearcher", () => { return SearchBox1Text; });
                Mappings.Add("PCCSearcher", () => { return SearchBox2Text; });
                Mappings.Add("ExpIDSearcher", () => { return SearchBox3Text; });
                Mappings.Add("HashSearch", () => { return SearchBox4Text; });

                SearchMethods.Add("NameSearcher", NameSearcher);
                SearchMethods.Add("PCCSearcher", PCCSearcher);
                SearchMethods.Add("ExpIDSearcher", ExpIDSearcher);
                SearchMethods.Add("HashSearch", HashSearch);
            }
            else
            {
                Mappings.AddRange(mappings);
                SearchMethods.AddRange(methods);
            }
        }

        private ICollection<T> Test(string searchString)
        {
            ICollection<T> tempResults = SearchableCollection;
            foreach (var searcher in SearchMethods)
            {
                List<T> tempSearching = new List<T>();
                foreach (var item in tempResults)
                    if (searcher.Value(item, searchString))
                        tempSearching.Add(item);

                tempResults = tempSearching;
            }

            return tempResults;
        }

        public virtual void Search(string val, string Searcher = null, bool SearchInParallel = false, ICollection<T> collection = null)
        {
            if (String.IsNullOrEmpty(val))
                return;


            ICollection<T> results = new ConcurrentBag<T>() as ICollection<T>;
            
            if (Searcher == "AllSearcher")
            {
                //foreach (var item in searchEngine.SearchableCollection)
                /*Parallel.ForEach(searchEngine.SearchableCollection, item =>
                {
                    List<bool> searcherresults = new List<bool>();
                    foreach (var searcher1 in searchEngine.SearchMethods)
                        searcherresults.Add(searcher1.Value(item, val));

                    if (searcherresults.Contains(true))
                        results.Add(item);
                });*/
                
                /*var tempResults = SearchableCollection.Where(item => 
                    {
                        foreach (var searcher in SearchMethods)
                            if (searcher(item))
                                return true;
                    });
                    
                    
               OR*/

                results = Test(val);
            }
            else
            {
                ConcurrentBag<IEnumerable<T>> tempresults = new ConcurrentBag<IEnumerable<T>>();

                // KFreon: Then look in all other searches using each result as the collection to search in
                Parallel.ForEach(SearchMethods, searcher =>
                    {
                        if (searcher.Key != Searcher)
                        {
                            tempresults.Add(Test(val));
                        }
                    });

                foreach (var list in tempresults)
                {
                    if (results.Count == 0)
                        results = new ConcurrentBag<T>(list) as ICollection<T>;
                    else if (list.Count() == 0)
                        continue;
                    else
                        results = new ConcurrentBag<T>(list.Intersect(results)) as ICollection<T>;
                }
            }



            // KFreon: Unique values only
            List<T> newResults = results.Distinct().ToList(results.Count);
        
            // KFreon: Either add to results list (for Texplorer), or filter view (TPFTools and Modmaker)
            if (UseResults)
            {
                Results.Clear();
                Results.AddRange(newResults);
            }
            else
            {
                bool found = false;
                bool defaultFound = val.Length == 0;
                Parallel.ForEach(SearchableCollection, item =>
                {
                    found = defaultFound;
                    foreach (var result in newResults)
                        if (item.Equals(result))
                            found = true;
        
                    item.IsSearchVisible = found;
                });
            }
        
            ItemView.Refresh();
            
        }

        internal void ClearAll()
        {
            SearchBox1Text = null;
            SearchBox2Text = null;
            SearchBox3Text = null;
            SearchBox4Text = null;
            SearchBoxText = null;
        }
    }
}
