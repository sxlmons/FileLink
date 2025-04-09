using System.Globalization;

namespace FileLink.Client.Converters
{
    // Converts integers to progress (0.0 to 1.0) based on total
    public class ProgressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int current && parameter is int total && total > 0)
                return (double)current / total;
            
            if (value is int currentValue)
            {
                if (parameter is BindableObject bindable &&
                    bindable.BindingContext is DirectoryNavigation.DirectoryMap directoryMap)
                {
                    int totalValue = directoryMap.TotalDownloadProgress;
                    if (totalValue > 0)
                        return (double)currentValue / totalValue;
                }
            }
            
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Inverts boolean values
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return !boolValue;
            
            return false;
        }
    }
    
    // Display file size or "Folder" for directories
    public class FileSizeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // ShownFiles object passed directly
            if (value is DirectoryNavigation.ShownFiles file)
            {
                // For directories, show "Folder"
                if (file.IsDirectory)
                    return "Folder";
                
                // For files, return the formatted size if available
                if (!string.IsNullOrEmpty(file.FormattedSize))
                    return file.FormattedSize;
                
                return "—"; // Em dash for unknown size
            }
            
            return "—"; // Em dash for unknown
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    // NEW: Convert boolean (IsDirectory) to background color
    public class BoolToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDirectory && isDirectory)
                return Color.FromArgb("#F5F5FF"); // Light blue for directories
            
            return Colors.White; // White for files
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}