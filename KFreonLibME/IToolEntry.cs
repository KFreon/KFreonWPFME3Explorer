using System.Collections.Generic;
using UsefulThings.WPF;

namespace KFreonLibME
{
    public interface IToolEntry : ISearchable
    {
        string EntryName { get; set; }
        RangedObservableCollection<PCCEntry> PCCs { get; set; }
        uint Hash { get; set; }
        bool IsSelected { get; set; }
    }
}
