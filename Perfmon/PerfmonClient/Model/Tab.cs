using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonClient.Model
{
    public class Tab
    {
        public string Name { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public ObservableCollection<ChartItem> ChartItems { get; set; }

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
    }
}
