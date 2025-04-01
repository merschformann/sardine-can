using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Elements;
using SC.ObjectModel.Interfaces;
using SC.ObjectModel.IO.Json;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace SC.ObjectModel
{
    /// <summary>
    /// Defines a solution to the problem
    /// </summary>
    public class COSolution : IXmlSerializable
    {
        /// <summary>
        /// Creates a new solution
        /// </summary>
        /// <param name="instance">The instance this solution belongs to</param>
        /// <param name="config">The configuration being used.</param>
        internal COSolution(Instance instance, Configuration.Configuration config, Random random)
        {
            InstanceLinked = instance;
            Configuration = config;
            _random = random;
            ContainedPieces = new HashSet<VariablePiece>();
            OffloadPieces = new HashSet<VariablePiece>(instance.Pieces);
            Orientations = new int[instance.PiecesWithVirtuals.Count()];
            OrientedPieces = new ComponentsSet[instance.PiecesWithVirtuals.Count()];
            Positions = new MeshPoint[instance.PiecesWithVirtuals.Count()];
            Containers = new Container[instance.PiecesWithVirtuals.Count()];
            InitMetaInfo();
            InitFlagHandling();
            ContainerContent = instance.Containers.Select(c => new HashSet<VariablePiece>()).ToArray();
            ContainerInfos = new ContainerInfo[instance.Containers.Count];
            for (int i = 0; i < instance.Containers.Count; i++)
                ContainerInfos[i] = new ContainerInfo(this, instance.Containers[i]);
            Objective = new Objective(this);
            ContainerOrderSupply = new ContainerOrderSupply(instance.Containers, instance.Pieces, config.ContainerOrderInit, config.ContainerOrderReorder, config.ContainerOpen, _random);
            MaterialsPerContainer = new int[instance.Containers.Count, Enum.GetValues(typeof(MaterialClassification)).Length];
        }

        /// <summary>
        /// The ID of the solution
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The instance this solution belongs to
        /// </summary>
        public Instance InstanceLinked { get; set; }
        /// <summary>
        /// The configuration this solution is based on
        /// </summary>
        public Configuration.Configuration Configuration { get; private set; }
        /// <summary>
        /// The randomizer of this solution.
        /// </summary>
        private Random _random = null;

        #region Core information

        /// <summary>
        /// Contains all pieces that are allocated to a container
        /// </summary>
        public HashSet<VariablePiece> ContainedPieces { get; set; }
        /// <summary>
        /// Contains all pieces not allocated to a container
        /// </summary>
        public HashSet<VariablePiece> OffloadPieces { get; set; }

        /// <summary>
        /// The items contained in the different containers
        /// </summary>
        public HashSet<VariablePiece>[] ContainerContent { get; set; }
        /// <summary>
        /// The orientations used per piece
        /// </summary>
        public int[] Orientations { get; set; }
        /// <summary>
        /// The pre-oriented pieces used
        /// </summary>
        public ComponentsSet[] OrientedPieces { get; set; }
        /// <summary>
        /// The positions of the pieces used
        /// </summary>
        public MeshPoint[] Positions { get; set; }
        /// <summary>
        /// The containers used per piece
        /// </summary>
        public Container[] Containers { get; set; }

        /// <summary>
        /// Used to store the currently used order of containers.
        /// </summary>
        public List<Container> ConstructionContainerOrder;
        /// <summary>
        /// Used to store the currently used orientations per piece.
        /// </summary>
        public int[][] ConstructionOrientationOrder;

        #endregion

        #region Solution manipulation

        /// <summary>
        /// Adds a piece to a container at a position and in an orientation
        /// </summary>
        /// <param name="container">The container to add the piece to</param>
        /// <param name="piece">The piece to add to the solution</param>
        /// <param name="orientation">The orientation to use for the piece</param>
        /// <param name="position">The position of the piece inside the container</param>
        public void Add(Container container, VariablePiece piece, int orientation, MeshPoint position)
        {
            Objective.AddPiece(piece, orientation, position);
            ContainerInfos[container.VolatileID].AddPiece(piece, orientation, position);
            MaterialsPerContainer[container.VolatileID, (int)piece.Material.MaterialClass]++;
            ContainedPieces.Add(piece);
            OffloadPieces.Remove(piece);
            ContainerContent[container.VolatileID].Add(piece);
            Orientations[piece.VolatileID] = orientation;
            Positions[piece.VolatileID] = position;
            Containers[piece.VolatileID] = container;
            OrientedPieces[piece.VolatileID] = piece[orientation];
            AddPieceFlags(container, piece);
        }

        /// <summary>
        /// Removes a specific piece from the solution
        /// </summary>
        /// <param name="container">The container to remove the piece from</param>
        /// <param name="piece">The piece to remove</param>
        /// <returns>The position at which the piece was inserted</returns>
        public MeshPoint Remove(Container container, VariablePiece piece)
        {
            Objective.RemovePiece(piece, Orientations[piece.VolatileID], Positions[piece.VolatileID]);
            ContainerInfos[container.VolatileID].RemovePiece(piece, Orientations[piece.VolatileID], Positions[piece.VolatileID]);
            MaterialsPerContainer[container.VolatileID, (int)piece.Material.MaterialClass]--;
            ContainedPieces.Remove(piece);
            OffloadPieces.Add(piece);
            ContainerContent[container.VolatileID].Remove(piece);
            MeshPoint position = Positions[piece.VolatileID];
            Containers[piece.VolatileID] = null;
            Positions[piece.VolatileID] = null;
            Orientations[piece.VolatileID] = 0;
            OrientedPieces[piece.VolatileID] = null;
            RemovePieceFlags(container, piece);
            return position;
        }

        /// <summary>
        /// Clears the content of one container
        /// </summary>
        /// <param name="container">The container to clear</param>
        public void RemoveContainer(Container container)
        {
            ContainerInfos[container.VolatileID].Clear();
            foreach (var piece in ContainerContent[container.VolatileID])
            {
                Objective.RemovePiece(piece, Orientations[piece.VolatileID], Positions[piece.VolatileID]);
                MaterialsPerContainer[container.VolatileID, (int)piece.Material.MaterialClass]--;
                ContainedPieces.Remove(piece);
                OffloadPieces.Add(piece);
                Containers[piece.VolatileID] = null;
                Positions[piece.VolatileID] = null;
                Orientations[piece.VolatileID] = 0;
                OrientedPieces[piece.VolatileID] = null;
                RemovePieceFlags(container, piece);
            }
            ContainerContent[container.VolatileID].Clear();
        }

        /// <summary>
        /// Clears the solution
        /// </summary>
        public void Clear()
        {
            EPCounter = 0;
            ContainedPieces.Clear();
            OffloadPieces = new HashSet<VariablePiece>(InstanceLinked.Pieces);
            foreach (var container in InstanceLinked.Containers)
            {
                ContainerInfos[container.VolatileID].Clear();
                for (int i = 0; i < Enum.GetValues(typeof(MaterialClassification)).Length; i++)
                    MaterialsPerContainer[container.VolatileID, i] = 0;
                ContainerContent[container.VolatileID].Clear();
                ExtremePoints[container.VolatileID].Clear();
                if (ResidualSpace != null)
                {
                    ResidualSpace.Clear();
                }
            }
            GenerateDefaultEPs();
            foreach (var piece in InstanceLinked.Pieces)
            {
                Orientations[piece.VolatileID] = 0;
                OrientedPieces[piece.VolatileID] = null;
                Positions[piece.VolatileID] = null;
                Containers[piece.VolatileID] = null;
            }
            switch (Configuration.MeritType)
            {
                case MeritFunctionType.MFV:
                    break;
                case MeritFunctionType.MMPSXY:
                case MeritFunctionType.LPXY:
                    {
                        foreach (var container in InstanceLinked.Containers)
                        {
                            PackingMaxX[container.VolatileID] = 0;
                            PackingMaxY[container.VolatileID] = 0;
                        }
                    }
                    break;
                case MeritFunctionType.MRSU:
                    break;
                case MeritFunctionType.MEDXYZ:
                    break;
                case MeritFunctionType.MEDXY:
                    break;
                case MeritFunctionType.None:
                default:
                    break;
            }
            LevelPackingC = 0;
            Objective.Clear();
            ClearFlagHandling();
        }

        #endregion

        #region Cloning

        /// <summary>
        /// Clones the current solution (does not clone meta-information)
        /// </summary>
        /// <returns>A clone of this solution</returns>
        public COSolution Clone(bool unofficial = true)
        {
            COSolution clone = InstanceLinked.CreateSolution(Configuration, unofficial);
            clone.InstanceLinked = InstanceLinked;
            clone.ID = ID;
            clone.ContainedPieces = new HashSet<VariablePiece>(ContainedPieces);
            clone.OffloadPieces = new HashSet<VariablePiece>(OffloadPieces);
            clone.MaterialsPerContainer = new int[InstanceLinked.Containers.Count, Enum.GetValues(typeof(MaterialClassification)).Length];
            foreach (var container in InstanceLinked.Containers)
            {
                foreach (var piece in ContainerContent[container.VolatileID])
                {
                    clone.ContainerContent[container.VolatileID].Add(piece);
                    clone.MaterialsPerContainer[container.VolatileID, (int)piece.Material.MaterialClass]++;
                }
            }
            foreach (var piece in InstanceLinked.Pieces)
            {
                clone.Orientations[piece.VolatileID] = Orientations[piece.VolatileID];
                clone.OrientedPieces[piece.VolatileID] = OrientedPieces[piece.VolatileID];
                clone.Positions[piece.VolatileID] = (Positions[piece.VolatileID] != null) ? Positions[piece.VolatileID].Clone() : null;
                clone.Containers[piece.VolatileID] = Containers[piece.VolatileID];
            }
            clone.ContainerInfos = new ContainerInfo[InstanceLinked.Containers.Count];
            foreach (var container in InstanceLinked.Containers)
            {
                clone.ContainerInfos[container.VolatileID] = ContainerInfos[container.VolatileID].Clone(clone);
            }
            clone.ExtremePoints = ExtremePoints.Select(c => c.ToList()).ToArray();
            // Add info about the virtual pieces
            clone.GenerateVirtualPieceInfo();
            // Copy meta information
            clone.EndPointsBoundingBoxInner = EndPointsBoundingBoxInner.Select(p => p.Clone()).ToArray();
            clone.EndPointsBoundingBoxOuter = EndPointsBoundingBoxOuter.Select(p => p.Clone()).ToArray();
            clone.EndPointsComponentInner = EndPointsComponentInner?.Select(p => p.Clone()).ToArray();
            clone.EndPointsComponentOuter = EndPointsComponentOuter?.Select(p => p.Clone()).ToArray();
            clone.PushedPosition = PushedPosition.Select(p => p.Clone()).ToArray();
            clone.EndPointsDelta = EndPointsDelta.Select(p => p.Clone()).ToArray();
            clone.PiecesByVolatileID = PiecesByVolatileID.ToArray();
            clone.ContainerByVolatileID = ContainerByVolatileID.ToArray();
            if (Configuration.MeritType == MeritFunctionType.MMPSXY)
            {
                clone.PackingMaxX = PackingMaxX.ToArray();
                clone.PackingMaxY = PackingMaxY.ToArray();
            }
            if (Configuration.MeritType == MeritFunctionType.MRSU)
            {
                clone.ResidualSpace = ResidualSpace.Select(c => c.Clone()).ToList();
            }
            clone.EPCounter = EPCounter;
            clone.LevelPackingC = LevelPackingC;
            clone.ContainerOrderSupply = new ContainerOrderSupply(clone.InstanceLinked.Containers, clone.InstanceLinked.Pieces, Configuration.ContainerOrderInit, Configuration.ContainerOrderReorder, Configuration.ContainerOpen, clone._random);
            // Copy construction information
            if (ConstructionContainerOrder != null)
                clone.ConstructionContainerOrder = ConstructionContainerOrder.ToList();
            if (ConstructionOrientationOrder != null)
                clone.ConstructionOrientationOrder = ConstructionOrientationOrder.Select(p => p.ToArray()).ToArray();
            clone.Objective = Objective.Clone(clone);
            // Return it
            return clone;
        }

        #endregion

        #region Auxiliary information

        /// <summary>
        /// Fast access field for exploited volume
        /// </summary>
        public Objective Objective { get; private set; }

        /// <summary>
        /// The handler for container sorting.
        /// </summary>
        public ContainerOrderSupply ContainerOrderSupply { get; set; } = null;

        /// <summary>
        /// Fast accessible information about the number of items with the correpsonding material per container
        /// </summary>
        public int[,] MaterialsPerContainer = null;

        /// <summary>
        /// Stores the inner points of the bounding-boxes of the pieces
        /// </summary>
        public MeshPoint[] EndPointsBoundingBoxInner = null;

        /// <summary>
        /// Stores the outer points of the bounding-boxes of the pieces
        /// </summary>
        public MeshPoint[] EndPointsBoundingBoxOuter = null;

        /// <summary>
        /// Stores the inner points of the components
        /// </summary>
        public MeshPoint[] EndPointsComponentInner = null;

        /// <summary>
        /// Stores the outer points of the components
        /// </summary>
        public MeshPoint[] EndPointsComponentOuter = null;

        /// <summary>
        /// Stores the pushed positions of the pieces
        /// </summary>
        public MeshPoint[] PushedPosition = null;

        /// <summary>
        /// Stores the push-delta-information of the pieces
        /// </summary>
        public MeshPoint[] EndPointsDelta = null;

        /// <summary>
        /// Stores the pieces belonging to their respective volatile IDs
        /// </summary>
        public Piece[] PiecesByVolatileID = null;

        /// <summary>
        /// Stores the container belonging to their respective volatile IDs
        /// </summary>
        public Container[] ContainerByVolatileID = null;

        /// <summary>
        /// Stores the current size of the packing regarding x
        /// </summary>
        public double[] PackingMaxX = null;

        /// <summary>
        /// Stores the current size of the packing regarding y
        /// </summary>
        public double[] PackingMaxY = null;

        /// <summary>
        /// Stores the residual space currently available at the different insertion points
        /// </summary>
        public List<MeshPoint> ResidualSpace = null;

        /// <summary>
        /// Stores the currently available EPs
        /// </summary>
        public List<MeshPoint>[] ExtremePoints = null;

        /// <summary>
        /// Counts the added EPs
        /// </summary>
        private int EPCounter = 0;

        /// <summary>
        /// The C constant for the level packing merit function
        /// </summary>
        private double LevelPackingC = 0;

        /// <summary>
        /// Keeps track of information on container level.
        /// </summary>
        public ContainerInfo[] ContainerInfos = null;

        /// <summary>
        /// Initiates the meta-information for the underlying instance
        /// </summary>
        private void InitMetaInfo()
        {
            int pieceID = 0;
            int componentID = 0;
            int containerID = 0;
            // Generate piece info
            PiecesByVolatileID = new Piece[InstanceLinked.PiecesWithVirtuals.Count()];
            foreach (var piece in InstanceLinked.PiecesWithVirtuals)
            {
                foreach (var component in piece.Original.Components)
                {
                    foreach (var orientation in MeshConstants.ORIENTATIONS)
                    {
                        piece[orientation][component.ID].VolatileID = componentID;
                    }
                    component.VolatileID = componentID++;
                }
                PiecesByVolatileID[pieceID] = piece;
                piece.VolatileID = pieceID++;
            }
            // Generate container info
            ContainerByVolatileID = new Container[InstanceLinked.Containers.Count];
            foreach (var container in InstanceLinked.Containers.OrderBy(c => c.ID))
            {
                ContainerByVolatileID[containerID] = container;
                container.VolatileID = containerID++;
            }
            if (Configuration.MeritType == MeritFunctionType.MRSU)
            {
                ResidualSpace = new List<MeshPoint>();
            }
            ContainerInfos = new ContainerInfo[InstanceLinked.Containers.Count];
            foreach (var container in InstanceLinked.Containers)
                ContainerInfos[container.VolatileID] = new ContainerInfo(this, container);
            // Generate default EPs
            GenerateDefaultEPs();
            // Generate merit-info
            switch (Configuration.MeritType)
            {
                case MeritFunctionType.None:
                case MeritFunctionType.MFV:
                    break;
                case MeritFunctionType.MMPSXY:
                    {
                        PackingMaxX = new double[InstanceLinked.Containers.Count];
                        PackingMaxY = new double[InstanceLinked.Containers.Count];
                    }
                    break;
                case MeritFunctionType.LPXY:
                    {
                        PackingMaxX = new double[InstanceLinked.Containers.Count];
                        PackingMaxY = new double[InstanceLinked.Containers.Count];
                        LevelPackingC = Math.Max(InstanceLinked.Containers.Max(c => c.Mesh.Length), InstanceLinked.Containers.Max(c => c.Mesh.Width)) + 1;
                    }
                    break;
                case MeritFunctionType.MRSU:
                default:
                    break;
            }
            // Init endpoint-info
            EndPointsBoundingBoxInner = new MeshPoint[pieceID];
            EndPointsBoundingBoxOuter = new MeshPoint[pieceID];
            PushedPosition = new MeshPoint[pieceID];
            EndPointsDelta = new MeshPoint[pieceID];
            for (int i = 0; i < pieceID; i++)
            {
                EndPointsBoundingBoxInner[i] = new MeshPoint() { ParentPiece = PiecesByVolatileID[i], VertexID = 1 };
                EndPointsBoundingBoxOuter[i] = new MeshPoint() { ParentPiece = PiecesByVolatileID[i], VertexID = 8 };
                PushedPosition[i] = new MeshPoint();
                EndPointsDelta[i] = new MeshPoint();
            }
            if (Configuration.Tetris)
            {
                EndPointsComponentInner = new MeshPoint[componentID];
                EndPointsComponentOuter = new MeshPoint[componentID];
                for (int i = 0; i < componentID; i++)
                {
                    EndPointsComponentInner[i] = new MeshPoint();
                    EndPointsComponentOuter[i] = new MeshPoint();
                }
            }
            else
            {
                EndPointsComponentInner = null;
                EndPointsComponentOuter = null;
            }
            // Add info about the virtual pieces
            GenerateVirtualPieceInfo();
        }

        #endregion

        #region Flag handling

        /// <summary>
        /// Stores all information about how many flags of a type and value are contained in a container.
        /// </summary>
        private MultiKeyDictionary<int, int, int>[] _flagCountPerContainer;
        /// <summary>
        /// Stores all information about which values of the different flag types are contained in which container.
        /// </summary>
        private MultiKeyDictionary<int, HashSet<int>>[] _flagTypesPerContainer;
        /// <summary>
        /// Inits flag handling for this solution.
        /// </summary>
        private void InitFlagHandling()
        {
            _flagCountPerContainer = new MultiKeyDictionary<int, int, int>[InstanceLinked.Containers.Count];
            _flagTypesPerContainer = new MultiKeyDictionary<int, HashSet<int>>[InstanceLinked.Containers.Count];
            foreach (var container in InstanceLinked.Containers)
            {
                _flagCountPerContainer[container.VolatileID] = new MultiKeyDictionary<int, int, int>(defaultIfNotPresent: true);
                _flagTypesPerContainer[container.VolatileID] = new MultiKeyDictionary<int, HashSet<int>>(defaultIfNotPresent: true, defaultValueIfNotPresent: () => new HashSet<int>());
            }
        }
        /// <summary>
        /// Clears all flag handling information from this solution.
        /// </summary>
        private void ClearFlagHandling()
        {
            foreach (var container in InstanceLinked.Containers)
            {
                _flagCountPerContainer[container.VolatileID].Clear();
                _flagTypesPerContainer[container.VolatileID].Clear();
            }
        }
        /// <summary>
        /// Updates the flag information implied by adding the piece to the given container.
        /// </summary>
        /// <param name="container">The container the piece is added to.</param>
        /// <param name="piece">The piece that is added to the container.</param>
        private void AddPieceFlags(Container container, VariablePiece piece)
        {
            foreach (var (flag, value) in piece.GetFlags())
            {
                _flagCountPerContainer[container.VolatileID][flag, value]++;
                _flagTypesPerContainer[container.VolatileID][flag].Add(value);
            }

        }
        /// <summary>
        /// Updates the flag information implied by removing the piece from the given container.
        /// </summary>
        /// <param name="container">The container the piece is removed from.</param>
        /// <param name="piece">The piece that is remove </param>
        private void RemovePieceFlags(Container container, VariablePiece piece)
        {
            foreach (var (flag, value) in piece.GetFlags())
            {
                int contained = --_flagCountPerContainer[container.VolatileID][flag, value];
                if (contained <= 0)
                    _flagTypesPerContainer[container.VolatileID][flag].Remove(value);
            }
        }

        /// <summary>
        /// Returns a set of all flag values of the given flag type contained in the given container.
        /// </summary>
        /// <param name="container">The container to check the contained flag types for.</param>
        /// <param name="flagType">The flag type to check.</param>
        /// <returns>A set containing all flag values of a given flag type contained in the given container.</returns>
        public HashSet<int> GetFlagInfoTypesContained(Container container, int flagType) => _flagTypesPerContainer[container.VolatileID][flagType];
        /// <summary>
        /// Returns the number of pieces contained in the given container for a given flag value of a given flag type.
        /// </summary>
        /// <param name="container">The container to check.</param>
        /// <param name="flagType">The type of the flag to check.</param>
        /// <param name="flagValue">The flag value to lookup the number of pieces for.</param>
        /// <returns>The number of pieces contained in the given container that have the given flag value for the given flag type.</returns>
        public int GetFlagInfoPiecesContained(Container container, int flagType, int flagValue) => _flagCountPerContainer[container.VolatileID][flagType, flagValue];

        #endregion

        #region Extreme point handling

        /// <summary>
        /// Generates the default extreme-points for the given solution.
        /// </summary>
        private void GenerateDefaultEPs()
        {
            // Init EP info fields
            ExtremePoints = new List<MeshPoint>[InstanceLinked.Containers.Count];
            // Generate default EPs
            foreach (var container in InstanceLinked.Containers)
            {
                ExtremePoints[container.VolatileID] = new List<MeshPoint>();
                AddEP(container, new MeshPoint() { X = 0, Y = 0, Z = 0 });
            }
            // Generate EPs for virtual pieces and already contained pieces
            foreach (var container in InstanceLinked.Containers)
                foreach (var virtualPiece in container.VirtualPieces)
                    GenerateEPsForPiece(container, virtualPiece, virtualPiece.FixedPosition, virtualPiece.FixedOrientation);
            foreach (var piece in ContainedPieces)
                GenerateEPsForPiece(Containers[piece.VolatileID], piece, Positions[piece.VolatileID], Orientations[piece.VolatileID]);
            // Generate EPs for slants
            foreach (var container in InstanceLinked.Containers)
                foreach (var slant in container.Slants.Where(s => s.NormalVector.X <= 0 || s.NormalVector.Y <= 0 || s.NormalVector.Z <= 0))
                    foreach (var intersection in slant.ContainerIntersections)
                        AddEP(container, new MeshPoint() { X = intersection.X, Y = intersection.Y, Z = intersection.Z });
        }

        /// <summary>
        /// Generates extreme-points for a given piece at a given position and orientation.
        /// </summary>
        /// <param name="container">The container to generate the extreme-points in.</param>
        /// <param name="piece">The piece to generate the extreme-points for.</param>
        /// <param name="position">The position of the piece.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        private void GenerateEPsForPiece(Container container, Piece piece, MeshPoint position, int orientation)
        {
            if (Configuration.Tetris)
            {
                foreach (var component in piece[orientation].Components)
                {
                    // Init extreme points to defaults
                    MeshPoint ep11 = new MeshPoint()
                    {
                        X = position.X + component.RelPosition.X + component.Length,
                        Y = 0,
                        Z = position.Z + component.RelPosition.Z
                    }; // Taking endpoint regarding x and projecting along the y-axis
                    MeshPoint ep12 = new MeshPoint()
                    {
                        X = position.X + component.RelPosition.X + component.Length,
                        Y = position.Y + component.RelPosition.Y,
                        Z = 0
                    }; // Taking endpoint regarding x and projecting along the z-axis
                    MeshPoint ep21 = new MeshPoint()
                    {
                        X = 0,
                        Y = position.Y + component.RelPosition.Y + component.Width,
                        Z = position.Z + component.RelPosition.Z
                    }; // Taking endpoint regarding y and projecting along the x-axis
                    MeshPoint ep22 = new MeshPoint()
                    {
                        X = position.X + component.RelPosition.X,
                        Y = position.Y + component.RelPosition.Y + component.Width,
                        Z = 0
                    }; // Taking endpoint regarding y and projecting along the z-axis
                    MeshPoint ep31 = new MeshPoint()
                    {
                        X = 0,
                        Y = position.Y + component.RelPosition.Y,
                        Z = position.Z + component.RelPosition.Z + component.Height
                    }; // Taking endpoint regarding z and projecting along the x-axis
                    MeshPoint ep32 = new MeshPoint()
                    {
                        X = position.X + component.RelPosition.X,
                        Y = 0,
                        Z = position.Z + component.RelPosition.Z + component.Height
                    }; // Taking endpoint regarding z and projecting along the y-axis

                    // Add the EPs
                    AddEP(container, ep11);
                    AddEP(container, ep12);
                    AddEP(container, ep21);
                    AddEP(container, ep22);
                    AddEP(container, ep31);
                    AddEP(container, ep32);
                }
            }
            else
            {
                // Init extreme points to defaults
                MeshPoint ep11 = new MeshPoint()
                {
                    X = position.X + piece[orientation].BoundingBox.Length,
                    Y = 0,
                    Z = position.Z
                }; // Taking endpoint regarding x and projecting along the y-axis
                MeshPoint ep12 = new MeshPoint()
                {
                    X = position.X + piece[orientation].BoundingBox.Length,
                    Y = position.Y,
                    Z = 0
                }; // Taking endpoint regarding x and projecting along the z-axis
                MeshPoint ep21 = new MeshPoint()
                {
                    X = 0,
                    Y = position.Y + piece[orientation].BoundingBox.Width,
                    Z = position.Z
                }; // Taking endpoint regarding y and projecting along the x-axis
                MeshPoint ep22 = new MeshPoint()
                {
                    X = position.X,
                    Y = position.Y + piece[orientation].BoundingBox.Width,
                    Z = 0
                }; // Taking endpoint regarding y and projecting along the z-axis
                MeshPoint ep31 = new MeshPoint()
                {
                    X = 0,
                    Y = position.Y,
                    Z = position.Z + piece[orientation].BoundingBox.Height
                }; // Taking endpoint regarding z and projecting along the x-axis
                MeshPoint ep32 = new MeshPoint()
                {
                    X = position.X,
                    Y = 0,
                    Z = position.Z + piece[orientation].BoundingBox.Height
                }; // Taking endpoint regarding z and projecting along the y-axis

                // Add the EPs
                AddEP(container, ep11);
                AddEP(container, ep12);
                AddEP(container, ep21);
                AddEP(container, ep22);
                AddEP(container, ep31);
                AddEP(container, ep32);
            }
        }

        /// <summary>
        /// Prepares all the necessary information about the virtual pieces
        /// </summary>
        private void GenerateVirtualPieceInfo()
        {
            // Generate detailed virtual piece positions
            foreach (var container in InstanceLinked.Containers)
            {
                foreach (var virtualPiece in container.VirtualPieces)
                {
                    Positions[virtualPiece.VolatileID] = virtualPiece.FixedPosition;
                    Containers[virtualPiece.VolatileID] = container;
                    Orientations[virtualPiece.VolatileID] = virtualPiece.FixedOrientation;
                    OrientedPieces[virtualPiece.VolatileID] = virtualPiece[virtualPiece.FixedOrientation];
                    EndPointsBoundingBoxInner[virtualPiece.VolatileID].X = virtualPiece.FixedPosition.X;
                    EndPointsBoundingBoxOuter[virtualPiece.VolatileID].X = virtualPiece.FixedPosition.X + virtualPiece[virtualPiece.FixedOrientation].BoundingBox[8].X;
                    EndPointsBoundingBoxInner[virtualPiece.VolatileID].Y = virtualPiece.FixedPosition.Y;
                    EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Y = virtualPiece.FixedPosition.Y + virtualPiece[virtualPiece.FixedOrientation].BoundingBox[8].Y;
                    EndPointsBoundingBoxInner[virtualPiece.VolatileID].Z = virtualPiece.FixedPosition.Z;
                    EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Z = virtualPiece.FixedPosition.Z + virtualPiece[virtualPiece.FixedOrientation].BoundingBox[8].Z;
                    if (Configuration.Tetris)
                    {
                        foreach (var component in virtualPiece[virtualPiece.FixedOrientation].Components)
                        {
                            EndPointsComponentInner[component.VolatileID].X = virtualPiece.FixedPosition.X + component[1].X;
                            EndPointsComponentOuter[component.VolatileID].X = virtualPiece.FixedPosition.X + component[8].X;
                            EndPointsComponentInner[component.VolatileID].Y = virtualPiece.FixedPosition.Y + component[1].Y;
                            EndPointsComponentOuter[component.VolatileID].Y = virtualPiece.FixedPosition.Y + component[8].Y;
                            EndPointsComponentInner[component.VolatileID].Z = virtualPiece.FixedPosition.Z + component[1].Z;
                            EndPointsComponentOuter[component.VolatileID].Z = virtualPiece.FixedPosition.Z + component[8].Z;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes infeasible EPs
        /// </summary>
        /// <param name="container">The container to prone</param>
        /// <param name="exhaustive">Exhaustiveness of prone</param>
        public void ProneEPs(Container container, bool exhaustive)
        {
            if (!exhaustive)
            {
                ExtremePoints[container.VolatileID] = ExtremePoints[container.VolatileID].Distinct().ToList();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Adds the specified EPs
        /// </summary>
        /// <param name="container">The container to add the EPs to</param>
        /// <param name="eps">The EPs to add</param>
        public void AddEPs(Container container, IEnumerable<MeshPoint> eps)
        {
            if (Configuration.MeritType == MeritFunctionType.MRSU)
            {
                MeshPoint[] epArray = eps.ToArray();
                foreach (var ep in epArray)
                {
                    ep.VolatileID = EPCounter++;
                    ResidualSpace.Add(new MeshPoint() { X = container.Mesh.Length - ep.X, Y = container.Mesh.Width - ep.Y, Z = container.Mesh.Height - ep.Z });
                }
                ExtremePoints[container.VolatileID].AddRange(epArray);
            }
            else
            {
                ExtremePoints[container.VolatileID].AddRange(eps);
            }
        }

        /// <summary>
        /// Adds a single specified EP
        /// </summary>
        /// <param name="container">The container to add the EP to</param>
        /// <param name="ep">The EP to add</param>
        public void AddEP(Container container, MeshPoint ep)
        {
            if (Configuration.MeritType == MeritFunctionType.MRSU)
            {
                ep.VolatileID = EPCounter++;
                ResidualSpace.Add(new MeshPoint() { X = container.Mesh.Length - ep.X, Y = container.Mesh.Width - ep.Y, Z = container.Mesh.Height - ep.Z });
            }
            ExtremePoints[container.VolatileID].Add(ep);
        }

        /// <summary>
        /// Removes the specified EPs
        /// </summary>
        /// <param name="container">The container to remove the EPs from</param>
        /// <param name="ep">The EPs to remove</param>
        public void RemoveEP(Container container, MeshPoint ep)
        {
            ExtremePoints[container.VolatileID].Remove(ep);
        }

        /// <summary>
        /// Clears all EPs from the specified container
        /// </summary>
        /// <param name="container">The container to clear</param>
        public void ClearEPs(Container container)
        {
            ExtremePoints[container.VolatileID].Clear();
        }

        #endregion

        #region Enhanced piece positioning functionality

        /// <summary>
        /// Adds the piece at the given position in the given orientation to the container
        /// </summary>
        /// <param name="container">The container to add the piece to</param>
        /// <param name="piece">The piece to add</param>
        /// <param name="orientation">The orientation to use</param>
        /// <param name="point">The position of the piece</param>
        public void PositionPiece(Container container, VariablePiece piece, int orientation, MeshPoint point)
        {
            this.Add(container, piece, orientation, point);
            // Update endpoints
            EndPointsBoundingBoxInner[piece.VolatileID].X = point.X;
            EndPointsBoundingBoxInner[piece.VolatileID].Y = point.Y;
            EndPointsBoundingBoxInner[piece.VolatileID].Z = point.Z;
            EndPointsBoundingBoxOuter[piece.VolatileID].X = point.X + this.OrientedPieces[piece.VolatileID].BoundingBox.Length;
            EndPointsBoundingBoxOuter[piece.VolatileID].Y = point.Y + this.OrientedPieces[piece.VolatileID].BoundingBox.Width;
            EndPointsBoundingBoxOuter[piece.VolatileID].Z = point.Z + this.OrientedPieces[piece.VolatileID].BoundingBox.Height;
            if (EndPointsComponentInner != null)
            {
                foreach (var component in piece[orientation].Components)
                {
                    EndPointsComponentInner[component.VolatileID].X = point.X + component.RelPosition.X;
                    EndPointsComponentInner[component.VolatileID].Y = point.Y + component.RelPosition.Y;
                    EndPointsComponentInner[component.VolatileID].Z = point.Z + component.RelPosition.Z;
                    EndPointsComponentOuter[component.VolatileID].X = point.X + component.RelPosition.X + component.Length;
                    EndPointsComponentOuter[component.VolatileID].Y = point.Y + component.RelPosition.Y + component.Width;
                    EndPointsComponentOuter[component.VolatileID].Z = point.Z + component.RelPosition.Z + component.Height;
                }
            }
            // Update meta-info
            switch (Configuration.MeritType)
            {
                case MeritFunctionType.MMPSXY:
                case MeritFunctionType.LPXY:
                    {
                        if (EndPointsBoundingBoxOuter[piece.VolatileID].X > PackingMaxX[container.VolatileID])
                        {
                            PackingMaxX[container.VolatileID] = EndPointsBoundingBoxOuter[piece.VolatileID].X;
                        }
                        if (EndPointsBoundingBoxOuter[piece.VolatileID].Y > PackingMaxY[container.VolatileID])
                        {
                            PackingMaxY[container.VolatileID] = EndPointsBoundingBoxOuter[piece.VolatileID].Y;
                        }
                    }
                    break;
                case MeritFunctionType.MRSU:
                    {
                        foreach (var ep in ExtremePoints[container.VolatileID])
                        {
                            if (ep.Z >= point.Z && ep.Z < point.Z + piece[orientation].BoundingBox.Height)
                            {
                                if (ep.X <= point.X && point.Y <= ep.Y && ep.Y <= point.Y + piece[orientation].BoundingBox.Width)
                                {
                                    ResidualSpace[ep.VolatileID].X = Math.Min(ResidualSpace[ep.VolatileID].X, point.X - ep.X);
                                }
                                if (ep.Y <= point.Y && point.X <= ep.X && ep.X <= point.X + piece[orientation].BoundingBox.Length)
                                {
                                    ResidualSpace[ep.VolatileID].Y = Math.Min(ResidualSpace[ep.VolatileID].Y, point.Y - ep.Y);
                                }
                            }
                            if (ep.Z <= point.Z &&
                                point.X <= ep.X && ep.X <= point.X + piece[orientation].BoundingBox.Length &&
                                point.Y <= ep.Y && ep.Y <= point.Y + piece[orientation].BoundingBox.Width)
                            {
                                ResidualSpace[ep.VolatileID].Z = Math.Min(ResidualSpace[ep.VolatileID].Z, point.Z - ep.Z);
                            }
                        }
                    }
                    break;
                case MeritFunctionType.None:
                case MeritFunctionType.MFV:
                default:
                    break;
            }
        }

        /// <summary>
        /// Adds the piece at the given position in the given orientation to the container
        /// </summary>
        /// <param name="container">The container to add the piece to</param>
        /// <param name="piece">The piece to add</param>
        /// <param name="box">The box which defines the insertion origin or anchor</param>
        /// <param name="vertexID">The anchor to use of the specified box</param>
        /// <param name="orientation">The orientation to use</param>
        /// <param name="point">The position of the piece</param>
        public void PositionPiece(Container container, VariablePiece piece, MeshCube box, int vertexID, int orientation, MeshPoint point)
        {
            MeshPoint insertionPoint = new MeshPoint()
            {
                X = point.X - ((box == null) ? piece[orientation].BoundingBox[vertexID].X : box[vertexID].X),
                Y = point.Y - ((box == null) ? piece[orientation].BoundingBox[vertexID].Y : box[vertexID].Y),
                Z = point.Z - ((box == null) ? piece[orientation].BoundingBox[vertexID].Z : box[vertexID].Z)
            };
            // Update endpoints
            this.Add(container, piece, orientation, insertionPoint);
            EndPointsBoundingBoxInner[piece.VolatileID].X = insertionPoint.X;
            EndPointsBoundingBoxInner[piece.VolatileID].Y = insertionPoint.Y;
            EndPointsBoundingBoxInner[piece.VolatileID].Z = insertionPoint.Z;
            EndPointsBoundingBoxOuter[piece.VolatileID].X = insertionPoint.X + this.OrientedPieces[piece.VolatileID].BoundingBox.Length;
            EndPointsBoundingBoxOuter[piece.VolatileID].Y = insertionPoint.Y + this.OrientedPieces[piece.VolatileID].BoundingBox.Width;
            EndPointsBoundingBoxOuter[piece.VolatileID].Z = insertionPoint.Z + this.OrientedPieces[piece.VolatileID].BoundingBox.Height;
            if (EndPointsComponentInner != null)
            {
                foreach (var component in piece[orientation].Components)
                {
                    EndPointsComponentInner[component.VolatileID].X = insertionPoint.X + component.RelPosition.X;
                    EndPointsComponentInner[component.VolatileID].Y = insertionPoint.Y + component.RelPosition.Y;
                    EndPointsComponentInner[component.VolatileID].Z = insertionPoint.Z + component.RelPosition.Z;
                    EndPointsComponentOuter[component.VolatileID].X = insertionPoint.X + component.RelPosition.X + component.Length;
                    EndPointsComponentOuter[component.VolatileID].Y = insertionPoint.Y + component.RelPosition.Y + component.Width;
                    EndPointsComponentOuter[component.VolatileID].Z = insertionPoint.Z + component.RelPosition.Z + component.Height;
                }
            }
            // Update meta-info
            switch (Configuration.MeritType)
            {
                case MeritFunctionType.MMPSXY:
                case MeritFunctionType.LPXY:
                    {
                        if (EndPointsBoundingBoxOuter[piece.VolatileID].X > PackingMaxX[container.VolatileID])
                        {
                            PackingMaxX[container.VolatileID] = EndPointsBoundingBoxOuter[piece.VolatileID].X;
                        }
                        if (EndPointsBoundingBoxOuter[piece.VolatileID].Y > PackingMaxY[container.VolatileID])
                        {
                            PackingMaxY[container.VolatileID] = EndPointsBoundingBoxOuter[piece.VolatileID].Y;
                        }
                    }
                    break;
                case MeritFunctionType.MRSU:
                    {
                        foreach (var component in piece[orientation].Components)
                        {
                            foreach (var ep in ExtremePoints[container.VolatileID])
                            {
                                if (ep.Z >= insertionPoint.Z && ep.Z < insertionPoint.Z + component.Height)
                                {
                                    if (ep.X <= insertionPoint.X && insertionPoint.Y <= ep.Y && ep.Y <= insertionPoint.Y + component.Width)
                                    {
                                        ResidualSpace[ep.VolatileID].X = Math.Min(ResidualSpace[ep.VolatileID].X, insertionPoint.X - ep.X);
                                    }
                                    if (ep.Y <= insertionPoint.Y && insertionPoint.X <= ep.X && ep.X <= insertionPoint.X + component.Length)
                                    {
                                        ResidualSpace[ep.VolatileID].Y = Math.Min(ResidualSpace[ep.VolatileID].Y, insertionPoint.Y - ep.Y);
                                    }
                                }
                                if (ep.Z <= insertionPoint.Z &&
                                    insertionPoint.X <= ep.X && ep.X <= insertionPoint.X + component.Length &&
                                    insertionPoint.Y <= ep.Y && ep.Y <= insertionPoint.Y + component.Width)
                                {
                                    ResidualSpace[ep.VolatileID].Z = Math.Min(ResidualSpace[ep.VolatileID].Z, insertionPoint.Z - ep.Z);
                                }
                            }
                        }
                    }
                    break;
                case MeritFunctionType.None:
                case MeritFunctionType.MFV:
                default:
                    break;
            }
        }

        /// <summary>
        /// Repositions a piece inside a container
        /// </summary>
        /// <param name="piece">The piece to reposition</param>
        /// <param name="x">The x-value of the new position</param>
        /// <param name="y">The y-value of the new position</param>
        /// <param name="z">The z-value of the new position</param>
        public void RepositionPiece(VariablePiece piece, double x, double y, double z)
        {
            this.Positions[piece.VolatileID].X = x;
            this.Positions[piece.VolatileID].Y = y;
            this.Positions[piece.VolatileID].Z = z;
            EndPointsBoundingBoxInner[piece.VolatileID].X = x;
            EndPointsBoundingBoxInner[piece.VolatileID].Y = y;
            EndPointsBoundingBoxInner[piece.VolatileID].Z = z;
            EndPointsBoundingBoxOuter[piece.VolatileID].X = x + this.OrientedPieces[piece.VolatileID].BoundingBox.Length;
            EndPointsBoundingBoxOuter[piece.VolatileID].Y = y + this.OrientedPieces[piece.VolatileID].BoundingBox.Width;
            EndPointsBoundingBoxOuter[piece.VolatileID].Z = z + this.OrientedPieces[piece.VolatileID].BoundingBox.Height;
            if (EndPointsComponentInner != null)
            {
                foreach (var component in this.OrientedPieces[piece.VolatileID].Components)
                {
                    EndPointsComponentInner[component.VolatileID].X = x + component.RelPosition.X;
                    EndPointsComponentInner[component.VolatileID].Y = y + component.RelPosition.Y;
                    EndPointsComponentInner[component.VolatileID].Z = z + component.RelPosition.Z;
                    EndPointsComponentOuter[component.VolatileID].X = x + component.RelPosition.X + component.Length;
                    EndPointsComponentOuter[component.VolatileID].Y = y + component.RelPosition.Y + component.Width;
                    EndPointsComponentOuter[component.VolatileID].Z = z + component.RelPosition.Z + component.Height;
                }
            }
        }

        /// <summary>
        /// Repositions a piece inside a container
        /// </summary>
        /// <param name="piece">The piece to reposition</param>
        /// <param name="x">The x-value of the new position</param>
        public void RepositionPieceXOnly(VariablePiece piece, double x)
        {
            this.Positions[piece.VolatileID].X = x;
            EndPointsBoundingBoxInner[piece.VolatileID].X = x;
            EndPointsBoundingBoxOuter[piece.VolatileID].X = x + this.OrientedPieces[piece.VolatileID].BoundingBox.Length;
            if (EndPointsComponentInner != null)
            {
                foreach (var component in this.OrientedPieces[piece.VolatileID].Components)
                {
                    EndPointsComponentInner[component.VolatileID].X = x + component.RelPosition.X;
                    EndPointsComponentOuter[component.VolatileID].X = x + component.RelPosition.X + component.Length;
                }
            }
        }

        /// <summary>
        /// Repositions a piece inside a container
        /// </summary>
        /// <param name="piece">The piece to reposition</param>
        /// <param name="y">The y-value of the new position</param>
        public void RepositionPieceYOnly(VariablePiece piece, double y)
        {
            this.Positions[piece.VolatileID].Y = y;
            EndPointsBoundingBoxInner[piece.VolatileID].Y = y;
            EndPointsBoundingBoxOuter[piece.VolatileID].Y = y + this.OrientedPieces[piece.VolatileID].BoundingBox.Width;
            if (EndPointsComponentInner != null)
            {
                foreach (var component in this.OrientedPieces[piece.VolatileID].Components)
                {
                    EndPointsComponentInner[component.VolatileID].Y = y + component.RelPosition.Y;
                    EndPointsComponentOuter[component.VolatileID].Y = y + component.RelPosition.Y + component.Width;
                }
            }
        }

        /// <summary>
        /// Repositions a piece inside a container
        /// </summary>
        /// <param name="piece">The piece to reposition</param>
        /// <param name="z">The z-value of the new position</param>
        public void RepositionPieceZOnly(VariablePiece piece, double z)
        {
            this.Positions[piece.VolatileID].Z = z;
            EndPointsBoundingBoxInner[piece.VolatileID].Z = z;
            EndPointsBoundingBoxOuter[piece.VolatileID].Z = z + this.OrientedPieces[piece.VolatileID].BoundingBox.Height;
            if (EndPointsComponentInner != null)
            {
                foreach (var component in this.OrientedPieces[piece.VolatileID].Components)
                {
                    EndPointsComponentInner[component.VolatileID].Z = z + component.RelPosition.Z;
                    EndPointsComponentOuter[component.VolatileID].Z = z + component.RelPosition.Z + component.Height;
                }
            }
        }

        /// <summary>
        /// Removes a piece from the solution
        /// </summary>
        /// <param name="container">The container the piece is currently placed in</param>
        /// <param name="piece">The piece to remove</param>
        /// <returns>The old position of the piece</returns>
        public MeshPoint DepositionPiece(Container container, VariablePiece piece)
        {
            MeshPoint point = this.Remove(container, piece);
            return point;
        }

        #endregion

        #region Merit scoring

        /// <summary>
        /// Calculates the score of the allocation depending on the used merit-type
        /// </summary>
        /// <param name="container">The container to use</param>
        /// <param name="piece">The piece to allocate</param>
        /// <param name="orientation">The orientation to use</param>
        /// <param name="position">The position to use</param>
        /// <returns>The score of the allocation</returns>
        public double ScorePieceAllocation(Container container, VariablePiece piece, int orientation, MeshPoint position)
        {
            var score = 0.0;
            if (ContainerOrderSupply.OpenContainers.Count > 0 && ContainerOrderSupply.OpenContainers.Contains(container))
            {
                score -= ContainerOrderSupply.OpenContainerBigM;
            }
            switch (Configuration.MeritType)
            {
                case MeritFunctionType.MFV:
                    {
                        score += container.Mesh.Volume - ContainerInfos[container.VolatileID].VolumeContained - piece.Volume;
                    }
                    break;
                case MeritFunctionType.MMPSXY:
                    {
                        if (position.X + piece[orientation].BoundingBox.Length > PackingMaxX[container.VolatileID])
                        {
                            score += position.X + piece[orientation].BoundingBox.Length - PackingMaxX[container.VolatileID];
                        }
                        if (position.Y + piece[orientation].BoundingBox.Width > PackingMaxY[container.VolatileID])
                        {
                            score += position.Y + piece[orientation].BoundingBox.Width - PackingMaxY[container.VolatileID];
                        }
                    }
                    break;
                case MeritFunctionType.LPXY:
                    {
                        if (position.X + piece[orientation].BoundingBox.Length > PackingMaxX[container.VolatileID])
                        {
                            score += (position.X + piece[orientation].BoundingBox.Length - PackingMaxX[container.VolatileID]) * LevelPackingC;
                        }
                        else
                        {
                            score += PackingMaxX[container.VolatileID] - position.X + piece[orientation].BoundingBox.Length;
                        }
                        if (position.Y + piece[orientation].BoundingBox.Width > PackingMaxY[container.VolatileID])
                        {
                            score += (position.Y + piece[orientation].BoundingBox.Width - PackingMaxY[container.VolatileID]) * LevelPackingC;
                        }
                        else
                        {
                            score += PackingMaxY[container.VolatileID] - position.Y + piece[orientation].BoundingBox.Width;
                        }
                    }
                    break;
                case MeritFunctionType.MRSU:
                    {
                        score += ResidualSpace[position.VolatileID].X - piece[orientation].BoundingBox.Length +
                            ResidualSpace[position.VolatileID].Y - piece[orientation].BoundingBox.Width +
                            ResidualSpace[position.VolatileID].Z - piece[orientation].BoundingBox.Height;
                    }
                    break;
                case MeritFunctionType.MEDXYZ:
                    {
                        score += Math.Sqrt(
                                Math.Pow(position.X + piece[orientation].BoundingBox.Length, 2) +
                                Math.Pow(position.Y + piece[orientation].BoundingBox.Width, 2) +
                                Math.Pow(position.Z + piece[orientation].BoundingBox.Height, 2));
                    }
                    break;
                case MeritFunctionType.MEDXY:
                    {
                        score += Math.Sqrt(
                                Math.Pow(position.X + piece[orientation].BoundingBox.Length, 2) +
                                Math.Pow(position.Y + piece[orientation].BoundingBox.Width, 2));
                    }
                    break;
                case MeritFunctionType.H:
                    {
                        score += position.Z + piece[orientation].BoundingBox.Height;
                    }
                    break;
                case MeritFunctionType.None:
                default:
                    break;
            }
            return score;
        }

        #endregion

        #region Solution validation

        /// <summary>
        /// Validates the solution and returns an enumeration of flaws in the solution
        /// </summary>
        /// <returns>An enumeration of flaws of this solution</returns>
        public IEnumerable<Flaw> Validate()
        {
            int trailingDigits = 7;
            foreach (var container in InstanceLinked.Containers)
            {
                // Check overlaps with slants
                foreach (var piece1 in ContainerContent[container.VolatileID].OrderBy(p => p.ID))
                {
                    foreach (var cube1 in OrientedPieces[piece1.VolatileID].Components)
                    {
                        double x1Inner = Positions[piece1.VolatileID].X + cube1.RelPosition.X;
                        double y1Inner = Positions[piece1.VolatileID].Y + cube1.RelPosition.Y;
                        double z1Inner = Positions[piece1.VolatileID].Z + cube1.RelPosition.Z;
                        double x1Outer = Positions[piece1.VolatileID].X + cube1.RelPosition.X + cube1.Length;
                        double y1Outer = Positions[piece1.VolatileID].Y + cube1.RelPosition.Y + cube1.Width;
                        double z1Outer = Positions[piece1.VolatileID].Z + cube1.RelPosition.Z + cube1.Height;
                        foreach (var slant in container.Slants)
                        {
                            if (((slant.NormalVector.X >= 0 ? x1Outer : x1Inner) - slant.Position.X) * slant.NormalVector.X +
                                ((slant.NormalVector.Y >= 0 ? y1Outer : y1Inner) - slant.Position.Y) * slant.NormalVector.Y +
                                ((slant.NormalVector.Z >= 0 ? z1Outer : z1Inner) - slant.Position.Z) * slant.NormalVector.Z > 0)
                            {
                                yield return new Flaw()
                                {
                                    Type = FlawType.OverlapSlant,
                                    Container = container,
                                    Piece1 = piece1,
                                    Cube1 = cube1,
                                    Position1 = new MeshPoint()
                                    {
                                        X = (slant.NormalVector.X >= 0 ? x1Outer : x1Inner),
                                        Y = (slant.NormalVector.Y >= 0 ? y1Outer : y1Inner),
                                        Z = (slant.NormalVector.Z >= 0 ? z1Outer : z1Inner)
                                    },
                                    Slant = slant
                                };
                            }
                        }
                    }
                }

                foreach (var piece1 in ContainerContent[container.VolatileID].OrderBy(p => p.ID))
                {
                    // Check usage of forbidden orientations
                    if (piece1.ForbiddenOrientations.Contains(Orientations[piece1.VolatileID]))
                    {
                        yield return new Flaw()
                        {
                            Type = FlawType.ForbiddenOrientation,
                            Container = container,
                            Piece1 = piece1,
                            Position1 = Positions[piece1.VolatileID]
                        };
                    }

                    foreach (var piece2 in ContainerContent[container.VolatileID].Where(p => p.ID > piece1.ID))
                    {
                        // Check forbidden material loading
                        if (piece1.Material.IncompatibleMaterials.Any(m => piece2.Material.MaterialClass == m))
                        {
                            yield return new Flaw()
                            {
                                Type = FlawType.Compatibility,
                                Container = container,
                                Piece1 = piece1,
                                Position1 = Positions[piece1.VolatileID],
                                Piece2 = piece2,
                                Position2 = Positions[piece2.VolatileID]
                            };
                        }
                        foreach (var cube1 in OrientedPieces[piece1.VolatileID].Components)
                        {
                            // Calculate flb and rrt for piece1
                            double x1Inner = Positions[piece1.VolatileID].X + cube1.RelPosition.X;
                            double y1Inner = Positions[piece1.VolatileID].Y + cube1.RelPosition.Y;
                            double z1Inner = Positions[piece1.VolatileID].Z + cube1.RelPosition.Z;
                            double x1Outer = Positions[piece1.VolatileID].X + cube1.RelPosition.X + cube1.Length;
                            double y1Outer = Positions[piece1.VolatileID].Y + cube1.RelPosition.Y + cube1.Width;
                            double z1Outer = Positions[piece1.VolatileID].Z + cube1.RelPosition.Z + cube1.Height;
                            // Round the values to overcome solver's inaccuracy
                            x1Inner = Math.Round(x1Inner, trailingDigits);
                            y1Inner = Math.Round(y1Inner, trailingDigits);
                            z1Inner = Math.Round(z1Inner, trailingDigits);
                            x1Outer = Math.Round(x1Outer, trailingDigits);
                            y1Outer = Math.Round(y1Outer, trailingDigits);
                            z1Outer = Math.Round(z1Outer, trailingDigits);

                            // Check container bounds
                            if (x1Inner < 0 ||
                                y1Inner < 0 ||
                                z1Inner < 0 ||
                                x1Outer > container.Mesh.Length ||
                                y1Outer > container.Mesh.Width ||
                                z1Outer > container.Mesh.Height)
                            {
                                yield return new Flaw()
                                {
                                    Type = FlawType.OverlapContainer,
                                    Container = container,
                                    Piece1 = piece1,
                                    Cube1 = cube1,
                                    Position1 = new MeshPoint()
                                    {
                                        X = Positions[piece1.VolatileID].X + cube1.RelPosition.X,
                                        Y = Positions[piece1.VolatileID].Y + cube1.RelPosition.Y,
                                        Z = Positions[piece1.VolatileID].Z + cube1.RelPosition.Z
                                    }
                                };
                            }

                            // Check overlaps with other pieces
                            foreach (var cube2 in OrientedPieces[piece2.VolatileID].Components)
                            {
                                // Calculate flb and rrt for piece2
                                double x2Inner = Positions[piece2.VolatileID].X + cube2.RelPosition.X;
                                double y2Inner = Positions[piece2.VolatileID].Y + cube2.RelPosition.Y;
                                double z2Inner = Positions[piece2.VolatileID].Z + cube2.RelPosition.Z;
                                double x2Outer = Positions[piece2.VolatileID].X + cube2.RelPosition.X + cube2.Length;
                                double y2Outer = Positions[piece2.VolatileID].Y + cube2.RelPosition.Y + cube2.Width;
                                double z2Outer = Positions[piece2.VolatileID].Z + cube2.RelPosition.Z + cube2.Height;
                                // Round the values to overcome solver's inaccuracy
                                x2Inner = Math.Round(x2Inner, trailingDigits);
                                y2Inner = Math.Round(y2Inner, trailingDigits);
                                z2Inner = Math.Round(z2Inner, trailingDigits);
                                x2Outer = Math.Round(x2Outer, trailingDigits);
                                y2Outer = Math.Round(y2Outer, trailingDigits);
                                z2Outer = Math.Round(z2Outer, trailingDigits);

                                // Check overlaps
                                if (x1Inner >= x2Outer ||
                                    x1Outer <= x2Inner ||
                                    y1Inner >= y2Outer ||
                                    y1Outer <= y2Inner ||
                                    z1Inner >= z2Outer ||
                                    z1Outer <= z2Inner)
                                {
                                    continue;
                                }
                                else
                                {
                                    yield return new Flaw()
                                    {
                                        Type = FlawType.OverlapPiece,
                                        Container = container,
                                        Piece1 = piece1,
                                        Cube1 = cube1,
                                        Position1 = new MeshPoint()
                                        {
                                            X = Positions[piece1.VolatileID].X + cube1.RelPosition.X,
                                            Y = Positions[piece1.VolatileID].Y + cube1.RelPosition.Y,
                                            Z = Positions[piece1.VolatileID].Z + cube1.RelPosition.Z
                                        },
                                        Piece2 = piece2,
                                        Cube2 = cube2,
                                        Position2 = new MeshPoint()
                                        {
                                            X = Positions[piece2.VolatileID].X + cube2.RelPosition.X,
                                            Y = Positions[piece2.VolatileID].Y + cube2.RelPosition.Y,
                                            Z = Positions[piece2.VolatileID].Z + cube2.RelPosition.Z
                                        }
                                    };
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Information support

        /// <summary>
        /// The (exact) volume of all pieces contained in the solution
        /// </summary>
        public double VolumeContained { get { return ContainerContent.Sum(c => c.Sum(p => p.Original.Components.Sum(com => com.Volume))); } }

        /// <summary>
        /// The relative volume utilized by the assigned pieces.
        /// </summary>
        public double VolumeContainedRelative { get { double conVol = VolumeOfContainers; return conVol > 0 ? VolumeContained / VolumeOfContainers : 0; } }

        /// <summary>
        /// The volume of all containers of the instance
        /// </summary>
        public double VolumeOfContainers { get { return InstanceLinked.Containers.Sum(c => c.Volume); } }

        /// <summary>
        /// The volume of the used containers only
        /// </summary>
        public double VolumeOfContainersInUse { get { return InstanceLinked.Containers.Where(c => ContainerContent[c.VolatileID].Any()).Sum(c => c.Volume); } }

        /// <summary>
        /// The number of active containers
        /// </summary>
        public int NumberOfContainersInUse { get { return InstanceLinked.Containers.Where(c => ContainerContent[c.VolatileID].Any()).Count(); } }

        /// <summary>
        /// The number of pieces packed into containers
        /// </summary>
        public int NumberOfPiecesPacked { get { return InstanceLinked.Containers.Sum(c => ContainerContent[c.VolatileID].Count); } }

        #endregion

        #region IXmlSerializable Members

        public void LoadXML(XmlNode node)
        {
            // ID
            this.ID = int.Parse(node.Attributes[Helper.Check(() => this.ID)].Value);

            // Read components
            foreach (var childNode in node.FirstChild.ChildNodes.OfType<XmlNode>())
            {
                // Load the container by its ID
                Container container = InstanceLinked.Containers.Single(c => c.ID == int.Parse(childNode.Attributes[Helper.Check(() => this.ID)].Value));

                // Load all pieces contained in the bin
                foreach (var pieceNode in childNode.ChildNodes.OfType<XmlNode>())
                {
                    // Get corresponding piece
                    VariablePiece piece = InstanceLinked.Pieces.Single(p => p.ID == int.Parse(pieceNode.Attributes[Helper.Check(() => this.ID)].Value));

                    // Set solution info
                    ContainerContent[container.VolatileID].Add(piece);
                    Orientations[piece.VolatileID] = int.Parse(pieceNode.Attributes[Helper.Check(() => ExportationConstants.XML_ORIENTATION_IDENT)].Value);
                    MeshPoint position = new MeshPoint();
                    position.LoadXML(pieceNode.FirstChild);
                    Positions[piece.VolatileID] = position;
                    Containers[piece.VolatileID] = container;
                    OrientedPieces[piece.VolatileID] = piece[Orientations[piece.VolatileID]];
                }
            }
        }

        public XmlNode WriteXML(XmlDocument document)
        {
            // Create the element
            XmlNode node = document.CreateElement(ExportationConstants.XML_SOLUTION_IDENT);

            // ID
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => this.ID));
            attr.Value = this.ID.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
            node.Attributes.Append(attr);

            // Create bin root
            XmlNode binRoot = document.CreateElement(ExportationConstants.XML_BIN_COLLECTION_IDENT);

            // Append pieces
            foreach (var container in InstanceLinked.Containers)
            {
                // Create node for bin content
                XmlNode binNode = document.CreateElement(ExportationConstants.XML_BIN_IDENT);

                // ID of container belonging to this bin
                attr = document.CreateAttribute(Helper.Check(() => container.ID));
                attr.Value = container.ID.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
                binNode.Attributes.Append(attr);

                // Add
                binRoot.AppendChild(binNode);

                // Add content
                foreach (var piece in ContainerContent[container.VolatileID])
                {
                    // Create node for content element
                    XmlNode itemNode = document.CreateElement(ExportationConstants.XML_ITEM_IDENT);

                    // ID of piece / item
                    attr = document.CreateAttribute(Helper.Check(() => piece.ID));
                    attr.Value = piece.ID.ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
                    itemNode.Attributes.Append(attr);

                    // Add orientation
                    attr = document.CreateAttribute(ExportationConstants.XML_ORIENTATION_IDENT);
                    attr.Value = Orientations[piece.VolatileID].ToString(ExportationConstants.XML_PATTERN, ExportationConstants.XML_FORMATTER);
                    itemNode.Attributes.Append(attr);

                    // Add position
                    itemNode.AppendChild(Positions[piece.VolatileID].WriteXML(document));

                    // Add
                    binNode.AppendChild(itemNode);
                }
            }

            // Attach it
            node.AppendChild(binRoot);

            // Return it
            return node;
        }

        #endregion

        #region JSON I/O

        /// <summary>
        /// Converts the solution to a simplified representation used for JSON exchange.
        /// </summary>
        /// <returns>The simplified representation.</returns>
        public JsonSolution ToJsonSolution()
        {
            // Convert to simplified representation
            var jsonSol = new JsonSolution()
            {
                Containers = InstanceLinked.Containers.Select(c => new JsonSolutionContainer()
                {
                    ID = c.ID,
                    Length = c.Mesh.Length,
                    Width = c.Mesh.Width,
                    Height = c.Mesh.Height,
                    Assignments = ContainerContent[c.VolatileID].Select(p => new JsonAssignment()
                    {
                        Piece = p.ID,
                        Position = new JsonPosition()
                        {
                            X = Positions[p.VolatileID].X,
                            Y = Positions[p.VolatileID].Y,
                            Z = Positions[p.VolatileID].Z,
                            A = RotationMatrices.GetRotationAngles(Orientations[p.VolatileID]).alpha,
                            B = RotationMatrices.GetRotationAngles(Orientations[p.VolatileID]).beta,
                            C = RotationMatrices.GetRotationAngles(Orientations[p.VolatileID]).gamma,
                        },
                        Cubes = p[Orientations[p.VolatileID]].Components.Select(com => new JsonCube()
                        {
                            X = com.RelPosition.X,
                            Y = com.RelPosition.Y,
                            Z = com.RelPosition.Z,
                            Length = com.Length,
                            Width = com.Width,
                            Height = com.Height,
                        }).ToList(),
                        Data = p.Data,
                    }).ToList(),
                }).ToList(),
                Offload = OffloadPieces.Select(p => p.ID).ToList(),
                Data = InstanceLinked.Data,
            };

            // Return the simplified representation
            return jsonSol;
        }
        /// <summary>
        /// Converts the solution to a JSON string.
        /// </summary>
        /// <returns>The JSON string.</returns>
        public string WriteJson()
        {
            // Serialize
            string json = JsonSerializer.Serialize(ToJsonSolution());
            // Return JSON string
            return json;
        }

        #endregion
    }
}
