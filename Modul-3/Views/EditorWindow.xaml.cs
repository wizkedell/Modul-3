using Modul_3.Models;
using Modul_3.ViewModels;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
using System.Windows.Media;

namespace Modul_3.Views
{
    public partial class EditorWindow : Window
    {
        private EditorViewModel ViewModel => (EditorViewModel)DataContext;
        private Dictionary<ContactMarker, ContactMarkerControl> _markerControls = new Dictionary<ContactMarker, ContactMarkerControl>();

        // Фиксированные размеры изображения
        private const double ImageWidth = 800;
        private const double ImageHeight = 600;

        public EditorWindow(Product product)
        {
            InitializeComponent();

            var viewModel = new EditorViewModel();
            viewModel.CurrentProduct = product;
            DataContext = viewModel;

            // Устанавливаем фиксированный размер изображения в ViewModel
            viewModel.ImageSize = new Size(ImageWidth, ImageHeight);

            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.Markers.CollectionChanged += Markers_CollectionChanged;

            System.Diagnostics.Debug.WriteLine("EditorWindow инициализирован с фиксированным размером изображения");

            Dispatcher.BeginInvoke(new Action(() => UpdateMarkers()), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Свойство изменено: {e.PropertyName}");

            if (e.PropertyName == nameof(EditorViewModel.SelectedConnector))
            {
                UpdateMarkers();
            }
        }

        private void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"CollectionChanged: {e.Action}");
            UpdateMarkers();
        }

        private void UpdateMarkers()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Обновление маркеров. Всего в ViewModel: {ViewModel?.Markers?.Count}");

                MarkersCanvas.Children.Clear();
                _markerControls.Clear();

                if (ViewModel?.Markers == null || ViewModel.Markers.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Нет маркеров для отображения");
                    return;
                }

                // Добавляем новые маркеры
                foreach (var marker in ViewModel.Markers)
                {
                    var markerControl = new ContactMarkerControl();
                    markerControl.DataContext = marker;

                    // Устанавливаем позицию на основе относительных координат
                    UpdateMarkerPosition(marker, markerControl);

                    MarkersCanvas.Children.Add(markerControl);
                    _markerControls[marker] = markerControl;

                    System.Diagnostics.Debug.WriteLine($"Добавлен маркер {marker.ContactNumber} на относительную позицию: {marker.RelativeX:F3}, {marker.RelativeY:F3}");

                    // Подписываемся на изменение относительных координат
                    marker.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ContactMarker.RelativeX) ||
                            e.PropertyName == nameof(ContactMarker.RelativeY))
                        {
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                if (_markerControls.TryGetValue(marker, out var control))
                                {
                                    UpdateMarkerPosition(marker, control);
                                    EditorViewModel._hasUnsavedChanges = true;
                                }
                            }));
                        }
                    };
                }

                System.Diagnostics.Debug.WriteLine($"Добавлено {ViewModel.Markers.Count} маркеров на Canvas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении маркеров: {ex.Message}");
            }
        }

        private void UpdateMarkerPosition(ContactMarker marker, ContactMarkerControl control)
        {
            // Вычисляем абсолютные координаты на основе относительных и фиксированного размера изображения
            double absoluteX = (marker.RelativeX * ImageWidth) - (control.Width / 2);
            double absoluteY = (marker.RelativeY * ImageHeight) - (control.Height / 2);

            // Ограничиваем в пределах Canvas
            absoluteX = Math.Max(0, Math.Min(ImageWidth - control.Width, absoluteX));
            absoluteY = Math.Max(0, Math.Min(ImageHeight - control.Height, absoluteY));

            Canvas.SetLeft(control, absoluteX);
            Canvas.SetTop(control, absoluteY);

           

            System.Diagnostics.Debug.WriteLine($"Маркер {marker.ContactNumber} установлен на Absolute({absoluteX:F0}, {absoluteY:F0}) из Relative({marker.RelativeX:F3}, {marker.RelativeY:F3})");
        }

        protected override void OnClosed(EventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
                ViewModel.Markers.CollectionChanged -= Markers_CollectionChanged;
            }
            base.OnClosed(e);
        }
    }
}
