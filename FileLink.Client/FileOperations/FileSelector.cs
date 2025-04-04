using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileLink.Client.Services;
using System.Threading;
using FileLink.Client.DirectoryNavigation;

namespace FileLink.Client.FileOperations;

public class FileSelector : INotifyPropertyChanged
{
    private DirectoryMap _directoryMap;
    private readonly FileService _fileService;
    private readonly AuthenticationService _authService;
    private readonly CancellationTokenSource _cts;
    
    public event PropertyChangedEventHandler PropertyChanged;
    public ObservableCollection<FilesSelected> Files { get; set; } = new();

    private bool _isButtonVisible;
    public bool IsButtonVisible
    {
        get => _isButtonVisible;
        set
        {
            if (_isButtonVisible != value)
            {
                _isButtonVisible = value;
                NotifyPropertyChanged();
            }
        }
    }

    public ICommand AddFilesCommand { get; }
    public ICommand RemoveFilesCommand { get; }
    public ICommand SendFilesCommand { get; }
    
    public FileSelector(FileService fileService, AuthenticationService authService, DirectoryService directoryService, DirectoryMap directoryMap)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _directoryMap = directoryMap ?? throw new ArgumentNullException(nameof(directoryMap));
        _cts = new CancellationTokenSource();
        
        AddFilesCommand = new Command(async () => await AddFile());
        RemoveFilesCommand = new Command<FilesSelected>(RemoveFile);
        SendFilesCommand = new Command(async () => await SendFiles());
    }

    // Add files to file upload queue on button click
    private async Task AddFile()
    {
        var filesPicked = await MainThread.InvokeOnMainThreadAsync(async () =>
            await FilePicker.PickMultipleAsync());
        
        if (filesPicked != null)
        {
            foreach (var file in filesPicked)
            {
                var newFile = new FilesSelected();
                newFile.fileName = file.FileName;
                newFile.fullPath = file.FullPath;
                Files.Add(newFile);
            }
        }

        UpdateButtonVisibility();
    }

    // Remove files from file upload queue on button click
    private void RemoveFile(FilesSelected file)
    {
        if (file == null)
            return;
        Files.Remove(file);
        UpdateButtonVisibility();
    }
    
    public async Task SendFiles()
    {
        // Verify authentication
        if (!_authService.IsLoggedIn)
        {
            Console.WriteLine("Error Sending File: Not authenticated. Please log in first.");
            return;
        }
        
        string userId = _authService.CurrentUser?.Id;
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("Error Sending File: User ID is missing.");
            return;
        }
        
        foreach (var file in Files)
        {
            try
            {
                var result = await _fileService.UploadFileAsync(file.fullPath, _directoryMap._currentDirectoryId, userId);
                if (result != null)
                {
                    // This should remove the files as there sent but theres a bug where the click to send only send 1
                    // it works whenever you send the file those so its low priority 
                    RemoveFile(file);
                    Console.WriteLine($"File uploaded successfully: {file.fileName}");
                    await _directoryMap.LoadCurrentDirectory();
                }
                else
                {
                    Console.WriteLine($"Failed to upload file: {file.fileName}");
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Sending File: {ex.Message}");
            }
        }
    }
    
    // Updates the boolean based on Files count
    private void UpdateButtonVisibility()
    {
        IsButtonVisible = Files.Count > 0; 
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class FilesSelected
{
    private string _fileName;
    public string fileName
    {
        get => _fileName;
        set
        {
            if (_fileName != value)
                _fileName = value;
        }
    }
    
    private string _fullPath;
    public string fullPath
    {
        get => _fullPath;
        set
        {
            if (_fullPath != value)  
                _fullPath = value;
        }
    }
}