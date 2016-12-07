using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace Perfmon
{
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
        public bool IsDataInjectionRunning { get; set; }
        public List<PerformanceCounter> Counters { get; set; }

        public MainWindow()
        {
            InitializeComponent();

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
            IsDataInjectionRunning = false;

            Counters = new List<PerformanceCounter>();
            //Counters.Add(new PerformanceCounter("ServiceModelOperation 4.0.0.0", "Calls", "Calcu50.ICalculatorService.Add@HTTP:||LOCALHOST:8000|CALCULATOR|"));
            Counters.Add(new PerformanceCounter("ServiceModelOperation 4.0.0.0", "Calls Per Second", "Calcu50.ICalculatorService.Add@HTTP:||LOCALHOST:8000|CALCULATOR|"));
            //Counters.Add(new PerformanceCounter("ServiceModelOperation 4.0.0.0", "Calls Duration", "Calcu50.ICalculatorService.Add@HTTP:||LOCALHOST:8000|CALCULATOR|"));
            //Counters.Add(new PerformanceCounter("ServiceModelOperation 4.0.0.0", "Calls Outstanding", "Calcu50.ICalculatorService.Add@HTTP:||LOCALHOST:8000|CALCULATOR|"));
            Counters.Add(new PerformanceCounter("Test category", "Test counter"));

            for (var i = 0; i < Counters.Count; i++)
            {
                LineSeries series = new LineSeries()
                {
                    PointGeometrySize = 9,
                    StrokeThickness = 2,
                    Title = Counters[i].CounterName,
                    Values = new ChartValues<MeasureModel>()
                };
                SeriesCollection.Add(series);
            }

            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void runData_Click(object sender, RoutedEventArgs e)
        {
            if (IsDataInjectionRunning)
            {
                Timer.Stop();
                IsDataInjectionRunning = false;
            }
            else
            {
                Timer.Start();
                IsDataInjectionRunning = true;
            }
        }

        private void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks;
            AxisMin = now.Ticks - TimeSpan.FromSeconds(8).Ticks;
        }

        private void Timer_Tick(object sender, EventArgs eventArgs)
        {
            for (int i = 0; i < Counters.Count; i++)
            {
                var now = DateTime.Now;
                var values = SeriesCollection[i].Values;

                values.Add(new MeasureModel()
                {
                    DateTime = now,
                    Value = Counters[i].NextValue()
                });
                SetAxisLimits(now);

                if (values.Count > 30) values.RemoveAt(0);
            }
        }
    }
}
