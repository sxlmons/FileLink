using FileLink.Client.DirectoryNavigation;
using FileLink.Client.FileOperations;
using FileLink.Client.Services;

namespace FileLink.Client;

// This class is used to have all of our view models viewable as data contexts
public class MainViewModel
{ 
    public FileSelector FileSelectorVM { get; set; }
    public DirectoryMap DirectoryVM { get; set; }
    
    // Take services as parameters instead of creating new instances
    public MainViewModel(
        FileService fileService, 
        AuthenticationService authService, 
        DirectoryService directoryService)
    {
        // Initialize with the authenticated services
        FileSelectorVM = new FileSelector(fileService, authService);
        
        // Initialize directory navigation with the right service
        // This will need to be updated to use DirectoryService too
        DirectoryVM = new DirectoryMap(directoryService, authService, fileService);
    }
}