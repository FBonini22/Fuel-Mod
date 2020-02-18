using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;

namespace Fuel1._1a
{
    public class GasVehicle
    {
        //Constants
        private readonly float LITERS_PER_GALLON = 3.78541f;
        private readonly float MPH_PER_MPS = 2.23694f;
        private readonly float MILE_CONSTANT = 10f / 8f;
        private readonly float MIN_SPEED = 0.1f;
        private readonly float DEFAULT_FUEL_CAPACITY = 20f;
        private readonly float ADJUSTMENT_CONSTANT = 14.5f;     //Previous value: 14.5
        private readonly float IDLE_RPM = 0.2f;                 //Default engine idle RPM
        private readonly float IDLE_CONSUMPTION = 0.5f;         //In gallons per hour
        private readonly float CITY_SPEED_CUTOFF = 21f;
        private readonly float CRUISING_SPEED = 37f;


        //Instance Variables
        private Vehicle vInstance = null;

        private float fuel = 0f;                    //Vehicle fuel level in liters
        private float fuelCapacity = 0f;            //Vehicle fuel capacity in liters
        private float MPG = 0f;                     //Vehicle fuel consumption in miles per gallon

        private float currentMPG = 0f;
        private float averageMPG = 0f;

        private int MPGSamples = 0;

        //Vehicle Attributes to save
        private int vModelHash;
        private string vName;
        private VehicleColor vColor;
        private int vHash;
        private int vMods;
        private Vector3 lastPosition;

        /// <summary>
        /// Variable to keep track of how far the vehicle has traveled. This is used for measuring fuel consumption.
        /// </summary>
        public float distanceT = 0f;
        



        public GasVehicle(Vehicle v)
        {
            vInstance = v;
            fuelCapacity = v.FuelLevel;
            //fuelCapacity = DEFAULT_FUEL_CAPACITY;
            fuel = fuelCapacity;
            MPG = 20f;
            
            //Save vehicle attributes
            vModelHash = v.Model.Hash;
            vName = v.DisplayName;
            vHash = v.GetHashCode();
            v.IsPersistent = true;

            //vInstance.FuelLevel = 0;
            //vInstance.FuelLevel = DEFAULT_FUEL_CAPACITY;
        }

        public void RunCar()
        {
            CheckFuelLevel();
            CalculateFuelConsumption();
        }

        private void CalculateFuelConsumption()
        {
            float adjustedMPG = MPG;
            float fuelConsumed = 0;

            //Car is stationary
            if (isIdling())
            {
                //Car is idle
                fuelConsumed = IDLE_CONSUMPTION * (float)ModClass.FUEL_CHECK_INTERVAL / 3600000f;
                //Car is revving
                fuelConsumed *= (vInstance.CurrentRPM / IDLE_RPM);
            }


            //Car is moving
            else
            {
                
                //Speed factor
                adjustedMPG = speedFactor(vInstance.Speed);

                //NEW CODE HERE

                adjustedMPG /= vInstance.Acceleration;
                //NEW CODE END

                if(vInstance.Speed <= CITY_SPEED_CUTOFF)
                {
                    adjustedMPG /= 2f;
                }


                //GTA.UI.Notification.Show("MPG with speed adjust is: " + adjustedMPG.ToString());

                //RPM factor
                adjustedMPG /= ((0.75f + (float)Math.Pow(vInstance.CurrentRPM, 2d)) * 0.75f);
                //GTA.UI.Notification.Show("MPG with RPM adjust is: " + adjustedMPG.ToString());


                if (vInstance.Acceleration <= 0)
                {
                    adjustedMPG *= (vInstance.Speed >= CITY_SPEED_CUTOFF)
                                    ? (vInstance.Speed >= CRUISING_SPEED)
                                        ? 5f
                                        : 2.25f
                                    : 1f;

                }


                if (adjustedMPG < 3.5f)
                {
                    adjustedMPG = 3.5f;
                }

                float distTraveled = MILE_CONSTANT * vInstance.Speed / 1000f * ((float)ModClass.FUEL_CHECK_INTERVAL / 1000f);
                fuelConsumed = ADJUSTMENT_CONSTANT * (distTraveled / adjustedMPG);

                distanceT += distTraveled;

                //GTA.UI.Notification.Show("Distance traveled: " + distTraveled.ToString() + "\nFuel consumed: " + fuelConsumed.ToString()
                //    + "\nCurrent fuel level is: " + fuel.ToString());


                //vInstance.FuelLevel = fuel;

                //GTA.UI.Notification.Show("Fuel after consumption is: " + vInstance.FuelLevel.ToString());
                currentMPG = adjustedMPG;

                CalculateAverageFuelConsumption(currentMPG);
            }

            fuel -= liters(fuelConsumed);
            vInstance.FuelLevel -= liters(fuelConsumed);
        }

        /// <summary>
        /// Returns the instantaneous MPG of the vehicle if applicable.
        /// </summary>
        /// <returns></returns>
        public double GetCurrentFuelConsumption()
        {
            return (double)currentMPG;
        }

        private void CalculateAverageFuelConsumption(float newMPG)
        {
            float newSum = averageMPG * (float)MPGSamples + newMPG;

            MPGSamples++;

            averageMPG = newSum / (float)MPGSamples;
        }

        public float GetAverageFuelConsumption()
        {
            return averageMPG;
        }

        /// <summary>
        /// Override for default toEquals method. Uses a "points" system, where matching criteria add points
        /// to a counter. If the total points is greater than a threshold, returns true. Else false.
        /// </summary>
        /// <param name="obj">Object to be compared. Should be of GasVehicle type.</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            //"obj" is new

            Vehicle toCompare = (obj as GasVehicle).GetVehicleInstance();
            //GTA.UI.Notification.Show("Inside custom .equals method..");
            //GTA.UI.Notification.Show("new vehic: " + toCompare.GetHashCode().ToString() + "\nold Vehic: " + vInstance.GetHashCode().ToString());

            int points = 0;

            if(vHash == toCompare.GetHashCode())
            {
                //points += 2;
                //GTA.UI.Notification.Show("Hashes match");
                return true;
            }
            if(vName == toCompare.DisplayName || vModelHash == toCompare.Model.Hash)
            {
                points += 3;
            }

            //GTA.UI.Notification.Show
            //GTA.UI.Notification.Show(vInstance.DisplayName);
            //GTA.UI.Notification.Show(vInstance.FriendlyName);
            //GTA.UI.Notification.Show(vInstance.Model.Hash.ToString());

            return points >= 4;
        }

        //Conversion method
        /// <summary>
        /// Converts liters to gallons.
        /// </summary>
        /// <param name="liters"></param>
        /// <returns></returns>
        private float gallons(float liters)
        {
            return liters / LITERS_PER_GALLON;
        }
        /// <summary>
        /// Converts gallons to liters
        /// </summary>
        /// <param name="gallons"></param>
        /// <returns></returns>
        private float liters(float gallons)
        {
            return gallons * LITERS_PER_GALLON;
        }
        /// <summary>
        /// Converts meters per second to mph
        /// </summary>
        /// <param name="ms">meters per second</param>
        /// <returns></returns>
        private float mph(float ms)
        {
            return ms * MPH_PER_MPS;
        }


        /// <summary>
        /// Equation for speed factor on fuel consumption. This was determined through an Excel polynomial fit
        /// on a data set that fits the desired model.
        /// </summary>
        /// <param name="speed">The current speed at which the car is traveling in GTA speed units</param>
        /// <returns>Instantaneous fuel consumption in MPG</returns>
        private float speedFactor(float speed)
        {
            return
                (2f) * (float)Math.Pow(10d, -9d) * (float)Math.Pow(speed, 5d)
                - (2f) * (float)Math.Pow(10d, -6d) * (float)Math.Pow(speed, 4d)
                + 0.0004f * (float)Math.Pow(speed, 3d)
                - 0.0402f * (float)Math.Pow(speed, 2d)
                + 1.3928f * speed
                + 2.5329f;


            //OLD (July 2017)
            /*
            return
                (-5f) * (float)Math.Pow(10d, -8d) * (float)Math.Pow(speed, 5d)
                + (float)Math.Pow(10d, -5d) * (float)Math.Pow(speed, 4d)
                - 0.0007f * (float)Math.Pow(speed, 3d)
                - 0.0003f * (float)Math.Pow(speed, 2d)
                + 1.0263f * speed
                + 2;
            */
        }


        public float GetFuelLevel()
        {
            return fuel;
        }

        /// <summary>
        /// Returns vehicle's current fuel level as a decimal percentage of the maximum fuel level.
        /// </summary>
        /// <returns></returns>
        public float GetFuelLevelPercent()
        {
            return fuel / fuelCapacity;
        }
        /// <summary>
        /// DEBUGGING ONLY. Returns fuel level of Vehicle Type instance
        /// </summary>
        /// <returns></returns>
        public float GetActualFuelLevel()
        {
            return vInstance.FuelLevel;
        }

        /// <summary>
        /// Returns how much fuel is needed to fill up the vehicle.
        /// </summary>
        /// <returns></returns>
        public float GetFuelNeededToFillUp()
        {
            return fuelCapacity - fuel;
        }

        /// <summary>
        /// Make sure that the Vehicle type's fuel level is equal to the tracked value in this class.
        /// </summary>
        private void CheckFuelLevel()
        {
            if(vInstance.FuelLevel > fuel)
            {
                vInstance.FuelLevel = fuel;
            }
            else if(vInstance.FuelLevel < fuel)
            {
                GTA.UI.Notification.Show("ERROR. See GasVehicle.cs CheckFuelLevel method");
            }
        }


        public void Refuel(FUEL fuelAmount)
        {
            switch (fuelAmount)
            {
                case FUEL.Fuel_Pump:
                    vInstance.FuelLevel = fuelCapacity;
                    fuel = fuelCapacity;
                    break;
                case FUEL.Jerry_Can:
                    if(vInstance.FuelLevel + FuelSources.JERRY_CAN_AMOUNT > fuelCapacity)
                    {
                        vInstance.FuelLevel = fuelCapacity;
                        fuel = fuelCapacity;
                    }
                    else
                    {
                        vInstance.FuelLevel += FuelSources.JERRY_CAN_AMOUNT;
                        fuel += FuelSources.JERRY_CAN_AMOUNT;
                    }
                    break;
            }
        }

        /// <summary>
        /// Get the instance of the associated Vehicle type
        /// </summary>
        /// <returns></returns>
        public Vehicle GetVehicleInstance()
        {
            return vInstance;
        }

        private void StoreVehicleInformation()
        {
            lastPosition = vInstance.Position;
        }


        public bool isIdling()
        {
            return vInstance.Speed < 0.1 && vInstance.IsEngineRunning;
        }

        public void dumpObject()
        {
            if(vInstance != null)
            {
                vInstance.IsPersistent = false;
                vInstance.MarkAsNoLongerNeeded();
            }
        }

    }
}
