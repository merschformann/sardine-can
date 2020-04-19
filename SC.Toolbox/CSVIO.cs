using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Toolbox
{
    /// <summary>
    /// Contains auxiliary CSV I/O functionality.
    /// </summary>
    public class CSVIO
    {
        /// <summary>
        /// Reads CSV data from a given file.
        /// </summary>
        /// <param name="filename">The file to read.</param>
        /// <param name="delimiter">The delimiter for splitting the lines (elements of a line will also be trimmed).</param>
        /// <returns>All split lines read from the file (first line may be the header).</returns>
        public static List<string[]> ReadCSV(string filename, char delimiter, Action<string> logger)
        {
            // Read data
            List<string[]> data = File.ReadAllLines(filename).Select(l => l.Split(delimiter).Select(e => e.Trim()).ToArray()).Where(l => l.Length > 0).ToList();
            // Small sanity check
            if (!data.Any())
                throw new InvalidDataException("File does not contain any data: " + filename);
            if (!data.All(l => l.Length == data.First().Length))
                throw new InvalidDataException("File column count inconsistent across rows! E.g.: min: " + data.Min(l => l.Length) + " max: " + data.Max(l => l.Length));
            logger?.Invoke("Read " + data.Count + " lines of data with " + data.First().Length + " columns from file " + filename);
            // Return it
            return data;
        }
    }
}
