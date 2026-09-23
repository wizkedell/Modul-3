using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Modul_3.Converters
{
    public class BooleanToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
                return new SolidColorBrush(Colors.Orange);
            else
                return new SolidColorBrush(Colors.Green);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }    

    public class SelectedMarkerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Временно возвращаем стили по условию
            if (value is bool isSelected && isSelected)
            {
                // Стиль для выбранного маркера
                return new Style(typeof(System.Windows.Shapes.Ellipse))
                {
                    Setters =
                    {
                        new Setter(System.Windows.Shapes.Ellipse.WidthProperty, 30.0),
                        new Setter(System.Windows.Shapes.Ellipse.HeightProperty, 30.0),
                        new Setter(System.Windows.Shapes.Ellipse.FillProperty, new SolidColorBrush(Colors.Orange)),
                        new Setter(System.Windows.Shapes.Ellipse.StrokeProperty, new SolidColorBrush(Colors.Red)),
                        new Setter(System.Windows.Shapes.Ellipse.StrokeThicknessProperty, 3.0),
                       // new Setter(System.Windows.Shapes.Ellipse.OpacityProperty, 0.1),
                        new Setter(System.Windows.Shapes.Ellipse.CursorProperty, System.Windows.Input.Cursors.Hand)
                    }
                };
            }
            else
            {
                // Стиль для обычного маркера
                return new Style(typeof(System.Windows.Shapes.Ellipse))
                {
                    Setters =
                    {
                        new Setter(System.Windows.Shapes.Ellipse.WidthProperty, 30.0),
                        new Setter(System.Windows.Shapes.Ellipse.HeightProperty, 30.0),
                        new Setter(System.Windows.Shapes.Ellipse.FillProperty, new SolidColorBrush(Colors.Green)),
                        new Setter(System.Windows.Shapes.Ellipse.StrokeProperty, new SolidColorBrush(Colors.White)),
                        new Setter(System.Windows.Shapes.Ellipse.StrokeThicknessProperty, 2.0),
                       // new Setter(System.Windows.Shapes.Ellipse.OpacityProperty, 0.1),
                        new Setter(System.Windows.Shapes.Ellipse.CursorProperty, System.Windows.Input.Cursors.Hand)
                    }
                };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
