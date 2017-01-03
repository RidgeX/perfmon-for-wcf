using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfmonClient.Model
{
    public abstract class Item : INotifyPropertyChanged
    {
        private bool isExpanded;
        private bool isSelected;

        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                isExpanded = value;
                OnPropertyChanged("IsExpanded");
            }
        }
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
        public string Name { get; set; }
        public ObservableCollection<CounterItem> CounterItems { get; set; }

        public CategoryItem(string name)
        {
            Name = name;
            CounterItems = new ObservableCollection<CounterItem>();
        }
    }

    public class CounterItem : Item
    {
        private bool? isChecked;

        public string Name { get; set; }
        public CategoryItem Parent { get; set; }
        public ObservableCollection<InstanceItem> InstanceItems { get; set; }
        public bool? IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public CounterItem(string name, CategoryItem parent)
        {
            Name = name;
            Parent = parent;
            InstanceItems = new ObservableCollection<InstanceItem>();
            IsChecked = false;
        }
    }

    public class InstanceItem : Item
    {
        public string Name { get; set; }
        public CounterItem Parent { get; set; }
        public string DisplayName
        {
            get
            {
                if (Parent == null) return string.Empty;

                // ("Available MBytes", "*") => "Available MBytes"
                // ("% Processor Time", "_Total") => "% Processor Time (_Total)"
                // ("Calls", "Calcu50.ICalculatorService.Add@http:||localhost:8000|Calculator|") => "Calls (Calcu50.ICalculatorService.Add)"
                if (Name == "*")
                {
                    return Parent.Name;
                }
                else
                {
                    int index = Name.IndexOf('@');
                    return string.Format("{0} ({1})", Parent.Name, (index != -1 ? Name.Substring(0, index) : Name));
                }
            }
        }
        public string Path
        {
            get
            {
                if (Parent == null || Parent.Parent == null) return string.Empty;

                // ("Processor", "% Processor Time", "_Total") => "\Processor(_Total)\% Processor Time"
                return string.Format(@"\{0}({1})\{2}", Parent.Parent.Name, Name, Parent.Name);
            }
        }

        public InstanceItem(string name, CounterItem parent)
        {
            Name = name;
            Parent = parent;
        }
    }
}
