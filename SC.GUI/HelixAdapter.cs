using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SC.Toolbox;

namespace SC.GUI
{
    /// <summary>
    /// Defines the different available brush types
    /// </summary>
    public enum BrushType
    {
        /// <summary>
        /// A simple and plain brush
        /// </summary>
        Plain,

        /// <summary>
        /// A striped brush using white as the second color
        /// </summary>
        StripedWhite,

        /// <summary>
        /// A striped brush using black as the second color
        /// </summary>
        StripedBlack,

        /// <summary>
        /// A checkered brush using white as the second color
        /// </summary>
        CheckeredWhite,

        /// <summary>
        /// A checkered brush using black as the second color
        /// </summary>
        CheckeredBlack
    }

    /// <summary>
    /// Wraps one instance/solution visualization in an object providing additional context information.
    /// </summary>
    public class HelixVisualization
    {
        /// <summary>
        /// Mapping of all containers to their visuals.
        /// </summary>
        private Dictionary<Container, List<ModelVisual3D>> _containerVisuals { get; set; } = new Dictionary<Container, List<ModelVisual3D>>();
        /// <summary>
        /// Inverse mapping of all container visuals to their respective containers.
        /// </summary>
        private Dictionary<Visual3D, Container> _visualToContainer { get; set; } = new Dictionary<Visual3D, Container>();
        /// <summary>
        /// Mapping of all pieces to their visuals.
        /// </summary>
        private Dictionary<Piece, List<ModelVisual3D>> _pieceVisuals { get; set; } = new Dictionary<Piece, List<ModelVisual3D>>();
        /// <summary>
        /// The offsets of all the containers to draw.
        /// </summary>
        private Dictionary<Container, Point3D> _containerOffsets { get; set; } = new Dictionary<Container, Point3D>();
        /// <summary>
        /// Returns all visuals of this visualization.
        /// </summary>
        public IEnumerable<ModelVisual3D> AllVisuals { get { return _containerVisuals.Values.SelectMany(v => v).Concat(_pieceVisuals.Values.SelectMany(v => v)); } }

        /// <summary>
        /// Sets the offset for the given container (just for meta information).
        /// </summary>
        /// <param name="container">The container to set the offset for.</param>
        /// <param name="x">The x-coordinate of the offset.</param>
        /// <param name="y">The y-coordinate of the offset.</param>
        /// <param name="z">The z-coordinate of the offset.</param>
        internal void SetOffset(Container container, double x, double y, double z) => _containerOffsets[container] = new Point3D(x, y, z);
        /// <summary>
        /// Adds a visual for the given container.
        /// </summary>
        /// <param name="container">The container to add a visual for.</param>
        /// <param name="visual">A visual representing (a part of) the container.</param>
        public void Add(Container container, ModelVisual3D visual)
        {
            if (!_containerVisuals.ContainsKey(container))
                _containerVisuals[container] = new List<ModelVisual3D>();
            _containerVisuals[container].Add(visual);
            _visualToContainer[visual] = container;
            foreach (var child in visual.Children)
                _visualToContainer[child] = container;
        }
        /// <summary>
        /// Adds a visual for the given piece.
        /// </summary>
        /// <param name="piece">The piece to add a visual for.</param>
        /// <param name="visual">A visual representing (a part of) the piece.</param>
        public void Add(Piece piece, ModelVisual3D visual)
        {
            if (!_pieceVisuals.ContainsKey(piece))
                _pieceVisuals[piece] = new List<ModelVisual3D>();
            _pieceVisuals[piece].Add(visual);
        }
        /// <summary>
        /// Returns the visuals associated with the given container.
        /// </summary>
        /// <param name="piece">The container to get the associated visuals for.</param>
        /// <returns>All visuals associated with the container.</returns>
        public IEnumerable<ModelVisual3D> this[Container container] { get => container != null && _containerVisuals.ContainsKey(container) ? _containerVisuals[container] : Enumerable.Empty<ModelVisual3D>(); }
        /// <summary>
        /// Returns the visuals associated with the given piece.
        /// </summary>
        /// <param name="piece">The piece to get the associated visuals for.</param>
        /// <returns>All visuals associated with the piece.</returns>
        public IEnumerable<ModelVisual3D> this[Piece piece] { get => piece != null && _pieceVisuals.ContainsKey(piece) ? _pieceVisuals[piece] : Enumerable.Empty<ModelVisual3D>(); }
        /// <summary>
        /// Returns the container associated with the given visual.
        /// </summary>
        /// <param name="visual">The visual to look-up the container for.</param>
        /// <returns>The container belonging to the visual, or <code>null</code> if there is no container associated with it.</returns>
        public Container GetContainer(Visual3D visual) => visual != null && _visualToContainer.ContainsKey(visual) ? _visualToContainer[visual] : null;
        /// <summary>
        /// Gets the offset of the given container, if specified.
        /// </summary>
        /// <param name="container">The container to lookup the offset for.</param>
        /// <returns>The offset of the container, if present. <code>null</code> if not present.</returns>
        public Point3D GetOffset(Container container) => container != null && _containerOffsets.ContainsKey(container) ? _containerOffsets[container] : new Point3D();
    }

    /// <summary>
    /// Provides basic methods to translate objects into a drawable representation
    /// </summary>
    public static class HelixAdapter
    {
        #region Basic 3D methods

        /// <summary>
        /// Creates a ModelVisual3D containing a text label. <see cref="http://www.ericsink.com/wpf3d/4_Text.html"/>
        /// </summary>
        /// <param name="text">The string</param>
        /// <param name="textColor">The color of the text.</param>
        /// <param name="bDoubleSided">Visible from both sides?</param>
        /// <param name="height">Height of the characters</param>
        /// <param name="center">The center of the label</param>
        /// <param name="over">Horizontal direction of the label</param>
        /// <param name="up">Vertical direction of the label</param>
        /// <returns>Suitable for adding to your Viewport3D</returns>
        public static ModelVisual3D CreateTextLabel3D(
            string text,
            Brush textColor,
            bool bDoubleSided,
            double maxWidth,
            double maxHeight,
            Point3D center,
            Vector3D over,
            Vector3D up)
        {
            // Initiate textblock containing the text of the label
            TextBlock tb = new TextBlock(new Run(text));
            tb.Foreground = textColor;
            tb.FontFamily = new FontFamily("Arial");

            // Use that TextBlock as the brush for a material
            DiffuseMaterial mat = new DiffuseMaterial();
            mat.Brush = new VisualBrush(tb);

            // Assume the characters are square
            double width;
            double height;
            if (text.Length * maxHeight < maxWidth)
            {
                height = maxHeight;
                width = text.Length * maxHeight;
            }
            else
            {
                height = maxWidth / text.Length;
                width = maxWidth;
            }

            // Find the four corners
            // p0 is the lower left corner
            // p1 is the upper left
            // p2 is the lower right
            // p3 is the upper right
            Point3D p0 = center - width / 2 * over - height / 2 * up;
            Point3D p1 = p0 + up * 1 * height;
            Point3D p2 = p0 + over * width;
            Point3D p3 = p0 + up * 1 * height + over * width;

            // Now build the geometry for the sign.  It's just a rectangle made of two triangles, on each side.
            MeshGeometry3D mg = new MeshGeometry3D();
            mg.Positions = new Point3DCollection();
            mg.Positions.Add(p0);    // 0
            mg.Positions.Add(p1);    // 1
            mg.Positions.Add(p2);    // 2
            mg.Positions.Add(p3);    // 3

            if (bDoubleSided)
            {
                mg.Positions.Add(p0);    // 4
                mg.Positions.Add(p1);    // 5
                mg.Positions.Add(p2);    // 6
                mg.Positions.Add(p3);    // 7
            }

            mg.TriangleIndices.Add(0);
            mg.TriangleIndices.Add(3);
            mg.TriangleIndices.Add(1);
            mg.TriangleIndices.Add(0);
            mg.TriangleIndices.Add(2);
            mg.TriangleIndices.Add(3);

            if (bDoubleSided)
            {
                mg.TriangleIndices.Add(4);
                mg.TriangleIndices.Add(5);
                mg.TriangleIndices.Add(7);
                mg.TriangleIndices.Add(4);
                mg.TriangleIndices.Add(7);
                mg.TriangleIndices.Add(6);
            }

            // These texture coordinates basically stretch the TextBox brush to cover the full side of the label.
            mg.TextureCoordinates.Add(new Point(0, 1));
            mg.TextureCoordinates.Add(new Point(0, 0));
            mg.TextureCoordinates.Add(new Point(1, 1));
            mg.TextureCoordinates.Add(new Point(1, 0));

            if (bDoubleSided)
            {
                mg.TextureCoordinates.Add(new Point(1, 1));
                mg.TextureCoordinates.Add(new Point(1, 0));
                mg.TextureCoordinates.Add(new Point(0, 1));
                mg.TextureCoordinates.Add(new Point(0, 0));
            }

            // That's all.  Return the result.
            ModelVisual3D mv3d = new ModelVisual3D();
            mv3d.Content = new GeometryModel3D(mg, mat); ;
            return mv3d;
        }

        /// <summary>
        /// Creates a ModelVisual3D containing a text label. <see cref="http://www.ericsink.com/wpf3d/4_Text.html"/>
        /// </summary>
        /// <param name="text">The string</param>
        /// <param name="textColor">The color of the text.</param>
        /// <param name="bDoubleSided">Visible from both sides?</param>
        /// <param name="height">Height of the characters</param>
        /// <param name="center">The center of the label</param>
        /// <param name="over">Horizontal direction of the label</param>
        /// <param name="up">Vertical direction of the label</param>
        /// <returns>Suitable for adding to your Viewport3D</returns>
        public static ModelVisual3D CreateImageLabel3D(
            ImageBrush labelBrush,
            double width,
            double height,
            Point3D center,
            Vector3D over,
            Vector3D up)
        {
            // Use the brush for a material
            DiffuseMaterial mat = new DiffuseMaterial(labelBrush);

            // Find the four corners
            // p0 is the lower left corner
            // p1 is the upper left
            // p2 is the lower right
            // p3 is the upper right
            Point3D p0 = center - width / 2 * over - height / 2 * up;
            Point3D p1 = p0 + up * 1 * height;
            Point3D p2 = p0 + over * width;
            Point3D p3 = p0 + up * 1 * height + over * width;

            // Now build the geometry for the sign.  It's just a rectangle made of two triangles, on each side.
            MeshGeometry3D mg = new MeshGeometry3D();
            mg.Positions = new Point3DCollection();
            mg.Positions.Add(p0);    // 0
            mg.Positions.Add(p1);    // 1
            mg.Positions.Add(p2);    // 2
            mg.Positions.Add(p3);    // 3

            mg.TriangleIndices.Add(0);
            mg.TriangleIndices.Add(3);
            mg.TriangleIndices.Add(1);
            mg.TriangleIndices.Add(0);
            mg.TriangleIndices.Add(2);
            mg.TriangleIndices.Add(3);

            // These texture coordinates basically stretch the brush to cover the full side of the label.
            mg.TextureCoordinates.Add(new Point(0, 1));
            mg.TextureCoordinates.Add(new Point(0, 0));
            mg.TextureCoordinates.Add(new Point(1, 1));
            mg.TextureCoordinates.Add(new Point(1, 0));

            // That's all.  Return the result.
            ModelVisual3D mv3d = new ModelVisual3D();
            mv3d.Content = new GeometryModel3D(mg, mat); ;
            return mv3d;
        }

        /// <summary>
        /// Generates a checkered brush which can be applied to drawable objects
        /// </summary>
        /// <param name="color">The main color to use</param>
        /// <param name="secondColor">The second color to use</param>
        /// <returns>The brush</returns>
        public static Brush GenerateCheckerBrush(Color color, Color secondColor)
        {
            DrawingBrush brush = new DrawingBrush();

            GeometryDrawing backgroundSquare =
                new GeometryDrawing(
                    new SolidColorBrush(secondColor),
                    null,
                    new RectangleGeometry(new Rect(0, 0, 100, 100)));

            GeometryGroup aGeometryGroup = new GeometryGroup();
            aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 50, 50)));
            aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(50, 50, 50, 50)));

            LinearGradientBrush checkerBrush = new LinearGradientBrush();
            checkerBrush.GradientStops.Add(new GradientStop(color, 0.0));
            checkerBrush.GradientStops.Add(new GradientStop(color, 1.0));

            GeometryDrawing checkers = new GeometryDrawing(checkerBrush, null, aGeometryGroup);

            DrawingGroup checkersDrawingGroup = new DrawingGroup();
            checkersDrawingGroup.Children.Add(backgroundSquare);
            checkersDrawingGroup.Children.Add(checkers);

            brush.Drawing = checkersDrawingGroup;
            brush.Viewport = new Rect(0, 0, 0.25, 0.25);
            brush.TileMode = TileMode.Tile;
            return brush;
        }

        /// <summary>
        /// Generates a striped brush which can be applied to drawable objects
        /// </summary>
        /// <param name="color">The main color to use</param>
        /// <param name="secondColor">The second color to use</param>
        /// <returns>The brush</returns>
        public static Brush GenerateStripeBrush(Color color, Color secondColor)
        {
            LinearGradientBrush brush = new LinearGradientBrush(secondColor, color, new Point(0, 0), new Point(1, 1))
            {
                SpreadMethod = GradientSpreadMethod.Repeat,
                RelativeTransform = new ScaleTransform(0.075, 0.075)
            };
            brush.GradientStops.Add(new GradientStop(secondColor, 0));
            brush.GradientStops.Add(new GradientStop(secondColor, 0.5));
            brush.GradientStops.Add(new GradientStop(color, 0.5));
            brush.GradientStops.Add(new GradientStop(color, 1));
            return brush;
        }

        /// <summary>
        /// Generates a plain brush which can be applied to drawable objects
        /// </summary>
        /// <param name="color">The main color to use</param>
        /// <returns>The brush</returns>
        public static Brush GeneratePlainBrush(Color color)
        {
            return new SolidColorBrush(color);
        }

        #endregion

        #region Translator methods (Point based)

        /// <summary>
        /// Translates a point into a solid drawable object
        /// </summary>
        /// <param name="point">The point to draw</param>
        /// <param name="color">The color of the object</param>
        /// <param name="offSetX">An optional offset by which the object gets shifted along the x-axis</param>
        /// <param name="offSetY">An optional offset by which the object gets shifted along the y-axis</param>
        /// <param name="offSetZ">An optional offset by which the object gets shifted along the z-axis</param>
        /// <returns>The newly composed drawable object</returns>
        public static ModelVisual3D Translate(
            MeshPoint point,
            Color color,
            double offSetX = 0,
            double offSetY = 0,
            double offSetZ = 0,
            double radius = 0.1)
        {
            SphereVisual3D sphere = new SphereVisual3D();
            sphere.Center = new Point3D(
                point.X + offSetX,
                point.Y + offSetY,
                point.Z + offSetZ);
            sphere.Radius = radius;
            sphere.Material = new DiffuseMaterial(GeneratePlainBrush(color));
            sphere.SetName(point.VolatileID.ToString()); // TODO remove debug?
            return sphere;
        }

        #endregion

        #region Translator methods (Mesh based)

        /// <summary>
        /// Translates a mesh into a solid drawable object
        /// </summary>
        /// <param name="mesh">The mesh to translate</param>
        /// <param name="color">The color to use for the object</param>
        /// <param name="offSetX">An optional offset by which the object gets shifted along the x-axis</param>
        /// <param name="offSetY">An optional offset by which the object gets shifted along the y-axis</param>
        /// <param name="offSetZ">An optional offset by which the object gets shifted along the z-axis</param>
        /// <param name="brushType">Indicates the brush to use</param>
        /// <param name="text">The text which is drawn on the object</param>
        /// <param name="hazMatBrush">A brush declaring the class of hazardous materials</param>
        /// <param name="handlingBrush">A brush indicating the handling class of the object</param>
        /// <returns>The newly composed drawable object</returns>
        public static ModelVisual3D Translate(
            MeshCube mesh,
            Color color,
            double offSetX = 0,
            double offSetY = 0,
            double offSetZ = 0,
            BrushType brushType = BrushType.Plain,
            string text = null,
            ImageBrush hazMatBrush = null,
            ImageBrush handlingBrush = null)
        {
            // Init
            BoxVisual3D box = new BoxVisual3D();
            box.Center = new Point3D(
                (mesh.RelPosition.X + mesh.Length / 2.0) + offSetX,
                (mesh.RelPosition.Y + mesh.Width / 2.0) + offSetY,
                (mesh.RelPosition.Z + mesh.Height / 2.0) + offSetZ);
            box.Length = mesh.Length;
            box.Width = mesh.Width;
            box.Height = mesh.Height;
            // Add brush based on supplied type
            box.Material = brushType switch
            {
                BrushType.Plain => new DiffuseMaterial(GeneratePlainBrush(color)),
                BrushType.StripedWhite => new DiffuseMaterial(GenerateStripeBrush(color, Colors.White)),
                BrushType.StripedBlack => new DiffuseMaterial(GenerateStripeBrush(color, Colors.Black)),
                BrushType.CheckeredWhite => new DiffuseMaterial(GenerateCheckerBrush(color, Colors.White)),
                BrushType.CheckeredBlack => new DiffuseMaterial(GenerateCheckerBrush(color, Colors.Black)),
                _ => new DiffuseMaterial(GeneratePlainBrush(color)),
            };
            // Draw text on brush if supplied
            if (text != null && text != "")
            {
                if (handlingBrush == null && hazMatBrush == null)
                {
                    box.Children.Add(CreateTextLabel3D(
                        text,
                        Brushes.Black,
                        false,
                        mesh.Width / 1.1,
                        mesh.Length / 1.1,
                        new Point3D(
                            offSetX + mesh.RelPosition.X + mesh.Length / 2.0,
                            offSetY + mesh.RelPosition.Y + mesh.Width / 2.0,
                            offSetZ + mesh.RelPosition.Z + mesh.Height + 0.001),
                        new Vector3D(0, -1, 0),
                        new Vector3D(1, 0, 0)));
                }
                else
                {
                    box.Children.Add(CreateTextLabel3D(
                        text,
                        Brushes.Black,
                        false,
                        mesh.Width / 1.1,
                        (mesh.Length / 2.0) / 1.1,
                        new Point3D(
                            offSetX + mesh.RelPosition.X + (3.0 * (mesh.Length / 4.0)),
                            offSetY + mesh.RelPosition.Y + mesh.Width / 2.0,
                            offSetZ + mesh.RelPosition.Z + mesh.Height + 0.001),
                        new Vector3D(0, -1, 0),
                        new Vector3D(1, 0, 0)));
                }
            }
            // Draw handling icon on brush if supplied
            if (handlingBrush != null)
            {
                box.Children.Add(CreateImageLabel3D(
                    handlingBrush,
                    Math.Min(mesh.Length / 2.0, mesh.Width / 2.0) / 1.1,
                    Math.Min(mesh.Length / 2.0, mesh.Width / 2.0) / 1.1,
                    new Point3D(
                        offSetX + mesh.RelPosition.X + (1.0 * (mesh.Length / 4.0)),
                        offSetY + mesh.RelPosition.Y + (1.0 * (mesh.Width / 4.0)),
                        offSetZ + mesh.RelPosition.Z + mesh.Height + 0.001),
                    new Vector3D(0, -1, 0),
                    new Vector3D(1, 0, 0)));
            }
            // Draw material icon on brush if supplied
            if (hazMatBrush != null)
            {
                box.Children.Add(CreateImageLabel3D(
                    hazMatBrush,
                    Math.Min(mesh.Length / 2.0, mesh.Width / 2.0) / 1.1,
                    Math.Min(mesh.Length / 2.0, mesh.Width / 2.0) / 1.1,
                    new Point3D(
                        offSetX + mesh.RelPosition.X + (1.0 * (mesh.Length / 4.0)),
                        offSetY + mesh.RelPosition.Y + (3.0 * (mesh.Width / 4.0)),
                        offSetZ + mesh.RelPosition.Z + mesh.Height + 0.001),
                    new Vector3D(0, -1, 0),
                    new Vector3D(1, 0, 0)));
            }


            // Return it
            return box;
        }

        public const double CONTAINER_EDGE_THICKNESS_FRACTION = 0.01;

        /// <summary>
        /// Translates a mesh into a wired drawable object
        /// </summary>
        /// <param name="mesh">The mesh to draw</param>
        /// <param name="x">The x-position of the meshs FLB</param>
        /// <param name="y">The y-position of the meshs FLB</param>
        /// <param name="z">The z-position of the meshs FLB</param>
        /// <param name="thickness">The thickness of the edges</param>
        /// <returns>The newly composed drawable object</returns>
        public static ModelVisual3D TranslateWired(MeshCube mesh, double x, double y, double z, double thickness)
        {
            // Init frame model container
            ModelVisual3D model = new ModelVisual3D();
            // Define edge builder function
            static BoxVisual3D makeEdge(Point3D center, double length, double width, double height)
            {
                return new BoxVisual3D()
                {
                    Material = new DiffuseMaterial(Brushes.Black),
                    Center = center,
                    Length = length,
                    Width = width,
                    Height = height
                };
            };
            // Make bottom frame
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length / 2.0, y, z), mesh.Length, thickness, thickness));
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length / 2.0, y + mesh.Width, z), mesh.Length, thickness, thickness));
            model.Children.Add(makeEdge(new Point3D(x, y + mesh.Width / 2.0, z), thickness, mesh.Width, thickness));
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length, y + mesh.Width / 2.0, z), thickness, mesh.Width, thickness));
            // Make top frame
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length / 2.0, y, z + mesh.Height), mesh.Length, thickness, thickness));
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length / 2.0, y + mesh.Width, z + mesh.Height), mesh.Length, thickness, thickness));
            model.Children.Add(makeEdge(new Point3D(x, y + mesh.Width / 2.0, z + mesh.Height), thickness, mesh.Width, thickness));
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length, y + mesh.Width / 2.0, z + mesh.Height), thickness, mesh.Width, thickness));
            // Make sides
            model.Children.Add(makeEdge(new Point3D(x, y, z + mesh.Height / 2.0), thickness, thickness, mesh.Height));
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length, y, z + mesh.Height / 2.0), thickness, thickness, mesh.Height));
            model.Children.Add(makeEdge(new Point3D(x, y + mesh.Width, z + mesh.Height / 2.0), thickness, thickness, mesh.Height));
            model.Children.Add(makeEdge(new Point3D(x + mesh.Length, y + mesh.Width, z + mesh.Height / 2.0), thickness, thickness, mesh.Height));
            // Return it
            return model;
        }

        #endregion

        #region Translator methods (polygon based)

        /// <summary>
        /// Translates a slant into a drawable object symbolizing the unusable area
        /// </summary>
        /// <param name="slant">The slant</param>
        /// <param name="container">The container the slants belong to</param>
        /// <param name="offsetX">The x-offset</param>
        /// <param name="offsetY">The y-offset</param>
        /// <param name="offsetZ">The z-offset</param>
        /// <returns>A drawable object</returns>
        public static ModelVisual3D Translate(Slant slant, Container container, double offsetX = 0, double offsetY = 0, double offsetZ = 0)
        {
            // Generate cutting plane
            Plane3D cutPlane = new Plane3D(
                new Point3D(slant.Position.X + offsetX, slant.Position.Y + offsetY, slant.Position.Z + offsetZ),
                new Vector3D(slant.NormalVector.X, slant.NormalVector.Y, slant.NormalVector.Z));

            // Build a box to draw the unusable area (TODO: other options!?)
            MeshBuilder mb = new MeshBuilder(false, false);
            mb.AddBox(new Point3D(container.Mesh.Length / 2.0 + offsetX, container.Mesh.Width / 2.0 + offsetY, container.Mesh.Height / 2.0 + offsetZ),
                container.Mesh.Length,
                container.Mesh.Width,
                container.Mesh.Height);
            MeshGeometry3D box = mb.ToMesh();
            GeometryModel3D cutModel = new GeometryModel3D(box, new DiffuseMaterial(new SolidColorBrush(Colors.Black) { Opacity = 0.5 }));
            cutModel.BackMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Black) { Opacity = 0.2 });
            MeshGeometryVisual3D cutVisual = new MeshGeometryVisual3D();
            cutVisual.Content = cutModel;

            // Build the cut group
            CuttingPlaneGroup group = new CuttingPlaneGroup();
            group.CuttingPlanes.Add(cutPlane);
            group.Children.Add(cutVisual);

            // Return it
            return group;
        }

        #endregion

        #region Translator methods (Worker methods)

        /// <summary>
        /// Translates a mesh into a solid drawable object
        /// </summary>
        /// <param name="mesh">The mesh to translate</param>
        /// <param name="color">The color to use for the object</param>
        /// <param name="text">The text to draw on the object</param>
        /// <param name="x">The position's x-value</param>
        /// <param name="y">The position's y-value</param>
        /// <param name="z">The position's z-value</param>
        /// <returns>The newly composed drawable object</returns>
        public static IEnumerable<ModelVisual3D> Translate(ComponentsSet mesh, Color color, string text, double x, double y, double z)
        {
            // Init model list
            List<ModelVisual3D> models = new List<ModelVisual3D>();

            // Determine top component since only this one gets text drawn on it
            MeshCube topComponent = mesh.Components.OrderByDescending(com => com.RelPosition.Z + com.Height).First();

            // Draw all components of the piece at their correct positions
            foreach (var component in mesh.Components)
            {
                // Determine whether the current component is the top component
                if (component == topComponent)
                {
                    models.Add(Translate(component, color, x, y, z, text: text));
                }
                else
                {
                    models.Add(Translate(component, color, x, y, z));
                }
            }

            // Return
            return models;
        }

        /// <summary>
        /// Translates a set of points into drawable objects
        /// </summary>
        /// <param name="points">The enumeration of points to draw</param>
        /// <param name="offSetX">The x-offset</param>
        /// <param name="offSetY">The y-offset</param>
        /// <param name="offSetZ">The z-offset</param>
        /// <param name="radius">The radius to draw the points with.</param>
        /// <returns>The drawable objects</returns>
        public static IEnumerable<ModelVisual3D> Translate(IEnumerable<MeshPoint> points, double offSetX = 0, double offSetY = 0, double offSetZ = 0, double radius = 0.1)
        {
            List<ModelVisual3D> models = new List<ModelVisual3D>();
            foreach (var point in points)
                models.Add(Translate(point, Colors.Black, offSetX, offSetY, offSetZ, radius));
            return models;
        }

        /// <summary>
        /// Translates a complete solution into drawable objects
        /// </summary>
        /// <param name="solution">The solution to translate</param>
        /// <param name="colorProvider">A function providing different colors per piece-ID</param>
        /// <param name="drawText">Defines whether to draw the ID on the piece or not</param>
        /// <param name="markerProvider">Defines whether to use stripes when drawing the model or not</param>
        /// <returns>Drawable objects which visualize the given solution</returns>
        public static HelixVisualization Translate(
            COSolution solution,
            Func<VariablePiece, Color> colorProvider,
            Func<VariablePiece, BrushType> markerProvider,
            Func<VariablePiece, ImageBrush> hazMatBrushProvider,
            Func<VariablePiece, ImageBrush> handlingBrushProvider,
            bool drawText = false)
        {
            // Init visualization
            HelixVisualization visualization = new HelixVisualization();

            // Init offset for container-drawing
            double defaultOffsetContainer = solution.InstanceLinked.Containers.Any() ? solution.InstanceLinked.Containers.Min(c => c.Mesh.Length) * 0.1 : 0;
            double offsetX = 0;

            // Calculate edge thickness of containers
            double containerEdgeThickness = solution.InstanceLinked.Containers.Min(c => Math.Min(c.Mesh.Length, Math.Min(c.Mesh.Width, c.Mesh.Height))) * CONTAINER_EDGE_THICKNESS_FRACTION;

            // Draw all containers and their content
            foreach (var container in solution.InstanceLinked.Containers.OrderByDescending(c => solution.ContainerContent[c.VolatileID].Sum(p => p.Original.Components.Sum(com => com.Volume))))
            {
                // Set offset
                visualization.SetOffset(container, offsetX, 0, 0);
                // Draw container
                var containerMesh = TranslateWired(container.Mesh, offsetX, 0, 0, containerEdgeThickness);
                visualization.Add(container, containerMesh);
                SphereVisual3D origin = new SphereVisual3D
                {
                    Center = new Point3D(offsetX, 0, 0),
                    Radius = 0.1
                };
                visualization.Add(container, origin);
                // Draw slants
                foreach (var slant in container.Slants)
                    visualization.Add(container, Translate(slant, container, offsetX));
                // Draw virtual pieces
                foreach (var virtualPiece in container.VirtualPieces)
                    foreach (var virtualComponent in virtualPiece[virtualPiece.FixedOrientation].Components)
                    {
                        visualization.Add(container, TranslateWired(virtualComponent,
                            virtualPiece.FixedPosition.X + virtualComponent.RelPosition.X + offsetX,
                            virtualPiece.FixedPosition.Y + virtualComponent.RelPosition.Y,
                            virtualPiece.FixedPosition.Z + virtualComponent.RelPosition.Z,
                            containerEdgeThickness));
                    }

                // Draw content of the container
                foreach (var piece in solution.ContainerContent[container.VolatileID])
                {
                    // Determine orientation
                    int orientation = solution.Orientations[piece.VolatileID];

                    // Decide whether to draw text
                    if (!drawText)
                    {
                        // Draw all components of the piece at their correct positions
                        foreach (var component in piece[orientation].Components)
                        {
                            visualization.Add(piece,
                                Translate(
                                    component,
                                    colorProvider(piece),
                                    offsetX + solution.Positions[piece.VolatileID].X,
                                    solution.Positions[piece.VolatileID].Y,
                                    solution.Positions[piece.VolatileID].Z,
                                    brushType: markerProvider(piece)
                                    ));
                        }
                    }
                    else
                    {
                        // Determine top component since only this one gets text drawn on it
                        MeshCube topComponent = piece[orientation].Components.OrderByDescending(com => com.RelPosition.Z + com.Height).First();

                        // Draw all components of the piece at their correct positions
                        foreach (var component in piece[orientation].Components)
                        {
                            // Determine whether the current component is the top component
                            visualization.Add(piece,
                                Translate(
                                    component,
                                    colorProvider(piece),
                                    offsetX + solution.Positions[piece.VolatileID].X,
                                    solution.Positions[piece.VolatileID].Y,
                                    solution.Positions[piece.VolatileID].Z,
                                    text: component == topComponent ? piece.ID.ToString() : null,
                                    brushType: markerProvider(piece),
                                    hazMatBrush: hazMatBrushProvider(piece),
                                    handlingBrush: handlingBrushProvider(piece)
                                    ));
                        }
                    }
                }

                // Increase offset for the next container to draw
                offsetX += container.Mesh.Length + defaultOffsetContainer;
            }

            // Draw offload
            double offSetOffloadX = offsetX + 1;
            double offsetOffloadXDelta = ((solution.InstanceLinked.Pieces != null && solution.InstanceLinked.Pieces.Any()) ? solution.InstanceLinked.Pieces.Min(p => p.Original.BoundingBox.Length) : 0) * 0.1;
            double offsetOffloadYDelta = ((solution.InstanceLinked.Pieces != null && solution.InstanceLinked.Pieces.Any()) ? solution.InstanceLinked.Pieces.Min(p => p.Original.BoundingBox.Width) : 0) * 0.1;
            double offSetOffloadXMax = ((solution.InstanceLinked.Pieces != null && solution.InstanceLinked.Pieces.Any()) ? solution.InstanceLinked.Pieces.Max(p => p.Original.BoundingBox.Length) : 0) * 6;
            double offSetOffloadY = 0;
            double offloadMaxY = 0;
            // Translate all pieces
            foreach (var piece in solution.InstanceLinked.Pieces.Except(solution.ContainerContent.SelectMany(c => c)))
            {
                // Draw components of the piece
                foreach (var component in piece.Original.Components)
                {
                    // Determine top component since only this one gets text drawn on it
                    MeshCube topComponent = piece.Original.Components.OrderByDescending(com => com.RelPosition.Z + com.Height).First();

                    // Decide whether to draw text
                    visualization.Add(piece,
                        Translate(
                            component,
                            colorProvider(piece),
                            offSetOffloadX,
                            offSetOffloadY,
                            0,
                            text: drawText && component == topComponent ? piece.ID.ToString() : null,
                            brushType: markerProvider(piece),
                            hazMatBrush: drawText && component == topComponent ? hazMatBrushProvider(piece) : null,
                            handlingBrush: drawText && component == topComponent ? handlingBrushProvider(piece) : null
                            )
                        );
                }

                // Handle offset
                offSetOffloadX += piece.Original.BoundingBox.Length + offsetOffloadXDelta;
                if (offloadMaxY < piece.Original.BoundingBox.Width)
                {
                    offloadMaxY = piece.Original.BoundingBox.Width;
                }
                if (offSetOffloadX > offsetX + offSetOffloadXMax)
                {
                    offSetOffloadY += offloadMaxY + offsetOffloadYDelta;
                    offloadMaxY = 0;
                    offSetOffloadX = offsetX + 1;
                }
            }

            // Return
            return visualization;
        }

        /// <summary>
        /// Transforms all pieces into drawable objects in all supported orientations
        /// </summary>
        /// <param name="pieces">The pieces to draw</param>
        /// <param name="colorProvider">The color provider</param>
        /// <returns>The drawable objects</returns>
        public static IEnumerable<ModelVisual3D> TranslateToOrientationOverview(IEnumerable<Piece> pieces, Func<int, Color> colorProvider, bool wrapAround)
        {
            // Init
            List<ModelVisual3D> models = new List<ModelVisual3D>();
            double offsetX = 0.0;
            double offsetY = 0.0;
            double offsetXDelta = 0.2;
            double offsetYDelta = 0.2;

            // Rotate and translate all pieces
            foreach (var piece in pieces)
            {
                // Add the original
                models.AddRange(
                    Translate(
                        piece.Original,
                        colorProvider(piece.ID),
                        "Original " + piece.ID,
                        offsetX,
                        -(piece.Original.BoundingBox.Width + offsetYDelta),
                        0));

                // Add all orientations
                int counter = 0, maxPerLine = 12;
                double largest = 0;
                foreach (var orientation in MeshConstants.ORIENTATIONS)
                {
                    // Next orientation
                    counter++;
                    largest = Math.Max(largest, piece[orientation].BoundingBox.Length);
                    // Determine top component since only this one gets text drawn on it
                    MeshCube topComponent = piece[orientation].Components.OrderByDescending(com => com.RelPosition.Z + com.Height).First();

                    foreach (var component in piece[orientation].Components)
                    {
                        // Decide whether to draw text
                        if (component == topComponent)
                        {
                            models.Add(
                                Translate(
                                    component,
                                    colorProvider(piece.ID),
                                    offsetX,
                                    offsetY,
                                    0,
                                    text: orientation.ToString()
                                    )
                                );
                        }
                        else
                        {
                            models.Add(
                                Translate(
                                    component,
                                    colorProvider(piece.ID),
                                    offsetX,
                                    offsetY,
                                    0
                                ));
                        }
                    }

                    // Break into lines if desired
                    offsetY += piece[orientation].BoundingBox.Width + offsetYDelta;
                    if (wrapAround && counter % maxPerLine == 0 && counter != MeshConstants.ORIENTATIONS.Length)
                    {
                        offsetX += largest + offsetXDelta;
                        offsetY = 0;
                    }
                }

                // Go to next line
                offsetY = 0;
                offsetX += largest + offsetXDelta;
            }

            // Return
            return models;
        }

        #endregion

        #region Translator methods (wireframe based)

        /// <summary>
        /// Converts a box to a wireframe.
        /// </summary>
        /// <param name="box">The box to convert.</param>
        /// <returns>The wireframe representation of the box.</returns>
        public static LinesVisual3D GetWireframe(BoxVisual3D box)
        {
            LinesVisual3D wireframe = new LinesVisual3D();
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z + box.Height / 2));

            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z + box.Height / 2));

            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X - box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y - box.Width / 2, box.Center.Z + box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z - box.Height / 2));
            wireframe.Points.Add(new Point3D(box.Center.X + box.Length / 2, box.Center.Y + box.Width / 2, box.Center.Z + box.Height / 2));

            return wireframe;
        }

        #endregion
    }
}
