using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class HostValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string host = (string) value;

            if (string.IsNullOrEmpty(host))
            {
                return new ValidationResult(false, "Host must be non-empty");
            }

            return ValidationResult.ValidResult;
        }
    }

    public class PortValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int port;
            if (!int.TryParse((string) value, out port))
            {
                return new ValidationResult(false, "Port must be an integer");
            }

            if (port < 1 || port > 65535)
            {
                return new ValidationResult(false, "Port must be between 1 and 65535");
            }

            return ValidationResult.ValidResult;
        }
    }

    /// <summary>
    /// Interaction logic for ConnectDialog.xaml
    /// </summary>
    public partial class ConnectDialog : Window
    {
        public string Host { get; set; }
        public int Port { get; set; }

        public ConnectDialog()
        {
            InitializeComponent();
            DataContext = this;

            Host = "localhost";
            Port = 8080;
        }

        private bool IsValid(DependencyObject node)
        {
            // Check if dependency object was passed
            if (node != null)
            {
                // Check if dependency object is valid
                bool isValid = !Validation.GetHasError(node);

                if (!isValid)
                {
                    // If the dependency object is invalid, and it can receive the focus,
                    // set the focus
                    if (node is IInputElement)
                    {
                        Keyboard.Focus((IInputElement) node);
                    }

                    return false;
                }
            }

            // If this dependency object is valid, check all child dependency objects
            foreach (object subnode in LogicalTreeHelper.GetChildren(node))
            {
                if (subnode is DependencyObject)
                {
                    // If a child dependency object is invalid, return false immediately,
                    // otherwise keep checking
                    if (!IsValid((DependencyObject) subnode)) return false;
                }
            }

            // All dependency objects are valid
            return true;
        }

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid(this)) return;

            DialogResult = true;
        }
    }
}
