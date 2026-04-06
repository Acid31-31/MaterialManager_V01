using GeoArbeitsvorbereitung.Dialogs;
using Microsoft.Win32;

namespace GeoArbeitsvorbereitung.Services;

public class WpfDialogService : IDialogService
{
    public string? BrowseFolder(string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false,
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public string? ShowTargetExistsDialog(string targetFolder, string suggestedFileName)
    {
        var dlg = new TargetExistsDialog(targetFolder, suggestedFileName)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        return dlg.ShowDialog() == true ? dlg.ResultFileName : null;
    }
}
