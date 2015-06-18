using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Gibbed.IO;
using KFreonLibGeneral.Debugging;
using KFreonLibME.Scripting.ModMaker;
using KFreonLibME.Textures;
using KFreonLibME.ViewModels;
using UsefulThings.WPF;
using BitConverter = KFreonLibGeneral.Misc.BitConverter;
using UsefulThings;

namespace KFreonLibME.Scripting
{
    /// <summary>
    /// Provides ModMaker and general .mod functions.
    /// </summary>
    public static class ModMakerHelper
    {
        public static string exec;
        readonly static object Locker = new object();


        #region Properties
        // KFreon: Gets current job data from anywhere incl scripts
        public static byte[] ModData
        {
            get
            {
                return Instance.ModData;
            }
            set
            {
                Instance.ModData = value;
            }
        }


        // KFreon: Allows current ViewModel to be accessed from anywhere.
        static ModMakerViewModel vm;
        public static ModMakerViewModel Instance
        {
            get
            {
                if (vm == null)
                    vm = new ModMakerViewModel();

                return vm;
            }
            set
            {
                vm = value;
            }
        }


        // KFreon: Provides access to ModMaker's job list
        public static MTRangedObservableCollection<ModJob> JobList
        {
            get
            {
                return Instance.LoadedMods;
            }
            set
            {
                Instance.LoadedMods = value;
            }
        }
        #endregion


        #region ModCache Manipulation
        /// <summary>
        /// Get job data from mod cache.
        /// </summary>
        /// <param name="Length">Length of data to read.</param>
        /// <param name="Offset">Offset in cache to start looking from.</param>
        /// <returns>Data read from cache.</returns>
        public static byte[] GetDataFromCache(uint Length, uint Offset)
        {
            byte[] retval = null;
            lock (Locker)
            {
                using (FileStream fs = new FileStream(exec + "ModData.cache", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    fs.Seek((long)Offset, SeekOrigin.Begin);
                    retval = fs.ReadBytes(Length);
                }
            }
            return retval;
        }


        /// <summary>
        /// Write job data to mod cache.
        /// </summary>
        /// <param name="Length">Length of data to write.</param>
        /// <param name="value">Data to write.</param>
        /// <param name="Offset">Offset in cache to start writing at.</param>
        public static void WriteDataToCache(ref uint Length, byte[] value, ref uint Offset)
        {
            lock (Locker)
            {
                if (value.Length > Length)
                {
                    using (FileStream fs = new FileStream(exec + "ModData.cache", FileMode.Append, FileAccess.Write))
                    {
                        Offset = (uint)fs.Position;
                        Length = (uint)value.Length;
                        fs.WriteBytes(value);
                    }
                }
                else
                {
                    using (FileStream fs = new FileStream(exec + "ModData.cache", FileMode.Open, FileAccess.Write))
                    {
                        fs.Seek(Offset, SeekOrigin.Begin);
                        Length = (uint)value.Length;
                        fs.WriteBytes(value);
                    }
                }
            }
        }
        #endregion


        #region Misc
        /// <summary>
        /// Sets up ModMaker static things, like the JobList.
        /// </summary>
        public static void Initialise()
        {
            // KFreon: Setup Executable path and endianness
            exec = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "exec") + "\\";
            BitConverter.IsLittleEndian = true;

            // KFreon: Overwrites file if exists, otherwise create
            File.Create(exec + "ModData.cache");
            /*while (true)
            {
                try
                {
                    File.Delete(exec + "ModData.cache");
                    
                    using (FileStream fs = new FileStream(exec + "ModData.cache", FileMode.CreateNew, FileAccess.Write))
                        fs.WriteByte(0x00);
                    break;
                }
                catch (IOException)
                { System.Threading.Thread.Sleep(50); }
            }*/
        }


        
        /// <summary>
        /// Corrects PCC names, checks for duplicates, and ensures PCC/ExpID combo is of the expected Export type.
        /// </summary>
        /// <param name="PCCs">PCCEntries to check.</param>
        /// <param name="ObjectName">Type of Export object (Texture, mesh)</param>
        /// <param name="pathBIOGame">Path to BIOGame folder.</param>
        /// <param name="multiples">List of PCC's found in multiple games.</param>
        /// <param name="MultiInds">List of Game indicies each was found in.</param>
        /// <param name="retval">Setting to false indicates some PCC's weren't found at all.</param>
        /// <param name="isTexture">Indicates whether PCC's belong to a texture job.</param>
        internal static void ValidateGivenModPCCs(ObservableCollection<PCCEntry> PCCs, string ObjectName, string pathBIOGame, out List<string> multiples, out List<int> MultiInds, ref bool retval, bool isTexture)
        {
            multiples = new List<string>();
            MultiInds = new List<int>();

            // KFreon: Fix pccs
            for (int i = 0; i < PCCs.Count; i++)
            {
                // KFreon: Test if pcc naming is correct. If not, fix.
                string pcc = PCCs[i].File;
                string test = pcc;
                if (!pcc.Contains(pathBIOGame))
                    test = Path.Combine(pathBIOGame, pcc);

                // KFreon: If PCC not at expected location, search for it.
                List<string> searchResults = new List<string>();
                if (!File.Exists(test))
                    searchResults = PCCObjects.Misc.SearchForPCC(pcc, pathBIOGame, PCCs[i].ExpID, ObjectName, isTexture);

                // KFreon: Paths not found at all (probably due to the base directory not existing)
                if (searchResults.Count == 0)
                {
                    retval = false;
                    return;
                }

                // KFreon: Note PCC as being found in multiple games.
                if (searchResults.Count > 0)
                {
                    multiples.AddRange(searchResults);
                    for (int m = 0; m < searchResults.Count; m++)
                        MultiInds.Add(i);
                }
                else if (searchResults.Count != 0)   // KFreon: Single instance found. Store for later.
                {
                    string temp = test.Remove(0, pathBIOGame.Length + 1);
                    if (!temp.Contains("\\\\"))
                        temp = temp.Replace("\\", "\\\\");
                    PCCs[i].File = temp;
                }
                else
                {
                    DebugOutput.PrintLn("Unable to find path for: " + pcc + ". This WILL cause errors later.");
                    PCCs[i].File = pcc;
                    retval = false;
                }
            }

            // KFreon: Deal with multiples
            if (multiples.Count > 0)
            {
                DebugOutput.PrintLn("Dealing with multiples...PROBABLY NOT GOING TO WORK.");

                int found = 0;
                for (int i = 0; i < multiples.Count; i++)
                {
                    // TODO KFREON Need both multiples here
                    string pcc1 = multiples[i];
                    for (int j = i + 1; j < multiples.Count; j++)
                    {
                        string pcc2 = multiples[j];
                        if (pcc1 == pcc2)
                        {
                            found++;

                            if (!pcc1.Contains("\\\\"))
                                pcc1 = pcc1.Replace("\\", "\\\\");

                            PCCs[MultiInds[i]].File = pcc1;
                        }
                    }
                }

                // KFreon: Multiples still unresolved
                if (found != 0)
                {
                    // TODO KFreon add check to look at the given name fisrst. Might have something in it to clarify.
                    // TODO:  KFreon add selection ability
                    DebugOutput.PrintLn("MULTIPLES STILL UNRESOLVED!!!");
                }
            }
        }


        /// <summary>
        /// Create a texture ModJob from a tex2D with some pathing stuff.
        /// </summary>
        /// <param name="tex2D">Texture2D to build job from.</param>
        /// <param name="imgPath">Path of texture image to create job with.</param>
        /// <param name="WhichGame">Game to target.</param>
        /// <param name="pathBIOGame">Path to BIOGame of targeted game.</param>
        /// <returns>New ModJob based on provided image and Texture2D.</returns>
        public static ModJob CreateTextureJob(METexture2D tex2D, string imgPath, int WhichGame, string pathBIOGame)
        {
            // KFreon: Get script
            string script = GenerateTextureScript(exec, tex2D.PCCs.Select(t => t.File), tex2D.PCCs.Select(t => t.ExpID), tex2D.texName, WhichGame, pathBIOGame);
            ModJob job = new ModJob();
            job.Script = script;

            // KFreon: Get image data
            FileInfo info = new FileInfo(imgPath);
            job.SetFileAsData(info);
            // KFreon: The key here is that the data is already on disk, so why duplicate it in the cache?


            job.Name = (tex2D.Mips > 1 ? "Upscale (with MIP's): " : "Upscale: ") + tex2D.texName;
            return job;
        }


        /// <summary>
        /// Gets .mod build version, current toolset build version, and a boolean showing if they match.
        /// </summary>
        /// <param name="version">Version area extracted from .mod. May NOT be version if .mod is outdated.</param>
        /// <param name="newversion">.mod build version if applicable.</param>
        /// <param name="validVers">True if .mod supported by current toolset.</param>
        /// <returns>Current toolset build version.</returns>
        private static string GetVersion(string version, out string newversion, out bool validVers)
        {
            validVers = false;

            // KFreon: Get .mod version bits and check if valid version.
            int numDots = version.Count(f => f == '.');
            if (numDots == 3)
                validVers = true;

            // KFreon: Get toolset version as a weirdly formatted string
            string ExecutingVersion = KFreonLibGeneral.Misc.Methods.GetBuildVersion();
            DebugOutput.PrintLn("Current Version: " + ExecutingVersion + "    Mod built with version: " + version);

            // KFreon: Get major part of the .mod version
            newversion = "";
            if (numDots != 3)
                DebugOutput.PrintLn("Version parse failed: ", "Modmaker Get Job Version", new InvalidOperationException("Version Parse failed."));
            else
                newversion = version.Substring(0, version.LastIndexOf('.'));

            // KFreon: Return major part of toolset version

            return String.Join(".", ExecutingVersion.Substring(0, ExecutingVersion.LastIndexOf('.')));
        }


        /// <summary>
        /// Loads a .mod into the toolset.
        /// </summary>
        /// <param name="file">Filename of .mod to load.</param>
        /// <param name="ExternalCall">Suppresses user input and AutoUpdates if true.</param>
        /// <param name="ProgReporter">Callback to report progress.</param>
        /// <param name="MaxProgReporter">Callback to report maximum progress.</param>
        /// <param name="StatusReporter">Callback to report status.</param>
        /// <param name="commands">List of commands for modjob operations.</param>
        /// <param name="ExecFolder">Location of ME3Explorer.exe</param>
        /// <param name="BIOGames">List of BIOGame paths.</param>
        /// <returns>true if AutoUpdating, null if cancelled loading, false if not AutoUpdating.</returns>
        public static bool? LoadDotMod(string file, Action<int> ProgReporter, Action<int> MaxProgReporter, Action<string> StatusReporter, List<ICommand> commands, string ExecFolder, List<string> BIOGames, List<ModJob> tempJobs)
        {
            bool AutoUpdate = false;
            bool requiresUpdate = false;

            // KFreon: Load from file
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // KFreon: Attempt to get version
                fs.Seek(0, SeekOrigin.Begin);
                int versionLength = fs.ReadValueS32();
                long countOffset = fs.Seek(0, SeekOrigin.Current);  // Just in case
                string version = "";
                int count = -1;
                string ExecutingVersion = null;
                bool validVersion = false;
                if (versionLength > 14)     // KFreon: Version is definitely wrong
                    ExecutingVersion = "";
                else
                {
                    // KFreon: Do version checking
                    for (int i = 0; i < versionLength; i++)
                        version += (char)fs.ReadByte();

                    // KFreon: Get Executing Version and check validity of read .mod version
                    string vers;
                    ExecutingVersion = GetVersion(version, out vers, out validVersion);
                    version = vers;

                    count = fs.ReadValueS32();

                    // KFreon: Check if update required
                    if (version != ExecutingVersion)
                    {
                        AutoUpdate = true;
                    }
                    else   // KFreon: Reset to null to signify success
                        ExecutingVersion = null;
                }



                // KFreon: Update version by default
                if (ExecutingVersion != null)
                {
                    DebugOutput.PrintLn("Update required for: " + file);
                    requiresUpdate = true;

                    // KFreon: Reset stream position if necessary
                    if (!validVersion)
                    {
                        count = versionLength;
                        fs.Seek(countOffset, SeekOrigin.Begin);
                    }
                }


                // KFreon: Set progress
                MaxProgReporter(count);

                // KFreon: Read Data
                DebugOutput.PrintLn("Found " + count + " Jobs");
                for (int i = 0; i < count; i++)
                {
                    DebugOutput.PrintLn("Loading ...  " + (i + 1) + " of " + count + ".");
                    StatusReporter("Loading ...  " + (i + 1) + " of " + count + ".");
                    ProgReporter(i + 1);

                    // KFreon: Read name
                    ModMaker.ModJob md = new ModMaker.ModJob(commands) { RequiresUpdate = requiresUpdate, ExecPath = ExecFolder };
                    md.Name = fs.ReadString((uint)fs.ReadValueS32());

                    // KFreon: Read script
                    md.Script = fs.ReadString((uint)fs.ReadValueS32());

                    // KFreon: Read data - NEW!! Use existing file instead of actually reading data.
                    int len = fs.ReadValueS32();
                    md.SetFileAsData(file, (uint)fs.Position, (uint)len);

                    // KFreon: Move stream position to skip data
                    fs.Seek(len, SeekOrigin.Current);

                    // KFreon: Get pcc's, etc
                    md.GetJobDetails();

                    md.pathBIOGame = BIOGames[md.GameVersion - 1];

                    // KFreon: Update job if requested
                    if (AutoUpdate)
                    {
                        bool updateSuccess =  md.UpdateJob(BIOGames, ExecFolder, StatusReporter);  
                        DebugOutput.PrintLn(updateSuccess ? "Successfully updated job: {0}" : "Failed to update job: {0}", md.Name);
                    }

                    tempJobs.Add(md);
                    DebugOutput.PrintLn("Loaded job: " + md.Name);
                }
            }
            return AutoUpdate;
        }


        /// <summary>
        /// Add Texture job to joblist.
        /// </summary>
        /// <param name="tex2D">Texture to create job from.</param>
        /// <param name="ReplacingImage">Name of texture being replaced.</param>
        /// <param name="WhichGame">Game target of texture.</param>
        /// <param name="pathBIOGame">BIOGame path.</param>
        public static void AddJob(METexture2D tex2D, string ReplacingImage, int WhichGame, string pathBIOGame)
        {
            // KFreon: Initialise if required.
            if (JobList.Count == 0)
                Initialise();

            ModJob job = ModMakerHelper.CreateTextureJob(tex2D, ReplacingImage, WhichGame, pathBIOGame);
            JobList.Add(job);
        }
        #endregion


        #region Writing .mods
        /// <summary>
        /// Writes the first invariable parts of a .mod (version, number of jobs) to FileStream.
        /// </summary>
        /// <param name="fs">FileStream to write to.</param>
        /// <param name="jobcount">Number of jobs. Exists because it's not always JobList.Count.</param>
        public static void WriteModHeader(FileStream fs, int jobcount)
        {
            // KFreon: Write version
            string version = KFreonLibGeneral.Misc.Methods.GetBuildVersion();
            fs.Seek(0, SeekOrigin.Begin);
            /*fs.WriteValueS32(version.Length);
            foreach (char c in version)
                fs.WriteByte((byte)c);*/
            fs.WriteString(version);

            // KFreon: Write number of jobs to be included in this .mod
            fs.WriteValueS32(jobcount);

            DebugOutput.PrintLn("Version: " + version);
            DebugOutput.PrintLn("Number of jobs: " + jobcount);
        }


        /// <summary>
        /// Write single job to file.
        /// </summary>
        /// <param name="filename">File to write to.</param>
        /// <param name="job">Job to write.</param>
        /// <returns>True if success, false otherwise.</returns>
        public static bool WriteJobToFile(string filename, ModJob job)
        {
            bool retval = false;
            DebugOutput.PrintLn("Writing job: " + job.Name + " to file: " + filename);
            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                ModMakerHelper.WriteModHeader(fs, 1);
                retval = job.WriteJobToFile(fs);
            }
            return retval;
        }


        public static bool WriteJobsToFile()
        {
            // TODO: KFreon: Writing multiple jobs to file
            return true;
        }
        #endregion


        #region Script Generation/Manipulation
        /// <summary>
        /// Returns a texture .mod script built from template in exec folder, given pcc's, expID's, texture name, GameVersion, and some extra pathing stuff.
        /// </summary>
        /// <param name="ExecPath">Path to ME3Explorer exec folder.</param>
        /// <param name="pccs">PCC's to be affected by .mod.</param>
        /// <param name="ExpIDs">ExpID's of PCC's of texName to be affected.</param>
        /// <param name="texName">Name of texture to have the .mod edit.</param>
        /// <param name="WhichGame">Number of game texName belongs to.</param>
        /// <param name="pathBIOGame">BIOGame path of game in question.</param>
        /// <returns>Job script based on all the things.</returns>
        public static string GenerateTextureScript(string ExecPath, IEnumerable<string> pccs, IEnumerable<int> ExpIDs, string texName, int WhichGame, string pathBIOGame)
        {
            // KFreon: Get game independent path to remove from all pcc names, in order to make script computer independent.  (i.e. relative instead of absolute paths)
            string MainPath = (WhichGame == 1) ? Path.GetDirectoryName(pathBIOGame) : pathBIOGame;

            // KFreon: Read template in from file
            string script;
            using (StreamReader scriptFile = new StreamReader(ExecPath + "TexScript.txt"))
                script = scriptFile.ReadToEnd();

            // KFreon: Set functions to run
            script = script.Replace("**m1**", "AddImage();");
            script = script.Replace("**m2**", "//No images to remove");
            script = script.Replace("**which**", WhichGame.ToString());

            // KFreon: Add pcc's to script
            string allpccs = "";
            foreach (string filename in pccs)
            {
                //string tempfile = Path.Combine(Path.GetFileName(Path.GetDirectoryName(filename)), Path.GetFileName(filename));
                string tempfile = (filename.Contains(MainPath, StringComparison.CurrentCultureIgnoreCase)) ? filename.Remove(0, MainPath.Length + 1) : filename;

                //tempfile = ModGenerator.UpdatePathing(filename, tempfile);
                tempfile = tempfile.Replace("\\", "\\\\");
                allpccs += "pccs.Add(\"" + tempfile + "\");" + Environment.NewLine + "\t\t\t";
            }
            script = script.Replace("**m3**", allpccs);

            // KFreon: Add ExpID's to script
            string allIDs = "";
            foreach (int id in ExpIDs)
            {
                allIDs += "IDs.Add(" + id + ");" + Environment.NewLine + "\t\t\t";
            }
            script = script.Replace("**m4**", allIDs);

            // KFreon: Add texture name
            script = script.Replace("**m5**", texName);

            return script;
        }


        /// <summary>
        /// Creates mesh script given details.
        /// </summary>
        /// <param name="expID">Export ID of mesh in PCC.</param>
        /// <param name="pcc">Name of PCC.</param>
        /// <returns>Mesh script.</returns>
        public static string GenerateMeshScript(string expID, string pcc)
        {
            string loc = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
            string template = System.IO.File.ReadAllText(loc + "\\exec\\JobTemplate_Binary2.txt");
            template = template.Replace("**m1**", expID);
            template = template.Replace("**m2**", pcc);
            return template;
        }


        /// <summary>
        /// Builds a mesh ModJob from details.
        /// </summary>
        /// <param name="newfile">File to get job data from. Ignored if data specified.</param>
        /// <param name="expID">Export ID of mesh in PCC.</param>
        /// <param name="pccname">Name of PCC.</param>
        /// <param name="data">Mesh data. Can be null - newfile must be specified.</param>
        /// <returns>Mesh job.</returns>
        public static ModJob GenerateMeshModJob(string newfile, int expID, string pccname, byte[] data)
        {
            KFreonLibME.Scripting.ModMaker.ModJob mj = new KFreonLibME.Scripting.ModMaker.ModJob();

            // KFreon: Get replacing data
            /*byte[] buff = data;
            if (data == null)
            {
                
                using (FileStream fs = new FileStream(newfile, FileMode.Open, FileAccess.Read))
                {
                    buff = new byte[fs.Length];
                    int cnt;
                    int sum = 0;
                    while ((cnt = fs.Read(buff, sum, buff.Length - sum)) > 0) sum += cnt;
                }
            }
            else
                buff = data;*/


            // KFreon: Get details
            string currfile = Path.GetFileName(pccname);


            // KFreon: Set data - NEW use existing file instead of copying data
            if (data == null)
                mj.data = data;
            else
                mj.SetFileAsData(new FileInfo(newfile));


            mj.Name = "Binary Replacement for file \"" + currfile + "\" in Object #" + expID + " with " + mj.Length + " bytes of data";
            mj.Script = GenerateMeshScript(expID.ToString(), currfile);
            return mj;
        }


        /// <summary>
        /// Returns list of PCC's from job script.
        /// </summary>
        /// <param name="script">Script to search through.</param>
        /// <param name="isTexture">Indicates whether script relates to a texture.</param>
        /// <returns>List of PCC's found in script.</returns>
        public static List<string> GetPCCsFromScript(string script, bool isTexture)
        {
            List<string> lines = new List<string>(script.Split('\n'));
            List<string> pccs = new List<string>();

            // KFreon: Search through script lines looking for different things based on being a texture or not.
            if (isTexture)
            {
                // KFreon: TEXTURE
                foreach (string line in lines)
                    if (line.Contains("pccs."))   // KFreon: Only look at pcc lines.
                    {
                        string item = line.Split('"')[1];
                        pccs.Add(item);
                    }
                    else if (line.Contains("public void RemoveTex()"))   // KFreon: Stop at the end of the first section.
                        break;
            }
            else
            {
                // KFreon: MESH
                string pathing = "";
                foreach (string line in lines)
                {
                    if (line.Contains("string filename = "))  // KFreon: Get PCC name
                    {
                        string[] parts = line.Split('=');
                        string pcc = parts[1].Split('"')[1];
                        if (pcc.Contains("sfar"))
                            pcc = "TOO OLD TO FIX";
                        pathing += pcc;
                    }
                    else if (line.Contains("string pathtarget = "))  // KFreon: Get PCC path
                    {
                        string[] parts = line.Split('"');
                        string path = "";
                        if (parts.Count() > 1)
                            path = parts[1];
                        pathing = path + pathing;
                    }
                }

                if (pathing != "")
                {
                    pathing = pathing.TrimStart("\\".ToCharArray());
                    pccs.Add(pathing);
                }
            }

            return pccs;
        }

        /// <summary>
        /// Returns list of ExpID's from job script.
        /// </summary>
        /// <param name="script">Script to search through.</param>
        /// <param name="isTexture">Indicates whether script is from a texture job.</param>
        /// <returns>List of ExpID's found in script.</returns>
        public static List<int> GetExpIDsFromScript(string script, bool isTexture)
        {
            List<string> lines = new List<string>(script.Split('\n'));
            List<int> ids = new List<int>();

            // KFreon: Search through script lines.
            if (isTexture)
            {
                foreach (string line in lines)
                    if (line.Contains("IDs."))   // KFreon: Only look at ExpID lines.
                        ids.Add(Int32.Parse(UsefulThings.General.ExtractString(line, "(", ")")));
                    else if (line.Contains("public void RemoveTex()"))   // KFreon: Stop at end of first section.
                        break;
            }
            else
            {
                foreach (string line in lines)
                {
                    if (line.Contains("int objidx = "))  // KFreon: Only look at ExpID lines
                    {
                        string[] parts = line.Split('=');
                        string number = parts[1].Substring(1, parts[1].Length - 3);
                        ids.Add(Int32.Parse(number));
                        break;
                    }
                }
            }

            return ids;
        }


        /// <summary>
        /// Returns name of texture from job script.
        /// </summary>
        /// <param name="script">Script to search through.</param>
        /// <param name="isTexture">Indicates whether script is from a texture job.</param>
        /// <returns>Texture name found in script.</returns>
        public static string GetObjectNameFromScript(string script, bool isTexture)
        {
            List<string> lines = new List<string>(script.Split('\n'));
            string texname = "";

            // KFreon: Search through lines.
            if (isTexture)
            {
                foreach (string line in lines)
                    if (line.Contains("tex."))  // KFreon: Look for texture name
                        return line.Split('"')[1];
            }
            else
                texname = "Some Mesh";

            return texname;
        }


        /// <summary>
        /// Return which game script is targeting.
        /// </summary>
        /// <param name="script">Script to search through.</param>
        /// <param name="isTexture">Indicates whether script is from a texture job.</param>
        /// <returns>Game version.</returns>
        public static int GetGameVersionFromScript(string script, bool isTexture)
        {
            List<string> lines = new List<string>(script.Split('\n'));
            int retval = -1;

            // KFreon: Search through lines.
            if (isTexture)
            {
                foreach (string line in lines)
                {
                    // KFreon: Look for Texplorer line, which contains game target.
                    if (line.Contains("Texplorer2("))
                    {
                        string[] parts = line.Split(',');
                        int test = -1;
                        if (parts.Length > 1)
                            if (int.TryParse(parts[1].Split(')')[0], out test))
                                retval = test;
                    }
                }
            }
            else
                retval = 3;

            return retval;
        }
        #endregion
    }
}
