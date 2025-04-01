using FileLink.Client.Services;

namespace FileLink.Client.Pages
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthenticationService _authService;
        private readonly NetworkService _networkService;
        
        public LoginPage(AuthenticationService authService, NetworkService networkService)
        {
            InitializeComponent();
            _authService = authService;
            _networkService = networkService;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Clear any previous credentials
            UsernameEntry.Text = "";
            PasswordEntry.Text = "";
            NewUsernameEntry.Text = "";
            NewPasswordEntry.Text = "";
            EmailEntry.Text = "";
            
            // Make sure we're showing the login panel
            LoginPanel.IsVisible = true;
            CreateAccountPanel.IsVisible = false;
        }

        private async void LoginButton_Clicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text?.Trim() ?? "";
            string password = PasswordEntry.Text ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Sign In Failed", "Please enter both username and password", "OK");
                return;
            }

            // Show loading indicator
            LoadingOverlay.IsVisible = true;
            ActivitySpinner.IsRunning = true;
            StatusLabel.Text = "Signing in...";

            try
            {
                var (success, message) = await _authService.LoginAsync(username, password);

                if (success)
                {
                    // Navigate to main page
                    await Shell.Current.GoToAsync("//MainPage");
                }
                else
                {
                    // Show error message
                    await DisplayAlert("Sign In Failed", message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                // Hide loading indicator
                LoadingOverlay.IsVisible = false;
                ActivitySpinner.IsRunning = false;
            }
        }

        private void CreateAccountButton_Clicked(object sender, EventArgs e)
        {
            // Switch to registration panel
            LoginPanel.IsVisible = false;
            CreateAccountPanel.IsVisible = true;
        }

        private void CancelButton_Clicked(object sender, EventArgs e)
        {
            // Switch back to  panel
            LoginPanel.IsVisible = true;
            CreateAccountPanel.IsVisible = false;
        }

        private async void SubmitAccountButton_Clicked(object sender, EventArgs e)
        {
            string username = NewUsernameEntry.Text?.Trim() ?? "";
            string password = NewPasswordEntry.Text ?? "";
            string email = EmailEntry.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Account Creation Failed", "Please enter both username and password", "OK");
                return;
            }

            if (TermsCheckBox.IsChecked == false)
            {
                await DisplayAlert("Terms & Conditions", "Please agree to the Terms & Conditions to continue", "OK");
                return;
            }

            // Show loading indicator
            LoadingOverlay.IsVisible = true;
            ActivitySpinner.IsRunning = true;
            StatusLabel.Text = "Creating account...";

            try
            {
                var (success, message, userId) = await _authService.CreateAccountAsync(username, password, email);

                if (success)
                {
                    // Account created successfully
                    await DisplayAlert("Success", "Your account has been created successfully", "OK");
                    
                    // Switch back to  panel
                    LoginPanel.IsVisible = true;
                    CreateAccountPanel.IsVisible = false;
                    
                    // Pre-fill login fields with the new account info
                    UsernameEntry.Text = username;
                    PasswordEntry.Text = password;
                }
                else
                {
                    // Show error message
                    await DisplayAlert("Account Creation Failed", message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                // Hide loading indicator
                LoadingOverlay.IsVisible = false;
                ActivitySpinner.IsRunning = false;
            }
        }

        private async void SaveSettingsButton_Clicked(object sender, EventArgs e)
        {
            string serverAddress = ServerAddressEntry.Text?.Trim() ?? "localhost";
            string portText = ServerPortEntry.Text?.Trim() ?? "9000";

            if (string.IsNullOrEmpty(serverAddress))
            {
                serverAddress = "localhost";
            }

            if (!int.TryParse(portText, out int port) || port <= 0 || port > 65535)
            {
                port = 9000;
            }

            // Save the server settings
            _networkService.SetServer(serverAddress, port);

            // Update the UI
            ServerAddressEntry.Text = serverAddress;
            ServerPortEntry.Text = port.ToString();

            // Show confirmation
            LoadingOverlay.IsVisible = true;
            ActivitySpinner.IsRunning = true;
            StatusLabel.Text = "Settings saved";
            
            // Auto-hide after a delay
            await Task.Delay(1500);
            
            // Hide loading overlay and settings panel
            LoadingOverlay.IsVisible = false;
            ActivitySpinner.IsRunning = false;
            ServerSettingsPanel.IsVisible = false;
        }
        
        private void ToggleSettingsButton_Clicked(object sender, EventArgs e)
        {
            // Toggle the visibility of the settings panel
            ServerSettingsPanel.IsVisible = !ServerSettingsPanel.IsVisible;
        }
        
        private void TogglePassword_Tapped(object sender, EventArgs e)
        {
            // Toggle password visibility
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        }
        
        private void ToggleNewPassword_Tapped(object sender, EventArgs e)
        {
            // Toggle password visibility for registration form
            NewPasswordEntry.IsPassword = !NewPasswordEntry.IsPassword;
        }
    }
}