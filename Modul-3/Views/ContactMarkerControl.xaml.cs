using Modul_3.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Modul_3.Views
{
    public partial class ContactMarkerControl : UserControl
    {
        private bool _isDragging = false;
        private bool _isResizing = false;
        private Point _dragStartPoint;
        private Canvas _parentCanvas;

        public ContactMarkerControl()
        {
            InitializeComponent();

            this.Loaded += OnLoaded;
            this.MouseLeftButtonDown += OnMouseLeftButtonDown;
            this.MouseLeftButtonUp += OnMouseLeftButtonUp;
            this.MouseMove += OnMouseMove;
            this.MouseWheel += OnMouseWheel;

            // Обработчики для изменения размера
            ResizeHandle.MouseLeftButtonDown += ResizeHandle_MouseLeftButtonDown;
            ResizeHandle.MouseLeftButtonUp += ResizeHandle_MouseLeftButtonUp;
            ResizeHandle.MouseMove += ResizeHandle_MouseMove;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Получаем родительский Canvas после загрузки
            _parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
            UpdatePositionFromDataContext();
        }

        private void UpdatePositionFromDataContext()
        {
            if (DataContext is ContactMarker marker && _parentCanvas != null)
            {
                // Обновляем позицию на основе данных из ViewModel
                Canvas.SetLeft(this, marker.RelativeX * _parentCanvas.ActualWidth - this.Width / 2);
                Canvas.SetTop(this, marker.RelativeY * _parentCanvas.ActualHeight - this.Height / 2);

                // Позиционируем resize handle
                Canvas.SetLeft(ResizeHandle, this.Width - ResizeHandle.Width);
                Canvas.SetTop(ResizeHandle, this.Height - ResizeHandle.Height);
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == DataContextProperty)
            {
                if (e.OldValue is ContactMarker oldMarker)
                {
                    oldMarker.PropertyChanged -= Marker_PropertyChanged;
                }

                if (e.NewValue is ContactMarker newMarker)
                {
                    newMarker.PropertyChanged += Marker_PropertyChanged;
                    UpdatePositionFromDataContext();
                }
            }
        }

        private void Marker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContactMarker.RelativeX) ||
                e.PropertyName == nameof(ContactMarker.RelativeY) ||
                e.PropertyName == nameof(ContactMarker.Diameter))
            {
                UpdatePositionFromDataContext();
            }
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ContactMarker marker)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(_parentCanvas);

                this.CaptureMouse();
                e.Handled = true;

                marker.IsSelected = true;

                System.Diagnostics.Debug.WriteLine($"Начато перетаскивание маркера {marker.ContactNumber} с позиции ({marker.RelativeX}, {marker.RelativeY})");
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed && this.IsMouseCaptured)
            {
                if (DataContext is ContactMarker marker && _parentCanvas != null && _parentCanvas.ActualWidth > 0 && _parentCanvas.ActualHeight > 0)
                {
                    Point currentPosition = e.GetPosition(_parentCanvas);

                    // Вычисляем относительные координаты
                    double relativeX = currentPosition.X / _parentCanvas.ActualWidth;
                    double relativeY = currentPosition.Y / _parentCanvas.ActualHeight;

                    // Ограничиваем в пределах [0, 1]
                    relativeX = Math.Max(0, Math.Min(1, relativeX));
                    relativeY = Math.Max(0, Math.Min(1, relativeY));

                    // Обновляем модель
                    marker.RelativeX = relativeX;
                    marker.RelativeY = relativeY;

                    // Немедленно обновляем позицию
                    UpdatePositionFromDataContext();

                    System.Diagnostics.Debug.WriteLine($"Маркер {marker.ContactNumber} перемещен: Relative({relativeX:F3}, {relativeY:F3})");
                }
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                this.ReleaseMouseCapture();
                e.Handled = true;

                if (DataContext is ContactMarker marker)
                {
                    marker.IsSelected = false;
                    System.Diagnostics.Debug.WriteLine($"Перетаскивание завершено. Финальная позиция: ({marker.RelativeX}, {marker.RelativeY})");
                }
            }

            if (_isResizing)
            {
                _isResizing = false;
                ResizeHandle.ReleaseMouseCapture();
                e.Handled = true;
            }

        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is ContactMarker marker && marker.IsSelected)
            {
                double delta = e.Delta > 0 ? 2 : -2;
                marker.Diameter = Math.Max(10, Math.Min(100, marker.Diameter + delta));
                e.Handled = true;

                System.Diagnostics.Debug.WriteLine($"Размер маркера {marker.ContactNumber} изменен: {marker.Diameter}");
            }
        }

        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing = true;
            ResizeHandle.CaptureMouse();
            e.Handled = true;

            System.Diagnostics.Debug.WriteLine("Начато изменение размера маркера");
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isResizing && e.LeftButton == MouseButtonState.Pressed && ResizeHandle.IsMouseCaptured)
            {
                if (DataContext is ContactMarker marker)
                {
                    Point delta = e.GetPosition(this);
                    double newSize = Math.Max(10, Math.Min(100, marker.Diameter + delta.X));
                    marker.Diameter = newSize;
                    e.Handled = true;

                    System.Diagnostics.Debug.WriteLine($"Размер маркера {marker.ContactNumber} изменен через маркер: {marker.Diameter}");
                }
            }
        }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            ResizeHandle.ReleaseMouseCapture();
            e.Handled = true;

            System.Diagnostics.Debug.WriteLine("Изменение размера маркера завершено");
        }
    }
}