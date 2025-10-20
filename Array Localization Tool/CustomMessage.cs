using System.Windows;
using System.Windows.Controls;

namespace Array_Translate_Tool
{
    public static class CustomMessageBox
    {
        public static void Show(string message, string title = "Повідомлення")
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = Application.Current.Resources["WindowBackgroundBrush"] as System.Windows.Media.Brush,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Application.Current.Resources["TextForegroundBrush"] as System.Windows.Media.Brush
            };
            Grid.SetRow(textBlock, 0);

            var button = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 30,
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            button.Click += (s, e) => window.Close();
            Grid.SetRow(button, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(button);

            window.Content = grid;
            window.ShowDialog();
        }

        public static bool ShowYesNo(string message, string title = "Підтвердження")
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = Application.Current.Resources["WindowBackgroundBrush"] as System.Windows.Media.Brush,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            bool result = false;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Application.Current.Resources["TextForegroundBrush"] as System.Windows.Media.Brush
            };
            Grid.SetRow(textBlock, 0);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var btnYes = new Button
            {
                Content = "Так",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            btnYes.Click += (s, e) => { result = true; window.Close(); };

            var btnNo = new Button
            {
                Content = "Ні",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            btnNo.Click += (s, e) => { result = false; window.Close(); };

            buttonPanel.Children.Add(btnYes);
            buttonPanel.Children.Add(btnNo);

            Grid.SetRow(buttonPanel, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(buttonPanel);

            window.Content = grid;
            window.ShowDialog();

            return result;
        }

        public enum MessageBoxResult
        {
            Yes,
            No,
            Cancel
        }

        public static MessageBoxResult ShowYesNoCancel(string message, string title = "Підтвердження")
        {
            var window = new Window
            {
                Title = title,
                Width = 450,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = Application.Current.Resources["WindowBackgroundBrush"] as System.Windows.Media.Brush,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            MessageBoxResult result = MessageBoxResult.Cancel;

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Application.Current.Resources["TextForegroundBrush"] as System.Windows.Media.Brush
            };
            Grid.SetRow(textBlock, 0);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var btnYes = new Button
            {
                Content = "Так",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            btnYes.Click += (s, e) => { result = MessageBoxResult.Yes; window.Close(); };

            var btnNo = new Button
            {
                Content = "Ні",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            btnNo.Click += (s, e) => { result = MessageBoxResult.No; window.Close(); };

            var btnCancel = new Button
            {
                Content = "Скасувати",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };
            btnCancel.Click += (s, e) => { result = MessageBoxResult.Cancel; window.Close(); };

            buttonPanel.Children.Add(btnYes);
            buttonPanel.Children.Add(btnNo);
            buttonPanel.Children.Add(btnCancel);

            Grid.SetRow(buttonPanel, 1);

            grid.Children.Add(textBlock);
            grid.Children.Add(buttonPanel);

            window.Content = grid;
            window.ShowDialog();

            return result;
        }
    }
}