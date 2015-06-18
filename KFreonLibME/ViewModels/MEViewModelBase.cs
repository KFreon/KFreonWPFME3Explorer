using KFreonLibGeneral.Debugging;
using KFreonLibME.Textures;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using UsefulThings.WPF;

namespace KFreonLibME.ViewModels
{
    public abstract class MEViewModelBase : ViewModelBase
    {
        public ICollectionView ItemsView { get; set; }






        protected DispatcherTimer timer = null;
        protected Stopwatch MainStopWatch = null;

        public ICommand SelectAllCommand { get; set; }
        public ICommand DeSelectAllCommand { get; set; }

        ICommand showGameInfoCommand = null;
        public ICommand ShowGameInfoCommand
        {
            get
            {
                return showGameInfoCommand;
            }
            set
            {
                SetProperty(ref showGameInfoCommand, value);
            }
        }

        public ICommand CancelEverythingCommand { get; set; }

        int displayGameInfo = 0;
        public int DisplayGameInfo
        {
            get
            {
                return displayGameInfo;
            }
            set
            {
                displayGameInfo = value;
                OnPropertyChanged();
            }
        }

        string memoryUsage = null;
        public string MemoryUsage
        {
            get
            {
                return memoryUsage;
            }
            set
            {
                SetProperty(ref memoryUsage, value);
            }
        }

        string elapsedTime = null;
        public string ElapsedTime
        {
            get
            {
                return elapsedTime;
            }
            set
            {
                SetProperty(ref elapsedTime, value);
            }
        }

        int numThreads = 4;
        public int NumThreads
        {
            get
            {
                return numThreads;
            }
            set
            {
                SetProperty(ref numThreads, value);
            }
        }

        bool primaryIndeterminate = false;
        public bool PrimaryIndeterminate
        {
            get
            {
                return primaryIndeterminate;
            }
            set
            {
                SetProperty(ref primaryIndeterminate, value);
            }
        }

        int primaryProgress = 0;
        public int PrimaryProgress
        {
            get
            {
                return primaryProgress;
            }
            set
            {
                SetProperty(ref primaryProgress, value);
            }
        }

        int maxPrimaryProgress = 0;
        public int MaxPrimaryProgress
        {
            get
            {
                return maxPrimaryProgress;
            }
            set
            {
                SetProperty(ref maxPrimaryProgress, value);
            }
        }

        string primaryStatus = null;
        public string PrimaryStatus
        {
            get
            {
                return primaryStatus;
            }
            set
            {
                SetProperty(ref primaryStatus, value);
            }
        }

        bool busy = false;
        public bool Busy
        {
            get
            {
                return busy;
            }
            set
            {
                SetProperty(ref busy, value);
            }
        }

        public bool Cancelled
        {
            get
            {
                if (cts == null)
                    return true;
                
                return cts.IsCancellationRequested && !Busy;
            }
        }
        public CancellationTokenSource cts { get; set; }

        bool doesGame1Exist = false;
        public bool DoesGame1Exist
        {
            get
            {
                return doesGame1Exist;
            }
            set
            {
                SetProperty(ref doesGame1Exist, value);
            }
        }


        bool doesGame2Exist = false;
        public bool DoesGame2Exist
        {
            get
            {
                return doesGame2Exist;
            }
            set
            {
                SetProperty(ref doesGame2Exist, value);
            }
        }


        bool doesGame3Exist = false;
        public bool DoesGame3Exist
        {
            get
            {
                return doesGame3Exist;
            }
            set
            {
                SetProperty(ref doesGame3Exist, value);
            }
        }

        string version = null;
        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                SetProperty(ref version, value);
            }
        }

        public int GameVersion
        {
            get
            {
                return MEExDirecs.GameVersion;
            }
        }


        public MEDirectories.MEDirectories MEExDirecs { get; set; }


        

        public MEViewModelBase(int game)
        {
            SelectAllCommand = new UsefulThings.WPF.CommandHandler(element => KFreonLibME.Misc.Methods.ChangeListSelection(element, true));
            DeSelectAllCommand = new UsefulThings.WPF.CommandHandler(element => KFreonLibME.Misc.Methods.ChangeListSelection(element, false));

            // KFreon: Setup ElapsedTime and MemoryUsage timers
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            MainStopWatch = new Stopwatch();
            timer.Tick += (sender, e) =>
            {
                if (MainStopWatch.IsRunning)
                    ElapsedTime = MainStopWatch.Elapsed.ToString("hh':'mm':'ss");

                MemoryUsage = UsefulThings.General.GetFileSizeAsString(Environment.WorkingSet);
            };
            timer.Start();

            MEExDirecs = new MEDirectories.MEDirectories(game);            

            ShowGameInfoCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                DisplayGameInfo = Int32.Parse((string)t);
            }, true);

            NumThreads = KFreonLibME.Misc.Methods.SetupThreadCount();

            DoesGame1Exist = MEExDirecs.DoesGame1Exist;
            DoesGame2Exist = MEExDirecs.DoesGame2Exist;
            DoesGame3Exist = MEExDirecs.DoesGame3Exist;

            Version = "Version: " + KFreonLibGeneral.Misc.Methods.GetBuildVersion();

            ValidateCancellationTokenSource();

            PrimaryStatus = "Getting Game Directory Information...";

            CancelEverythingCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                cts.Cancel();
            }, true);
        }

        protected bool LoadTrees(IList<TreeDB> Trees, TreeDB CurrentTree, bool isChanging)
        {
            if (isChanging)
            {
                if (!CurrentTree.Valid)
                    CurrentTree.LoadTree();
            }
            else
            {
                Trees[0].LoadTree();
                Trees[1].LoadTree();
                Trees[2].LoadTree();
            }

            if (CurrentTree.Valid)
            {
                CurrentTree.ConstructTree();
                PrimaryStatus = "Ready.";
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual void ChangeTree(object NewTreeSelected, IList<TreeDB> Trees)
        {
            foreach (TreeDB tree in Trees)
                tree.IsSelected = tree == NewTreeSelected;            

            MEExDirecs.GameVersion = ((TreeDB)NewTreeSelected).GameVersion;
            OnPropertyChanged("GameVersion");
            OnPropertyChanged("CurrentTree");
        }

        public void ValidateCancellationTokenSource()
        {
            if (Cancelled)
                cts = new CancellationTokenSource();
        }


        /// <summary>
        /// Refreshes pathing information after path changes.
        /// </summary>
        public void RefreshDirecs()
        {
            MEExDirecs.SetupPathing();
        }

        internal void SavePCCList<T>(string filename, IEnumerable<T> texes) where T : IToolEntry
        {
            try
            {
                using (StreamWriter fs = new StreamWriter(filename))
                    foreach (var item in texes)
                        if (item.IsSelected)
                            foreach (PCCEntry pccentry in item.PCCs)
                                if (pccentry.Using)
                                    fs.WriteLine(pccentry.ExpID + "  " + pccentry.File);

                PrimaryStatus = "Saved PCC list!";
            }
            catch (Exception ex)
            {
                DebugOutput.PrintLn("Failed to save PCCList to: " + filename, "Texture Tools", ex);
            }
        }

        public void SaveProperties()
        {
            try
            {
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Error saving properties: ", "Texture Tools", e);
            }
        }
    }
}
