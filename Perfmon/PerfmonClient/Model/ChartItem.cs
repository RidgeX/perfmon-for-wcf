using LiveCharts;
using LiveCharts.Wpf;
using PerfmonClient.UI.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
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

        public void SetAxisLimits(DateTime timestamp)
        {
            AxisMax = timestamp.Ticks + TimeSpan.FromSeconds(1).Ticks;
            AxisMin = timestamp.Ticks - TimeSpan.FromSeconds(8).Ticks;
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
                        string title = reader.GetAttribute("Title");

                        var color = (Color) ColorConverter.ConvertFromString(reader.GetAttribute("Color"));
                        LineSeriesBrushConverter brushConverter = new LineSeriesBrushConverter();
                        object[] brushes = brushConverter.ConvertBack(color, new Type[] { typeof(SolidColorBrush), typeof(SolidColorBrush) }, null, CultureInfo.InvariantCulture);
                        var strokeBrush = (SolidColorBrush) brushes[0];
                        var fillBrush = (SolidColorBrush) brushes[1];

                        string path = reader.GetAttribute("Path");

                        Series series = MainWindow.CreateSeries(title);
                        series.Stroke = strokeBrush;
                        series.Fill = fillBrush;

                        SeriesCollection.Add(series);
                        mainWindow.AddCounterListener(path, series);

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

                writer.WriteAttributeString("Title", series.Title);

                LineSeriesBrushConverter brushConverter = new LineSeriesBrushConverter();
                var color = (Color) brushConverter.Convert(new object[] { series.Stroke, series.Fill }, typeof(Color), null, CultureInfo.InvariantCulture);
                writer.WriteAttributeString("Color", color.ToString());

                string path = mainWindow.CounterListeners.Where(kvp => kvp.Value.Contains(series)).Select(kvp => kvp.Key).First();
                writer.WriteAttributeString("Path", path);

                writer.WriteEndElement();
            }
        }

        #endregion
    }
}
