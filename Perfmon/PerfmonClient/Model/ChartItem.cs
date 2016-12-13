using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonClient.Model
{
    public class ChartItem
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public ChartItem(int row, int col)
        {
            Row = row;
            Column = col;
        }
    }
}
