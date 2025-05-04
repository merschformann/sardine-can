using SC.Heuristics.PrimalHeuristic;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Interfaces;
using SC.Linear;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SC.Playground.Lib
{
    internal class PlaygroundFunctions
    {
        public static void ExportConfigs()
        {
            foreach (var method in Enum.GetValues(typeof(MethodType)).Cast<MethodType>())
            {
                Configuration config = new Configuration(method, true) { Name = method + "Default" };
                config.TimeLimit = TimeSpan.FromMinutes(5);
                config.Write(config.Name);
            }
        }

        /// <summary>
        /// Exports all models in all formulations from the given directories
        /// </summary>
        public static void ExportMPSModels(string modelDirPath)
        {
            int counter = 0;
            string fileEnding = ".mps";
            string exportDir = "ExportedMPS";
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }
            foreach (var file in Directory.EnumerateFiles(Path.Combine(Directory.GetCurrentDirectory(), modelDirPath), "*.xinst"))
            {
                string instanceName = Path.GetFileNameWithoutExtension(file);
                Instance instance = Instance.ReadXML(file);
                Configuration configFLB = new Configuration(MethodType.FrontLeftBottomStyle, true) { /* No gravity here */ HandleGravity = false, /* No stackability here */ HandleStackability = false, HandleCompatibility = true, HandleForbiddenOrientations = true, HandleRotatability = true };
                Configuration configTetris = new Configuration(MethodType.TetrisStyle, true) { /* No gravity here */ HandleGravity = false, /* No stackability here */ HandleStackability = false, HandleCompatibility = true, HandleForbiddenOrientations = true, HandleRotatability = true };
                Configuration configHybrid = new Configuration(MethodType.HybridStyle, true) { /* No gravity here */ HandleGravity = false, /* No stackability here */ HandleStackability = false, HandleCompatibility = true, HandleForbiddenOrientations = true, HandleRotatability = true };
                Console.WriteLine("Exporting (" + (++counter) + "/flb) " + Path.GetFileNameWithoutExtension(file));
                LinearModelFLB transFLB = new LinearModelFLB(instance, configFLB);
                transFLB.ExportMPS(Path.Combine(exportDir, instanceName + "-flb" + fileEnding));
                Console.WriteLine("Exporting (" + (++counter) + "/tetris) " + Path.GetFileNameWithoutExtension(file));
                LinearModelTetris transTetris = new LinearModelTetris(instance, configTetris);
                transTetris.ExportMPS(Path.Combine(exportDir, instanceName + "-tetris" + fileEnding));
                Console.WriteLine("Exporting (" + (++counter) + "/hybrid) " + Path.GetFileNameWithoutExtension(file));
                LinearModelHybrid transHybrid = new LinearModelHybrid(instance, configHybrid);
                transHybrid.ExportMPS(Path.Combine(exportDir, instanceName + "-hybrid" + fileEnding));
            }
        }
    }
}
