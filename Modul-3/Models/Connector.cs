using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modul_3.Models
{
    public class Connector : INotifyPropertyChanged
    {
        private string _name;
        private string _imagePath;
        private Dictionary<int, string> _contacts = new Dictionary<int, string>();
        private bool _isChecked;

        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ImagePath
        {
            get => _imagePath;
            set
            {
                if (_imagePath != value)
                {
                    _imagePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public Dictionary<int, string> Contacts
        {
            get => _contacts;
            set
            {
                if (_contacts != value)
                {
                    _contacts = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    //Класс-снимок для сохранения параметров для продолжения прозвонки
    public class TestProgress
    {
        public string ProductName { get; set; }         // "Блок управления X1"
        public string ProductNumber { get; set; }       // серийный номер
        public string Operator { get; set; }            // "Мазур И.Ю."
        public DateTime SavedAt { get; set; }           // когда сохранили
        public DateTime StartedAt { get; set; }         // когда начали

        // Прогресс: ключ "ProductName_ConnectorName" → {номер контакта → активированные сегменты}
        public Dictionary<string, Dictionary<int, int>> ConnectorMemoryStates { get; set; }
            = new Dictionary<string, Dictionary<int, int>>();

        // Какие модули уже полностью пройдены
        public List<string> CheckedConnectors { get; set; } = new List<string>();

        // Какой модуль был активен на момент паузы
        public string LastConnectorName { get; set; }
    }
}
