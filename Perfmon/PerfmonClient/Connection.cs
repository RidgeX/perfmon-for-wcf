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
        private bool hasConnectedOnce;
        private bool ignoreCheckedEvent;
        private DuplexChannelFactory<IPerfmonService> factory;
        private IPerfmonService service;

        public string Host { get; set; }
        public int Port { get; set; }
        public MachineItem MachineItem { get; set; }

        public Connection(string host, int port)
        {
            Host = host;
            Port = port;
            MachineItem = new MachineItem(host, port);

            hasConnectedOnce = false;
            ignoreCheckedEvent = false;

            NetTcpBinding binding = new NetTcpBinding();
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.Security.Mode = SecurityMode.None;
            string address = string.Format("net.tcp://{0}:{1}/Perfmon/", host, port);
            factory = new DuplexChannelFactory<IPerfmonService>(this, binding, address);

            service = factory.CreateChannel();
            ((IClientChannel) service).Faulted += OnFault;
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
                category.Counters.Sort((a, b) => a.Name.CompareTo(b.Name));
            }

            foreach (Category category in categories)
            {
                CategoryItem categoryItem = new CategoryItem(category.Name, MachineItem);

                foreach (Counter counter in category.Counters)
                {
                    CounterItem counterItem = new CounterItem(counter.Name, categoryItem);
                    counterItem.PropertyChanged += OnPropertyChanged;
                    categoryItem.CounterItems.Add(counterItem);
                }

                MachineItem.CategoryItems.Add(categoryItem);
            }

            mainWindow.MachineItems.Add(MachineItem);
        }

        private void ClearTreeView()
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            foreach (CategoryItem categoryItem in MachineItem.CategoryItems)
            {
                foreach (CounterItem counterItem in categoryItem.CounterItems)
                {
                    counterItem.PropertyChanged -= OnPropertyChanged;
                }
            }

            MachineItem.CategoryItems.Clear();
            mainWindow.MachineItems.Remove(MachineItem);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked" && !ignoreCheckedEvent)
            {
                CounterItem counterItem = (CounterItem) sender;

                if (counterItem.IsChecked == true)
                {
                    if (!TrySubscribe(counterItem.Parent.Name, counterItem.Name))
                    {
                        MessageBox.Show(string.Format("The following counter no longer exists:\n\\{0}\\{1}",
                            counterItem.Parent.Name, counterItem.Name), "Performance Monitor for WCF",
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
            try
            {
                service.Join();
                hasConnectedOnce = true;
                PopulateTreeView();
            }
            catch (CommunicationException) { }
        }

        public void Close()
        {
            try
            {
                ClearTreeView();
                service.Leave();
            }
            catch (CommunicationException) { }
        }

        public void Refresh()
        {
            var activeCounters = new HashSet<Tuple<string, string>>();

            foreach (CategoryItem categoryItem in MachineItem.CategoryItems)
            {
                foreach (CounterItem counterItem in categoryItem.CounterItems)
                {
                    if (counterItem.IsChecked == true)
                    {
                        activeCounters.Add(Tuple.Create(categoryItem.Name, counterItem.Name));
                    }
                }
            }

            ClearTreeView();
            TryRefresh();
            PopulateTreeView();

            foreach (Tuple<string, string> tuple in activeCounters)
            {
                CategoryItem categoryItem = MachineItem.CategoryItems.FirstOrDefault(item => item.Name == tuple.Item1);
                if (categoryItem == null) continue;
                CounterItem counterItem = categoryItem.CounterItems.FirstOrDefault(item => item.Name == tuple.Item2);
                if (counterItem == null) continue;

                ignoreCheckedEvent = true;
                counterItem.IsChecked = true;
                ignoreCheckedEvent = false;
            }
        }

        public void OnFault(object sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = (MainWindow) Application.Current.MainWindow;

                ((IClientChannel) service).Abort();

                string message;
                if (hasConnectedOnce)
                {
                    message = "Lost connection to {0}:{1}. Try reconnecting?";
                }
                else
                {
                    message = "Couldn't connect to {0}:{1}. Try reconnecting?";
                }

                if (MessageBox.Show(string.Format(message, Host, Port), "Performance Monitor for WCF",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    service = factory.CreateChannel();
                    ((IClientChannel) service).Faulted += OnFault;

                    try
                    {
                        service.Join();

                        foreach (CategoryItem categoryItem in MachineItem.CategoryItems)
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
                    mainWindow.Connections.Remove(this);
                }
            });
        }

        public void OnNext(EventData e)
        {
            var mainWindow = (MainWindow) Application.Current.MainWindow;

            Category category = e.Category;
            DateTime timestamp = e.Timestamp;

            CategoryItem categoryItem = MachineItem.CategoryItems.FirstOrDefault(item => item.Name == category.Name);
            if (categoryItem == null) return;

            foreach (Counter counter in category.Counters)
            {
                CounterItem counterItem = categoryItem.CounterItems.FirstOrDefault(item => item.Name == counter.Name);
                if (counterItem == null) continue;

                if (!counter.Instances.Any())
                {
                    if (!counterItem.InstanceItems.Contains(MainWindow.NoneItem))
                    {
                        counterItem.InstanceItems.Clear();
                        counterItem.InstanceItems.Add(MainWindow.NoneItem);
                    }
                }
                else
                {
                    counterItem.InstanceItems.Remove(MainWindow.NoneItem);
                }

                counter.Instances.Sort((a, b) => a.Name.CompareTo(b.Name));

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
