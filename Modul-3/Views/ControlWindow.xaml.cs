using Modul_3.Models;
using Modul_3.Services;
using Modul_3.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Modul_3.Views
{

    public partial class ControlWindow : Window
    {
        private ControlViewModel ViewModel => (ControlViewModel)DataContext;
        private Dictionary<ContactMarker, MemorySegmentControl> _markerControls = new Dictionary<ContactMarker, MemorySegmentControl>();

        // Фиксированные размеры изображения
        private const double ImageWidth = 800;
        private const double ImageHeight = 600;

        public ControlWindow(Product product, string productNumber, string operatorName,
                     ArduinoService arduinoService, TestProgress savedProgress = null)
        {
            InitializeComponent();

            var viewModel = new ControlViewModel(product, productNumber, operatorName,
                                  arduinoService, savedProgress);
            DataContext = viewModel;

            viewModel.ImageSize = new Size(ImageWidth, ImageHeight);

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.Markers.CollectionChanged += Markers_CollectionChanged;

            // Убедимся, что маркеры обновляются при загрузке окна
            Loaded += ControlWindow_Loaded;
        }

        private void ControlWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Принудительно обновляем маркеры после загрузки окна
            UpdateMarkers();
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ControlViewModel.SelectedConnector))
            {
                UpdateMarkers();
            }
        }

        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            try
            {
                MarkersCanvas.Children.Clear();
                _markerControls.Clear();

                if (ViewModel?.Markers == null) return;

                foreach (var marker in ViewModel.Markers)
                {
                    var markerControl = new MemorySegmentControl();

                    // Привязки свойств
                    markerControl.SetBinding(MemorySegmentControl.IsActiveProperty, "IsActive");
                    markerControl.SetBinding(MemorySegmentControl.ActivatedSegmentsProperty, "ActivatedSegments");
                    markerControl.SetBinding(MemorySegmentControl.TotalSegmentsProperty, "TotalSegments");
                    markerControl.SetBinding(MemorySegmentControl.DiameterProperty, "Diameter");

                    markerControl.DataContext = marker;

                    UpdateMarkerPosition(marker, markerControl);

                    MarkersCanvas.Children.Add(markerControl);
                    _markerControls[marker] = markerControl;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении маркеров: {ex.Message}");
            }
        }

        private void UpdateMarkerPosition(ContactMarker marker, MemorySegmentControl control)
        {
            if (marker == null || control == null) return;

            // Используем диаметр из маркера для позиционирования
            double absoluteX = (marker.RelativeX * ImageWidth) - (marker.Diameter / 2);
            double absoluteY = (marker.RelativeY * ImageHeight) - (marker.Diameter / 2);

            // Ограничиваем в пределах Canvas
            absoluteX = Math.Max(0, Math.Min(ImageWidth - marker.Diameter, absoluteX));
            absoluteY = Math.Max(0, Math.Min(ImageHeight - marker.Diameter, absoluteY));

            Canvas.SetLeft(control, absoluteX);
            Canvas.SetTop(control, absoluteY);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.Markers.CollectionChanged -= Markers_CollectionChanged;
                ViewModel.Dispose();
            }
            base.OnClosed(e);
        }
    }

}