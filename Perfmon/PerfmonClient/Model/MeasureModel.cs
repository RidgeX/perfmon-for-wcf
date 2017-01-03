using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonClient.Model
{
    public class MeasureModel
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }

        public MeasureModel(DateTime dateTime, double value)
        {
            DateTime = dateTime;
            Value = value;
        }
    }
}
