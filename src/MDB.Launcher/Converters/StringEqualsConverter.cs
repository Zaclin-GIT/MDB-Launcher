using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace MDB.Launcher.Converters;

/// <summary>
/// Converter that checks if a string value equals the ConverterParameter.
/// Used for RadioButton IsChecked binding to a string property.
/// Can be used as a markup extension for inline usage.
/// </summary>
public class StringEqualsConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return parameter?.ToString() ?? string.Empty;
        return Binding.DoNothing;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
