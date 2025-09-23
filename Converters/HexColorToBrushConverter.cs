using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RadialMenu.Converters
{
    public class HexColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
            {
                try
                {
                    // Remove # if present and ensure proper format
                    var cleanHex = hexColor.Replace("#", "");
                    if (cleanHex.Length == 6)
                        cleanHex = "FF" + cleanHex; // Add alpha channel
                    
                    if (cleanHex.Length == 8)
                    {
                        var a = System.Convert.ToByte(cleanHex.Substring(0, 2), 16);
                        var r = System.Convert.ToByte(cleanHex.Substring(2, 2), 16);
                        var g = System.Convert.ToByte(cleanHex.Substring(4, 2), 16);
                        var b = System.Convert.ToByte(cleanHex.Substring(6, 2), 16);
                        
                        return new SolidColorBrush(Color.FromArgb(a, r, g, b));
                    }
                }
                catch
                {
                    // Fall through to default
                }
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}