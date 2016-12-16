using LiveCharts;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PerfmonClient.Model
{
    public class ChartItem : INotifyPropertyChanged, IXmlSerializable
    {
        private double axisMax;
        private double axisMin;

        public int Row { get; set; }
        public int Column { get; set; }

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

        public ChartItem()
        {
            SeriesCollection = new SeriesCollection();
            DateTimeFormatter = ticks => new DateTime((long) ticks).ToString("mm:ss");
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            SetAxisLimits(DateTime.Now);
        }

        public ChartItem(int row, int col)
        {
            Row = row;
            Column = col;

            SeriesCollection = new SeriesCollection();
            DateTimeFormatter = ticks => new DateTime((long) ticks).ToString("mm:ss");
            AxisStep = TimeSpan.FromSeconds(1).Ticks;
            SetAxisLimits(DateTime.Now);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public void SetAxisLimits(DateTime now)
        {
            AxisMax = now.Ticks + TimeSpan.FromSeconds(1).Ticks;
            AxisMin = now.Ticks - TimeSpan.FromSeconds(8).Ticks;
        }

        #region Serialization

        public XmlSchema GetSchema() { return null; }

        public void ReadXml(XmlReader reader)
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName.Equals("ChartItem"))
            {
                Row = int.Parse(reader.GetAttribute("Row"));
                Column = int.Parse(reader.GetAttribute("Column"));

                if (reader.ReadToDescendant("Series"))
                {
                    while (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName.Equals("Series"))
                    {
                        string categoryName = reader.GetAttribute("CategoryName");
                        string instanceName = reader.GetAttribute("InstanceName");
                        string counterName = reader.GetAttribute("CounterName");
                        CounterItem counterItem = mainWindow.FindCounterItem(categoryName, instanceName, counterName);

                        if (counterItem == null)
                        {
                            MessageBox.Show(string.Format("The following counter could not be found:\n\\{0}({1})\\{2}",
                                categoryName, instanceName, counterName), "Load Tab",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            reader.Read();
                            continue;
                        }

                        LineSeries series = new LineSeries()
                        {
                            PointGeometrySize = 9,
                            StrokeThickness = 2,
                            Title = counterItem.Name,
                            Values = new ChartValues<MeasureModel>()
                        };
                        SeriesCollection.Add(series);
                        mainWindow.CounterSource.Add(series, counterItem);

                        reader.Read();
                    }
                }

                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            writer.WriteAttributeString("Row", Row.ToString());
            writer.WriteAttributeString("Column", Column.ToString());

            foreach (Series series in SeriesCollection)
            {
                writer.WriteStartElement("Series");

                PerformanceCounter counter = mainWindow.CounterSource[series].Counter;
                writer.WriteAttributeString("CategoryName", counter.CategoryName);
                writer.WriteAttributeString("InstanceName", string.IsNullOrEmpty(counter.InstanceName) ? "*" : counter.InstanceName);
                writer.WriteAttributeString("CounterName", counter.CounterName);

                writer.WriteEndElement();
            }
        }

        #endregion
    }
}
