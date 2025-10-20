using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Array_Translate_Tool
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            LoadTheme();

            var mainWindow = new MainWindow();
            mainWindow.Show();

            if (e.Args.Length > 0)
            {
                string filePath = e.Args[0];
                if (File.Exists(filePath) && Path.GetExtension(filePath).ToLower() == ".json")
                {
                    mainWindow.OpenFile(filePath);
                }
                else
                {
                    CustomMessageBox.Show("Програма працює лише з файлами JSON.", "Помилка");
                }
            }
        }

        public static void ApplyTheme(bool isDark)
        {
            var resources = Application.Current.Resources;

            if (isDark)
            {
                resources["WindowBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B222A"));
                resources["TextForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d0d3"));
                resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#646464"));

                resources["DataGridBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1b222a"));
                resources["DataGridSelectionBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#234c9f"));
                resources["DataGridSelectionForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d0d3"));
                resources["ModifiedRowBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C98FB98"));

                resources["MenuBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B222A"));
                resources["MenuForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d0d3"));

                resources["TextBoxBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#232C37"));
                resources["TextBoxForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d0d3"));

                resources["ButtonBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1b222a"));
                resources["ButtonForegroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c9d0d3"));
                resources["ButtonHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2e3947"));
                resources["ButtonPressedBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1b222a"));

                resources["ScrollBarBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1B222A"));
                resources["ScrollBarThumbBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4d4d4d"));
                resources["ScrollBarThumbHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7a7a7a"));
                resources["ScrollBarThumbPressedBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6a6a6"));
                resources["ScrollBarLineButtonHoverBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#373737"));
                resources["ScrollBarLineButtonPressedBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6a6a6"));

            }
            else
            {
                resources["WindowBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["TextForegroundBrush"] = new SolidColorBrush(Colors.Black);
                resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(194, 195, 201));

                resources["DataGridBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["DataGridSelectionBrush"] = new SolidColorBrush(Color.FromRgb(51, 153, 255));
                resources["DataGridSelectionForegroundBrush"] = new SolidColorBrush(Colors.White);
                resources["ModifiedRowBrush"] = new SolidColorBrush(Color.FromArgb(76, 152, 251, 152));

                resources["MenuBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["MenuForegroundBrush"] = new SolidColorBrush(Colors.Black);

                resources["TextBoxBackgroundBrush"] = new SolidColorBrush(Colors.White);
                resources["TextBoxForegroundBrush"] = new SolidColorBrush(Colors.Black);

                resources["ButtonBackgroundBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dddddd"));
                resources["ButtonForegroundBrush"] = new SolidColorBrush(Colors.Black);
                resources["ButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(229, 229, 229));
                resources["ButtonPressedBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dddddd"));

                resources["ScrollBarBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                resources["ScrollBarThumbBrush"] = new SolidColorBrush(Color.FromRgb(205, 205, 205));
                resources["ScrollBarThumbHoverBrush"] = new SolidColorBrush(Color.FromRgb(180, 180, 180));
                resources["ScrollBarThumbPressedBrush"] = new SolidColorBrush(Color.FromRgb(160, 160, 160));
                resources["ScrollBarLineButtonHoverBrush"] = new SolidColorBrush(Color.FromRgb(230, 230, 230));
                resources["ScrollBarLineButtonPressedBrush"] = new SolidColorBrush(Color.FromRgb(210, 210, 210));

            }

            SaveTheme(isDark);
        }

        private static void LoadTheme()
        {
            string settingsPath = GetSettingsPath();
            bool isDark = false;

            if (File.Exists(settingsPath))
            {
                string theme = File.ReadAllText(settingsPath).Trim();
                isDark = theme == "Dark";
            }

            ApplyTheme(isDark);
        }

        private static void SaveTheme(bool isDark)
        {
            string settingsPath = GetSettingsPath();
            File.WriteAllText(settingsPath, isDark ? "Dark" : "Light");
        }

        private static string GetSettingsPath()
        {
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string folder = Path.Combine(appData, "ArrayLocalizationTool");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Path.Combine(folder, "theme.txt");
        }

        public static bool IsDarkTheme()
        {
            var bg = Application.Current.Resources["WindowBackgroundBrush"] as SolidColorBrush;
            if (bg != null)
            {
                return bg.Color.R < 128;
            }
            return false;
        }
    }
}