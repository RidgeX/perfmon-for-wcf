using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using PerfmonClient.Model;
using PerfmonClient.UI;
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
using System.Windows.Threading;

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

        public DispatcherTimer Timer { get; set; }
        public Random R { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)
                .Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            Tabs = new ObservableCollection<Tab>();
            Tab tab = new Tab("Default", 2, 2);
            Tabs.Add(tab);
            tabControl.SelectedItem = tab;

            Timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            Timer.Tick += Timer_Tick;
            R = new Random();

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
                            Value = R.NextDouble()
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

        private DragAdorner dragAdorner;
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
                    Window mainWindow = Application.Current.MainWindow;
                    mainWindow.AllowDrop = true;

                    var template = new DataTemplate(typeof(TreeViewItem));
                    var textBlock = new FrameworkElementFactory(typeof(TextBlock));
                    textBlock.SetBinding(TextBlock.TextProperty, new Binding("Header"));
                    template.VisualTree = textBlock;
                    var adornedElement = (UIElement) mainWindow.Content;
                    var adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
                    dragAdorner = new DragAdorner(item, template, adornedElement, adornerLayer);

                    mainWindow.PreviewDragOver += MainWindow_PreviewDragOver;

                    DataObject data = new DataObject(typeof(TreeViewItem), item);
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
            if (e.Data.GetDataPresent(typeof(TreeViewItem)))
            {
                var item = (TreeViewItem) e.Data.GetData(typeof(TreeViewItem));
                var chart = (CartesianChart) sender;

                LineSeries series = new LineSeries()
                {
                    PointGeometrySize = 9,
                    StrokeThickness = 2,
                    Title = (string) item.Header,
                    Values = new ChartValues<MeasureModel>()
                };
                chart.Series.Add(series);
            }
        }

        private void CartesianChart_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chart = (CartesianChart) sender;

            if (chart.Series.Any())
            {
                chart.Series.Remove(chart.Series.Last());
            }
        }
    }
}
