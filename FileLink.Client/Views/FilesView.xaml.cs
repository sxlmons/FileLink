using System;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace FileLink.Client.Views
{
    public partial class FilesView : ContentView
    {
        public FilesView()
        {
            InitializeComponent();
        }

        // Forward the search text changed event to the parent page
        private void SearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Get the parent page's binding context and call the search method
            if (BindingContext is MainViewModel viewModel)
            {
                viewModel.DirectoryVM.PerformSearch(e.NewTextValue);
            }
        }
        
        // Show the popup when New button is tapped
        private void NewButton_Tapped(object sender, EventArgs e)
        {
            var popup = new NewButtonPopup();
            popup.OptionSelected += Popup_OptionSelected;
            
            // Find the parent page to show the popup
            var parentPage = Parent;
            while (parentPage != null && !(parentPage is Page))
            {
                parentPage = parentPage.Parent;
            }
            
            if (parentPage is Page page)
            {
                // Set the popup anchor to position it near the button
                popup.Anchor = NewButton;
                
                // Show the popup
                page.ShowPopup(popup);
            }
        }
        
        // Handle popup option selection
        private void Popup_OptionSelected(object sender, string option)
        {
            if (BindingContext is MainViewModel viewModel)
            {
                switch (option)
                {
                    case "UploadFile":
                        viewModel.FileSelectorVM.AddFilesCommand.Execute(null);
                        break;
                    case "CreateFolder":
                        viewModel.DirectoryVM.createDirectory.Execute(null);
                        break;
                }
            }
        }
    }
}