using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Array_Translate_Tool
{
    public partial class IndexSelectionWindow : Window
    {
        public int SelectedIndex { get; private set; } = -1;

        public IndexSelectionWindow(string prompt, IEnumerable<string> items)
        {
            InitializeComponent();
            LblPrompt.Text = prompt;
            ListBoxIndices.ItemsSource = items;
        }

        private void ListBoxIndices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtnOk.IsEnabled = ListBoxIndices.SelectedItem != null;
            if (ListBoxIndices.SelectedItem != null)
            {
                var selectedItem = ListBoxIndices.SelectedItem.ToString();
                var parts = selectedItem.Split(':');
                if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int index))
                {
                    SelectedIndex = index;
                }
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}