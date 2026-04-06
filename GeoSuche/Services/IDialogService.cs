namespace GeoArbeitsvorbereitung.Services;

public interface IDialogService
{
    /// <summary>Opens a folder browser and returns the selected path, or null if cancelled.</summary>
    string? BrowseFolder(string title);

    /// <summary>
    /// Shows the "target file already exists" dialog.
    /// Returns the (possibly edited) filename the user confirmed, or null if cancelled.
    /// </summary>
    string? ShowTargetExistsDialog(string targetFolder, string suggestedFileName);
}
