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
    public class NameValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string name = (string) value;

            if (string.IsNullOrEmpty(name))
            {
                return new ValidationResult(false, "Name must be non-empty");
            }

            return ValidationResult.ValidResult;
        }
    }

    public class SizeValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int size;
            if (!int.TryParse((string) value, out size))
            {
                return new ValidationResult(false, "Size must be an integer");
            }

            if (size <= 0)
            {
                return new ValidationResult(false, "Size must be positive");
            }

            return ValidationResult.ValidResult;
        }
    }

    /// <summary>
    /// Interaction logic for NewTabDialog.xaml
    /// </summary>
    public partial class NewTabDialog : Window
    {
        public string TabName { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }

        public NewTabDialog()
        {
            InitializeComponent();
            DataContext = this;

            TabName = "Untitled";
            Rows = 1;
            Columns = 1;
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

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid(this)) return;

            DialogResult = true;
        }
    }
}
