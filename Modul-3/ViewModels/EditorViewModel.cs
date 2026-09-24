using Modul_3.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace Modul_3.ViewModels
{
    public class EditorViewModel : INotifyPropertyChanged
    {
        private Product _currentProduct;
        private Connector _selectedConnector;
        private ContactMarker _selectedMarker;
        private string _imagePath;
        private Size _imageSize = new Size(800, 600);
        public static bool _hasUnsavedChanges;

        public Product CurrentProduct
        {
            get => _currentProduct;
            set
            {
                if (_currentProduct == value) return;

                // Проверяем изменения перед сменой продукта
                if (!CheckUnsavedChanges()) return;

                _currentProduct = value;
                OnPropertyChanged(nameof(CurrentProduct));
                OnPropertyChanged(nameof(Connectors));

                SelectedConnector = Connectors.FirstOrDefault();
                _hasUnsavedChanges = false;
            }
        }

        public ObservableCollection<Connector> Connectors =>
            new ObservableCollection<Connector>(_currentProduct?.Connectors ?? Enumerable.Empty<Connector>());

        public Connector SelectedConnector
        {
            get => _selectedConnector;
            set
            {
                if (_selectedConnector == value) return;

                // Проверяем изменения перед переключением разъема
                if (!CheckUnsavedChanges()) return;

                _selectedConnector = value;
                OnPropertyChanged(nameof(SelectedConnector));
                LoadConnectorData();
                _hasUnsavedChanges = false;
            }
        }

        public ObservableCollection<ContactMarker> Markers { get; } = new ObservableCollection<ContactMarker>();

        public ContactMarker SelectedMarker
        {
            get => _selectedMarker;
            set
            {
                if (_selectedMarker == value) return;
                _selectedMarker = value;
                OnPropertyChanged(nameof(SelectedMarker));
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath == value) return;
                _imagePath = value;
                OnPropertyChanged(nameof(ImagePath));
                _hasUnsavedChanges = true;
            }
        }

        public Size ImageSize
        {
            get => _imageSize;
            set
            {
                if (_imageSize == value) return;
                _imageSize = value;
                OnPropertyChanged(nameof(ImageSize));
            }
        }

        public ICommand AddMarkerCommand { get; }
        public ICommand RemoveMarkerCommand { get; }
        public ICommand SaveLayoutCommand { get; }
        public ICommand LoadImageCommand { get; }

        public EditorViewModel()
        {
            AddMarkerCommand = new RelayCommand(AddMarker);
            RemoveMarkerCommand = new RelayCommand(RemoveMarker, () => SelectedMarker != null);
            SaveLayoutCommand = new RelayCommand(SaveLayout);
            LoadImageCommand = new RelayCommand(LoadImage);

            // Отслеживаем изменения маркеров
            Markers.CollectionChanged += (s, e) => _hasUnsavedChanges = true;
        }

        private bool CheckUnsavedChanges()
        {
            if (!_hasUnsavedChanges) return true;

            var result = MessageBox.Show(
                "Есть несохраненные изменения. Хотите сохранить перед продолжением?",
                "Несохраненные изменения",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SaveLayout();
                    return true;
                case MessageBoxResult.No:
                    return true;
                case MessageBoxResult.Cancel:
                    return false;
                default:
                    return true;
            }
        }

        private void LoadConnectorData()
        {
            if (_selectedConnector == null)
            {
                Markers.Clear();
                ImagePath = null;
                return;
            }

            // Всегда загружаем из файла или инициализируем
            if (!LoadLayoutFromFile())
            {
                InitializeNewConnectorState();
            }
        }

        private bool LoadLayoutFromFile()
        {
            string layoutFilePath = GetLayoutFilePath();
            if (!File.Exists(layoutFilePath)) return false;

            try
            {
                string json = File.ReadAllText(layoutFilePath);
                var layoutData = JsonSerializer.Deserialize<LayoutData>(json);

                ImagePath = layoutData.ImagePath;
                ImageSize = layoutData.ImageSize;

                Markers.Clear();
                foreach (var position in layoutData.ContactPositions)
                {
                    Markers.Add(new ContactMarker
                    {
                        ContactNumber = position.ContactNumber,
                        ContactTag = position.ContactTag,
                        RelativeX = position.RelativeX,
                        RelativeY = position.RelativeY,
                        Diameter = position.Diameter
                    });
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void InitializeNewConnectorState()
        {
            ImagePath = _selectedConnector?.ImagePath;

            Markers.Clear();
            if (_selectedConnector?.Contacts != null)
            {
                foreach (var contact in _selectedConnector.Contacts)
                {
                    Markers.Add(new ContactMarker
                    {
                        ContactNumber = contact.Key,
                        ContactTag = contact.Value,
                        RelativeX = 0.1,
                        RelativeY = 0.1,
                        Diameter = 56
                    });
                }
            }
        }

        private void AddMarker()
        {
            var newMarker = new ContactMarker
            {
                ContactNumber = GetNextContactNumber(),
                ContactTag = $"Бирка_{GetNextContactNumber()}",
                RelativeX = 0.5,
                RelativeY = 0.5,
                Diameter = 35
            };

            Markers.Add(newMarker);
            SelectedMarker = newMarker;
        }

        private int GetNextContactNumber()
        {
            return Markers.Count == 0 ? 1 : Markers.Max(m => m.ContactNumber) + 1;
        }

        private void RemoveMarker()
        {
            if (SelectedMarker != null)
            {
                Markers.Remove(SelectedMarker);
                SelectedMarker = Markers.FirstOrDefault();
            }
        }

        private void SaveLayout()
        {
            if (_selectedConnector == null) return;

            var layoutData = new LayoutData
            {
                ConnectorName = _selectedConnector.Name,
                ImagePath = ImagePath,
                ImageSize = ImageSize
            };

            foreach (var marker in Markers)
            {
                layoutData.ContactPositions.Add(new ContactPosition
                {
                    ContactNumber = marker.ContactNumber,
                    ContactTag = marker.ContactTag,
                    RelativeX = marker.RelativeX,
                    RelativeY = marker.RelativeY,
                    Diameter = marker.Diameter
                });
            }

            string layoutFilePath = GetLayoutFilePath();
            string directory = Path.GetDirectoryName(layoutFilePath);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(layoutData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(layoutFilePath, json);
            _hasUnsavedChanges = false;

            MessageBox.Show($"Разметка сохранена в файл:\n{layoutFilePath}", "Успех",
                              MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg; *.jpeg; *.png; *.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ImagePath = openFileDialog.FileName;
                if (_selectedConnector != null)
                {
                    _selectedConnector.ImagePath = ImagePath;
                }
            }
        }

        private string GetLayoutFilePath()
        {
            if (_selectedConnector == null) return string.Empty;

            string connectorDir = Path.GetDirectoryName(_selectedConnector.ImagePath);
            string connectorName = Path.GetFileNameWithoutExtension(_selectedConnector.ImagePath);

            if (string.IsNullOrEmpty(connectorDir))
            {
                connectorDir = _currentProduct?.ConnectorsFolderPath;
                connectorName = _selectedConnector.Name;
            }

            return Path.Combine(connectorDir, $"{connectorName}.layout.json");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

       