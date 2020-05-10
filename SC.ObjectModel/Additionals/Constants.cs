using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Defines basic constants which need to be used across the project
    /// </summary>
    public class ExportationConstants
    {
        /// <summary>
        /// The default directory for file-exportation
        /// </summary>
        public const string ExportDir = "Output";

        /// <summary>
        /// The basic formatter to use for string-representations of values
        /// </summary>
        public static CultureInfo FORMATTER = CultureInfo.InvariantCulture;

        /// <summary>
        /// A format pattern to use when shortened string-representations of values are desired
        /// </summary>
        public const string EXPORT_FORMAT_SHORT = "F";

        /// <summary>
        /// The delimiter to use when building string identifiers for objects
        /// </summary>
        public const char STRING_IDENTIFIER_DELIMITER = ';';

        /// <summary>
        /// The delimiter to use when exporting csv-files
        /// </summary>
        public const char CSV_DELIMITER = ';';

        /// <summary>
        /// The delimiter to use when exporting files to use with gnuplot.
        /// </summary>
        public const char GNUPLOT_DELIMITER = ' ';

        /// <summary>
        /// Line termination character
        /// </summary>
        public const string LINE_TERMINATOR = "\n";

        /// <summary>
        /// The default formatter when exporting / importing xml files
        /// </summary>
        public static CultureInfo XML_FORMATTER = CultureInfo.InvariantCulture;

        /// <summary>
        /// The default pattern when exporting / importing xml files
        /// </summary>
        public const string XML_PATTERN = "";

        /// <summary>
        /// The delimiter to use when separating values in attributes of xml-files
        /// </summary>
        public const char XML_ATTRIBUTE_DELIMITER = ';';

        /// <summary>
        /// The ident-name of a piece node
        /// </summary>
        public const string XML_PIECE_IDENT = "Piece";

        /// <summary>
        /// The ident-name of a piece-collection node
        /// </summary>
        public const string XML_PIECE_COLLECTION_IDENT = "Pieces";

        /// <summary>
        /// The ident-name of a virtual piece node
        /// </summary>
        public const string XML_VIRTUAL_PIECE_IDENT = "VirtualPiece";

        /// <summary>
        /// The ident-name of a virtual piece-collection node
        /// </summary>
        public const string XML_VIRTUAL_PIECE_COLLECTION_IDENT = "VirtualPieces";

        /// <summary>
        /// The ident-name of a slant node
        /// </summary>
        public const string XML_SLANT_IDENT = "Slant";

        /// <summary>
        /// The ident-name of a slant-collection node
        /// </summary>
        public const string XML_SLANT_COLLECTION_IDENT = "Slants";

        /// <summary>
        /// The ident-name of the position of a plane
        /// </summary>
        public const string XML_SLANT_POSITION_IDENT = "Position";

        /// <summary>
        /// The ident-name of the normal vector of a plane
        /// </summary>
        public const string XML_SLANT_NORMAL_VECTOR_IDENT = "Normal";

        /// <summary>
        /// The ident-name of a container node
        /// </summary>
        public const string XML_CONTAINER_IDENT = "Container";

        /// <summary>
        /// The ident-name of a container-collection node
        /// </summary>
        public const string XML_CONTAINER_COLLECTION_IDENT = "Containers";

        /// <summary>
        /// The ident-name of a MeshCube node
        /// </summary>
        public const string XML_MESHCUBE_IDENT = "Cube";

        /// <summary>
        /// The ident-name of a MeshCube-collection node
        /// </summary>
        public const string XML_MESHCUBE_COLLECTION_IDENT = "Cubes";

        /// <summary>
        /// The ident-name of a components-set node
        /// </summary>
        public const string XML_COMPONENTS_SET_IDENT = "Components";

        /// <summary>
        /// The ident-name of a point node
        /// </summary>
        public const string XML_POINT_IDENT = "Point";

        /// <summary>
        /// The version attribute name
        /// </summary>
        public const string XML_VERSION_IDENT = "Version";

        /// <summary>
        /// The current version of the xml export
        /// </summary>
        public const string XML_VERSION = "1.1";

        /// <summary>
        /// The ident-name of a instance node
        /// </summary>
        public const string XML_INSTANCE_IDENT = "Instance";

        /// <summary>
        /// The ident-name of a solution node
        /// </summary>
        public const string XML_SOLUTION_IDENT = "Solution";

        /// <summary>
        /// The ident-name of a solution-collection node
        /// </summary>
        public const string XML_SOLUTION_COLLECTION_IDENT = "Solutions";

        /// <summary>
        /// The ident-name of a bin-node which depicts the content of a container in a solution
        /// </summary>
        public const string XML_BIN_IDENT = "Bin";

        /// <summary>
        /// The ident-name of a bin-collection node
        /// </summary>
        public const string XML_BIN_COLLECTION_IDENT = "Bins";

        /// <summary>
        /// The ident-name of an item-node which depicts attributes of a piece in a solution
        /// </summary>
        public const string XML_ITEM_IDENT = "Item";

        /// <summary>
        /// The ident-name of an orientation-node depicting the orientation of a piece inside a solution
        /// </summary>
        public const string XML_ORIENTATION_IDENT = "Orientation";
    }

    /// <summary>
    /// Defines all constants used when decribing meshes
    /// </summary>
    public class MeshConstants
    {
        /// <summary>
        /// Defines the different vertices by ID.
        /// Defined as follows: (Viewing from the origin of coordinates, meaning left is a lesser x than right, front is a lesser y than back and bottom is a lesser z than top): 
        /// (0 = centre, 1 = FLB, 2 = FRB, 3 = BLB, 4 = BRB, 5 = FLT, 6 = FRT, 7 = BLT, 8 = BRT)
        /// </summary>
        public static readonly int[] VERTEX_IDS = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        /// <summary>
        /// Defines the different vertices by ID asa they are used by the hybrid model.
        /// Defined as follows: (Viewing from the origin of coordinates, meaning left is a lesser x than right, front is a lesser y than back and bottom is a lesser z than top): 
        /// (1 = FLB, 8 = BRT)
        /// </summary>
        public static readonly int[] VERTEX_IDS_HYBRID_SUBSET = { 1, 8 };

        /// <summary>
        /// Defines the different possible orientations by ID. (0-23)
        /// </summary>
        public static readonly int[] ORIENTATIONS = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23 };

        /// <summary>
        /// Defines the necessary orientations-IDs to achieve all unique orientations of a parallelepiped
        /// </summary>
        public static readonly int[] ORIENTATIONS_PARALLELEPIPED_SUBSET = { 0, 1, 4, 5, 16, 17 };

        /// <summary>
        /// Defines the orientations allowed when a piece is declared as 'this side up'
        /// </summary>
        public static readonly int[] ORIENTATIONS_THIS_SIDE_UP = { 0, 1, 2, 3 };

        /// <summary>
        /// Defines the dimensions by ID. (x = 0, y = 1, z = 2)
        /// </summary>
        public static readonly int[] DIMENSION_IDS = { 0, 1, 2 };

        /// <summary>
        /// Defines the set of vertex IDs which defines left endpoints regarding x
        /// </summary>
        public static readonly HashSet<int> VERTEX_IDS_LEFT_ENDPOINTS_X = new HashSet<int>(new int[] { 1, 3, 5, 7 });

        /// <summary>
        /// Defines the set of vertex IDs which defines left endpoints regarding y
        /// </summary>
        public static readonly HashSet<int> VERTEX_IDS_LEFT_ENDPOINTS_Y = new HashSet<int>(new int[] { 1, 2, 5, 6 });

        /// <summary>
        /// Defines the set of vertex IDs which defines left endpoints regarding z
        /// </summary>
        public static readonly HashSet<int> VERTEX_IDS_LEFT_ENDPOINTS_Z = new HashSet<int>(new int[] { 1, 2, 3, 4 });
    }

    /// <summary>
    /// Defines the different dimensions as an enumeration
    /// </summary>
    public enum DimensionMarker
    {
        /// <summary>
        /// The x dimension
        /// </summary>
        X,

        /// <summary>
        /// The y dimension
        /// </summary>
        Y,

        /// <summary>
        /// The z dimension
        /// </summary>
        Z
    }
}
