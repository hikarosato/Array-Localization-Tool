using System.IO;
using System.Windows;

namespace Array_Translate_Tool
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
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
                    MessageBox.Show("Програма працює лише з файлами JSON.", "Помилка");
                }
            }
        }
    }
}