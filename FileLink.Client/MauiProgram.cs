using FileLink.Client.Pages;
using FileLink.Client.Services;
using FileLink.Client.Views;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using CommunityToolkit.Maui;

namespace FileLink.Client
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    
                    // Add Inter font family
                    fonts.AddFont("Inter_18-Regular.ttf", "InterRegular");
                    fonts.AddFont("Inter_18-Medium.ttf", "InterMedium");
                    fonts.AddFont("Inter_18-SemiBold.ttf", "InterSemiBold");
                    fonts.AddFont("Inter_18-Bold.ttf", "InterBold");
                    
                    // SymbolsOk
                    fonts.AddFont("MaterialSymbolsOutlined-Regular.ttf", "MaterialSymbols");
                })
                .UseSkiaSharp();
            
            // Register services as singletons so they maintain state throughout the app
            builder.Services.AddSingleton<NetworkService>();
            builder.Services.AddSingleton<AuthenticationService>(); 
            builder.Services.AddSingleton<FileService>();
            builder.Services.AddSingleton<DirectoryService>();

            // Register pages with transient scope
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<MainPage>();
            
            // Register content views
            builder.Services.AddTransient<FilesView>();
            builder.Services.AddTransient<AccountView>();
            builder.Services.AddTransient<StorageView>();
            builder.Services.AddTransient<SettingsView>();
            builder.Services.AddTransient<HomeView>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}