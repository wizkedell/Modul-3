using System.Collections.Generic;


namespace Modul_3.Models
{
    public class Product
    {
        public string Name { get; set; }
        public string ConnectorsFolderPath { get; set; }
        public List<Connector> Connectors { get; set; } = new List<Connector>();
    }
}
