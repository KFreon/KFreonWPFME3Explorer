using KFreonLibME.Textures;
using KFreonLibME.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using UsefulThings;
using KFreonLibGeneral.Debugging;
using System.Diagnostics;

namespace KFreonLibME
{
    /// <summary>
    /// Interaction logic for KFreonTPFToolsV3.xaml
    /// </summary>
    public partial class KFreonTPFToolsV3 : Window
    {
        KFreonTPFToolsViewModel vm;

        // KFreon: Valid image extensions. Probably not limited to these, but there needs to be some limitations.
        public List<string> exts = new List<string> { ".dds", ".png", ".jpg", ".bmp", ".gif" };

        public KFreonTPFToolsV3()
        {
            InitializeComponent();
            KFreonLibME.Misc.Methods.UpgradeProperties();


            KFreonLibGeneral.Debugging.DebugOutput.StartDebugger("TPF/DDS Tools");


            vm = new KFreonTPFToolsViewModel(ExtractConvertGUIPart, ReplaceGUIPart);
            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "DisplayGameInfo")
                    Helpers.Methods.ShowPathInfo(vm, "Mass Effect " + vm.DisplayGameInfo, vm.DisplayGameInfo);
            };

            DataContext = vm;
        }

        private void ExtractConvertGUIPart(TPFTexInfo texture, TextureFormat format)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Select destination";
            sfd.Filter = "DirectX Files|*.dds";
            sfd.FileName = texture.EntryName.GetPathWithoutInvalids() + KFreonLibME.Textures.Methods.FormatTexmodHashAsString(texture.Hash);
            if (sfd.ShowDialog() == true)
            {
                bool success = false;
                if (format == TextureFormat.Unknown)
                    try
                    {
                        File.WriteAllBytes(sfd.FileName, texture.Extract());
                        success = true;
                    }
                    catch (Exception e)
                    {
                        DebugOutput.PrintLn("Failed to extract Texture: {0}.  Reason: ", "TPFTools Extract", e, texture.EntryName);
                    }
                else
                {
                    if (texture.ExtractConvert(sfd.FileName, format))
                        success = true;
                }


                if (success)
                    vm.PrimaryStatus = "Texture extracted/converted!";
                else
                    vm.PrimaryStatus = "Texture extraction/conversion failed! See DebugWindow for details.";
            }
        }

        private void ReplaceGUIPart(TPFTexInfo texture)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Select replacement image.";
            sfd.Filter = UsefulThings.General.GetExtsAsFilter(exts, "Image files");
            sfd.FileName = texture.EntryName.GetPathWithoutInvalids();
            if (sfd.ShowDialog() == true)
            {
                if (texture.Replace(sfd.FileName))
                    vm.PrimaryStatus = "Texture replaced!";
                else
                    vm.PrimaryStatus = "Texture replacement failed! See DebugWindow for details.";
            }
        }
        

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadHandler();
        }

        private async Task<bool> LoadHandler()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "All Supported|*.dds;*.tpf;*.MEtpf;*.jpg;*.jpeg;*.png;*.bmp;*.mod|DDS Images|*.dds|Texmod TPF's|*.tpf|Texplorer TPF's|*.MEtpf|Images|*.jpg;*.jpeg;*.png;*.bmp;*.dds|Standard Images|*.jpg;*.jpeg;*.png;*.bmp|ME_Explorer MOD's|*.mod";
            ofd.Title = "Select files to load.";
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == true)
            {
                await vm.LoadFiles(ofd.FileNames);
                return true;
            }
            else
                return false;
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            vm.Clear();
        }

        private async void InstallValidButton_Click(object sender, RoutedEventArgs e)
        {
            bool autofix = AutofixCheckBox.IsChecked == true;
            if (await CheckVMState())
            {
                if (autofix)
                    await vm.AutoFix();

                await vm.InstallTextures();
            }
        }

        private void AutofixInstallButton_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private async void AnalyseButton_Click(object sender, RoutedEventArgs e)
        {
            CheckVMState();
        }

        private async Task<bool> CheckVMState()
        {
            bool analysing = true;
            if (vm.Textures.Count == 0)
            {
                if ((analysing = await LoadHandler()))
                    vm.AnalyseWithTexplorer();
            }
            else
                vm.AnalyseWithTexplorer();
            return analysing;
        }

        private void SavePCCButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Select destination";
            sfd.Filter = "Text File|*.txt";
            if (sfd.ShowDialog() == true)
                vm.SavePCCList(sfd.FileName, vm.Textures);
        }

        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.GetType() == typeof(TPFTexInfo))
                    vm.GetPreview((TPFTexInfo)e.AddedItems[0]);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vm.SaveProperties();
        }

        private async void AutoFixALL_Click(object sender, RoutedEventArgs e)
        {
            await vm.AutoFix();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            bool enabled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Debug.WriteLine("Got data present.");
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                Debug.WriteLine("Files: " + String.Join(Environment.NewLine, files));

                if (files != null)
                {
                    foreach (string file in files)
                    {
                        string ext = System.IO.Path.GetExtension(file).ToUpperInvariant();
                        Debug.WriteLine("Ext: " + ext);
                        if (ext != ".TPF" && ext != ".DDS" && !ResILWrapper.ResILImage.ValidFormats.Contains(t => t.Contains(ext, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            enabled = false;
                            break;
                        }
                    }
                }
                else
                    enabled = false;
            }
            else
                enabled = false;

            Debug.WriteLine("Enabled? " + enabled);
            if (enabled)
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            vm.LoadFiles(files);
        }

        private void MainListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                TPFTexInfo selected = ((ListBox)sender).SelectedItem as TPFTexInfo;
                if (selected != null)
                    vm.RemoveEntry(selected);
            }
        }
    }
}
