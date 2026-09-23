using Modul_3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Modul_3.Services
{
    public class ProductCatalogLoader
    {
        private readonly string _defaultConfigPath;

        public ProductCatalogLoader(string configPath = null)
        {
            // Если путь не указан, используем путь по умолчанию на диске C:
            _defaultConfigPath = configPath ?? @"C:\Users\ТЕСТ\Desktop\Modul-3\PROG\prog.txt";
        }

        public ProductCatalog LoadCatalog()
        {
            return LoadCatalog(_defaultConfigPath);
        }

        public ProductCatalog LoadCatalog(string configPath)
        {
            var catalog = new ProductCatalog();

            if (!File.Exists(configPath))
            {
                // Создаем папку и пример конфигурации, если файла нет
                CreateSampleConfig(configPath);
                return catalog;
            }

            var lines = File.ReadAllLines(configPath);

            foreach (var line in lines)
            {
                var product = ParseProductLine(line);
                if (product != null)
                {
                    LoadProductConnectors(product);
                    catalog.Products.Add(product);
                }
            }

            return catalog;
        }

        // Метод ParseProductLine теперь внутри класса
        private Product ParseProductLine(string line)
        {
            // Формат: "Название изделия" - "путь к папке"
            var match = Regex.Match(line, @"\""(.+?)\""\s*-\s*\""(.+?)\""");

            if (match.Success && match.Groups.Count == 3)
            {
                return new Product
                {
                    Name = match.Groups[1].Value,
                    ConnectorsFolderPath = match.Groups[2].Value
                };
            }

            return null;
        }

        // Метод LoadProductConnectors теперь внутри класса
        private void LoadProductConnectors(Product product)
        {
            if (!Directory.Exists(product.ConnectorsFolderPath))
                return;

            var txtFiles = Directory.GetFiles(product.ConnectorsFolderPath, "*.txt");

            foreach (var txtFile in txtFiles)
            {
                var connector = LoadConnector(txtFile);
                if (connector != null)
                {
                    product.Connectors.Add(connector);
                }
            }
        }

        private Connector LoadConnector(string txtFilePath)
        {
            var connectorName = Path.GetFileNameWithoutExtension(txtFilePath);
            var imagePath = FindImageForConnector(txtFilePath);
            var contacts = LoadContactsFromFile(txtFilePath);

            return new Connector
            {
                Name = connectorName,
                ImagePath = imagePath,
                Contacts = contacts
            };
        }

        private string FindImageForConnector(string txtFilePath)
        {
            var baseName = Path.GetFileNameWithoutExtension(txtFilePath);
            var directory = Path.GetDirectoryName(txtFilePath);

            var imageExtensions = new[] { ".jpeg", ".jpg", ".png", ".bmp", ".svg" };
            foreach (var ext in imageExtensions)
            {
                var imagePath = Path.Combine(directory, baseName + ext);
                if (File.Exists(imagePath))
                    return imagePath;
            }

            return null;
        }

        private Dictionary<int, string> LoadContactsFromFile(string txtFilePath)
        {
            var contacts = new Dictionary<int, string>();

            try
            {
                var lines = File.ReadAllLines(txtFilePath);

                foreach (var line in lines)
                {
                    System.Diagnostics.Debug.WriteLine($"Обрабатываем строку: '{line}'");

                    // Улучшенный парсинг с учетом кавычек
                    var dashIndex = line.IndexOf('-');
                    if (dashIndex >= 0)
                    {
                        string numberPart = line.Substring(0, dashIndex).Trim();

                        if (int.TryParse(numberPart, out int contactNumber))
                        {
                            // Извлекаем часть после дефиса
                            string tagPart = line.Substring(dashIndex + 1).Trim();

                            // Убираем обрамляющие кавычки если они есть
                            if (tagPart.StartsWith("\"") && tagPart.EndsWith("\""))
                            {
                                tagPart = tagPart.Substring(1, tagPart.Length - 2);
                            }

                            contacts[contactNumber] = tagPart;
                            System.Diagnostics.Debug.WriteLine($"Добавлен контакт: {contactNumber} -> '{tagPart}'");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Не удалось разобрать номер контакта: {numberPart}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Отсутствует разделитель '-' в строке: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки файла {txtFilePath}: {ex.Message}");
            }

            return contacts;
        }

        private void CreateSampleConfig(string configPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Создаем пример конфигурации
                var sampleContent =
                    "\"Блок управления X1\" - \"C:\\TestProducts\\X1\"\n" +
                    "\"Панель управления Y2\" - \"C:\\TestProducts\\Y2\"";

                File.WriteAllText(configPath, sampleContent);

                // Создаем пример структуры папок для тестирования
                CreateSampleProductStructure();
            }
            catch (Exception ex)
            {
                // Логируем ошибку, но не падаем
                System.Diagnostics.Debug.WriteLine($"Не удалось создать конфиг: {ex.Message}");
            }
        }

        private void CreateSampleProductStructure()
        {
            try
            {
                // Создаем тестовую структуру папок и файлов
                var testProductPath = @"C:\TestProducts\X1";
                if (!Directory.Exists(testProductPath))
                {
                    Directory.CreateDirectory(testProductPath);

                    // Создаем пример файла разъема А
                    File.WriteAllText(Path.Combine(testProductPath, "А.txt"),
                        "1 - \"475\"\n2 - \"846\"\n3 - \"921\"");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось создать тестовую структуру: {ex.Message}");
            }
        }
    }
    

    //Сохранение прогресса
    public class ProgressService
    {
        
        private static string SessionsFolder =>
            @"C:\PROG\Sessions";


        // Сохранить прогресс в файл.

        public string Save(TestProgress progress)
        {
            if (!Directory.Exists(SessionsFolder))
                Directory.CreateDirectory(SessionsFolder);

            string fileName = $"{progress.ProductName}_{progress.ProductNumber}_" +
                              $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";

            // Убираем недопустимые символы из имени
            fileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));

            string path = Path.Combine(SessionsFolder, fileName);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(progress, options);
            File.WriteAllText(path, json);

            return path;
        }


        // Загрузить прогресс из файла.

        public TestProgress Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Файл сессии не найден: {path}");

            string json = File.ReadAllText(path);
            var progress = JsonSerializer.Deserialize<TestProgress>(json);

            if (progress == null)
                throw new InvalidDataException("Файл сессии повреждён");

            return progress;
        }


        // Список всех сохранённых сессий.

        public List<string> GetSavedSessions()
        {
            if (!Directory.Exists(SessionsFolder))
                return new List<string>();

            return Directory.GetFiles(SessionsFolder, "*.json")
                            .OrderByDescending(File.GetLastWriteTime)
                            .ToList();
        }
    }
}
