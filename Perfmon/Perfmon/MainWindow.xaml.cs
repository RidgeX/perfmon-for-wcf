using LiveCharts;
using LiveCharts.Configurations;
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

        public ChartValues<MeasureModel> ChartValues { get; set; }
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
        public PerformanceCounter Counter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            var mapper = Mappers.Xy<MeasureModel>()
                .X(model => model.DateTime.Ticks)
                .Y(model => model.Value);
            Charting.For<MeasureModel>(mapper);

            ChartValues = new ChartValues<MeasureModel>();
            DateTimeFormatter = value => new DateTime((long) value).ToString("mm:ss");
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            SetAxisLimits(DateTime.Now);

            Timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            Timer.Tick += Timer_Tick;
            IsDataInjectionRunning = false;

            Counter = new PerformanceCounter("ServiceModelOperation 4.0.0.0", "Calls", "Calcu50.ICalculatorService.Add@HTTP:||LOCALHOST:8000|CALCULATOR|");
            //Counter = new PerformanceCounter("Test category", "Test counter");

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
            var now = DateTime.Now;

            ChartValues.Add(new MeasureModel()
            {
                DateTime = now,
                Value = Counter.NextValue()
            });
            SetAxisLimits(now);

            if (ChartValues.Count > 30) ChartValues.RemoveAt(0);
        }
    }
}
