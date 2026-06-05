namespace Fcry.App.Services;

public interface IPickerService
{
    Task<IReadOnlyList<string>> PickFilesAsync();
    Task<string?> PickFolderAsync();
    Task<string?> PickOutputFolderAsync();
    Task<string?> PickKeyFileAsync();
}
