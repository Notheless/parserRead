using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parserRead.Model
{
    class ConfigurationFile
    {
        public string FolderPath { get; set; }
        public long ResetTimer { get; set; }
        public bool ResetTrigger { get; set; }
        public bool HighLight { get; set; }
    }
}
