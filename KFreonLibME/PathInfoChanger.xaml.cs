using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace KFreonLibME
{
    /// <summary>
    /// Interaction logic for PathInfoChanger.xaml
    /// </summary>
    public partial class PathInfoChanger : Window
    {
        public PathChangerViewModel vm;
        public PathInfoChanger()
        {
            vm = new PathChangerViewModel();
            DataContext = vm;
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // KFreon: Save all items
            vm.SaveAll();
            this.DialogResult = true;
            Close();
        }
    }

    public class PathChangerViewModel
    {
        public ObservableCollection<ViewItem> Items { get; set; }
        public PathChangerViewModel()
        {
            // KFreon: Get initial values for all properties -> maybe just set them in properties
            Items = new ObservableCollection<ViewItem>() { new ViewItem("Mass Effect 1", 1), new ViewItem("Mass Effect 2", 2), new ViewItem("Mass Effect 3", 3) };
        }

        internal void SaveAll()
        {
            foreach (ViewItem vi in Items)
                vi.Save();
        }
    }

    public class ViewItem : BasePathChangerViewModel
    {
        public ViewItem(string header, int game) : base(game)
        {
            TitleText = header;
        }
    }
}
