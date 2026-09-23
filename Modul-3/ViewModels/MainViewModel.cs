using Microsoft.Win32;
using Modul_3.Models;
using Modul_3.Services;
using Modul_3.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Modul_3.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ProductCatalogLoader _loader;
        private Product _selectedProduct;
        private string _productNumber;
        private string _selectedOperator;
        private readonly ArduinoService _arduinoService;
        private string _arduinoStatus;
        private string path = @"C:\PROG\Operators.txt"; //Стандартный путь для создания директории
        private bool _isArduinoConnected;

        public ProductCatalog Catalog { get; private set; }
        public ObservableCollection<Product> Products { get; private set; }
        public ObservableCollection<string> Operators { get; private set; }
        public ICommand ResumeSessionCommand { get; private set; }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                _selectedProduct = value;
                OnPropertyChanged();
            }
        }

        public string ProductNumber
        {
            get => _productNumber;
            set
            {
                _productNumber = value;
                OnPropertyChanged();
            }
        }

        public string SelectedOperator
        {
            get => _selectedOperator;
            set
            {
                _selectedOperator = value;
                OnPropertyChanged();
            }
        }

        public string ArduinoStatus
        {
            get => _arduinoStatus;
            set
            {
                _arduinoStatus = value;
                OnPropertyChanged();
            }
        }

        public bool IsArduinoConnected
        {
            get => _isArduinoConnected;
            set
            {
                _isArduinoConnected = value;
                OnPropertyChanged();
            }
        }

        public ICommand StartCommand { get; private set; }
        public ICommand EditorCommand { get; private set; }

        public MainViewModel()
        {
            _loader = new ProductCatalogLoader();
            _arduinoService = new ArduinoService();
            _arduinoService.MessageReceived += OnMessageReceived;

            LoadCatalog();
            InitializeOperators();
            InitializeCommands();

            // Инициализируем статус Arduino
            ArduinoStatus = "Не подключено";
        }

        private void LoadCatalog()
        {
            Catalog = _loader.LoadCatalog();
            Products = new ObservableCollection<Product>(Catalog.Products);
            OnPropertyChanged(nameof(Products));

            if (Products.Count > 0)
            {
                SelectedProduct = Products[0];
            }
        }
        //Создание Стандартной папки пути списка операторов
        private void CreateDefaultOperatorsFile(string path, List<string> defaults) // defaults - список стандартных имен операторов.
        {
            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllLines(path, defaults);
                Debug.WriteLine($"Создан файл операторов: {path}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Не удалось создать файл операторов: {ex.Message}");
            }
        }

        private void InitializeOperators()
        {

            string operatorsPath = @"C:\PROG\Operators.txt";
            List<string> operatorsList;
            try
            {
                if (File.Exists(operatorsPath))
                {
                    string content = File.ReadAllText(operatorsPath);
                    operatorsList = content.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .Where(s => !string.IsNullOrWhiteSpace(s))
                                           .ToList();
                }
                else
                {
                    // Если файла нет — создает новый файл с дефолтными значениями
                    operatorsList = new List<string> { "Оператор №1", "Оператор №2" };
                    CreateDefaultOperatorsFile(path, operatorsList);
                }
            }
            catch (Exception ex)
            {
                //Дефолтный список
                Debug.WriteLine($"Ошибка загрузки операторов: {ex.Message}");

                operatorsList = new List<string> { "Оператор №1", "Оператор №2" };
            }
            Operators = new ObservableCollection<string>(operatorsList);
            OnPropertyChanged(nameof(Operators));

            if (Operators.Count > 0)
            {
                SelectedOperator = Operators[0];
            }
        }

        private void InitializeCommands()
        {
            StartCommand = new RelayCommand(
                execute: async () => await StartTestingAsync(),
                canExecute: () => CanStartTesting());

            EditorCommand = new RelayCommand(
                execute: () => OpenEditor());
            ResumeSessionCommand = new RelayCommand(
                execute: async() =>  await ResumeSessionAsync());
                
        }

        private async System.Threading.Tasks.Task StartTestingAsync()
        {
            try
            {
                // Если Arduino уже подключено, сразу открываем окно контроля
                if (IsArduinoConnected && _arduinoService.IsConnected)
                {
                    OpenControlWindow();
                    return;
                }

                ArduinoStatus = "Поиск Модуль-1...";

                // Автоматическое определение порта
                var ports = _arduinoService.AvailablePorts;
                if (ports.Length == 0)
                {
                    MessageBox.Show("Модуль-1 не найден. Проверьте подключение.", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    ArduinoStatus = "Модуль-1 не найден";
                    return;
                }
                bool connected = false;
                foreach (var port in ports)
                {

                    connected = await _arduinoService.ConnectAsync(port);
                    if (connected == true)
                    {
                        break;
                    }

                }
                if (connected)
                {
                    IsArduinoConnected = true;
                    ArduinoStatus = "Подключено";
                    OpenControlWindow();

                }
                else
                {
                    MessageBox.Show("Не удалось подключиться к Модуль-1", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    ArduinoStatus = "Ошибка подключения";
                }
                // Пытаемся подключиться к первому доступному порту Нужно поменять на скан (отправление и получение сообщение
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске: {ex.Message}", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
                ArduinoStatus = "Ошибка";
            }
        }

        private void OpenControlWindow()
        {
            var controlWindow = new ControlWindow(SelectedProduct, ProductNumber, SelectedOperator, _arduinoService);
            controlWindow.Owner = Application.Current.MainWindow;
            controlWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            // Подписываемся на событие закрытия окна контроля
            controlWindow.Closed += (s, e) =>
            {
                // При закрытии окна контроля не отключаем Arduino, чтобы можно было быстро перезапустить
                ArduinoStatus = "Готов к работе";
            };

            controlWindow.ShowDialog();
        }

        private bool CanStartTesting()
        {
            return SelectedProduct != null &&
                   !string.IsNullOrWhiteSpace(ProductNumber) &&
                   !string.IsNullOrWhiteSpace(SelectedOperator);
        }

        private void OpenEditor()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show("Выберите изделие для редактирования", "Внимание",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editorWindow = new EditorWindow(SelectedProduct);
                editorWindow.Owner = Application.Current.MainWindow;
                editorWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                editorWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии редактора: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMessageReceived(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (message.Contains("kak:1") || message.Contains("Hello") || message.Contains("Arduino"))
                {

                    ArduinoStatus = message;
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Метод для продолжения прозвонки.
        private async Task ResumeSessionAsync()   // ← async Task
        {
            // 1. Показать диалог выбора файла
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл сессии",
                Filter = "Файлы сессий (*.json)|*.json",
                InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sessions")
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                ArduinoStatus = "Поиск Модуль-1...";
                var progressService = new ProgressService();
                var progress = progressService.Load(dialog.FileName);

                var product = Products.FirstOrDefault(p => p.Name == progress.ProductName);
                if (product == null)
                {
                    MessageBox.Show($"Изделие '{progress.ProductName}' не найдено в каталоге.",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!_arduinoService.IsConnected)
                {
                    bool connected = false;
                    foreach (var port in _arduinoService.AvailablePorts)
                    {
                        connected = await _arduinoService.ConnectAsync(port);
                        
                        if (connected)
                        {
                            ArduinoStatus = "Подключено";
                            break;
                        }
                    }

                    if (!connected)
                    {
                        MessageBox.Show("Модуль-1 не найден.", "Ошибка",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        ArduinoStatus = "Ошибка подключения";
                        return;
                    }
                }

                var controlWindow = new ControlWindow(
                    product,
                    progress.ProductNumber,
                    progress.Operator,
                    _arduinoService,
                    progress);

                controlWindow.Owner = Application.Current.MainWindow;
                controlWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                controlWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
              
            }
        }
    }
}
