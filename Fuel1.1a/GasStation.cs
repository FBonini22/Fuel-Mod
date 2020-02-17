using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;

namespace Fuel1._1a
{
    internal class GasStation : Location
    {
        //Constants
        private readonly float NEAR_DISTANCE = 200f;             //Distance to be considered "near" a gas station
        private readonly double DEFAULT_PRICE_PER_GALLON = 5.00d;

        //Instance Variables
        private double pricePerGallon;

        private Vector3 Location { get; set; }

        /// <summary>
        /// Default constructor for the GasStation class. Takes two inputs.
        /// </summary>
        /// <param name="c1">The first corner coordinates of the refuel area</param>
        /// <param name="c2">The second corner coordinates of the refuel area</param>
        public GasStation(Vector2 c1, Vector2 c2) : base(c1, c2)
        {
            pricePerGallon = DEFAULT_PRICE_PER_GALLON;
            InitializeOnMap();
        }
        
        /// <summary>
        /// Main constructor for the GasStation class. Takes three inputs
        /// </summary>
        /// <param name="c1">The first corner coordinates of the refuel area</param>
        /// <param name="c2">The second corner coordinates of the refuel area</param>
        /// <param name="price">Price for one gallon of fuel</param>
        public GasStation(Vector2 c1, Vector2 c2, double price) : base(c1, c2)
        {
            pricePerGallon = price;
            InitializeOnMap();
        }

        /// <summary>
        /// Internal function to make this gas station visible on the world map.
        /// </summary>
        private void InitializeOnMap()
        {
            float Z = World.GetGroundHeight(this.center);

            Location = new Vector3(center.X, center.Y, Z);

            var gSMarker = World.CreateBlip(Location);
            gSMarker.Position = Location;
            gSMarker.Sprite = BlipSprite.JerryCan;
            gSMarker.IsShortRange = true;
            gSMarker.Name = "Gas Station";
            gSMarker.Color = BlipColor.Green;
            gSMarker.Alpha = 255;

        }


        /// <summary>
        /// Returns whether the player is near a gas station
        /// </summary>
        /// <param name="pLocation">The location of the player</param>
        /// <returns></returns>
        public bool PlayerIsNear(Vector3 pLocation)
        {
            return vicinity(new Vector2(pLocation.X, pLocation.Y)) <= NEAR_DISTANCE;
        }

        /// <summary>
        /// Returns whether the player is at a gas station
        /// </summary>
        /// <param name="pLocation">The location of the player</param>
        /// <returns></returns>
        public bool PlayerIsAt(Vector3 pLocation)
        {
            return isInside(new Vector2(pLocation.X, pLocation.Y));
        }

        public double GetPrice()
        {
            return pricePerGallon;
        }

        public double GetPriceForRefuel(float gallons)
        {
            return pricePerGallon * gallons;
        }

    }
}
