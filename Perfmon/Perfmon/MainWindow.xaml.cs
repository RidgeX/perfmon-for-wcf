﻿using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace Perfmon
{
    public class CounterPath
    {
        public string CategoryName { get; set; }
        public string InstanceName { get; set; }
        public string CounterName { get; set; }

        public CounterPath() { }

        public CounterPath(PerformanceCounter counter)
        {
            CategoryName = counter.CategoryName;
            InstanceName = string.IsNullOrEmpty(counter.InstanceName) ? "*" : counter.InstanceName;
            CounterName = counter.CounterName;
        }
    }

    public class Settings
    {
        public List<CounterPath> CounterPaths { get; set; }

        public Settings()
        {
            CounterPaths = new List<CounterPath>();
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private double axisMax;
        private double axisMin;

        public SeriesCollection SeriesCollection { get; set; }
        public Func<double, string> DateTimeFormatter { get; set; }
        public double AxisStep { get; set; }
        public double AxisMax
        {
            get { return axisMax; }
            set
            {
                axisMax = value;
                OnPropertyChanged("AxisMax");
            }
        }
        public double AxisMin
        {
            get { return axisMin; }
            set
            {
                axisMin = value;
                OnPropertyChanged("AxisMin");
            }
        }
        public DispatcherTimer Timer { get; set; }
        public ObservableCollection<CategoryItem> Categories { get; set; }
        public List<PerformanceCounter> Counters { get; set; }
        public Dictionary<PerformanceCounter, LineSeries> CounterMap { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)
                .Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            SeriesCollection = new SeriesCollection();
            DateTimeFormatter = value => new DateTime((long) value).ToString("mm:ss");
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            SetAxisLimits(DateTime.Now);

            Timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            Timer.Tick += Timer_Tick;

            Categories = new ObservableCollection<CategoryItem>();
            Categories.Add(MakeCategoryItem("ServiceModelEndpoint 4.0.0.0"));
            Categories.Add(MakeCategoryItem("ServiceModelOperation 4.0.0.0"));
            Categories.Add(MakeCategoryItem("ServiceModelService 4.0.0.0"));
            Categories.Add(MakeCategoryItem("Test category"));

            Counters = new List<PerformanceCounter>();
            CounterMap = new Dictionary<PerformanceCounter, LineSeries>();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                instanceItems.Add(MakeInstanceItem("*", category.GetCounters(string.Empty)));
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
            counterItem.PropertyChanged += CounterItem_PropertyChanged;
            return counterItem;
        }

        private void CounterItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsChecked"))
            {
                var counterItem = (CounterItem) sender;

                if (counterItem.IsChecked == true)
                {
                    LineSeries series = new LineSeries()
                    {
                        PointGeometrySize = 9,
                        StrokeThickness = 2,
                        Title = counterItem.Name,
                        Values = new ChartValues<MeasureModel>()
                    };
                    SeriesCollection.Add(series);
                    CounterMap.Add(counterItem.Counter, series);
                    Counters.Add(counterItem.Counter);
                }
                else
                {
                    LineSeries series;
                    if (CounterMap.TryGetValue(counterItem.Counter, out series))
                    {
                        Counters.Remove(counterItem.Counter);
                        CounterMap.Remove(counterItem.Counter);
                        SeriesCollection.Remove(series);
                    }
                }
            }
        }

        private void captureData_Click(object sender, RoutedEventArgs e)
        {
            if (!Timer.IsEnabled)
            {
                Timer.Start();
                captureData.Content = "Stop Monitoring";
            }
            else
            {
                Timer.Stop();
                captureData.Content = "Start Monitoring";
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Settings file|*.xml";
            saveFileDialog.Title = "Save settings";

            if (saveFileDialog.ShowDialog() == true)
            {
                var fs = (FileStream) saveFileDialog.OpenFile();
                var settings = new Settings();

                foreach (CategoryItem category in Categories)
                {
                    foreach (InstanceItem instance in category.Instances)
                    {
                        foreach (CounterItem counter in instance.Counters)
                        {
                            if (counter.IsChecked == true)
                            {
                                settings.CounterPaths.Add(new CounterPath(counter.Counter));
                            }
                        }
                    }
                }

                var serial = new XmlSerializer(typeof(Settings));
                serial.Serialize(fs, settings);

                fs.Close();
            }
        }

        private void LoadSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Settings file|*.xml";
            openFileDialog.Title = "Load settings";

            if (openFileDialog.ShowDialog() == true)
            {
                var fs = (FileStream) openFileDialog.OpenFile();

                var serial = new XmlSerializer(typeof(Settings));
                var settings = (Settings) serial.Deserialize(fs);

                foreach (CounterPath path in settings.CounterPaths)
                {
                    CategoryItem category = Categories.FirstOrDefault(item => item.Name == path.CategoryName);
                    if (category == null) continue;
                    InstanceItem instance = category.Instances.FirstOrDefault(item => item.Name == path.InstanceName);
                    if (instance == null) continue;
                    CounterItem counter = instance.Counters.FirstOrDefault(item => item.Name == path.CounterName);
                    if (counter == null) continue;

                    counter.IsChecked = true;
                }

                fs.Close();
            }
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks;
            AxisMin = now.Ticks - TimeSpan.FromSeconds(8).Ticks;
        }

        private void Timer_Tick(object sender, EventArgs eventArgs)
        {
            foreach (PerformanceCounter counter in Counters)
            {
                LineSeries series;
                if (CounterMap.TryGetValue(counter, out series))
                {
                    var now = DateTime.Now;
                    var values = series.Values;

                    values.Add(new MeasureModel()
                    {
                        DateTime = now,
                        Value = counter.NextValue()
                    });
                    SetAxisLimits(now);

                    if (values.Count > 30) values.RemoveAt(0);
                }
            }
        }
    }
}
