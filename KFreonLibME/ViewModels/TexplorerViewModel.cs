using KFreonLibGeneral.Debugging;
using KFreonLibME.Misc;
using KFreonLibME.PCCObjects;
using KFreonLibME.Scripting.ModMaker;
using KFreonLibME.Textures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UsefulThings;
using UsefulThings.WPF;

namespace KFreonLibME.ViewModels
{   
    public class TexplorerViewModel : MEViewModelBase
    {
        public ObservableCollection<Error> AllErrors { get; set; }

        public ICommand CheckAllCommand { get; set; }
        public ICommand UncheckAllCommand { get; set; }

        public RangedObservableCollection<TreeComparisonItem> TreeComparisonMismatches { get; set; }


        bool hasErrors = false;
        public bool HasErrors 
        {
            get
            {
                return hasErrors;
            }
            set
            {
                SetProperty(ref hasErrors, value);
            }
        }

        int dlcfilescount = 0;
        public int DLCFilesCount
        {
            get
            {
                return dlcfilescount;
            }
            set
            {
                SetProperty(ref dlcfilescount, value);
            }
        }

        public static bool CheckBaseGameEntryVisibility(object t, bool NewOnly, bool ModifiedOnly)
        {
            if (t == null)
                return true;

            bool visible = true;

            FileEntry entry = (FileEntry)t;
            if (ModifiedOnly)
                visible = entry.IsScanned && entry.ValidDate == true;
            else if (NewOnly)
                visible = !entry.IsScanned;


            visible = entry.IsSearchVisible && visible;
            return visible;
        }

        public static bool CheckDLCEntryVisibility(object t, bool NewOnly, bool ModifiedOnly)
        {
            if (t == null)
                return true;

            bool visible = false;

            DLCFileEntry dlc = (DLCFileEntry)t;

            if (!NewOnly && !ModifiedOnly || dlc.Files.Count == 0)
                visible = true;

            foreach (FileEntry entry in dlc.Files)
            {

                if (ModifiedOnly)
                    visible = entry.IsScanned && entry.ValidDate == true;
                else if (NewOnly)
                    visible = !entry.IsScanned;

                visible = entry.IsSearchVisible && visible;

                if (visible)
                    break;
            }

            return visible;
        }



        public bool NewUnscannedFiles
        {
            get
            {
                if (BaseGameFiles == null || BaseGameFiles.Count == 0)
                    return false;

                return BaseGameFiles.Where(t => t != null && !t.IsScanned).Count() > 0 || DLCs == null ? true : DLCs.Where(t => TexplorerViewModel.CheckDLCEntryVisibility(t, DLCModifiedChecker == false, DLCModifiedChecker == true)).Count() > 0;
            }
        }

        bool loadingGameFiles = false;
        public bool LoadingGameFiles
        {
            get
            {
                return loadingGameFiles;
            }
            set
            {
                SetProperty(ref loadingGameFiles, value);
            }
        }

        bool? dlcmodifiedchecker = null;
        public bool? DLCModifiedChecker
        {
            get
            {
                return dlcmodifiedchecker;
            }
            set
            {
                SetProperty(ref dlcmodifiedchecker, value);
                foreach (DLCFileEntry dlc in DLCs)
                {
                    dlc.ModifiedOnly = value == true;
                    dlc.NewOnly = value == false;
                }

                DLCsView.Refresh();
            }
        }

        bool? basegamemodifiedchecker = null;
        public bool? BaseGameModifiedChecker
        {
            get
            {
                return basegamemodifiedchecker;
            }
            set
            {
                SetProperty(ref basegamemodifiedchecker, value);
                BaseGameFilesView.Refresh();
            }
        }

        #region Properties
        bool needsFTCS = false;
        public bool NeedsFTCS
        {
            get
            {
                return needsFTCS;
            }
            set
            {
                needsFTCS = value;
                OnPropertyChanged();
            }
        }

        BitmapImage img = null;
        public BitmapImage ImagePreview
        {
            get
            {
                return img;
            }
            set
            {
                SetProperty(ref img, value);
            }
        }

        public ObservableCollection<TreeDB> Trees { get; set; }
        public RangedObservableCollection<FileEntry> BaseGameFiles { get; set; }
        public ICollectionView BaseGameFilesView { get; set; }
        public RangedObservableCollection<DLCFileEntry> DLCs { get; set; }
        public ICollectionView DLCsView { get; set; }

        public TreeDB CurrentTree
        {
            get
            {
                foreach (TreeDB tree in Trees)
                    if (tree.IsSelected)
                        return tree;

                return null;
            }
        }

        public int GameVersion
        {
            get
            {
                return CurrentTree.GameVersion;
            }
        }
        #endregion

        public ICommand SaveCommand { get; set; }

        public ICommand ChangeTreeCommand { get; set; }

        public ICommand ShowTreeGameInfoPanelCommand { get; set; }
        public ICommand RevertCommand { get; set; }

        public int NumTreeTexes
        {
            get
            {
                return CurrentTree.NumTreeTexes;
            }
        }


        bool displayGameTreeInfoPanel = false;
        public bool DisplayGameTreeInfoPanel
        {
            get
            {
                return displayGameTreeInfoPanel;
            }
            set
            {
                SetProperty(ref displayGameTreeInfoPanel, value);
            }
        }


        MESearchViewModel<TreeTexInfo> search = null;
        public MESearchViewModel<TreeTexInfo> Search
        {
            get
            {
                return search;
            }
            set
            {
                SetProperty(ref search, value);
            }
        }

        FileEntrySearchViewModel<FileEntry> basegamesearch = null;

        public FileEntrySearchViewModel<FileEntry> BaseGameSearch
        {
            get
            {
                return basegamesearch;
            }
            set
            {
                SetProperty(ref basegamesearch, value);
            }
        }

        FileEntrySearchViewModel<DLCFileEntry> dlcsearch = null;

        public FileEntrySearchViewModel<DLCFileEntry> DLCSearch
        {
            get
            {
                return dlcsearch;
            }
            set
            {
                SetProperty(ref dlcsearch, value);
            }
        }

        private void ChangeFTSCheckedState(bool isBaseGame, bool state)
        {
            ICollectionView temp = BaseGameFilesView;
            if (!isBaseGame)
                temp = ((DLCFileEntry)DLCsView.CurrentItem).FilesView;

            foreach (var item in temp)
                ((FileEntry)item).IsChecked = state;

            temp.Refresh();
        }

        public TexplorerViewModel(int game = -1) : base(game == -1 ? Properties.Settings.Default.TexplorerGameVersion : game)
        {
            CheckAllCommand = new UsefulThings.WPF.CommandHandler(isBaseGame =>
            {
                ChangeFTSCheckedState(((bool?)isBaseGame) ?? false, true);
            });

            UncheckAllCommand = new UsefulThings.WPF.CommandHandler(isBaseGame =>
            {
                ChangeFTSCheckedState(((bool?)isBaseGame) ?? false, false);
            });



            AllErrors = DebugOutput.AllErrors;
            TreeComparisonMismatches = new RangedObservableCollection<TreeComparisonItem>();

            RevertCommand = new UsefulThings.WPF.CommandHandler(element =>
            {
                if (element == null)
                    return;

                RevertTexture((TreeTexInfo)element);
            }, true);


            SaveCommand = new UsefulThings.WPF.CommandHandler(async element =>
            {
                TreeTexInfo[] texes = CurrentTree.Textures.Where(t => t.HasChanged).ToArray();

                if (texes.Length == 0)
                {
                    PrimaryStatus = "No changes detected!";
                    return;
                }

                Action<string> PrimaryStatusUpdater = status => PrimaryStatus = status;
                Action<int> PrimaryProgressUpdater = progress => PrimaryProgress = progress;
                Action<int> MaxPrimaryUpdater = max => MaxPrimaryProgress = max;
                Action<bool> PrimaryIndeterminateUpdater = indeterminate => PrimaryIndeterminate = indeterminate;

                MainStopWatch.Start();
                await InstallTextures(GameVersion, MEExDirecs, PrimaryIndeterminateUpdater, PrimaryStatusUpdater, PrimaryProgressUpdater, MaxPrimaryUpdater, texes);

                MainStopWatch.Stop();
                PrimaryProgress = MaxPrimaryProgress;
            }, true);

            NeedsFTCS = false;

            // KFreon: Command to change trees when buttons clicked
            ChangeTreeCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                ChangeTree(t, Trees);
                LoadStuff(true);
            }, true);

            ShowTreeGameInfoPanelCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                DisplayGameTreeInfoPanel = (bool)t;
            }, true);

            Trees = new ObservableCollection<TreeDB>() { new TreeDB(MEExDirecs, 1, MEExDirecs.GameVersion == 1, ChangeTreeCommand, true), new TreeDB(MEExDirecs, 2, MEExDirecs.GameVersion == 2, ChangeTreeCommand, true), new TreeDB(MEExDirecs, 3, MEExDirecs.GameVersion == 3, ChangeTreeCommand, true) };


            ItemsView = CollectionViewSource.GetDefaultView(CurrentTree.Textures);
            Search = new MESearchViewModel<TreeTexInfo>(CurrentTree.Textures, ItemsView, true);

            BaseGameFiles = new RangedObservableCollection<FileEntry>();
            BaseGameFilesView = CollectionViewSource.GetDefaultView(BaseGameFiles);
            BaseGameFilesView.Filter = t => TexplorerViewModel.CheckBaseGameEntryVisibility(t, BaseGameModifiedChecker == false, BaseGameModifiedChecker == true);

            BaseGameSearch = new FileEntrySearchViewModel<FileEntry>(BaseGameFiles, BaseGameFilesView);


            DLCs = new RangedObservableCollection<DLCFileEntry>();
            DLCsView = CollectionViewSource.GetDefaultView(DLCs);
            DLCsView.Filter = t => TexplorerViewModel.CheckDLCEntryVisibility(t, DLCModifiedChecker == false, DLCModifiedChecker == true);

            DLCSearch = new FileEntrySearchViewModel<DLCFileEntry>(DLCs, DLCsView);


            LoadStuff(false);
        }

        public async void LoadStuff(bool changing)
        {
            Busy = true;
            PrimaryIndeterminate = true;

            // KFreon: Begin loading trees
            await Task.Run(() =>
            {
                if (!base.LoadTrees(Trees, CurrentTree, changing))
                {
                    if (MEExDirecs.DoesGameExist(GameVersion))
                    {
                        NeedsFTCS = true;
                        PrimaryStatus = "No tree found. Beginning First Time Setup.";
                    }
                    else
                        PrimaryStatus = "ME" + GameVersion + " game files not found!";
                }
                OnPropertyChanged("NumTreeTexes");
            });
            PrimaryIndeterminate = false;
            Busy = false;
        }

        internal void GetImagePreview()
        {
            foreach (var item in CurrentTree.Textures)
                if (item.IsSelected)
                {
                    ImagePreview = UsefulThings.WPF.Images.CreateWPFBitmap(item.GetImageData());
                    break;
                }
        }

        internal async void GetCurrentBaseGame()
        {
            ConcurrentBag<FileEntry> entries = new ConcurrentBag<FileEntry>();
            DebugOutput.PrintLn(String.Format("Getting ME{0} basegame files...", GameVersion));
            await Task.Run(() =>
            {
                int basePathLength = MEExDirecs.BasePath.Length;
                List<string> test = MEExDirecs.GetBaseGameFiles(GameVersion).Where(t => !t.EndsWith(".tfc")).ToList();
                Parallel.ForEach(test, file => 
                {
                    if (file == null)
                        return;

                    // KFreon: See if pcc has been scanned previously
                    TreeDB.TreePCC pcc = null;
                    if (CurrentTree.Valid)
                        pcc = CurrentTree.PCCs.Find(p => p.Name.Contains(file.Remove(0, basePathLength)));

                    FileEntry entry = new FileEntry(file, CurrentTree.ValidYear, CurrentTree.ValidMonth, pcc != null);

                    // KFreon: Append date scanned if done so
                    if (pcc != null)
                        entry.ValidityString += pcc.ScannedDate;

                    if (entry == null)
                        Debugger.Break();
                    entries.Add(entry);
                });
            });

            BaseGameFiles.Clear();
            BaseGameFiles.AddRange(entries);

            LoadingGameFiles = false;
            BaseGameModifiedChecker = false;   // KFreon: Files not in tree only
            OnPropertyChanged("NewUnscannedFiles");
        }

        internal async void GetCurrentDLC()
        {
            List<DLCFileEntry> entries = new List<DLCFileEntry>();
            DebugOutput.PrintLn(String.Format("Getting ME{0} DLC files...", GameVersion));
            await Task.Run(() =>
            {
                int basePathLength = MEExDirecs.BasePath.Length;
                List<string> dlcs = MEDirectories.MEDirectories.GetInstalledDLC(MEExDirecs.DLCPath);
                if (dlcs == null)
                    DebugOutput.PrintLn("No DLC's Detected.");
                else
                {
                    List<string> treepccs = null;
                    if (CurrentTree.Valid)
                        treepccs = CurrentTree.PCCs.Select(pcc => pcc.Name.Replace('/','\\')).ToList(CurrentTree.PCCs.Count);

                    foreach (string directory in dlcs)
                        entries.Add(new DLCFileEntry(directory, GameVersion, CurrentTree.GetDLCExtractionDate(directory.Split('\\').Last()), file => 
                        {
                            if (treepccs == null)
                                return false;  // KFreon: Set as not scanned

                            return treepccs.Contains(file.Remove(0, basePathLength));
                        }, DLCModifiedChecker == false, DLCModifiedChecker == true));
                }
                    
            });

            DLCs.Clear();
            DLCs.AddRange(entries);

            DLCModifiedChecker = false;   // KFreon: Files not in tree only
            OnPropertyChanged("NewUnscannedFiles");


            // KFreon: Count DLC files
            int count = 0;
            foreach (var dlc in DLCs)
                count += dlc.Files.Count;

            DLCFilesCount = count;
        }

        internal void ReloadTree()
        {
            CurrentTree.LoadTree();
            if (CurrentTree.Valid)
            {
                CurrentTree.ConstructTree();
                OnPropertyChanged("NumTreeTexes");
            }
        }

        internal string ExportTree(string FileName)
        {
            string error = null;
            try
            {
                File.Copy(CurrentTree.TreeLocation, FileName);
                PrimaryStatus = "Successfully exported ME" + GameVersion + " tree";
            }
            catch (Exception e)
            {
                error = e.ToString();
                PrimaryStatus = "Failed to export ME" + GameVersion + " tree!";
                DebugOutput.PrintLn("Failed to export ME" + GameVersion + " tree! Reason: ", "Texplorer Export Tree", e);
            }
            return error;
        }

        internal string ImportTree(string FileName)
        {
            string error = null;
            try
            {
                File.Copy(FileName, CurrentTree.TreeLocation);
                ReloadTree();
                PrimaryStatus = "Successfully imported ME" + GameVersion + " tree";
            }
            catch (Exception e)
            {
                error = e.ToString();
                PrimaryStatus = "Failed to import ME" + GameVersion + " tree!";
                DebugOutput.PrintLn("Failed to import ME" + GameVersion + " tree! Reason: ", "Texplorer Import Tree", e);
            }
            return error;
        }

        internal string RemoveCurrentTree()
        {
            string error = null;
            try
            {
                DebugOutput.PrintLn("Removing current tree: " + GameVersion);
                File.Delete(CurrentTree.TreeLocation);
                CurrentTree.Delete();
                ReloadTree();
                PrimaryStatus = "Successfully removed ME" + GameVersion + " tree";
                BaseGameModifiedChecker = null;
                DLCModifiedChecker = null;

                foreach (var item in BaseGameFiles)
                    item.IsScanned = false;

                foreach (var item in DLCs)
                    item.IsScanned = false;

                DebugOutput.PrintLn("Tree removed successfully!");
                DebugOutput.PrintLn();
                DebugOutput.PrintLn();
            }
            catch (Exception e)
            {
                error = e.ToString();
                PrimaryStatus = "Failed to remove ME" + GameVersion + " tree!";
                DebugOutput.PrintLn("Failed to remove ME" + GameVersion + " tree! Reason: ", "Texplorer Remove Current Tree", e);
            }
            return error;
        }

        internal async void BeginScan()
        {
            Busy = true;
            PrimaryStatus = "Performing Pre-Treescan operations...";
            DebugOutput.PrintLn("Performing Pre-Treescan operations...");

            // KFreon: Setup timers etc            
            MainStopWatch.Start();


            ///// KFreon: Get items to scan
            List<string> DLCSfars = null;
            await Task.Run(() => DLCSfars = PerformPreScanOperations());

            PrimaryProgress = 0;
            MaxPrimaryProgress = CurrentTree.PCCs.Count;

            ConcurrentDictionary<string, long> BaseGameFileInfos = new ConcurrentDictionary<string, long>();

            // KFreon: Scan all files
            await Task.Run(() => { ScanFiles(CurrentTree.PCCs, BaseGameFileInfos); });
            //await Task.Run(() => { ScanFiles(new List<TreeDB.TreePCC>() { new TreeDB.TreePCC(@"R:\Games\Mass Effect\BioGame\CookedPC\BIOC_Materials.u", DateTime.Now) }, BaseGameFileInfos); });


            /*KFreonLibME.TreeDB.TreePCC test = new TreeDB.TreePCC(@"R:\Games\Origin Games\Mass Effect 3\BIOGame\CookedPCConsole\GuidCache.pcc", DateTime.Today); 
            ConcurrentBag<string> errors = await Task<ConcurrentBag<string>>.Run(() => { return ScanFiles(new List<KFreonLibME.TreeDB.TreePCC>() { test }, BaseGameFileInfos); });*/

            if (cts.IsCancellationRequested)
            {
                PrimaryProgress = MaxPrimaryProgress;
                PrimaryIndeterminate = false;
                PrimaryStatus = "Tree scan cancelled!";
                return;
            }

            // KFreon: Do AutoTOC
            PrimaryStatus = "Updating TOC's...";
            PrimaryIndeterminate = true;
            await Task.Run(() => Helpers.Methods.UpdateTOCs(MEExDirecs, DLCSfars, BaseGameFileInfos));
            PrimaryIndeterminate = false;


            // KFreon: Stop timers
            MainStopWatch.Stop();

            PrimaryStatus = "'Sheparding' Tree to disk...";
            await Task.Run(() => CurrentTree.SaveTree(CurrentTree.TreeLocation));

            PrimaryStatus = "Constructing Tree...";
            await Task.Run(() => CurrentTree.ConstructTree());

            OnPropertyChanged("NumTreeTexes");

            CurrentTree.Exists = true;
            CurrentTree.AdvancedFeatures = true;
            CurrentTree.Valid = true;

            PrimaryStatus = "Tree constructed. Ready.";
            Busy = false;
        }


        private List<string> PerformPreScanOperations()
        {
            PrimaryIndeterminate = true;


            // KFreon: Close search so deleting thumbnails doesn't run into IO Exception
            Search.ClearAll();

            List<string> DLCSfars = new List<string>();

            // KFreon: Get selected basegame files
            foreach (FileEntry file in BaseGameFiles)
                if (file.IsChecked)
                    CurrentTree.AddPCC(file.Info.FullName);


            // KFreon: Get dlc items to scan
            string DLCBasePath = MEExDirecs.DLCPath;
            DebugOutput.PrintLn("DLC Path: " + DLCBasePath);

            MaxPrimaryProgress = DLCs.Count;
            PrimaryIndeterminate = false;
            PrimaryProgress = 0;
            foreach (DLCFileEntry dlc in DLCs)
            {
                if (!dlc.IsChecked)
                    continue;

                if (GameVersion == 3 && dlc.Files.Count == 0)  // KFreon: ANY valid files outside the .sfar will prevent that DLC from extracting.
                {
                    PrimaryStatus = "Extracting: " + dlc.Name;
                    // KFreon: Packed DLC. Method/Idea borrowed from The Fobs' ExtractAllDLC function in DLCExplorer2

                    DLCPackage dlcPack = new DLCPackage(dlc.FullPath);
                    List<string> dlcitems = dlcPack.UnpackContent(NumThreads, cts.Token);
                    dlcitems.ForEach(t => t = t.Replace('/', '\\'));

                    CurrentTree.AddPCCs(dlcitems);

                    // KFreon: Redefine package to get proper details? 
                    dlcPack = new DLCPackage(dlcPack.MyFileName);

                    PrimaryStatus = "Updating TOC for " + dlc.Name;
                    dlcPack.UpdateTOCbin(true);

                    CurrentTree.UpdateDLCDate(dlc.Name, DateTime.Now);
                    PrimaryProgress++;
                }
                else
                {
                    // KFreon: Previously unpacked DLC
                    foreach (FileEntry file in dlc.Files)
                        if (file.IsChecked)
                        {
                            string name = file.Info.FullName;
                            CurrentTree.AddPCC(name);
                        }
                }
                if (GameVersion == 3)
                    DLCSfars.Add(dlc.FullPath);
            }

            PrimaryStatus = "Performing Pre-Treescan operations...";
            PrimaryIndeterminate = true;

            // KFreon: Setup directories
            if (Directory.Exists(MEExDirecs.ThumbnailCache))
            {
                DebugOutput.PrintLn("Attempting to delete old thumbnails...");
                try
                {
                    Directory.Delete(MEExDirecs.ThumbnailCache, true);
                    DebugOutput.PrintLn("Thumbnails successfully cleared.");
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("ERROR removing thumbnails: ", "Texplorer Pre-Treescan", e);
                }
            }

            CreateThumbnailDirectory();
            if (!Directory.Exists(MEExDirecs.ThumbnailCache))
            {
                Console.WriteLine();
            }

            PrimaryIndeterminate = false;
            return DLCSfars;
        }

        private void CreateThumbnailDirectory()
        {
            try
            {
                Directory.CreateDirectory(MEExDirecs.ThumbnailCache);
                DebugOutput.PrintLn("Thumbnail cache created at: " + MEExDirecs.ThumbnailCache);
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("FAILED to create Thumbnail Directory. Reason: ", "Texplorer CreateThumbDirectory", e);
            }
        }


        private Task[] BeginPCCConsumers(BlockingCollection<AbstractPCCObject> PCCObjects)
        {
            int ExtraConsumerThreads = NumThreads - 1;   // KFreon: Minus UI, Producer -> NOW assuming that one of those threads can be suspended for long enough for a sensible amount of work to get done on another thread, so allowing one more thread.

            Task[] Consumers = new Task[ExtraConsumerThreads];

            // KFreon: Creates a set of consumer threads that wait for a collection to be ready to take from.
            for (int i = 0; i < ExtraConsumerThreads; i++)
            {
                Consumers[i] = Task.Factory.StartNew(token =>
                {
                    // KFreon: Waits for entry in PCCObjects to become available, then threadsafely removes and processes that entry.
                    foreach (var pcc in PCCObjects.GetConsumingEnumerable())
                    {
                        // KFreon:  Cancel if requested
                        if (cts.Token.IsCancellationRequested)
                        {
                            pcc.Dispose();
                            break;
                        }

                        try
                        {
                            List<Exception> errors = pcc.Scan(MEExDirecs.ExecFolder, CurrentTree);
                            foreach (Exception e in errors)
                                DebugOutput.PrintLn("ERROR while scanning pcc. Reason: ", "Texplorer Treescan", e);
                        }
                        catch(Exception e)
                        {
                            DebugOutput.PrintLn("ERROR while scanning pcc. Reason: ", "Texplorer Treescan", e);
                        }

                        pcc.Dispose();
                    }

                    Console.WriteLine("Done");
                }, cts.Token, TaskCreationOptions.LongRunning);
            }

            return Consumers;
        }

        private void BeginPCCProducer(List<KFreonLibME.TreeDB.TreePCC> files, BlockingCollection<AbstractPCCObject> PCCObjects, bool SingleThreaded, ConcurrentDictionary<string, long> BaseGameFileInfos)
        {
            // KFreon: Single threaded producer. Loop over all pccs and read them into memory but not all at the same time.
            int numPCCs = files.Count;
            for (int i = 0; i < numPCCs; i++)
            {
                // KFreon: Handle Cancellation
                if (cts.Token.IsCancellationRequested)
                    break;

                // KFreon: Update status and increment status
                if (i % 5 == 0)
                {
                    PrimaryStatus = String.Format("Scanning: {0} / {1}", i, numPCCs);
                    PrimaryProgress += 5;
                }

                string file = files[i].Name;

                // KFreon: Process PCC file and move to next stage in pipeline, or scan file if SingleThreaded
                try
                {
                    AbstractPCCObject pcc;
                    if (SingleThreaded)
                        using (pcc = AbstractPCCObject.Create(file, GameVersion, MEExDirecs.PathBIOGame))
                            pcc.Scan(MEExDirecs.ExecFolder, CurrentTree);
                    else
                    {
                        pcc = AbstractPCCObject.Create(file, GameVersion, MEExDirecs.PathBIOGame);
                        PCCObjects.Add(pcc);
                    }

                    if (!pcc.isDLC)
                        BaseGameFileInfos.TryAdd(pcc.pccFileName, pcc.FileLength);

                }
                catch (Exception e)
                {
                    string message = null;
                    if (e.Message.Contains("Compression Method unknown"))
                    {
                        message = "ERROR: File probably modified. Skipping: " + file;
                        Debug.WriteLine("sharp error: " + file);
                    }
                    else
                    {
                        message = "ERROR: Skipping file: " + file + "  REASON: " + e.Message;
                        Debug.WriteLine("Unknownerror: " + e.Message + "  " + file);
                    }

                    HasErrors = true;
                    DebugOutput.PrintLn(message, "Texplorer Treescan", e);
                }
            }
        }

        private void ScanFiles(List<KFreonLibME.TreeDB.TreePCC> files, ConcurrentDictionary<string, long> BaseGameFileInfos)
        {
            // KFreon: Begin Pipeline
            if (NumThreads > 2)  // KFreon: Multithread if possible
            {
                BlockingCollection<AbstractPCCObject> PCCObjects = new BlockingCollection<AbstractPCCObject>(NumThreads + 3);
                Task[] Consumers = BeginPCCConsumers(PCCObjects);
                BeginPCCProducer(files, PCCObjects, false, BaseGameFileInfos);

                // KFreon: Signal we're done and wait for consumers to finish
                PCCObjects.CompleteAdding();
                Task.WaitAll(Consumers);
            }
            else
                BeginPCCProducer(files, null, true, BaseGameFileInfos);


            // KFreon: SOMETHING TO WITH CANCELLATION
        }

        internal void Cancel()
        {
            cts.Cancel();
        }

        internal bool SetupGameFiles()
        {
            LoadingGameFiles = true;

            if (!MEExDirecs.DoesGameExist(GameVersion))
            {
                DebugOutput.PrintLn(String.Format("ERROR: ME{0} gamefiles missing!", GameVersion));
                PrimaryStatus = String.Format("ME{0} gamefiles missing!", GameVersion);

                LoadingGameFiles = false;

                return false;
            }


            // KFreon: Setup gamefiles lists
            GetCurrentBaseGame();
            GetCurrentDLC();

            return true;
        }


        internal async void ChangeTexture(string filename)
        {
            Busy = true;
            PrimaryIndeterminate = true;
            PrimaryStatus = "Installing texture...";


            // KFreon: Get Selected Texture
            TreeTexInfo tex = GetSelectedTexture();
            if (tex == null)
            {
                PrimaryStatus = "TELL KFREON! Tex was null in Add Bigger";
                return;
            }

            bool errorOccured = false;

            await Task.Run(() => 
            {
                METexture2D tex2D = null;
                try
                {
                    tex2D = tex.OrigTex2D ?? new METexture2D(tex);
                    tex2D.PopulateFromPCC();
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("Failed to create tex2d from: " + tex.EntryName, "Texplorer ChangeTexture", e);
                }

                byte[] imgData = UsefulThings.General.GetExternalData(filename);
                if (imgData == null)
                {
                    DebugOutput.PrintLn("File: " + filename + " unable to be read.", "Texplorer ChangeTexture", new IOException("File: " + filename + " in use."));
                }
                else
                {
                    try
                    {
                        tex2D.OneImageToRuleThemAll(MEExDirecs.PathBIOGame, imgData);
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("Failed to install texture: " + tex.EntryName, "Texplorer ChangeTexture", e);
                    }
                }

                // KFreon: NEW THUMBNAIL and updated properties?

                if (!errorOccured)
                    tex.ChangedTex2D = tex2D;
            });

            PrimaryStatus = errorOccured ? "Some errors occured!" : "Texture installed!";
            PrimaryIndeterminate = false;
            Busy = false;
        }

        private TreeTexInfo GetSelectedTexture()
        {
            foreach (TreeTexInfo tex in CurrentTree.Textures)
                if (tex.IsSelected)
                    return tex;

            return null;
        }


        public bool InstallSingleTexture(string texname, List<string> PCCs, List<int> IDs, byte[] imgData)
        {
            List<string> newpccs = new List<string>();
            PCCs.ForEach(p => newpccs.Add(Path.GetFullPath(Path.Combine(MEExDirecs.PathBIOGame, p))));  // KFreon: GetFullPath strips out unnecessary slashes for proper string comparison later
        

            // KFreon: Setup first pcc
            AbstractPCCObject pcc = AbstractPCCObject.Create(newpccs[0], GameVersion, MEExDirecs.PathBIOGame);
            if (!pcc.Exports[IDs[0]].ValidTextureClass())
                throw new InvalidDataException("Export not a texture!");


            // KFreon: Create texture
            METexture2D tex2D = null;
            try
            {
                tex2D = new METexture2D(texname, newpccs, IDs, MEExDirecs.PathBIOGame, GameVersion, TextureFormat.Unknown);
                tex2D.PopulateFromPCC();
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to create tex2D from: " + texname + " Reason: ", "Texplorer InstallSingleTexture", e);
            }



            // KFreon: Install texture
            try
            {
                tex2D.OneImageToRuleThemAll(MEExDirecs.PathBIOGame, imgData);
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to install texture: " + texname + " Reason: ", "Texplorer InstallSingleTexture", e);
            }


            // KFreon: Save PCC
            if (!SaveTextureInPCC(pcc, tex2D, IDs))
                Console.WriteLine();
            else
                Console.WriteLine();

            return true;
        }


        /// <summary>
        /// Saves a single texture to a PCC.
        /// </summary>
        /// <param name="pcc"></param>
        /// <param name="tex2D"></param>
        /// <param name="expIDs"></param>
        /// <returns></returns>
        private bool SaveTextureInPCC(AbstractPCCObject pcc, METexture2D tex2D, ICollection<int> expIDs)
        {
            try
            {
                foreach (int expID in expIDs)
                {
                    SaveSingleTextureFiles(tex2D.PCCs, tex2D);
                }

                return true;
            }
            catch(Exception e)
            {
                DebugOutput.PrintLn("Failed to save texture: " + tex2D.texName + " in pcc: " + pcc.pccFileName + ". Reason: ", "Texplorer SaveTexture", e);
                return false;
            }
        }


        /// <summary>
        /// Saves single texture to relevant PCCs.
        /// </summary>
        /// <param name="pccs"></param>
        /// <param name="expIDs"></param>
        /// <param name="tex2D"></param>
        private void SaveSingleTextureFiles(List<string> pccs, List<int> expIDs, METexture2D tex2D)
        {
 	        for (int i = 0; i < pccs.Count; i++)
            {
                AbstractPCCObject pcc = AbstractPCCObject.Create(pccs[i], GameVersion, MEExDirecs.PathBIOGame);
                if (String.Compare(tex2D.texName, pcc.Exports[expIDs[i]].ObjectName, true) != 0 || !pcc.Exports[expIDs[i]].ValidTextureClass())
                    throw new InvalidDataException("Export or texname incorrect!");

                // KFreon: Update and replace entry, then save file.
                pcc.UpdatePCCTextureEntry(tex2D, expIDs[i]);
                pcc.SaveToFile(pcc.pccFileName);
            }
        }


        /// <summary>
        /// Saves single texture to its relevant PCCs.
        /// </summary>
        /// <param name="pccs"></param>
        /// <param name="tex2D"></param>
        private void SaveSingleTextureFiles(List<PCCEntry> pccs, METexture2D tex2D)
        {
            SaveSingleTextureFiles(pccs.Select(t => t.File).ToList(pccs.Count), pccs.Select(t => t.ExpID).ToList(pccs.Count), tex2D);
        }


        /// <summary>
        /// Installs multiple textures with a more disk efficient approach.
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static bool InstallMutlipleTextures(int gameversion, string pathbiogame, Action<string> PrimaryStatusUpdater, Action<bool> PrimaryIndeterminateUpdater, Action<int> PrimaryProgressUpdater, Action<int> MaxPrimaryUpdater, out Dictionary<string, long> FileInfos, params AbstractTexInfo[] Texes)
        {
            List<string> Files = null;
            if (PrimaryStatusUpdater != null)
                PrimaryStatusUpdater("Preloading textures...");

            if (PrimaryIndeterminateUpdater != null)
                PrimaryIndeterminateUpdater(true);

            List<METexture2D> baseTexes = GetMETex2Ds(pathbiogame, gameversion, out Files, Texes);

            FileInfos = new Dictionary<string, long>();

            // KFreon: Loop over all files, modifying all relevent entries, then save each.
            int count = 1;
            int maxcount = Files.Count;

            if (PrimaryIndeterminateUpdater != null)
                PrimaryIndeterminateUpdater(false);

            if (MaxPrimaryUpdater != null)
                MaxPrimaryUpdater(maxcount);

            foreach (var file in Files)
            {
                if (PrimaryStatusUpdater != null)
                    PrimaryStatusUpdater("Installing textures to file: " + Path.GetFileName(file) + "  " + count++ + "/" + maxcount);

                if (PrimaryProgressUpdater != null)
                    PrimaryProgressUpdater(count);

                AbstractPCCObject pcc = null;
                try
                {
                    pcc = AbstractPCCObject.Create(file, gameversion, pathbiogame);
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("File: " + file + " couldn't be found.", "Texplorer InstallMultipleTextures", e);
                    continue;
                }

                FileInfos.Add(file, pcc.FileLength);

                foreach (var tex in baseTexes)
                {
                    if (tex.PCCs.Contains(t => t.File.Contains(pcc.pccFileName)))
                    {
                        // KFreon: Secondary progres? BAD NAME
                        pcc.UpdatePCCTextureEntry(tex, tex.GetExpID(pcc.pccFileName));  // expid?
                    }
                }

                pcc.SaveToFile(pcc.pccFileName);
            }

            return true;
        }

        private static List<METexture2D> GetMETex2Ds(string pathbiogame, int gameversion, out List<string> Files, params AbstractTexInfo[] Texes)
        {
            List<METexture2D> baseTexes = new List<METexture2D>();
            Files = new List<string>();  

            // KFreon: Load all textures into TFC's, saving relevent details for later use.
            foreach (AbstractTexInfo tex in Texes)
            {
                bool? requestPCCStored = false;
                if (gameversion == 1)
                    requestPCCStored = null;

                METexture2D NormalTex = PreGenerateMETex2D(tex, requestPCCStored);
                if (NormalTex != null)
                {
                    baseTexes.Add(NormalTex);
                    
                    // KFreon: Add to list of total files for use in install
                    foreach (var pcc in NormalTex.PCCs.Where(p => p.Using))
                        if (!Files.Contains(pcc.File))
                            Files.Add(pcc.File);
                }
                
                
                if (gameversion != 1)
                {
                    METexture2D PCCStoredTex = PreGenerateMETex2D(tex, true);
                    if (PCCStoredTex != null)
                    {
                        baseTexes.Add(PCCStoredTex);
                        
                        // KFreon: Add to list of total files for use in install
                        foreach (var pcc in PCCStoredTex.PCCs.Where(p => p.Using))
                            if (!Files.Contains(pcc.File))
                                Files.Add(pcc.File);
                    }
                }
            }

            return baseTexes;
        }
        

        private static METexture2D PreGenerateMETex2D(AbstractTexInfo tex, bool? RequestPCCStored = null)
        {
            METexture2D tex2D = null;
            if (tex.GetType() == typeof(TPFTexInfo))
            {
                tex2D = new METexture2D(tex);
                METexture2D.Tex2DPCCPopulationResult result = tex2D.PopulateFromPCC(RequestPCCStored);  // bool parameter -> true indicates we want a pcc texture, default false
                // need return value to indicate whether successful in obtaining the requested texture
                // also all this only for me2 and 3
                
                
                // KFreon: Wasn't a version of the requested type.
                if (result != METexture2D.Tex2DPCCPopulationResult.GotRequested)
                    return null;
                

                try
                {
                    byte[] data = ((TPFTexInfo)tex).Extract();
                    tex2D.OneImageToRuleThemAll(tex.PathBIOGame, data);
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("Failed to install texture: " + tex2D.texName + "Reason: ", "Texplorer GetMETex2Ds", e);
                }
            }
            else if (tex.GetType() == typeof(TreeTexInfo))
                tex2D = ((TreeTexInfo)tex).ChangedTex2D;
            else
                Console.WriteLine();


            return tex2D;
        }
        

        internal void SelectSearchResults(TreeTexInfo selected)
        {
            if (selected != null)
            {
                SelectSearchResult(selected.Parent);
                selected.Parent.IsSelected = true;
                selected.IsSelected = true;
            }
        }

        private void SelectSearchResult(HierarchicalTreeTexes parent)
        {
            if (parent.Parent == null)
                parent.IsExpanded = true;
            else
                SelectSearchResult(parent.Parent);
        }


        /// <summary>
        /// Primary texture save method. 
        /// Installs all relevant textures to a PCC, then moves to the next PCC.
        /// </summary>
        /// <param name="gameversion"></param>
        /// <param name="MEExDirecs"></param>
        /// <param name="PrimaryIndeterminateUpdater"></param>
        /// <param name="PrimaryStatusUpdater"></param>
        /// <param name="PrimaryProgressUpdater"></param>
        /// <param name="MaxPrimaryUpdater"></param>
        /// <param name="texes"></param>
        /// <returns></returns>
        public static async Task InstallTextures(int gameversion, MEDirectories.MEDirectories MEExDirecs, Action<bool> PrimaryIndeterminateUpdater, Action<string> PrimaryStatusUpdater, Action<int> PrimaryProgressUpdater, Action<int> MaxPrimaryUpdater, params AbstractTexInfo[] texes)
        {
            Dictionary<string, long> AllFileInfos = null;
            bool success = await Task.Run(() => { return InstallMutlipleTextures(gameversion, MEExDirecs.PathBIOGame, PrimaryStatusUpdater, PrimaryIndeterminateUpdater, PrimaryProgressUpdater, MaxPrimaryUpdater, out AllFileInfos, texes); });

            if (PrimaryIndeterminateUpdater != null)
                PrimaryIndeterminateUpdater(true);


            // KFreon: Update tocs for ME3 
            if (gameversion == 3)
            {
                List<string> DLCSfars = new List<string>();
                ConcurrentDictionary<string, long> BaseGameFileInfos = new ConcurrentDictionary<string, long>();
                foreach (KeyValuePair<string, long> file in AllFileInfos)
                {
                    string sfarPath = Path.Combine(Path.GetDirectoryName(file.Key), "Default.sfar");
                    if (file.Key.Contains("DLC") && DLCSfars.Contains(sfarPath))
                        DLCSfars.Add(sfarPath);
                    else if (!BaseGameFileInfos.ContainsKey(file.Key))
                        BaseGameFileInfos.TryAdd(file.Key, file.Value);
                }

                await Task.Run(() => Helpers.Methods.UpdateTOCs(MEExDirecs, DLCSfars, BaseGameFileInfos));
            }
            

            if (PrimaryIndeterminateUpdater != null)
                PrimaryIndeterminateUpdater(false);

            if (PrimaryStatusUpdater != null)
                PrimaryStatusUpdater(success ? "All textures installed!" : "Install failed! See DebugWindow for more details.");
        }

        internal void RevertTexture(TreeTexInfo tex)
        {
            tex.OrigTex2D = tex.ChangedTex2D;
            tex.ChangedTex2D = null;
        }


        /// <summary>
        /// From mods DO NOT USE also DOESN"T GET USED
        /// </summary>
        /// <param name="texname"></param>
        /// <param name="pccs"></param>
        /// <param name="ids"></param>
        public void removeTopTexture(string texname, List<string> pccs, List<int> ids)
        {
            // NOTHIGN
        }

        public override void ChangeTree(object NewTreeSelected, IList<TreeDB> Trees)
        {
            base.ChangeTree(NewTreeSelected, Trees);

            Properties.Settings.Default.TexplorerGameVersion = CurrentTree.GameVersion;
            SaveProperties();
        }

        internal async Task<bool> CompareTrees(string firstTree, string secondTree = null)
        {
            TreeDB First = new TreeDB(firstTree);
            TreeDB Second = null;

            // KFreon: Select trees
            if (CurrentTree.Valid && secondTree == null)
                Second = CurrentTree;
            else if (secondTree != null)
                Second = new TreeDB(secondTree);
            else
            {
                PrimaryStatus = "Not enough trees to compare. Check current tree is valid, or select a valid second tree.";
                return false;
            }

            PrimaryStatus = "Comparing trees. This can take around an hour.";

            // KFreon: Compare trees
            TreeComparisonMismatches.Clear();
            TreeComparisonResult result = await CompareTrees(First, Second);

            PrimaryStatus = "Tree comparison complete!";
            return true;
        }

        public enum TreeComparisonResult
        {
            TreeWasNull, TreeWasInvalid, MismatchedGameVersions, CompleteMatch, TreesDifferent
        }

        public class TreeBit
        {
            public string Name { get; set; }
            public uint Hash { get; set; }
            public string Pack { get; set; }
            public bool PackSame { get; set; }
            public bool PCCSame { get; set; }
            public bool Good
            {
                get;
                set;
            }

            public TreeBit(string name, uint hash)
            {
                Name = name;
                Hash = hash;
            }
        }

        async Task<TreeComparisonResult> CompareTrees(TreeDB Tree1, TreeDB Tree2)
        {
            // KFreon: Validation stuff
            if (Tree1 == null || Tree2 == null)
                return TreeComparisonResult.TreeWasNull;

            if (!Tree1.Valid || !Tree2.Valid)
                return TreeComparisonResult.TreeWasInvalid;

            if (Tree1.GameVersion != Tree2.GameVersion)
                return TreeComparisonResult.MismatchedGameVersions;


            
            if (Tree1.TexCount == Tree2.TexCount)
                Console.WriteLine("Trees have same texcount.");

            
            // KFreon: Run comparison
            await Task<List<TreeBit>>.Run(() =>
                {
                    List<TreeBit> things = new List<TreeBit>();

                    List<TreeBit> tempmismatches = new List<TreeBit>();
                    // KFreon: Check for stuff missing from tree2 in tree1, and also matches between trees.
                    foreach (var item1 in Tree1.Textures)
                    {
                        TreeBit bit = new TreeBit(item1.EntryName, item1.Hash);
                        bit.Pack = item1.Package;

                        foreach (var item2 in Tree2.Textures)
                        {
                            if (item1.EntryName == item2.EntryName && item1.Hash == item2.Hash)
                            {
                                bit.PackSame = item1.Package == item2.Package;
                                bit.PCCSame = item1.PCCs.Count == item2.PCCs.Count;
                                bit.Good = true;
                                

                                /*Console.WriteLine(item1.EntryName + "   " + item2.EntryName);
                                Console.WriteLine(item1.Hash + "   " + item2.Hash);
                                Console.WriteLine(item1.TFCOffset + "   " + item2.TFCOffset);
                                Console.WriteLine(item1.Package + "   " + item2.Package);
                                Console.WriteLine(item1.PCCs.Count + "   " + item2.PCCs.Count);
                                Console.WriteLine();

                                found = true;
                                TreeComparisonItem temp = new TreeComparisonItem(item1, item2, TreeComparisonItemResult.DetailsMismatch);
                                tempmismatches.Add(temp);*/
                            }
                        }

                        if (!bit.Good)
                        {
                            Console.WriteLine(item1.EntryName);
                            Console.WriteLine(item1.Hash);
                            Console.WriteLine(item1.TFCOffset);
                            Console.WriteLine(item1.Package);
                            Console.WriteLine(item1.PCCs.Count);
                            Console.WriteLine();
                            tempmismatches.Add(bit);
                        }
                    }


                    // KFreon: Check for stuff missing from tree1 in tree2
                    foreach (var item2 in Tree2.Textures)
                    {
                        bool found = false;
                        foreach (var item1 in Tree1.Textures)
                        {
                            if (item2.EntryName == item1.EntryName && item2.Hash == item1.Hash)
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            Console.WriteLine(item2.EntryName);
                            Console.WriteLine(item2.Hash);
                            Console.WriteLine(item2.TFCOffset);
                            Console.WriteLine(item2.Package);
                            Console.WriteLine(item2.PCCs.Count);
                            Console.WriteLine();
                            //tempmismatches.Add(new TreeComparisonItem(item2, null, TreeComparisonItemResult.NotFoundInTree1));
                        }
                    }

                    return tempmismatches;
                });



            if (TreeComparisonMismatches.Count > 0)
                return TreeComparisonResult.CompleteMatch;
            else
                return TreeComparisonResult.TreesDifferent;
        }

        /*TextureCompareResult CheckComparedTextures(TreeTexInfo tex1, TreeTexInfo tex2)
        {
            TextureCompareResult result = TextureCompareResult.Match;

            if (item1.EntryName != item2.EntryName)
                return TextureCompareResult.NameMismatch;


        }*/
    }

    /*enum TextureCompareResult
    {
        Match, FilesMismatch, HashMismatch, NameMismatch
    }
    */
    public enum TreeComparisonItemResult
    {
        NotFoundInTree1, NotFoundInTree2, DetailsMismatch
    }

    public class TreeComparisonItem
    {
        public TreeTexInfo tex1 { get; set; }
        public TreeTexInfo tex2 { get; set; }
        public ObservableCollection<TreeComparisonItemResult> Reasons { get; set; }
        public TreeComparisonItem(TreeTexInfo info1, TreeTexInfo info2, params TreeComparisonItemResult[] reasons)
        {
            tex1 = info1;
            tex2 = info2;
            Reasons = new ObservableCollection<TreeComparisonItemResult>(reasons);
        }
    }
}
