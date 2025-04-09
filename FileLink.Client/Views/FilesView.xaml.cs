using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}