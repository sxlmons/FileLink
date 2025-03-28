using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileLink.Client.Session;

namespace FileLink.Client.FileOperations;

public class FileSelector : INotifyPropertyChanged
{
    private readonly CloudSession _session;
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
    
    public FileSelector(CloudSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
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
        foreach (var file in Files)
        {
            try
            {
                await _session.FileManager.UploadFileAsync(file.fullPath);
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