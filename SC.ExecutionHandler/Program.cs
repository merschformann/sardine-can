using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ExecutionHandler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 4)
                Executor.Execute(args[0], args[1], args[2], args[3]);
            else
                Console.WriteLine("Usage: SC.ExecutionHandler <instanceFile> <configFile> <outputDirectory> <seedNumber>");
        }
    }
}
