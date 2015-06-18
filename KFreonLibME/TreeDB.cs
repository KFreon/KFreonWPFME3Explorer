using KFreonLibGeneral.Debugging;
using KFreonLibME.Textures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using UsefulThings.WPF;
using UsefulThings;
using System.Windows.Threading;
using System.Windows.Input;
using System.IO.Compression;
using System.Diagnostics;

namespace KFreonLibME
{
    public class TreeDB : ViewModelBase
    {
        public class TreePCC
        {
            public string Name { get; set; }
            public DateTime ScannedDate { get; set; }

            public TreePCC()
            {
                
            }

            public TreePCC(string name, DateTime scannedtime)
            {
                Name = name;
                ScannedDate = scannedtime;
            }

            public bool Exists { get; set; }

            public bool ValidDate { get; set; }
        }

        static List<int> Years = new List<int>() { 2008, 2009, 2012 };
        static List<int> Months = new List<int>() { 6, 12, 4 };

        FileSystemWatcher treeWatcher = null;
        MEDirectories.MEDirectories MEExDirecs;

        Dictionary<string, DateTime> DLCExtractionDates = new Dictionary<string, DateTime>(20);

        #region Properties
        int numtexes = 0;
        public int NumTreeTexes
        {
            get
            {
                return numtexes;
            }
            set
            {
                SetProperty(ref numtexes, value);
            }
        }


        public int TexCount
        {
            get
            {
                return Textures.Count;
            }
        }

        bool isSelected = false;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }
            set
            {
                SetProperty(ref isSelected, value);
            }
        }
        public string TreeLocation
        {
            get
            {
                return Path.Combine(MEExDirecs.ExecFolder, "me" + GameVersion + "tree.bin");
            }
        }
        public List<TreePCC> PCCs { get; set; }

        bool exists = false;
        public bool Exists
        {
            get
            {
                return exists;
            }
            set
            {
                exists = value;
                OnPropertyChanged();
            }
        }

        bool valid = false;
        public bool Valid
        {
            get
            {
                return valid;
            }
            set
            {
                valid = value;
                OnPropertyChanged();
            }
        }
         
         
         

        readonly object TreeLocker = new object();
        readonly object RunningLocker = new object();

        public List<TreeTexInfo> Textures { get; set; }

        public RangedObservableCollection<HierarchicalTreeTexes> TreeTexes { get; set; }

        public bool? AdvancedFeatures { get; set; }
        public int GameVersion { get; set; }

        public int ValidYear
        {
            get
            {
                return Years[GameVersion - 1];
            }
        }

        public int ValidMonth
        {
            get
            {
                return Months[GameVersion - 1];
            }
        }

        public int ValidDLCYear { get; set; }
        public int ValidDLCMonth { get; set; }
        #endregion Properties


        ICommand selectCommand = null;
        public ICommand SelectCommand
        {
            get
            {
                return selectCommand;
            }
            set
            {
                SetProperty(ref selectCommand, value);
            }
        }

        public TreeDB(List<string> pccs, MEDirectories.MEDirectories direcs, int game, bool Selected, ICommand selectcommand, bool DontLoad = false)
            : this(direcs, game, Selected, selectcommand, DontLoad)
        {
            List<TreePCC> temppccs = new List<TreePCC>();
            DateTime now = DateTime.Now;
            pccs.ForEach(pcc => temppccs.Add(new TreePCC(pcc, now)));
            PCCs = temppccs;
        }

        public TreeDB(string filename) : this()
        {
            GameVersion = int.Parse(Path.GetFileName(filename)[2] + "");
            MEExDirecs = new MEDirectories.MEDirectories(GameVersion);
            LoadTree(filename);
        }

        private TreeDB()
        {
            PCCs = new List<TreePCC>();
            Textures = new List<TreeTexInfo>();
            TreeTexes = new RangedObservableCollection<HierarchicalTreeTexes>();
        }

        public TreeDB(MEDirectories.MEDirectories direcs, int game, bool Selected, ICommand selectcommand, bool DontLoad = false) : this()
        {
            MEExDirecs = new MEDirectories.MEDirectories(direcs, game);
            GameVersion = game;

            SelectCommand = selectcommand;

            BindingOperations.EnableCollectionSynchronization(TreeTexes, TreeLocker);

            ValidDLCMonth = Properties.Settings.Default.DLCExtractionMonth;
            ValidDLCYear = Properties.Settings.Default.DLCExtractionYear;

            treeWatcher = new FileSystemWatcher(MEExDirecs.ExecFolder, Path.GetFileName(TreeLocation));
            treeWatcher.Changed += treeWatcher_Changed;
            treeWatcher.Created += treeWatcher_Changed;
            treeWatcher.Deleted += treeWatcher_Changed;
            treeWatcher.Renamed += treeWatcher_Changed;

            treeWatcher.EnableRaisingEvents = true;

            IsSelected = Selected;

            // KFreon: Read DLC extraction dates if present
            if (GameVersion == 3)
            {
                try
                {
                    if (Properties.Settings.Default.DLCExtractionDates != null)
                    {
                        foreach (var item in Properties.Settings.Default.DLCExtractionDates)
                        {
                            string[] parts = item.Split('|');
                            UpdateDLCDate(parts[0], DateTime.FromBinary(Int64.Parse(parts[1])));
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("Failed to read DLC Extraction dates.", "TreeDB ctor", e);
                }
            }

            if (!DontLoad)
                LoadTree();
        }

        void treeWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            Debug.WriteLine(GameVersion + " Tree renamed!");
        }

        void treeWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Debug.WriteLine(GameVersion + " Tree changed: " + e.ChangeType);
        }

        public void SaveTree(string destinationFileName)
        {
            DebugOutput.PrintLn(String.Format("Saving ME{0} tree to file at: {1}", GameVersion, destinationFileName));
            using (FileStream fs = new FileStream(destinationFileName, FileMode.Create, FileAccess.Write))
            {
                using (GZipStream Compressor = new GZipStream(fs, CompressionLevel.Optimal))
                    WriteTreeToStream(Compressor);
            }
        }

        private void WriteTreeToStream(Stream output)
        {
            using (BinaryWriter bin = new BinaryWriter(output))
            {
                bin.Write(631991); // KFreon: Marker for advanced features

                // KFreon: Write pccs scanned 
                DateTime current = DateTime.Now;
                bin.Write(PCCs.Count);
                foreach (TreePCC pcc in PCCs)
                {
                    //bin.Write(pcc.Name.Length);
                    bin.Write(pcc.Name.Remove(0, MEExDirecs.BasePath.Length));  // writing as string? can change all others?
                    bin.Write(pcc.ScannedDate.ToBinary());
                }

                // KFreon: Write DLC dates
                /*bin.Write(DLCExtractionDates.Count);
                foreach (KeyValuePair<string, DateTime> date in DLCExtractionDates)
                {
                    bin.Write(date.Key);
                    bin.Write(date.Value.ToBinary());
                }*/

                if (GameVersion == 3)
                    try
                    {
                        if (Properties.Settings.Default.DLCExtractionDates == null)
                            Properties.Settings.Default.DLCExtractionDates = new System.Collections.Specialized.StringCollection();

                        foreach (var item in DLCExtractionDates)
                            Properties.Settings.Default.DLCExtractionDates.Add(item.Key + "|" + item.Value.ToBinary());
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("Failed to set DLCExtraction Dates.", "TreeDB", e);
                    }

                // KFreon: Write Textures
                bin.Write(TexCount);
                for (int i = 0; i < TexCount; i++)
                {
                    TreeTexInfo tex = Textures[i];

                    // KFreon: Set texname if unknown - Prevents crashes on broken texture
                    if (String.IsNullOrEmpty(tex.EntryName))
                        tex.EntryName = "UNKNOWN";


                    bin.Write(tex.EntryName);

                    bin.Write(tex.Hash);

                    string fullpackage = tex.FullPackage;
                    if (String.IsNullOrEmpty(fullpackage))
                        fullpackage = "Base Package";
                    bin.Write(fullpackage);

                    string thumbpath = tex.ThumbnailPath != null ? tex.ThumbnailPath.Split('\\').Last() : "placeholder.ico";
                    bin.Write(thumbpath);

                    bin.Write(tex.NumMips);
                    bin.Write((int)tex.Format);
                    bin.Write(tex.PCCs.Count);


                    foreach (PCCEntry entry in tex.PCCs)
                    {
                        string tempfile = entry.File;
                        tempfile = tempfile.Remove(0, MEExDirecs.BasePath.Length + 1);

                        // KFreon: Write file entry
                        bin.Write(tempfile);

                        // KFreon: Write corresponding expID
                        bin.Write(entry.ExpID);
                    }
                }
            }
        }

        public void ConstructTree()
        {
            if (TreeTexes.Count != 0)
            {
                OnPropertyChanged("TreeTexes");
                OnPropertyChanged("NumTreeTexes");
                DebugOutput.PrintLn("TOTAL ME" + GameVersion + " TEXTURES: " + NumTreeTexes);
                return;
            }


            List<HierarchicalTreeTexes> tempTreeTexes = new List<HierarchicalTreeTexes>((int)(Textures.Count / 2));

            foreach (TreeTexInfo tex in Textures)
            {
                string[] packages = tex.FullPackage.Split('.');

                // KFreon: Recursively find correct node to add to
                HierarchicalTreeTexes found = FindCorrectNode(tempTreeTexes, packages, 0);

                if (found == null)
                {
                    HierarchicalTreeTexes top = null;
                    HierarchicalTreeTexes curr = null;
                    foreach (string pack in packages)
                    {
                        HierarchicalTreeTexes temp = new HierarchicalTreeTexes(pack);

                        if (curr == null)
                        {
                            curr = temp;
                            top = temp;
                        }
                        else
                        {
                            temp.Parent = curr;
                            curr.TreeTexes.Add(temp);
                            curr = temp;
                        }
                    }

                    if (curr.Textures == null)
                        Dispatcher.CurrentDispatcher.Invoke(() => curr.Textures = new ObservableCollection<TreeTexInfo>());

                    tex.Parent = curr;
                    curr.Textures.Add(tex);
                    tempTreeTexes.Add(top);
                }
                else
                {
                    if (found.Textures == null)
                        Dispatcher.CurrentDispatcher.Invoke(() => found.Textures = new ObservableCollection<TreeTexInfo>());

                    tex.Parent = found;
                    found.Textures.Add(tex);
                }
            }

            // KFreon: Sort tree
            tempTreeTexes.Sort((tex1, tex2) => tex1.Name.CompareTo(tex2.Name));
            Dispatcher.CurrentDispatcher.Invoke(() => TreeTexes.AddRange(tempTreeTexes));

            int count = 0;
            foreach (var tex in TreeTexes)
                count += tex.FullTexCount;

            NumTreeTexes = count;
            DebugOutput.PrintLn("TOTAL ME" + GameVersion + " TEXTURES: " + NumTreeTexes);
        }

        public HierarchicalTreeTexes FindCorrectNode(ICollection<HierarchicalTreeTexes> texes, string[] packs, int index)
        {
            if (index < packs.Length)
            {
                string pack = packs[index];
                foreach (HierarchicalTreeTexes treet in texes)
                {
                    if (pack == treet.Name)
                    {
                        HierarchicalTreeTexes te = FindCorrectNode(treet.TreeTexes, packs, (index + 1));
                        if (te == null)
                            return treet;
                        else
                            return te;
                    }
                }
            }
            return null;
        }

        public void LoadTree(string filename = null)
        {
            Exists = false;
            Valid = false;
            AdvancedFeatures = false;
            if (File.Exists(filename ?? TreeLocation))
            {
                Exists = true;
                try
                {
                    FileStream fs = new FileStream(filename ?? TreeLocation, FileMode.Open, FileAccess.Read);
                    MemoryTributary mem = UsefulThings.General.DecompressStream(fs);
                    if (mem == null)
                    {
                        ReadTree(fs);
                        fs.Dispose();
                    }
                    else
                    {
                        fs.Dispose();
                        using (mem)
                        {
                            ReadTree(mem);
                        }
                    }
                    
                    Valid = true;
                }
                catch (Exception e)
                {
                    PCCs.Clear();
                    DebugOutput.PrintLn("Failed to load tree. Reason: ", "TreeDB Load", e);
                }
            }

            Task.Run(() =>
            {
                foreach (TreePCC pcc in PCCs)
                    if (File.Exists(pcc.Name))
                    {
                        pcc.Exists = true;

                        if (pcc.ScannedDate != null)
                            pcc.ValidDate = new FileInfo(pcc.Name).LastWriteTime.CompareTo(pcc.ScannedDate) == -1;
                    }
            });
        }

        private void ReadTree(Stream input)
        {
            input.Seek(0, SeekOrigin.Begin);
            using (BinaryReader bin = new BinaryReader(input))
            {
                int numTexes = bin.ReadInt32();
                if (numTexes == 1991)
                {
                    // KFreon: Pre WPF tree. ~rev 686
                    AdvancedFeatures = null;
                    numTexes = bin.ReadInt32();
                    DebugOutput.PrintLn("Medium ME" + GameVersion + " Tree features detected.");
                }
                else if (numTexes == 631991)
                {
                    // KFreon: WPF Tree
                    AdvancedFeatures = true;
                    numTexes = bin.ReadInt32();
                    DebugOutput.PrintLn("Advanced ME" + GameVersion + " Tree features detected.");
                }
                else
                    DebugOutput.PrintLn("Advanced ME" + GameVersion + " Tree features disabled.");


                if (AdvancedFeatures == true)
                {
                    // KFreon: numTexes = numPCCs here
                    for (int i = 0; i < numTexes; i++)
                    {
                        TreePCC pcc = new TreePCC();
                        string test = bin.ReadString();
                        pcc.Name = Path.Combine(MEExDirecs.BasePath, test);
                        pcc.ScannedDate = DateTime.FromBinary(bin.ReadInt64());
                        PCCs.Add(pcc);
                    }

                    numTexes = bin.ReadInt32();            
                }


                for (int i = 0; i < numTexes; i++)
                {
                    TreeTexInfo tempStruct = new TreeTexInfo(GameVersion, MEExDirecs.PathBIOGame);

                    tempStruct.EntryName = ReadString(bin);
                    tempStruct.Hash = bin.ReadUInt32();
                    tempStruct.FullPackage = ReadString(bin);


                    if (AdvancedFeatures != false)
                    {
                        string thum = ReadString(bin);
                        tempStruct.ThumbnailPath = Path.Combine(MEExDirecs.ThumbnailCache, thum);
                    }

                    tempStruct.NumMips = bin.ReadInt32();

                    if (AdvancedFeatures != true)
                    {
                        string format = ReadString(bin);
                        tempStruct.Format = KFreonLibME.Textures.Methods.ParseTextureFormat(format);
                    }
                    else
                        tempStruct.Format = (TextureFormat)bin.ReadInt32();

                    int numFiles = bin.ReadInt32();

                    if (AdvancedFeatures != true)
                    {
                        List<string> pccs = new List<string>(numFiles);
                        for (int j = 0; j < numFiles; j++)
                        {
                            string tempStr = ReadString(bin);
                            pccs.Add(Path.Combine(MEExDirecs.BasePath, tempStr));
                        }

                        List<int> ExpIDs = new List<int>(numFiles);
                        for (int j = 0; j < numFiles; j++)
                            ExpIDs.Add(bin.ReadInt32());


                        tempStruct.PCCs.AddRange(PCCEntry.PopulatePCCEntries(pccs, ExpIDs));
                    }
                    else
                    {
                        List<PCCEntry> tempEntries = new List<PCCEntry>(numFiles);
                        for (int j = 0; j < numFiles; j++)
                        {
                            string file = ReadString(bin);
                            file = Path.Combine(MEExDirecs.BasePath, file);

                            int expID = bin.ReadInt32();

                            PCCEntry entry = new PCCEntry(file, expID);
                            tempEntries.Add(entry);
                        }
                        tempStruct.PCCs.AddRange(tempEntries);
                    }

                    Textures.Add(tempStruct);
                }
            }
        }

        private string ReadString(BinaryReader bin)
        {
            if (AdvancedFeatures == true)
                return bin.ReadString();
            else
            {
                int length = bin.ReadInt32();
                char[] str = bin.ReadChars(length);
                return new string(str);
            }
        }

        public void AddPCCs(List<string> files)
        {
            files.RemoveAll(file => file.ToUpperInvariant().EndsWith(".TFC"));

            List<TreePCC> temp = new List<TreePCC>();
            DateTime now = DateTime.Now;
            files.ForEach(file => temp.Add(new TreePCC(file, now)));

            PCCs.AddRange(temp);
        }

        public void AddPCC(string file)
        {
            if (!file.ToUpperInvariant().EndsWith(".TFC"))
                PCCs.Add(new TreePCC(file, DateTime.Now)); 
        }

        private TreeTexInfo CheckTreeAddition(TreeTexInfo tex, string UpperCaseFilenameOnly)
        {
            int count = 0;
            foreach (TreeTexInfo treeTex in Textures)
            {
                count++;
                if (treeTex.Compare(tex, UpperCaseFilenameOnly))
                    return treeTex;
            }
            
            return null;
        }

        public bool AddTex(TreeTexInfo tex, string UpperCaseFilenameOnly)
        {
            bool Added = true;
            lock (RunningLocker)
            {
                TreeTexInfo treeTex = CheckTreeAddition(tex, UpperCaseFilenameOnly);
                if (treeTex != null)
                {
                    treeTex.Update(tex);
                    Added = false;
                }
                else
                    Textures.Add(tex);
            }

            return Added;
        }

        internal void Delete()
        {
            if (Textures != null)
                Textures.Clear();

            if (PCCs != null)
                PCCs.Clear();

            if (TreeTexes != null)
                TreeTexes.Clear();

            if (DLCExtractionDates != null)
                DLCExtractionDates.Clear();

            NumTreeTexes = 0;
        }



        internal void UpdateDLCDate(string dlcname, DateTime extractionDate)
        {
            if (DLCExtractionDates.ContainsKey(dlcname))
                DLCExtractionDates[dlcname] = extractionDate;
            else
                DLCExtractionDates.Add(dlcname, extractionDate);
        }

        internal DateTime GetDLCExtractionDate(string dlcname)
        {
            DateTime dlctime = DateTime.MinValue;
            switch(GameVersion)
            {
                case 1:
                case 2:
                    dlctime = new DateTime(2011, 2, 1);
                    break;
                case 3:
                    if (DLCExtractionDates.ContainsKey(dlcname))
                        dlctime = DLCExtractionDates[dlcname];
                break;
            }

            return dlctime;
        }
    }
}
