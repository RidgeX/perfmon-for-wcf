using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PerfmonClient.Model
{
    public class Tab : IXmlSerializable
    {
        public string Name { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public ObservableCollection<ChartItem> ChartItems { get; set; }

        public Tab()
        {
            ChartItems = new ObservableCollection<ChartItem>();
        }

        public Tab(string name, int rows, int cols)
        {
            Name = name;
            Rows = rows;
            Columns = cols;
            ChartItems = new ObservableCollection<ChartItem>();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    ChartItems.Add(new ChartItem(row, col));
                }
            }
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
