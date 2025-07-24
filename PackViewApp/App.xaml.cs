using PackViewApp.Helpers;
using System.Configuration;
using System.Data;
using System.Windows;

namespace PackViewApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show("Unhandled exception: " + ex.ExceptionObject.ToString());
            };

            List<string> products = new List<string>();
            long gidNumber = 0;

            if (e.Args.Length >= 2)
            {
                // Remove outer quotes and split by semicolon
                string rawProducts = e.Args[0].Trim('"');
                products = rawProducts.Split(';').Select(p => p.Trim()).ToList();

                if (long.TryParse(e.Args[1].Trim('"'), out long parsedGid))
                {
                    gidNumber = parsedGid;
                }
            }
            else
            {
                MessageBox.Show("Nie przekazano parametrów");
                return;
            }

            var mainWindow = new MainWindow(products, gidNumber);
            mainWindow.Show();
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}