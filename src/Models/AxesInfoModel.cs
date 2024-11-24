using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoChemistryNexus.Models
{
    public class AxisInfoModel
    {
        public string Name { get; set; }
        public object Axis { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
