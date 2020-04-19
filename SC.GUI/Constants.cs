using SC.ObjectModel.Additionals;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SC.GUI
{
    /// <summary>
    /// Constants used for the UI
    /// </summary>
    internal class Constants
    {
        /// <summary>
        /// A beamer friendly color-set
        /// </summary>
        public static Color[] COLORS_BEAMER = { Colors.LightPink, Colors.LightSeaGreen, Colors.LightCoral, Colors.LightGreen, Colors.LightSalmon, Colors.LightSkyBlue, Colors.LightSlateGray, Colors.LightBlue, Colors.LightCyan, Colors.LightGray, Colors.LightGoldenrodYellow, Colors.LightSteelBlue, Colors.LightYellow };

        /// <summary>
        /// A big set of colors
        /// </summary>
        public static Color[] COLORS_FULL = { Colors.CadetBlue, Colors.Chartreuse, Colors.Sienna, Colors.CornflowerBlue, Colors.Crimson, Colors.DarkBlue, Colors.DarkGreen, Colors.DarkMagenta, Colors.DarkOrange, Colors.DarkRed, Colors.DarkSeaGreen, Colors.DarkSlateBlue, Colors.DarkSlateGray, Colors.DeepPink, Colors.Firebrick, Colors.ForestGreen, Colors.Gold, Colors.Gray, Colors.Green, Colors.GreenYellow, Colors.IndianRed, Colors.Indigo, Colors.Khaki, Colors.LawnGreen, Colors.LightGreen, Colors.LightSeaGreen, Colors.LightSkyBlue, Colors.Lime, Colors.Maroon, Colors.MediumBlue, Colors.MediumSlateBlue, Colors.MidnightBlue, Colors.Navy, Colors.Olive, Colors.OliveDrab, Colors.Orange, Colors.OrangeRed, Colors.PaleGreen, Colors.Peru, Colors.Purple, Colors.Red, Colors.RoyalBlue, Colors.SeaGreen, Colors.Chocolate, Colors.SkyBlue, Colors.SlateBlue, Colors.SpringGreen, Colors.SteelBlue, Colors.Teal, Colors.Tomato, Colors.Violet, Colors.Yellow, Colors.YellowGreen, Colors.White };

        /// <summary>
        /// A mapping for expressive colors for each material classification
        /// </summary>
        public static Dictionary<MaterialClassification, Color> COLORS_PER_MATERIAL = new Dictionary<MaterialClassification, Color>()
        {
            { MaterialClassification.Default, Colors.Gray },
            { MaterialClassification.Explosive, Colors.Yellow },
            { MaterialClassification.FlammableGas, Colors.OrangeRed },
            { MaterialClassification.FlammableLiquid, Colors.Red },
            { MaterialClassification.Toxic, Colors.LimeGreen },
            { MaterialClassification.FreshFood, Colors.LawnGreen },
            { MaterialClassification.LiveAnimals, Colors.Brown },
        };

        /// <summary>
        /// A mapping for brushes to use when drawing hazardous material labels on objects
        /// </summary>
        public static Dictionary<MaterialClassification, ImageBrush> HAZ_MAT_BRUSHES = new Dictionary<MaterialClassification, ImageBrush>() 
        {
            { MaterialClassification.Default, null },
            { MaterialClassification.Explosive, null /* CreateBrushFromBitmap(Properties.Resources.HazMat_1_1_Explosives) */ },
            { MaterialClassification.FlammableGas, null /* CreateBrushFromBitmap(Properties.Resources.HazMat_2_1_FlammableGas) */ },
            { MaterialClassification.FlammableLiquid, null /* CreateBrushFromBitmap(Properties.Resources.HazMat_3_FlammableLiquid) */ },
            { MaterialClassification.Toxic, null /* CreateBrushFromBitmap(Properties.Resources.HazMat_6_1_Toxic) */ },
            { MaterialClassification.FreshFood, null /* CreateBrushFromBitmap(Properties.Resources.HazMat_1_1_Explosives) */ },
            { MaterialClassification.LiveAnimals, null /* CreateBrushFromBitmap(Properties.Resources.HazMat_1_1_Explosives) */ }
        };

        /// <summary>
        /// A mapping for brushes to use when drawing hazardous material labels on objects
        /// </summary>
        public static Dictionary<HandlingInstructions, ImageBrush> HANDLING_BRUSHES = new Dictionary<HandlingInstructions, ImageBrush>() 
        {
            { HandlingInstructions.Default, null },
            { HandlingInstructions.ThisSideUp, null /* CreateBrushFromBitmap(Properties.Resources.Handling_This_side_up) */ },
            { HandlingInstructions.NotStackable, null /* CreateBrushFromBitmap(Properties.Resources.Handling_Handle_with_care) */ },
            { HandlingInstructions.Fragile, null /* CreateBrushFromBitmap(Properties.Resources.Handling_Fragile) */ }
        };

        ///// <summary>
        ///// Creates a brush from a bitmap source
        ///// </summary>
        ///// <param name="bmp">The bitmap source</param>
        ///// <returns>The brush containing the bitmap source</returns>
        //public static ImageBrush CreateBrushFromBitmap(System.Drawing.Bitmap bmp)
        //{
        //    return new ImageBrush(System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions()));
        //}
    }
}
