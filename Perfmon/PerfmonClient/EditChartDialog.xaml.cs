using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Xceed.Wpf.Toolkit;

namespace PerfmonClient
{
    /// <summary>
    /// Interaction logic for EditChartDialog.xaml
    /// </summary>
    public partial class EditChartDialog : Window
    {
        /// <summary>
        /// Google's Material Design color palette
        /// https://material.io/guidelines/style/color.html
        /// </summary>
        public static ObservableCollection<ColorItem> MaterialColors { get; private set; }

        static EditChartDialog()
        {
            MaterialColors = new ObservableCollection<ColorItem>()
            {
                new ColorItem(Color.FromRgb(244,  67,  54), "Red"),
                new ColorItem(Color.FromRgb(233,  30,  99), "Pink"),
                new ColorItem(Color.FromRgb(156,  39, 176), "Purple"),
                new ColorItem(Color.FromRgb(103,  58, 183), "Deep Purple"),
                new ColorItem(Color.FromRgb( 63,  81, 181), "Indigo"),
                new ColorItem(Color.FromRgb( 33, 150, 243), "Blue"),
                new ColorItem(Color.FromRgb(  3, 169, 244), "Light Blue"),
                new ColorItem(Color.FromRgb(  0, 188, 212), "Cyan"),
                new ColorItem(Color.FromRgb(  0, 150, 136), "Teal"),
                new ColorItem(Color.FromRgb( 76, 175,  80), "Green"),
                new ColorItem(Color.FromRgb(139, 195,  74), "Light Green"),
                new ColorItem(Color.FromRgb(205, 220,  57), "Lime"),
                new ColorItem(Color.FromRgb(255, 235,  59), "Yellow"),
                new ColorItem(Color.FromRgb(255, 193,   7), "Amber"),
                new ColorItem(Color.FromRgb(255, 152,   0), "Orange"),
                new ColorItem(Color.FromRgb(255,  87,  34), "Deep Orange"),
                new ColorItem(Color.FromRgb(121,  85,  72), "Brown"),
                new ColorItem(Color.FromRgb(158, 158, 158), "Grey"),
                new ColorItem(Color.FromRgb( 96, 125, 139), "Blue Grey"),
                new ColorItem(Color.FromRgb(  0,   0,   0), "Black")
            };
        }

        public EditChartDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
