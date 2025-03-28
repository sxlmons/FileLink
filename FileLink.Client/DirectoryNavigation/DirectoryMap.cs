using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace FileLink.Client.DirectoryNavigation;

/*
    This class will cover the logic that allows the client to navigate the directories they
    have hosted on the server. The problem we ran into was having a reliable navigation method 
    without having prebuilt directory paths to follow inside the client. 
    
    We came up with storing them in a map that maps the file path to all files inside each directory.
    This allows up to update specific directories without having to rewrite the entire client's directory 
    each time, and all without the client having any of this directory tree built on their device. 

    This summary is to get everyone caught up on client navigation. can be deleting later. 
*/

public class DirectoryMap : INotifyPropertyChanged
{
    Dictionary<string, string[]> directoryMap = new();

    private string UserDirectoryPath;
    private string LocalDirectoryPath;
    
    public event PropertyChangedEventHandler PropertyChanged;
    
    public ObservableCollection<ShownFiles> Files { get; set; } = new();
    
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
    
    public ICommand clickDirectoryCommand { get; }
    public ICommand backButtonCommand { get; }
    
    public DirectoryMap()
    {
        // gets the path for the directory where our local directory storage sits
        UserDirectoryPath = Path.Combine(FileSystem.AppDataDirectory, "FileLinkLocalDirectory");
        
        // Ensure the directory exists
        if (!Directory.Exists(UserDirectoryPath))
            Directory.CreateDirectory(UserDirectoryPath);
        
        initClientDirectoryMap(UserDirectoryPath);

        setRootDirectoryOnStartUp();

        clickDirectoryCommand = new Command<ShownFiles>(directoryClicked);
        backButtonCommand = new Command(clickBackDirectory);
    }
    
    // sets the ui to whatever is in the root dir at start up 
    public void setRootDirectoryOnStartUp()
    {
        LocalDirectoryPath = "/root";
        // same process as reload function
        foreach (var file in directoryMap[LocalDirectoryPath])
        {
            var newFile = new ShownFiles();
            if (file.Contains('.'))
                newFile.pngType = "file.png";
            else 
                newFile.pngType = "folder.png";
            newFile.fileName = file;
            Files.Add(newFile);
        }
        
        // TODO find a way to reuse the reload directory function here 
        // TODO currently when you add File.Clear(); it doesnt work because its 
        // TODO not initiated yet 
        // try adding a blank file then clearing it then continuing 
    }

    // called each time we want to refresh the UI and display the current directory contents
    public void reloadDirectoryView()
    {
        // empty the current files
        Files.Clear();
        // access the map with the current local dir path as the key 
        // and go through each of the values in this foreach loop
        foreach (var file in directoryMap[LocalDirectoryPath])
        {
            var newFile = new ShownFiles();
            if (file.Contains('.'))
                newFile.pngType = "file.png";
            else 
                newFile.pngType = "folder.png";
            // make and populate newFile object 
            
            newFile.fileName = file;
            // add it to our ObservableCollection
            Files.Add(newFile);
        }
        // refresh
        UpdateButtonVisibility();
    }
    
    // initiate map at beginning of the program 
    public void initClientDirectoryMap(string folderPath)
    {
        // TODO right now its broken if you start it up for the first time or without a file
        // TODO it should add the root directory to the file and the map and then display 
        // TODO and empty directory 
        
        /*if (!File.Exists(folderPath))
        {
            using (StreamWriter writer = File.CreateText(folderPath))
            {
                writer.WriteLine("/root,");
            }
            directoryMap.Add("/root,", Array.Empty<string>());
            return;
        }*/
        // goes to location that the directory info is locally stored 
        string filePath = Path.Combine(folderPath, "ClientDirectoryStorage.txt");
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            string[] parts;
            while ((line = reader.ReadLine()) != null)
            {
                // split the whole line by the ',' 
                // turns "/root/dir1,file1,file2,file3,"
                // to    "/root/dir" "file1" "file2" "file3"
                parts = line.Split(',');
                
                // add to our map with the directory path as the key 
                // and the rest of the elements as an array of files and directories
                // the servers sends the lines with an extra ',' at the end so the 'parts.Length - 2' accounts for that 
                directoryMap.Add(parts[0], parts.Skip(1).Take(parts.Length - 2).ToArray());
            }
        }
    }

    // called when a dir is clicked 
    private void directoryClicked(ShownFiles file)
    {
        // checks if the 'file' has a dot in it, currently indicating file 
        // TODO find a more secure way to distinguish between files and directories
        if (file.fileName.Contains('.'))
            return;
        // if it's not a file but a directory, take that directories name 
        // add it to the current directory with a '/' in between 
        LocalDirectoryPath +=  Path.DirectorySeparatorChar + file.fileName;
        // reload with edited path
        reloadDirectoryView();
    }

    // the functionality for going back one directory without the actual directory to navigate 
    private void splitPathForBackPressed()
    {
        // counts the amount of slashes in the current local directory path
        int slashCount = LocalDirectoryPath.Count(c => c == '/');
        
        // splits the path by the slashes 
        string[] parts = LocalDirectoryPath.Split('/');
        
        // rejoins the path with all components before the count of the last slash
        // turning /root/dir1/dir2,file1,file2 to 
        //         /root/dir1 
        // taking 3 components: "", "/root", and "/dir1" and combining 
        LocalDirectoryPath = string.Join("/", parts.Take(slashCount));
    }
    
    // command for clicking the back button to go back one directory 
    private void clickBackDirectory()
    {
        splitPathForBackPressed();
        reloadDirectoryView();
    }
    
    // Checks if we are in the base directory and takes away the button if we are so we can back out of it 
    private void UpdateButtonVisibility()
    {
        IsButtonVisible = LocalDirectoryPath != "/root";
    }
    
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

// class that holds any information in the Files var
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
}