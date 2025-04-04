using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FileLink.Client.Models;
using FileLink.Client.Services;

namespace FileLink.Client.DirectoryNavigation;

/*
    This class will cover the logic that allows the client to navigate the directories they
    have hosted on the server. The problem we ran into was having a reliable navigation method 
    without having prebuilt directory paths to follow inside the client. 

    We came up with storing them in a map that maps the file path to all files inside each directory.
    This allows up to update specific directories without having to rewrite the entire client's directory 
    each time, and all without the client having any of this directory tree built on their device. 

    This summary is to get everyone caught up on client navigation. can be deleting later. 

    UPDATE: 01/04/2025
    What Was Preserved:
    The core concept and public API (Files collection, commands)
    UI interaction patterns and property change notifications
    The ShownFiles class structure

    What Was Significantly Modified:
    Changed from using local storage to server communication
    Added proper service dependencies (DirectoryService, AuthenticationService, FileService)
    Replaced file system operations with API calls

    What Was Added:
    Server-based directory navigation
    ID tracking for files and directories
    Type flag to distinguish files from directories
    Methods to talk to the server API instead of the local file system
*/


public class DirectoryMap : INotifyPropertyChanged
{
    private readonly DirectoryService _directoryService;
    private readonly AuthenticationService _authService;
    private readonly FileService _fileService;
    
    private string? _currentDirectoryId;
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    public ObservableCollection<ShownFiles> Files { get; set; } = new();
    public ObservableCollection<ShownFiles> QueuedFiles { get; set; } = new();
    
    private bool _isRetrieveButtonVisible;
    public bool IsRetrieveButtonVisible
    {
        get => _isRetrieveButtonVisible;
        set
        {
            if (_isRetrieveButtonVisible != value)
            {
                _isRetrieveButtonVisible = value;
                NotifyPropertyChanged();
            }
        }
    }
    
    private bool _isBackButtonVisible;
    public bool IsBackButtonVisible
    {
        get => _isBackButtonVisible;
        set
        {
            if (_isBackButtonVisible != value)
            {
                _isBackButtonVisible = value;
                NotifyPropertyChanged();
            }
        }
    }
    
    public ICommand clickDirectoryCommand { get; }
    public ICommand backButtonCommand { get; }
    public ICommand removeFilesCommand { get; }
    public ICommand retrieveFiles { get; }
    
    public DirectoryMap(DirectoryService directoryService, AuthenticationService authService, FileService fileService)
    {
        _directoryService = directoryService ?? throw new ArgumentNullException(nameof(directoryService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        
        // Start at root directory (null means root)
        _currentDirectoryId = null;
        
        clickDirectoryCommand = new Command<ShownFiles>(directoryClicked);
        backButtonCommand = new Command(clickBackDirectory);
        removeFilesCommand = new Command<ShownFiles>(RemoveFile);
        retrieveFiles = new Command(RetrieveFiles);
        // Load the root directory on startup
        MainThread.BeginInvokeOnMainThread(async () => await LoadCurrentDirectory());
    }
    
    // Load and display contents of current directory
    private async Task LoadCurrentDirectory()
    {
        if (!_authService.IsLoggedIn)
        {
            Console.WriteLine("User is not logged in. Cannot load directory contents.");
            return;
        }
        
        string userId = _authService.CurrentUser?.Id;
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("User ID is missing. Cannot load directory contents.");
            return;
        }
        
        try
        {
            // Clear existing items
            Files.Clear();
            
            // Get contents from the server
            var (files, directories) = await _directoryService.GetDirectoryContentsAsync(_currentDirectoryId, userId);
            
            // Add directories first
            foreach (var dir in directories)
            {
                Files.Add(new ShownFiles
                {
                    fileName = dir.Name,
                    pngType = "folder.png",
                    ItemId = dir.Id,
                    IsDirectory = true
                });
            }
            
            // Then add files
            foreach (var file in files)
            {
                Files.Add(new ShownFiles
                {
                    fileName = file.FileName,
                    pngType = "file.png",
                    ItemId = file.Id,
                    IsDirectory = false
                });
            }
            
            // Update back button visibility
            UpdateBackButtonVisibility();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading directory: {ex.Message}");
        }
    }

    // Same logic as the send files i just dont know the download flow to implemement it 
    // also this along with the majority of the retrieving functions and commands can be moved 
    // but i see a deleting file warning in FileDownloads so i have it here 
    
    private async void RetrieveFiles()
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
        
        foreach (var file in QueuedFiles)
        {
            try
            {
                // Download based of itemId 
                // Remove(file) to remove from queue
                // move it to a predetermined FileLink storage place for the user retrivals 
                // probably just a desktop/download folder we can move the downloaded file to 
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error Sending File: {ex.Message}");
            }
        }
    }
    
    // Handle clicking on a directory
    private async void directoryClicked(ShownFiles file)
    {
        if (file == null) return;
        
        // Only navigate if it's a directory
        if (file.IsDirectory)
        {
            // Store parent directory ID to enable back navigation
            string previousDirectoryId = _currentDirectoryId;
            
            // Set new current directory
            _currentDirectoryId = file.ItemId;
            
            // Load contents of the new directory
            await LoadCurrentDirectory();
        }
        else
        {
            QueuedFiles.Add(file);
            UpdateRetrieveButtonVisibility();
            // Here you could implement file preview or download
            Console.WriteLine($"File clicked: {file.fileName}");
        }
    }
    
    private void RemoveFile(ShownFiles file)
    {
        if (file == null)
            return;
        QueuedFiles.Remove(file);
        UpdateRetrieveButtonVisibility();
    }
    
    private void UpdateRetrieveButtonVisibility()
    {
        IsRetrieveButtonVisible = QueuedFiles.Count > 0; 
    }
    
    // Go back to parent directory
    private async void clickBackDirectory()
    {
        if (_currentDirectoryId == null)
            return; // Already at root
            
        try
        {
            if (!_authService.IsLoggedIn)
            {
                Console.WriteLine("User is not logged in. Cannot navigate.");
                return;
            }
            
            string userId = _authService.CurrentUser?.Id;
            
            // Get current directory to find its parent
            var directory = await _directoryService.GetDirectoryByIdAsync(_currentDirectoryId, userId);
            if (directory == null)
            {
                // Something went wrong, reset to root
                _currentDirectoryId = null;
            }
            else
            {
                // Navigate to parent
                _currentDirectoryId = directory.ParentDirectoryId;
            }
            
            // Load the contents of the parent directory
            await LoadCurrentDirectory();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navigating back: {ex.Message}");
        }
    }
    
    // Update back button visibility
    private void UpdateBackButtonVisibility()
    {
        // Show back button if not at root
        IsBackButtonVisible = _currentDirectoryId != null;
    }
    
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// Updated to include ID and type information
public class ShownFiles
{
    private string _fileName;
    public string fileName
    {
        get => _fileName;   
        set => _fileName = value;
    }

    private string _pngType;
    public string pngType
    {
        get => _pngType;
        set => _pngType = value;
    }
    
    // Add ID to track the server-side ID
    private string _itemId;
    public string ItemId
    {
        get => _itemId;
        set => _itemId = value;
    }
    
    // Flag to distinguish directories from files
    private bool _isDirectory;
    public bool IsDirectory
    {
        get => _isDirectory;
        set => _isDirectory = value;
    }
}