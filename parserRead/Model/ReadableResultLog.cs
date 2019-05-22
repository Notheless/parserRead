using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace parserRead.Model
{
    class ReadableResultLog
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int NumberOfHits { get; set; }
        public double Damage { get; set; }
        public double Heal { get; set; }
        public double DPS { get; set; }
        public int NumberOfCrit { get; set; }
        public int NumberOfJA { get; set; }

        public DateTime Start { get; set; }
        public DateTime LastHit { get; set; }
        public TimeSpan TimeLenght { get
            {
                return (Start - LastHit);
            }
        }
        public List<playersSkillLog> PlayersSkill { get; set; }
    }
}
