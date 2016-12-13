using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PerfmonClient.UI
{
    public class GridHelpers
    {
        #region RowCount Property

        /// <summary>
        /// Adds the specified number of rows to RowDefinitions.
        /// </summary>
        public static readonly DependencyProperty RowCountProperty =
            DependencyProperty.RegisterAttached(
                "RowCount", typeof(int), typeof(GridHelpers),
                new PropertyMetadata(-1, RowCountChanged));

        public static int GetRowCount(DependencyObject obj)
        {
            return (int) obj.GetValue(RowCountProperty);
        }

        public static void SetRowCount(DependencyObject obj, int value)
        {
            obj.SetValue(RowCountProperty, value);
        }

        public static void RowCountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int) e.NewValue < 0) return;

            var grid = (Grid) obj;
            grid.RowDefinitions.Clear();

            for (int i = 0; i < (int) e.NewValue; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition() {
                    Height = new GridLength(1, GridUnitType.Star)
                });
            }
        }

        #endregion

        #region ColumnCount Property

        /// <summary>
        /// Adds the specified number of columns to ColumnDefinitions.
        /// </summary>
        public static readonly DependencyProperty ColumnCountProperty =
            DependencyProperty.RegisterAttached(
                "ColumnCount", typeof(int), typeof(GridHelpers),
                new PropertyMetadata(-1, ColumnCountChanged));

        public static int GetColumnCount(DependencyObject obj)
        {
            return (int) obj.GetValue(ColumnCountProperty);
        }

        public static void SetColumnCount(DependencyObject obj, int value)
        {
            obj.SetValue(ColumnCountProperty, value);
        }

        public static void ColumnCountChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (!(obj is Grid) || (int) e.NewValue < 0) return;

            var grid = (Grid) obj;
            grid.ColumnDefinitions.Clear();

            for (int i = 0; i < (int) e.NewValue; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition() {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            }
        }

        #endregion
    }
}
