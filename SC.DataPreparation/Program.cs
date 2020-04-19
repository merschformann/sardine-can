using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.DataPreparation
{
    class Program
    {
        static void Main(string[] args)
        {
            // Say hello
            Console.WriteLine("<<< Welcome to the SardineCan DataPreparator >>>");

            // Init
            DataProcessor preparer = new DataProcessor();

            // Use argument path if available
            if (args.Length == 1)
                preparer.PrepareAllResults(args[0]);
            else
            {
                // Read path
                Console.WriteLine("Enter the path to the root result folder:");
                string path = Console.ReadLine();
                // Determine all results / only footprint condensation
                Console.WriteLine("Plot graphs? (y/n)");
                char plotGraphKey = Console.ReadKey().KeyChar;
                Console.WriteLine();
                if (char.ToLower(plotGraphKey) == 'y')
                    preparer.PrepareAllResults(path);
                else
                    preparer.PrepareOnlyFootprints(path);
            }

            // End
            Console.WriteLine(".Fin.");
            Console.ReadLine();
        }
    }
}
