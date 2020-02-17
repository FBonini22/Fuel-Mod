using GTA;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using System.Media;
using GTA.Native;

namespace Fuel1._1a
{
    public class ModClass : Script
    {
        //Controls
        private static readonly Keys STATION_REFUEL_CONTROL = Keys.Space;
        private static readonly Keys CAN_REFUEL_CONTROL = Keys.Y;

        //Constants
        public static readonly int          FUEL_CHECK_INTERVAL             = 500;
        public static readonly int          FUEL_REFILL_COOLDOWN            = 5000;
        public static readonly int          REFRESH_INTERVAL                = 25;
        public static readonly int          MAX_SAVED_VEHICLES              = 10;
        private static readonly float       FUEL_LEVEL_WARNING_THRESHOLD    = 0.16f;
        private static readonly float       MIN_REFUEL_DISTANCE             = 10f;

        private static List<GasStation> STATIONS = new List<GasStation>()
        {
            new GasStation(new Vector2(-2563.68f,2323f), new Vector2(-2545.8f,2345.98f)),
            new GasStation(new Vector2(-2108.87f,-330.9f), new Vector2(-2085f,-307.37f)),
            new GasStation(new Vector2(169f,6594.28f), new Vector2(191f,6610f)),
            new GasStation(new Vector2(1696.4f,6410f), new Vector2(1706f,6422f)),
            new GasStation(new Vector2(609f,260f), new Vector2(631f,278.5f), 3.13d),
            new GasStation(new Vector2(174f,-1573f), new Vector2(176.6f,-1549.4f), 3.88d),
            new GasStation(new Vector2(-735f,-940.8f), new Vector2(-711f,-929.6f), 3.59d),
            new GasStation(new Vector2(-80f,-1770f), new Vector2(-61.5f,-1751f), 3.69d),
            new GasStation(new Vector2(-533f,-1220f), new Vector2(-520f,-1200f), 3.69d),
            new GasStation(new Vector2(1201f,-1403f), new Vector2(1217f,-1400f), 3.69d),
            new GasStation(new Vector2(254f,-1270f), new Vector2(276f, -1251f), 5.69d),
            new GasStation(new Vector2(2570f,354f), new Vector2(2591f,370f), 3.88d),
            new GasStation(new Vector2(1781f,3330f), new Vector2(1788f,3332f), 3.13d),
            new GasStation(new Vector2(1998f,3773f), new Vector2(2011f,3774f), 2.90d),
            new GasStation(new Vector2(261f,2604f), new Vector2(267.2f, 2609.9f), 2.90d),
            new GasStation(new Vector2(1207f,2656f), new Vector2(1208f,2665f), 2.90)
        };


        //Instance Variables
        private List<GasVehicle> savedVehicles = new List<GasVehicle>();
        private GasVehicle currentCar;
        private GasVehicle vehicleForRefuel = null;
        private Ped p = Game.Player.Character;

        private GasStation currentStation = null;
        /*
         * These types are deprecated. Use the new equivalent from ScriptHookVDotNet3
        GTA.UIRectangle gaugeBackground;
        GTA.UIRectangle gauge;
        */
        GTA.UI.TextElement gaugeText;

        private int elapsedIntervalTime = 0;
        private int elapsedTimeSinceLastRefuel = 0;

        private bool refuelMenuEnabled = false;
        private bool justEnteredCar = false;
        private bool justRefueled = false;
        private bool inRangeForRefuel = false;
        private bool readyForCanRefuel = false;
        private bool warningIsPlaying = false;
        private bool lowFuelLevel = false;

        private int refillPrice;


        public ModClass()
        {
            Tick += onTick;
            KeyUp += onKeyUp;
            KeyDown += onKeyDown;

            Interval = REFRESH_INTERVAL;

            //InitializeFuelGauge();
        }

        private void onKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            
        }

        private void onKeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == CAN_REFUEL_CONTROL)
            {
                if (p.IsInVehicle() && currentCar != null)
                {
                    //currentCar = new GasVehicle(p.CurrentVehicle);
                    GTA.UI.Notification.Show(currentCar.GetFuelLevel().ToString() + "\n" +
                        currentCar.GetActualFuelLevel().ToString() + "\n" +
                        currentCar.GetCurrentFuelConsumption().ToString() + " MPG\n" +
                        currentCar.distanceT.ToString());
                }


                else if (readyForCanRefuel)
                {
                    vehicleForRefuel.Refuel(FUEL.Jerry_Can);

                    GTA.UI.Notification.Show("Vehicle refueled!");

                    vehicleForRefuel = null;
                    p.Weapons.Remove(GTA.WeaponHash.PetrolCan);

                    readyForCanRefuel = false;
                }

            }

            //Refuel vehicle
            else if(e.KeyCode == STATION_REFUEL_CONTROL)
            {
                if (refuelMenuEnabled)
                {
                    if (IsAtGasStation())
                    {
                        //Refill the vehicle and charge the player
                        if((int)refillPrice <= Game.Player.Money)
                        {
                            ChargePlayer(refillPrice);

                            currentCar.Refuel(FUEL.Fuel_Pump);
                            GTA.UI.Notification.Show("Vehicle refueled!");

                            justRefueled = true;
                            elapsedTimeSinceLastRefuel = 0;
                            refillPrice = 0;
                        }
                        else
                        {
                            GTA.UI.Notification.Show("You don\'t have enough cash to refuel your vehicle! ");
                            //Add a random pity message
                        }
                    }
                }
            }
        }

        private void onTick(object sender, EventArgs e)
        {
            elapsedIntervalTime += REFRESH_INTERVAL;

            //If the player is in the vehicle
            if (p.IsInVehicle() && VehicleIsCar(p.CurrentVehicle))
            {
                //If the player has just entered this vehicle (Keep the "!")
                if (!justEnteredCar)
                {
                    justEnteredCar = true;

                    //Initialize script and objects
                    InitializeVehicle();

                }

                //Run functions
                CheckFuelLevel();
                ConsumeFuel();
                DrawFuelGauge();
                GasStationMenu();
                FuelWarning();
                //DrawDebugInfo();



            }
            else
            {
                //Save the vehicle
                if (justEnteredCar)
                {
                    SaveVehicle();
                    justEnteredCar = false;
                    currentCar = null;
                }

                //RefuelMenu();
                //GTA.Entity g;
                //GTA.Native.WeaponHash.PetrolCan;
                //p.Weapons.Current.Hash
            }

            RefuelMenu();

            //Reset elapsedInterval Times
            ResetIntervals();

        }

        /// <summary>
        /// Initializes the vehicle and prepares related objects.
        /// </summary>
        private void InitializeVehicle()
        {
            currentCar = new GasVehicle(p.CurrentVehicle);
            AddVehicle(currentCar);

            //DEBUGInitializeVehicle();

        }
        /// <summary>
        /// FOR DEBUGGING ONLY
        /// </summary>
        private void DEBUGInitializeVehicle()
        {
            currentCar = new GasVehicle(p.CurrentVehicle);
            AddVehicle(currentCar);

            //Check list for vehicle
            string vehicles = "";
            foreach (var v in savedVehicles)
            {
                vehicles += (v.GetVehicleInstance().ToString() + "\n");
            }
            GTA.UI.Notification.Show(vehicles);

            string vehicleHashes = "";
            foreach (var v in savedVehicles)
            {
                vehicleHashes += (v.GetVehicleInstance().GetHashCode().ToString() + "\n");
            }
            GTA.UI.Notification.Show(vehicleHashes);
        }


        //VEHICLE LIST METHODS
        /// <summary>
        /// Adds a vehicle to the savedVehicles List. Checks if vehicle is already in list.
        /// </summary>
        /// <param name="gV"></param>
        private void AddVehicle(GasVehicle gV)
        {
            //Previously was "!SavedVehicles.Contains(gV)"
            if (!savedVehicles.Contains(gV))
            {
                GTA.UI.Notification.Show("Adding gas vehicle to list...");
                if(savedVehicles.Count >= MAX_SAVED_VEHICLES)
                {
                    removeVehicle(0);
                }
                savedVehicles.Add(gV);
            }
            else
            {
                GTA.UI.Notification.Show("List of vehicles already contains this vehicle.");

                currentCar = savedVehicles.Find((GasVehicle gGV)=> { return gGV.Equals(gV); });
                GTA.UI.Notification.Show("Loaded vehicle from list. Current fuel level is: " + currentCar.GetFuelLevel());
            }
        }
        /// <summary>
        /// Vehicles must have been added before this method is called. This is only for modifying the list
        /// items post-entry;
        /// </summary>
        private void SaveVehicle()
        {
            int vehicleIndex = savedVehicles.IndexOf(currentCar);
            savedVehicles[vehicleIndex] = currentCar;
        }
        /// <summary>
        /// Removes vehicle from the savedVehicles list and calls proper object disposal methods
        /// </summary>
        /// <param name="index">Index of item to be removed</param>
        private void removeVehicle(int index)
        {
            savedVehicles[index].dumpObject();
            savedVehicles.RemoveAt(index);
        }


        //GUI METHODS
        /// <summary>
        /// Method to draw the fuel gauge every refresh
        /// </summary>
        private void DrawFuelGauge()
        {
            gaugeBackground = new UIRectangle(
                Visuals.FUEL_BAR_COORDINATES,
                Visuals.FUEL_BAR_SIZE,
                (currentCar.GetFuelLevelPercent() > 0.15f)
                    ? Visuals.FUEL_BAR_BACKGROUND_COLOR
                    : Visuals.FUEL_BAR_WARNING_COLOR);

            int gaugeWidth = (int)((float)Visuals.FUEL_BAR_SIZE.Width * currentCar.GetFuelLevelPercent());
            Size gaugeSize = new Size(gaugeWidth, Visuals.FUEL_BAR_SIZE.Height);

            gauge = new UIRectangle(
                Visuals.FUEL_BAR_COORDINATES, gaugeSize, Visuals.FUEL_BAR_COLOR);

            gaugeText = new GTA.UI.TextElement((currentCar.GetFuelLevelPercent() * 100).ToString("F0") +
                "%\n Current MPG: " + currentCar.GetCurrentFuelConsumption().ToString("F2") + 
                "\n Average MPG: " + currentCar.GetAverageFuelConsumption().ToString("F2") + 
                "\n Acceleration: " + currentCar.GetVehicleInstance().Acceleration.ToString("F6"),
                new Point(Visuals.FUEL_BAR_COORDINATES.X, Visuals.FUEL_BAR_COORDINATES.Y - 120),
                                    0.5f, (lowFuelLevel) ? Visuals.FUEL_BAR_WARNING_COLOR : Color.White);




            gaugeBackground.Draw();
            gauge.Draw();
            gaugeText.Draw();
        }
        /// <summary>
        /// Method for playing audible warning for low fuel level
        /// </summary>
        private void FuelWarning()
        {
            if (lowFuelLevel && !warningIsPlaying)
            {
                SoundPlayer lowFuelAudio = new SoundPlayer(Properties.Resources.FuelWarning);
                lowFuelAudio.Play();
                warningIsPlaying = true;
            }
        }

        /// <summary>
        /// Method to check the fuel level
        /// </summary>
        private void CheckFuelLevel()
        {
            lowFuelLevel = (currentCar.GetFuelLevelPercent() <= FUEL_LEVEL_WARNING_THRESHOLD);
        }
        private void DrawDebugInfo()
        {
            GTA.UIText spMPG = new UIText("Speed: " + currentCar.GetVehicleInstance().Speed.ToString() +
                                            "\nMPG: " + currentCar.GetCurrentFuelConsumption() +
                                            "\nFuel Level: " + currentCar.GetFuelLevel() + 
                                            "\nAverage MPG: " + currentCar.GetAverageFuelConsumption(), new Point(500, 500), 1f);

            spMPG.Draw();
        }





        //LOGIC METHODS
        /// <summary>
        /// Method to call for fuel consumption
        /// </summary>
        private void ConsumeFuel()
        {
            if(elapsedIntervalTime >= FUEL_CHECK_INTERVAL)
            {
                currentCar.RunCar();
            }
        }
        /// <summary>
        /// Resets intervals if their elapsed time has reached the specified
        /// limits declared as constants.
        /// </summary>
        private void ResetIntervals()
        {
            if (elapsedIntervalTime >= FUEL_CHECK_INTERVAL)
            {
                elapsedIntervalTime = 0;
            }
        }

        //MENU METHODS
        /// <summary>
        /// DO NOT USE CURRENTLY. TO DO: FIX
        /// Script stops running after this method has been called multiple times.
        /// </summary>
        private void RefuelMenu()
        {
            if(elapsedIntervalTime >= FUEL_CHECK_INTERVAL)
            {
                //GTA.UI.Notification.Show("Fuel check interval...");
                if (!p.IsInVehicle())
                {
                    //GTA.UI.Notification.Show("Checking for player vicinity to vehicle");

                    //List<Vehicle> nearbyVehicles = new List<Vehicle>(World.GetNearbyVehicles(p,MIN_REFUEL_DISTANCE));

                    //vehicleForRefuel = savedVehicles.Find((GasVehicle gV) =>
                    //{ return p.Position.DistanceTo(gV.GetVehicleInstance().Position) <= MIN_REFUEL_DISTANCE; });

//                    GTA.UI.Notification.Show(vehicleForRefuel.GetFuelLevel().ToString());

                    if(vehicleForRefuel == null)
                    {
                        foreach (var gV in savedVehicles)
                        {
                            if (p.Position.DistanceTo(gV.GetVehicleInstance().Position) <= MIN_REFUEL_DISTANCE){
                                vehicleForRefuel = gV;
                                break;
                            }
                        }
                    }



                    if (vehicleForRefuel != null && !inRangeForRefuel)
                    {
                        //GTA.UI.Notification.Show("In range for refuel!");
                        inRangeForRefuel = true;
                    }

                    if (inRangeForRefuel)
                    {
                        WeaponHash currentWeapon = p.Weapons.Current.Hash;
                        //GTA.UI.Notification.Show("checking current wep");
                        //If the user is holding a jerry can
                        if (currentWeapon == GTA.Native.WeaponHash.PetrolCan)
                        {
                            readyForCanRefuel = true;
                            GTA.UI.Notification.Show("Press Y to use the jerry can to refuel your vehicle.");
                        }
                        else
                        {
                            readyForCanRefuel = false;
                        }
                    }
                }
                else
                {
                    inRangeForRefuel = false;
                    readyForCanRefuel = false;
                    if(vehicleForRefuel != null)
                    {
                        vehicleForRefuel = null;
                    }
                }
            }
        }

        /// <summary>
        /// Method for instantiating and managing the gas station menu
        /// </summary>
        private void GasStationMenu()
        {

            if (justRefueled)
            {
                elapsedTimeSinceLastRefuel += REFRESH_INTERVAL;
                if(elapsedTimeSinceLastRefuel >= FUEL_REFILL_COOLDOWN)
                {
                    justRefueled = false;
                }
            }

            //Change to GAS_STATION_CHECK_INTERVAL
            if (elapsedIntervalTime >= FUEL_CHECK_INTERVAL)
            {
                //Check that car is stationary
                if (currentCar.isIdling() && !refuelMenuEnabled)
                {
                    if (IsAtGasStation())
                    {
                        refillPrice =
                            (int)(currentStation.GetPriceForRefuel(currentCar.GetFuelNeededToFillUp()));

                        //Only enable the refuel dialog if the refill price is noticable
                        refuelMenuEnabled = (refillPrice > 1);
                    }
                }
                else if (refuelMenuEnabled && !justRefueled)
                {
                    GTA.UI.Notification.Show("Press SpaceBar to refuel your vehicle. It will cost $" + refillPrice.ToString());
                }

                if (!IsAtGasStation())
                {
                    refuelMenuEnabled = false;
                    justRefueled = false;
                }
            }
        }

        /// <summary>
        /// Charges the in-game character money. Only use this when character spends money on fuel
        /// </summary>
        /// <param name="amount">Amount to charge player</param>
        protected void ChargePlayer(int amt)
        {
            Game.Player.Money -= amt;
        }


        //BOOLEAN METHODS
        private bool IsAtGasStation()
        {
            //foreach(var gS in STATIONS)
            //{
            //    if (gS.playerIsAt(p.Position))
            //    {
            //        return true;
            //    }
            //}

            currentStation = STATIONS.Find((GasStation s) => { return s.PlayerIsAt(p.Position); });
            return currentStation != null;
        }
        
        /// <summary>
        /// DO NOT USE
        /// </summary>
        /// <param name="gV"></param>
        /// <returns></returns>
        private bool ContainsVehicle(GasVehicle gV)
        {
            foreach (var v in savedVehicles)
            {
                if (gV.GetVehicleInstance() == v.GetVehicleInstance())
                {
                    return true;
                }
            }
            return false;
        }
       
        /// <summary>
        /// DO NOT USE
        /// </summary>
        /// <returns></returns>
        private bool ReadyToConsumeFuel()
        {
            return false;
        }

        /// <summary>
        /// Method to check whether the passed vehicle is a car.
        /// </summary>
        /// <param name="currentVehicle">The vehicle to check</param>
        /// <returns></returns>
        private bool VehicleIsCar(Vehicle currentVehicle)
        {
            //Current vehicle types that are not supported
            return (
                currentVehicle.ClassType != VehicleClass.Boats &&
                currentVehicle.ClassType != VehicleClass.Helicopters &&
                currentVehicle.ClassType != VehicleClass.Planes &&
                currentVehicle.ClassType != VehicleClass.Trains
                );
        }
    }
}
