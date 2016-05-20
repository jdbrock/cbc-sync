using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBC.Sync
{
    public class CbcBeer
    {
        public String Id { get; set; }

        public String BreweryName { get; set; }
        public String BeerName { get; set; }
        public Decimal ABV { get; set; }
        public String StyleName { get; set; }

        public String SessionName { get; set; }
        public Int32 SessionNumber { get; set; }

        public override string ToString()
        {
            return $"{BreweryName} - {BeerName}";
        }
    }
}
