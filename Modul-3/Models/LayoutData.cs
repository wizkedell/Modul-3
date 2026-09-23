using System.Collections.Generic;
using System.Windows;

namespace Modul_3.Models
{
    public class LayoutData
    {
        public string ConnectorName { get; set; }
        public string ImagePath { get; set; }
        public Size ImageSize { get; set; }
        public List<ContactPosition> ContactPositions { get; set; } = new List<ContactPosition>();
    }

    public class ContactPosition
    {
        public int ContactNumber { get; set; }
        public double RelativeX { get; set; }
        public double RelativeY { get; set; }
        public string ContactTag { get; set; }
        public double Diameter { get; set; } = 30; // Добавляем размер маркера
    }
}
