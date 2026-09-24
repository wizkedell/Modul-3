using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System;

namespace Modul_3.Views
{
    public partial class MemorySegmentControl : UserControl
    {
        public MemorySegmentControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register("IsActive", typeof(bool), typeof(MemorySegmentControl),
            new PropertyMetadata(false, OnIsActiveChanged));

        public static readonly DependencyProperty ActivatedSegmentsProperty =
            DependencyProperty.Register("ActivatedSegments", typeof(int), typeof(MemorySegmentControl),
            new PropertyMetadata(0, OnSegmentsChanged));

        public static readonly DependencyProperty TotalSegmentsProperty =
            DependencyProperty.Register("TotalSegments", typeof(int), typeof(MemorySegmentControl),
            new PropertyMetadata(1, OnSegmentsChanged));

        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register("Diameter", typeof(double), typeof(MemorySegmentControl),
            new PropertyMetadata(30.0, OnDiameterChanged));

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public int ActivatedSegments
        {
            get { return (int)GetValue(ActivatedSegmentsProperty); }
            set { SetValue(ActivatedSegmentsProperty, value); }
        }

        public int TotalSegments
        {
            get { return (int)GetValue(TotalSegmentsProperty); }
            set { SetValue(TotalSegmentsProperty, value); }
        }

        public double Diameter
        {
            get { return (double)GetValue(DiameterProperty); }
            set { SetValue(DiameterProperty, value); }
        }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MemorySegmentControl)d;
            control.UpdateInnerCircle();
        }

        private static void OnSegmentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MemorySegmentControl)d;
            control.DrawSegments();
        }

        private static void OnDiameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (MemorySegmentControl)d;
            control.UpdateSize();
        }

        private void UpdateSize()
        {
            this.Width = Diameter;
            this.Height = Diameter;
            DrawSegments();
            UpdateInnerCircle();
        }

        private void UpdateInnerCircle()
        {
            if (InnerCircle == null) return;

            InnerCircle.Width = Diameter * 0.6;
            InnerCircle.Height = Diameter * 0.6;
            InnerCircleBrush.Color = IsActive ? Colors.Green : Color.FromRgb(249,102,102);
        }

        private void DrawSegments()
        {
            if (SegmentsCanvas == null) return;

            SegmentsCanvas.Children.Clear();
            SegmentsCanvas.Width = Diameter;
            SegmentsCanvas.Height = Diameter;

            if (TotalSegments <= 0) return;

            double centerX = Diameter / 2;
            double centerY = Diameter / 2;

            // Для одиночного контакта рисуем полную окружность
            if (TotalSegments == 1)
            {
                var circle = new Ellipse
                {
                    Width = Diameter,
                    Height = Diameter,
                    Stroke = ActivatedSegments > 0 ? Brushes.Green : new SolidColorBrush(Color.FromRgb(249,102,102)),
                    StrokeThickness = 15,
                    Fill = Brushes.Transparent
                };

                Canvas.SetLeft(circle, 0);
                Canvas.SetTop(circle, 0);
                SegmentsCanvas.Children.Add(circle);
                return;
            }

            // Для нескольких сегментов - сегментированное отображение
            double outerRadius = Diameter / 2;
            double innerRadius = outerRadius * 0.5;

            for (int i = 0; i < TotalSegments; i++)
            {
                double startAngle = 360.0 / TotalSegments * i;
                double endAngle = 360.0 / TotalSegments * (i + 1);

                bool isSegmentActive = i < ActivatedSegments;

                var segmentPath = new Path
                {
                    Fill = isSegmentActive ? Brushes.Green : new SolidColorBrush(Color.FromRgb(249, 102, 102)),
                    //Stroke = isSegmentActive ? Brushes.DarkGreen : Brushes.Gray,
                    StrokeThickness = 1
                };

                var geometry = new PathGeometry();
                var figure = new PathFigure();

                // Начальная точка на внутреннем радиусе
                figure.StartPoint = new Point(
                    centerX + innerRadius * Math.Cos(startAngle * Math.PI / 180),
                    centerY + innerRadius * Math.Sin(startAngle * Math.PI / 180));

                // Дуга по внутреннему радиусу
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(
                        centerX + innerRadius * Math.Cos(endAngle * Math.PI / 180),
                        centerY + innerRadius * Math.Sin(endAngle * Math.PI / 180)),
                    Size = new Size(innerRadius, innerRadius),
                    SweepDirection = SweepDirection.Clockwise,
                    IsLargeArc = false
                });

                // Линия к внешнему радиусу
                figure.Segments.Add(new LineSegment
                {
                    Point = new Point(
                        centerX + outerRadius * Math.Cos(endAngle * Math.PI / 180),
                        centerY + outerRadius * Math.Sin(endAngle * Math.PI / 180))
                });

                // Дуга по внешнему радиусу (обратно)
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(
                        centerX + outerRadius * Math.Cos(startAngle * Math.PI / 180),
                        centerY + outerRadius * Math.Sin(startAngle * Math.PI / 180)),
                    Size = new Size(outerRadius, outerRadius),
                    SweepDirection = SweepDirection.Counterclockwise,
                    IsLargeArc = false
                });

                figure.IsClosed = true;
                geometry.Figures.Add(figure);
                segmentPath.Data = geometry;

                SegmentsCanvas.Children.Add(segmentPath);
            }
        }
    }
}