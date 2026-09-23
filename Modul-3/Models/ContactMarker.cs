using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Modul_3.Models
{
    public class ContactMarker : INotifyPropertyChanged
    {
        public int _contactNumber;
        private string _contactTag;
        private bool _isSelected;
        private double _relativeX;
        private double _relativeY;
        private double _diameter = 30;
        private bool _isActive;

        private int _activatedSegments;
        private int _totalSegments = 1;
        private int _activationDelay = 500;

        public List<int> LinkedContacts { get; set; } = new List<int>();


        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged();
                }
            }
        }


        public int ActivatedSegments
        {
            get => _activatedSegments;
            set
            {
                if (_activatedSegments != value)
                {
                    _activatedSegments = value;
                    OnPropertyChanged();
                }
            }
        }

        public int TotalSegments
        {
            get => _totalSegments;
            set
            {
                if (_totalSegments != value)
                {
                    _totalSegments = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ActivationDelay
        {
            get => _activationDelay;
            set
            {
                if (_activationDelay != value)
                {
                    _activationDelay = value;
                    OnPropertyChanged();
                }
            }
        }


        public int ContactNumber
        {
            get => _contactNumber;
            set
            {
                if (_contactNumber != value)
                {
                    _contactNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ContactTag
        {
            get => _contactTag;
            set
            {
                if (_contactTag != value)
                {
                    _contactTag = value;
                    OnPropertyChanged();
                    UpdateSegmentsFromTag();
                }
            }
        }

        private void UpdateSegmentsFromTag()
        {
            if (string.IsNullOrEmpty(_contactTag) || _contactTag.ToUpper() == "ПУСТО")
            {
                TotalSegments = 1;
            }
            else
            {
                // Считаем количество сегментов по количеству запятых + 1
                var segments = _contactTag.Split(',').Length;
                TotalSegments = segments;
            }
        }


        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public double RelativeX
        {
            get => _relativeX;
            set
            {
                if (_relativeX != value)
                {
                    _relativeX = value;
                    OnPropertyChanged();
                }
            }
        }

        public double RelativeY
        {
            get => _relativeY;
            set
            {
                if (_relativeY != value)
                {
                    _relativeY = value;
                    OnPropertyChanged();
                }
            }
        }

        public double Diameter
        {
            get => _diameter;
            set
            {
                if (_diameter != value)
                {
                    _diameter = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return $"ContactMarker {ContactNumber}";
        }
    }
}
