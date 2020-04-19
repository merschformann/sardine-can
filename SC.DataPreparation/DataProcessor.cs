using SC.ExecutionHandler;
using SC.ObjectModel.Additionals;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.DataPreparation
{
    public class DataProcessor
    {
        #region Core

        /// <summary>
        /// Creates a new <code>DataProcessor</code> instance.
        /// </summary>
        public DataProcessor() { }

        /// <summary>
        /// Prepares the results of all directories contained in the given directory.
        /// </summary>
        /// <param name="parentDir">The parent directory of the result sub-directories.</param>
        public void PrepareAllResults(string parentDir)
        {
            string[] dirs = Directory.EnumerateDirectories(parentDir).ToArray();
            Dictionary<string, string> footprints = new Dictionary<string, string>();
            Dictionary<string, string> completeInstanceNames = new Dictionary<string, string>();
            Dictionary<string, string> configNames = new Dictionary<string, string>();
            foreach (var dir in dirs)
            {
                // Fetch instance name
                string resultDir = Path.GetFileName(dir);
                if (File.Exists(Path.Combine(dir, Executor.FILENAME_INSTANCE_NAME)))
                {
                    using (StreamReader sr = new StreamReader(Path.Combine(dir, Executor.FILENAME_INSTANCE_NAME)))
                    {
                        string instanceNameLine = "";
                        while (string.IsNullOrWhiteSpace((instanceNameLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                        instanceNameLine = instanceNameLine.Trim();
                        completeInstanceNames[resultDir] = instanceNameLine;
                    }
                }
                else
                {
                    completeInstanceNames[resultDir] = resultDir;
                }
                // Fetch config name
                if (File.Exists(Path.Combine(dir, Executor.FILENAME_CONFIG_NAME)))
                {
                    using (StreamReader sr = new StreamReader(Path.Combine(dir, Executor.FILENAME_CONFIG_NAME)))
                    {
                        string configNameLine = "";
                        while (string.IsNullOrWhiteSpace((configNameLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                        configNameLine = configNameLine.Trim();
                        configNames[resultDir] = configNameLine;
                    }
                }
                // Prepare consolidated results
                if (File.Exists(Path.Combine(dir, Executor.FILENAME_FOOTPRINT)))
                {
                    using (StreamReader sr = new StreamReader(Path.Combine(dir, Executor.FILENAME_FOOTPRINT)))
                    {
                        string footPrintLine = "";
                        while (string.IsNullOrWhiteSpace((footPrintLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                        footprints[resultDir] = footPrintLine;
                    }
                }
                // Prepare internal results (if there is a time horizon available)
                PrepareResults(
                    dir, // The result dir
                    completeInstanceNames.ContainsKey(resultDir) ? completeInstanceNames[resultDir] : "", // The real name of the instance
                    configNames.ContainsKey(resultDir) ? configNames[resultDir] : "" // The name of the config
                    );
            }
            // Write consolidated result file
            Console.WriteLine("Writing consolidated result file.");
            using (StreamWriter sw = new StreamWriter(Path.Combine(parentDir, Executor.FILENAME_CONSOLIDATED_FOOTPRINTS)))
            {
                sw.WriteLine(Executor.GetFootPrintHeader());
                foreach (var kvp in footprints.OrderBy(k => k.Key))
                {
                    sw.WriteLine(kvp.Value); // Add the result footprint
                }
            }
        }

        /// <summary>
        /// Prepares the results of all directories contained in the given directory.
        /// </summary>
        /// <param name="parentDir">The parent directory of the result sub-directories.</param>
        public void PrepareOnlyFootprints(string parentDir)
        {
            string[] dirs = Directory.EnumerateDirectories(parentDir).ToArray();
            Dictionary<string, string> footprints = new Dictionary<string, string>();
            Dictionary<string, string> completeInstanceNames = new Dictionary<string, string>();
            Dictionary<string, string> configNames = new Dictionary<string, string>();
            foreach (var dir in dirs)
            {
                // Fetch instance name
                string resultDir = Path.GetFileName(dir);
                if (File.Exists(Path.Combine(dir, Executor.FILENAME_INSTANCE_NAME)))
                {
                    using (StreamReader sr = new StreamReader(Path.Combine(dir, Executor.FILENAME_INSTANCE_NAME)))
                    {
                        string instanceNameLine = "";
                        while (string.IsNullOrWhiteSpace((instanceNameLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                        instanceNameLine = instanceNameLine.Trim();
                        completeInstanceNames[resultDir] = instanceNameLine;
                    }
                }
                else
                {
                    completeInstanceNames[resultDir] = resultDir;
                }
                // Fetch config name
                if (File.Exists(Path.Combine(dir, Executor.FILENAME_CONFIG_NAME)))
                {
                    using (StreamReader sr = new StreamReader(Path.Combine(dir, Executor.FILENAME_CONFIG_NAME)))
                    {
                        string configNameLine = "";
                        while (string.IsNullOrWhiteSpace((configNameLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                        configNameLine = configNameLine.Trim();
                        configNames[resultDir] = configNameLine;
                    }
                }
                // Prepare consolidated results
                if (File.Exists(Path.Combine(dir, Executor.FILENAME_FOOTPRINT)))
                {
                    using (StreamReader sr = new StreamReader(Path.Combine(dir, Executor.FILENAME_FOOTPRINT)))
                    {
                        string footPrintLine = "";
                        while (string.IsNullOrWhiteSpace((footPrintLine = sr.ReadLine()))) { /* Nothing to do - the head of the loop does the work */ }
                        footprints[resultDir] = footPrintLine;
                    }
                }
            }
            // Write consolidated result file
            Console.WriteLine("Writing consolidated result file.");
            using (StreamWriter sw = new StreamWriter(Path.Combine(parentDir, Executor.FILENAME_CONSOLIDATED_FOOTPRINTS)))
            {
                sw.WriteLine(Executor.GetFootPrintHeader());
                foreach (var kvp in footprints.OrderBy(k => k.Key))
                {
                    sw.WriteLine(kvp.Value); // Add the result footprint
                }
            }
        }

        /// <summary>
        /// Prepares all results of one evaluation run.
        /// </summary>
        /// <param name="path">The path to the directory containing all the evaluation results of one run.</param>
        /// <param name="instanceName">The name of the instance.</param>
        /// <param name="configName">The name of the config.</param>
        public void PrepareResults(string path, string instanceName, string configName)
        {
            // Log
            Console.WriteLine("Preparing results for (" + instanceName + "/" + configName + "):");
            // Generate scripts
            Console.WriteLine("Generating progression scripts ...");
            string script = GenerateProgressionScript(path, instanceName, configName);
            // Execute script
            ExecuteScript(script);
        }

        #endregion

        #region Helper methods

        #region Key sector definition

        /// <summary>
        /// Defines the different sectors of a Gnuplot key.
        /// </summary>
        public enum QuadDirections : int
        {
            /// <summary>
            /// North-West sector.
            /// </summary>
            NW = 0,

            /// <summary>
            /// North-East sector.
            /// </summary>
            NE = 1,

            /// <summary>
            /// South-West sector.
            /// </summary>
            SW = 2,

            /// <summary>
            /// South-East sector.
            /// </summary>
            SE = 3
        }

        #endregion

        #region Gnuplot parameters

        private static string GenerateOutputDefinitionScriptCode()
        {
            return "reset" + ExportationConstants.LINE_TERMINATOR +
                "# Output definition" + ExportationConstants.LINE_TERMINATOR +
                "set terminal postscript clip color eps \"Arial\" 14";
        }

        private static string GenerateParameterScriptCode(QuadDirections keyPosition, string xLabel, string yLabel, bool logX, bool logY, double xMin, double xMax, double yMin, double yMax)
        {
            string keyPositionParam = "right bottom Right";
            switch (keyPosition)
            {
                case QuadDirections.NW:
                    keyPositionParam = "left top Left";
                    break;
                case QuadDirections.NE:
                    keyPositionParam = "right top Right";
                    break;
                case QuadDirections.SW:
                    keyPositionParam = "left bottom Left";
                    break;
                case QuadDirections.SE: // Do nothing - default
                default:
                    break;
            }

            return "# Parameters" + ExportationConstants.LINE_TERMINATOR +
                "set key " + keyPositionParam + ExportationConstants.LINE_TERMINATOR +
                "set xlabel \"" + xLabel + "\"" + ExportationConstants.LINE_TERMINATOR +
                "set ylabel \"" + yLabel + "\"" + ExportationConstants.LINE_TERMINATOR +
                (logX ? "set log x" + ExportationConstants.LINE_TERMINATOR : "") +
                (logY ? "set log y" + ExportationConstants.LINE_TERMINATOR : "") +
                ((double.IsNaN(xMin) || double.IsNaN(xMax)) ? "" : "set xrange [" + xMin + ":" + xMax + "]" + ExportationConstants.LINE_TERMINATOR) +
                ((double.IsNaN(yMin) || double.IsNaN(yMax)) ? "" : "set yrange [" + yMin + ":" + yMax + "]" + ExportationConstants.LINE_TERMINATOR) +
                "set grid" + ExportationConstants.LINE_TERMINATOR +
                "set style fill solid 0.25";
        }

        private static string GenerateLineStyleScriptCode()
        {
            return "# Line-Styles" + ExportationConstants.LINE_TERMINATOR +
                "set style line 1 linetype 1 linecolor rgb \"#474749\" linewidth 3" + ExportationConstants.LINE_TERMINATOR +
                "set style line 2 linetype 1 linecolor rgb \"#7090c8\" linewidth 3" + ExportationConstants.LINE_TERMINATOR +
                "set style line 3 linetype 1 linecolor rgb \"#42b449\" linewidth 3" + ExportationConstants.LINE_TERMINATOR +
                "set style line 4 linetype 1 linecolor rgb \"#f7cb38\" linewidth 3" + ExportationConstants.LINE_TERMINATOR +
                "set style line 5 linetype 1 linecolor rgb \"#db4a37\" linewidth 3";
        }

        private static string GenerateTailScriptCode()
        {
            return "reset" + ExportationConstants.LINE_TERMINATOR + "exit";
        }

        #endregion

        #region Gnuplot plots

        private static string GenerateProgressionScript(
            string outputDirPath,
            string instanceName,
            string configName)
        {
            // Init script path
            string scriptPath = Path.Combine(outputDirPath, Executor.FILENAME_PROGRESSION_SCRIPT_BATCH_FILE);
            // Generate plot script
            using (StreamWriter sw = new StreamWriter(Path.Combine(outputDirPath, Executor.FILENAME_PROGRESSION_SCRIPT)))
            {
                // Preamble
                sw.WriteLine(GenerateOutputDefinitionScriptCode());
                sw.WriteLine(GenerateParameterScriptCode(QuadDirections.NW, "Time (in s)", "Volume utilization", false, false, double.NaN, double.NaN, double.NaN, double.NaN));
                sw.WriteLine(GenerateLineStyleScriptCode());
                sw.WriteLine("set title \"" + instanceName + (string.IsNullOrWhiteSpace(configName) ? "" : " / " + configName) + "\"");
                // Plots
                sw.WriteLine("set ylabel \"Volume utilization\"");
                sw.WriteLine("set output \"progression.eps\"");
                sw.WriteLine("plot \\");
                sw.WriteLine("\"" + Executor.FILENAME_STATUS_OVER_TIME + "\" u 1:2 w steps linestyle 1 t \"Volume utilization\"");
                // Tail
                sw.WriteLine(GenerateTailScriptCode());
            }
            // Generate short batch script
            using (StreamWriter sw = new StreamWriter(scriptPath))
            {
                sw.WriteLine("gnuplot " + Executor.FILENAME_PROGRESSION_SCRIPT);
            }
            return scriptPath;
        }

        #region Gnuplot execution

        private void ExecuteScript(string script, bool changeWorkingDirToScript = true)
        {
            // Check if gnuplot command is available
            string commandName = script.Split(' ')[0];
            if (!CheckCommandExists(commandName))
            {
                Console.WriteLine("Command is not available: " + commandName);
                return;
            }
            // Change dir to the one of the script
            string currentDir = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(Path.GetDirectoryName(script));
            // Execute it
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = Path.GetFileName(script);
            Console.WriteLine("Executing " + startInfo.FileName);
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
            // Change back to first dir
            Directory.SetCurrentDirectory(currentDir);
        }

        #endregion

        #region Additional helpers

        /// <summary>
        /// Checks if a given command is available. If no extension is given, a .exe extension is assumed.
        /// </summary>
        /// <param name="command">The command to check.</param>
        /// <returns><code>true</code> if the command is available, <code>false</code> otherwise.</returns>
        private static bool CheckCommandExists(string command)
        {
            if (!Path.HasExtension(command))
                command = command + ".exe";
            if (File.Exists(command))
                return true;
            string[] paths = Environment.GetEnvironmentVariable("PATH").Split(';');
            foreach (var path in paths)
                if (File.Exists(Path.Combine(path, command)))
                    return true;
            return false;
        }

        /// <summary>
        /// Counts the elements of a list within the given range.
        /// </summary>
        /// <param name="elements">The list of elements already in sorted order.</param>
        /// <param name="lb">The lower bound of the range.</param>
        /// <param name="lbInclusive">Indicates whether the lower bound is inclusive.</param>
        /// <param name="ub">The upper bound of the range.</param>
        /// <param name="ubInclusive">Indicates whether the upper bound is inclusive.</param>
        /// <returns>The number of elements within the given range.</returns>
        private static int CountElementsWithinRange(List<double> elements, double lb, bool lbInclusive, double ub, bool ubInclusive)
        {
            // Check bounds
            if (lb > ub)
                throw new ArgumentException("Lower bound has to be smaller than upper bound.");
            // Check inclusive or exclusive
            if (!lbInclusive)
                lb += double.Epsilon;
            if (!ubInclusive)
                ub += double.Epsilon;
            // Get indexes of the lower and upper bound to calculate elements within range
            int lbIndex = elements.Count / 2;
            int ubIndex = elements.Count / 2;
            lbIndex = elements.BinarySearch(lb);
            ubIndex = elements.BinarySearch(ub);
            // Keep in bounds of array (in case the element was not found)
            if (lbIndex < 0)
                lbIndex = ~lbIndex;
            if (ubIndex < 0)
                ubIndex = ~ubIndex;
            for (int i = lbIndex; i >= 0 && i < elements.Count && lb <= elements[i]; i--)
                lbIndex = i;
            for (int i = ubIndex; i < elements.Count && elements[i] <= ub; i++)
                ubIndex = i;
            // Calculate and return the elements within range
            int elementsInRange = ubIndex - lbIndex;
            return elementsInRange;
        }

        #endregion

        #endregion

        #endregion
    }
}
