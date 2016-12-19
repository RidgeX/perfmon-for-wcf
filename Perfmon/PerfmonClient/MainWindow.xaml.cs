using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Microsoft.Win32;
using PerfmonClient.Model;
using PerfmonClient.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    static class CustomCommands
    {
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
        public ObservableCollection<CategoryItem> CategoryItems { get; set; }
        public Dictionary<Series, CounterItem> CounterSource { get; set; }
        public ObservableCollection<Tab> Tabs { get; set; }
        public DispatcherTimer Timer { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)
                .Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            CategoryItems = new ObservableCollection<CategoryItem>();
            CategoryItems.Add(MakeCategoryItem("ServiceModelEndpoint 4.0.0.0"));
            CategoryItems.Add(MakeCategoryItem("ServiceModelOperation 4.0.0.0"));
            CategoryItems.Add(MakeCategoryItem("ServiceModelService 4.0.0.0"));
            CategoryItems.Add(MakeCategoryItem("Test category"));

            CounterSource = new Dictionary<Series, CounterItem>();

            Tabs = new ObservableCollection<Tab>();
            Tab tab = new Tab("Default", 2, 2);
            Tabs.Add(tab);
            tabControl.SelectedItem = tab;

            Timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;

            foreach (Tab tab in Tabs)
            {
                foreach (ChartItem chartItem in tab.ChartItems)
                {
                    foreach (Series series in chartItem.SeriesCollection)
                    {
                        IChartValues values = series.Values;

                        values.Add(new MeasureModel()
                        {
                            DateTime = now,
                            Value = CounterSource[series].Counter.NextValue()
                        });
                        chartItem.SetAxisLimits(now);

                        if (values.Count > 30) values.RemoveAt(0);
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

        private void treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = (Item) treeView.SelectedItem;

            if (item != null)
            {
                item.IsSelected = false;
            }
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

            if (item != null && item is CounterItem && e.LeftButton == MouseButtonState.Pressed)
            {
                Vector offset = e.GetPosition(null) - dragStart;

                if (Math.Abs(offset.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(offset.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    Window mainWindow = Application.Current.MainWindow;
                    mainWindow.AllowDrop = true;

                    var template = new DataTemplate(typeof(CounterItem));
                    var textBlock = new FrameworkElementFactory(typeof(TextBlock));
                    textBlock.SetBinding(TextBlock.TextProperty, new Binding("Name"));
                    template.VisualTree = textBlock;
                    var adornedElement = (UIElement) mainWindow.Content;
                    var adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
                    dragAdorner = new DragAdorner(item, template, adornedElement, adornerLayer);

                    mainWindow.PreviewDragOver += MainWindow_PreviewDragOver;

                    DataObject data = new DataObject(typeof(CounterItem), item);
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
            if (e.Data.GetDataPresent(typeof(CounterItem)))
            {
                var item = (CounterItem) e.Data.GetData(typeof(CounterItem));
                var chart = (CartesianChart) sender;

                LineSeries series = new LineSeries()
                {
                    PointGeometrySize = 9,
                    StrokeThickness = 2,
                    Title = item.Name,
                    Values = new ChartValues<MeasureModel>()
                };
                chart.Series.Add(series);
                CounterSource.Add(series, item);
            }
        }

        private void editChartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var chart = (CartesianChart) ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            EditChartDialog dialog = new EditChartDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                // TODO
            }
        }

        private void removeSeriesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var chart = (CartesianChart) ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (chart.Series.Any())
            {
                var series = (Series) chart.Series.Last();
                CounterSource.Remove(series);
                chart.Series.Remove(series);
            }
        }

        private CategoryItem MakeCategoryItem(string categoryName)
        {
            var instanceItems = new ObservableCollection<InstanceItem>();
            var category = PerformanceCounterCategory.GetCategories().First(c => c.CategoryName == categoryName);

            string[] instanceNames = category.GetInstanceNames();
            Array.Sort(instanceNames);

            if (instanceNames.Any())
            {
                foreach (string instanceName in instanceNames)
                {
                    if (category.InstanceExists(instanceName))
                    {
                        instanceItems.Add(MakeInstanceItem(instanceName, category.GetCounters(instanceName)));
                    }
                }
            }
            else
            {
                instanceItems.Add(MakeInstanceItem("*", category.GetCounters()));
            }

            return new CategoryItem(categoryName, instanceItems);
        }

        private InstanceItem MakeInstanceItem(string instanceName, PerformanceCounter[] counters)
        {
            var counterItems = new ObservableCollection<CounterItem>();

            foreach (PerformanceCounter counter in counters)
            {
                counterItems.Add(MakeCounterItem(counter.CounterName, counter));
            }

            return new InstanceItem(instanceName, counterItems);
        }

        private CounterItem MakeCounterItem(string counterName, PerformanceCounter counter)
        {
            CounterItem counterItem = new CounterItem(counterName, counter);
            return counterItem;
        }

        public CounterItem FindCounterItem(string categoryName, string instanceName, string counterName)
        {
            CategoryItem categoryItem = CategoryItems.FirstOrDefault(item => item.Name == categoryName);
            if (categoryItem == null) return null;
            InstanceItem instanceItem = categoryItem.InstanceItems.FirstOrDefault(item => item.Name == instanceName);
            if (instanceItem == null) return null;
            CounterItem counterItem = instanceItem.CounterItems.FirstOrDefault(item => item.Name == counterName);
            return counterItem;
        }
    }
}
