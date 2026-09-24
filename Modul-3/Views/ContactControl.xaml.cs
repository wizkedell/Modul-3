using Modul_3.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Modul_3.Views
{
    public partial class ContactControl : UserControl
    {
        private Canvas _parentCanvas;

        public ContactControl()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;

            // Убираем все обработчики мыши - элемент только для отображения
            this.IsHitTestVisible = false; // Отключаем взаимодействие с мышью
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Получаем родительский Canvas после загрузки
            _parentCanvas = VisualTreeHelper.GetParent(this) as Canvas;
            UpdatePositionFromDataContext();
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
            else if (e.Property == ActualWidthProperty || e.Property == ActualHeightProperty)
            {
                // Обновляем позицию при изменении размера контрола
                UpdatePositionFromDataContext();
            }
        }

        private void Marker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ContactMarker.RelativeX) ||
                e.PropertyName == nameof(ContactMarker.RelativeY) ||
                e.PropertyName == nameof(ContactMarker.Diameter) ||
                e.PropertyName == nameof(ContactMarker.IsSelected) ||
                e.PropertyName == nameof(ContactMarker.IsActive) ||
                e.PropertyName == nameof(ContactMarker.ContactTag))
            {
                UpdatePositionFromDataContext();
            }
        }

        private void UpdatePositionFromDataContext()
        {
            if (DataContext is ContactMarker marker && _parentCanvas != null)
            {
                // Обновляем позицию на основе данных из ViewModel
                Canvas.SetLeft(this, marker.RelativeX * _parentCanvas.ActualWidth - this.ActualWidth / 2);
                Canvas.SetTop(this, marker.RelativeY * _parentCanvas.ActualHeight - this.ActualHeight / 2);
            }
        }

        // Метод для принудительного обновления позиции (если нужно извне)
        public void RefreshPosition()
        {
            UpdatePositionFromDataContext();
        }
    }
}