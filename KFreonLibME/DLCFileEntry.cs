using KFreonLibME.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace KFreonLibME
{
    public class DLCFileEntry : FileEntryBase
    {
        bool newOnly = false;
        public bool NewOnly
        {
            get
            {
                return newOnly;
            }
            set
            {
                newOnly = value;

                if (FilesView != null)
                    FilesView.Refresh();
            }
        }

        public string Name { get; set; }
        public string FullPath { get; set; }
        bool modifiedonly = false;
        public bool ModifiedOnly
        {
            get
            {
                return modifiedonly;
            }
            set
            {
                modifiedonly = value;
                
                if (FilesView != null)
                    FilesView.Refresh();
            }
        }

        public DLCFileEntry(string dlcpath, int game, DateTime extractedDate, Func<string, bool> isScannedPredicate, bool _newonly, bool _modifiedonly) : base(Path.Combine(dlcpath, game == 3 ? "CookedPCConsole" : "CookedPC", game == 3 ? "Default.sfar" : ""))
        {
            NewOnly = _newonly;
            ModifiedOnly = _modifiedonly;

            ValidityString = "HSSSS SFAR NO MONITORED!";
            ThresholdString = "Immaterial.";
            FullPath = Path.Combine(dlcpath, game == 3 ? "CookedPCConsole" : "CookedPC", game == 3 ? "Default.sfar" : "");
            Name = MEDirectories.MEDirectories.GetDLCNameFromPath(dlcpath);

            Files = new List<FileEntry>();

            foreach (string file in MEDirectories.MEDirectories.EnumerateGameFiles(game, dlcpath).Where(file => !file.EndsWith("tfc")))
            {
                bool isScanned = isScannedPredicate(file);
                Files.Add(new FileEntry(file, extractedDate, isScanned));
            }

            FilesView = CollectionViewSource.GetDefaultView(Files);
            FilesView.Filter = t => TexplorerViewModel.CheckBaseGameEntryVisibility(t, NewOnly, ModifiedOnly);

            IsScanned = Files != null && Files.Count != 0;
        }
    }
}
