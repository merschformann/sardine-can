using SC.ObjectModel.Elements;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Encapsulates the generation of all necessary information regarding the different orientations of pieces
    /// </summary>
    public class VertexGenerator
    {
        /// <summary>
        /// Generates the vertex information for a container
        /// </summary>
        /// <param name="container">The container for which the vertex information shall get generated</param>
        public static void GenerateVertexInformation(Container container)
        {
            // Rotate vertices
            foreach (var vertexID in MeshConstants.VERTEX_IDS)
            {
                container.Mesh[vertexID] = GenerateVertex(container.Mesh, vertexID);
            }
        }

        /// <summary>
        /// Generates the vertex corresponding to the supplied ID
        /// </summary>
        /// <param name="mesh">The mesh for which the vertex shall be generated</param>
        /// <param name="vertexID">The ID of the vertex</param>
        /// <returns>The generated vertex</returns>
        private static MeshPoint GenerateVertex(MeshCube mesh, int vertexID)
        {
            // Initiate vertex point
            MeshPoint point = null;
            switch (vertexID)
            {
                case 0:
                    {
                        // Center
                        point = new MeshPoint()
                        {
                            VertexID = 0,
                            X = mesh.Length / 2.0,
                            Y = mesh.Width / 2.0,
                            Z = mesh.Height / 2.0
                        };
                    }
                    break;
                case 1:
                    {
                        // Front left bottom
                        point = new MeshPoint()
                        {
                            VertexID = 1,
                            X = 0,
                            Y = 0,
                            Z = 0
                        };
                    }
                    break;
                case 2:
                    {
                        // Front right bottom
                        point = new MeshPoint()
                        {
                            VertexID = 2,
                            X = mesh.Length,
                            Y = 0,
                            Z = 0
                        };
                    }
                    break;
                case 3:
                    {
                        // Rear left bottom
                        point = new MeshPoint()
                        {
                            VertexID = 3,
                            X = 0,
                            Y = mesh.Width,
                            Z = 0
                        };
                    }
                    break;
                case 4:
                    {
                        // Rear right bottom
                        point = new MeshPoint()
                        {
                            VertexID = 4,
                            X = mesh.Length,
                            Y = mesh.Width,
                            Z = 0
                        };
                    }
                    break;
                case 5:
                    {
                        // Front left top
                        point = new MeshPoint()
                        {
                            VertexID = 5,
                            X = 0,
                            Y = 0,
                            Z = mesh.Height
                        };
                    }
                    break;
                case 6:
                    {
                        // Front right top
                        point = new MeshPoint()
                        {
                            VertexID = 6,
                            X = mesh.Length,
                            Y = 0,
                            Z = mesh.Height
                        };
                    }
                    break;
                case 7:
                    {
                        // Rear left top
                        point = new MeshPoint()
                        {
                            VertexID = 7,
                            X = 0,
                            Y = mesh.Width,
                            Z = mesh.Height
                        };
                    }
                    break;
                case 8:
                    {
                        // Rear right top
                        point = new MeshPoint()
                        {
                            VertexID = 8,
                            X = mesh.Length,
                            Y = mesh.Width,
                            Z = mesh.Height
                        };
                    }
                    break;
                default:
                    throw new ArgumentException("No vertex with ID " + vertexID + " defined");
            }
            return point;
        }

        /// <summary>
        /// Generates the meshes for all orientations
        /// </summary>
        /// <param name="piece">The piece to generate the information for</param>
        internal static void GenerateMeshesForAllOrientations(Piece piece)
        {
            GenerateVertexInformation(piece);
        }

        /// <summary>
        /// Generates the vertex information and side lengths for all components of a piece according to all orientations
        /// </summary>
        /// <param name="piece">The piece to generate the information for</param>
        private static void GenerateVertexInformation(Piece piece)
        {
            // Init
            List<Matrix> rotationMatrices = RotationMatrices.GetRotationMatrices();
            int index = 0;
            Dictionary<int, Matrix> _rotationMatrices = new Dictionary<int, Matrix>();
            foreach (var matrix in rotationMatrices)
            {
                _rotationMatrices[index++] = matrix;
            }

            // Create vertex info for original
            foreach (var component in piece.Original.Components)
            {
                foreach (var vertexID in MeshConstants.VERTEX_IDS)
                {
                    component[vertexID] = GenerateVertex(vertexID, component, piece);
                }
            }
            foreach (var vertexID in MeshConstants.VERTEX_IDS)
            {
                piece.Original.BoundingBox[vertexID] = GenerateVertex(piece.Original.BoundingBox, vertexID);
            }

            // Create clones for the different orientations
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                piece[orientation] = piece.Original.Clone();
            }

            // Rotate vertices
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                foreach (var component in piece[orientation].Components)
                {
                    // Calculate rotated sides
                    Matrix referenceSidesVector = new Matrix(1, 3);
                    referenceSidesVector[0, 0] = component.Length;
                    referenceSidesVector[0, 1] = component.Width;
                    referenceSidesVector[0, 2] = component.Height;
                    Matrix rotatedSidesVector = referenceSidesVector * _rotationMatrices[orientation];
                    double length = rotatedSidesVector[0, 0];
                    double width = rotatedSidesVector[0, 1];
                    double height = rotatedSidesVector[0, 2];

                    // Calculate rotated vertices
                    Dictionary<int, MeshPoint> referencePoints = MeshConstants.VERTEX_IDS.ToDictionary(k => k, v => GenerateVertex(v, component, piece));
                    foreach (var vertexID in MeshConstants.VERTEX_IDS)
                    {
                        MeshPoint referencePoint = referencePoints[vertexID];
                        Matrix referencePointVector = new Matrix(1, 3);
                        referencePointVector[0, 0] = referencePoint.X;
                        referencePointVector[0, 1] = referencePoint.Y;
                        referencePointVector[0, 2] = referencePoint.Z;
                        Matrix rotatedPointVector = referencePointVector * _rotationMatrices[orientation];
                        component[vertexID] = new MeshPoint() { ParentPiece = piece };
                        component[vertexID].X = rotatedPointVector[0, 0];
                        component[vertexID].Y = rotatedPointVector[0, 1];
                        component[vertexID].Z = rotatedPointVector[0, 2];
                    }

                    // Set sides
                    component.Length = length;
                    component.Width = width;
                    component.Height = height;
                }
            }

            // Transform all coordinates into the first octant
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                double minVertexX = 0;
                double minVertexY = 0;
                double minVertexZ = 0;
                foreach (var component in piece[orientation].Components)
                {
                    foreach (var vertexID in MeshConstants.VERTEX_IDS)
                    {
                        if (component[vertexID].X < minVertexX)
                        {
                            minVertexX = component[vertexID].X;
                        }
                        if (component[vertexID].Y < minVertexY)
                        {
                            minVertexY = component[vertexID].Y;
                        }
                        if (component[vertexID].Z < minVertexZ)
                        {
                            minVertexZ = component[vertexID].Z;
                        }
                    }
                }
                foreach (var component in piece[orientation].Components)
                {
                    foreach (var vertexID in MeshConstants.VERTEX_IDS)
                    {
                        component[vertexID].X += Math.Abs(minVertexX);
                        component[vertexID].Y += Math.Abs(minVertexY);
                        component[vertexID].Z += Math.Abs(minVertexZ);
                    }
                }
            }
            // No negative side-lengths
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                foreach (var component in piece[orientation].Components)
                {
                    component.Length = Math.Abs(component.Length);
                    component.Width = Math.Abs(component.Width);
                    component.Height = Math.Abs(component.Height);
                }
            }
            // Update relative positions
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                foreach (var component in piece[orientation].Components)
                {
                    component.RelPosition.X = component.Vertices.Min(v => v.X);
                    component.RelPosition.Y = component.Vertices.Min(v => v.Y);
                    component.RelPosition.Z = component.Vertices.Min(v => v.Z);
                }
            }
            // Recalibrate vertex IDs
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                foreach (var component in piece[orientation].Components)
                {
                    List<double> x = new List<double>(MeshConstants.VERTEX_IDS.Where(vid => vid != 0).Select(vid => component[vid].X));
                    List<double> y = new List<double>(MeshConstants.VERTEX_IDS.Where(vid => vid != 0).Select(vid => component[vid].Y));
                    List<double> z = new List<double>(MeshConstants.VERTEX_IDS.Where(vid => vid != 0).Select(vid => component[vid].Z));
                    // vid 1
                    component[1].VertexID = 1;
                    component[1].X = x.Min();
                    component[1].Y = y.Min();
                    component[1].Z = z.Min();
                    // vid 2
                    component[2].VertexID = 2;
                    component[2].X = x.Max();
                    component[2].Y = y.Min();
                    component[2].Z = z.Min();
                    // vid 3
                    component[3].VertexID = 3;
                    component[3].X = x.Min();
                    component[3].Y = y.Max();
                    component[3].Z = z.Min();
                    // vid 4
                    component[4].VertexID = 4;
                    component[4].X = x.Max();
                    component[4].Y = y.Max();
                    component[4].Z = z.Min();
                    // vid 5
                    component[5].VertexID = 5;
                    component[5].X = x.Min();
                    component[5].Y = y.Min();
                    component[5].Z = z.Max();
                    // vid 6
                    component[6].VertexID = 6;
                    component[6].X = x.Max();
                    component[6].Y = y.Min();
                    component[6].Z = z.Max();
                    // vid 7
                    component[7].VertexID = 7;
                    component[7].X = x.Min();
                    component[7].Y = y.Max();
                    component[7].Z = z.Max();
                    // vid 8
                    component[8].VertexID = 8;
                    component[8].X = x.Max();
                    component[8].Y = y.Max();
                    component[8].Z = z.Max();
                }
            }

            // Seal the orientation clones
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                piece[orientation].Seal();
            }

            // Create vertex info for bounding box of original
            foreach (var vertexID in MeshConstants.VERTEX_IDS)
            {
                piece.Original.BoundingBox[vertexID] = GenerateVertex(vertexID, piece.Original.BoundingBox, piece);
            }

            // Create vertex info for all orientations bounding boxes
            foreach (var orientation in MeshConstants.ORIENTATIONS)
            {
                foreach (var vertexID in MeshConstants.VERTEX_IDS)
                {
                    piece[orientation].BoundingBox[vertexID] = GenerateVertex(vertexID, piece[orientation].BoundingBox, piece);
                }
            }
        }

        /// <summary>
        /// Generates the given vertex for the given component of a piece
        /// </summary>
        /// <param name="piece">The piece the component belongs to</param>
        /// <param name="vertexID">The ID of the vertex to generate</param>
        /// <param name="component">The component the vertex is generated for</param>
        /// <returns>The newly generated vertex</returns>
        public static MeshPoint GenerateVertex(int vertexID, MeshCube component, Piece piece)
        {
            // Initiate vertex point
            MeshPoint point = null;
            switch (vertexID)
            {
                case 0:
                    {
                        // Center
                        point = new MeshPoint()
                        {
                            VertexID = 0,
                            ParentPiece = piece,
                            X = component.RelPosition.X + (component.Length / 2.0),
                            Y = component.RelPosition.Y + (component.Width / 2.0),
                            Z = component.RelPosition.Z + (component.Height / 2.0),
                        };
                    }
                    break;
                case 1:
                    {
                        // Front left bottom
                        point = new MeshPoint()
                        {
                            VertexID = 1,
                            ParentPiece = piece,
                            X = component.RelPosition.X,
                            Y = component.RelPosition.Y,
                            Z = component.RelPosition.Z,
                        };
                    }
                    break;
                case 2:
                    {
                        // Front right bottom
                        point = new MeshPoint()
                        {
                            VertexID = 2,
                            ParentPiece = piece,
                            X = component.RelPosition.X + component.Length,
                            Y = component.RelPosition.Y,
                            Z = component.RelPosition.Z,
                        };
                    }
                    break;
                case 3:
                    {
                        // Rear left bottom
                        point = new MeshPoint()
                        {
                            VertexID = 3,
                            ParentPiece = piece,
                            X = component.RelPosition.X,
                            Y = component.RelPosition.Y + component.Width,
                            Z = component.RelPosition.Z,
                        };
                    }
                    break;
                case 4:
                    {
                        // Rear right bottom
                        point = new MeshPoint()
                        {
                            VertexID = 4,
                            ParentPiece = piece,
                            X = component.RelPosition.X + component.Length,
                            Y = component.RelPosition.Y + component.Width,
                            Z = component.RelPosition.Z,
                        };
                    }
                    break;
                case 5:
                    {
                        // Front left top
                        point = new MeshPoint()
                        {
                            VertexID = 5,
                            ParentPiece = piece,
                            X = component.RelPosition.X,
                            Y = component.RelPosition.Y,
                            Z = component.RelPosition.Z + component.Height,
                        };
                    }
                    break;
                case 6:
                    {
                        // Front right top
                        point = new MeshPoint()
                        {
                            VertexID = 6,
                            ParentPiece = piece,
                            X = component.RelPosition.X + component.Length,
                            Y = component.RelPosition.Y,
                            Z = component.RelPosition.Z + component.Height,
                        };
                    }
                    break;
                case 7:
                    {
                        // Rear left top
                        point = new MeshPoint()
                        {
                            VertexID = 7,
                            ParentPiece = piece,
                            X = component.RelPosition.X,
                            Y = component.RelPosition.Y + component.Width,
                            Z = component.RelPosition.Z + component.Height,
                        };
                    }
                    break;
                case 8:
                    {
                        // Rear right top
                        point = new MeshPoint()
                        {
                            VertexID = 8,
                            ParentPiece = piece,
                            X = component.RelPosition.X + component.Length,
                            Y = component.RelPosition.Y + component.Width,
                            Z = component.RelPosition.Z + component.Height,
                        };
                    }
                    break;
                default:
                    throw new ArgumentException("No vertex with ID " + vertexID + " defined");
            }
            return point;
        }
    }
}
