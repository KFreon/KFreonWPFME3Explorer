using KFreonLibGeneral.Debugging;
using KFreonLibME.Scripting;
using KFreonLibME.Scripting.ModMaker;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using UsefulThings.WPF;

namespace KFreonLibME.ViewModels
{
    public class ModMakerViewModel : MEViewModelBase
    {
        #region Properties, etc
        #region State properties
        // KFreon: Represents user cancellation request for major operations.
        bool cancelRequested = false;
        public bool CancelRequested
        {
            get
            {
                return cancelRequested;
            }
            set
            {
                cancelRequested = value;
                OnPropertyChanged();
            }
        }

        // KFreon: Indicates whether mods have been loaded or not.
        bool loaded = false;
        public bool Loaded
        {
            get
            {
                return loaded;
            }
            set
            {

                loaded = value;
                OnPropertyChanged();
            }
        }
        #endregion


        #region Progress properties
        // KFreon: Indicates minor progress.
        int secondary = -1;
        public int SecondaryProgress
        {
            get
            {
                return secondary;
            }
            set
            {
                secondary = value;
                SecondaryVisible = secondary > -1;
                OnPropertyChanged();
            }
        }

        int maxSecondaryProgress = 0;
        public int MaxSecondaryProgress
        {
            get
            {
                return maxSecondaryProgress;
            }
            set
            {
                SetProperty(ref maxSecondaryProgress, value);
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
                secondaryIndeterminate = value;
                OnPropertyChanged();
            }
        }
        #endregion


        #region Status properties
        // KFreon: Minor status indicator.
        string secondarystatus = null;
        public string SecondaryStatus
        {
            get
            {
                return secondarystatus;
            }
            set
            {
                secondarystatus = value;
                OnPropertyChanged();
            }
        }
        #endregion


        #region Visibility properties
        // KFreon: Indicates minor progress and status visibility.
        bool secondaryvisible = false;
        public bool SecondaryVisible
        {
            get
            {
                return secondaryvisible;
            }
            set
            {
                secondaryvisible = value;
                OnPropertyChanged();
            }
        }
        #endregion


        public ICommand CancelThumbsCommand { get; set; }


        #region Misc
        bool multiSelected = true;
        public bool MultiSelected
        {
            get
            {
                return multiSelected;
            }
            set
            {
                multiSelected = value;
                OnPropertyChanged();
            }
        }


        // KFreon: Number of valid mods loaded.
        public int NumValid
        {
            get
            {
                return LoadedMods.Where(j => j.Valid).Count();
            }
        }

        // KFreon: Indicates whether thumbnail generation is in progress. Setting to false will cancel thumbnail generation.
        bool thumbGenInit = false;
        public bool ThumbsGenInit
        {
            get
            {
                return thumbGenInit;
            }
            set
            {
                thumbGenInit = value;
                OnPropertyChanged();
            }
        }

        // KFreon: Indicates whether thumbnails will be generated.
        private bool thumbsEnabled = true;
        public bool ThumbsEnabled
        {
            get
            {
                return thumbsEnabled;
            }
            set
            {
                thumbsEnabled = value;
                OnPropertyChanged();
            }
        }

        // KFreon: Current mod data variable. So now data doesn't have to get written to disk several times.
        public byte[] ModData { get; set; }

        // KFreon: Version of program.
        public string Version
        {
            get
            {
                return "Version: " + KFreonLibGeneral.Misc.Methods.GetBuildVersion();
            }
        }

        // KFreon: Number of loaded mods.
        public int NumMods
        {
            get
            {
                return LoadedMods.Count;
            }
        }

        // KFreon: List of loaded mods. Uses a threadsafe implementation of ObservableCollection (not mine).
        public MTRangedObservableCollection<ModJob> LoadedMods { get; set; }
        #endregion Misc


        #region Commands
        // KFreon: Commands work well from WPF lists cos they can get context for the item contents directly (ModJob)
        // Most commands that actually do stuff have to be off thread so they don't freeze up the GUI.
        public bool CanExecute = true; // KFreon: True for now

        List<ICommand> Commands = null;  // KFreon: List of commands to pass to modjobs

        private ICommand _genThumb;
        public ICommand GenerateThumbnailCommand
        {
            get
            {
                return _genThumb ?? (_genThumb = new CommandHandler(job => GenerateThumbnail(job), CanExecute));
            }
        }

        private ICommand _updateCommand;
        public ICommand UpdateCommand
        {
            get
            {
                return _updateCommand ?? (_updateCommand = new CommandHandler(job => UpdateJob(job), CanExecute));
            }
        }

        private ICommand _extractCommand;
        public ICommand ExtractCommand
        {
            get
            {
                return _extractCommand ?? (_extractCommand = new CommandHandler(job => ExtractJobData(job), CanExecute));
            }
        }

        private ICommand _runCommand;
        public ICommand RunCommand
        {
            get
            {
                return _runCommand ?? (_runCommand = new CommandHandler(job => RunJob(job), CanExecute));
            }
        }

        private ICommand _resetScriptCommand;
        public ICommand ResetScriptCommand
        {
            get
            {
                return _resetScriptCommand ?? (_resetScriptCommand = new CommandHandler(job => ResetScript(job), CanExecute));
            }
        }

        private ICommand _saveModCommand;
        public ICommand SaveModCommand
        {
            get
            {
                return _saveModCommand ?? (_saveModCommand = new CommandHandler(job => SaveJobToMod(job), CanExecute));
            }
        }
        #endregion


        #region Callback reporters
        // KFreon: Progress and status reporters passed into external functions so they can report back here.
        Action<int> PrimaryProgReporter;
        Action<int> SecondaryProgReporter;
        Action<int> PrimaryMaxReporter;
        Action<int> SecondaryMaxReporter;

        Action<string> PrimaryStatusReporter;
        Action<string> SecondaryStatusReporter;
        #endregion
        #endregion

        public int[] GameVersions { get; set; }


        public ModMakerSearchViewModel<ModJob> Search { get; set; }

        public ModMakerViewModel()
            : base(Properties.Settings.Default.TexplorerGameVersion)
        {
            GameVersions = new int[] { 1, 2, 3 };

            LoadedMods = new MTRangedObservableCollection<ModJob>();
            ItemsView = CollectionViewSource.GetDefaultView(LoadedMods);
            
            ItemsView.Filter = t =>
            {
                return ((ModJob)t).IsSearchVisible;
            };

            CancelThumbsCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                ThumbsGenInit = false;
            }, true);

            // KFreon: Ridiculous delegation of progress reports due to being unable to pass properties by ref
            PrimaryProgReporter = new Action<int>(val => PrimaryProgress = val);
            SecondaryProgReporter = new Action<int>(val => SecondaryProgress = val);

            PrimaryMaxReporter = new Action<int>(pm => MaxPrimaryProgress = pm);
            SecondaryMaxReporter = new Action<int>(sm => MaxSecondaryProgress = sm);

            PrimaryStatusReporter = new Action<string>(rep => PrimaryStatus = rep);
            SecondaryStatusReporter = new Action<string>(rep => SecondaryStatus = rep);


            // KFreon: Set misc defaults
            PrimaryStatus = "Ready.";
            Loaded = NumMods > 0;
            KFreonLibME.Scripting.ModMakerHelper.Instance = this;

            Search = new ModMakerSearchViewModel<ModJob>(LoadedMods, ItemsView);

            // KFreon: Setup commands
            Commands = new List<ICommand>() { UpdateCommand, ExtractCommand, SaveModCommand, ResetScriptCommand, RunCommand, GenerateThumbnailCommand };
        }


        #region Job List Manipulation
        /// <summary>
        /// Removes job from tool.
        /// </summary>
        /// <param name="index">Index of job to remove.</param>
        public void DeleteJobs()
        {
            foreach (ModJob job in ModMakerHelper.JobList)
                if (job.IsSelected)
                    ModMakerHelper.JobList.Remove(job);

            // KFreon: Reset things if last job
            if (NumMods == 0)
            {
                PrimaryProgress = 0;
                PrimaryStatus = "Ready.";
                Loaded = false;
                Busy = false;
            }
        }


        /// <summary>
        /// Removes all jobs from tool.
        /// </summary>
        public void ClearJobs()
        {
            // KFreon: Reset everything
            ModMakerHelper.JobList.Clear();
            PrimaryProgress = 0;
            PrimaryStatus = "Ready.";
            Loaded = false;
            Busy = false;
        }


        /// <summary>
        /// Moves job down in list.
        /// </summary>
        /// <param name="index">Current index of job.</param>
        /// <returns>True if job moved, false if unable to move.</returns>
        public bool MoveJobDown(int index)
        {
            return MoveJob(index, index + 1);
        }


        /// <summary>
        /// Moves job up in list.
        /// </summary>
        /// <param name="index">Current index of job.</param>
        /// <returns></returns>
        public bool MoveJobUp(int index)
        {
            return MoveJob(index, index - 1);
        }


        /// <summary>
        /// Moves job in list.
        /// </summary>
        /// <param name="from">Current job index.</param>
        /// <param name="to">Intended index.</param>
        /// <returns>True if job moved, false if unable to move.</returns>
        private bool MoveJob(int from, int to)
        {
            // KFreon: Check validity. NOTE: Moving is disabled when thumbnails are being generated.
            if (ThumbsGenInit || from < 0 || from >= NumMods || to < 0 || to > NumMods - 1 || MultiSelected)
                return false;

            ModJob job = LoadedMods[from];
            LoadedMods.RemoveAt(from);
            LoadedMods.Insert(to, job);
            return true;
        }
        #endregion


        #region Main Operations
        /// <summary>
        /// Loads list of mods asynchronously.
        /// </summary>
        /// <param name="filenames">Files to load.</param>
        public async void LoadMods(string[] filenames)
        {
            // KFreon: Set status flags
            Loaded = false;
            Busy = true;

            // KFreon: Setup if necessary
            if (NumMods == 0)
                KFreonLibME.Scripting.ModMakerHelper.Initialise();

            /*// KFreon: Begin to allow thumbnail generation if enabled
            if (ThumbsEnabled)
            {
                DebugOutput.PrintLn("Thumbnail generation is enabled. Beginning asynchronous generation...");
                ThumbsGenInit = true;
                Task.Run(() => BeginThumbGeneration());
            }*/

            // KFreon: Set progress
            PrimaryProgress = 0;
            MaxPrimaryProgress = filenames.Count();

            // KFreon: Load from all files
            for (int i = 0; i < filenames.Count(); i++)
            {
                // KFreon: Load current file
                string file = filenames[i];
                PrimaryStatus = "Loading " + (i + 1) + " of " + filenames.Count() + " mods.";
                List<ModJob> tempjobs = new List<ModJob>();
                bool? AutoUpdate = await Task<bool?>.Run(() => KFreonLibME.Scripting.ModMakerHelper.LoadDotMod(file, SecondaryProgReporter, SecondaryMaxReporter, SecondaryStatusReporter, Commands, MEExDirecs.ExecFolder, MEExDirecs.BIOGames, tempjobs));

                LoadedMods.AddRange(tempjobs);
                PrimaryProgress++;
            }

            // KFreon: Begin to allow thumbnail generation if enabled
            if (ThumbsEnabled)
            {
                DebugOutput.PrintLn("Thumbnail generation is enabled. Beginning asynchronous generation...");
                ThumbsGenInit = true;


                ParallelOptions po = new ParallelOptions();
                po.MaxDegreeOfParallelism = NumThreads - 2;
                await Task.Run(() => Parallel.ForEach(ModMakerHelper.JobList, po, job =>
                {
                    if (thumbGenInit == false)
                        return;

                    job.CreateJobThum();
                }));

                ThumbsGenInit = false;
            }

            // KFreon: Report on results of overall loading
            if (NumMods != 0)
            {
                Loaded = true;
                PrimaryStatus = "Loaded " + NumMods + " mods from " + filenames.Count() + " files.";
            }
            else
            {
                PrimaryStatus = "Loading cancelled!";
                ThumbsGenInit = false;
            }

            SecondaryProgress = -1;
            Busy = false;
        }
        #endregion


        #region Job tasks
        public async void GenerateThumbnails(ModJob job)
        {
            MaxPrimaryProgress = 1;
            PrimaryProgress = 0;
            await Task.Run(() => job.CreateJobThum());
            PrimaryProgress = 1;
        }

        public void GenerateThumbnail(object job)
        {
            GenerateThumbnails((ModJob)job);
        }

        public async void UpdateJobs(params Scripting.ModMaker.ModJob[] jobs)
        {
            List<ModJob> tempjobs = jobs.ToList();
            if (jobs.Count() == 0)
                tempjobs = LoadedMods.Where(j => j.IsSelected).ToList();

            if (tempjobs.Count == 0)
            {
                PrimaryStatus = "No jobs selected to update.";
                return;
            }

            MaxPrimaryProgress = tempjobs.Count();
            PrimaryProgress = 0;

            PrimaryStatus = "Updating " + (tempjobs.Count() > 1 ? "jobs" : "job") + "...";


            bool success = await Task.Run(() =>
                {
                    bool updateSuccess = true;
                    foreach (ModJob job in tempjobs)
                    {
                        // KFreon: Skip already up-to-date jobs
                        if (job.RequiresUpdate)
                        {
                            if (!job.UpdateJob(MEExDirecs.BIOGames, MEExDirecs.ExecFolder, PrimaryStatusReporter))  // TODO KFreon: Visual feedback on whats wrong?
                                updateSuccess = false;
                            PrimaryProgress++;
                        }
                    }
                    return updateSuccess;
                });

            PrimaryStatus = success ? "Update completed successfully." : "Update failed! See DebugWindow for details";
            PrimaryProgress = MaxPrimaryProgress;
        }

        public void UpdateJob(object job)
        {
            UpdateJobs((ModJob)job);
        }

        public async void ExtractJobsData(params ModJob[] jobs)
        {
            List<ModJob> tempjobs = jobs.ToList();
            if (jobs.Count() == 0)
                tempjobs = LoadedMods.Where(j => j.IsSelected).ToList();

            // KFreon: Do nothing
            if (tempjobs.Count == 0)
            {
                PrimaryStatus = "No jobs selected for extraction.";
                return;
            }

            MaxPrimaryProgress = tempjobs.Count();

            PrimaryProgress = 0;
            string filename = null;
            bool isFile = true;

            // KFreon: Select path
            if (tempjobs.Count() == 1)
            {
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.Title = "Select destination for data";
                sfd.Filter = tempjobs[0].JobType == "TEXTURE" ? "DirectX images|*.dds" : "Meshes|*.mesh";

                if (sfd.ShowDialog() == true)
                {
                    filename = sfd.FileName;
                }
                else
                {
                    PrimaryStatus = "Extraction cancelled.";
                    return;
                }
            }
            else   // KFreon: Saving multiple files
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        filename = fbd.SelectedPath;
                        isFile = false;
                    }
                    else
                        return;
                }
            }

            await Task.Run(() => 
                {
                    foreach (ModJob job in tempjobs)
                    {
                        PrimaryStatus = "Extracting Job: " + job.Name + " data...";

                        // KFreon: Get save path.
                        job.ExtractData(isFile ? filename : Path.Combine(filename, job.Name.Replace(":", "")));
                        PrimaryStatus = "Job: " + job.Name+ " data extracted!";

                        PrimaryProgress++;
                    }
                });
        }

        public void ExtractJobData(object job)
        {
            ExtractJobsData((Scripting.ModMaker.ModJob)job);
        }

        public async void RunJobs(params Scripting.ModMaker.ModJob[] jobs)
        {
            List<ModJob> jobList = jobs.ToList();
            if (jobs.Count() == 0)
                jobList = LoadedMods.Where(j => j.IsSelected).ToList();

            if (jobList.Count == 0)
            {
                PrimaryStatus = "No jobs selected to run.";
                return;
            }

            MaxPrimaryProgress = jobList.Where(j => j.Valid).Count();
            PrimaryProgress = 0;

            int whichGame = -1;
            List<List<string>> DLCUpdateList = new List<List<string>>() { null, null, null };

            SecondaryVisible = true;
            SecondaryIndeterminate = true;

            int count = 0;

            await Task.Run(() =>
            {
                for (int i = 0; i < jobList.Count; i++)
                {
                    ModJob job = jobList[i];
                    if (!job.Valid)
                    {
                        DebugOutput.PrintLn("Skipping invalid job: " + job.Name);
                        continue;
                    }

                    SecondaryStatus = "Installing job: " + job.Name;
                    PrimaryStatus = "Installing job " + count++ + " of " + MaxPrimaryProgress;

                    List<string> tempdlcs = job.RunJob(out whichGame);
                    List<string> dlcUpdates = DLCUpdateList[job.GameVersion - 1];

                    // KFreon: Only proceed if DLC mods present
                    if (tempdlcs.Count > 1)
                    {
                        // KFreon: Initialise list if required
                        if (dlcUpdates == null)
                            dlcUpdates = new List<string>(tempdlcs);


                        // KFreon: Stop duplicates - doing it here cos it could make this list rather large if we add everything and do a list.Distinct() later.
                        foreach (string dlc in tempdlcs)
                            if (!dlcUpdates.Contains(dlc))
                                dlcUpdates.Add(dlc);
                    }

                    
                    PrimaryProgress++;
                }

                SecondaryVisible = false;
                SecondaryIndeterminate = false;

                PrimaryIndeterminate = true;
                PrimaryStatus = "Updating TOC's...";

                // KFreon: Update TOCs
                for (int i = 0; i < 3; i++)
                {
                    int game = i + 1;

                    // KFreon: Skip games that don't have updates
                    var test = jobs.Where(jo => jo.GameVersion == game);
                    if (test.Count() == 0)
                        continue;

                    if (game != 3)
                    {
                        KFreonLibME.Helpers.Methods.UpdateTOCs(MEExDirecs.GetDifferentPathBIOGame(game), game, MEExDirecs.GetDifferentDLCPath(game));
                        continue;
                    }


                    // KFreon: Below only used for ME3 DLC TOC Updates for now until someone/me figures out the other 2 DLCs
                    List<string> dlc = DLCUpdateList[i];
                    if (dlc != null)
                    {
                        DebugOutput.PrintLn("Updating DLC TOC for: " + dlc);
                        
                        KFreonLibME.Helpers.Methods.UpdateTOCs(MEExDirecs.GetDifferentPathBIOGame(game), game, MEExDirecs.GetDifferentDLCPath(game), dlc);
                    }
                }
            });

            PrimaryIndeterminate = false;
            PrimaryStatus = "Update Complete!";            
        }

        public void RunJob(object job)
        {
            RunJobs((Scripting.ModMaker.ModJob)job);
        }

        public void ResetScripts(params ModJob[] jobs)
        {
            List<ModJob> tempjobs = jobs.ToList();
            if (jobs.Count() == 0)
                tempjobs = LoadedMods.Where(j => j.IsSelected).ToList();

            if (tempjobs.Count == 0)
            {
                PrimaryStatus = "No jobs selected to reset.";
                return;
            }

            MaxPrimaryProgress = tempjobs.Count();
            PrimaryProgress = 0;

            foreach (ModJob job in tempjobs)
            {
                job.ResetScript();
                PrimaryProgress++;
            }

            PrimaryStatus = "Scripts reset!";
        }

        public void ResetScript(object job)
        {
            ResetScripts((ModJob)job);
        }

        public async void SaveJobsToMod(params ModJob[] jobs)
        {
            bool isFile = true;
            List<ModJob> jobList = jobs.ToList();
            if (jobs.Count() == 0)
                jobList = LoadedMods.Where(j => j.IsSelected).ToList();

            if (jobList.Count == 0)
            {
                PrimaryStatus = "No jobs selected to save.";
                return;
            }

            MaxPrimaryProgress = jobList.Count();
            PrimaryProgress = 0;

            SecondaryVisible = true;
            SecondaryIndeterminate = true;

            string filename = null;

            if (jobList.Count() > 1)
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    filename = fbd.SelectedPath;
                    isFile = false;
                }
                else
                    return;
            }
            else
            {
                Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
                sfd.Title = "Select destination";
                sfd.Filter = "ME3 Mods|*.mod";
                if (sfd.ShowDialog() == true)
                    filename = sfd.FileName;
                else
                    return;
            }

            int count = 0;

            await Task.Run(() =>
            {
                foreach (ModJob job in jobList)
                {
                    /*if (!job.Valid)
                    {
                        DebugOutput.PrintLn("Skipping invalid job: " + job.Name);
                        continue;
                    }*/


                    PrimaryStatus = "Saving job " + count + " of " + MaxPrimaryProgress + " to file...";

                    bool res = ModMakerHelper.WriteJobToFile(isFile ? filename : Path.Combine(filename, job.Name.Replace(":", "")) + ".mod", job);
                        if (res)
                            PrimaryStatus = "Job saved!";
                        else
                            PrimaryStatus = "Saving failed!";
                    PrimaryProgress++;
                }
            });
            SecondaryVisible = false;
            SecondaryIndeterminate = false;
        }

        public void SaveJobToMod(object job)
        {
            SaveJobsToMod((ModJob)job);
        }
        #endregion
    }
}
