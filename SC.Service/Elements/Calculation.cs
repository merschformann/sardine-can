using SC.ObjectModel.IO.Json;
using SC.Service.Elements.IO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace SC.Service.Elements
{
    public class Calculation : IComparable<Calculation>
    {
        public Calculation(int id, JsonJob instance, Action<string> logger)
        {
            Id = id;
            Problem = instance;
            Status = new JsonStatus() { Id = id };
            Logger = logger;
        }

        public int Id { get; private set; }
        public JsonJob Problem { get; private set; }
        public JsonStatus Status { get; private set; }
        public JsonSolution Solution { get; internal set; }

        internal Action<string> Logger { get; private set; }

        public int CompareTo(Calculation other)
        {
            if (other == null) return 1;
            return Problem.Priority.CompareTo(other.Problem.Priority);
        }
    }
}
