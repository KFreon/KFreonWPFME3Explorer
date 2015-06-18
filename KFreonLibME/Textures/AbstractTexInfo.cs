using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using UsefulThings.WPF;

namespace KFreonLibME.Textures
{
    public abstract class AbstractTexInfo : ViewModelBase, IToolEntry
    {
        #region Properties
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


        string texname = null;
        public string EntryName
        {
            get
            {
                return texname;
            }
            set
            {
                SetProperty(ref texname, value);
            }
        }

        public RangedObservableCollection<PCCEntry> PCCs {get; set;}

        uint hash = 0;
        public virtual uint Hash
        {
            get
            {
                return hash;
            }
            set
            {
                SetProperty(ref hash, value);
            }
        }

        int gameversion = 0;
        public int GameVersion
        {
            get
            {
                return gameversion;
            }
            set
            {
                SetProperty(ref gameversion, value);
            }
        }

        string pathbio = null;
        public string PathBIOGame
        {
            get
            {
                return pathbio;
            }
            set
            {
                SetProperty(ref pathbio, value);
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

        int numMips = -1;
        public int NumMips
        {
            get
            {
                return numMips;
            }
            set
            {
                SetProperty(ref numMips, value);
            }
        }

        TextureFormat format = TextureFormat.Unknown;
        public TextureFormat Format
        {
            get
            {
                return format;
            }
            set
            {
                SetProperty(ref format, value);
            }
        }

        int width = -1;
        public int Width
        {
            get
            {
                return width;
            }
            set
            {
                SetProperty(ref width, value);
            }
        }

        int height = -1;
        public int Height
        {
            get
            {
                return height;
            }
            set
            {
                SetProperty(ref height, value);
            }
        }
        #endregion Properties

        public ICommand SelectAllCommand { get; set; }
        public ICommand DeSelectAllCommand { get; set; }

        public AbstractTexInfo()
        {
            PCCs = new RangedObservableCollection<PCCEntry>();

            SelectAllCommand = new UsefulThings.WPF.CommandHandler(() => KFreonLibME.Misc.Methods.ChangeListSelection(this, true));
            DeSelectAllCommand = new UsefulThings.WPF.CommandHandler(() => KFreonLibME.Misc.Methods.ChangeListSelection(this, false));
        }
    }
}
