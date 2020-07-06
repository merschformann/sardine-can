using SC.ObjectModel.IO.Behavior;
using SC.ObjectModel.IO.Json;
using System;
using System.Collections.Generic;
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
    }
}
