﻿using SC.Heuristics.PrimalHeuristic;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Elements;
using SC.ObjectModel.Generator;
using SC.ObjectModel.Interfaces;
using SC.Playground.Lib;
using SC.Linear;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using SC.ObjectModel.IO;

namespace SC.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            // Choose option
            Console.WriteLine(">>> Choose option: ");
            Console.WriteLine("0: Experimental");
            char optionKey = Console.ReadKey().KeyChar; Console.WriteLine();
            switch (optionKey)
            {

                case '1': { } break;
                case '2': { } break;
                case '3': { } break;
                case '4': { } break;
                case '5': { } break;
                case '6': { } break;
                case '7': { } break;
                case '8': { } break;
                case '9': { } break;
                case '0': { Experimental(); } break;
                default: break;
            }
            Console.WriteLine(".Fin.");
        }

        static void Experimental()
        {
            Console.WriteLine("Create JSON configuration ...");
            Configuration config = new Configuration(MethodType.ExtremePointInsertion, false);
            var configString = JsonIO.To(config);
            Console.WriteLine("Write JSON configuration to disk ...");
            File.WriteAllText("configuration.json", configString);
            // Console.WriteLine("Creating JSON ...");
            // JsonHelpers.CreateJson();
            // Console.WriteLine("Parsing JSON ...");
            // JsonHelpers.ParseJson();
        }
    }
}
