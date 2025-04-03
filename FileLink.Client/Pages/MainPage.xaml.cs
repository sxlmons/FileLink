using FileLink.Client.Services;

namespace FileLink.Client.Pages;

public partial class MainPage : ContentPage
{
    private readonly AuthenticationService _authService;
    private readonly NetworkService _networkService;
    private readonly FileService _fileService;
    private readonly DirectoryService _directoryService;
    
    public MainPage(
        AuthenticationService authService, 
        NetworkService networkService, 
        FileService fileService,
        DirectoryService directoryService)
    {
        InitializeComponent();
        
        _authService = authService;
        _networkService = networkService;
        _fileService = fileService;
        _directoryService = directoryService;
        
        // Create view model with the authenticated services
        BindingContext = new MainViewModel(_fileService, _authService, _directoryService);
    }
    
    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Check if user is logged in
        if (!_authService.IsLoggedIn)
        {
            // If not logged in, redirect to login page
            Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        // Update UI with current user's information
        UserInfoLabel.Text = $"User: {_authService.CurrentUser?.Username}";
    }
    
    private async void LogoutButton_Clicked(object sender, EventArgs e)
    {
        // Show loading indicator
        ActivitySpinner.IsVisible = true;
        ActivitySpinner.IsRunning = true;
        StatusLabel.Text = "Logging out...";
        StatusLabel.IsVisible = true;
        LogoutButton.IsEnabled = false;

        try
        {
            var (success, message) = await _authService.LogoutAsync();

            if (success)
            {
                // Navigate back to login page
                await Shell.Current.GoToAsync("//LoginPage");
            }
            else
            {
                // Show error message
                await DisplayAlert("Logout Failed", message, "OK");
                    
                // Hide loading indicator
                ActivitySpinner.IsVisible = false;
                ActivitySpinner.IsRunning = false;
                StatusLabel.IsVisible = false;
                LogoutButton.IsEnabled = true;
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
                
            // Hide loading indicator
            ActivitySpinner.IsVisible = false;
            ActivitySpinner.IsRunning = false;
            StatusLabel.IsVisible = false;
            LogoutButton.IsEnabled = true;
        }
    }
}