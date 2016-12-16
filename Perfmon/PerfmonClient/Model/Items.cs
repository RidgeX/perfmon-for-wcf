using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonClient.Model
{
    public abstract class Item : INotifyPropertyChanged
    {
        private bool isSelected;

        public string Name { get; set; }
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

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
        public ObservableCollection<InstanceItem> InstanceItems { get; set; }

        public CategoryItem(string name, ObservableCollection<InstanceItem> instanceItems)
        {
            Name = name;
            InstanceItems = instanceItems;
        }
    }

    public class InstanceItem : Item
    {
        public ObservableCollection<CounterItem> CounterItems { get; set; }

        public InstanceItem(string name, ObservableCollection<CounterItem> counterItems)
        {
            Name = name;
            CounterItems = counterItems;
        }
    }

    public class CounterItem : Item
    {
        //private bool? isChecked;

        public PerformanceCounter Counter { get; set; }
        /*
        public bool? IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }
        */

        public CounterItem(string name, PerformanceCounter counter)
        {
            Name = name;
            Counter = counter;
            //IsChecked = false;
        }
    }
}
