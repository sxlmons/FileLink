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
}