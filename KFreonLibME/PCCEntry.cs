using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UsefulThings.WPF;

namespace KFreonLibME
{
    public class PCCEntry : ViewModelBase
    {
        public Action<PCCEntry> PCCEntryChangeAction { get; set; }
        public bool IsPCCStored { get; set; }


        // KFreon: Object for storing values and settings related to a PCC filename and corresponding expID
        bool _using = true;
        public bool Using     // KFreon: Indicates whether entry is considered in main operations
        {
            get
            {
                return _using;
            }
            set
            {
                SetProperty(ref _using, value);
                if (PCCEntryChangeAction != null)
                    PCCEntryChangeAction(this);
            }
        }

        string file = null;
        public string File   // KFreon: PCC filename
        {
            get
            {
                return file;
            }
            set
            {
                SetProperty(ref file, value);
                if (PCCEntryChangeAction != null)
                    PCCEntryChangeAction(this);
            }
        }
        public string Display  // KFreon: String to display in GUI
        {
            get
            {
                return ToString();
            }
        }


        int expid = -1;
        public int ExpID    // KFreon: ExpID
        {
            get
            {
                return expid;
            }
            set
            {
                SetProperty(ref expid, value);
                if (PCCEntryChangeAction != null)
                    PCCEntryChangeAction(this);
            }
        }

        public PCCEntry(Action<PCCEntry> pccEntryChangeAction = null)
        {
            Using = true;
            PCCEntryChangeAction = pccEntryChangeAction;
        }

        public PCCEntry(string pccName, int expID, Action<PCCEntry> pccEntryChangeAction = null)
            : this(pccEntryChangeAction)
        {
            File = pccName;
            ExpID = expID;
        }


        public static List<PCCEntry> PopulatePCCEntries(List<string> pccs, List<int> ExpIDs, Action<PCCEntry> pccEntryChangeAction = null)
        {
            List<PCCEntry> Entries = new List<PCCEntry>();
            for (int i = 0; i < (pccs.Count > ExpIDs.Count ? pccs.Count : ExpIDs.Count); i++)
            {
                PCCEntry entry = new PCCEntry(pccEntryChangeAction);
                entry.File = pccs.Count <= i ? null : pccs[i];
                entry.ExpID = ExpIDs.Count <= i ? -1 : ExpIDs[i];
                if (entry.File == null || entry.ExpID == -1)
                    entry.Using = false;
                Entries.Add(entry);
            }
            return Entries;
        }

        public override string ToString()
        {
            return File + "  @ " + ExpID;
        }

    }
}
