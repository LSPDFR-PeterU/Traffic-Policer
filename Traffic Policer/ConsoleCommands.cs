using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage.Attributes;
using System.Reflection;
using Rage;
using Traffic_Policer.Ambientevents;

namespace Traffic_Policer
{
    /// <summary>
    /// Console commands that Traffic Policer exposes to the Rage Plugin Hook console.
    /// </summary>
    public static class ConsoleCommands
    {
#if DEBUG

        /// <summary>
        /// Force the Traffic Policer ambient event with the specified <paramref name="className"/>
        /// to immediately start.
        /// </summary>
        /// <param name="className"></param>
        [Rage.Attributes.ConsoleCommand(Description = "Force ambient event with name", Name = "ForceTrafficPolicerAmbientEvent")]
        public static void ForceTrafficPolicerAmbientEvent(string className)
        {
            try
            {
                Vehicle[] vehs = Game.LocalPlayer.Character.GetNearbyVehicles(15);
                Ped driver = null;
                foreach(Vehicle veh in vehs)
                {
                    if (veh && veh.HasDriver && veh.Driver)
                    {
                        driver = veh.Driver;
                        break;
                    }
                }

                // find the target class name
                // trying to be clever, but constructors are different
                //Type targetEventType = Assembly.GetExecutingAssembly().GetType($"Traffic_Policer.Ambientevents.{className}");

                //Activator.CreateInstance(targetEventType, driver, true, true, "Forcing Ambient Event");

                switch (className)
                {
                    case "BrokenDownVehicle":
                        BrokenDownVehicle brokenDownVehicle = new BrokenDownVehicle(true, true);                        
                        break;
                }

            }
            catch (Exception e)
            {
                Game.LogTrivial($"Failed to force start an ambient event: {e}");
            }
        }
#endif
    }
}
