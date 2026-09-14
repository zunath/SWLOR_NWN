using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SWLOR.Toolset.Shell.Views
{
    /// <summary>
    /// Maps "is an error" to the severity colour. Errors use a red that is not otherwise in the
    /// toolset palette; warnings reuse AmberBrush, which already means "changed / needs attention"
    /// everywhere else in the app.
    /// </summary>
    public sealed class SeverityBrushConverter : IValueConverter
    {
        private static readonly IBrush Error = new SolidColorBrush(Color.Parse("#E05A5A"));
        private static readonly IBrush Warning = new SolidColorBrush(Color.Parse("#D9A155"));

        public static SeverityBrushConverter Instance { get; } = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is true ? Error : Warning;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
