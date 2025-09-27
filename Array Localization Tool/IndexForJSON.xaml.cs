using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Array_Translate_Tool
{
    public partial class IndexForJSON : Window
    {
        public int SelectedIndex { get; private set; } = -1;

        public IndexForJSON(string prompt, IEnumerable<string> items)
        {
            InitializeComponent();

            if (LblPrompt != null)
                LblPrompt.Text = prompt;

            if (ListBoxIndices != null)
                ListBoxIndices.ItemsSource = items;

            if (BtnOk != null)
                BtnOk.IsEnabled = false;
        }

        private void ListBoxIndices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxIndices.SelectedItem == null)
            {
                if (BtnOk != null) BtnOk.IsEnabled = false;
                return;
            }

            if (BtnOk != null) BtnOk.IsEnabled = true;

            var selectedItem = ListBoxIndices.SelectedItem.ToString();
            var parts = selectedItem.Split(':');
            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int index))
                SelectedIndex = index;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
