using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Gibbed.IO;
using KFreonLibGeneral.Debugging;
using KFreonLibME.MEDirectories;
using KFreonLibME.PCCObjects;
using ResILWrapper;
using UsefulThings;
using UsefulThings.WPF;

namespace KFreonLibME.Scripting.ModMaker
{
    /// <summary>
    /// This is the object for storing .mod job data. 
    /// </summary>
    public class ModJob : ViewModelBase, IToolEntry
    {
        #region Properties
        public uint Hash { get; set; }  // KFreon: NOT USED. Inherited.

        

        public string pathBIOGame = null;
        public string ExecPath = null;

        #region KFreon: ViewModel
        #region Commands
        // KFreon: Asynchronous commands for use in WPF GUI. These work better than events as they get direct context for the ModJob.
        ICommand genThumb = null;
        public ICommand GenerateThumbnailCommand
        {
            get
            {
                return genThumb;
            }
            set
            {
                genThumb = value;
                OnPropertyChanged();
            }
        }

        ICommand updateCommand = null;
        public ICommand UpdateCommand
        {
            get
            {
                return updateCommand;
            }
            set
            {
                updateCommand = value;
                OnPropertyChanged();
            }
        }

        ICommand extractCommand = null;
        public ICommand ExtractCommand
        {
            get
            {
                return extractCommand;
            }
            set
            {
                extractCommand = value;
                OnPropertyChanged();
            }
        }

        ICommand runCommand = null;
        public ICommand RunCommand
        {
            get
            {
                return runCommand;
            }
            set
            {
                runCommand = value;
                OnPropertyChanged();
            }
        }

        ICommand resetScriptCommand = null;
        public ICommand ResetScriptCommand
        {
            get
            {
                return resetScriptCommand;
            }
            set
            {
                resetScriptCommand = value;
                OnPropertyChanged();
            }
        }

        ICommand saveModCommand = null;
        public ICommand SaveModCommand
        {
            get
            {
                return saveModCommand;
            }
            set
            {
                saveModCommand = value;
                OnPropertyChanged();
            }
        }
        #endregion

        bool issearchvisible = true;
        public bool IsSearchVisible
        {
            get
            {
                return issearchvisible;
            }
            set
            {
                SetProperty(ref issearchvisible, value);
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
                isSelected = value;
                OnPropertyChanged();
            }
        }


        bool hasThumb = false;
        public bool HasThumbnail  // KFreon: Indicates whether thumbnail has been generated
        {
            get
            {
                return hasThumb;
            }
            set
            {
                hasThumb = value;
                OnPropertyChanged();
            }
        }

        public string EntryName  // KFreon: String to display in GUI
        {
            get
            {
                return Name + "  --> Size: " + size;
            }
            set
            {

            }
        }

        string siz = null;
        public string size  // KFreon: Size of data as string
        {
            get
            {
                if (siz == null)
                {
                    // KFreon: Calculate size
                    siz = UsefulThings.General.GetFileSizeAsString(Length, true);
                }
                return siz;
            }
        }

        BitmapImage bmp = null;
        public BitmapImage Thumbnail  // KFreon: Thumbnail of job data
        {
            get
            {
                return bmp;
            }
            set
            {
                bmp = value;
                if (bmp != null)
                    HasThumbnail = true;
                OnPropertyChanged();
            }
        }

        string details = null;
        public string Details   // KFreon: Gets details of job as single string
        {
            get
            {
                return details;
            }
            set
            {
                details = value;
                OnPropertyChanged();
            }
        }
        #endregion


        bool requires = false;
        public bool RequiresUpdate  // KFreon: Indicates whether job update is required
        {
            get
            {
                return requires;
            }
            set
            {
                requires = value;
                OnPropertyChanged();
            }
        }

        string script = null;
        public string Script   // KFreon: Job script
        {
            get
            {
                return script;
            }
            set
            {
                script = value;
                OnPropertyChanged();
            }
        }

        public string OriginalScript = null;  // KFreon: Original script for restoring from a user modified script
        public string Name = null;
        public string ObjectName   // KFreon: Gets whether job is a texture or mesh
        {
            get
            {
                string retval = Name;
                if (Name.Contains("Binary"))
                    retval = Name.Split('"')[1].Trim();
                else
                {
                    if (Name.Contains(':'))
                        retval = Name.Split(':')[1].Trim();
                    else if (Name.Contains(" in "))
                        retval = Name.Substring(Name.IndexOf(" in ") + 4);
                }
                return retval;
            }
        }

        // KFreon: Data details
        public bool DontUseCache { get; set; }
        public uint Offset { get; private set; }
        public uint Length { get; private set; }
        public List<PCCEntry> Orig = null;
        public RangedObservableCollection<PCCEntry> PCCs { get; set; }
        public string Texname { get; set; }
        public string JobType { get; set; }

        int game = -1;
        public int GameVersion  // KFreon: Job game target
        {
            get
            {
                return game;
            }
            set
            {
                game = value;
                OnPropertyChanged();
            }
        }

        bool valid = false;
        public bool Valid  // KFreon: Indicates validity
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

        private string CacheFile { get; set; }

        // KFreon: Gets and sets data by storing it in a data cache file. Unfortunately, not everyone has 64 bit Windows.
        public byte[] data
        {
            get
            {
                if (DontUseCache)
                {
                    if (!File.Exists(CacheFile))
                        throw new InvalidOperationException("Opted to not use default cache, but didn't specifiy CacheFile.");
                    else
                    {
                        if (Length == 0)
                            throw new InvalidOperationException("Length of data not set.");
                        else
                        {
                            using (FileStream fs = new FileStream(CacheFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                fs.Seek(Offset, SeekOrigin.Begin);
                                return fs.ReadBytes(Length);
                            }
                        }
                    }
                }
                else
                    return ModMakerHelper.GetDataFromCache(Length, Offset);
            }
            set
            {
                if (!DontUseCache)
                {
                    uint offset = 0;
                    uint length = 0;
                    ModMakerHelper.WriteDataToCache(ref length, value, ref offset);
                    Offset = offset;
                    Length = length;
                }
            }
        }
        #endregion


        #region Constructors
        /// <summary>
        /// Constructor. Empty cos things get added dynamically.
        /// </summary>
        public ModJob()
        {
            PCCs = new RangedObservableCollection<PCCEntry>();
            GameVersion = -1;
            RequiresUpdate = false;
        }


        /// <summary>
        /// Extended constructor. Sets job commands.
        /// </summary>
        /// <param name="commands">List of commands for job.</param>
        public ModJob(List<ICommand> commands)
            : this()
        {
            if (commands != null)
            {
                UpdateCommand = commands[0];
                ExtractCommand = commands[1];
                SaveModCommand = commands[2];
                ResetScriptCommand = commands[3];
                RunCommand = commands[4];
                GenerateThumbnailCommand = commands[5];
            }
        }
        #endregion


        #region Misc
        /// <summary>
        /// Resets script to as loaded.
        /// </summary>
        public void ResetScript()
        {
            Script = OriginalScript;
            RequiresUpdate = true;
        }


        /// <summary>
        /// Gets PCC names out of PCCEntry list. Optionally only checked entries.
        /// </summary>
        /// <param name="OnlyChecked">True = Only includes checked entries, false includes all.</param>
        /// <returns>List of PCC names.</returns>
        public IEnumerable<string> GetPCCsAsList(bool OnlyChecked)
        {
            return from element in PCCs
                where (OnlyChecked ? element.Using : true)  // KFreon: Decide whether to filter or not
                select element.File;
        }


        /// <summary>
        /// Gets ExpIDs out of PCCEntry list. Optionally only checked entries.
        /// </summary>
        /// <param name="OnlyChecked">True = Only includes checked entries, false includes all.</param>
        /// <returns>List of ExpIDs.</returns>
        public IEnumerable<int> GetExpIDsAsList(bool OnlyChecked)
        {
            return from element in PCCs
                where (OnlyChecked ? element.Using : true)  // KFreon: Decide whether to filter or not
                select element.ExpID;
        }
        #endregion


        #region Job Loading/Setup
        /// <summary>
        /// Creates thumbnail image of job.
        /// </summary>
        internal void CreateJobThum()
        {
            using (MemoryTributary imgData = new MemoryTributary(data))
                using (ResILImage img = new ResILImage(imgData))
                    Thumbnail = img.ToImage(width: 64);
        }


        /// <summary>
        /// Decides what current job is. If a texture job, returns TEXTURE, else OTHER. For now anyway, likely add mesh detection later.
        /// </summary>
        /// <returns>Job type.</returns>
        public string DetectJobType()
        {
            string retval;
            if (Script.Contains("Texplorer"))
                retval = "TEXTURE";
            else
                retval = "MESH";
            return retval;
        }


        /// <summary>
        /// Gets details, like pcc's and expID's, from current script and sets local properties.
        /// Properties:
        ///     ExpID's, PCC's, Texname, WhichGame, JobType.
        /// </summary>
        public void GetJobDetails()
        {
            // KFreon: Decide whether job is a texture or not
            JobType = DetectJobType();
            bool isTexture = JobType == "TEXTURE" ? true : false;

            // KFreon: Get PCCs and ExpIDs
            List<int> ExpIDs = ModMakerHelper.GetExpIDsFromScript(Script, isTexture);
            List<string> pccs = ModMakerHelper.GetPCCsFromScript(Script, isTexture);

            // KFreon: Detect other properties
            Texname = ModMakerHelper.GetObjectNameFromScript(Script, isTexture);
            GameVersion = ModMakerHelper.GetGameVersionFromScript(Script, isTexture);

            // KFreon: Guess game version if required
            if (GameVersion == -1)
                GameVersion = GuessGame(pccs);
            

            // KFreon: Create list of PCCEntries, Basically for GUI use :(
            Action<PCCEntry> PCCEntryChangeAction = entry =>
            {
                if (JobType == "TEXTURE")
                    Script = KFreonLibME.Scripting.ModMakerHelper.GenerateTextureScript(ExecPath, GetPCCsAsList(true), GetExpIDsAsList(true), Texname, GameVersion, pathBIOGame);

                // KFreon: Ignore meshes as there's only 1 pcc in mesh mods (I think...)
            };

            // KFreon: Get ExpID's if required
            if (ExpIDs.Count == 0)
            {
                string biogame = MEDirectories.MEDirectories.GetDefaultBIOGame(GameVersion);
                List<string> gameFiles = MEDirectories.MEDirectories.EnumerateGameFiles(GameVersion, biogame);
                foreach (string pcc in pccs)
                {
                    int index = -1;
                    if ((index = gameFiles.FindIndex(t => t.Contains(pcc, StringComparison.InvariantCultureIgnoreCase))) >= 0)
                    {
                        AbstractPCCObject pccObject = AbstractPCCObject.Create(gameFiles[index], GameVersion, biogame);
                        int count = 0;
                        foreach (AbstractExportEntry export in pccObject.Exports)
                        {
                            if (export.ObjectName.Contains(Texname))
                                ExpIDs.Add(count);
                            count++;
                        }
                    }
                }
            }

            PCCs.AddRange(PCCEntry.PopulatePCCEntries(pccs, ExpIDs, PCCEntryChangeAction));

            

            OriginalScript = Script;

            //Details = GetDetails();
        }

        private int GuessGame(List<string> pccs)
        {
            int[] NumFounds = new int[3] { 0, 0, 0 };

            DebugOutput.PrintLn("Starting to guess game...");

            IEnumerable<string>[] GameFiles = new IEnumerable<string>[3];
            GameFiles[0] = ME1Directory.Files;
            DebugOutput.PrintLn("Got ME1 files...");
            GameFiles[1] = ME2Directory.Files;
            DebugOutput.PrintLn("Got ME2 files...");
            GameFiles[2] = ME3Directory.Files;
            DebugOutput.PrintLn("Got ME3 Gamefiles...");

            try
            {
                int test = 0;
                foreach (IEnumerable<string> gamefiles in GameFiles)
                {
                    if (gamefiles == null)
                    {
                        DebugOutput.PrintLn("Gamefiles was null in GuessGame for ME" + test++ + 1);
                        continue;
                    }
                    int found = 0;
                    Parallel.ForEach(pccs, pcc =>
                    {
                        if (pcc == null)
                        {
                            DebugOutput.PrintLn("PCC was null in GuessGame related to ME" + test + 1);
                            return;
                        }
                        //DebugOutput.PrintLn("Searching for game in pcc: " + pcc);
                        if (gamefiles.FirstOrDefault(t => t.Contains(pcc)) != null)
                            found++;
                    });

                    NumFounds[test++] = found;
                }
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Error guessing game: " + e.ToString());
            }


            DebugOutput.PrintLn("Finished guessing game.");

            return NumFounds.ToList().IndexOf(NumFounds.Max()) + 1;
        }

        public bool Validate()
        {
            bool pccCount = PCCs.Where(t => t.ExpID > 0).Count() != 0;
            bool filenamesCount = PCCs.Where(y => !String.IsNullOrEmpty(y.File)).Count() != 0;
            bool texnameCheck = !String.IsNullOrEmpty(Texname);
            bool whichgameCheck = GameVersion != -1;
            bool updateCheck = !RequiresUpdate;
            bool isDLC = PCCs.Count == 0 ? false : !PCCs[0].File.Contains("sfar");
            return pccCount && filenamesCount && texnameCheck && whichgameCheck && updateCheck && isDLC;
        }

        /*public string GetDetails()  change this to proper gui stuff rather than a string
        {
            StringBuilder message = new StringBuilder();
            int pccCount = PCCs.Where(t => !String.IsNullOrEmpty(t.File)).Count();
            int expIDCount = PCCs.Where(t => t.ExpID != -1).Count();
            message.AppendLine("Object Name:   " + Texname);
            message.AppendLine("Number of ExpID's detected:   " + expIDCount);
            message.AppendLine("Number of PCC's detected:   " + pccCount);
            message.Append("Valid?   " + Valid + "  ");


            // KFreon: Check flimsy validity
            if (expIDCount == 0)
                message.Append("EXPIDS ");

            if (pccCount == 0)
                message.Append("PCCS ");

            if (Texname == "")
                message.Append("TEXNAME ");

            if (GameVersion == -1)
                message.Append("WHICHGAME ");

            if (RequiresUpdate)
                message.Append("SCRIPT");

            return message.ToString();
        }*/


        /// <summary>
        /// Updates current job script to new format. Returns true if all bits to udpdate are found. NOTE that true does not mean updated script works.
        /// </summary>
        /// <param name="BIOGames">List of BIOGame paths for the games. MUST have only 3 elements. Each can be null if game files not found.</param>
        /// <param name="ExecFolder">Path to the ME3Explorer \exec\ folder.</param>
        /// <param name="StatusReporter">Callback function for reporting status changes.</param>
        /// <returns>True if all bits to update are found in current script.</returns>
        public bool UpdateJob(List<string> BIOGames, string ExecFolder, Action<string> StatusReporter)
        {
            StatusReporter("Updating Job: " + Name);
            DebugOutput.PrintLn("Updating Job: " + Name);

            bool retval = true;

            // KFreon: Ensure game target known
            if (GameVersion == -1)
            {
                DebugOutput.PrintLn("Game target unknown. Searching...");

                // KFreon: See if given pcc's exist on disk, and if so which game to they belong to. All basegame pcc's must be part of the same game.
                int game = PCCObjects.Misc.SearchForPCC(null, BIOGames, null, ObjectName, JobType == "TEXTURE", PCCs.ToList());

                if (game == -1)
                {
                    DebugOutput.PrintLn("Unable to find pcc's for job: " + Name);
                    retval = false;
                    GameVersion = 0;
                }
                else
                    GameVersion = game;
            }
            DebugOutput.PrintLn("Game target: " + GameVersion);

            // KFreon: Return if already failed
            if (!retval)
            {
                DebugOutput.PrintLn("Update failed for: {0}! Game target undeterminable.", Name);
                StatusReporter("Update failed! Game target undeterminable.");
                return retval;
            }


            // KFreon: If texture job, fix pcc pathing.
            string pathBIOGame = GameVersion == 1 ? Path.GetDirectoryName(BIOGames[GameVersion - 1]) : BIOGames[GameVersion - 1];

            // KFreon: Deal with multiple files found during search
            List<string> multiples;
            List<int> MultiInds;

            // KFreon: Must be the same number of pcc's and expID's
            int pccCount = PCCs.Where(h => h.File != null).Count();
            int expIDCount = PCCs.Where(j => j.ExpID > 0).Count();
            if (pccCount != expIDCount)
                DebugOutput.PrintLn("Job: {0} has {1} PCC's and {2} ExpID's. Incorrect, so skipping...", Name, pccCount, expIDCount);
            else
            {
                string script = "";
                ModMakerHelper.ValidateGivenModPCCs(PCCs, ObjectName, pathBIOGame, out multiples, out MultiInds, ref retval, JobType == "TEXTURE");
                Orig = new List<PCCEntry>(PCCs);

                // KFreon: Texture job
                if (JobType == "TEXTURE")
                {
                    DebugOutput.PrintLn(Name + " is a texture mod.");

                    // KFreon: Get script for job
                    script = ModMakerHelper.GenerateTextureScript(ExecFolder, GetPCCsAsList(false), GetExpIDsAsList(false), Texname, GameVersion, pathBIOGame);
                }
                else
                {
                    // KFreon: HOPEFULLY a mesh mod...
                    DebugOutput.PrintLn(Name + " is a mesh mod. Hopefully...");

                    script = ModMakerHelper.GenerateMeshScript(PCCs[0].ExpID.ToString(), PCCs[0].File);
                }
                Script = script;
            }

            RequiresUpdate = !retval;
            DebugOutput.PrintLn((retval ? "Job: {0} updated!" : "Update failed for: {0}! Unable to validate PCC's."), Name);
            StatusReporter((retval ? "Job updated!" : "Update failed. Unable to validate PCC's."));

            Valid = Validate();
            //Details = GetDetails(); 

            return retval;
        }
        #endregion


        #region Operations
        /// <summary>
        /// Write current job to fileStream.
        /// </summary>
        /// <param name="fs">FileStream to write to.</param>
        public bool WriteJobToFile(FileStream fs)
        {
            bool retval = true;
            try
            {
                // KFreon: Write job name
                /*fs.WriteValueS32(Name.Length);
                foreach (char c in Name)
                    fs.WriteByte((byte)c);*/
                fs.WriteString(Name);

                // KFreon: Write script
                /*fs.WriteValueS32(Script.Length);
                foreach (char c in Script)
                    fs.WriteByte((byte)c);*/
                fs.WriteString(Script);

                // KFreon: Write job data
                fs.WriteValueS32(data.Length);
                fs.WriteBytes(data);
                retval = true;
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Error writing job: {0} to file: {1} with error: ", "Modmaker WriteJobToFile", e, Name, fs.Name);
                retval = false;
            }
            return retval;
        }


        /// <summary>
        /// Saves job data to file.
        /// </summary>
        /// <param name="filepath">Path to extract data to.</param>
        /// <returns>Null if successful, error message otherwise.</returns>
        public string ExtractData(string filepath)   
        {
            try
            {
                File.WriteAllBytes(filepath, data);
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Saving Job: " + Name + " failed with error: ", "Modmaker Extract Job Data", e);
                return e.ToString();
            }
            return null;
        }


        /// <summary>
        /// Runs job.
        /// </summary>
        /// <param name="whichGame">Game target of this job.</param>
        /// <returns>List of DLC's changed.</returns>
        public List<string> RunJob(out int whichGame)
        {
            DebugOutput.PrintLn("Installing job: " + Name);

            List<string> DLCs = new List<string>();
            whichGame = GameVersion;

            // KFreon: Get script and data
            ScriptCompiler sc = new ScriptCompiler();
            ModMakerHelper.ModData = data;
            sc.rtb1.Text = Script;

            // KFreon: Compile and run script, and get list of modified DLC files.
            try
            {
                sc.Compile();
                foreach (PCCEntry pcc in PCCs)
                {
                    string dlcname = MEDirectories.MEDirectories.GetDLCNameFromPath(pcc.File);
                    if (dlcname != null && dlcname != "" && !DLCs.Contains(dlcname))
                        DLCs.Add(dlcname);
                }
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Error occured: ", "Modmaker RunJob", e);
            }
            return DLCs;
        }
        #endregion

        public void SetFileAsData(FileInfo info)
        {
            SetFileAsData(info.FullName, 0, (uint)info.Length);
        }

        public void SetFileAsData(string filename, uint offset, uint length)
        {
            CacheFile = filename;
            Offset = offset;
            Length = length;
            DontUseCache = true;
        }
    }
}
