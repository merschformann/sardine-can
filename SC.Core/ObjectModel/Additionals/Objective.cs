using System;
using System.Collections.Generic;
using System.Linq;
using SC.Core.ObjectModel.Elements;
using SC.Toolbox;

namespace SC.Core.ObjectModel.Additionals
{
    /// <summary>
    /// Tracks information about the objective.
    /// </summary>
    public class Objective
    {
        /// <summary>
        /// Creates a new instance of the objective.
        /// </summary>
        /// <param name="solution">The solution this objective belongs to.</param>
        public Objective(COSolution solution)
        {
            Solution = solution;
            _heightBigM = solution.InstanceLinked.Containers.Max(c => c.Mesh.Height);
        }

        /// <summary>
        /// The solution this objective belongs to.
        /// </summary>
        private COSolution Solution { get; set; }

        /// <summary>
        /// The objective value of the corresponding solution.
        /// </summary>
        public double Value
        {
            get
            {
                switch (Solution.Configuration.Objective)
                {
                    case ObjectiveType.MaxVolume:
                        return _volumeContained;
                    case ObjectiveType.MaxDensity:
                        {
                            return
                                -Solution.OffloadPieces.Count * _heightBigM * 2
                                - Solution.ContainerInfos.Count(c => c.NumberOfPieces > 0) * _heightBigM
                                - Solution.ContainerInfos.Where(c => c.NumberOfPieces > 0).Sum(c => c.PackingHeight / c.Container.Mesh.Height);
                        }
                    default:
                        throw new ArgumentException($"Unknown objective type: {Solution.Configuration.Objective}");
                }
            }
        }

        /// <summary>
        /// The big M value for container height.
        /// </summary>
        private double _heightBigM;

        /// <summary>
        /// The volume packed inside of the containers.
        /// </summary>
        private double _volumeContained;


        /// <summary>
        /// Adds a piece to the container.
        /// </summary>
        /// <param name="piece">The piece to add.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        /// <param name="position">The position of the piece.</param>
        public void AddPiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            _volumeContained += Solution.Configuration.Tetris ? piece.Volume : piece.Original.BoundingBox.Volume;
        }

        /// <summary>
        /// Removes a piece from the container.
        /// </summary>
        /// <param name="piece">The piece to remove.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        /// <param name="position">The position of the piece.</param>
        public void RemovePiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            _volumeContained -= Solution.Configuration.Tetris ? piece.Volume : piece.Original.BoundingBox.Volume;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void Clear()
        {
            _volumeContained = 0;
        }

        /// <summary>
        /// Clones the container info.
        /// </summary>
        /// <param name="solution">The solution this objective belongs to.</param>
        /// <returns>A new instance of the container info.</returns>
        public Objective Clone(COSolution solution)
        {
            return new Objective(solution)
            {
                _volumeContained = _volumeContained
            };
        }
    }
}
