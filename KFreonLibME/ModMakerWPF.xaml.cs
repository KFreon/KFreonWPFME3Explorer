using KFreonLibGeneral.Debugging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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
using KFreonLibME.ViewModels;
using System.ComponentModel;
using System.Windows.Controls.Primitives;

namespace KFreonLibME
{
    /// <summary>
    /// Interaction logic for NewModMaker.xaml
    /// </summary>
    public partial class NewModMaker : Window
    {
        ModMakerViewModel vm;
        GridLengthAnimation ThirdAnimator = new GridLengthAnimation();
        GridLengthAnimation SecondAnimator = new GridLengthAnimation();

        ThicknessAnimation PCCContext = new ThicknessAnimation();
        Thickness PCCOpen = new Thickness(5, 180, 2, 30);
        Thickness PCCClosed = new Thickness(5, 180, 2, 5);

        ThicknessAnimation MainContext = new ThicknessAnimation();
        Thickness MainContextOpen = new Thickness(0, 0, 0, 30);
        Thickness MainContextClosed = new Thickness(0, 0, 0, 5);

        // KFreon: Visual state: true = fully open, null = standard view, false = collapsed.
        bool? state = false;
        int buttonSize = 80;

        #region size properties
        ColumnDefinition FirstColumn
        {
            get
            {
                return MainGrid.ColumnDefinitions[0];
            }
        }

        ColumnDefinition SecondColumn
        {
            get
            {
                return MainGrid.ColumnDefinitions[2];
            }
        }

        ColumnDefinition ThirdColumn
        {
            get
            {
                return MainGrid.ColumnDefinitions[4];
            }
        }

        RowDefinition FirstRow
        {
            get
            {
                return MainGrid.RowDefinitions[0];
            }
        }

        GridLength SecondColumnOpenLength { get; set; }
        GridLength ThirdColumnOpenLength { get; set; }
        #endregion

        public NewModMaker()
        {
            InitializeComponent();
            KFreonLibME.Misc.Methods.UpgradeProperties();


            // KFreon: Set number of threads if necessary
            if (KFreonLibME.Misc.Methods.GetNumThreads() == 0)
                KFreonLibME.Misc.Methods.SetNumThreads(false);

            // KFreon: Start debugger
            DebugOutput.StartDebugger("ModMaker WPF");

            // KFreon: Setup PCC pane context menu animation
            CubicEase easingFunction = new CubicEase();
            easingFunction.EasingMode = EasingMode.EaseOut;

            // KFreon: Setup script pane animations
            ThirdAnimator.Duration = TimeSpan.FromSeconds(0.5);
            ThirdAnimator.EasingFunction = easingFunction;
            ThirdAnimator.FillBehavior = FillBehavior.Stop;
            SecondAnimator.EasingFunction = easingFunction;
            SecondAnimator.Duration = TimeSpan.FromSeconds(0.5);
            SecondAnimator.FillBehavior = FillBehavior.Stop;

            // KFreon: Set column size properties
            SecondColumnOpenLength = SecondColumn.Width;
            ThirdColumnOpenLength = ThirdColumn.Width;


            // KFreon: Setup some animations
            CubicEase easer = new CubicEase();
            easer.EasingMode = EasingMode.EaseOut;
            TimeSpan dur = TimeSpan.FromSeconds(0.2);

            PCCContext.Duration = dur;
            PCCContext.EasingFunction = easer;

            MainContext.Duration = dur;
            MainContext.EasingFunction = easer;


            // KFreon: Set default view
            TotalCollapse();
            ChangeContextMenu(false);



            // KFreon: Bind viewmodel to GUI
            vm = new ModMakerViewModel();

            vm.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == "DisplayGameInfo")
                        Helpers.Methods.ShowPathInfo(vm, "Mass Effect " + vm.DisplayGameInfo, vm.DisplayGameInfo);
                };

            DataContext = vm;   
        }

        private void TotalCollapse()
        {
            ChangeColumns(0);

            state = false;

            ChangePCCContext(false);
        }


        private void ChangeColumns(int state)
        {
            GridLength zero = new GridLength(0, GridUnitType.Pixel);
            switch(state)
            {
                case 0:  // KFreon: Close everything
                    SecondAnimator.From = SecondColumn.Width;
                    SecondAnimator.To = zero;
                    ThirdAnimator.From = ThirdColumn.Width;
                    ThirdAnimator.To = zero;
                    break;
                case 1:
                    double diff = ThirdAnimator.From.Value - ThirdAnimator.To.Value;
                    GridLength len = new GridLength(SecondColumn.Width.Value + diff, GridUnitType.Star);
                    SecondAnimator.To = len;
                    SecondAnimator.From = SecondColumn.Width;
                    break;
                case 2:
                    SecondAnimator.From = SecondColumn.Width;
                    break;
                case 3:
                    ThirdAnimator.From = zero;
                    ThirdAnimator.To = ThirdColumnOpenLength;
                    SecondAnimator.From = zero;
                    SecondAnimator.To = SecondColumnOpenLength;
                    break;
            }


            SecondColumn.BeginAdjustableAnimation(ColumnDefinition.WidthProperty, SecondAnimator);
            ThirdColumn.BeginAdjustableAnimation(ColumnDefinition.WidthProperty, ThirdAnimator);
        }


        private void ChangePCCContext(bool opening)
        {
            if (opening)
                PCCContext.To = PCCOpen;
            else
                PCCContext.To = PCCClosed;

            PCCListBox.BeginAnimation(ListBox.MarginProperty, PCCContext);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select .mod to load.";
            ofd.Filter = "ME3Modmaker Mods|*.mod";
            ofd.Multiselect = true;

            // KFreon: Load mods
            if (ofd.ShowDialog() == true)
                vm.LoadMods(ofd.FileNames);
        }

        private void RunAllButton_Click(object sender, RoutedEventArgs e)
        {
            vm.RunJobs(vm.LoadedMods.ToArray());
        }

        private void ContractorButton_Click(object sender, RoutedEventArgs e)
        {
            // KFreon: Closes and opens script pane.

            if (state == false)  // KFreon: Collapsed to open
            {
                ThirdAnimator.From = new GridLength(buttonSize, GridUnitType.Pixel);
                ThirdAnimator.To = ThirdColumnOpenLength;
                ContractorButton.Content = ">>";
                state = null;
            }
            else  // KFreon: Open to collapsed
            {
                // KFreon: Save column positions if necessary
                if (SecondColumn.Width.Value > 10)
                {
                    SecondColumnOpenLength = SecondColumn.Width;
                    ThirdColumnOpenLength = ThirdColumn.Width;
                }
                

                ThirdAnimator.From = ThirdColumn.Width;
                ThirdAnimator.To = new GridLength(buttonSize, GridUnitType.Pixel);
                ContractorButton.Content = "<<";
                ExpanderButton.Content = "<<<";
                state = false;
            }

            ChangeColumns(1);
        }

        private void ExpanderButton_Click(object sender, RoutedEventArgs e)
        {
            // KFreon: Expands and contracts script pane
            if (state == null)  // KFreon: Standard view to fully open.
            {
                ThirdColumnOpenLength = ThirdColumn.Width;
                SecondColumnOpenLength = SecondColumn.Width;

                ThirdAnimator.From = ThirdColumn.Width;
                ThirdAnimator.To = new GridLength(ThirdColumnOpenLength.Value + SecondColumnOpenLength.Value, GridUnitType.Star);

                SecondAnimator.To = new GridLength(0, GridUnitType.Star);
                ExpanderButton.Content = ">>>";
                ContractorButton.Content = ">>";
                state = true;
            }
            else 
            {
                if (state == true)  // KFreon: Fully open to standard view.
                {
                    state = null;
                    ExpanderButton.Content = "<<<";
                    ThirdAnimator.To = ThirdColumnOpenLength;
                    SecondAnimator.To = SecondColumnOpenLength;
                }
                else  // KFreon: Closed to fully open.
                {
                    state = true;
                    ExpanderButton.Content = ">>>";
                    ThirdAnimator.To = new GridLength(ThirdColumnOpenLength.Value + SecondColumnOpenLength.Value, GridUnitType.Star);
                    SecondAnimator.To = new GridLength(0, GridUnitType.Star);
                }

                ContractorButton.Content = ">>";
                ThirdAnimator.From = ThirdColumn.Width;
            }

            ChangeColumns(2);
        }

        private void MainListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(MainListBox, (e.OriginalSource as DependencyObject)) as ListBoxItem;
            if (item == null)
                TotalCollapse();
            else if (state == false)
            {
                ChangeColumns(3);
                state = null;
            }
        }

        private void ClearJobsButton_Click(object sender, RoutedEventArgs e)
        {
            vm.ClearJobs();
            //TotalCollapse();
            GC.Collect();
        }

        private void MainListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!vm.Busy)
            {
                // KFreon: Tool is NOT doing stuff

                int index = MainListBox.SelectedIndex;
                if (index >= 0)
                {
                    // KFreon: Delete job
                    if (e.Key == Key.Delete)
                        vm.DeleteJobs();


                    // KFreon: Move job down
                    if (e.Key == Key.PageDown)
                        if (vm.MoveJobDown(index))
                            MainListBox.SelectedIndex = index + 1;

                    if (e.Key == Key.PageUp)
                        if (vm.MoveJobUp(index))
                            MainListBox.SelectedIndex = index - 1;

                    e.Handled = true;
                }
            }
        }

        private void MainListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // KFreon: If no selection, collapse panes
            int index = MainListBox.SelectedIndex;
            int numSelected = MainListBox.SelectedItems.Count;

            if (index == -1 || numSelected > 1)
                TotalCollapse();

            ChangeContextMenu(numSelected > 1);
            vm.MultiSelected = numSelected > 1;
        }

        private void ChangeContextMenu(bool show)
        {
            if (show)
            {
                MainContext.To = MainContextOpen;
                //MainContext.From = MainContextClosed;
            }
            else
            {
                MainContext.To = MainContextClosed;
                //MainContext.From = MainContextOpen;
            }

            MainListBox.BeginAnimation(ListBox.MarginProperty, MainContext);
        }        

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DebugOutput.PrintLn("-----Execution of ModMaker closing...-----");
            vm.SaveProperties();
            vm.ClearJobs();
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            bool enabled = true;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];

                if (files != null)
                {
                    foreach (string file in files)
                        if (System.IO.Path.GetExtension(file).ToUpperInvariant() != ".MOD")
                        {
                            enabled = false;
                            break;
                        }
                }
                else
                    enabled = false;
            }
            else
                enabled = false;


            if (enabled)
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;

        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            vm.LoadMods(files);
        }

        private void PCCListBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ChangePCCContext(true);
        }

        private void PCCListBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ChangePCCContext(false);            
        }

        private void CancelThumbs_Click(object sender, RoutedEventArgs e)
        {
            DebugOutput.PrintLn("Cancelling thumbnail generation.");
            vm.ThumbsGenInit = false;
        }

        private void RunSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            vm.RunJobs();

        }

        private void SaveSelectedModButton_Click(object sender, RoutedEventArgs e)
        {
            vm.SaveJobsToMod();
        }

        private void ExtractSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            vm.ExtractJobsData();
        }

        private void UpdateSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            vm.UpdateJobs();
        }

        private void SavePCCList_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Select destination";
            sfd.Filter = "Text File|*.txt";
            if (sfd.ShowDialog() == true)
                vm.SavePCCList(sfd.FileName, vm.LoadedMods);
        }
    }
}
