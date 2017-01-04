using PerfmonClient.Model;
using PerfmonServiceLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PerfmonClient
{
    public class Connection : IPerfmonCallback
    {
        private DuplexChannelFactory<IPerfmonService> factory;
        private IPerfmonService service;

        public Connection(string host, int port)
        {
            NetTcpBinding binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.Security.Mode = SecurityMode.None;
            string address = string.Format("net.tcp://{0}:{1}/Perfmon/", host, port);

            factory = new DuplexChannelFactory<IPerfmonService>(this, binding, address);
            service = factory.CreateChannel();
            ((IClientChannel) service).Faulted += OnFault;
        }

        private void TryJoin()
        {
            try
            {
                service.Join();
            }
            catch (CommunicationException) { }
        }

        private void TryLeave()
        {
            try
            {
                service.Leave();
            }
            catch (CommunicationException) { }
        }

        private CategoryList TryList()
        {
            try
            {
                return service.List();
            }
            catch (CommunicationException)
            {
                return new CategoryList();
            }
        }

        private void TryRefresh()
        {
            try
            {
                service.Refresh();
            }
            catch (CommunicationException) { }
        }

        private bool TrySubscribe(string categoryName, string counterName)
        {
            try
            {
                return service.Subscribe(categoryName, counterName);
            }
            catch (CommunicationException)
            {
                return false;
            }
        }

        private void TryUnsubscribe(string categoryName, string counterName)
        {
            try
            {
                service.Unsubscribe(categoryName, counterName);
            }
            catch (CommunicationException) { }
        }

        private void PopulateTreeView()
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            CategoryList categories = TryList();
            categories.Sort((a, b) => a.Name.CompareTo(b.Name));

            foreach (Category category in categories)
            {
                CategoryItem categoryItem = new CategoryItem(category.Name);

                foreach (Counter counter in category.Counters)
                {
                    CounterItem counterItem = new CounterItem(counter.Name, categoryItem);
                    counterItem.PropertyChanged += OnPropertyChanged;
                    categoryItem.CounterItems.Add(counterItem);
                }

                mainWindow.CategoryItems.Add(categoryItem);
            }
        }

        private void ClearTreeView()
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            foreach (CategoryItem categoryItem in mainWindow.CategoryItems)
            {
                foreach (CounterItem counterItem in categoryItem.CounterItems)
                {
                    counterItem.PropertyChanged -= OnPropertyChanged;
                }
            }

            mainWindow.CategoryItems.Clear();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                CounterItem counterItem = (CounterItem) sender;

                if (counterItem.IsChecked == true)
                {
                    if (!TrySubscribe(counterItem.Parent.Name, counterItem.Name))
                    {
                        MessageBox.Show(string.Format("The following counter no longer exists:\n\\{0}\\{1}",
                            counterItem.Parent.Name, counterItem.Name), "Performance Monitor",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    counterItem.IsExpanded = true;
                }
                else
                {
                    TryUnsubscribe(counterItem.Parent.Name, counterItem.Name);

                    counterItem.IsExpanded = false;
                    counterItem.InstanceItems.Clear();
                }
            }
        }

        public void Open()
        {
            TryJoin();
            PopulateTreeView();
        }

        public void Close()
        {
            TryLeave();
        }

        public void OnFault(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = (MainWindow) Application.Current.MainWindow;

                ((IClientChannel) service).Abort();

                if (MessageBox.Show("Lost connection to server. Try reconnecting?", "Performance Monitor",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    service = factory.CreateChannel();
                    ((IClientChannel) service).Faulted += OnFault;

                    try
                    {
                        service.Join();

                        foreach (CategoryItem categoryItem in mainWindow.CategoryItems)
                        {
                            foreach (CounterItem counterItem in categoryItem.CounterItems)
                            {
                                if (counterItem.IsChecked == true)
                                {
                                    if (!TrySubscribe(categoryItem.Name, counterItem.Name))
                                    {
                                        counterItem.IsChecked = false;
                                    }
                                }
                            }
                        }
                    }
                    catch (CommunicationException) { }
                }
                else
                {
                    ClearTreeView();
                }
            });
        }

        public void OnNext(EventData e)
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            Category category = e.Category;
            DateTime timestamp = e.Timestamp;

            CategoryItem categoryItem = mainWindow.CategoryItems.FirstOrDefault(item => item.Name == category.Name);
            if (categoryItem == null) return;

            foreach (Counter counter in category.Counters)
            {
                CounterItem counterItem = categoryItem.CounterItems.FirstOrDefault(item => item.Name == counter.Name);
                if (counterItem == null) continue;

                foreach (Instance instance in counter.Instances)
                {
                    InstanceItem instanceItem = counterItem.InstanceItems.FirstOrDefault(item => item.Name == instance.Name);

                    if (instanceItem == null)
                    {
                        instanceItem = new InstanceItem(instance.Name, counterItem);
                        counterItem.InstanceItems.Add(instanceItem);
                    }

                    mainWindow.UpdateSeries(instanceItem.Path, timestamp, instance.Value);
                }
            }
        }
    }
}
