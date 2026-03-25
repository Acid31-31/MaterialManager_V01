using System.Windows;
using MaterialManager_V01.Views;

namespace MaterialManager_V01
{
    public partial class MainWindow
    {
        private void OnProgrammHilfe(object sender, RoutedEventArgs e)
        {
            var dlg = new ProgrammHilfeDialog { Owner = this };
            dlg.ShowDialog();
        }
    }
}
