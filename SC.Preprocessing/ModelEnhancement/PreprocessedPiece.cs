using System.Collections.Generic;
using System.Linq;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;
using SC.Preprocessing.Tools;

namespace SC.Preprocessing.ModelEnhancement
{
    /// <summary>
    /// The preporessed piece is a composition of variable pieces. It hides some variable pieces and is represented as one simpler piece.
    /// </summary>
    public class PreprocessedPiece : VariablePiece
    {
        /// <summary>
        /// hidden pieces
        /// </summary>
        public List<VariablePiece> HiddenPieces = new List<VariablePiece>();

        /// <summary>
        /// relative Positions of the hidden pieces
        /// </summary>
        public List<MeshPoint> HiddenPiecesRelPosition = new List<MeshPoint>();

        /// <summary>
        /// orientation of the hidden pieces
        /// </summary>
        public List<int> HiddenPiecesOrientation = new List<int>();

        /// <summary>
        /// listeners for preprocessing events
        /// </summary>
        public static List<IPreprocessingListener> Listener = new List<IPreprocessingListener>();
        
        /// <summary>
        /// empty constructor
        /// </summary>
        public PreprocessedPiece()
        {
            
        }

        /// <summary>
        /// empty constructor
        /// </summary>
        public PreprocessedPiece(CombinablePair combinablePair, bool complexCubeReduction)
        {
            AddHiddenPiece(combinablePair.Piece1, combinablePair.Piece1Relpos, combinablePair.Piece1Orientation);
            AddHiddenPiece(combinablePair.Piece2, combinablePair.Piece2Relpos, combinablePair.Piece2Orientation);
            Seal(complexCubeReduction); //seal will add the preproPiece to the Shape and delete the hidden onces due to the event onSeal
        }

        /// <summary>
        /// add a hidden piece
        /// </summary>
        public void AddHiddenPiece(VariablePiece piece, MeshPoint relPosition, int orientation)
        {
            HiddenPieces.Add(piece);
            HiddenPiecesRelPosition.Add(relPosition);
            HiddenPiecesOrientation.Add(orientation);
        }

        /// <summary>
        /// finalize the class
        /// </summary>
        /// <param name="components">components that represent the body</param>
        /// <param name="complexCubeReduction">do complex cube reduction</param>
        public void Seal(List<MeshCube> components,bool complexCubeReduction)
        {

            //1. Add all MeshCubes from HiddenPieces
            Original = new ComponentsSet { Components = components };
            ForbiddenOrientations = new HashSet<int>();

            //all the same Material?
            var haveSameMaterial = true;
            var sameMaterial = HiddenPieces[0].Material;

            for (var i = 0; i < HiddenPieces.Count; i++)
            {
                var hiddenPiece = HiddenPieces[i];

                //combine forbidden orientations
                foreach (var forbiddenOrientation in hiddenPiece.ForbiddenOrientations)
                    ForbiddenOrientations.Add(OrientationTranslator.TranslateOrientation(HiddenPiecesOrientation[i], forbiddenOrientation));

                //same Material?
                if (sameMaterial.MaterialClass != HiddenPieces[i].Material.MaterialClass ||
                    HiddenPieces[i].Material.MaterialClass == MaterialClassification.Merged)
                    haveSameMaterial = false;


            }

            //4. Set material restriction
            if (haveSameMaterial)
            {
                Material = sameMaterial;
            }
            else
            {
                //create merged Material
                Material = new MergedMaterial();
                var incompatibleMaterials = Material.IncompatibleMaterials as HashSet<MaterialClassification>;
                if (incompatibleMaterials != null)
                    foreach (var hashSets in HiddenPieces.Select(h => h.Material.IncompatibleMaterials))
                        foreach (var matClass in hashSets) incompatibleMaterials.Add(matClass);
            }

            //reduce components
            RefreshVerticesAndIds();

            if (complexCubeReduction)
            {
                ReduceComponents();
                new CubeReduction(this).TryReduceCubes();
            }
            else
            {
                ReduceComponents();
            }

            RefreshVerticesAndIds();

            //5. Call Base seal to create all orientations
            base.Seal();

            //raise event
            foreach (var listener in Listener)
            {
                listener.OnSeal(this);
            }
        }

        /// <summary>
        /// finalize the class. Components will be derived by hidden components.
        /// </summary>
        public override sealed void Seal()
        {
            Seal(false);
        }

        /// <summary>
        /// finalize the class. Components will be derived by hidden components.
        /// </summary>
        public void Seal(bool complexCubeReduction)
        {
            var components = new List<MeshCube>();

            for (var i = 0; i < HiddenPieces.Count; i++)
            {
                var hiddenPiece = HiddenPieces[i];

                //Super Component Set
                foreach (var newComponent in hiddenPiece[HiddenPiecesOrientation[i]].Components.Select(component => component.Clone()))
                {
                    newComponent.RelPosition.X += HiddenPiecesRelPosition[i].X;
                    newComponent.RelPosition.Y += HiddenPiecesRelPosition[i].Y;
                    newComponent.RelPosition.Z += HiddenPiecesRelPosition[i].Z;
                    components.Add(newComponent);
                }

            }

            Seal(components, complexCubeReduction);
        }

        /// <summary>
        /// reduce compontents by overlapping faces
        /// </summary>
        private void ReduceComponents()
        {
            var merged = true;
            while (merged)
            {
                merged = false;

                //cartesian product
                for (var i = 0; i < Original.Components.Count && !merged; i++)
                {
                    for (var j = 0; j < Original.Components.Count && !merged; j++)
                    {

                        if (i == j)
                            continue;

                        //check if component i and compoend j can be merged

                        // Z
                        // ^       
                        // |      ^ Y          
                        // | 7----------8
                        // |/|  /      /|
                        // 5----------6 |      
                        // | |/       | |
                        // | 3--------|-4
                        // |/         |/
                        // 1----------2---------->X

                        //2 on 1 (Z-Extend)
                        if (Original.Components[i][5] == Original.Components[j][1] &&
                            Original.Components[i][6] == Original.Components[j][2] &&
                            Original.Components[i][7] == Original.Components[j][3] &&
                            Original.Components[i][8] == Original.Components[j][4])
                        {
                            merged = true;

                            var cube = new MeshCube
                            {
                                RelPosition = Original.Components[i].RelPosition,
                                Length = Original.Components[i].Length,
                                Width = Original.Components[i].Width,
                                Height = Original.Components[i].Height + Original.Components[j].Height
                            };

                            Original.Components.Add(cube);

                            if (j > i)
                            {
                                Original.Components.RemoveAt(j);
                                Original.Components.RemoveAt(i);
                            }
                            else
                            {
                                Original.Components.RemoveAt(i);
                                Original.Components.RemoveAt(j);
                            }

                            RefreshVerticesAndIds();
                            continue;
                        }


                        //2 behind 1 (Y-Extend)
                        if (Original.Components[i][3] == Original.Components[j][1] &&
                            Original.Components[i][4] == Original.Components[j][2] &&
                            Original.Components[i][7] == Original.Components[j][5] &&
                            Original.Components[i][8] == Original.Components[j][6])
                        {
                            merged = true;

                            var cube = new MeshCube
                            {
                                RelPosition = Original.Components[i].RelPosition,
                                Length = Original.Components[i].Length,
                                Width = Original.Components[i].Width + Original.Components[j].Width,
                                Height = Original.Components[i].Height
                            };

                            Original.Components.Add(cube);

                            if (j > i)
                            {
                                Original.Components.RemoveAt(j);
                                Original.Components.RemoveAt(i);
                            }
                            else
                            {
                                Original.Components.RemoveAt(i);
                                Original.Components.RemoveAt(j);
                            }

                            RefreshVerticesAndIds();
                            continue;
                        }

                        //2 in front of 1 (X-Extend)
                        if (Original.Components[i][2] == Original.Components[j][1] &&
                            Original.Components[i][4] == Original.Components[j][3] &&
                            Original.Components[i][6] == Original.Components[j][5] &&
                            Original.Components[i][8] == Original.Components[j][7])
                        {
                            merged = true;

                            var cube = new MeshCube
                            {
                                RelPosition = Original.Components[i].RelPosition,
                                Length = Original.Components[i].Length + Original.Components[j].Length,
                                Width = Original.Components[i].Width,
                                Height = Original.Components[i].Height
                            };

                            Original.Components.Add(cube);

                            if (j > i)
                            {
                                Original.Components.RemoveAt(j);
                                Original.Components.RemoveAt(i);
                            }
                            else
                            {
                                Original.Components.RemoveAt(i);
                                Original.Components.RemoveAt(j);
                            }
                            // ReSharper disable once RedundantJumpStatement

                            RefreshVerticesAndIds();
                        }

                    }
                }
            }
        }

        /// <summary>
        /// recompute all ids
        /// </summary>
        private void RefreshVerticesAndIds()
        {
            for (var id = 0; id < Original.Components.Count; id++)
            {
                foreach (var vertexId in MeshConstants.VERTEX_IDS)
                {
                    Original.Components[id][vertexId] = VertexGenerator.GenerateVertex(vertexId, Original.Components[id], this);
                    Original.Components[id].ID = id;
                }
            }

        }

        /// <summary>
        /// Event Interface
        /// </summary>
        public interface IPreprocessingListener
        {
            /// <summary>
            /// event call, on create preprocessed piece finish
            /// </summary>
            /// <param name="sender">finished piece</param>
            void OnSeal(PreprocessedPiece sender);
        }

        /// <summary>
        /// The volume of this piece
        /// </summary>
        public override double Volume
        {
            get
            {
                if (!double.IsNaN(_volume)) return _volume;
                _volume = HiddenPieces.Sum(h => h.Original.Components.Sum(c => c.Volume));   
                return _volume;
            }
        }

        /// <summary>
        /// add new event listener
        /// </summary>
        /// <param name="listener">event listener</param>
        internal static void RegisterListener(IPreprocessingListener listener)
        {
            Listener.Add(listener);
        }

        /// <summary>
        /// remove event listener
        /// </summary>
        /// <param name="listener">event listener</param>
        internal static void DeleteListener(IPreprocessingListener listener)
        {
            Listener.Remove(listener);
        }
    }
}
