using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PerfmonClient
{
    public class SavedCounterItem
    {
        private bool? isChecked;

        public long Id { get; set; }
        public string Path { get; set; }
        public bool? IsChecked
        {
            get { return isChecked; }
            set
            {
                isChecked = value;
                OnPropertyChanged("IsChecked");
            }
        }

        public SavedCounterItem(long id, string path)
        {
            Id = id;
            Path = path;
            IsChecked = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// Interaction logic for ExportDataDialog.xaml
    /// </summary>
    public partial class ExportDataDialog : Window
    {
        public ObservableCollection<SavedCounterItem> SavedCounters { get; set; }

        public ExportDataDialog(SQLiteConnection database)
        {
            InitializeComponent();
            DataContext = this;

            SavedCounters = new ObservableCollection<SavedCounterItem>();

            SQLiteCommand cmd = new SQLiteCommand("SELECT * FROM counter", database);
            SQLiteDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                long id = (long) reader["id"];
                string path = (string) reader["path"];
                SavedCounters.Add(new SavedCounterItem(id, path));
            }
        }

        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
