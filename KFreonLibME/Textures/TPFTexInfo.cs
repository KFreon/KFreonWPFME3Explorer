using KFreonLibGeneral.Debugging;
using KFreonLibME.ViewModels;
using ResILWrapper;
using SaltTPF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using UsefulThings;
using UsefulThings.WPF;

namespace KFreonLibME.Textures
{
    public class TPFTexInfo : AbstractTexInfo
    {
        public string AutoFixPath
        {
            get
            {
                return Path.Combine(UsefulThings.General.GetExecutingLoc(), "TPFToolsTEMP", "ME" + GameVersion, EntryName + (Path.GetExtension(EntryName) != ".dds" ? ".dds" : ""));
            }
        }



        #region Properties
        public RangedObservableCollection<TPFTexInfo> FileDuplicates { get; set; }

        public string SourceInfo
        {
            get
            {
                return isExternal ? "External file\n\nPath: " + FilePath + "\\" + EntryName + Path.GetExtension(OriginalEntryName) : Zippy.Description;
            }
        }

        bool analysed = false;
        public bool Analysed
        {
            get
            {
                return analysed;
            }
            set
            {
                SetProperty(ref analysed, value);
            }
        }

        int expectedMips = -1;
        public int ExpectedMips
        {
            get
            {
                return expectedMips;
            }
            set
            {
                SetProperty(ref expectedMips, value);
                MipsCorrect = ExpectedMips <= NumMips;
            }
        }

        TextureFormat expectedFormat = TextureFormat.Unknown;
        public TextureFormat ExpectedFormat
        {
            get
            {
                return expectedFormat;
            }
            set
            {
                SetProperty(ref expectedFormat, value);
                FormatCorrect = ExpectedFormat == Format;
            }
        }

        public string FilePath { get; set; }
        public int TPFEntryIndex { get; set; }
        readonly object previewlocker = new object();

        
        public bool isExternal
        {
            get
            {
                return FilePath != null;
            }
        }
        public bool isDef
        {
            get
            {
                return (EntryName.Contains(".def", StringComparison.CurrentCultureIgnoreCase) || EntryName.Contains(".txt", StringComparison.CurrentCultureIgnoreCase) || EntryName.Contains(".log", StringComparison.CurrentCultureIgnoreCase)) ? true : false;
            }
        }
        public ZipReader Zippy { get; set; }

        BitmapImage thumbnail = null;
        public BitmapImage Thumbnail
        {
            get
            {
                return thumbnail;
            }
            set
            {
                SetProperty(ref thumbnail, value);
            }
        }
        public uint OriginalHash { get; set; }
        public List<string> LogContents { get; set; }
        #endregion Properties


        public ICommand AutoFixCommand { get; set; }

        public ICommand ExtractConvertCommand { get; set; }   // KFreon: Called by ExtractConvertButton


        private TextureFormat saveFormat = TextureFormat.Unknown;
        public TextureFormat SaveFormat
        {
            get
            {
                return saveFormat;
            }
            set
            {
                SetProperty(ref saveFormat, value);
            }
        }

        public bool ValidTexture
        {
            get
            {
                return FormatCorrect && MipsCorrect;
            }
        }
        public Action<TPFTexInfo, TextureFormat> ExtractConvertDelegate { get; set; }
        public Action<TPFTexInfo> InstallDelegate { get; set; }

        public List<string> ValidImageFormats
        {
            get
            {
                return Enum.GetNames(typeof(TextureFormat)).ToList();
            }
        }

        public ICommand InstallCommand { get; set; }
        public ICommand ResetHashCommand { get; set; }

        bool formatcorrect = false;
        public bool FormatCorrect
        {
            get
            {
                return formatcorrect;
            }
            set
            {
                SetProperty(ref formatcorrect, value);
                OnPropertyChanged("ValidTexture");
            }
        }

        bool mipscorrect = false;
        public bool MipsCorrect
        {
            get
            {
                return mipscorrect;
            }
            set
            {
                SetProperty(ref mipscorrect, value);
                OnPropertyChanged("ValidTexture");
            }
        }

        public override uint Hash
        {
            get
            {
                return base.Hash;
            }
            set
            {
                base.Hash = value;
                OnPropertyChanged("IsHashChanged");
            }
        }

        public bool IsHashChanged
        {
            get
            {
                return OriginalHash != Hash;
            }
        }

        string originalEntryName = null;
        public string OriginalEntryName
        {
            get
            {
                return originalEntryName;
            }
            set
            {
                SetProperty(ref originalEntryName, value);
            }
        }

        public RangedObservableCollection<TPFTexInfo> TreeDuplicates { get; set; }

        public Action<TPFTexInfo> ReplaceDelegate { get; set; }
        public Action<TPFTexInfo> AutoFixDelegate { get; set; }
        public ICommand ReplaceCommand { get; set; }

        public TPFTexInfo() : base()
        {
            ReplaceCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                if (ReplaceDelegate != null)
                {
                    ReplaceDelegate(this);
                }
            }, true);

            ExtractConvertCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                if (ExtractConvertDelegate != null)
                {
                    ExtractConvertDelegate(this, SaveFormat);
                }
            }, true);

            AutoFixCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                if (AutoFixDelegate != null)
                    AutoFixDelegate(this);
            });


            InstallCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                if (InstallDelegate != null)
                    InstallDelegate(this);
            });

            ResetHashCommand = new UsefulThings.WPF.CommandHandler(t =>
            {
                Hash = OriginalHash;
            }, true);

            LogContents = new List<string>();
            FileDuplicates = new RangedObservableCollection<TPFTexInfo>();
            TreeDuplicates = new RangedObservableCollection<TPFTexInfo>();
        }

        public TPFTexInfo(string filename, string path, int tpfind, ZipReader zippy, int gameVersion) : this()
        {
            EntryName = filename;
            OriginalEntryName = filename;
            TPFEntryIndex = tpfind;
            Zippy = zippy;
            GameVersion = gameVersion;
            FilePath = path;

            OnPropertyChanged("IsDef");
        }

        public TPFTexInfo(TreeTexInfo treetex, TPFTexInfo orig) : this(treetex.EntryName, orig.FilePath, orig.TPFEntryIndex, orig.Zippy, treetex.GameVersion)
        {
            OriginalEntryName = orig.OriginalEntryName;
        }

        public void EnumerateDetails()
        {
            // KFreon: Textures only
            if (isDef)
                return;

            byte[] data = Extract();
            if (data == null)
                DebugOutput.PrintLn("Unable to get image data for: " + EntryName);
            else
            {
                try
                {
                    using (ResILImage img = new ResILImage(data))
                    {
                        Width = img.Width;
                        Height = img.Height;
                        NumMips = img.Mips;
                        Format = KFreonLibME.Textures.Methods.ParseTextureFormat(img.SurfaceFormat.ToString());

                        int decodeWidth = img.Width > img.Height ? 64 : 0;
                        int decodeHeight = img.Width > img.Height ? 0 : 64;

                        Thumbnail = img.ToImage(width: decodeWidth, height: decodeHeight);
                    }
                }
                catch (Exception e)
                {
                    Format = TextureFormat.Unknown;
                    DebugOutput.PrintLn("Failed to process image through ResIL: " + OriginalEntryName, "TPFTools EnumerateDetails", e);
                }
            }
        }

        public byte[] Extract()
        {
            byte[] imgdata = null;
            try
            {
                if (isExternal)
                    imgdata = UsefulThings.General.GetExternalData(File.Exists(AutoFixPath) ? AutoFixPath : OriginalEntryName);
                else
                    imgdata = Zippy.Entries[TPFEntryIndex].Extract(true);
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to extract image data.", "TPFTools Extract Image", e);
            }
            return imgdata;
        }

        public async Task<BitmapImage> GetPreview()
        {
            BitmapImage temp = null;
            await Task.Run(() =>
            {
                byte[] data = Extract();
                if (data == null)
                    return;

                try
                {
                    using (ResILImage img = new ResILImage(data))
                    {
                        int decodeWidth = img.Width > img.Height ? 256 : 0;
                        int decodeHeight = img.Width > img.Height ? 0 : 256;

                        temp = img.ToImage(width: decodeWidth, height: decodeHeight);
                    }
                }
                catch (Exception e)
                {
                    DebugOutput.PrintLn("Failed to get preview: " + OriginalEntryName, "TPFTools GetPreview", e);
                }
            });
            
            
            return temp;
        }

        public bool Compare(TPFTexInfo tex)
        {
            if (tex.isDef || isDef)
                return false;

            return Hash == tex.Hash && tex.EntryName == EntryName;
        }

        internal TPFTexInfo UpdateFromTreeTex(TreeTexInfo treetex)
        {
            TPFTexInfo newtex = null;
            if (Hash == treetex.Hash)
            {
                if (PCCs.Count == 0)
                    UpdateTex(treetex);
                else
                {
                    newtex = new TPFTexInfo(treetex, this);
                    TreeDuplicates.Add(newtex);  // filedupes?
                    newtex.TreeDuplicates.Add(this);
                    newtex.Analysed = true;
                }
            }
            Analysed = true;
            return newtex;
        }

        private void UpdateTex(TreeTexInfo treetex)
        {
            PCCs.AddRange(treetex.PCCs);
            
            // KFreon: Get expected stuff
            ExpectedFormat = treetex.Format;
            ExpectedMips = treetex.NumMips;
            EntryName = treetex.EntryName;
        }

        public bool ExtractConvert(string destinationName, TextureFormat format, bool fixMips = false)
        {
            ResILImage img = new ResILImage(Extract());
            ResIL.Unmanaged.ImageType type;
            ResIL.Unmanaged.CompressedDataFormat surface = KFreonLibME.Textures.Methods.ConvertTextureFormatToResIL(format, out type);

                
            // KFreon: Convert image
            MemoryTributary stream = new MemoryTributary();
            
            // KFreon: Convert WITHOUT saving
            if (img.ConvertAndSave(type, stream, surface: surface))
            {
                img.Dispose();  // KFreon: Don't need the old image anymore

                using (ResILImage newimg = new ResILImage(stream))
                {
                    stream.Dispose();  // KFreon: Don't need this stream anymore  

                    // KFreon: Fix mips
                    if (fixMips && KFreonLibME.Textures.Methods.IsTextureFormatDDS(format) && !MipsCorrect && !FixMips(newimg))
                        DebugOutput.PrintLn("Failed to fix mips for: " + EntryName);

                    return newimg.ConvertAndSave(type, destinationName, surface: surface);
                }
            }
            else
            {
                DebugOutput.PrintLn("Failed to convert image: " + EntryName + "  with error: " + ResILImage.GetResILError());
                return false;
            }
        }

        private bool FixMips(ResILImage img)
        {
            // KFreon: Build or remove mips depending on requirements. Note case where expected == existing not present as that's what MipsCorrect is.
            if (ExpectedMips > NumMips)
            {
                if (!img.BuildMipmaps(NumMips == 1))
                {
                    DebugOutput.PrintLn(String.Format("Failed to build mipmaps for {0}: {1}", EntryName, ResILImage.GetResILError()));
                    return false;
                }
            }
            else
            {
                if (!img.RemoveMipmaps(NumMips == 1))
                {
                    DebugOutput.PrintLn(String.Format("Failed to remove mipmaps for {0}: {1}", EntryName, ResILImage.GetResILError()));
                    return false;
                }
            }

            return true;
        }

        public bool Replace(string newImage)
        {
            this.FilePath = Path.GetDirectoryName(newImage);
            bool success = true;

            try
            {
                using (ResILImage img = new ResILImage(newImage))
                {
                    Format = KFreonLibME.Textures.Methods.ConvertResILToTextureFormat(img.imgType, img.SurfaceFormat);
                    Height = img.Height;
                    Width = img.Width;
                    NumMips = img.Mips;

                    if (Height > Width)
                        Thumbnail = img.ToImage(height: Height);
                    else
                        Thumbnail = img.ToImage(width: Width);
                }
            }
            catch (Exception e)
            {
                DebugOutput.PrintLn("Failed to replace image: " + OriginalEntryName + " with: " + newImage, "TPFTools Replace", e);
                success = false;
            }

            FormatCorrect = Format == ExpectedFormat;
            MipsCorrect = NumMips >= ExpectedMips;

            return success;
        }
    }
}
