using System;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace FileLink.Client.Views
{
    public partial class NewButtonPopup : Popup
    {
        public event EventHandler<string> OptionSelected;
        
        public NewButtonPopup()
        {
            InitializeComponent();
        }
        
        private async void UploadFile_Tapped(object sender, EventArgs e)
        {
            // Store the option to trigger after popup closes
            var option = "UploadFile";
            
            // Just close the popup without triggering event immediately
            Close();
            
            // Short delay to ensure popup is fully closed before triggering the event
            await Task.Delay(100);
            
            // Now trigger the event after popup is closed
            OptionSelected?.Invoke(this, option);
        }
        
        private async void CreateFolder_Tapped(object sender, EventArgs e)
        {
            // Store the option to trigger after popup closes
            var option = "CreateFolder";
            
            // Just close the popup without triggering event immediately
            Close();
            
            // Short delay to ensure popup is fully closed before triggering the event
            await Task.Delay(100);
            
            // Now trigger the event after popup is closed
            OptionSelected?.Invoke(this, option);
        }
    }
}