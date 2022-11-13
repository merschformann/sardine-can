using SC.ObjectModel.IO.Behavior;
using SC.ObjectModel.IO.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SC.ObjectModel.IO
{
    /// <summary>
    /// Contains some JSON (de-)serializer helper methods.
    /// </summary>
    public static class JsonIO
    {
        /// <summary>
        /// Simply used for syncing getters.
        /// </summary>
        private static readonly object SyncLock = new object();
        /// <summary>
        /// The options that must be used for all JSON serialization.
        /// </summary>
        private static JsonSerializerOptions _options = null;
        /// <summary>
        /// The options that must be used for all JSON serialization.
        /// </summary>
        public static JsonSerializerOptions OPTIONS
        {
            get
            {
                lock (SyncLock)
                {
                    // Simply return options, if already present
                    if (_options != null) return _options;
                    // Initiate and return options
                    _options = new JsonSerializerOptions();
                    SetJsonSerializerOptions(_options);
                    return _options;
                }
            }
        }
        /// <summary>
        /// Sets the default JSON serializer options at the given options object.
        /// </summary>
        /// <param name="opts">The options instance (must be unaltered otherwise).</param>
        public static void SetJsonSerializerOptions(JsonSerializerOptions opts)
        {
            opts.Converters.Add(new JsonStringEnumConverter(CapslockNamingPolicy.Capslock));
            opts.IgnoreNullValues = true;
        }

        /// <summary>
        /// Creates a JSON representation of the given object.
        /// </summary>
        /// <typeparam name="T">Representing JSON type (e.g.: <see cref="JsonInstance"/>).</typeparam>
        /// <param name="problemInstance">The object to convert.</param>
        /// <returns>The JSON representation of the object.</returns>
        public static string To<T>(T problemInstance) => JsonSerializer.Serialize(problemInstance, OPTIONS);

        /// <summary>
        /// Deserializes the given JSON to the corresponding object representation.
        /// </summary>
        /// <typeparam name="T">Representing JSON type (e.g.: <see cref="JsonInstance"/>).</typeparam>
        /// <param name="json">The JSON.</param>
        /// <returns>The corresponding object representation.</returns>
        public static T From<T>(string json) => JsonSerializer.Deserialize<T>(json, OPTIONS);

        /// <summary>
        /// Checks basic inconsistencies of the given instance and returns informative errors if found.
        /// </summary>
        /// <param name="instance">The instance to check.</param>
        /// <returns>Returns an error describing the identified problem or <code>null</code> if no problems were found.</returns>
        public static string Validate(JsonInstance instance)
        {
            // Check mandatory
            if (instance == null)
                return "no instance provided";
            if ((instance.Containers?.Count ?? 0) <= 0)
                return "no containers provided";
            if ((instance.Pieces?.Count ?? 0) <= 0)
                return "no pieces provided";

            // Check containers
            var knownContainerIDs = new HashSet<int>();
            foreach (var container in instance.Containers)
            {
                var added = knownContainerIDs.Add(container.ID);
                if (!added)
                    return $"container ID {container.ID} is duplicated";
                if (container.Length <= 0)
                    return $"invalid length {container.Length.ToString(CultureInfo.InvariantCulture)} of container {container.ID}";
                if (container.Width <= 0)
                    return $"invalid width {container.Width.ToString(CultureInfo.InvariantCulture)} of container {container.ID}";
                if (container.Height <= 0)
                    return $"invalid height {container.Height.ToString(CultureInfo.InvariantCulture)} of container {container.ID}";
            }

            // Check pieces
            var knownPieceIDs = new HashSet<int>();
            foreach (var piece in instance.Pieces)
            {
                var added = knownPieceIDs.Add(piece.ID);
                if (!added)
                    return $"piece ID {piece.ID} is duplicated";
                if ((piece.Cubes?.Count ?? 0) <= 0)
                    return $"no cubes for piece {piece.ID} provided";
                foreach (var cube in piece.Cubes)
                {
                    if (cube.Length <= 0)
                        return $"invalid length {cube.Length.ToString(CultureInfo.InvariantCulture)} of cube of piece {piece.ID}";
                    if (cube.Width <= 0)
                        return $"invalid width {cube.Width.ToString(CultureInfo.InvariantCulture)} of cube of piece {piece.ID}";
                    if (cube.Height <= 0)
                        return $"invalid height {cube.Height.ToString(CultureInfo.InvariantCulture)} of cube of piece {piece.ID}";
                    if (cube.X < 0)
                        return $"invalid x-offset {cube.X.ToString(CultureInfo.InvariantCulture)} of cube of piece {piece.ID}";
                    if (cube.Y < 0)
                        return $"invalid y-offset {cube.Y.ToString(CultureInfo.InvariantCulture)} of cube of piece {piece.ID}";
                    if (cube.Z < 0)
                        return $"invalid z-offset {cube.Z.ToString(CultureInfo.InvariantCulture)} of cube of piece {piece.ID}";
                }
            }

            // No errors found
            return null;
        }
    }
}
