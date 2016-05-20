using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CBC.Sync
{
    public class Cbc
    {
        public String Note { get; set; }
        public List<CbcBeer> Beers { get; } = new List<CbcBeer>();
    }
}
