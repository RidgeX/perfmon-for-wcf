using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PerfmonClient.Model
{
    public class Tab : INotifyPropertyChanged, IXmlSerializable
    {
        private string name;
        private int rows;
        private int columns;

        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                OnPropertyChanged("Name");
            }
        }
        public int Rows
        {
            get { return rows; }
            set
            {
                rows = value;
                OnPropertyChanged("Rows");
            }
        }
        public int Columns
        {
            get { return columns; }
            set
            {
                columns = value;
                OnPropertyChanged("Columns");
            }
        }
        public ObservableCollection<ChartItem> ChartItems { get; set; }

        public Tab()
        {
            ChartItems = new ObservableCollection<ChartItem>();
        }

        public Tab(string name, int rows, int cols) : this()
        {
            Name = name;
            Rows = rows;
            Columns = cols;

            Initialize();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Initialize()
        {
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    ChartItems.Add(new ChartItem(row, col));
                }
            }
        }

        public void Destroy()
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            foreach (ChartItem chartItem in ChartItems)
            {
                foreach (Series series in chartItem.SeriesCollection)
                {
                    mainWindow.RemoveCounterListener(series);
                }
            }

            ChartItems.Clear();
        }

        #region Serialization

        public XmlSchema GetSchema() { return null; }

        public void ReadXml(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName.Equals("Tab"))
            {
                Name = reader.GetAttribute("Name");
                Rows = int.Parse(reader.GetAttribute("Rows"));
                Columns = int.Parse(reader.GetAttribute("Columns"));

                if (reader.ReadToDescendant("ChartItem"))
                {
                    while (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName.Equals("ChartItem"))
                    {
                        ChartItem chartItem = new ChartItem();
                        chartItem.ReadXml(reader);
                        ChartItems.Add(chartItem);
                    }
                }

                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Name", Name);
            writer.WriteAttributeString("Rows", Rows.ToString());
            writer.WriteAttributeString("Columns", Columns.ToString());

            foreach (ChartItem chartItem in ChartItems)
            {
                writer.WriteStartElement("ChartItem");
                chartItem.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
}
