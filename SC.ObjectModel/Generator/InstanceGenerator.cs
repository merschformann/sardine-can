using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Generator
{
    /// <summary>
    /// Used to generate synthetic test-instances
    /// </summary>
    public class InstanceGenerator
    {
        #region Simple pieces

        /// <summary>
        /// Generates a set of pieces
        /// </summary>
        /// <param name="count">The number of pieces to generate</param>
        /// <param name="minSize">The minimal size of the pieces</param>
        /// <param name="maxSize">The maximal size of the pieces</param>
        /// <param name="minEquals">The number of minimal equals</param>
        /// <param name="maxEquals">The number of maximal equals</param>
        /// <param name="seed">The seed used for generation</param>
        /// <param name="roundedDecimals">The number of decimal places</param>
        /// <returns>The generated pieces</returns>
        public static List<VariablePiece> GeneratePieces(int count, double minSize, double maxSize, int minEquals, int maxEquals, int seed = 0, int roundedDecimals = 0)
        {
            // Init
            Random random = new Random(seed);
            List<VariablePiece> pieces = new List<VariablePiece>();

            // Generate as many pieces as desired
            for (int i = 0; i < count; )
            {
                // Init piece
                VariablePiece piece = new VariablePiece()
                {
                    ID = i,
                };
                // Add the parallelepiped component
                piece.AddComponent(0, 0, 0,
                    Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals),
                    Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals),
                    Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals));
                // Seal it
                piece.Seal();
                // Possibly make it non rotatable
                double forbiddenOrientationsChance = random.NextDouble();
                if (forbiddenOrientationsChance > 0.9)
                {
                    piece.ForbiddenOrientations = new HashSet<int>(MeshConstants.ORIENTATIONS.Except(MeshConstants.ORIENTATIONS_THIS_SIDE_UP));
                }
                // Possibly give a non-standard material
                double materialChance = random.NextDouble();
                if (materialChance > 0.8)
                {
                    if (materialChance > 0.9)
                    {
                        piece.Material = new Material() { MaterialClass = MaterialClassification.Explosive };
                    }
                    else
                    {
                        piece.Material = new Material() { MaterialClass = MaterialClassification.FlammableGas };
                    }
                }
                // Possibly make piece not stackable
                double stackableChance = random.NextDouble();
                if (stackableChance > 0.9)
                {
                    piece.Stackable = false;
                }
                // Clone the piece to generate sufficient 'equals'
                int equalCount = random.Next(minEquals, maxEquals + 1);
                for (int j = 0; i < count && j < equalCount; j++, i++)
                {
                    VariablePiece clonePiece = piece.Clone();
                    clonePiece.ID = i + 1;
                    pieces.Add(clonePiece);
                }
            }

            // Return
            return pieces;
        }

        #endregion

        #region Tetris-like clustered pieces

        /// <summary>
        /// Generates a tetris "L"
        /// </summary>
        /// <param name="random">The randomizer to use</param>
        /// <param name="pieceID">The ID of the piece</param>
        /// <param name="minSize">The minimal size</param>
        /// <param name="maxSize">The maximal size</param>
        /// <param name="roundedDecimals">The number of decimal places</param>
        /// <returns>The generated piece</returns>
        public static VariablePiece GenerateTetrisL(Random random, ref int pieceID, double minSize, double maxSize, int roundedDecimals, double lengthBreak)
        {
            double length = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double width = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double height = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double breakLength = length * lengthBreak;
            double breakHeight = height * lengthBreak;
            VariablePiece piece = new VariablePiece();
            piece.ID = ++pieceID;
            piece.AddComponent(0, 0, 0, breakLength, width, height);
            piece.AddComponent(breakLength, 0, 0, length - breakLength, width, breakHeight);
            piece.Seal();
            return piece;
        }

        /// <summary>
        /// Generates a tetris "T"
        /// </summary>
        /// <param name="random">The randomizer to use</param>
        /// <param name="pieceID">The ID of the piece</param>
        /// <param name="minSize">The minimal size</param>
        /// <param name="maxSize">The maximal size</param>
        /// <param name="roundedDecimals">The number of decimal places</param>
        /// <returns>The generated piece</returns>
        public static VariablePiece GenerateTetrisT(Random random, ref int pieceID, double minSize, double maxSize, int roundedDecimals, double lengthBreak)
        {
            double length = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double width = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double height = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double breakLength1 = (length / 2.0) - (length / 2.0 * lengthBreak);
            double breakLength2 = length - breakLength1;
            double breakHeight = height * lengthBreak;
            VariablePiece piece = new VariablePiece();
            piece.ID = ++pieceID;
            piece.AddComponent(breakLength1, 0, breakHeight, breakLength2 - breakLength1, width, height - breakHeight);
            piece.AddComponent(0, 0, 0, length, width, breakHeight);
            piece.Seal();
            return piece;
        }

        /// <summary>
        /// Generates a tetris "U"
        /// </summary>
        /// <param name="random">The randomizer to use</param>
        /// <param name="pieceID">The ID of the piece</param>
        /// <param name="minSize">The minimal size</param>
        /// <param name="maxSize">The maximal size</param>
        /// <param name="roundedDecimals">The number of decimal places</param>
        /// <returns>The generated piece</returns>
        public static VariablePiece GenerateTetrisU(Random random, ref int pieceID, double minSize, double maxSize, int roundedDecimals, double lengthBreak)
        {
            double length = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double width = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double height = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals);
            double breakLength1 = (length / 2.0) * lengthBreak;
            double breakLength2 = length - breakLength1;
            double breakHeight = (height / 2.0) * lengthBreak;
            VariablePiece piece = new VariablePiece();
            piece.ID = ++pieceID;
            piece.AddComponent(0, 0, breakHeight, breakLength1, width, height - breakHeight);
            piece.AddComponent(breakLength2, 0, breakHeight, length - breakLength2, width, height - breakHeight);
            piece.AddComponent(0, 0, 0, length, width, breakHeight);
            piece.Seal();
            return piece;
        }

        /// <summary>
        /// Generates a tetris "Block"
        /// </summary>
        /// <param name="random">The randomizer to use</param>
        /// <param name="pieceID">The ID of the piece</param>
        /// <param name="minSize">The minimal size</param>
        /// <param name="maxSize">The maximal size</param>
        /// <param name="roundedDecimals">The number of decimal places</param>
        /// <returns>The generated piece</returns>
        public static VariablePiece GenerateTetrisBlock(Random random, ref int pieceID, double minSize, double maxSize, int roundedDecimals)
        {
            VariablePiece piece = new VariablePiece();
            piece.ID = ++pieceID;
            piece.AddComponent(0, 0, 0,
                Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals),
                Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals),
                Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals));
            piece.Seal();
            return piece;
        }

        /// <summary>
        /// Generates a set of tetris-pieces
        /// </summary>
        /// <param name="count">The number of pieces to generate</param>
        /// <param name="minSize">The minimal size of the pieces</param>
        /// <param name="maxSize">The maximal size of the pieces</param>
        /// <param name="minEquals">The number of minimal equals</param>
        /// <param name="maxEquals">The number of maximal equals</param>
        /// <param name="seed">The seed used for generation</param>
        /// <param name="roundedDecimals">The number of decimal places</param>
        /// <param name="lengthBreak">The length break influencing the clipping of pieces</param>
        /// <returns>The generated pieces</returns>
        public static List<VariablePiece> GenerateTetrisPieces(int count, double minSize, double maxSize, int minEquals, int maxEquals, int weightBox = 1, int weightL = 1, int weightT = 1, int weightU = 1, int seed = 0, int roundedDecimals = 0, double lengthBreakL = 0.5, double lengthBreakT = 0.5, double lengthBreakU = 0.5)
        {
            // Init
            Random random = new Random(seed);
            List<VariablePiece> pieces = new List<VariablePiece>();
            int pieceID = 0;
            Func<ShapeType, int> shapeWeigher = (ShapeType type) =>
            {
                switch (type)
                {
                    case ShapeType.L: return weightL;
                    case ShapeType.T: return weightT;
                    case ShapeType.U: return weightU;
                    case ShapeType.Box: return weightBox;
                    default:
                        throw new ArgumentException("No such shape available: " + type.ToString());
                }
            };

            // Generate a set of tetris pieces
            for (int i = 0; i < count; i++)
            {
                // Generate the next shape to add
                ShapeType nextTetrisShape = GetRandomShapeType(random, shapeWeigher);
                VariablePiece nextTetrisItem = null;
                switch (nextTetrisShape)
                {
                    case ShapeType.L:
                        {
                            // Generate an L
                            nextTetrisItem = GenerateTetrisL(random, ref pieceID, minSize, maxSize, roundedDecimals, lengthBreakL);
                        }
                        break;
                    case ShapeType.Box:
                        {
                            // Generate a simple block
                            nextTetrisItem = GenerateTetrisBlock(random, ref pieceID, minSize, maxSize, roundedDecimals);
                        }
                        break;
                    case ShapeType.T:
                        {
                            // Generate a T
                            nextTetrisItem = GenerateTetrisT(random, ref pieceID, minSize, maxSize, roundedDecimals, lengthBreakT);
                        }
                        break;
                    case ShapeType.U:
                        {
                            // Generate a U
                            nextTetrisItem = GenerateTetrisU(random, ref pieceID, minSize, maxSize, roundedDecimals, lengthBreakU);
                        }
                        break;
                    default:
                        break;
                }
                // Possibly make it non rotatable
                if (random.NextDouble() > 0.9)
                {
                    nextTetrisItem.ForbiddenOrientations = new HashSet<int>(MeshConstants.ORIENTATIONS_THIS_SIDE_UP);
                }
                // Possibly give a non-standard material
                double materialChance = random.NextDouble();
                if (materialChance > 0.8)
                {
                    if (materialChance > 0.9)
                    {
                        nextTetrisItem.Material = new Material() { MaterialClass = MaterialClassification.Explosive };
                    }
                    else
                    {
                        nextTetrisItem.Material = new Material() { MaterialClass = MaterialClassification.FlammableGas };
                    }
                }
                // Possibly make piece not stackable
                double stackableChance = random.NextDouble();
                if (stackableChance > 0.95)
                {
                    nextTetrisItem.Stackable = false;
                }
                // Add it
                pieces.Add(nextTetrisItem);

                // Clone the new item to get sufficient equal items
                int equalCount = minEquals + random.Next(maxEquals - minEquals) - 1;
                for (int j = 0; j < equalCount && i < count; j++, i++)
                {
                    VariablePiece clone = nextTetrisItem.Clone();
                    clone.ID = ++pieceID;
                    pieces.Add(clone);
                }
            }

            // Return
            return pieces;
        }

        /// <summary>
        /// Gives a random shape type based on a weight-function
        /// </summary>
        /// <param name="randomizer">The randomizer used to access random numbers</param>
        /// <param name="weightFunction">The weight function</param>
        /// <returns>The random shape type</returns>
        private static ShapeType GetRandomShapeType(Random randomizer, Func<ShapeType, int> weightFunction)
        {
            ShapeType[] choices = Enum.GetValues(typeof(ShapeType)).Cast<ShapeType>().ToArray();
            double weightSum = choices.Sum(c => weightFunction(c));
            int numberOfChoices = choices.Length;
            double[] probabilities = new double[numberOfChoices];
            for (int i = 0; i < choices.Length; i++)
            {
                probabilities[i] = ((double)weightFunction(choices[i])) / weightSum;
            }
            double randomNumber = randomizer.NextDouble();
            int index = 0;
            while (randomNumber >= 0 && index < numberOfChoices)
            {
                randomNumber -= probabilities[index];
                index++;
            }
            return choices[index - 1];
        }

        /// <summary>
        /// Generates a set of performance-test tetris-pieces
        /// </summary>
        /// <param name="count">The number of pieces to generate</param>
        /// <param name="minSize">The minimal size of the pieces</param>
        /// <param name="maxSize">The maximal size of the pieces</param>
        /// <param name="minEquals">The number of minimal equals</param>
        /// <param name="maxEquals">The number of maximal equals</param>
        /// <param name="seed">The seed used for generation</param>
        /// <param name="roundedDecimals">The number of decimal places</param>
        /// <returns>The generated pieces</returns>
        public static List<VariablePiece> GeneratePerformanceTestTetrisPieces(int count, double minSize, double maxSize, int minEquals, int maxEquals, int seed = 0, int roundedDecimals = 0)
        {
            // Init
            Random random = new Random(seed);
            List<VariablePiece> pieces = new List<VariablePiece>();
            int pieceID = 0;

            // Generate a set of tetris pieces
            for (int i = 0; i < count; i++)
            {
                // Generate the next shape to add
                int nextTetrisShape = random.Next(1000);
                VariablePiece nextTetrisItem = null;
                if (nextTetrisShape <= 30)
                {
                    nextTetrisItem = GenerateTetrisU(random, ref pieceID, minSize, maxSize, roundedDecimals, 0.5);
                }
                else
                {
                    if (nextTetrisShape >= 950)
                    {
                        nextTetrisItem = GenerateTetrisT(random, ref pieceID, minSize, maxSize, roundedDecimals, 0.5);
                    }
                    else
                    {
                        if (nextTetrisShape <= 300)
                        {
                            nextTetrisItem = GenerateTetrisL(random, ref pieceID, minSize, maxSize, roundedDecimals, 0.5);
                        }
                        else
                        {
                            nextTetrisItem = GenerateTetrisBlock(random, ref pieceID, minSize, maxSize, roundedDecimals);
                        }
                    }
                }
                // Possibly make it non rotatable
                if (random.NextDouble() > 0.9)
                {
                    nextTetrisItem.ForbiddenOrientations = new HashSet<int>(MeshConstants.ORIENTATIONS_THIS_SIDE_UP);
                }
                // Possibly give a non-standard material
                double materialChance = random.NextDouble();
                if (materialChance > 0.8)
                {
                    if (materialChance > 0.9)
                    {
                        nextTetrisItem.Material = new Material() { MaterialClass = MaterialClassification.Explosive };
                    }
                    else
                    {
                        nextTetrisItem.Material = new Material() { MaterialClass = MaterialClassification.FlammableGas };
                    }
                }
                // Possibly make piece not stackable
                double stackableChance = random.NextDouble();
                if (stackableChance > 0.95)
                {
                    nextTetrisItem.Stackable = false;
                }
                // Add it
                pieces.Add(nextTetrisItem);

                // Clone the new item to get sufficient equal items
                int equalCount = minEquals + random.Next(maxEquals - minEquals) - 1;
                for (int j = 0; j < equalCount && i < count; j++, i++)
                {
                    VariablePiece clone = nextTetrisItem.Clone();
                    clone.ID = ++pieceID;
                    pieces.Add(clone);
                }
            }

            // Return
            return pieces;
        }

        #endregion

        #region Container

        /// <summary>
        /// Generates a set of containers
        /// </summary>
        /// <param name="count">The number of containers to generate</param>
        /// <param name="minSize">The minimal side-length of a container</param>
        /// <param name="maxSize">The maximal side-length of a container</param>
        /// <param name="roundedDecimals">The number of decimal places to round to</param>
        /// <param name="seed">The seed to use</param>
        /// <returns>The set of generated containers</returns>
        public static List<Container> GenerateContainer(int count, double minSize, double maxSize, int roundedDecimals, int seed = 0)
        {
            Random random = new Random(seed);
            List<Container> container = new List<Container>();
            for (int i = 1; i <= count; i++)
            {
                Container c = new Container()
                {
                    ID = i,
                    Mesh = new MeshCube()
                    {
                        Length = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals),
                        Width = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals),
                        Height = Math.Round(minSize + random.NextDouble() * (maxSize - minSize), roundedDecimals)
                    }
                };
                c.Seal();
                container.Add(c);
            }
            return container;
        }

        #endregion

        #region Container (realistic)

        /// <summary>
        /// Generates the specified number of realistic containers
        /// </summary>
        /// <param name="countLD3">Number of LD3 containers to generate</param>
        /// <param name="countAKW">Number of AKW containers to generate</param>
        /// <param name="countAMP">Number of AMP containers to generate</param>
        /// <param name="countRKN">Number of RKN containers to generate</param>
        /// <param name="countAMJ">Number of AMJ containers to generate</param>
        /// <returns></returns>
        public static List<Container> GenerateRealisticContainers(int countLD3, int countAKW, int countAMP, int countRKN, int countAMJ)
        {
            // Init
            int currentID = 0;
            List<Container> container = new List<Container>();

            // Generate LD3 containers
            for (int i = 0; i < countLD3; i++)
            {
                container.Add(GenerateContainerLD3(currentID++));
            }

            // Generate AKW containers
            for (int i = 0; i < countAKW; i++)
            {
                container.Add(GenerateContainerAKW(currentID++));
            }

            // Generate AMP containers
            for (int i = 0; i < countAMP; i++)
            {
                container.Add(GenerateContainerAMP(currentID++));
            }

            // Generate RKN containers
            for (int i = 0; i < countRKN; i++)
            {
                container.Add(GenerateContainerRKN(currentID++));
            }

            // Generate RKN containers
            for (int i = 0; i < countAMJ; i++)
            {
                container.Add(GenerateContainerAMJ(currentID++));
            }

            // Return them
            return container;
        }

        /// <summary>
        /// Generates a LD3-container using the official dimensions
        /// </summary>
        /// <param name="id">The ID of the container</param>
        /// <returns>The newly generated container</returns>
        public static Container GenerateContainerLD3(int id)
        {
            // Create it
            Container container = new Container()
            {
                ID = id,
                Mesh = new MeshCube()
                {
                    Length = 15.3,
                    Width = 20.1,
                    Height = 16.3
                }
            };
            // Add slants
            Slant slant = new Slant()
            {
                Container = container,
                Position = new MeshPoint() { X = 0.0, Y = 15.6, Z = 0.0 },
                NormalVector = new MeshPoint() { X = 0.0, Y = 1.0, Z = -1.0 }
            };
            slant.Seal();
            container.AddSlant(slant);

            // Seal the container
            container.Seal();

            // Return it
            return container;
        }

        /// <summary>
        /// Generates a AKW-container using the official dimensions
        /// </summary>
        /// <param name="id">The ID of the container</param>
        /// <returns>The newly generated container</returns>
        public static Container GenerateContainerAKW(int id)
        {
            // Create it
            Container container = new Container()
            {
                ID = id,
                Mesh = new MeshCube()
                {
                    Length = 23.9,
                    Width = 14.4,
                    Height = 11.1
                }
            };
            // Add slants
            Slant slant1 = new Slant()
            {
                Container = container,
                Position = new MeshPoint() { X = 4.65, Y = 0.0, Z = 0.0 },
                NormalVector = new MeshPoint() { X = -1.0, Y = 0.0, Z = -1.0 }
            };
            slant1.Seal();
            container.AddSlant(slant1);
            Slant slant2 = new Slant()
            {
                Container = container,
                Position = new MeshPoint() { X = 19.25, Y = 0.0, Z = 0.0 },
                NormalVector = new MeshPoint() { X = 1.0, Y = 0.0, Z = -1.0 }
            };
            slant2.Seal();
            container.AddSlant(slant2);

            // Seal the container
            container.Seal();

            // Return it
            return container;
        }

        /// <summary>
        /// Generates a AMP-container using the official dimensions
        /// </summary>
        /// <param name="id">The ID of the container</param>
        /// <returns>The newly generated container</returns>
        public static Container GenerateContainerAMP(int id)
        {
            // Create it
            Container container = new Container()
            {
                ID = id,
                Mesh = new MeshCube()
                {
                    Length = 30.5,
                    Width = 22.3,
                    Height = 15.4
                }
            };

            // Seal the container
            container.Seal();

            // Return it
            return container;
        }

        /// <summary>
        /// Generates a RKN-container using the official dimensions
        /// </summary>
        /// <param name="id">The ID of the container</param>
        /// <returns>The newly generated container</returns>
        public static Container GenerateContainerRKN(int id)
        {
            // Create it
            Container container = new Container()
            {
                ID = id,
                Mesh = new MeshCube()
                {
                    Length = 15.3,
                    Width = 20.1,
                    Height = 16.3
                }
            };
            // Add slants
            Slant slant = new Slant()
            {
                Container = container,
                Position = new MeshPoint() { X = 0.0, Y = 15.6, Z = 0.0 },
                NormalVector = new MeshPoint() { X = 0.0, Y = 1.0, Z = -1.0 }
            };
            slant.Seal();
            container.AddSlant(slant);
            // Add virtual piece
            VirtualPiece virtualPiece = new VirtualPiece()
            {
                Container = container,
                FixedOrientation = 0,
                FixedPosition = new MeshPoint()
                {
                    X = 1,
                    Y = 17.1,
                    Z = 11.0
                }
            };
            virtualPiece.AddComponent(0, 0, 0, 13.3, 3.0, 5.0);
            virtualPiece.Seal();
            container.AddVirtualPiece(virtualPiece);

            // Seal the container
            container.Seal();

            // Return it
            return container;
        }

        /// <summary>
        /// Generates a AMJ-container using the official dimensions
        /// </summary>
        /// <param name="id">The ID of the container</param>
        /// <returns>The newly generated container</returns>
        public static Container GenerateContainerAMJ(int id)
        {
            // Create it
            Container container = new Container()
            {
                ID = id,
                Mesh = new MeshCube()
                {
                    Length = 30.6,
                    Width = 23.0,
                    Height = 24.0
                }
            };
            // Add slants
            Slant slant1 = new Slant()
            {
                Container = container,
                Position = new MeshPoint() { X = 0.0, Y = 15, Z = 24.0 },
                NormalVector = new MeshPoint() { X = 0.0, Y = 1.0, Z = 1.0 }
            };
            slant1.Seal();
            container.AddSlant(slant1);
            //Slant slant2 = new Slant()
            //{
            //    Container = container,
            //    Position = new MeshPoint() { X = 0.0, Y = 15, Z = 24.0 },
            //    NormalVector = new MeshPoint() { X = 0.0, Y = 1.0, Z = 0.5 }
            //};
            //slant2.Seal();
            //container.AddSlant(slant2);
            //Slant slant3 = new Slant()
            //{
            //    Container = container,
            //    Position = new MeshPoint() { X = 0.0, Y = 15, Z = 24.0 },
            //    NormalVector = new MeshPoint() { X = 0.0, Y = 1.0, Z = 2.0 }
            //};
            //slant3.Seal();
            //container.AddSlant(slant3);

            // Add virtual pieces
            VirtualPiece virtualPiece1 = new VirtualPiece()
            {
                Container = container,
                FixedOrientation = 0,
                FixedPosition = new MeshPoint()
                {
                    X = 9.7,
                    Y = 1.0,
                    Z = 0.0
                }
            };
            virtualPiece1.AddComponent(0, 0, 0, 1.0, 7.0, 24.0);
            virtualPiece1.Seal();
            container.AddVirtualPiece(virtualPiece1);
            VirtualPiece virtualPiece2 = new VirtualPiece()
            {
                Container = container,
                FixedOrientation = 0,
                FixedPosition = new MeshPoint()
                {
                    X = 19.9,
                    Y = 1.0,
                    Z = 0.0
                }
            };
            virtualPiece2.AddComponent(0, 0, 0, 1.0, 7.0, 24.0);
            virtualPiece2.Seal();
            container.AddVirtualPiece(virtualPiece2);

            // Seal the container
            container.Seal();

            // Return it
            return container;
        }

        #endregion
    }
}
