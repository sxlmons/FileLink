using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace FileLink.Client;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        
        // Force the app to use light mode
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}