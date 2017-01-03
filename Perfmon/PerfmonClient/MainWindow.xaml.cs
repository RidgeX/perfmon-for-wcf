using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Microsoft.Win32;
using PerfmonClient.Model;
using PerfmonClient.UI;
using PerfmonServiceLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.ServiceModel;
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
        public static readonly RoutedCommand NewTab = new RoutedCommand();
        public static readonly RoutedCommand SaveTab = new RoutedCommand();
        public static readonly RoutedCommand LoadTab = new RoutedCommand();
        public static readonly RoutedCommand CloseTab = new RoutedCommand();
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IPerfmonCallback
    {
        public IPerfmonService Service { get; set; }
        public ObservableCollection<CategoryItem> CategoryItems { get; set; }
        public Dictionary<InstanceItem, List<Series>> CounterListeners { get; set; }
        public ObservableCollection<Tab> Tabs { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)
                .Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            NetTcpBinding binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.Security.Mode = SecurityMode.None;
            string address = "net.tcp://localhost:8080/Perfmon/";
            DuplexChannelFactory<IPerfmonService> factory = new DuplexChannelFactory<IPerfmonService>(this, binding, address);
            Service = factory.CreateChannel();
            CategoryList categories = Service.List();
            categories.Sort((a, b) => a.Name.CompareTo(b.Name));

            CategoryItems = new ObservableCollection<CategoryItem>();

            foreach (Category category in categories)
            {
                CategoryItem categoryItem = new CategoryItem(category.Name);

                foreach (Counter counter in category.Counters)
                {
                    CounterItem counterItem = new CounterItem(counter.Name, categoryItem);

                    counterItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == "IsChecked")
                        {
                            CounterItem ci = (CounterItem) s;

                            if (ci.IsChecked == true)
                            {
                                Service.Subscribe(category.Name, counter.Name);
                                ci.IsExpanded = true;
                            }
                            else
                            {
                                Service.Unsubscribe(category.Name, counter.Name);
                                ci.IsExpanded = false;
                                ci.InstanceItems.Clear();
                            }
                        }
                    };

                    categoryItem.CounterItems.Add(counterItem);
                }

                CategoryItems.Add(categoryItem);
            }

            CounterListeners = new Dictionary<InstanceItem, List<Series>>();

            Tabs = new ObservableCollection<Tab>();
            Tab tab = new Tab("Default", 2, 2);
            Tabs.Add(tab);
            tabControl.SelectedItem = tab;
        }

        public void OnNext(EventData e)
        {
            Category category = e.Category;
            DateTime timestamp = e.Timestamp;

            CategoryItem categoryItem = CategoryItems.FirstOrDefault(item => item.Name == category.Name);
            if (categoryItem == null) return;

            foreach (Counter counter in category.Counters)
            {
                CounterItem counterItem = categoryItem.CounterItems.FirstOrDefault(item => item.Name == counter.Name);
                if (counterItem == null) continue;

                foreach (Instance instance in counter.Instances)
                {
                    InstanceItem instanceItem = counterItem.InstanceItems.FirstOrDefault(item => item.Name == instance.Name);

                    if (instanceItem == null)
                    {
                        instanceItem = new InstanceItem(instance.Name, counterItem);
                        counterItem.InstanceItems.Add(instanceItem);
                    }

                    List<Series> listeners;
                    if (CounterListeners.TryGetValue(instanceItem, out listeners))
                    {
                        foreach (Series series in listeners)
                        {
                            series.Values.Add(new MeasureModel(timestamp, instance.Value));

                            if (series.DataContext != BindingOperations.DisconnectedSource)
                            {
                                var chartItem = (ChartItem) series.DataContext;

                                if (chartItem != null)
                                {
                                    chartItem.SetAxisLimits(timestamp);
                                }
                            }

                            if (series.Values.Count > 30)
                            {
                                series.Values.RemoveAt(0);
                            }
                        }
                    }
                }
            }
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

        private void editTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var tab = (Tab) tabControl.SelectedItem;

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
            var tab = (Tab) tabControl.SelectedItem;
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

        private void quitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            ((IClientChannel) Service).Close();
        }

        private void treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = (Item) treeView.SelectedItem;

            if (item != null)
            {
                item.IsSelected = false;
            }
        }

        private void TreeViewItem_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private DragAdorner dragAdorner;
        private Point dragStart;

        private void TreeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            dragStart = e.GetPosition(null);
        }

        private void treeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            var item = (Item) treeView.SelectedItem;

            if (item != null && item is InstanceItem && e.LeftButton == MouseButtonState.Pressed)
            {
                Vector offset = e.GetPosition(null) - dragStart;

                if (Math.Abs(offset.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(offset.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    Window mainWindow = Application.Current.MainWindow;
                    mainWindow.AllowDrop = true;

                    var template = new DataTemplate(typeof(InstanceItem));
                    var textBlock = new FrameworkElementFactory(typeof(TextBlock));
                    textBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
                    template.VisualTree = textBlock;
                    var adornedElement = (UIElement) mainWindow.Content;
                    var adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
                    dragAdorner = new DragAdorner(item, template, adornedElement, adornerLayer);

                    mainWindow.PreviewDragOver += MainWindow_PreviewDragOver;

                    DataObject data = new DataObject(typeof(InstanceItem), item);
                    DragDrop.DoDragDrop(treeView, data, DragDropEffects.Move);

                    mainWindow.PreviewDragOver -= MainWindow_PreviewDragOver;
                    dragAdorner.Detach();
                    mainWindow.AllowDrop = false;
                }
            }
        }

        private void MainWindow_PreviewDragOver(object sender, DragEventArgs e)
        {
            Point point = e.GetPosition(this);
            dragAdorner.SetPosition(point.X, point.Y);
        }

        private void CartesianChart_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(InstanceItem)))
            {
                var instanceItem = (InstanceItem) e.Data.GetData(typeof(InstanceItem));
                var chart = (CartesianChart) sender;

                LineSeries series = new LineSeries()
                {
                    PointGeometrySize = 9,
                    StrokeThickness = 2,
                    Title = instanceItem.Name,
                    Values = new ChartValues<MeasureModel>()
                };

                chart.Series.Add(series);
                AddCounterListener(instanceItem, series);
            }
        }

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

        public void AddCounterListener(InstanceItem instanceItem, Series series)
        {
            List<Series> listeners;
            if (!CounterListeners.TryGetValue(instanceItem, out listeners))
            {
                listeners = new List<Series>();
                CounterListeners.Add(instanceItem, listeners);
            }
            listeners.Add(series);
        }

        public void RemoveCounterListener(Series series)
        {
            foreach (var kvp in CounterListeners.ToList())
            {
                InstanceItem instanceItem = kvp.Key;
                List<Series> listeners = kvp.Value;

                listeners.Remove(series);

                if (!listeners.Any())
                {
                    CounterListeners.Remove(instanceItem);
                }
            }
        }

        public InstanceItem FindInstanceItem(string categoryName, string counterName, string instanceName)
        {
            CategoryItem categoryItem = CategoryItems.FirstOrDefault(item => item.Name == categoryName);
            if (categoryItem == null) return null;
            CounterItem counterItem = categoryItem.CounterItems.FirstOrDefault(item => item.Name == counterName);
            if (counterItem == null) return null;
            InstanceItem instanceItem = counterItem.InstanceItems.FirstOrDefault(item => item.Name == instanceName);
            return instanceItem;
        }
    }
}
