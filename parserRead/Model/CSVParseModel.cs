using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parserRead.Model
{
    public class CSVParseModel
    {
        public string timestamp{ get; set; }
        public string sourceID{ get; set; }
        public string sourceName{ get; set; }
        public string targetID{ get; set; }
        public string targetName{ get; set; }
        public string attackID{ get; set; }
        public string damage{ get; set; }
        public string IsJA{ get; set; }
        public string IsCrit{ get; set; }
        public string IsMultiHit{ get; set; }
        public string IsMisc{ get; set; }
        public string IsMisc2{ get; set; }
        public string instanceID { get; set; }

    }
}
