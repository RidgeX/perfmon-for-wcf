using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perfmon
{
    public abstract class Item : INotifyPropertyChanged
    {
        public string Name { get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class CategoryItem : Item
    {
        public ObservableCollection<InstanceItem> Instances { get; set; }

        public CategoryItem(string name, ObservableCollection<InstanceItem> instances)
        {
            Name = name;
            Instances = instances;
        }
    }

    public class InstanceItem : Item
    {
        public ObservableCollection<CounterItem> Counters { get; set; }

        public InstanceItem(string name, ObservableCollection<CounterItem> counters)
        {
            Name = name;
            Counters = counters;
        }
    }

    public class CounterItem : Item
    {
        private bool? isChecked;

        public PerformanceCounter Counter { get; set; }
        public bool? IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public CounterItem(string name, PerformanceCounter counter)
        {
            Name = name;
            Counter = counter;
            IsChecked = false;
        }
    }
}
