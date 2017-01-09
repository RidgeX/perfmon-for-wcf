using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Microsoft.Win32;
using PerfmonClient.Model;
using PerfmonClient.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace PerfmonClient
{
    public static class CustomCommands
    {
        public static readonly RoutedCommand ConnectTo = new RoutedCommand();
        public static readonly RoutedCommand NewTab = new RoutedCommand();
        public static readonly RoutedCommand SaveTab = new RoutedCommand();
        public static readonly RoutedCommand LoadTab = new RoutedCommand();
        public static readonly RoutedCommand CloseTab = new RoutedCommand();
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int maxPointsPerChart = 30;
        private const double pointSize = 9;
        private const double strokeThickness = 2;

        private DragAdorner dragAdorner;
        private Point dragStart;
        private GridLength savedWidth;

        public static readonly InstanceItem NoneItem = new InstanceItem("(none)", null);

        public ObservableCollection<MachineItem> MachineItems { get; set; }
        public Dictionary<string, List<Series>> CounterListeners { get; set; }
        public ObservableCollection<Tab> Tabs { get; set; }
        public List<Connection> Connections { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)
                .Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            MachineItems = new ObservableCollection<MachineItem>();
            CounterListeners = new Dictionary<string, List<Series>>();

            Tabs = new ObservableCollection<Tab>();
            Tab tab = new Tab("Default", 2, 2);
            Tabs.Add(tab);
            tabControl.SelectedItem = tab;

            Connections = new List<Connection>();

            /*
            Connection conn = new Connection("localhost", 8080);
            Connections.Add(conn);
            conn.Open();
            */
        }

        #region Connect To

        private void connectToMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConnectDialog dialog = new ConnectDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                string host = dialog.Host;
                int port = dialog.Port;

                Connection conn = new Connection(host, port);
                Connections.Add(conn);
                conn.Open();
            }
        }

        #endregion

        #region New Tab

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

        #endregion

        #region Save/Load Tab

        private void saveTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var tab = (Tab) tabControl.SelectedItem;
            if (tab == null) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = tab.Name;
            saveFileDialog.Filter = "Tab settings|*.xml";
            saveFileDialog.Title = "Save Tab";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    var fs = (FileStream) saveFileDialog.OpenFile();

                    var serial = new XmlSerializer(typeof(Tab));
                    serial.Serialize(fs, tab);

                    fs.Close();
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null) ex = ex.InnerException;
                    MessageBox.Show(ex.Message, "Save Tab", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void loadTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Tab settings|*.xml";
            openFileDialog.Title = "Load Tab";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var fs = (FileStream) openFileDialog.OpenFile();

                    var serial = new XmlSerializer(typeof(Tab));
                    var tab = (Tab) serial.Deserialize(fs);

                    Tabs.Add(tab);
                    tabControl.SelectedItem = tab;

                    fs.Close();
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null) ex = ex.InnerException;
                    MessageBox.Show(ex.Message, "Load Tab", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Edit/Close Tab

        private void editTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var tab = (Tab) (e.OriginalSource as MenuItem).DataContext;
            if (tab == null) return;

            NewTabDialog dialog = new NewTabDialog();
            dialog.Title = "Edit Tab";
            dialog.TabName = tab.Name;
            dialog.Rows = tab.Rows;
            dialog.Columns = tab.Columns;
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                tab.Name = dialog.TabName;

                if ((dialog.Rows != tab.Rows || dialog.Columns != tab.Columns) &&
                    MessageBox.Show(string.Format("Are you sure you want to resize \"{0}\"?", tab.Name), "Edit Tab",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    tab.Rows = dialog.Rows;
                    tab.Columns = dialog.Columns;
                    tab.Destroy();
                    tab.Initialize();
                }
            }
        }

        private void closeTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tab tab;

            if (e.OriginalSource is MenuItem && (e.OriginalSource as MenuItem).DataContext is Tab)
            {
                // Close the tab which this menu belongs to
                tab = (Tab) (e.OriginalSource as MenuItem).DataContext;
            }
            else
            {
                // Close the currently open tab (event called from File menu)
                tab = (Tab) tabControl.SelectedItem;
            }

            if (tab == null) return;

            if (MessageBox.Show(string.Format("Are you sure you want to close \"{0}\"?", tab.Name), "Close Tab",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Clear template before removal to avoid binding warnings from TabControl
                var tabItem = (TabItem) tabControl.ItemContainerGenerator.ContainerFromItem(tab);
                tabItem.Template = null;

                Tabs.Remove(tab);
                tab.Destroy();
            }
        }

        #endregion

        #region Quit

        private void quitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            foreach (Connection conn in Connections.ToList())
            {
                conn.Close();
                Connections.Remove(conn);
            }
        }

        #endregion

        #region Show/Hide Counter Browser

        private void showBrowserMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;

            gridSplitter.Visibility = Visibility.Visible;

            ColumnDefinition lastColumn = grid.ColumnDefinitions.Last();
            lastColumn.Width = savedWidth;

            treeView.Visibility = Visibility.Visible;
        }

        private void showBrowserMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            treeView.Visibility = Visibility.Collapsed;

            ColumnDefinition lastColumn = grid.ColumnDefinitions.Last();
            savedWidth = lastColumn.Width;
            lastColumn.Width = GridLength.Auto;

            gridSplitter.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Item Selection

        private void treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = (Item) treeView.SelectedItem;

            // Deselect item when clicking in blank area
            if (item != null)
            {
                item.IsSelected = false;
            }
        }

        private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Select items on right click
            var item = (Item) (sender as TreeViewItem).DataContext;
            item.IsSelected = true;
        }

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Ignore auto-scroll on item selection
            e.Handled = true;
        }

        #endregion

        #region Item Drag and Drop

        private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Save position of initial mouse down
            dragStart = e.GetPosition(null);
        }

        private void treeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var item = (Item) treeView.SelectedItem;

            if (item != null && item is InstanceItem && item != NoneItem && e.LeftButton == MouseButtonState.Pressed)
            {
                Vector offset = e.GetPosition(null) - dragStart;

                // Start drag if mouse was held for a minimum distance
                if (Math.Abs(offset.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(offset.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    // Allow dropping anywhere within the window
                    Window mainWindow = Application.Current.MainWindow;
                    mainWindow.AllowDrop = true;

                    // Create item ghost
                    var template = new DataTemplate(typeof(InstanceItem));
                    var textBlock = new FrameworkElementFactory(typeof(TextBlock));
                    textBlock.SetBinding(TextBlock.TextProperty, new Binding("DisplayName"));
                    template.VisualTree = textBlock;
                    var adornedElement = (UIElement) mainWindow.Content;
                    var adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
                    dragAdorner = new DragAdorner(item, template, adornedElement, adornerLayer);

                    mainWindow.PreviewDragOver += MainWindow_PreviewDragOver;

                    // Perform drag and drop
                    DataObject data = new DataObject(typeof(InstanceItem), item);
                    DragDrop.DoDragDrop(treeView, data, DragDropEffects.Move);

                    // Cleanup handlers
                    mainWindow.PreviewDragOver -= MainWindow_PreviewDragOver;
                    dragAdorner.Detach();
                    mainWindow.AllowDrop = false;
                }
            }
        }

        private void MainWindow_PreviewDragOver(object sender, DragEventArgs e)
        {
            // Move item ghost to current mouse position
            Point point = e.GetPosition(this);
            dragAdorner.SetPosition(point.X, point.Y);
        }

        private void CartesianChart_Drop(object sender, DragEventArgs e)
        {
            // Create chart series from dropped instance items
            if (e.Data.GetDataPresent(typeof(InstanceItem)))
            {
                var instanceItem = (InstanceItem) e.Data.GetData(typeof(InstanceItem));
                var chart = (CartesianChart) sender;

                Series series = CreateSeries(instanceItem.DisplayName);
                chart.Series.Add(series);
                AddCounterListener(instanceItem.Path, series);
            }
        }

        #endregion

        #region Edit/Remove Chart Series

        private void editChartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var chart = (CartesianChart) ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            EditChartDialog dialog = new EditChartDialog(chart);
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void removeSeriesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var chart = (CartesianChart) ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (chart.Series.Any())
            {
                var series = (Series) chart.Series.Last();

                RemoveCounterListener(series);
                chart.Series.Remove(series);
            }
        }

        #endregion

        #region Refresh/Disconnect Server

        private void refreshMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var machineItem = (MachineItem) (sender as MenuItem).DataContext;
            Connection conn = Connections.First(c => c.MachineItem == machineItem);
            conn.Refresh();
        }

        private void disconnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var machineItem = (MachineItem) (sender as MenuItem).DataContext;
            Connection conn = Connections.First(c => c.MachineItem == machineItem);
            conn.Close();
            Connections.Remove(conn);
        }

        #endregion

        #region Chart Helper Methods

        public void AddCounterListener(string path, Series series)
        {
            List<Series> listeners;
            if (!CounterListeners.TryGetValue(path, out listeners))
            {
                listeners = new List<Series>();
                CounterListeners.Add(path, listeners);
            }
            listeners.Add(series);
        }

        public void RemoveCounterListener(Series series)
        {
            foreach (var kvp in CounterListeners.ToList())
            {
                string path = kvp.Key;
                List<Series> listeners = kvp.Value;

                listeners.Remove(series);

                if (!listeners.Any())
                {
                    CounterListeners.Remove(path);
                }
            }
        }

        public static Series CreateSeries(string title)
        {
            return new LineSeries()
            {
                PointGeometrySize = pointSize,
                StrokeThickness = strokeThickness,
                Title = title,
                Values = new ChartValues<MeasureModel>()
            };
        }

        public string FindSeries(Series series)
        {
            return CounterListeners.Where(kvp => kvp.Value.Contains(series)).Select(kvp => kvp.Key).First();
        }

        public void UpdateSeries(string path, DateTime timestamp, float value)
        {
            MeasureModel newValue = new MeasureModel(timestamp, value);

            List<Series> listeners;
            if (CounterListeners.TryGetValue(path, out listeners))
            {
                foreach (Series series in listeners)
                {
                    IChartValues values = series.Values;
                    values.Add(newValue);

                    // Update axis limits if chart is visible
                    if (series.DataContext != BindingOperations.DisconnectedSource)
                    {
                        var chartItem = (ChartItem) series.DataContext;
                        chartItem?.SetAxisLimits(timestamp);
                    }

                    if (values.Count > maxPointsPerChart)
                    {
                        values.RemoveAt(0);
                    }
                }
            }
        }

        #endregion
    }
}
