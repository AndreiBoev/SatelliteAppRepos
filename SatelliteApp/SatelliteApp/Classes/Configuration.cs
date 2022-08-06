using System.Collections.Generic;

namespace SatelliteApp.Classes
{
    public class Configuration
    {
        public string Name { get; set; }
        public List<Parameter> Parameters { get; set; } = new List<Parameter>();
        public Parameter X;
        public Parameter Y;
    }
}
