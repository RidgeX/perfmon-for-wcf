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
using System.Data.SQLite;
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

        public string BasePath { get; set; }
        public Dictionary<string, long> CounterIds { get; set; }
        public SQLiteConnection Database { get; set; }
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

            BasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "perfmon-for-wcf");
            if (!Directory.Exists(BasePath)) Directory.CreateDirectory(BasePath);

            CounterIds = new Dictionary<string, long>();
            InitDatabase();

            MachineItems = new ObservableCollection<MachineItem>();
            CounterListeners = new Dictionary<string, List<Series>>();
            Tabs = new ObservableCollection<Tab>();
            Connections = new List<Connection>();

            OpenDefaultTab();
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

                if (Connections.Any(c => c.Host == host && c.Port == port)) return;

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
            saveFileDialog.InitialDirectory = BasePath;
            saveFileDialog.FileName = tab.Name;
            saveFileDialog.Filter = "Tab settings (*.xml)|*.xml";
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
            openFileDialog.InitialDirectory = BasePath;
            openFileDialog.Filter = "Tab settings (*.xml)|*.xml";
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

        private void OpenDefaultTab()
        {
            Tab tab = null;
            string defaultTabFile = Path.Combine(BasePath, "Default.xml");

            if (File.Exists(defaultTabFile))
            {
                try
                {
                    var fs = new FileStream(defaultTabFile, FileMode.Open);

                    var serial = new XmlSerializer(typeof(Tab));
                    tab = (Tab) serial.Deserialize(fs);

                    fs.Close();
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null) ex = ex.InnerException;
                    MessageBox.Show(ex.Message, "Load Tab", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            if (tab == null)
            {
                tab = new Tab("Default", 2, 2);
            }

            Tabs.Add(tab);
            tabControl.SelectedItem = tab;
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
                    MessageBox.Show(string.Format("Are you sure you want to resize \"{0}\"?\nYou will lose any unsaved changes.", tab.Name), "Edit Tab",
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

            Database.Close();
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

        #region Export Data

        private void exportDataMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ExportDataDialog dialog = new ExportDataDialog(Database);
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                long unixTime = Convert.ToInt64((DateTime.Now - epoch).TotalSeconds);

                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.InitialDirectory = BasePath;
                saveFileDialog.FileName = string.Format("export_{0}.csv", unixTime);
                saveFileDialog.Filter = "Comma separated values (*.csv)|*.csv";
                saveFileDialog.Title = "Export Data";

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        var fs = (FileStream) saveFileDialog.OpenFile();
                        ExportCSVFromDatabase(dialog.SavedCounters, fs);
                        fs.Close();
                    }
                    catch (Exception ex)
                    {
                        while (ex.InnerException != null) ex = ex.InnerException;
                        MessageBox.Show(ex.Message, "Export Data", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
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

        #region Inspect Mode

        private void inspectMenuItem_Checked(object sender, RoutedEventArgs e)
        {
            var chart = (CartesianChart) ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
            var chartItem = (ChartItem) chart.DataContext;

            chartItem.DataTooltip = new DefaultTooltip();

            Axis axisX = chart.AxisX[0];
            axisX.SetRange(axisX.MinValue, axisX.MaxValue);
            chart.Zoom = ZoomingOptions.X;
        }

        private void inspectMenuItem_Unchecked(object sender, RoutedEventArgs e)
        {
            var chart = (CartesianChart) ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
            var chartItem = (ChartItem) chart.DataContext;

            chartItem.DataTooltip = null;

            Axis axisX = chart.AxisX[0];
            chart.Zoom = ZoomingOptions.None;
            axisX.SetBinding(Axis.MinValueProperty, new Binding("MinX"));
            axisX.SetBinding(Axis.MaxValueProperty, new Binding("MaxX"));
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
            Connection conn = Connections.FirstOrDefault(c => c.MachineItem == machineItem);

            if (conn != null)
            {
                conn.Refresh();
            }
        }

        private void disconnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var machineItem = (MachineItem) (sender as MenuItem).DataContext;
            Connection conn = Connections.FirstOrDefault(c => c.MachineItem == machineItem);

            if (conn != null)
            {
                conn.Close();
                Connections.Remove(conn);
            }
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
            long id = GetCounterId(path);
            UpdateDatabase(id, timestamp, value);

            MeasureModel newValue = new MeasureModel(timestamp, Math.Round(value, 2));

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

        #region Logging

        private long GetCounterId(string path)
        {
            long id;
            if (!CounterIds.TryGetValue(path, out id))
            {
                SQLiteCommand cmd = new SQLiteCommand("SELECT id FROM counter WHERE path = @path", Database);
                cmd.Parameters.AddWithValue("@path", path);
                object result = cmd.ExecuteScalar();

                if (result == null)
                {
                    cmd = new SQLiteCommand("INSERT INTO counter (path) VALUES (@path)", Database);
                    cmd.Parameters.AddWithValue("@path", path);
                    cmd.ExecuteNonQuery();
                    cmd = new SQLiteCommand("SELECT last_insert_rowid()", Database);
                    result = cmd.ExecuteScalar();
                }

                id = (long) result;
                CounterIds.Add(path, id);
            }

            return id;
        }

        private void InitDatabase()
        {
            string dataFolder = Path.Combine(BasePath, "data");
            if (!Directory.Exists(dataFolder)) Directory.CreateDirectory(dataFolder);
            string dataFile = Path.Combine(dataFolder, "Data.sqlite3");
            if (!File.Exists(dataFile)) SQLiteConnection.CreateFile(dataFile);

            Database = new SQLiteConnection(string.Format("Data Source={0};Version=3;", dataFile));
            Database.Open();

            SQLiteCommand cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS counter (id INTEGER, path TEXT, PRIMARY KEY(id))", Database);
            cmd.ExecuteNonQuery();
            cmd = new SQLiteCommand("CREATE TABLE IF NOT EXISTS sample (timestamp DATETIME, counter_id INTEGER, value REAL, PRIMARY KEY(timestamp, counter_id), FOREIGN KEY(counter_id) REFERENCES counter(id))", Database);
            cmd.ExecuteNonQuery();
        }

        private void UpdateDatabase(long id, DateTime timestamp, float value)
        {
            SQLiteCommand cmd = new SQLiteCommand("INSERT INTO sample VALUES (@timestamp, @counter_id, @value)", Database);
            cmd.Parameters.AddWithValue("@timestamp", timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            cmd.Parameters.AddWithValue("@counter_id", id);
            cmd.Parameters.AddWithValue("@value", value);
            cmd.ExecuteNonQuery();
        }

        private void ExportCSVFromDatabase(ObservableCollection<SavedCounterItem> savedCounters, FileStream fs)
        {
            var selectedCounters = savedCounters.Where(item => item.IsChecked == true).ToList();
            selectedCounters.Sort((a, b) => a.Id.CompareTo(b.Id));
            StreamWriter writer = new StreamWriter(fs);

            /*
             * SQL format (after table join)
             * -------------------------------------------
             * | timestamp               | id | value    |
             * |-------------------------|----|----------|
             * | 2017-01-10 16:49:51.250 | 1  | 13.70664 |
             * | 2017-01-10 16:49:51.250 | 2  | NULL     |
             * | 2017-01-10 16:49:51.500 | 1  | NULL     |
             * | 2017-01-10 16:49:51.500 | 2  | 4.292823 |
             * | 2017-01-10 16:49:52.250 | 1  | 23.58469 |
             * | 2017-01-10 16:49:52.250 | 2  | NULL     |
             * | 2017-01-10 16:49:52.500 | 1  | NULL     |
             * | 2017-01-10 16:49:52.500 | 2  | 1.751746 |
             * -------------------------------------------
             *
             * CSV format
             * "(PDH-CSV 4.0) (UTC)(0)","\\RWAP1443\Processor(0)\% Processor Time","\\RWAP1443\Processor(1)\% Processor Time"
             * "2017-01-10 16:49:51.250","13.70664","0"
             * "2017-01-10 16:49:51.500","13.70664","4.292823"
             * "2017-01-10 16:49:52.250","23.58469","4.292823"
             * "2017-01-10 16:49:52.500","23.58469","1.751746"
             *
             * BLG format
             * relog -f bin export.csv -o export.blg
             */
            SQLiteCommand cmd = new SQLiteCommand("CREATE TEMPORARY TABLE temp (id INTEGER)", Database);
            cmd.ExecuteNonQuery();

            writer.Write("\"(PDH-CSV 4.0) (UTC)(0)\"");
            SQLiteTransaction transaction = Database.BeginTransaction();
            foreach (SavedCounterItem item in selectedCounters)
            {
                cmd = new SQLiteCommand("INSERT INTO temp VALUES (@id)", Database);
                cmd.Parameters.AddWithValue("@id", item.Id);
                cmd.ExecuteNonQuery();
                writer.Write(",\"{0}\"", item.Path);
            }
            transaction.Commit();

            string lastTimestamp = string.Empty;
            double[] lastValue = new double[selectedCounters.Count];
            string sql = string.Join(
                Environment.NewLine,
                "SELECT strftime('%m/%d/%Y %H:%M:%f', s.timestamp), t.value FROM counter",
                "CROSS JOIN (SELECT DISTINCT timestamp FROM sample) AS s",
                "LEFT JOIN sample AS t ON s.timestamp = t.timestamp AND id = t.counter_id",
                "WHERE id IN temp",
                "ORDER BY s.timestamp, id"
            );
            cmd = new SQLiteCommand(sql, Database);
            SQLiteDataReader reader = cmd.ExecuteReader();

            int i = 0;
            while (reader.Read())
            {
                string timestamp = (string) reader[0];

                if (timestamp != lastTimestamp)
                {
                    writer.WriteLine();
                    writer.Write("\"{0}\"", timestamp);
                    lastTimestamp = timestamp;
                    i = 0;
                }

                double value = (reader[1] != DBNull.Value ? (double) reader[1] : lastValue[i]);
                writer.Write(",\"{0}\"", value);
                lastValue[i] = value;
                i++;
            }

            cmd = new SQLiteCommand("DROP TABLE temp", Database);
            cmd.ExecuteNonQuery();

            writer.WriteLine();
            writer.Flush();
        }

        #endregion
    }
}
