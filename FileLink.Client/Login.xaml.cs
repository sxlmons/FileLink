using System.ComponentModel;
using System.Windows.Input;
using FileLink.Client.Session;
using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace FileLink.Client
{
    public partial class Login : ContentPage
    {
        public Login(AuthenticationManager authManager)
        {
            InitializeComponent();
            BindingContext = new LoginViewModel(authManager);
        }
    }
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username;
        private string _password;
        private string _email;
        private readonly AuthenticationManager _authManager;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged(nameof(Username));
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }
        
        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(AuthenticationManager authManager)
        {
            _authManager = authManager ?? throw new ArgumentNullException(nameof(authManager));
            LoginCommand = new Command(async () => await OnLogin());
        }

        private async Task OnLogin()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "Username and password are required", "OK");
                return;
            }

            var (success, message) = await _authManager.LoginAsync(Username, Password);

            if (success)
            {
                // Navigate to MainPage after successful login
                await Application.Current.MainPage.DisplayAlert("Good", "Logged in", "OK");
                App.Current.MainPage = new NavigationPage(new MainPage());
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Login Failed", message, "OK");
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}