using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;
using SC.ObjectModel.IO.Json;
using SC.ObjectModel.Rules;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace SC.ObjectModel
{
    public partial class Instance
    {
        #region Xml exportation

        /// <summary>
        /// Writes the instance to an XML-file without any solutions
        /// </summary>
        /// <param name="path">The path to write to</param>
        public void WriteXMLWithoutSolutions(string path)
        {
            // Delete if existing
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // Prepare
            XmlTextWriter textWriter = new XmlTextWriter(path, Encoding.UTF8);
            textWriter.WriteStartDocument();
            textWriter.WriteStartElement(ExportationConstants.XML_INSTANCE_IDENT);
            textWriter.WriteStartAttribute(ExportationConstants.XML_VERSION_IDENT);
            textWriter.WriteValue(ExportationConstants.XML_VERSION);
            textWriter.WriteEndAttribute();
            textWriter.WriteEndElement();
            textWriter.WriteEndDocument();
            textWriter.Close();

            // Load existing data
            XmlDocument document = new XmlDocument();
            document.Load(path);

            // Get root
            XmlNode root = document.SelectSingleNode("//" + ExportationConstants.XML_INSTANCE_IDENT);

            // Create container root
            XmlNode containerRoot = document.CreateElement(ExportationConstants.XML_CONTAINER_COLLECTION_IDENT);
            root.AppendChild(containerRoot);

            // Submit data
            foreach (var container in Containers)
            {
                containerRoot.AppendChild(container.WriteXML(document));
            }

            // Create pieces root
            XmlNode pieceRoot = document.CreateElement(ExportationConstants.XML_PIECE_COLLECTION_IDENT);
            root.AppendChild(pieceRoot);

            // Submit data
            foreach (var piece in Pieces)
            {
                pieceRoot.AppendChild(piece.WriteXML(document));
            }

            // Save the document
            document.Save(path);
        }

        /// <summary>
        /// Writes the instance to an XML-file
        /// </summary>
        /// <param name="path">The path to write to</param>
        public void WriteXML(string path)
        {
            // Delete if existing
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            // Prepare
            XmlTextWriter textWriter = new XmlTextWriter(path, Encoding.UTF8);
            textWriter.WriteStartDocument();
            textWriter.WriteStartElement(ExportationConstants.XML_INSTANCE_IDENT);
            textWriter.WriteStartAttribute(ExportationConstants.XML_VERSION_IDENT);
            textWriter.WriteValue(ExportationConstants.XML_VERSION);
            textWriter.WriteEndAttribute();
            textWriter.WriteEndElement();
            textWriter.WriteEndDocument();
            textWriter.Close();

            // Load existing data
            XmlDocument document = new XmlDocument();
            document.Load(path);

            // Get root
            XmlNode root = document.SelectSingleNode("//" + ExportationConstants.XML_INSTANCE_IDENT);

            // Name
            XmlAttribute attr = document.CreateAttribute(Helper.Check(() => Name));
            attr.Value = Name;
            root.Attributes.Append(attr);

            // Create container root
            XmlNode containerRoot = document.CreateElement(ExportationConstants.XML_CONTAINER_COLLECTION_IDENT);
            root.AppendChild(containerRoot);

            // Submit data
            foreach (var container in Containers)
            {
                containerRoot.AppendChild(container.WriteXML(document));
            }

            // Create pieces root
            XmlNode pieceRoot = document.CreateElement(ExportationConstants.XML_PIECE_COLLECTION_IDENT);
            root.AppendChild(pieceRoot);

            // Submit data
            foreach (var piece in Pieces)
            {
                pieceRoot.AppendChild(piece.WriteXML(document));
            }

            // Create solutions root
            XmlNode solutionRoot = document.CreateElement(ExportationConstants.XML_SOLUTION_COLLECTION_IDENT);
            root.AppendChild(solutionRoot);

            // Submit data
            foreach (var solution in Solutions.Where(s => s.ContainerContent.Any(c => c.Any())))
            {
                solutionRoot.AppendChild(solution.WriteXML(document));
            }

            // Save the document
            document.Save(path);
        }

        /// <summary>
        /// Reads an instance file from the given path.
        /// </summary>
        /// <param name="path">The path to the instance file</param>
        /// <returns>The read instance</returns>
        public static Instance ReadXML(string path)
        {
            // Prepare XML data
            XmlDocument document = new XmlDocument();
            document.Load(path);
            Instance instance = new Instance();

            // Name
            if (document.SelectSingleNode("//Instance").Attributes["Name"] != null)
                instance.Name = document.SelectSingleNode("//Instance").Attributes["Name"].Value;
            else
                instance.Name = "Default";


            // Read containers
            XmlNode containersRoot = document.SelectSingleNode("//Instance/Containers");
            List<Container> containers = new List<Container>();
            if (containersRoot != null)
            {
                foreach (var childNode in containersRoot.ChildNodes.OfType<XmlNode>())
                {
                    Container container = new Container();
                    container.LoadXML(childNode);
                    containers.Add(container);
                }
            }

            // Read pieces
            XmlNode piecesRoot = document.SelectSingleNode("//Instance/Pieces");
            List<VariablePiece> pieces = new List<VariablePiece>();
            if (piecesRoot != null)
            {
                foreach (var childNode in piecesRoot.ChildNodes.OfType<XmlNode>())
                {
                    VariablePiece piece = new VariablePiece();
                    piece.LoadXML(childNode);
                    pieces.Add(piece);
                }
            }

            // Add all items
            instance.Containers.AddRange(containers);
            instance.Pieces.AddRange(pieces);

            // Read solutions
            XmlNode solutionsRoot = document.SelectSingleNode("//Instance/Solutions");
            List<COSolution> solutions = new List<COSolution>();
            if (solutionsRoot != null)
            {
                foreach (var childNode in solutionsRoot.ChildNodes.OfType<XmlNode>())
                {
                    COSolution solution = instance.CreateSolution(false, MeritFunctionType.None);
                    solution.LoadXML(childNode);
                    solutions.Add(solution);
                }
            }

            // Return
            return instance;
        }

        #endregion

        #region JSON I/O

        /// <summary>
        /// Converts a simplified representation of an instance (used for JSON serialization) to a proper instance.
        /// </summary>
        /// <param name="jsonInstance">The instance in simplified presentation.</param>
        /// <returns>The converted instance.</returns>
        public static Instance FromJsonInstance(JsonInstance jsonInstance)
        {
            // Create instance with given parameters
            Instance instance = new Instance
            {
                Name = jsonInstance.Name
            };
            instance.Containers.AddRange(jsonInstance.Containers.Select(c => new Container()
            {
                ID = c.ID,
                Mesh = new MeshCube()
                {
                    Length = c.Length,
                    Width = c.Width,
                    Height = c.Height
                }
            }));
            instance.Pieces.AddRange(jsonInstance.Pieces.Select(p =>
            {
                var convp = new VariablePiece() { ID = p.ID, };
                if (p.Flags != null)
                    convp.SetFlags(p.Flags.Select(f => (f.FlagId, f.FlagValue)));
                foreach (var comp in p.Cubes)
                    convp.AddComponent(comp.X, comp.Y, comp.Z, comp.Length, comp.Width, comp.Height);
                return convp;
            }));
            if (jsonInstance.Rules != null && jsonInstance.Rules.FlagRules != null)
                instance.Rules = new RuleSet()
                {
                    FlagRules = jsonInstance.Rules.FlagRules.Select(r => new FlagRule()
                    {
                        FlagId = r.FlagId,
                        RuleType = r.RuleType,
                        Parameter = r.Parameter,
                    }).ToList(),
                };
            // Seal it
            foreach (var container in instance.Containers)
                container.Seal();
            foreach (var piece in instance.Pieces)
                piece.Seal();

            // Return new instance
            return instance;
        }
        /// <summary>
        /// Convert this instance into a simplified type for JSON serialization.
        /// </summary>
        /// <returns>The instance as a simplified representation.</returns>
        public JsonInstance ToJsonInstance()
        {
            // Convert to JSON representation
            var jsonInstance = new JsonInstance()
            {
                Name = Name,
                Containers = Containers.Select(c =>
                    new JsonContainer()
                    {
                        ID = c.ID,
                        Length = c.Mesh.Length,
                        Width = c.Mesh.Width,
                        Height = c.Mesh.Height
                    }).ToList(),
                Pieces = Pieces.Select(p =>
                    new JsonPiece()
                    {
                        ID = p.ID,
                        Flags = p.GetFlags().Select(f => new JsonFlag() { FlagId = f.flag, FlagValue = f.value }).ToList(),
                        Cubes = p.Original.Components.Select(c =>
                            new JsonCube()
                            {
                                X = c.RelPosition.X,
                                Y = c.RelPosition.Y,
                                Z = c.RelPosition.Z,
                                Length = c.Length,
                                Width = c.Width,
                                Height = c.Height
                            }).ToList()
                    }).ToList(),
                Rules = new JsonRuleSet()
                {
                    FlagRules = Rules.FlagRules.Select(r => new JsonFlagRule()
                    {
                        FlagId = r.FlagId,
                        RuleType = r.RuleType,
                        Parameter = r.Parameter
                    }).ToList(),
                }
            };
            // Return it
            return jsonInstance;
        }
        /// <summary>
        /// Reads an instance file from the given path.
        /// </summary>
        /// <param name="path">The path to the instance file</param>
        /// <returns>The read instance</returns>
        public static Instance ReadJson(string json)
        {
            // Deserialize JSON
            var jsonInstance = JsonSerializer.Deserialize<JsonInstance>(json);
            // Convert and return it
            return FromJsonInstance(jsonInstance);
        }
        /// <summary>
        /// Converts the instance to a JSON string.
        /// </summary>
        /// <returns>The JSON string.</returns>
        public string WriteJson()
        {
            // Serialize
            string json = JsonSerializer.Serialize(ToJsonInstance());
            // Return JSON string
            return json;
        }

        #endregion

        //#region CSV I/O

        ///// <summary>
        ///// Reads an Excel based instance file from the given path.
        ///// </summary>
        ///// <param name="path">The path to the instance file</param>
        ///// <returns>The read instance</returns>
        //public static Instance ReadExcel(string path)
        //{
        //    // Init
        //    ExcelPackage excelFile = new ExcelPackage(new FileInfo(path));
        //    var containerSheet = excelFile.Workbook.Worksheets[1];
        //    var orderSheet = excelFile.Workbook.Worksheets[2];

        //    List<Container> containers = new List<Container>();
        //    for (int row = 2; containerSheet.Cells[row, 1].Value != null; row++)
        //    {
        //        Container container = new Container()
        //        {
        //            ID = row - 1,
        //            Mesh = new MeshCube()
        //            {
        //                Length = containerSheet.Cells[row, 4].GetValue<double>(),
        //                Width = containerSheet.Cells[row, 5].GetValue<double>(),
        //                Height = containerSheet.Cells[row, 9].GetValue<double>() - containerSheet.Cells[row, 6].GetValue<double>(),
        //            }
        //        };
        //        container.Seal();
        //        containers.Add(container);
        //    }

        //    List<VariablePiece> pieces = new List<VariablePiece>(); int pieceID = 0;
        //    for (int row = 2; orderSheet.Cells[row, 1].Value != null; row++)
        //    {
        //        int quantity = orderSheet.Cells[row, 3].GetValue<int>();
        //        for (int i = 0; i < quantity; i++)
        //        {
        //            VariablePiece piece = new VariablePiece() { ID = (++pieceID), };
        //            // Add the parallelepiped component
        //            piece.AddComponent(0, 0, 0,
        //                orderSheet.Cells[row, 4].GetValue<double>(),
        //                orderSheet.Cells[row, 5].GetValue<double>(),
        //                orderSheet.Cells[row, 6].GetValue<double>());
        //            // Assume only this side up pieces
        //            bool allowTurning = true;
        //            if (allowTurning) piece.ForbiddenOrientations = new HashSet<int>();
        //            else piece.ForbiddenOrientations = new HashSet<int>(MeshConstants.ORIENTATIONS.Except(MeshConstants.ORIENTATIONS_THIS_SIDE_UP));
        //            // Seal it
        //            piece.Seal();
        //            pieces.Add(piece);
        //        }
        //    }

        //    // Create instance
        //    Instance instance = new Instance();
        //    // Add all items
        //    instance.Containers.AddRange(containers);
        //    instance.Pieces.AddRange(pieces);
        //    // Return it
        //    return instance;
        //}

        //#endregion
    }
}
