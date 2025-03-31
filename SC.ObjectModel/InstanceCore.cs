using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;
using SC.ObjectModel.Interfaces;
using SC.ObjectModel.IO.Json;
using SC.ObjectModel.Rules;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;

namespace SC.ObjectModel
{
    /// <summary>
    /// Defines the basic problem instance
    /// </summary>
    public partial class Instance
    {
        /// <summary>
        /// The name of the instance
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The randomizer of this instance.
        /// </summary>
        public Random Random { get; set; } = new Random(0);

        /// <summary>
        /// Contains all pieces which are part of this instance
        /// </summary>
        public List<VariablePiece> Pieces = new List<VariablePiece>();

        /// <summary>
        /// Contains all pieces including possible virtual ones
        /// </summary>
        public IEnumerable<Piece> PiecesWithVirtuals { get { return Pieces.Cast<Piece>().Concat(Containers.SelectMany(c => c.VirtualPieces)); } }

        /// <summary>
        /// Contains all containers which are part of this instance
        /// </summary>
        public List<Container> Containers = new List<Container>();

        /// <summary>
        /// Contains all rule definitions to adhere to when solving the problem instance.
        /// </summary>
        public RuleSet Rules { get; set; } = new RuleSet();

        /// <summary>
        /// Writes out basic information about the instance
        /// </summary>
        /// <param name="tw">The writer to use</param>
        public void OutputInfo(TextWriter tw)
        {
            if (Containers != null)
            {
                tw.WriteLine("Container:");
                foreach (var container in Containers.Where(c => c != null).OrderBy(c => c.ID))
                    tw.WriteLine(container.ToString());
            }
            if (Pieces != null)
            {
                tw.WriteLine("Pieces:");
                foreach (var piece in Pieces.Where(c => c != null).OrderBy(p => p.ID))
                    tw.WriteLine(piece.ToString());
            }
        }

        /// <summary>
        /// Returns a semi-ident which can be used to name the instance or identify it later
        /// </summary>
        /// <returns>The ident string.</returns>
        public string GetIdent()
        {
            if (Containers.Count == 0)
                throw new InvalidOperationException("Instance is empty!");

            // Build the ident string
            return
                Containers.Count() + "-" +
                Containers.Average(c => c.Mesh.Length).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "-" +
                Containers.Average(c => c.Mesh.Width).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "-" +
                Containers.Average(c => c.Mesh.Height).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "-" +
                Pieces.Count() + "-" +
                ((!Pieces.Any()) ?
                    (0.0).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) :
                    Pieces.Min(p => Math.Min(Math.Min(p.Original.BoundingBox.Length, p.Original.BoundingBox.Width), p.Original.BoundingBox.Height))
                    .ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER)) + "-" +
                ((!Pieces.Any()) ?
                    (0.0).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) :
                    Pieces.Max(p => Math.Max(Math.Max(p.Original.BoundingBox.Length, p.Original.BoundingBox.Width), p.Original.BoundingBox.Height))
                    .ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER)) +
                (Pieces.Any(p => p.Original.Components.Count > 1) ?
                    "-tetris-" + Pieces.Average(p => p.Original.Components.Count).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) :
                    "");
        }

        /// <summary>
        /// All solutions supplied for this instance
        /// </summary>
        public HashSet<COSolution> Solutions = new HashSet<COSolution>();

        /// <summary>
        /// Counts the solutions created for this instance
        /// </summary>
        private int _solutionCounter = 0;

        /// <summary>
        /// Creates a new solution and registers it to the instance
        /// </summary>
        /// <param name="unofficial">Indicates whether to keep track of the created solution or not</param>
        /// <param name="config">The configuration to use</param>
        /// <returns>The newly created solution</returns>
        public COSolution CreateSolution(Configuration.Configuration config, bool unofficial = false)
        {
            COSolution solution = new COSolution(this, config, Random);
            if (!unofficial)
            {
                solution.ID = ++_solutionCounter;
                Solutions.Add(solution);
            }
            return solution;
        }
    }
}
