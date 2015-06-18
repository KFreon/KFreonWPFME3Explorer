using KFreonLibGeneral.Debugging;
using KFreonLibME.PCCObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UsefulThings;
using UsefulThings.WPF;

namespace KFreonLibME.Textures
{
    public class TreeTexInfo : AbstractTexInfo
    {
        METexture2D origTex2D = null;
        public METexture2D OrigTex2D
        {
            get
            {
                return origTex2D;
            }
            set
            {
                SetProperty(ref origTex2D, value);
            }
        }

        METexture2D changedTex2D = null;
        public METexture2D ChangedTex2D
        {
            get
            {
                return changedTex2D;
            }
            set
            {
                SetProperty(ref changedTex2D, value);
                OnPropertyChanged("HasChanged");
                OnPropertyChanged("Details");
            }
        }

        public bool HasChanged
        {
            get
            {
                return ChangedTex2D != null;
            }
        }

        public HierarchicalTreeTexes Parent { get; set; }

        #region Properties
        string thumbpath = null;
        public string ThumbnailPath
        {
            get
            {
                return thumbpath;
            }
            set
            {
                SetProperty(ref thumbpath, value);
            }
        }

        public string Details
        {
            get
            {
                METexture2D tex = ChangedTex2D ?? OrigTex2D;

                if (tex == null && PCCs != null && PCCs.Count > 0)
                {
                    try
                    {
                        AbstractPCCObject pcc = AbstractPCCObject.Create(PCCs[0].File, GameVersion, PathBIOGame);
                        tex = pcc.CreateTexture2D(PCCs[0].ExpID, Hash);
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("Failed to get details for: " + tex.texName, "TreeTex Details", e);
                        return "ERROR: Texture2D is invalid.";
                    }
                    
                }

                if (tex == null)
                    return "ERROR: TEXTURE2D IS NULL.";

                return tex.GetDetailsAsString();
            }
        }

        public string FullPackage { get; set; }

        public int TFCOffset { get; set; }

        string pack = null;
        public string Package
        {
            get
            {
                if (pack == null)
                {
                    string retval = "";
                    if (!String.IsNullOrEmpty(FullPackage))
                    {
                        string temppack = FullPackage.Remove(FullPackage.Length - 1);
                        if (temppack.Split('.').Length > 1)
                            retval = temppack.Split('.')[temppack.Split('.').Length - 1];
                        else
                            retval = temppack.Split('.')[0];

                        pack = retval.ToUpperInvariant();
                    }
                    else
                        pack = "";
                }

                return pack;
            }
        }
        #endregion Properties


        public TreeTexInfo() : base()
        {
        }

        public TreeTexInfo(int game, string pathbio)
            : this()
        {
            PathBIOGame = pathbio;
            GameVersion = game;
        }

        public TreeTexInfo(METexture2D tex, int expID, uint hash, string pccName, ImageInfo info, int game, string pathbio, bool IsInTreeScan) : this(game, pathbio)
        {
            PCCEntry mainentry = new PCCEntry(pccName, expID);
            PCCs.Add(mainentry);

            GameVersion = game;
            NumMips = tex.Mips;
            EntryName = tex.texName;

            Width = (int)info.ImgSize.width;
            Height = (int)info.ImgSize.height;

            // KFreon: TEST Don't add all the data just for treescan...
            if (!IsInTreeScan)
                OrigTex2D = tex;

            Format = tex.texFormat;
            FullPackage = tex.FullPackage.ToUpperInvariant();
            TFCOffset = info.Offset;

            Hash = hash;

            if (tex.FullPackage == "Base Package")
                FullPackage = Path.GetFileNameWithoutExtension(pccName).ToUpperInvariant();
        }



        internal bool Compare(TreeTexInfo tex, string UpperCaseFilenameOnly)
        {
            if (tex.EntryName == EntryName)
                if ((tex.Hash == 0 && tex.TFCOffset == TFCOffset) || (tex.Hash != 0 && tex.Hash == Hash))
                {
                    /*if (tex.GameVersion == 1 && (tex.Package == Package || UpperCaseFilenameOnly.Contains(tex.Package.ToUpperInvariant())))
                        return true;
                    else if (tex.GameVersion != 1)
                        return true;
                    else
                        return false;*/
                    return true;
                }
            return false;
        }

        public void Update(TreeTexInfo tex)
        {
            Update(tex.PCCs.ToList(), tex.Hash);
        }

        public void Update(IEnumerable<PCCEntry> pccs, uint hash)
        {
            // KFreon: Initialise list if necessary
            if (PCCs == null)
                PCCs = new RangedObservableCollection<PCCEntry>();

            PCCs.AddRange(pccs);   // Should only be 1 pcc in pccs -> then can set this to valid or not based on the tex given in the parent function

            // KFreon: Update hash if applicable
            if (Hash == 0 && hash != 0)
                Hash = hash;
        }

        internal byte[] GetImageData()
        {
            byte[] imgData = OrigTex2D.GetRawImageData();

            if (OrigTex2D.texFormat == TextureFormat.V8U8)
            {
                ResILWrapper.ResILImage img = new ResILWrapper.ResILImage(imgData);

                using (MemoryTributary stream = new MemoryTributary())
                {
                    bool success = img.ConvertAndSave(ResIL.Unmanaged.ImageType.Jpg, stream);

                    // KFreon: Done with image now, so dispose of
                    img.Dispose();
                    imgData = stream.ToArray();
                }
            }
            
            return imgData;
        }


    }
}
