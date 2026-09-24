
using Modul_3.Models;
using Modul_3.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

using System.Windows;
using System.Windows.Input;



namespace Modul_3.ViewModels
{
    public class ControlViewModel : INotifyPropertyChanged, IDisposable
    {
        private Product _currentProduct;
        private Connector _selectedConnector;
        
        private string _imagePath;
        private Size _imageSize = new Size(800, 600);
        public static bool _hasUnsavedChanges;

        private string _activeMarkerText;
        private string _diagnosticMessage; // Новое свойство для диагностических сообщений
        private string _productInfo;
        private string _productNumber;
        private string _operator;
        private string _arduinoStatus;

        private string _currentReadCommand = "rpn:12"; // по умолчанию 10 контактов


        private int _globalActivationDelay = 500;
        private Dictionary<ContactMarker, Timer> _activationTimers = new Dictionary<ContactMarker, Timer>();

        // Словарь для хранения состояний ячеек памяти по разъемам
        private Dictionary<string, Dictionary<int, int>> _connectorMemoryStates = new Dictionary<string, Dictionary<int, int>>();

        // Arduino сервис и связанные свойства
        private readonly ArduinoService _arduinoService;


        // Команды
        public ICommand ResetModuleCommand { get; private set; }
        public ICommand FinishTestCommand { get; private set; }

        public ControlViewModel(Product product, string productNumber, string operatorName, ArduinoService arduinoService)
        {
            _currentProduct = product;
            _productInfo = product?.Name;
            _productNumber = productNumber;
            _operator = operatorName;
            _arduinoService = arduinoService;

            _arduinoService.ContactsStateChanged += OnContactsStateChanged;
            _arduinoService.MessageReceived += OnMessageReceived;

            InitializeCommands();

            // Запускаем опрос контактов (Данные о коннекторе еще не загружены)
            //if (_arduinoService.IsConnected)
            //{
            //    _currentReadCommand = DetermineCommand(Markers.Count);
            //    _arduinoService.StartContinuousReading(_currentReadCommand);
            //    ArduinoStatus = "Подключено";
            //}

            // Автоматически выбираем первый разъем при создании
            if (Connectors.Count > 0)
            {
                SelectedConnector = Connectors[0];
            }
            
        }
        //Метод определения команды. Получает значение 
        private string DetermineCommand(int pinCount)
        {
            // До 10 контактов включительно — rpn:12 (10 пинов)
            // Больше 10 — tr:37 (35 пинов)
            return pinCount <= 10 ? "rpn:12" : "tr:37";
        }

        private void InitializeCommands()
        {
            ResetModuleCommand = new RelayCommand(
                execute: () => ResetCurrentModule(),
                canExecute: () => SelectedConnector != null
            );

            FinishTestCommand = new RelayCommand(
                execute: () => FinishTesting()
            );
            PauseCommand = new RelayCommand(
            execute: () => PauseAndSave(),
            canExecute: () => _selectedConnector != null
             );
        }

        public string ProductInfo
        {
            get => _productInfo;
            set
            {
                _productInfo = value;
                OnPropertyChanged(nameof(ProductInfo));
            }
        }

        public string ProductNumber
        {
            get => _productNumber;
            set
            {
                _productNumber = value;
                OnPropertyChanged(nameof(ProductNumber));
            }
        }

        public string Operator
        {
            get => _operator;
            set
            {
                _operator = value;
                OnPropertyChanged(nameof(Operator));
            }
        }

        public string ArduinoStatus
        {
            get => _arduinoStatus;
            set
            {
                _arduinoStatus = value;
                OnPropertyChanged(nameof(ArduinoStatus));
            }
        }


        public int GlobalActivationDelay
        {
            get => _globalActivationDelay;
            set
            {
                if (_globalActivationDelay != value)
                {
                    _globalActivationDelay = value;
                    OnPropertyChanged(nameof(GlobalActivationDelay));

                    // Обновляем задержку для всех маркеров
                    foreach (var marker in Markers)
                    {
                        marker.ActivationDelay = value;
                    }
                }
            }
        }

        public string ActiveMarkerText
        {
            get => _activeMarkerText;
            set
            {
                if (_activeMarkerText != value)
                {
                    _activeMarkerText = value;
                    OnPropertyChanged(nameof(ActiveMarkerText));
                }
            }
        }

        public string DiagnosticMessage
        {
            get => _diagnosticMessage;
            set
            {
                if (_diagnosticMessage != value)
                {
                    _diagnosticMessage = value;
                    OnPropertyChanged(nameof(DiagnosticMessage));
                }
            }
        }

        public Product CurrentProduct
        {
            get => _currentProduct;
            set
            {
                if (_currentProduct == value) return;

                _currentProduct = value;
                OnPropertyChanged(nameof(CurrentProduct));
                OnPropertyChanged(nameof(Connectors));

                // Автоматически выбираем первый разъем при смене продукта
                if (Connectors.Count > 0)
                {
                    SelectedConnector = Connectors[0];
                }
                else
                {
                    SelectedConnector = null;
                }

                _hasUnsavedChanges = false;
            }
        }


        public ObservableCollection<Connector> Connectors =>
           new ObservableCollection<Connector>(_currentProduct?.Connectors ?? Enumerable.Empty<Connector>());

        public ObservableCollection<ContactMarker> Markers { get; } = new ObservableCollection<ContactMarker>();

        public Connector SelectedConnector
        {
            get => _selectedConnector;
            set
            {
                if (_selectedConnector == value) return;

                // Сохраняем состояние перед сменой разъема
                if (_selectedConnector != null)
                {
                    SaveCurrentMemoryState();
                }

                _selectedConnector = value;
                OnPropertyChanged(nameof(SelectedConnector));

                // Загружаем данные разъема, даже если значение null
                LoadConnectorData();

            }
        }


        private void ResetCurrentModule()
        {
            if (_selectedConnector == null) return;

            var connectorKey = GetConnectorKey();
            if (_connectorMemoryStates.ContainsKey(connectorKey))
            {
                _connectorMemoryStates[connectorKey].Clear();
            }

            foreach (var marker in Markers)
            {
                marker.ActivatedSegments = 0;
            }

            UpdateConnectorStatus();
            MessageBox.Show($"Состояние разъема {_selectedConnector.Name} сброшено", "Сброс",
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void FinishTesting()
        {
            var uncheckedConnectors = Connectors.Where(c => !c.IsChecked).ToList();
            if (uncheckedConnectors.Any())
            {
                string connectorNames = string.Join(", ", uncheckedConnectors.Select(c => c.Name));
                MessageBox.Show($"Не все разъемы проверены: {connectorNames}", "Внимание",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Запись результатов в файл
            SaveTestResults();

            // Закрытие окна
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.DataContext == this)
                    {
                        window.Close();
                        break;
                    }
                }
            });
        }

        private void SaveTestResults()
        {
            try
            {
                // TODO: УКАЖИТЕ ПУТЬ К ФАЙЛУ ДЛЯ СОХРАНЕНИЯ РЕЗУЛЬТАТОВ
                string filePath = @"C:\TestResults\results.txt"; // ИЗМЕНИТЕ ЭТОТ ПУТЬ

                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string result = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Изделие: {ProductInfo}, " +
                              $"Номер: {ProductNumber}, Оператор: {Operator}{Environment.NewLine}";

                File.AppendAllText(filePath, result);

                MessageBox.Show($"Результаты сохранены в файл: {filePath}", "Завершено",
                              MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении результатов: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void UpdateConnectorStatus()
        {
            if (_currentProduct?.Connectors == null) return;

            foreach (var connector in _currentProduct.Connectors)
            {
                connector.IsChecked = IsConnectorChecked(connector);
            }
        }

        private bool IsConnectorChecked(Connector connector)
        {
            var connectorKey = GetConnectorKey(connector);
            if (!_connectorMemoryStates.ContainsKey(connectorKey))
                return false;

            var memoryState = _connectorMemoryStates[connectorKey];

            // Загружаем разметку для проверки контактов
            var layoutData = LoadLayoutDataForConnector(connector);
            if (layoutData?.ContactPositions == null)
                return false;

            // Проверяем все контакты кроме тех, где ContactTag = "ПУСТО"
            foreach (var contact in layoutData.ContactPositions)
            {
                // Пропускаем контакты с тегом "ПУСТО"
                if (contact.ContactTag?.ToUpper() == "ПУСТО")
                    continue;

                // Получаем количество сегментов для контакта
                int totalSegments = GetTotalSegmentsFromTag(contact.ContactTag);

                // Проверяем, активированы ли все сегменты
                if (memoryState.ContainsKey(contact.ContactNumber))
                {
                    int activatedSegments = memoryState[contact.ContactNumber];
                    if (activatedSegments < totalSegments)
                        return false; // Не все сегменты активированы
                }
                else
                {
                    return false; // Нет данных о контакте
                }
            }

            return true; // Все непустые контакты полностью проверены
        }



        private int GetTotalSegmentsFromTag(string contactTag)
        {
            if (string.IsNullOrEmpty(contactTag) || contactTag.ToUpper() == "ПУСТО")
                return 1;

            return contactTag.Split(',').Length;
        }

        private LayoutData LoadLayoutDataForConnector(Connector connector)
        {
            string layoutFilePath = GetLayoutFilePath(connector);
            if (!File.Exists(layoutFilePath)) return null;

            try
            {
                string json = File.ReadAllText(layoutFilePath);
                return JsonSerializer.Deserialize<LayoutData>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки разметки для проверки статуса: {ex.Message}");
                return null;
            }
        }

        private string GetLayoutFilePath(Connector connector)
        {
            if (connector == null) return string.Empty;

            string connectorDir = Path.GetDirectoryName(connector.ImagePath);
            string connectorName = Path.GetFileNameWithoutExtension(connector.ImagePath);

            if (string.IsNullOrEmpty(connectorDir))
            {
                connectorDir = _currentProduct?.ConnectorsFolderPath;
                connectorName = connector.Name;
            }

            return Path.Combine(connectorDir, $"{connectorName}.layout.json");
        }

        private string GetConnectorKey(Connector connector)
        {
            return $"{_currentProduct?.Name}_{connector?.Name}";
        }


        private void LoadConnectorData()
        {
            // Останавливаем все таймеры при смене коннектора
            foreach (var timer in _activationTimers.Values)
            {
                timer?.Dispose();
            }
            _activationTimers.Clear();

            if (_selectedConnector == null)
            {
                Markers.Clear();
                ImagePath = null;
                ActiveMarkerText = string.Empty;
                DiagnosticMessage = string.Empty;
                return;
            }

            if (!LoadLayoutFromFile())
            {
                MessageBox.Show($"Отсутствует файл программы", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            if (_arduinoService.IsConnected)
            {
                _currentReadCommand = DetermineCommand(Markers.Count);
                _arduinoService.StartContinuousReading(_currentReadCommand);
            }
        }


        private void SaveCurrentMemoryState()
        {
            if (_selectedConnector == null || !Markers.Any()) return;

            var connectorKey = GetConnectorKey();
            _connectorMemoryStates[connectorKey] = new Dictionary<int, int>();

            foreach (var marker in Markers)
            {
                _connectorMemoryStates[connectorKey][marker.ContactNumber] = marker.ActivatedSegments;
            }

            UpdateConnectorStatus();
        }

        private void RestoreMemoryState()
        {
            if (_selectedConnector == null) return;

            var connectorKey = GetConnectorKey();
            if (_connectorMemoryStates.ContainsKey(connectorKey))
            {
                var memoryState = _connectorMemoryStates[connectorKey];
                foreach (var marker in Markers)
                {
                    if (memoryState.ContainsKey(marker.ContactNumber))
                    {
                        marker.ActivatedSegments = memoryState[marker.ContactNumber];
                    }
                }
            }
            UpdateConnectorStatus();
        }

        private string GetConnectorKey()
        {
            return $"{_currentProduct?.Name}_{_selectedConnector?.Name}";
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
                    var marker = new ContactMarker
                    {
                        ContactNumber = position.ContactNumber,
                        ContactTag = position.ContactTag,
                        RelativeX = position.RelativeX,
                        RelativeY = position.RelativeY,
                        Diameter = position.Diameter, // Используем диаметр из JSON
                        IsActive = false,
                        ActivationDelay = GlobalActivationDelay
                    };

                    Markers.Add(marker);
                }

                CreateContactLinks();
                RestoreMemoryState(); // Восстанавливаем после загрузки маркеров


                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки файла разметки: {ex.Message}");
                MessageBox.Show($"Ошибка загрузки файла разметки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }



        // Создание связей между контактами с одинаковым ContactTag
        private void CreateContactLinks()
        {
            // Группируем маркеры по ContactTag
            var groupedByTag = Markers
                .Where(m => !string.IsNullOrEmpty(m.ContactTag) && m.ContactTag.ToUpper() != "ПУСТО")
                .GroupBy(m => m.ContactTag);

            foreach (var group in groupedByTag)
            {
                var contactNumbers = group.Select(m => m.ContactNumber).ToList();
                foreach (var marker in group)
                {
                    marker.LinkedContacts = contactNumbers.Where(cn => cn != marker.ContactNumber).ToList();
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



        private void OnContactsStateChanged(bool[] states)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Работаем только если есть выбранный разъем
                if (_selectedConnector == null) return;

                string activeText = string.Empty;
                var diagnosticMessages = new List<string>();

                for (int i = 0; i < states.Length && i < Markers.Count; i++)
                {
                    var marker = Markers.FirstOrDefault(m => m.ContactNumber == i + 1);
                    if (marker != null)
                    {
                        bool wasActive = marker.IsActive;
                        marker.IsActive = states[i];

                        if (states[i] && !wasActive)
                        {
                            StartActivationTimer(marker);
                        }
                        else if (!states[i] && wasActive)
                        {
                            StopActivationTimer(marker);
                        }

                        // Показываем только первый активный тег
                        if (states[i] && !string.IsNullOrEmpty(marker.ContactTag) && string.IsNullOrEmpty(activeText))
                        {
                            activeText = $"{marker.ContactTag}";
                        }
                    }
                }

                ActiveMarkerText = activeText;
                CheckForFaults(diagnosticMessages);


                DiagnosticMessage = diagnosticMessages.Any()
                    ? "Диагностика:" + Environment.NewLine + string.Join(Environment.NewLine, diagnosticMessages)
                    : "Диагностика: Нет ошибок";
            });
        }

        private void StartActivationTimer(ContactMarker marker)
        {
            StopActivationTimer(marker);

            var timer = new Timer(_ =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (marker.ActivatedSegments < marker.TotalSegments)
                    {
                        marker.ActivatedSegments++;
                        SaveCurrentMemoryState(); // Сохраняем после каждой активации
                    }

                    StopActivationTimer(marker);
                });
            }, null, marker.ActivationDelay, Timeout.Infinite);

            _activationTimers[marker] = timer;
        }

        private void StopActivationTimer(ContactMarker marker)
        {
            if (_activationTimers.ContainsKey(marker))
            {
                _activationTimers[marker]?.Dispose();
                _activationTimers.Remove(marker);
            }
        }



        // Проверка на обрывы и замыкания
        private void CheckForFaults(List<string> diagnosticMessages)
        {
            // Проверка контактов с тегом "ПУСТО"
            var emptyContacts = Markers.Where(m =>
                m.ContactTag != null && m.ContactTag.ToUpper() == "ПУСТО" && m.IsActive).ToList();

            if (emptyContacts.Any())
            {
                _arduinoService.StopContinuousReading();
                _arduinoService.ClearPendingBuffer();
                var emptyNumbers = string.Join(", ", emptyContacts.Select(c => c.ContactNumber));
                string message = $"Замыкание: контакты {emptyNumbers} (ПУСТО)";
                diagnosticMessages.Add(message);
                MessageBox.Show(message, "Внимание!", MessageBoxButton.OK);
                _arduinoService.ClearPendingBuffer();

                // Сбрасываем состояния контактов после показа сообщения
                ResetContactStates();
                _arduinoService.StartContinuousReading(_currentReadCommand);

            }

            // Проверка групп контактов с одинаковым ContactTag
            var groupedContacts = Markers
                .Where(m => !string.IsNullOrEmpty(m.ContactTag) && m.ContactTag.ToUpper() != "ПУСТО")
                .GroupBy(m => m.ContactTag)
                .ToList();

            // Собираем все полностью активные цепи
            var fullyActiveCircuits = new List<string>();
            var breakErrors = new List<string>(); // Список для ошибок обрыва

            foreach (var group in groupedContacts)
            {
                var activeContacts = group.Where(m => m.IsActive).ToList();
                var allContacts = group.ToList();

                // Если в группе есть активные контакты, но не все - ОБРЫВ
                if (activeContacts.Any() && activeContacts.Count < allContacts.Count)
                {
                    var activeNumbers = string.Join(", ", activeContacts.Select(m => m.ContactNumber));
                    var allNumbers = string.Join(", ", allContacts.Select(m => m.ContactNumber));
                    string errorMessage = $"Обрыв: цепь '{group.Key}' (активны: {activeNumbers}, должны быть: {allNumbers})";
                    diagnosticMessages.Add(errorMessage);
                    breakErrors.Add(errorMessage);
                }

                // Если вся группа активна, добавляем в список
                if (activeContacts.Count == allContacts.Count)
                {
                    fullyActiveCircuits.Add(group.Key);
                }
            }

            // Если есть ошибки обрыва, показываем их одним сообщением
            if (breakErrors.Any())
            {
                _arduinoService.StopContinuousReading();
                _arduinoService.ClearPendingBuffer();
                string breakMessage = string.Join(Environment.NewLine, breakErrors);
                MessageBox.Show(breakMessage, "Обрыв цепи!", MessageBoxButton.OK);
                _arduinoService.ClearPendingBuffer();

                // Сбрасываем состояния контактов после показа сообщения
                ResetContactStates();
                _arduinoService.StartContinuousReading(_currentReadCommand);

            }

            // Проверяем замыкания между цепями только если есть более одной активной цепи
            if (fullyActiveCircuits.Count > 1)
            {
                CheckForCrossShort(fullyActiveCircuits, diagnosticMessages);
            }

            // Дополнительная проверка: если активен одиночный контакт, который должен быть в группе
            var singleContacts = Markers.Where(m =>
                !string.IsNullOrEmpty(m.ContactTag) &&
                m.ContactTag.ToUpper() != "ПУСТО" &&
                m.IsActive &&
                m.LinkedContacts.Any()).ToList();

            var singleContactErrors = new List<string>(); // Список для ошибок одиночных контактов

            foreach (var contact in singleContacts)
            {
                // Проверяем, активны ли все связанные контакты
                var linkedActive = Markers.Where(m =>
                    m.ContactNumber != contact.ContactNumber &&
                    contact.LinkedContacts.Contains(m.ContactNumber) &&
                    m.IsActive).ToList();

                if (!linkedActive.Any())
                {
                    string errorMessage = $"Обрыв: контакт {contact.ContactNumber} ({contact.ContactTag}) - отсутствует связь с связанными контактами";
                    diagnosticMessages.Add(errorMessage);
                    singleContactErrors.Add(errorMessage);
                }
            }

            // Если есть ошибки одиночных контактов, показываем их одним сообщением
            if (singleContactErrors.Any())
            {
                _arduinoService.StopContinuousReading();
                _arduinoService.ClearPendingBuffer();
                string singleMessage = string.Join(Environment.NewLine, singleContactErrors);
                MessageBox.Show(singleMessage, "Обрыв цепи!", MessageBoxButton.OK);
                _arduinoService.ClearPendingBuffer();

                // Сбрасываем состояния контактов после показа сообщения
                ResetContactStates();
                _arduinoService.StartContinuousReading(_currentReadCommand);

            }
        }

        // Проверка на замыкания между разными цепями
        private void CheckForCrossShort(List<string> activeCircuits, List<string> diagnosticMessages)
        {
            if (activeCircuits.Count < 2) return;

            // Останавливаем опрос контактов
            _arduinoService.StopContinuousReading();
            _arduinoService.ClearPendingBuffer();

            // Собираем информацию о всех активных контактах в этих цепях
            var allActiveContactsInCircuits = Markers
                .Where(m => activeCircuits.Contains(m.ContactTag) && m.IsActive)
                .ToList();

            var circuitContacts = new Dictionary<string, List<int>>();
            foreach (var circuit in activeCircuits)
            {
                var contacts = allActiveContactsInCircuits
                    .Where(m => m.ContactTag == circuit)
                    .Select(m => m.ContactNumber)
                    .ToList();
                circuitContacts[circuit] = contacts;
            }

            // Формируем сообщение
            var circuitDetails = activeCircuits.Select(circuit =>
                $"цепь '{circuit}' (контакты {string.Join(", ", circuitContacts[circuit])})");

            string message = $"Замыкание между цепями: {string.Join(" замыкает с ", circuitDetails)}";
            diagnosticMessages.Add(message);

            // Показываем сообщение и ждем подтверждения пользователя
            MessageBox.Show(message, "Внимание!", MessageBoxButton.OK);
            _arduinoService.ClearPendingBuffer();

            // Сбрасываем состояния контактов после показа сообщения
            ResetContactStates();

            // После нажатия ОК возобновляем опрос
            _arduinoService.StartContinuousReading(_currentReadCommand);

        }

        // Новый метод для сброса состояний контактов
        private void ResetContactStates()
        {
            foreach (var marker in Markers)
            {
                marker.IsActive = false;
            }
        }

        private void OnMessageReceived(string message)
        {
            Debug.WriteLine($"puk: {message}");

            if (message.Contains("kak:1") || message.Contains("Hello") || message.Contains("Arduino"))
            {
                ArduinoStatus = message;
            }
        }



        public void Dispose()
        {
            _arduinoService?.StopContinuousReading();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ControlViewModel(Product product, string productNumber, string operatorName,
                        ArduinoService arduinoService, TestProgress savedProgress = null)
        {
            _currentProduct = product;
            _productInfo = product?.Name;
            _productNumber = productNumber;
            _operator = operatorName;
            _arduinoService = arduinoService;

            _arduinoService.ContactsStateChanged += OnContactsStateChanged;
            _arduinoService.MessageReceived += OnMessageReceived;

            InitializeCommands();

            // ⬅️ ЕСЛИ ЕСТЬ СОХРАНЁННЫЙ ПРОГРЕСС — восстанавливаем
            if (savedProgress != null)
            {
                RestoreFromProgress(savedProgress);
            }

            if (Connectors.Count > 0)
            {
                // ⬅️ выбираем последний модуль, на котором остановились
                if (savedProgress != null && !string.IsNullOrEmpty(savedProgress.LastConnectorName))
                {
                    var lastConnector = Connectors.FirstOrDefault(c =>
                        c.Name == savedProgress.LastConnectorName);
                    SelectedConnector = lastConnector ?? Connectors[0];
                }
                else
                {
                    SelectedConnector = Connectors[0];
                }
            }
        }
        //Метод восстановления прогресса из json файла прогресса
        private void RestoreFromProgress(TestProgress progress)
        {
            // Восстанавливаем память по модулям
            _connectorMemoryStates = new Dictionary<string, Dictionary<int, int>>(
                progress.ConnectorMemoryStates);

            // Восстанавливаем флаги "проверено"
            foreach (var connector in _currentProduct.Connectors)
            {
                connector.IsChecked = progress.CheckedConnectors.Contains(connector.Name);
            }
        }

        public ICommand PauseCommand { get; private set; }



        //Метод для паузы и сохранения дефектной позиции
        private void PauseAndSave()
        {
            try
            {
                // 1. Сохраняем состояние текущего модуля
                SaveCurrentMemoryState();

                // 2. Собираем снимок
                var progress = new TestProgress
                {
                    ProductName = _currentProduct?.Name,
                    ProductNumber = ProductNumber,
                    Operator = Operator,
                    SavedAt = DateTime.Now,
                    ConnectorMemoryStates = new Dictionary<string, Dictionary<int, int>>(_connectorMemoryStates),
                    CheckedConnectors = _currentProduct.Connectors
                        .Where(c => c.IsChecked)
                        .Select(c => c.Name)
                        .ToList(),
                    LastConnectorName = _selectedConnector?.Name
                };

                // 3. Сохраняем в файл
                var service = new ProgressService();
                string path = service.Save(progress);

                // 4. Останавливаем опрос
                _arduinoService.StopContinuousReading();

                // 5. Закрываем окно
                MessageBox.Show($"Сессия сохранена:\n{path}", "Пауза",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (Window w in Application.Current.Windows)
                    {
                        if (w.DataContext == this)
                        {
                            w.Close();
                            break;
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    } 
}

