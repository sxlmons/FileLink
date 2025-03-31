namespace FileLink.Client;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // config
        UserAppTheme = AppTheme.Light;
        
        // app shell
        MainPage = new AppShell();
    }
}