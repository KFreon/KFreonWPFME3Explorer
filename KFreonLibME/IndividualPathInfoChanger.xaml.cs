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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UsefulThings.WPF;

namespace KFreonLibME
{
    /// <summary>
    /// Interaction logic for IndividualPathInfoChanger.xaml
    /// </summary>
    public partial class IndividualPathInfoChanger : Window
    {
        IndividualChangerViewModel vm = null;
        public IndividualPathInfoChanger(string title, int game)
        {
            vm = new IndividualChangerViewModel(title, game);
            DataContext = vm;
            InitializeComponent();
        }

        private void Savebutton_Click(object sender, RoutedEventArgs e)
        {
            vm.Save();
            DialogResult = true;
            Close();
        }
    }

    public class BasePathChangerViewModel
    {
        #region Properties
        public string ImageURI { get; set; }
        public KFreonLibME.MEDirectories.MEDirectories MEExDirec = null;
        public virtual int WhichGame { get; set; }
        public virtual string ExePath { get; set; }
        public virtual string BIOGamePath { get; set; }
        public virtual string DLCPath { get; set; }
        public virtual string CookedPath { get; set; }
        public virtual bool AllowExtraMods { get; set; }
        public string TitleText { get; set; }

        public ObservableCollection<string> DLCs { get; set; }
        
        #region Commands
        public ICommand ExeBrowseCommand
        {
            get
            {
                return new CommandHandler(() => ExePath = BrowseButton(true) ?? ExePath, true);
            }
        }
        public ICommand BIOGameBrowseCommand
        {
            get
            {
                return new CommandHandler(() => BIOGamePath = BrowseButton(folderName: "BIOGame") ?? BIOGamePath, true);
            }
        }
        public ICommand DLCBrowseCommand
        {
            get
            {
                return new CommandHandler(() => DLCPath = BrowseButton(folderName: "DLC") ?? DLCPath, true);
            }
        }
        public ICommand CookedBrowseCommand
        {
            get
            {
                return new CommandHandler(() => CookedPath = BrowseButton(folderName: "Cooked") ?? CookedPath, true);
            }
        }
        #endregion
        #endregion

        public BasePathChangerViewModel(int game)
        {
            // KFreon: Setup properties
            MEExDirec = new KFreonLibME.MEDirectories.MEDirectories(game);
            WhichGame = game;

            AllowExtraMods = false;

            ExePath = MEExDirec.ExePath;
            BIOGamePath = MEExDirec.PathBIOGame;
            DLCPath = MEExDirec.DLCPath;
            CookedPath = MEExDirec.pathCooked;

            DLCs = new ObservableCollection<string>(MEDirectories.MEDirectories.GetInstalledDLC(DLCPath).Select(t => MEDirectories.MEDirectories.GetDLCNameFromPath(t)));

            ImageURI = "/KFreonLibME;component/Resources/Mass Effect " + game + ".jpg";
        }

        public string BrowseButton(bool isExe = false, string folderName = null)
        {
            string retval = null;
            if (isExe)
            {
                string game = WhichGame == 1 ? "" : WhichGame.ToString();

                Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
                ofd.Title = "Select Mass Effect " + game + ".exe";
                ofd.Filter = "MassEffect" + WhichGame + ".exe|MassEffect" + game + ".exe";
                if (ofd.ShowDialog() == true)
                    retval = ofd.FileName;
            }
            else
            {
                using (System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog())
                {
                    fbd.Tag = "Select " + folderName + " location";
                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        retval = fbd.SelectedPath;
                }
            }
            return retval;
        }

        internal virtual void Save()
        {
            if (!String.IsNullOrEmpty(BIOGamePath))
                MEExDirec.PathBIOGame = BIOGamePath;

            if (!String.IsNullOrEmpty(CookedPath))
                MEExDirec.pathCooked = CookedPath;

            if (!String.IsNullOrEmpty(ExePath))
                MEExDirec.ExePath = ExePath;

            if (!String.IsNullOrEmpty(DLCPath))
                MEExDirec.DLCPath = DLCPath;

            //MEExDirec.SaveInstanceSettings();
        }
    }

    public class IndividualChangerViewModel : BasePathChangerViewModel, INotifyPropertyChanged
    {
        #region Properties
        bool allow = false;
        public override bool AllowExtraMods
        {
            get
            {
                return allow;
            }
            set
            {
                allow = value;
                OnPropertyChanged();
            }
        }

        string exepath = null;
        public override string ExePath
        {
            get
            {
                return exepath;
            }
            set
            {
                exepath = value;

                // KFreon: Adjust other properties if necessary
                if (!AllowExtraMods)
                {
                    BIOGamePath = MEDirectories.MEDirectories.GetBIOGameFromExe(exepath, WhichGame);
                    CookedPath = MEDirectories.MEDirectories.GetCookedFromBIOGame(BIOGamePath, WhichGame);
                    DLCPath = MEDirectories.MEDirectories.GetDLCFromBIOGame(BIOGamePath, WhichGame);
                }
                OnPropertyChanged();
            }
        }

        string bio = null;
        public override string BIOGamePath
        {
            get
            {
                return bio;
            }
            set
            {
                bio = value;
                OnPropertyChanged();
            }
        }

        string cooked = null;
        public override string CookedPath
        {
            get
            {
                return cooked;
            }
            set
            {
                cooked = value;
                OnPropertyChanged();
            }
        }

        string dlc = null;
        public override string DLCPath
        {
            get
            {
                return dlc;
            }
            set
            {
                dlc = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public IndividualChangerViewModel(string title, int game) : base(game)
        {
            TitleText = title;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
