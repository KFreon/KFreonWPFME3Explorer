using KFreonLibGeneral.Debugging;
using KFreonLibME.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UsefulThings.WPF;
using UsefulThings;
using Microsoft.Win32;
using System.IO;

namespace KFreonLibME
{
    /// <summary>
    /// Interaction logic for Texplorer2.xaml
    /// </summary>
    public partial class Texplorer2 : Window
    {
        #region Properties
        #region GUI stuff
        ThicknessAnimation MainListContext = new ThicknessAnimation();
        ThicknessAnimation PCCContext = new ThicknessAnimation();
        GridLengthAnimation DetailsCloserAnim = new GridLengthAnimation();
        DoubleAnimation DetailsOpacityAnim = new DoubleAnimation();

        bool detailsState = true;

        ColumnDefinition ThirdColumn
        {
            get
            {
                return MainGrid.ColumnDefinitions[2];
            }
        }

        #endregion GUI Stuff


        TexplorerViewModel vm;
        #endregion

        public Texplorer2()
        {
            InitializeComponent();
            KFreonLibME.Misc.Methods.UpgradeProperties();


            CubicEase easer = new CubicEase();
            easer.EasingMode = EasingMode.EaseOut;
            DebugOutput.StartDebugger("Texplorer WPF");

            DetailsCloserAnim.EasingFunction = easer;
            DetailsCloserAnim.Duration = TimeSpan.FromSeconds(0.5);
            CloserButton.Content = ">>";

            DetailsOpacityAnim.Duration = TimeSpan.FromSeconds(0.5);
            DetailsOpacityAnim.EasingFunction = easer;

            PCCContext.EasingFunction = easer;
            PCCContext.Duration = TimeSpan.FromSeconds(0.2);

            MainListContext.EasingFunction = easer;
            MainListContext.Duration = TimeSpan.FromSeconds(0.2);
            

            ChangeMainContext(false);
            ChangePCContext(false);

            vm = new TexplorerViewModel();
            vm.PropertyChanged += (sender, e) =>
            {
                if ((e.PropertyName == "DisplayGameTreeInfoPanel" && vm.DisplayGameTreeInfoPanel == true) || (e.PropertyName == "NeedsFTCS" && vm.NeedsFTCS))
                {
                    Dispatcher.Invoke(() => ShowGameTreeInfoPanel());
                    vm.DisplayGameTreeInfoPanel = false;
                }

                if (e.PropertyName == "DisplayGameInfo" && vm.DisplayGameInfo != 0)
                {
                    int game = vm.DisplayGameInfo;
                    Helpers.Methods.ShowPathInfo(vm, "Mass Effect " + game, game);
                }
            };

            if (vm.NeedsFTCS)
                ShowGameTreeInfoPanel();
            else
                ChangeTreePanelState(false);
            DataContext = vm;
        }


        public void ChangeMainContext(bool show)
        {
            if (show)
            {
                MainListContext.To = new Thickness(0,0,0,32);
            }
            else
            {
                MainListContext.To = new Thickness(0, 0, 0, 2);
            }

            MainListBox.BeginAnimation(ListBox.MarginProperty, MainListContext);
        }

        public void AddToTree(string path, int exportID)
        {
            throw new NotImplementedException();
        }

        private void CloserButton_Click(object sender, RoutedEventArgs e)
        {
            if (detailsState)
            {
                DetailsCloserAnim.To = new GridLength(40, GridUnitType.Pixel);
                DetailsCloserAnim.From = new GridLength(300, GridUnitType.Pixel);
                DetailsOpacityAnim.To = 1;
                CloserButton.Content = "<<";
            }
            else
            {
                CloserButton.Content = ">>";
                DetailsOpacityAnim.To = 0;
                DetailsCloserAnim.From = new GridLength(40, GridUnitType.Pixel);
                DetailsCloserAnim.To = new GridLength(300, GridUnitType.Pixel);
            }

            detailsState = !detailsState;
            ThirdColumn.BeginAnimation(ColumnDefinition.WidthProperty, DetailsCloserAnim);
        }

        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChangeMainContext(true);
        }

        private void MainListView_GotFocus(object sender, RoutedEventArgs e)
        {
            ChangeMainContext(true);
        }

        private void MainListView_LostFocus(object sender, RoutedEventArgs e)
        {
            ChangeMainContext(false);
        }

        private void ChangePCContext(bool show)
        {
            if (show)
            {
                PCCContext.To = new Thickness(0, 28, 0, 28);
                PCCContext.From = new Thickness(0, 28, 0, 2);
            }
            else
            {
                PCCContext.To = new Thickness(0, 28, 0, 2);
                PCCContext.From = new Thickness(0, 28, 0, 28);
            }

            PCCDocker.BeginAnimation(DockPanel.MarginProperty, PCCContext);
        }

        private void PCCListBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ChangePCContext(true);
        }

        private void PCCListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ChangePCContext(false);
        }

        private void MainListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            vm.GetImagePreview();
            e.Handled = true;
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ((Image)sender).Visibility = System.Windows.Visibility.Collapsed;
        }

        private void ShowGameTreeInfoPanel()
        {
            vm.SetupGameFiles();
            ChangeTreePanelState(true);
        }

        private void ChangeTreePanelState(bool show)
        {
            var StateManager = VisualStateManager.GetVisualStateGroups(TreeGrid);
            var CurrentStateGroup = StateManager[0] as VisualStateGroup;
            VisualStateManager.GoToElementState(TreeGrid, ((VisualState) CurrentStateGroup.States[!show ? 1 : 0]).Name, true);

        }

        private void ImportTreeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select Tree to import";
            ofd.Filter = "METrees|*.bin";

            if (ofd.ShowDialog() == true)
            {
                string err = null;
                if ((err = vm.ImportTree(ofd.FileName)) != null)
                    MessageBox.Show("Error importing Tree!" + Environment.NewLine + err);
            }
        }

        private void ExportTreeButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Select destination for Tree";
            sfd.Filter = "METrees|*.bin";
            sfd.FileName = "ME" + vm.GameVersion + "tree.bin";

            if (sfd.ShowDialog() == true)
            {
                string err = null;
                if ((err = vm.ExportTree(sfd.FileName)) != null)
                    MessageBox.Show("Failed to export tree!" + Environment.NewLine + err);
            }
        }

        private void RemoveCurrentTreeButton_Click(object sender, RoutedEventArgs e)
        {
            vm.RemoveCurrentTree();
        }

        private void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            // KFreon: Delete existing tree
            if (!vm.NewUnscannedFiles && vm.CurrentTree.Exists)
                vm.RemoveCurrentTree();
                

            ChangeTreePanelState(false);
            vm.BeginScan();
        }

        private void TreePanelClosingButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ChangeTreePanelState(false);
        }

        private void ChangeTextureButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select new texture.";
            ofd.Filter = "DirectDraw Texture|*.dds";

            if (ofd.ShowDialog() == true)
                vm.ChangeTexture(ofd.FileName);
        }

        private void SearchResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.SelectSearchResults(SearchResultsBox.SelectedItem as KFreonLibME.Textures.TreeTexInfo);

            if (SearchResultsBox.SelectedItem != null)
                SelectTex(((KFreonLibME.Textures.TreeTexInfo)SearchResultsBox.SelectedItem).Parent);
        }

        private void SelectTex(HierarchicalTreeTexes tex)
        {
            if (tex.Parent == null)
            {
                TreeViewItem treer = MainTreeView.ItemContainerGenerator.ContainerFromItem(tex) as TreeViewItem;
                if (treer != null)
                    treer.BringIntoView();
            }
            else
                SelectTex(tex.Parent);
        }

        private void SavePCCListButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Select destination";
            sfd.Filter = "Text File|*.txt";
            if (sfd.ShowDialog() == true)
                vm.SavePCCList(sfd.FileName, vm.CurrentTree.Textures);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vm.SaveProperties();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine();   // KFreon: Not used. For debugging only
        }

        private async void CompareWCurrent_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "me" + vm.GameVersion + "tree.bin|me" + vm.GameVersion + "tree.bin";
            ofd.FileName = "me" + vm.GameVersion + "tree.bin";
            ofd.Title = "Select tree to compare current against";

            if (ofd.ShowDialog() == true)
            {
                ChangeTreePanelState(false);
                if (!await vm.CompareTrees(ofd.FileName))
                {
                    ShowGameTreeInfoPanel();
                }
                ShowGameTreeInfoPanel();

            }
        }

        private async void CompareBetweenTrees_Click(object sender, RoutedEventArgs e)
        {
            // KFreon: First tree
            string firstTree = null;
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select first tree";
            ofd.Filter = "ME Trees|*.bin";

            if (ofd.ShowDialog() == true)
                firstTree = ofd.FileName;
            else
                return;


            // KFreon: Second tree
            ofd = new OpenFileDialog();
            ofd.Filter = "ME Trees|*.bin";
            ofd.Title = "Select second tree";

            if (ofd.ShowDialog() == true)
            {
                ChangeTreePanelState(false);
                if (await vm.CompareTrees(firstTree, ofd.FileName))
                {
                    ShowGameTreeInfoPanel();

                }
                ShowGameTreeInfoPanel();

            }
            else
                return;
        }
    }
}
