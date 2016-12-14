using LiveCharts.Wpf;
using PerfmonClient.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace PerfmonClient
{
    static class CustomCommands
    {
        public static readonly RoutedCommand Quit = new RoutedCommand();
    }
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<Tab> Tabs { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Tabs = new ObservableCollection<Tab>();
            Tab tab = new Tab("Default", 2, 2);
            Tabs.Add(tab);
            tabControl.SelectedItem = tab;
        }

        private void newTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewTabDialog dialog = new NewTabDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                string name = dialog.TabName;
                int rows = dialog.Rows;
                int cols = dialog.Columns;

                Tab tab = new Tab(name, rows, cols);
                Tabs.Add(tab);
                tabControl.SelectedItem = tab;
            }
        }

        private void quitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = (TreeViewItem) treeView.SelectedItem;

            if (item != null)
            {
                item.IsSelected = false;
            }
        }

        private Point dragStart;

        private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStart = e.GetPosition(null);
        }

        private void treeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var item = (TreeViewItem) treeView.SelectedItem;

            if (item != null && e.LeftButton == MouseButtonState.Pressed)
            {
                Vector offset = e.GetPosition(null) - dragStart;

                if (Math.Abs(offset.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(offset.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    DataObject data = new DataObject(typeof(TreeViewItem), item);
                    DragDrop.DoDragDrop(treeView, data, DragDropEffects.Move);
                }
            }
        }

        private void CartesianChart_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(TreeViewItem)))
            {
                var item = (TreeViewItem) e.Data.GetData(typeof(TreeViewItem));
                var chart = (CartesianChart) sender;
                Console.WriteLine("Received: {0}", item.Header);
            }
        }
    }
}
