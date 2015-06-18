using KFreonLibGeneral.Debugging;
using KFreonLibME.Textures;
using SaltTPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using UsefulThings.WPF;
using ZipEntry = SaltTPF.ZipReader.ZipEntryFull;
using UsefulThings;
using KFreonLibME.Scripting.ModMaker;
using ResILWrapper;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Windows.Data;
using System.Diagnostics;


namespace KFreonLibME.ViewModels
{
    public class KFreonTPFToolsViewModel : MEViewModelBase
    {
        public string TempPath 
        {
            get
            {
                return Path.Combine(UsefulThings.General.GetExecutingLoc(), "TPFToolsTEMP");
            }
        }


        BitmapImage preview = null;
        public BitmapImage Preview
        {
            get
            {
                return preview;
            }
            set
            {
                SetProperty(ref preview, value);
            }
        }

        public bool RequiresAutoFix
        {
            get
            {
                if (Textures != null && IsAnalysed)
                    return Textures.Where(t => !t.isDef && !t.ValidTexture).Count() > 0;

                return false;
            }
        }

        bool? mainListFilters = null;
        public bool? MainListFilters
        {
            get
            {
                return mainListFilters;
            }
            set
            {
                SetProperty(ref mainListFilters, value);
                ItemsView.Refresh();
            }
        }


        #region Properties
        bool isLoaded = false;
        public bool IsLoaded
        {
            get
            {
                return isLoaded;
            }
            set
            {
                SetProperty(ref isLoaded, value);
            }
        }

        bool isAnalysing = false;
        public bool IsAnalysing
        {
            get 
            {
                return isAnalysing; 
            }
            set 
            { 
                SetProperty(ref isAnalysing, value);
            }
        }

        bool isAnalysed = false;
        public bool IsAnalysed
        {
            get
            {
                return isAnalysed;
            }
            set
            {
                SetProperty(ref isAnalysed, value);
            }
        }

        public RangedObservableCollection<TPFTexInfo> Textures { get; set; }
        public RangedObservableCollection<TreeDB> Trees { get; set; }
        List<ZipReader> Zippys = new List<ZipReader>();

        bool multiSelected = false;
        public bool MultiSelected
        {
            get
            {
                return multiSelected;
            }
            set
            {
                SetProperty(ref multiSelected, value);
            }
        }

        bool thumbsGenInit = false;
        public bool ThumbsGenInit
        {
            get
            {
                return thumbsGenInit;
            }
            set
            {
                SetProperty(ref thumbsGenInit, value);
            }
        }

        public ICommand CancelThumbsCommand { get; set; }

        public TreeDB CurrentTree
        {
            get
            {
                if (Trees != null && Trees.Count != 0)
                    foreach (TreeDB tree in Trees)
                        if (tree.IsSelected)
                            return tree;
                
                return null;
            }
        }

        int secondaryProgress = 0;
        public int SecondaryProgress
        {
            get
            {
                return secondaryProgress;
            }
            set
            {
                SetProperty(ref secondaryProgress, value);
            }
        }

        int maxSecondary = 0;
        public int MaxSecondaryProgress
        {
            get
            {
                return maxSecondary;
            }
            set
            {
                SetProperty(ref maxSecondary, value);
            }
        }

        bool secondaryIndeterminate = false;
        public bool SecondaryIndeterminate
        {
            get
            {
                return secondaryIndeterminate;
            }
            set
            {
                SetProperty(ref secondaryIndeterminate, value);
            }
        }

        string secondaryStatus = null;
        public string SecondaryStatus
        {
            get
            {
                return secondaryStatus;
            }
            set
            {
                SetProperty(ref secondaryStatus, value);
            }
        }

        ICommand ChangeTreeCommand { get; set; }


        #endregion Properties

        public Action<TPFTexInfo, TextureFormat> ExtractConvertDelegate { get; private set; }  // KFreon: Passed in from code-behind. If null, get's ignored, thus unit testible.
        public Action<TPFTexInfo> ReplaceDelegate { get; private set; }     // KFreon: Passed in from code-behind. If null, get's ignored, thus unit testible.
        public MESearchViewModel<TPFTexInfo> Search { get; set; }
        public Action<TPFTexInfo> AutoFixDelegate { get; private set; }
        public Action<TPFTexInfo> InstallDelegate { get; private set; }

        Action<string> PrimaryStatusUpdater = null;
        Action<int> PrimaryProgressUpdater = null;
        Action<int> MaxPrimaryUpdater = null;
        Action<bool> PrimaryIndeterminateUpdater = null;

        public KFreonTPFToolsViewModel(Action<TPFTexInfo, TextureFormat> extractDelegate = null, Action<TPFTexInfo> replaceDelegate = null)
            : base(Properties.Settings.Default.TPFToolsGameVersion)
        {
            PrimaryStatusUpdater = t =>
                {
                    PrimaryStatus = t;
                };
            PrimaryProgressUpdater = t => PrimaryProgress = t;
            MaxPrimaryUpdater = t => MaxPrimaryProgress = t;
            PrimaryIndeterminateUpdater = t => PrimaryIndeterminate = t;

            ExtractConvertDelegate = extractDelegate;
            ReplaceDelegate = t => 
            {
                replaceDelegate(t);
                OnPropertyChanged("RequiresAutoFix");
            };

            InstallDelegate = element =>
            {
                TPFTexInfo tex = (TPFTexInfo)element;
                if (tex.ValidTexture)
                    TexplorerViewModel.InstallTextures(GameVersion, MEExDirecs, PrimaryIndeterminateUpdater, PrimaryStatusUpdater, PrimaryProgressUpdater, MaxPrimaryUpdater, tex);
            };

            // KFreon: Setup some GUI related commands
            ChangeTreeCommand = new UsefulThings.WPF.CommandHandler(t =>
                {
                    ChangeTree(t, Trees);
                }, true);

            CancelThumbsCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                ThumbsGenInit = false;
            }, true);

            AutoFixDelegate = async t =>
            {
                await this.AutoFix(t);
                PrimaryStatus = t.ValidTexture ? "Successfully fixed!" : "Failed to fix :(";
            };


            // KFreon: Setup main data storage centers
            Textures = new RangedObservableCollection<TPFTexInfo>();
            ItemsView = CollectionViewSource.GetDefaultView(Textures);
            Trees = new RangedObservableCollection<TreeDB>() { new TreeDB(MEExDirecs, 1, MEExDirecs.GameVersion == 1, ChangeTreeCommand, true), new TreeDB(MEExDirecs, 2, MEExDirecs.GameVersion == 2, ChangeTreeCommand, true), new TreeDB(MEExDirecs, 3, MEExDirecs.GameVersion == 3, ChangeTreeCommand, true) };

            // KFreon: Setup search
            Search = new MESearchViewModel<TPFTexInfo>(Textures, ItemsView);
          
            ItemsView.Filter = t =>
            {
                TPFTexInfo tex = (TPFTexInfo)t;

                // KFreon: No filters/filters not yet applicable
                if (MainListFilters == null || !IsAnalysed || !RequiresAutoFix)
                    return true && tex.IsSearchVisible; // KFreon: Filter with seach
                
                // KFreon: XOR to get filter result
                return (MainListFilters == true ^ tex.ValidTexture) && tex.IsSearchVisible;  // KFreon: Filter with search
            };

            // KFreon: Remove temp directory
            if (Directory.Exists(TempPath))
            {
                PrimaryStatus = "Attempting to cleanup temporary files...";

                // KFreon: Can cleanup on background thread as temp files not going to be in use until Analysing at least.
                Task.Run(() =>      
                {
                    try
                    {
                        Directory.Delete(TempPath, true);
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("Unable to remove temp files: ", "TPFTools ctor", e);
                    }
                });
            }

            BeginTreeLoading();
        }


        /// <summary>
        /// Loads tree asynchronously. Waits internally for all trees to be finished loading.
        /// </summary>
        private async void BeginTreeLoading()
        {
            Busy = true;
            PrimaryStatus = "Loading Trees...";

            await Task.Run(() => base.LoadTrees(Trees, CurrentTree, false));

            PrimaryStatus = "Ready!";
            Busy = false;
        }


        
        /// <summary>
        /// Loads textures from TPF. Returns textures found.
        /// </summary>
        /// <param name="TPF">Path to TPF file to load.</param>
        private List<TPFTexInfo> LoadTPF(string TPF)
        {
            // KFreon: Open TPF and set some properties
            SaltTPF.ZipReader zippy = new SaltTPF.ZipReader(TPF);
            zippy.Description = "Filename:  \n" + zippy._filename + "\n\nComment:  \n" + zippy.EOFStrct.Comment + "\nNumber of stored files:  " + zippy.Entries.Count;
            zippy.Scanned = false;
            Zippys.Add(zippy);

            int numEntries = zippy.Entries.Count;
            List<TPFTexInfo> entries = new List<TPFTexInfo>(numEntries);

            // KFreon: Set progress
            MaxSecondaryProgress = numEntries - 1;  // KFreon: Don't include the .log
            SecondaryProgress = 0;

            // KFreon: Load texture details from internal .log
            List<string> Lines = new List<string>(50);
            try
            {
                byte[] data = zippy.Entries[numEntries - 1].Extract(true);
                StringBuilder temp = new StringBuilder(100);
                for (int i = 0; i < data.Length; i++)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    // KFreon: Ignore some chars and split on newlines
                    char c = (char)data[i];
                    if (c == '\n')
                    {
                        Lines.Add(temp.ToString());
                        temp.Clear();
                        continue;
                    }

                    if (c != '\0' && c != '\r')
                        temp.Append(c);
                }
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to read TPF details: ", "TPFTools LoadTPF", e);
                return entries;
            }

            // KFreon: Remove duplicates
            Lines = Lines.Distinct().ToList(Lines.Count);


            for (int i = 0; i < numEntries; i++)
            {
                SecondaryStatus = String.Format("Loading {0}/{1}", i + 1, numEntries);

                // KFreon: Add TPF entries to TotalTexes list
                TPFTexInfo tmpTex = LoadTex(zippy.Entries[i].Filename, null, i, zippy, Lines);
                

                // KFreon: If hash gen failed, notify
                if (!tmpTex.isDef && tmpTex.Hash == 0)
                    DebugOutput.PrintLn("Failure to get hash for entry " + i + " in " + TPF);

                entries.Add(tmpTex);
                SecondaryProgress++;
            }

            return entries;
        }


        /// <summary>
        /// Gets Texmod hash out of TPF internal .log line entry. Returns 0 if none found.
        /// </summary>
        /// <param name="line">Line in TPF .log</param>
        /// <param name="filename">filename as seen in .log line</param>
        private uint GetHashFromLine(string line, string filename) 
        {
            int index = -1;
            index = line.IndexOf(filename, StringComparison.OrdinalIgnoreCase);
            if ((index) > 0)
                return KFreonLibME.Textures.Methods.FormatTexmodHashAsUint(line);

            return 0;
        }


        /// <summary>
        /// Loads non TPF based image, i.e. any normal image such as dds, jpg, bmp, etc.
        /// Returns texture entry or null if failed.
        /// </summary>
        /// <param name="file">Path to texture to load.</param>
        /// <param name="isDef">True = file is a .log file, false = texture image.</param>
        private TPFTexInfo LoadExternal(string file)
        {
            MaxSecondaryProgress = 0;
            PrimaryStatus = "Loading external file: " + file;

            TPFTexInfo tex = LoadTex(file);

            if (!tex.isDef && tex.Hash == 0)
                DebugOutput.PrintLn("Failed to get hash from {0}", file);

            return tex;
        }


        private TPFTexInfo LoadTex(string file)
        {
            return LoadTex(file, file.GetDirParent(), -1, null, null);
        }

        private TPFTexInfo LoadTex(string file, string path, int tpfind, ZipReader zippy, List<string> Lines)
        {
            TPFTexInfo tempTex = new TPFTexInfo(file, path, tpfind, zippy, GameVersion)
            {
                PathBIOGame = MEExDirecs.PathBIOGame,
                ExtractConvertDelegate = this.ExtractConvertDelegate,
                ReplaceDelegate = this.ReplaceDelegate,
                InstallDelegate = this.InstallDelegate,
                AutoFixDelegate = async t =>
                {
                    await this.AutoFix(t);
                    PrimaryStatus = t.ValidTexture ? "Successfully fixed!" : "Failed to fix :(";
                }
            };

            if (tempTex.isDef && zippy != null)
                tempTex.LogContents.AddRange(Lines);
            else
            {
                tempTex.Hash = GetHash(file, zippy == null, Lines);
                tempTex.OriginalHash = tempTex.Hash;

                if (!tempTex.isDef && tempTex.Hash == 0)
                    DebugOutput.PrintLn("Failure to get hash for entry {0} in {1}.", file, path ?? zippy._filename);
            }

            return tempTex;
        }

        private uint GetHash(string filename, bool external, List<string> Lines)
        {
            uint hash = 0;
            if (external)
            {
                int hashInd = filename.IndexOf("0x", StringComparison.OrdinalIgnoreCase);
                if (hashInd > 0)
                    hash = KFreonLibME.Textures.Methods.FormatTexmodHashAsUint(filename.Substring(hashInd, 10));
                else
                {
                    foreach (var tex in Textures)
                    {
                        if (tex.isDef)
                        {
                            hash = FindHashInDef(tex.LogContents, filename);
                            if (hash != 0)
                                break;
                        }
                    }
                }
            }
            else
                hash = FindHashInDef(Lines, filename);

            return hash;
        }

        private uint FindHashInDef(List<string> Lines, string filename)
        {
            uint hash = 0;
            foreach (var line in Lines)
            {
                hash = GetHashFromLine(line, filename);
                if (hash != 0)
                    break;
            }
            

            return hash;
        }

        
        /// <summary>
        /// Loads textures from given list of files. Can be mixed, TPF or external. 
        /// Returns awaitable task.
        /// </summary>
        /// <param name="Files">List of files to load textures from.</param>
        public async Task LoadFiles(IEnumerable<string> Files)
        {
            int numFiles = Files.Count();
            Busy = true;
            PrimaryProgress = 0;
            MaxPrimaryProgress = numFiles;


            List<TPFTexInfo> ProcessedFiles = null;
            
            try
            {
                ProcessedFiles = await Task<List<TPFTexInfo>>.Run(() =>
                    {
                        return ProcessLoadingFiles(Files, numFiles);
                    });
            } 
            catch(Exception e)
            {
                DebugOutput.PrintLn("Failed to load files: ", "TPFTools LoadFiles", e);
            }

            // KFreon: Add files to list
            Textures.AddRange(ProcessedFiles);

            PrimaryIndeterminate = true;
            MaxSecondaryProgress = 0;
            PrimaryStatus = "Getting details and generating thumbnails...";

            // KFreon: Generate Thumbnails with multiple threads
            ParallelOptions po = new ParallelOptions();
            po.MaxDegreeOfParallelism = NumThreads - 1;
            await Task.Run(() => Parallel.ForEach(Textures, po, t => t.EnumerateDetails()));

            OnPropertyChanged("RequiresAutoFix");

            PrimaryIndeterminate = false;
            PrimaryStatus = "Loaded!";

            IsLoaded = true;
            Busy = false;
        }


        /// <summary>
        /// Load textures from mixed format files (TPF, jpg, bmp, etc). Returns valid textures found.
        /// </summary>
        /// <param name="Files>Files to load textures from.</param>
        /// <param name="numFiles">Number of files to load textures from.</param>
        private List<TPFTexInfo> ProcessLoadingFiles(IEnumerable<string> Files, int numFiles)
        {
            List<TPFTexInfo> entries = new List<TPFTexInfo>();
            foreach (string file in Files)
            {
                cts.Token.ThrowIfCancellationRequested();

                PrimaryStatus = String.Format("Loading {0} textures from {1}.", numFiles, Path.GetFileName(file));

                string ext = Path.GetExtension(file).ToLowerInvariant();
                switch (ext)
                {
                    case ".tpf":
                    case ".metpf":
                        entries.AddRange(LoadTPF(file));
                        break;
                    case ".dds":
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                    case ".bmp":
                        entries.Add(LoadExternal(file));
                        break;
                    case ".log":
                    case ".txt":
                    case ".def":
                        entries.Add(LoadExternal(file));
                        break;
                    case ".mod":
                        entries.AddRange(LoadMOD(file));
                        break;
                    default:
                        DebugOutput.PrintLn("File: " + file + " is unsupported.");
                        break;
                }

                PrimaryProgress++;
            }

            return entries;
        }


        /// <summary>
        /// Loads textures from a .MOD file. 
        /// Returns valid textures.
        /// </summary>
        /// <param name="file">Path to .MOD file.</param>
        public List<TPFTexInfo> LoadMOD(string file)
        {
            PrimaryProgress = 0;
            List<TPFTexInfo> entries = new List<TPFTexInfo>();
            List<ModJob> tempJobs = new List<ModJob>();
            KFreonLibME.Scripting.ModMakerHelper.LoadDotMod(file, null, null, null, null, MEExDirecs.ExecFolder, MEExDirecs.BIOGames, tempJobs);

            MaxPrimaryProgress = tempJobs.Count;

            int count = 1;
            foreach (ModJob job in tempJobs)
            {
                cts.Token.ThrowIfCancellationRequested();

                PrimaryStatus = String.Format("Processing Job: {0}  {1}/{2} from {3}", job.ObjectName, count++, tempJobs.Count, Path.GetFileName(file));
                if (job.JobType == "TEXTURE")
                {
                    Debug.WriteLine("");
                }
                else
                    DebugOutput.PrintLn(String.Format("Job: {0} isn't a texture. Ignoring...", job.Name));
                
            }

            return entries;
        }

        internal void Clear()
        {
            foreach (var tex in Textures)
                if (tex.IsSelected)
                    tex.Thumbnail = null;

            Textures.Clear();
            Zippys.Clear();

            PrimaryProgress = 0;
            PrimaryIndeterminate = false;
            MaxPrimaryProgress = 0;

            SecondaryIndeterminate = false;
            SecondaryProgress = 0;
            MaxSecondaryProgress = 0;

            IsLoaded = false;
            IsAnalysed = false;

            PrimaryStatus = "Cleared Entries!";

            GC.Collect();
        }

        private void DealWithFileDuplicates()
        {
            for (int i = 0; i < Textures.Count - 1; i++)
            {
                TPFTexInfo tex1 = Textures[i];
                for (int j = (i + 1); j < Textures.Count; j++)
                {
                    TPFTexInfo tex2 = Textures[j];
                    if (tex1.Compare(tex2))
                        tex1.FileDuplicates.Add(tex2);
                }
            }
        }

        private void FindTreeMatches()
        {
            for (int i = Textures.Count - 1; i >= 0; i--)
            {
                if (!Textures[i].isDef)
                    Textures.InsertRange(i, UpdateTPFTexWithTree(Textures[i]));
            }
        }

        private List<TPFTexInfo> UpdateTPFTexWithTree(TPFTexInfo tpftex)
        {
            List<TPFTexInfo> NewTexes = new List<TPFTexInfo>();
            TPFTexInfo temp = null;
            foreach (TreeTexInfo treetex in CurrentTree.Textures)
            {
                temp = tpftex.UpdateFromTreeTex(treetex);
                if (temp != null)
                    NewTexes.Add(temp);
            }

            return NewTexes;
        }

        public void AnalyseWithTexplorer()
        {
            if (IsAnalysed)
                return;

            bool success = false;
            PrimaryProgress = 0;

            Busy = true;
            IsAnalysing = true;

            // KFreon: Analyse only if tree is available
            if (CurrentTree.Valid)
            {
                PrimaryStatus = "Analysing...";
                PrimaryIndeterminate = true;

                DealWithFileDuplicates();
                FindTreeMatches();

                PrimaryIndeterminate = false;
                success = true;
            }

            PrimaryStatus = success ? "Analysis complete." : "Analysis failed. Tree doesn't exist.";
            PrimaryProgress = MaxPrimaryProgress;


            // KFreon: Set finishing properties
            Busy = false;
            IsAnalysing = false;
            IsAnalysed = success;
        }

        public async Task InstallTextures()
        {
            if (IsAnalysed)
            {
                Busy = true;
                TPFTexInfo[] temptexes = Textures.Where(t => t.ValidTexture).ToArray();
                var pccs = temptexes.Select(t => t.PCCs.Select(j => j.File));
                pccs = pccs.Distinct();

                Debug.WriteLine("===============");
                foreach (var pcc in pccs)
                    Debug.WriteLine(pcc);
                Debug.WriteLine("===============");


                if (temptexes.Length > 0)
                    await TexplorerViewModel.InstallTextures(GameVersion, MEExDirecs, PrimaryIndeterminateUpdater, PrimaryStatusUpdater, PrimaryProgressUpdater, MaxPrimaryUpdater, temptexes);
                else
                    PrimaryStatus = "No textures suitable to install!";

                Busy = false;
            }
            else
            {
                PrimaryStatus = "Analysis incomplete! No tree?";
            }
        }

        public override void ChangeTree(object NewTreeSelected, IList<TreeDB> Trees)
        {
            base.ChangeTree(NewTreeSelected, Trees);

            Properties.Settings.Default.TPFToolsGameVersion = CurrentTree.GameVersion;
            Properties.Settings.Default.Save();
        }

        internal async void GetPreview(TPFTexInfo tPFTexInfo)
        {
            Preview = await tPFTexInfo.GetPreview();
        }

        internal async Task AutoFix(params TPFTexInfo[] texes)
        {
            Busy = true;


            List<TPFTexInfo> TempTexes = null;
            if (texes == null || texes.Length == 0)
                TempTexes = Textures.ToList(Textures.Count);
            else
                TempTexes = texes.ToList(texes.Length);

            PrimaryProgress = 0;
            MaxPrimaryProgress = TempTexes.Count;
            PrimaryIndeterminate = TempTexes.Count == 1;

            await Task.Run(() => 
            {
                foreach (TPFTexInfo tex in TempTexes)
                {
                    if (!tex.ValidTexture && !tex.isDef)
                    {
                        PrimaryStatus = "Attempting to fix: " + tex.EntryName; 

                        // KFreon: Create temp directories if required
                        Directory.CreateDirectory(tex.AutoFixPath.GetDirParent());

                        if (tex.ExtractConvert(tex.AutoFixPath, tex.ExpectedFormat, true))
                        {
                            if (!tex.Replace(tex.AutoFixPath))
                            {
                                DebugOutput.PrintLn("Texture replace failed: " + tex.EntryName);
                            }
                        }
                        else
                        {
                            DebugOutput.PrintLn("Failed to extract/convert texture: " + tex.EntryName + ". Reason: " + ResILImage.GetResILError());
                        }
                    }

                    PrimaryProgress++;
                }
            });

            PrimaryStatus = "Fixing complete!";
            PrimaryIndeterminate = false;
            Busy = false;
        }

        internal void RemoveEntry(TPFTexInfo tex)
        {
            if (tex != null)
                Textures.Remove(tex);
        }
    }
}
