using System.ComponentModel;
using System.Drawing;
using System.Xml.Serialization;
using SC.Core.Heuristics;
using SC.Core.Heuristics.PrimalHeuristic;
using SC.Core.ObjectModel;
using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Elements;
using SC.Core.ObjectModel.Generator;
using SC.Core.ObjectModel.Interfaces;
using SC.Core.Toolbox;
using SC.Core.Linear;
using HelixToolkit;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using Container = SC.Core.ObjectModel.Elements.Container;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = System.Windows.Point;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TextBox = System.Windows.Controls.TextBox;
using Timer = System.Threading.Timer;
using SC.Core.ObjectModel.Configuration;
using System.Globalization;
using System.IO.Enumeration;
using SC.Core.ObjectModel.IO;

namespace SC.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TextBoxWriter _textBoxWriter = null;
        private Timer _timerSolver = null;
        private DateTime _startTime = DateTime.Now;
        private Action _cancelAction;
        private Instance _instance = null;
        private COSolution _solution = null;
        private ColoringMode _coloringType = ColoringMode.Random;
        private ColoringPallet _coloringPallet = ColoringPallet.Beamer;
        private List<Light> _lights = new List<Light>() { new DirectionalLight(Colors.White, new Vector3D(-4, -5, -6)), new DirectionalLight(Colors.White, new Vector3D(5, 6, 4)), };
        private MeritFunctionType _meritType = MeritFunctionType.None;
        private PieceOrderType _pieceOrder = PieceOrderType.VwH;
        private DependencyProperty propertyModelVisual3D;

        public MainWindow()
        {
            // Catch every unhandled exception
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleException);
            // Prepare results dir
            if (!Directory.Exists(ExportationConstants.ExportDir))
                Directory.CreateDirectory(ExportationConstants.ExportDir);
            // Init component
            InitializeComponent();
            // TODO remove!?
            propertyModelVisual3D = DependencyProperty.Register("ModelVisual3D", typeof(ModelVisual3D), typeof(HelixViewport3D));
            // Init components
            _textBoxWriter = new TextBoxWriter(OutputTextBox);
            _timerSolver = new Timer(new TimerCallback(UpdateTimer));
            ComboBoxColoring.Items.Clear();
            foreach (var item in Enum.GetNames(typeof(ColoringMode)))
                ComboBoxColoring.Items.Add(item);
            ComboBoxPallet.Items.Clear();
            foreach (var item in Enum.GetNames(typeof(ColoringPallet)))
                ComboBoxPallet.Items.Add(item);
            // Init camera
            CameraHelper.LookAt(mainViewport.Camera, new Point3D(5, 5, 5), 50, 0);
            CameraHelper.ChangeDirection(mainViewport.Camera, new Vector3D(-1, -1, -1), new Vector3D(0, 0, 1), 0);
            // Init lights
            foreach (var light in _lights)
                mainViewport.Lights.Children.Add(light);
            // Init output-dir
            if (!Directory.Exists(ExportationConstants.ExportDir))
                Directory.CreateDirectory(ExportationConstants.ExportDir);
        }

        static void HandleException(object sender, UnhandledExceptionEventArgs args)
        {
            using (StreamWriter sw = new StreamWriter("UnhandledExceptions.txt", true) { AutoFlush = true })
            {
                sw.WriteLine("Unhandled exception caught!");
                sw.WriteLine("Time: " + DateTime.Now.ToString());
                Exception e = (Exception)args.ExceptionObject;
                sw.WriteLine("Message: " + e.Message);
                sw.WriteLine("StackTrace" + e.StackTrace);
                if (e.InnerException != null)
                {
                    sw.WriteLine("InnerException:");
                    sw.WriteLine("Message: " + e.InnerException.Message);
                    sw.WriteLine("StackTrace" + e.InnerException.StackTrace);
                }
            }
        }

        private void RepositionCamera(IEnumerable<Container> container)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            double offSet = 0;
            foreach (var c in container)
            {
                x += c.Mesh.RelPosition.X + offSet + 0.5 * c.Mesh.Length;
                y += c.Mesh.RelPosition.Y + 0.5 * c.Mesh.Width;
                z += c.Mesh.RelPosition.Z + 0.5 * c.Mesh.Height;
                offSet += c.Mesh.Length + 0.5;
            }
            double width = mainViewport.ActualWidth;
            double height = mainViewport.ActualHeight;
            Point3D center = new Point3D(x / container.Count(), y / container.Count(), z / container.Count());
            double innerConversionFactor = z / x;
            double windowConversionFactor = height / width;
            if (windowConversionFactor < innerConversionFactor)
            {
                windowConversionFactor = 1;
            }
            Point3D size = new Point3D(x / container.Count() * 2 * windowConversionFactor, y / container.Count() * 2 * windowConversionFactor, z / container.Count() * 2 * windowConversionFactor);
            Point3D origin = new Point3D(center.X - (size.X / 2.0), center.Y - (size.Y / 2.0), center.Z - (size.Z / 2.0));
            Rect3D rect = new Rect3D(origin.X, origin.Y, origin.Z, size.X, size.Y, size.Z);
            CameraHelper.ZoomExtents(
                mainViewport.Camera,
                mainViewport.Viewport, rect);
            //CameraHelper.LookAt(this.mainViewport.Camera, new Point3D(x / container.Count(), y / container.Count(), z / container.Count()), 0);
        }

        private List<ModelVisual3D> _lastItems = new List<ModelVisual3D>();

        private Dictionary<Container, List<ModelVisual3D>> _pointsDrawnForContainer = new Dictionary<Container, List<ModelVisual3D>>();

        private void OutputMessage(string message)
        {
            if (message != null)
            {
                try
                {
                    Dispatcher.Invoke(() =>
                            {
                                OutputTextBox.AppendText(message);
                                OutputTextBox.ScrollToEnd();
                            });
                }
                catch (Exception)
                {
                    // Ignore messages thrown due to logging
                }
            }
        }

        private void ClearOutput()
        {
            Dispatcher.Invoke(() =>
            {
                // Clear old output
                OutputTextBox.Clear();
            });
        }

        private void StartSingleSolve()
        {
            _startTime = DateTime.Now;
            _timerSolver = new Timer(new TimerCallback(UpdateTimer), null, 0, 200);
        }

        private void FinishSingleSolve()
        {
            _timerSolver.Change(Timeout.Infinite, Timeout.Infinite);
            _timerSolver.Dispose();
        }

        private void UpdateTimer(object data)
        {
            try
            {
                Dispatcher.Invoke(() =>
                    {
                        // Update the timer
                        TextBlockSolutionTime.Text = (DateTime.Now - _startTime).ToString(@"hh\:mm\:ss");
                    });
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private void DisableButtons()
        {
            Dispatcher.Invoke(() =>
                {
                    // Disable buttons
                    ImageOrientationOverview.Visibility = Visibility.Hidden;
                    ButtonOrientationOverview.IsEnabled = false;
                    ImageLoadShowCase.Visibility = Visibility.Hidden;
                    ButtonLoadShowCase.IsEnabled = false;
                    ImageSaveShowCase.Visibility = Visibility.Hidden;
                    ButtonSaveShowCase.IsEnabled = false;
                    ImageGenerateInstance.Visibility = Visibility.Hidden;
                    ButtonGenerateInstance.IsEnabled = false;
                    ImageGenerateInstanceShortcut.Visibility = Visibility.Hidden;
                    ButtonGenerateInstanceShortcut.IsEnabled = false;
                    ImageGenerateInstanceSet.Visibility = Visibility.Hidden;
                    ButtonGenerateInstanceSet.IsEnabled = false;
                    ImageSaveGenerator.Visibility = Visibility.Hidden;
                    ButtonSaveGenerator.IsEnabled = false;
                    ImageExecuteShowCase.Visibility = Visibility.Hidden;
                    ButtonExecuteShowCase.IsEnabled = false;
                    ImageExecuteShowCaseFast.Visibility = Visibility.Hidden;
                    ButtonExecuteShowCaseFast.IsEnabled = false;
                    ImageExecuteEvaluation.Visibility = Visibility.Hidden;
                    ButtonExecuteEvaluation.IsEnabled = false;
                    ImageCancelSimple.Visibility = Visibility.Visible;
                    ButtonCancelSimple.IsEnabled = true;
                    ImageCancelEvaluation.Visibility = Visibility.Visible;
                    ButtonCancelEvaluation.IsEnabled = true;
                    ImgButtonExecuteFolderEvaluationCancel.Visibility = Visibility.Visible;
                    ButtonExecuteFolderEvaluationCancel.IsEnabled = true;
                    ImgButtonExecuteFolderEvaluation.Visibility = Visibility.Hidden;
                    ButtonExecuteFolderEvaluation.IsEnabled = false;
                    ImagePieceFillingExecute.Visibility = Visibility.Hidden;
                    ButtonPieceFillingExecute.IsEnabled = false;
                    ImagePieceFillingLoad.Visibility = Visibility.Hidden;
                    ButtonPieceFillingLoad.IsEnabled = false;
                });
        }

        private void EnableButtons()
        {
            Dispatcher.Invoke(() =>
            {
                // Re-enable buttons
                ImageOrientationOverview.Visibility = Visibility.Visible;
                ButtonOrientationOverview.IsEnabled = true;
                ImageLoadShowCase.Visibility = Visibility.Visible;
                ButtonLoadShowCase.IsEnabled = true;
                ImageSaveShowCase.Visibility = Visibility.Visible;
                ButtonSaveShowCase.IsEnabled = true;
                ImageGenerateInstance.Visibility = Visibility.Visible;
                ButtonGenerateInstance.IsEnabled = true;
                ImageGenerateInstanceShortcut.Visibility = Visibility.Visible;
                ButtonGenerateInstanceShortcut.IsEnabled = true;
                ImageGenerateInstanceSet.Visibility = Visibility.Visible;
                ButtonGenerateInstanceSet.IsEnabled = true;
                ImageSaveGenerator.Visibility = Visibility.Visible;
                ButtonSaveGenerator.IsEnabled = true;
                ImageExecuteShowCase.Visibility = Visibility.Visible;
                ButtonExecuteShowCase.IsEnabled = true;
                ImageExecuteShowCaseFast.Visibility = Visibility.Visible;
                ButtonExecuteShowCaseFast.IsEnabled = true;
                ImageExecuteEvaluation.Visibility = Visibility.Visible;
                ButtonExecuteEvaluation.IsEnabled = true;
                ImageCancelSimple.Visibility = Visibility.Hidden;
                ButtonCancelSimple.IsEnabled = false;
                ImageCancelEvaluation.Visibility = Visibility.Hidden;
                ButtonCancelEvaluation.IsEnabled = false;
                ImgButtonExecuteFolderEvaluationCancel.Visibility = Visibility.Hidden;
                ButtonExecuteFolderEvaluationCancel.IsEnabled = false;
                ImgButtonExecuteFolderEvaluation.Visibility = Visibility.Visible;
                ButtonExecuteFolderEvaluation.IsEnabled = true;
                ImagePieceFillingExecute.Visibility = Visibility.Visible;
                ButtonPieceFillingExecute.IsEnabled = true;
                ImagePieceFillingLoad.Visibility = Visibility.Visible;
                ButtonPieceFillingLoad.IsEnabled = true;
            });
        }

        private OptimizationGoal GetOptimizationGoalSimple()
        {
            return (OptimizationGoal)Enum.Parse(typeof(OptimizationGoal), ComboBoxGoalSelectorSimple.Text);
        }

        private MethodType GetMethodSimple()
        {
            return (MethodType)Enum.Parse(typeof(MethodType), ComboBoxMethodSelectorSimple.Text);
        }

        private Solvers GetSolverChoiceSimple()
        {
            return (Solvers)Enum.Parse(typeof(Solvers), ComboBoxSolverSelectorSimple.Text);
        }

        private OptimizationGoal GetOptimizationGoalEvaluation()
        {
            return (OptimizationGoal)Enum.Parse(typeof(OptimizationGoal), ComboBoxGoalSelectorEvaluation.Text);
        }

        private MethodType GetMethodEvaluation()
        {
            return (MethodType)Enum.Parse(typeof(MethodType), ComboBoxMethodSelectorEvaluation.Text);
        }

        private Solvers GetSolverChoiceEvaluation()
        {
            return (Solvers)Enum.Parse(typeof(Solvers), ComboBoxSolverSelectorEvaluation.Text);
        }

        private void DrawInstance(COSolution solution)
        {
            DisplayNewSolution(solution, true, ExportationConstants.ExportDir, solution.InstanceLinked.GetIdent());
        }

        private void ResetCamera(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_instance != null && _instance.Containers.Any())
                {
                    RepositionCamera(_instance.Containers);
                }
                else
                {
                    CameraHelper.LookAt(mainViewport.Camera, new Point3D(0, 0, 0), 0);
                }
            });
        }

        private void Validate(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ClearOutput();
                if (_solution != null)
                {
                    OutputMessage("Flaws found in solution:\n");
                    IEnumerable<Flaw> flaws = _solution.Validate();
                    foreach (var flaw in flaws)
                    {
                        switch (flaw.Type)
                        {
                            case FlawType.OverlapContainer:
                                OutputMessage("OOB: " +
                                    flaw.Container.ToString() + " / " +
                                    flaw.Piece1.ToString() + "-" + flaw.Cube1.ToString() + "-" + flaw.Position1.ToString() +
                                    "\n");
                                break;
                            case FlawType.OverlapPiece:
                                OutputMessage("Overlap: " +
                                    flaw.Container.ToString() + " / " +
                                    flaw.Piece1.ToString() + "-" + flaw.Cube1.ToString() + "-" + flaw.Position1.ToString() + " / " +
                                    flaw.Piece2.ToString() + "-" + flaw.Cube2.ToString() + "-" + flaw.Position2.ToString() +
                                    "\n");
                                break;
                            case FlawType.OverlapSlant:
                                OutputMessage("Overlap(Slant): " +
                                    flaw.Container.ToString() + " / " +
                                    flaw.Slant.ToString() + " / " +
                                    flaw.Piece1.ToString() + "-" + flaw.Cube1.ToString() + "-" + flaw.Position1.ToString() +
                                    "\n");
                                break;
                            case FlawType.Compatibility:
                                OutputMessage("Incompatibility: " +
                                    flaw.Container.ToString() + " / " +
                                    flaw.Piece1.ToString() + "-" + ((VariablePiece)flaw.Piece1).Material.MaterialClass.ToString() + " / " +
                                    flaw.Piece2.ToString() + "-" + ((VariablePiece)flaw.Piece2).Material.MaterialClass.ToString() + "\n");
                                break;
                            case FlawType.ForbiddenOrientation:
                                OutputMessage("ForbiddenOrientation: " +
                                    flaw.Container.ToString() + " / " +
                                    flaw.Piece1.ToString() + "-" + flaw.Orientation1.ToString() + "\n");
                                break;
                            default:
                                break;
                        }
                    }
                    if (flaws.Any())
                    {
                        OutputMessage("End of validation.\n");
                    }
                    else
                    {
                        OutputMessage("No flaws found.\n");
                    }
                }
                else
                {
                    OutputMessage("No solution for validation available!\n");
                }
            });
        }

        private void ShowOrientations(object sender, RoutedEventArgs e)
        {
            // Create sample pieces
            List<VariablePiece> pieces = new List<VariablePiece>();
            VariablePiece pieceSimple = new VariablePiece() { ID = 1 };
            pieceSimple.AddComponent(0, 0, 0, 1, 2, 3);
            pieceSimple.Seal();
            pieces.Add(pieceSimple);
            VariablePiece pieceL = new VariablePiece() { ID = 2 };
            pieceL.AddComponent(0, 0, 0, 3, 1, 1);
            pieceL.AddComponent(0, 0, 1, 1, 1, 1);
            pieceL.Seal();
            pieces.Add(pieceL);
            VariablePiece pieceComplex = new VariablePiece() { ID = 3 };
            pieceComplex.AddComponent(0, 0, 0, 3, 1, 1);
            pieceComplex.AddComponent(0, 0, 1, 1, 2, 1);
            pieceComplex.Seal();
            pieces.Add(pieceComplex);

            // Translate pieces
            Color[] pallet = SelectedPallet;
            List<ModelVisual3D> models = HelixAdapter.TranslateToOrientationOverview(pieces, (int id) => { return pallet[id % pieces.Count]; }, CheckBoxOrientationBreak.IsChecked == true).ToList();
            DisplayNewSolution(models);
        }

        private void RedrawSolution(object sender, RoutedEventArgs e)
        {
            DisplayNewSolution(_solution, true, ExportationConstants.ExportDir, _solution.InstanceLinked.GetIdent());
        }

        private void LoadShowCase(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                // Set filter options and filter index.
                Filter = "XINST Files (.xinst)|*.xinst|JSON Files (.json)|*.json",
                FilterIndex = 1,
                Multiselect = false
            };

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = fileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                // Read instance from xml
                try
                {
                    if (fileDialog.FileName.EndsWith(".xinst"))
                        _instance = Instance.ReadXML(fileDialog.FileName);
                    else if (fileDialog.FileName.EndsWith(".json"))
                        _instance = Instance.ReadJson(File.ReadAllText(fileDialog.FileName));
                    else
                    { MessageBox.Show(this, "Unknown instance file ending: " + fileDialog.FileName); }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        "Error parsing instance: " + ex.Message + Environment.NewLine + ex.StackTrace + Environment.NewLine +
                        (ex.InnerException != null ? "Inner:" + ex.InnerException.Message + Environment.NewLine + ex.InnerException.StackTrace + Environment.NewLine : ""),
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Clear output
                ClearOutput();

                foreach (var container in _instance.Containers)
                {
                    OutputMessage(container.ToString() + "\n");
                }

                if (CheckBoxDrawSolution.IsChecked == true && _instance.Solutions.Any())
                {
                    // Get the best available solution
                    COSolution bestSolution = _instance.Solutions.OrderByDescending(s => s.VolumeContained / s.VolumeOfContainersInUse).First();

                    // Draw solution
                    DrawInstance(bestSolution);

                    // Update utilization indicator
                    double contentVolume = bestSolution.VolumeContained;
                    double containerVolume = bestSolution.VolumeOfContainersInUse;
                    double utilizationInPercent = ((containerVolume > 0) ? contentVolume / containerVolume : 0) * 100;
                    ProgressBarVolumeUtilization.Value = utilizationInPercent;

                    // Log message
                    OutputMessage("Loaded solution with ID: " + bestSolution.ID + "\n");
                    OutputMessage("Instance contained " + _instance.Pieces.Count + " items and " + _instance.Containers.Count + " container\n");
                    OutputMessage("Solution uses " + bestSolution.NumberOfContainersInUse + " container\n");
                    OutputMessage("Volume utilization: " +
                        contentVolume.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " / " +
                        containerVolume.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                        " (" + utilizationInPercent.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "%)\n");

                }
                else
                {
                    // Only draw instance
                    DrawInstance(_instance.CreateSolution(new Configuration() { Tetris = false, }));
                }
            }
        }

        private void SaveShowCase(object sender, RoutedEventArgs e)
        {
            if (_instance != null)
            {
                SaveFileDialog dialog = new SaveFileDialog();
                dialog.FileName = _instance.Name; // Default file name
                dialog.DefaultExt = ".xinst"; // Default file extension
                dialog.Filter = "XINST Files (.xinst)|*.xinst|JSON Files (.json)|*.json"; // Filter files by extension

                // Show save file dialog box
                bool? userClickedOK = dialog.ShowDialog();

                // Process save file dialog box results
                if (userClickedOK == true)
                {
                    // Save document
                    var filename = dialog.FileName;
                    if (filename.ToLower().EndsWith(".json"))
                    {
                        File.WriteAllText(filename, _instance.WriteJson());
                        if (_instance.Solutions.Any())
                        {
                            var solutionFilename = filename[..^5] + ".solution.json";
                            File.WriteAllText(solutionFilename, JsonIO.To(_instance.Solutions.Last().ToJsonSolution()));
                        }
                    }
                    else
                        _instance.WriteXML(filename);
                }
            }
        }

        private void GenerateInstance(object sender, RoutedEventArgs e)
        {
            int count;
            if (!int.TryParse(InstanceCount.Text, out count) || count != 1)
            {
                MessageBox.Show("Please generate one instance or choose the button multiple instances!");
                return;
            }

            int seed = int.Parse(BoxSeedSimple.Text);
            _instance = GenerateNewInstance(seed);
        }

        private Instance GenerateNewInstance(int seed, bool draw = true, bool writeXml = false, string path = "")
        {
            Instance instance = null;
            try
            {
                // Get parameters
                int containerCount = int.Parse(BoxContainerCountSimple.Text);
                int pieceCount = int.Parse(BoxPieceCountSimple.Text);
                double minSize = double.Parse(BoxMinSizeSimple.Text, ExportationConstants.FORMATTER);
                double maxSize = double.Parse(BoxMaxSizeSimple.Text, ExportationConstants.FORMATTER);
                double containerMinSize = double.Parse(BoxContainerMinSizeSimple.Text, ExportationConstants.FORMATTER);
                double containerMaxSize = double.Parse(BoxContainerMaxSizeSimple.Text, ExportationConstants.FORMATTER);
                int pieceMinEquals = int.Parse(BoxMinEqualsSimple.Text);
                int pieceMaxEquals = int.Parse(BoxMaxEqualsSimple.Text);
                int rounding = int.Parse(BoxRoundingSimple.Text);
                int weightBox = int.Parse(BoxTetrisShapeWeightBox.Text);
                int weightL = int.Parse(BoxTetrisShapeWeightL.Text);
                int weightT = int.Parse(BoxTetrisShapeWeightT.Text);
                int weightU = int.Parse(BoxTetrisShapeWeightU.Text);
                double lengthBreakL = double.Parse(BoxTetrisLengthBreakL.Text, ExportationConstants.FORMATTER);
                double lengthBreakT = double.Parse(BoxTetrisLengthBreakT.Text, ExportationConstants.FORMATTER);
                double lengthBreakU = double.Parse(BoxTetrisLengthBreakU.Text, ExportationConstants.FORMATTER);
                int containerCountLD3 = int.Parse(BoxLD3CountSimple.Text);
                int containerCountAKW = int.Parse(BoxAKWCountSimple.Text);
                int containerCountAMP = int.Parse(BoxAMPCountSimple.Text);
                int containerCountRKN = int.Parse(BoxRKNCountSimple.Text);
                int containerCountAMJ = int.Parse(BoxAMJCountSimple.Text);

                if (lengthBreakL < 0 || lengthBreakL > 1 ||
                    lengthBreakT < 0 || lengthBreakT > 1 ||
                    lengthBreakU < 0 || lengthBreakU > 1)
                    throw new ArgumentException("Thickness is limited to [0,1]");
                if (containerCount < 0 ||
                    pieceCount < 0 ||
                    minSize < 0 ||
                    maxSize < 0 ||
                    containerMinSize < 0 ||
                    containerMaxSize < 0 ||
                    pieceMinEquals < 0 ||
                    pieceMaxEquals < 0 ||
                    rounding < 0 ||
                    weightBox < 0 ||
                    weightL < 0 ||
                    weightT < 0 ||
                    weightU < 0 ||
                    containerCountLD3 < 0 ||
                    containerCountAKW < 0 ||
                    containerCountAMP < 0 ||
                    containerCountRKN < 0 ||
                    containerCountAMJ < 0)
                {
                    throw new ArgumentException("No negative values allowed");
                }
                bool tetris = CheckBoxTetrisSimple.IsChecked == true;
                bool realistic = CheckBoxRealisticContainers.IsChecked == true;
                if (containerCountLD3 + containerCountAKW + containerCountAMP + containerCountRKN + containerCountAMJ == 0 && realistic)
                {
                    throw new ArgumentException("Specify at least one container to generate");
                }

                // Generate instance
                if (tetris)
                {
                    if (realistic)
                    {
                        instance = new Instance();
                        instance.Containers.AddRange(InstanceGenerator.GenerateRealisticContainers(containerCountLD3, containerCountAKW, containerCountAMP, containerCountRKN, containerCountAMJ));
                        instance.Pieces.AddRange(InstanceGenerator.GenerateTetrisPieces(pieceCount, minSize, maxSize, pieceMinEquals, pieceMaxEquals, weightBox, weightL, weightT, weightU, seed, rounding, lengthBreakL, lengthBreakT, lengthBreakU));
                    }
                    else
                    {
                        instance = new Instance();
                        instance.Containers.AddRange(InstanceGenerator.GenerateContainer(containerCount, containerMinSize, containerMaxSize, rounding, seed));
                        instance.Pieces.AddRange(InstanceGenerator.GenerateTetrisPieces(pieceCount, minSize, maxSize, pieceMinEquals, pieceMaxEquals, weightBox, weightL, weightT, weightU, seed, rounding, lengthBreakL, lengthBreakT, lengthBreakU));
                    }
                }
                else
                {
                    if (realistic)
                    {
                        instance = new Instance();
                        instance.Containers.AddRange(InstanceGenerator.GenerateRealisticContainers(containerCountLD3, containerCountAKW, containerCountAMP, containerCountRKN, containerCountAMJ));
                        instance.Pieces.AddRange(InstanceGenerator.GeneratePieces(pieceCount, maxSize, minSize, pieceMinEquals, pieceMaxEquals, seed, rounding));
                    }
                    else
                    {
                        instance = new Instance();
                        instance.Containers.AddRange(InstanceGenerator.GenerateContainer(containerCount, containerMinSize, containerMaxSize, rounding, seed));
                        instance.Pieces.AddRange(InstanceGenerator.GeneratePieces(pieceCount, maxSize, minSize, pieceMinEquals, pieceMaxEquals, seed, rounding));
                    }
                }

                // Set appropriate name
                instance.Name = instance.GetIdent();

                if (draw)
                {
                    // Draw instance
                    DrawInstance(instance.CreateSolution(new Configuration() { Tetris = false, }));
                }

                if (writeXml)
                {
                    OutputMessage("Generated instance: " + instance.GetIdent() + " (s: " + seed + " r: " + rounding + ")\n");
                    instance.WriteXML(System.IO.Path.Combine(path, instance.GetIdent() + "-s" + seed + "-r" + rounding + ".xinst"));
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Input values not formatted correctly!");
            }
            return instance;
        }

        private void GenerateInstances(object sender, RoutedEventArgs e)
        {
            try
            {
                // Initializing Open Dialog
                var openDialog = new OpenFileDialog()
                {
                    FileName = "AnyFile",
                    Filter = string.Empty,
                    CheckFileExists = false,
                    CheckPathExists = false,
                };

                // Show dialog and take result into account
                bool? result = openDialog.ShowDialog();
                if (result == true)
                {

                    // Get selected folder path
                    string path = System.IO.Path.GetDirectoryName(openDialog.FileName);
                    int count;

                    if (!int.TryParse(InstanceCount.Text, out count) || count < 1)
                    {
                        MessageBox.Show("Please generate at least one instance!");
                        return;
                    }

                    for (var instanceNo = 1; instanceNo <= count; instanceNo++)
                    {
                        GenerateNewInstance(instanceNo, false, true, path);
                    }

                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Input values not formatted correctly!");
            }
        }

        private void ExecuteShowCase(object sender, RoutedEventArgs e)
        {
            if (_instance != null)
            {
                // Solve something
                var method = GetCurrentChoosenSimpleMethods(sender, _instance);

                // Disable buttons
                DisableButtons();
                // Clear output
                ClearOutput();

                // Execute in thread
                OptimizationRunner runner = new OptimizationRunner(method, _instance, ExportationConstants.ExportDir, DisplayNewSolution, EnableButtons, StartSingleSolve, FinishSingleSolve);
                ThreadPool.QueueUserWorkItem(new WaitCallback(runner.Run));
                _cancelAction = runner.Cancel;
            }
        }

        private IMethod GetCurrentChoosenSimpleMethods(object sender, Instance instance)
        {
            MethodType methodToUse = GetMethodSimple();
            IMethod method = null;
            OptimizationGoal goal = GetOptimizationGoalSimple();
            Solvers solverToUse = GetSolverChoiceSimple();
            bool threadsGiven = int.TryParse(BoxThreads.Text, out int threads);
            if (!threadsGiven || threads < 0) threads = 0;
            bool seedParseSuccess = int.TryParse(BoxSeed.Text, out int seed);
            switch (methodToUse)
            {
                case MethodType.FrontLeftBottomStyle:
                    {
                        Configuration configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                        {
                            TimeLimit = TimeSpan.MaxValue,
                            Log = OutputMessage,
                            SubmitSolution = DisplayNewSolution,
                            HandleGravity = (CheckBoxGravity.IsChecked == true),
                            HandleRotatability = (CheckBoxRotation.IsChecked == true),
                            HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                            HandleStackability = (CheckBoxStackability.IsChecked == true),
                            HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                            ThreadLimit = threads,
                            Goal = goal,
                            SolverToUse = solverToUse
                        };
                        method = new SC.Core.Linear.LinearModelFLB(instance, configuration);
                    }
                    break;
                case MethodType.TetrisStyle:
                    {
                        Configuration configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                        {
                            TimeLimit = TimeSpan.MaxValue,
                            Log = OutputMessage,
                            SubmitSolution = DisplayNewSolution,
                            HandleGravity = (CheckBoxGravity.IsChecked == true),
                            HandleRotatability = (CheckBoxRotation.IsChecked == true),
                            HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                            HandleStackability = (CheckBoxStackability.IsChecked == true),
                            HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                            ThreadLimit = threads,
                            Goal = goal,
                            SolverToUse = solverToUse
                        };
                        method = new SC.Core.Linear.LinearModelTetris(instance, configuration);
                    }
                    break;
                case MethodType.HybridStyle:
                    {
                        Configuration configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                        {
                            TimeLimit = TimeSpan.MaxValue,
                            Log = OutputMessage,
                            SubmitSolution = DisplayNewSolution,
                            HandleGravity = (CheckBoxGravity.IsChecked == true),
                            HandleRotatability = (CheckBoxRotation.IsChecked == true),
                            HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                            HandleStackability = (CheckBoxStackability.IsChecked == true),
                            HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                            ThreadLimit = threads,
                            Goal = goal,
                            SolverToUse = solverToUse
                        };
                        method = new SC.Core.Linear.LinearModelHybrid(instance, configuration);
                    }
                    break;
                case MethodType.SpaceIndexed:
                    throw new NotImplementedException("Space indexed not working right now");
                case MethodType.ExtremePointInsertion:
                    {
                        Configuration configuration = null;
                        if (CheckBoxUseTunedParameters.IsChecked == true)
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        else
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                Tetris = (CheckBoxTetris.IsChecked == true),
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                BestFit = _meritType != MeritFunctionType.None,
                                Improvement = (CheckBoxImprovement.IsChecked == true),
                                MeritType = _meritType,
                                PieceOrder = _pieceOrder,
                                PieceReorder = (CheckBoxScoreBasedOrder.IsChecked == true) ? PieceReorderType.Score : PieceReorderType.None,
                                InflateAndReplaceInsertion = false,
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        if (sender == ButtonExecuteShowCaseFast)
                        {
                            configuration.Tetris = false;
                            configuration.Improvement = false;
                            configuration.BestFit = false;
                        }
                        method = new ExtremePointInsertionHeuristic(instance, configuration);
                    }
                    break;
                case MethodType.SpaceDefragmentation:
                    {
                        Configuration configuration = null;
                        if (CheckBoxUseTunedParameters.IsChecked == true)
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        else
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                Tetris = false,
                                BestFit = false,
                                Improvement = (CheckBoxImprovement.IsChecked == true),
                                MeritType = MeritFunctionType.None,
                                PieceOrder = _pieceOrder,
                                PieceReorder = (CheckBoxScoreBasedOrder.IsChecked == true) ? PieceReorderType.Score : PieceReorderType.None,
                                InflateAndReplaceInsertion = true,
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        if (sender == ButtonExecuteShowCaseFast)
                        {
                            configuration.Tetris = false;
                            configuration.Improvement = false;
                            configuration.BestFit = false;
                        }
                        method = new SpaceDefragmentationHeuristic(instance, configuration);
                    }
                    break;
                case MethodType.PushInsertion:
                    {
                        Configuration configuration = null;
                        if (CheckBoxUseTunedParameters.IsChecked == true)
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        else
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                Tetris = (CheckBoxTetris.IsChecked == true),
                                BestFit = false,
                                Improvement = (CheckBoxImprovement.IsChecked == true),
                                MeritType = MeritFunctionType.None,
                                PieceOrder = _pieceOrder,
                                PieceReorder = (CheckBoxScoreBasedOrder.IsChecked == true) ? PieceReorderType.Score : PieceReorderType.None,
                                InflateAndReplaceInsertion = false,
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        if (sender == ButtonExecuteShowCaseFast)
                        {
                            configuration.Tetris = false;
                            configuration.Improvement = false;
                            configuration.BestFit = false;
                        }
                        method = new PushInsertion(instance, configuration);
                    }
                    break;
                case MethodType.ALNS:
                    {
                        Configuration configuration = null;
                        if (CheckBoxUseTunedParameters.IsChecked == true)
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        else
                        {
                            configuration = new Configuration(methodToUse, CheckBoxTetris.IsChecked == true)
                            {
                                TimeLimit = TimeSpan.MaxValue,
                                Log = OutputMessage,
                                SubmitSolution = DisplayNewSolution,
                                Tetris = (CheckBoxTetris.IsChecked == true),
                                HandleGravity = (CheckBoxGravity.IsChecked == true),
                                HandleRotatability = (CheckBoxRotation.IsChecked == true),
                                HandleForbiddenOrientations = (CheckBoxForbiddenOrientation.IsChecked == true),
                                HandleStackability = (CheckBoxStackability.IsChecked == true),
                                HandleCompatibility = (CheckBoxCompatibility.IsChecked == true),
                                BestFit = _meritType != MeritFunctionType.None,
                                Improvement = true,
                                MeritType = _meritType,
                                PieceOrder = _pieceOrder,
                                PieceReorder = (CheckBoxScoreBasedOrder.IsChecked == true) ? PieceReorderType.Score : PieceReorderType.None,
                                InflateAndReplaceInsertion = true,
                                ThreadLimit = threads,
                                Seed = seed
                            };
                        }
                        if (sender == ButtonExecuteShowCaseFast)
                        {
                            configuration.Tetris = false;
                            configuration.Improvement = true;
                            configuration.BestFit = false;
                        }
                        method = new ALNS(instance, configuration);
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown method: " + methodToUse.ToString());
            }

            //set preprocessor
            //var heuristic = method as Heuristic;
            //if (heuristic != null)
            //    heuristic.PreprocessorSteps = _selectedPreprocessingSteps;

            return method;

        }

        private void Cancel(object sender, RoutedEventArgs e) => _cancelAction?.Invoke();

        private void ExecuteEvaluation(object sender, RoutedEventArgs e)
        {
            try
            {
                // Disable controls
                DisableButtons();
                // Enable exportation of solution
                CheckBoxResetCameraAfterDrawing.IsChecked = true;
                // Get parameters
                int containerMin = int.Parse(BoxContainerMinEvaluation.Text);
                int containerMax = int.Parse(BoxContainerMaxEvaluation.Text);
                int pieceCount = int.Parse(BoxPieceCountEvaluation.Text);
                double minSize = double.Parse(BoxMinSizeEvaluation.Text, ExportationConstants.FORMATTER);
                double maxSize = double.Parse(BoxMaxSizeEvaluation.Text, ExportationConstants.FORMATTER);
                double containerMinSize = double.Parse(BoxContainerMinSizeEvaluation.Text, ExportationConstants.FORMATTER);
                double containerMaxSize = double.Parse(BoxContainerMaxSizeEvaluation.Text, ExportationConstants.FORMATTER);
                int pieceMinEquals = int.Parse(BoxMinEqualsEvaluation.Text);
                int pieceMaxEquals = int.Parse(BoxMaxEqualsEvaluation.Text);
                int rounding = int.Parse(BoxRoundingEvaluation.Text);
                int seedPasses = int.Parse(BoxSeedPassesEvaluation.Text);
                InstanceGeneratorConfiguration generatorConfig = new InstanceGeneratorConfiguration()
                {
                    ContainerMax = containerMax,
                    ContainerMin = containerMin,
                    MaxBoxCount = pieceCount,
                    PieceMinSize = minSize,
                    PieceMaxSize = maxSize,
                    ContainerSideLengthMin = containerMinSize,
                    ContainerSideLengthMax = containerMaxSize,
                    PieceMinEquals = pieceMinEquals,
                    PieceMaxEquals = pieceMaxEquals,
                    Rounding = rounding
                };
                // Execute in thread
                EvaluationRunner runner = new EvaluationRunner(
                    GetMethodEvaluation(),
                    ExportationConstants.ExportDir,
                    DisplayNewSolution,
                    OutputMessage,
                    EnableButtons,
                    StartSingleSolve,
                    FinishSingleSolve)
                {
                    SeedPasses = seedPasses,
                    GeneratorConfig = generatorConfig,
                    Config = new Configuration(GetMethodEvaluation(), true)
                    {
                        Goal = GetOptimizationGoalEvaluation(),
                        SolverToUse = GetSolverChoiceEvaluation(),
                        Log = OutputMessage
                    }
                };
                ThreadPool.QueueUserWorkItem(new WaitCallback(runner.Run));
                _cancelAction = runner.Cancel;
            }
            catch (FormatException)
            {
                MessageBox.Show("Input values not formatted correctly!");
                EnableButtons();
            }
        }

        /// <summary>
        /// The color pallet currently in use for drawing items.
        /// </summary>
        private Color[] SelectedPallet
        {
            get => _coloringPallet switch
            {
                ColoringPallet.Beamer => Constants.COLORS_BEAMER,
                ColoringPallet.Full => Constants.COLORS_FULL,
                _ => throw new ArgumentException($"Unknown pallet type: {_coloringPallet}"),
            };
        }
        /// <summary>
        /// Selects a color for the given piece according to configuration.
        /// </summary>
        /// <param name="piece">The piece to select a color for.</param>
        /// <returns>The color to use for the piece.</returns>
        private Color ColorSelector(VariablePiece piece)
        {
            Color[] pallet = SelectedPallet;
            switch (_coloringType)
            {
                case ColoringMode.Random: return pallet[piece.ID % pallet.Length];
                case ColoringMode.ClassDependent: return Constants.COLORS_PER_MATERIAL[piece.Material.MaterialClass];
                case ColoringMode.WireFrame: return Colors.Black;
                case ColoringMode.Flag0: return piece.GetFlag(0).HasValue ? pallet[piece.GetFlag(0).Value % pallet.Length] : Colors.Black;
                case ColoringMode.Flag1: return piece.GetFlag(1).HasValue ? pallet[piece.GetFlag(1).Value % pallet.Length] : Colors.Black;
                case ColoringMode.Flag2: return piece.GetFlag(2).HasValue ? pallet[piece.GetFlag(2).Value % pallet.Length] : Colors.Black;
                case ColoringMode.Flag3: return piece.GetFlag(3).HasValue ? pallet[piece.GetFlag(3).Value % pallet.Length] : Colors.Black;
                default: throw new ArgumentException($"Unknown coloring type: {_coloringType}");
            }
        }

        private void ComboBoxColoring_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Return on not set yet
            if (ComboBoxColoring.SelectedItem == null) return;
            // Get value
            _coloringType = (ColoringMode)Enum.Parse(typeof(ColoringMode), ComboBoxColoring.SelectedItem.ToString());
        }

        private void ComboBoxPallet_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Return on not set yet
            if (ComboBoxPallet.SelectedItem == null) return;
            // Get value
            _coloringPallet = (ColoringPallet)Enum.Parse(typeof(ColoringPallet), ComboBoxPallet.SelectedItem.ToString());
        }

        private void ComboBoxMerit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get value
            _meritType = (MeritFunctionType)Enum.Parse(typeof(MeritFunctionType), ComboBoxMerit.SelectedValue.ToString());
        }

        private void ComboBoxPieceOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Get value
            _pieceOrder = (PieceOrderType)Enum.Parse(typeof(PieceOrderType), ComboBoxPieceOrder.SelectedValue.ToString());
        }

        private void CheckBoxLighting_Checked(object sender, RoutedEventArgs e)
        {
            // Clear old lights
            foreach (var lightToRemove in _lights)
            {
                mainViewport.Lights.Children.Remove(lightToRemove);
            }
            _lights.Clear();

            // Add ambient light (really bright)
            Light light = new AmbientLight(Colors.White);
            mainViewport.Lights.Children.Add(light);
            _lights.Add(light);
        }

        private void CheckBoxLighting_Unchecked(object sender, RoutedEventArgs e)
        {
            // Clear old lights
            foreach (var lightToRemove in _lights)
            {
                mainViewport.Lights.Children.Remove(lightToRemove);
            }
            _lights.Clear();

            // Add directional lights (more smooth)
            Light light1 = new DirectionalLight(Colors.White, new Vector3D(-4, -5, -6));
            Light light2 = new DirectionalLight(Colors.White, new Vector3D(5, 6, 4));
            mainViewport.Lights.Children.Add(light1);
            mainViewport.Lights.Children.Add(light2);
            _lights.Add(light1);
            _lights.Add(light2);
        }

        /// <summary>
        /// Indicates whether the last used mouse-button was the left button
        /// </summary>
        private bool _leftClickedLast = false;

        private void mainViewport_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_leftClickedLast)
            {
                // Retrieve the coordinate of the mouse position.
                Point pt = e.GetPosition((UIElement)sender);

                // Clear the contents of the list used for hit test results.
                hitResultsList.Clear();

                // Set up a callback to receive the hit test result enumeration.
                VisualTreeHelper.HitTest(mainViewport, null,
                    new HitTestResultCallback(MyHitTestResult),
                    new PointHitTestParameters(pt));

                // Perform actions on the hit test results list. 
                if (hitResultsList.Count > 0)
                {
                    DependencyObject target = hitResultsList.FirstOrDefault();
                    if (target != null && target is BoxVisual3D)
                    {
                        // Remove box from view
                        _lastItems.Remove((ModelVisual3D)target);
                        mainViewport.Children.Remove((ModelVisual3D)target);
                        // Update view
                        mainViewport.InvalidateVisual();
                    }
                    if (target != null && _currentVisualization != null && _currentVisualization.GetContainer(target as ModelVisual3D) != null)
                    {
                        Container targetContainer = _currentVisualization.GetContainer(target as ModelVisual3D);
                        if (targetContainer != null &&
                            _solution.ContainerContent.Length > targetContainer.VolatileID &&
                            _solution.ContainerContent[targetContainer.VolatileID].Any() &&
                            _solution.ExtremePoints != null &&
                            _solution.ExtremePoints.Length > targetContainer.VolatileID &&
                            _solution.ExtremePoints[targetContainer.VolatileID] != null)
                        {
                            // Decide whether points were already drawn for this container
                            if (_pointsDrawnForContainer.ContainsKey(targetContainer) && _pointsDrawnForContainer[targetContainer].Any())
                            {
                                // Remove previously drawn points
                                foreach (var pointModel in _pointsDrawnForContainer[targetContainer])
                                {
                                    _lastItems.Remove(pointModel);
                                    mainViewport.Children.Remove(pointModel);
                                }
                                _pointsDrawnForContainer[targetContainer].Clear();
                            }
                            else
                            {
                                // Determine point diameter (use container edge thickness factor)
                                double radius =
                                    // Find minimal container length
                                    _solution.InstanceLinked.Containers.Min(c => Math.Min(c.Mesh.Length, Math.Min(c.Mesh.Width, c.Mesh.Height)))
                                    // Use edge thickness
                                    * HelixAdapter.CONTAINER_EDGE_THICKNESS_FRACTION;
                                // Add points of this container
                                _pointsDrawnForContainer[targetContainer] = HelixAdapter.Translate(
                                    points: _solution.ExtremePoints[targetContainer.VolatileID],
                                    offSetX: _currentVisualization.GetOffset(targetContainer).X,
                                    radius: radius).ToList();
                                _lastItems.AddRange(_pointsDrawnForContainer[targetContainer]);
                                foreach (var pointModel in _pointsDrawnForContainer[targetContainer])
                                    mainViewport.Children.Add(pointModel);
                            }
                            // Update view
                            mainViewport.InvalidateVisual();
                        }
                    }
                    if (target != null && target is SphereVisual3D) // TODO remove debug?
                    {
                        MessageBox.Show("VolatileID: " + target.GetName());
                    }
                }
            }
        }

        private void mainViewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _leftClickedLast = true;
            }
            else
            {
                _leftClickedLast = false;
            }
        }

        /// <summary>
        /// Used as a list for hit testing
        /// </summary>
        private List<DependencyObject> hitResultsList = new List<DependencyObject>();

        /// <summary>
        /// Returns the result of the hit test to the callback.
        /// </summary>
        /// <param name="result">A result</param>
        /// <returns></returns>
        public HitTestResultBehavior MyHitTestResult(HitTestResult result)
        {
            // Add the hit test result to the list that will be processed after the enumeration.
            hitResultsList.Add(result.VisualHit);

            // Set the behavior to return visuals at all z-order levels. 
            return HitTestResultBehavior.Continue;
        }

        /// <summary>
        /// The visualization currently being displayed.
        /// </summary>
        private HelixVisualization _currentVisualization = null;

        internal void DisplayNewSolution(COSolution solution, bool hasSolution, string exportationDir, string exportationName)
        {
            //this._instance = solution.InstanceLinked;
            Dispatcher.Invoke(() =>
            {
                // Clear old items
                foreach (var lastItem in _lastItems)
                    mainViewport.Children.Remove(lastItem);
                _lastItems.Clear();
                _pointsDrawnForContainer.Clear();
                // Update utilization indicator
                double contentVolume = (solution != null && solution.ContainerContent != null) ? solution.VolumeContained : 0;
                double containerVolume = (solution != null && solution.ContainerContent != null) ? solution.VolumeOfContainersInUse : 0;
                double utilization = (containerVolume > 0) ? contentVolume / containerVolume : 0;
                ProgressBarVolumeUtilization.Value = utilization * 100;
                ProgressBarVolumeUtilization.ToolTip = (utilization * 100).ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " %";
                // Generate visuals
                bool drawText = CheckBoxDrawText.IsChecked == true;
                if (hasSolution && solution != null)
                {
                    // Store solution
                    _solution = solution;
                    // Decide hazardous materials label per piece
                    static ImageBrush hazMatBrushProvider(VariablePiece piece) { return Constants.HAZ_MAT_BRUSHES[piece.Material.MaterialClass]; }
                    // Decide handling label per piece
                    static ImageBrush handlingBrushProvider(VariablePiece piece)
                    {
                        if (piece.ForbiddenOrientations.Any())
                        {
                            if (!piece.Stackable)
                                return Constants.HANDLING_BRUSHES[HandlingInstructions.Fragile];
                            else
                                return Constants.HANDLING_BRUSHES[HandlingInstructions.ThisSideUp];
                        }
                        else
                        {
                            if (!piece.Stackable)
                                return Constants.HANDLING_BRUSHES[HandlingInstructions.NotStackable];
                            else
                                return Constants.HANDLING_BRUSHES[HandlingInstructions.Default];
                        }
                    }
                    // Decide marking
                    BrushType markerFunction(VariablePiece piece)
                    {
                        if (CheckBoxMarkSpecials.IsChecked == true)
                        {
                            if (piece.ForbiddenOrientations.Any())
                            {
                                if (!piece.Stackable)
                                    return BrushType.CheckeredBlack;
                                else
                                    return BrushType.StripedBlack;
                            }
                            else
                            {
                                if (!piece.Stackable)
                                    return BrushType.StripedWhite;
                                else
                                    return BrushType.Plain;
                            }
                        }
                        else
                        {
                            return BrushType.Plain;
                        }
                    }

                    // Translate the solution
                    _currentVisualization = HelixAdapter.Translate(solution, ColorSelector, markerFunction, hazMatBrushProvider, handlingBrushProvider, drawText);
                    _lastItems.AddRange(_currentVisualization.AllVisuals);

                    // Add visuals
                    var _wireItems = new List<ModelVisual3D>();
                    foreach (var item in _lastItems)
                    {
                        if (!CheckBoxMarkSpecials.IsChecked == true && item is SphereVisual3D)
                            continue;

                        if (item is BoxVisual3D box && _coloringType == ColoringMode.WireFrame)
                        {
                            _wireItems.Add(HelixAdapter.GetWireframe(box));
                            mainViewport.Children.Add(_wireItems.Last());
                        }
                        else
                        {
                            mainViewport.Children.Add(item);
                        }
                    }
                    _lastItems.AddRange(_wireItems);

                    // Reposition camera
                    if (CheckBoxResetCameraAfterDrawing.IsChecked == true)
                    {
                        RepositionCamera(solution.InstanceLinked.Containers);
                    }
                    // Update view
                    mainViewport.InvalidateVisual();
                    // Export if desired
                    if (ComboBoxExportGraphics.SelectedIndex > 0)
                    {
                        // Temporarily remove offload for export, if desired
                        bool offloadRemoved = false;
                        if (CheckBoxExportOffload.IsChecked == false)
                        {
                            foreach (var offloadPiece in solution.OffloadPieces)
                                foreach (var offloadVisual in _currentVisualization[offloadPiece])
                                    mainViewport.Viewport.Children.Remove(offloadVisual);
                            offloadRemoved = true;
                        }
                        // Take screenshot
                        RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
                        renderTargetBitmap.Render(mainViewport);
                        PngBitmapEncoder pngImage = new PngBitmapEncoder();
                        pngImage.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
                        using (Stream fileStream = File.Create(System.IO.Path.Combine(exportationDir, exportationName + "_screenshot.png")))
                            pngImage.Save(fileStream);
                        // Export further formats, if desired
                        if (ComboBoxExportGraphics.SelectedIndex > 1)
                        {
                            // Export different formats
                            BitmapExporter pngExporter = new BitmapExporter() { Format = BitmapExporter.OutputFormat.Png };
                            using (FileStream fs = new FileStream(System.IO.Path.Combine(exportationDir, exportationName + ".png"), FileMode.Create))
                                pngExporter.Export(mainViewport.Viewport, fs);
                            StlExporter stlExporter = new StlExporter();
                            using (FileStream fs = new FileStream(System.IO.Path.Combine(exportationDir, exportationName + ".stl"), FileMode.Create))
                                stlExporter.Export(mainViewport.Viewport, fs);
                            ObjExporter objExporter = new ObjExporter() { MaterialsFile = exportationName + ".mat" };
                            using (FileStream fs = new FileStream(System.IO.Path.Combine(exportationDir, exportationName + ".obj"), FileMode.Create))
                                objExporter.Export(mainViewport.Viewport, fs);
                            // Cleanup
                            foreach (var file in Directory.EnumerateFiles(Environment.CurrentDirectory, "mat*.png").Concat(Directory.EnumerateFiles(exportationDir, "mat*.png")))
                                File.Delete(file);
                            foreach (var file in Directory.EnumerateFiles(Environment.CurrentDirectory, "*.mat").Concat(Directory.EnumerateFiles(exportationDir, "*.mat")))
                                File.Delete(file);
                        }
                        // Re-add offload pieces
                        if (offloadRemoved)
                            foreach (var offloadPiece in solution.OffloadPieces)
                                foreach (var offloadVisual in _currentVisualization[offloadPiece])
                                    mainViewport.Viewport.Children.Add(offloadVisual);
                    }
                }
            });

            return;
        }

        internal void DisplayNewSolution(IEnumerable<ModelVisual3D> drawableObjects)
        {
            Dispatcher.Invoke(() =>
            {
                // Clear old items
                foreach (var lastItem in _lastItems)
                {
                    mainViewport.Children.Remove(lastItem);
                }
                _lastItems.Clear();
                // Generate visuals
                _lastItems.AddRange(drawableObjects);
                // Add visuals
                foreach (var item in _lastItems)
                {
                    mainViewport.Children.Add(item);
                }
                // Update view
                mainViewport.InvalidateVisual();
            });

            return;
        }

        #region FolderEvaluation
        /// <summary>
        /// chosse folder to evaluate
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonFolderChoose(object sender, RoutedEventArgs e)
        {
            // Initializing Open Dialog
            var openDialog = new OpenFileDialog()
            {
                FileName = "AnyFile",
                Filter = string.Empty,
                CheckFileExists = false,
                CheckPathExists = false,
            };

            // Show dialog and take result into account
            bool? result = openDialog.ShowDialog();
            if (result == true)
            {
                // Get selected folder path
                EvalutationFolder.Text = System.IO.Path.GetDirectoryName(openDialog.FileName);
            }
        }

        /// <summary>
        /// cancel the evaluation process for the folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopFolderEvaluation(object sender, RoutedEventArgs e)
        {
            _cancelAction();
        }

        /// <summary>
        /// start the evaluation process of the folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExecuteFolderEvaluation(object sender, RoutedEventArgs e)
        {
            try
            {
                // Read instances from xml
                var instances = new List<Instance>();
                var names = new Dictionary<Instance, string>();
                var filenames = (from file in Directory.GetFiles(EvalutationFolder.Text)
                                 where file.ToLower().Contains("xinst") && !file.ToLower().Contains("solution")
                                 select file).ToList();

                if (filenames.Count == 0)
                    return;

                foreach (var filename in filenames)
                {
                    var instance = Instance.ReadXML(filename);
                    instances.Add(instance);
                    names.Add(instance, System.IO.Path.GetFileName(filename));
                }

                var method = GetCurrentChoosenSimpleMethods(sender, instances[0]);

                // Solve something

                // Disable buttons
                DisableButtons();
                // Clear output
                ClearOutput();

                // Execute in thread
                var timeOut = new TimeSpan(0, 0, 0, int.Parse(EvalutationFolderTimeOut.Text));
                var runner = new EvaluationFolderRunner(method, instances, names, EvalutationFolder.Text, null, EnableButtons, timeOut)
                {
                    DetailedLog = CheckBoxPreprocessingDetail.IsChecked == true
                };


                ThreadPool.QueueUserWorkItem(runner.Run);
                _cancelAction = runner.Cancel;
            }
            catch (FormatException)
            {
                MessageBox.Show("Input values not formatted correctly!");
                EnableButtons();
            }
        }

        #endregion

        #region Homogeneous piece filling

        List<Tuple<string, double, double, double>> _binFillInstanceData;
        string _binFillDir;

        private void SelectFillingData(object sender, RoutedEventArgs e)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog fileDialog = new OpenFileDialog() { Title = "Select input data CSV-file ..." };

            // Set filter options and filter index.
            fileDialog.Filter = "CSV Files|*.csv";
            fileDialog.FilterIndex = 1;
            fileDialog.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            bool? userClickedOK = fileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                // Read CSV
                _binFillInstanceData = CSVIO.ReadCSV(fileDialog.FileName, ';', OutputMessage).Skip(1)
                    .Where(s => s.Length == 4)
                    .Select(s => new Tuple<string, double, double, double>(
                        s[0],
                        double.Parse(s[1], CultureInfo.InvariantCulture),
                        double.Parse(s[2], CultureInfo.InvariantCulture),
                        double.Parse(s[3], CultureInfo.InvariantCulture))).ToList();
                _binFillDir = System.IO.Path.GetDirectoryName(fileDialog.FileName);
            }
        }

        private void ExecuteFillingEvaluation(object sender, RoutedEventArgs e)
        {
            // Quit on no data
            if (_binFillInstanceData == null || !_binFillInstanceData.Any())
                return;
            // Prepare
            double timeout = double.Parse(BoxFillContainerTimeout.Text, CultureInfo.InvariantCulture);

            // Create method (using dummy instance)
            var method = GetCurrentChoosenSimpleMethods(sender, new Instance());
            method.Config.SubmitSolution = null; // Suppress solution updates (only draw the final one)

            // Disable buttons
            DisableButtons();
            // Clear output
            ClearOutput();
            // Read data
            double containerLength = double.Parse(BoxFillContainerLength.Text, CultureInfo.InvariantCulture);
            double containerWidth = double.Parse(BoxFillContainerWidth.Text, CultureInfo.InvariantCulture);
            double containerHeight = double.Parse(BoxFillContainerHeight.Text, CultureInfo.InvariantCulture);
            double containerVolume = containerLength * containerWidth * containerHeight;

            // Define instance creation method
            List<Tuple<string, double, double, double>> binFillInstanceData = _binFillInstanceData.ToList();
            Func<Instance> instanceCreator = () =>
             {
                 // Return null on no more data
                 if (!binFillInstanceData.Any())
                     return null;
                 // Get next data element
                 Tuple<string, double, double, double> skuData = binFillInstanceData.First();
                 binFillInstanceData.RemoveAt(0);
                 // Determine impossible piece count for one bin
                 int pieceCount = (int)Math.Ceiling(containerVolume / (skuData.Item2 * skuData.Item3 * skuData.Item4));
                 // Create bin
                 Container container = new Container() { ID = 0, Mesh = new MeshCube() { Length = containerLength, Width = containerWidth, Height = containerHeight, } };
                 container.Seal();
                 // Create pieces
                 List<VariablePiece> pieces = new List<VariablePiece>(); int pieceID = 0;
                 for (int i = 0; i < pieceCount; i++)
                 {
                     VariablePiece piece = new VariablePiece() { ID = (++pieceID), };
                     // Add the parallelepiped component
                     piece.AddComponent(0, 0, 0, skuData.Item2, skuData.Item3, skuData.Item4);
                     // Assume only this side up pieces
                     piece.ForbiddenOrientations = new HashSet<int>();
                     // Seal it
                     piece.Seal();
                     pieces.Add(piece);
                 }
                 // Create instance
                 Instance instance = new Instance() { Name = skuData.Item1 };
                 // Add all items
                 instance.Containers.Add(container);
                 instance.Pieces.AddRange(pieces);
                 return instance;
             };
            // Make result dir for the given size
            string resultDir =
                containerLength.ToString("0.##", ExportationConstants.FORMATTER) + "-" +
                containerWidth.ToString("0.##", ExportationConstants.FORMATTER) + "-" +
                containerHeight.ToString("0.##", ExportationConstants.FORMATTER);
            // Execute in thread
            BatchRunner runner = new BatchRunner()
            {
                Method = method,
                InstanceCreator = instanceCreator,
                LogAction = OutputMessage,
                ResultsDir = System.IO.Path.Combine(_binFillDir, resultDir),
                StartSingleSolveAction = StartSingleSolve,
                FinishSingleSolveAction = FinishSingleSolve,
                FinishAction = EnableButtons,
                Timeout = timeout,
                StatisticsFile = "binfill.csv",
                SubmitSolutionAction = DisplayNewSolution,
            };
            ThreadPool.QueueUserWorkItem(new WaitCallback(runner.Run));
            _cancelAction = runner.Cancel;
        }

        #endregion


    }
}
