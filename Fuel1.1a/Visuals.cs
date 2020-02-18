using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using GTA;

namespace Fuel1._1a
{
    public class Visuals
    {
        public static readonly Point     FUEL_BAR_COORDINATES    = new Point((int)((float)GTA.UI.Screen.Width* 0.0125f), (int)((float)GTA.UI.Screen.Height * 0.75f));
        private static readonly int FUEL_BAR_WIDTH = (int)((float)GTA.UI.Screen.Width * 0.125f);
        public static Size      FUEL_BAR_SIZE           = new Size(FUEL_BAR_WIDTH,15);
        public static Color     FUEL_BAR_COLOR          = Color.Gold;
        public static Color     FUEL_BAR_BACKGROUND_COLOR= Color.Peru;
        public static Color     FUEL_BAR_WARNING_COLOR  = Color.Crimson;

    }
}
