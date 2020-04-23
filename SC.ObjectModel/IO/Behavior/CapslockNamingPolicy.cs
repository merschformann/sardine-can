using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SC.ObjectModel.IO.Behavior
{
    public class CapslockNamingPolicy : JsonNamingPolicy
    {
        public static CapslockNamingPolicy Capslock { get; } = new CapslockNamingPolicy();

        public override string ConvertName(string name)
        {
            // Simply convert to UPPER CASE
            return name.ToUpperInvariant();
        }
    }
}
