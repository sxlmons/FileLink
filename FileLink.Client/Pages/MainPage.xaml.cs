using FileLink.Client.Services;
using FileLink.Client.Views;
using Microsoft.Maui.Graphics;

namespace FileLink.Client.Pages;

public partial class MainPage : ContentPage
{
    private readonly AuthenticationService _authService;
    private readonly NetworkService _networkService;
    private readonly FileService _fileService;
    private readonly DirectoryService _directoryService;
    
    // Enum to track the current navigation section
    public enum NavigationSection
    {
        Home,
        Files,
        Account,
        Storage,
        Settings
    }
    
    // Keep track of the current section
    private NavigationSection _currentSection = NavigationSection.Files;
    
    // Colors for selected and unselected states
    private readonly Color _selectedBackgroundColor = Color.FromArgb("#8175B5");
    private readonly Color _unselectedBackgroundColor = Colors.Transparent;
    private readonly Color _textColor = Colors.White;
    
    public MainPage(AuthenticationService authService, NetworkService networkService, FileService fileService, DirectoryService directoryService)
    {
        InitializeComponent();
        
        _authService = authService;
        _networkService = networkService;
        _fileService = fileService;
        _directoryService = directoryService;
        
        // Create view model with the authenticated services
        BindingContext = new MainViewModel(_fileService, _authService, _directoryService);
        
        // Set the binding context for each content view
        FilesContentView.BindingContext = BindingContext;
        AccountContentView.BindingContext = BindingContext;
        StorageContentView.BindingContext = BindingContext;
        HomeContentView.BindingContext = BindingContext;
        SettingsContentView.BindingContext = BindingContext;
        
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
        UserInfoLabel.Text = _authService.CurrentUser?.Username ?? "User";
        
        // Ensure we show the correct section when returning to this page
        NavigateTo(_currentSection);
    }
    
    // Navigation method to switch between content views
    private void NavigateTo(NavigationSection section)
    {
        // Hide all views first
        FilesContentView.IsVisible = false;
        AccountContentView.IsVisible = false;
        StorageContentView.IsVisible = false;
        HomeContentView.IsVisible = false;
        SettingsContentView.IsVisible = false;
        
        // Save the current section
        _currentSection = section;
        
        // Highlight the selected navigation button
        UpdateNavigationButtons(section);
        
        // Show selected view
        switch (section)
        {
            case NavigationSection.Home:
                HomeContentView.IsVisible = true;
                break;
            case NavigationSection.Files:
                FilesContentView.IsVisible = true;
                break;
            case NavigationSection.Account:
                AccountContentView.IsVisible = true;
                break;
            case NavigationSection.Storage:
                StorageContentView.IsVisible = true;
                break;
            case NavigationSection.Settings:
                SettingsContentView.IsVisible = true;
                break;
        }
    }
    
    // Update the visual state of navigation buttons
    private void UpdateNavigationButtons(NavigationSection section)
    {
        // Reset all buttons to default state
        HomeButton.BackgroundColor = _unselectedBackgroundColor;
        MyCloudButton.BackgroundColor = _unselectedBackgroundColor;
        StorageButton.BackgroundColor = _unselectedBackgroundColor;
        AccountButton.BackgroundColor = _unselectedBackgroundColor;
        SettingsButton.BackgroundColor = _unselectedBackgroundColor;
        
        // Highlight the selected button
        switch (section)
        {
            case NavigationSection.Home:
                HomeButton.BackgroundColor = _selectedBackgroundColor;
                break;
            case NavigationSection.Files:
                MyCloudButton.BackgroundColor = _selectedBackgroundColor;
                break;
            case NavigationSection.Account:
                AccountButton.BackgroundColor = _selectedBackgroundColor;
                break;
            case NavigationSection.Storage:
                StorageButton.BackgroundColor = _selectedBackgroundColor;
                break;
            case NavigationSection.Settings:
                SettingsButton.BackgroundColor = _selectedBackgroundColor;
                break;
        }
    }
    
    // Navigation button click handlers
    private void HomeButton_Clicked(object sender, EventArgs e)
    {
        NavigateTo(NavigationSection.Home);
    }
    
    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        NavigateTo(NavigationSection.Settings);
    }
    
    private void MyCloudButton_Clicked(object sender, EventArgs e)
    {
        NavigateTo(NavigationSection.Files);
    }
    
    private void AccountButton_Clicked(object sender, EventArgs e)
    {
        NavigateTo(NavigationSection.Account);
    }
    
    private void StorageButton_Clicked(object sender, EventArgs e)
    {
        NavigateTo(NavigationSection.Storage);
    }
    
    // Logout functionality
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
    
    // Search bar handler - forwarded from FilesView
    public void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Get the ViewModel and call the search method
        if (BindingContext is MainViewModel viewModel)
        {
            // Use the search text from the search bar
            viewModel.DirectoryVM.PerformSearch(e.NewTextValue);
        }
    }
}