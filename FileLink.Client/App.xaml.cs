using FileLink.Client.Connection;
using FileLink.Client.Session;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace FileLink.Client;

public partial class App : Application
{
    
    private readonly CloudServerConnection _serverConnection;
    
    public App()
    {
        InitializeComponent();
        
        // Force the app to use light mode
        UserAppTheme = AppTheme.Light;
        
        // Initialize and connect to the server
        _serverConnection = new CloudServerConnection("localhost", 9000);
        InitializeServerConnection();
    }
    
    private async void InitializeServerConnection()
    {
        try
        {
            await _serverConnection.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server connection failed: {ex.Message}");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}