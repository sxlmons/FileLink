using FileLink.Client.FileOperations;
using FileLink.Client.Session;

namespace FileLink.Client;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        // Creates an instance to manage the file stack when uploading files 
        BindingContext = new MainViewModel();
    }
}

// This is used to have all of our view models viewable as data contexts 
public class MainViewModel
{
    public FileSelector FileVM { get; set; }
    
    CloudSession _session = new CloudSession("localhost", 9000);

    public MainViewModel()
    {
        FileVM = new FileSelector(_session);
    }
}
