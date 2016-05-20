using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CBC.Sync.Console
{
    public class Program
    {
        static void Main(string[] args)
        {
            Engine.Initialize();
            Engine.SyncFromCsv();
        }
    }
}
