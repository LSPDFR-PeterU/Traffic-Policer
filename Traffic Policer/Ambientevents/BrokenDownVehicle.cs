using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;
using Traffic_Policer.Extensions;
using Albo1125.Common.CommonLibrary;

namespace Traffic_Policer.Ambientevents
{
    internal class BrokenDownVehicle : AmbientEvent
    {
        private TupleList<Vector3, float> ValidTrafficStopSpawnPointsWithHeadings = new TupleList<Vector3, float>();
        private Tuple<Vector3, float> ChosenSpawnData;
        private Vector3 SpawnPoint;
        private float SpawnHeading;



        private static readonly string[] getOfficerAttentionDialogueLines =
        {
            "~b~Officer! Officer! ~s~Over here! My vehicle has broken down!",
            "~b~Officer! ~s~Please can you help me? I have broken down!",
            "~b~Officer! ~s~I'm stuck here on the road with a broken down vehicle!"
        };

        private static readonly string[] askOfficerToFixDialogueLines =
        {
           "~b~Officer~s~, I cannot start my vehicle any more. Please help me fix it.",
           "This damn piece of junk has broken down again. Is there anything you can do?",
           "This unreliable piece of crap is breaking down all the time. Can you help?",
           "Are you able to get my vehicle to start up again?",
           "Can you help me get the engine started again?"
        };

        private static readonly string[] officerFixedVehicleDialogueLines =
        {
            "~b~You: ~s~You should be able to drive your vehicle again!",
            "~b~You: ~s~Looks like that worked to cool things down.",
            "~b~You: ~s~Fixed for now! I always knew I was a greasemonkey at heart.",
            "~b~You: ~s~I have had some success, it seems.",
            "~b~You: ~s~Well that's a little better..."
        };

        private static readonly string[] officerRecommendVisitGarageDialogueLines =
        {
            "~b~You: ~s~Just make sure to visit a garage as soon as possible!",
            "~b~You: ~s~It will overheat again, so drive slowly to a repair shop.",
            "~b~You: ~s~This will only fix it for a short time. Go straight to your mechanic.",
            "~b~You: ~s~You will need to drive immediately to a mechanic shop."
        };

        private static readonly string[] thanksOfficerDialogueLines =
        {
            "~b~Thank you, officer! Take care!",
            "Wow, thanks!",
            "Cheers mate, as they say!",
            "That's amazing. Thank you so much!",
            "Thank you so much. At least I'm not stranded here now."
        };

        private static readonly string[] vehicleCannotBeRepairedDialogueLines =
        {
            "~b~You: ~s~Your vehicle seems to be completely dead.",
            "~b~You: ~s~I don't think there's anything I can do here.",
            "~b~You: ~s~I'm sorry, but there is nothing I can do with this.",
            "~b~You: ~s~I'm sorry, but this vehicle is not safe to drive.",
            "~b~You: ~s~I have no idea what I can do here."
        };

        private static readonly string[] officerWillCallTowTruckDialogueLines =
        {
            "~b~You: ~s~I'm calling a tow truck to take it away.",
            "~b~You: ~s~I will have to call to get the vehicle towed.",
            "~b~You: ~s~I will need to ask you to get this towed to a repair shop.",
            "~b~You: ~s~This vehicle will need to be towed to a garage.",
            "~b~You: ~s~The only thing to do is have it towed.",
        };

        private static readonly string[] pedAcceptsVehicleBeingTowedDialogueLines =
        {
            "~b~Thank you, officer! I guess I'll have to walk home now.",
            "Thanks anyway. I will try and get home from here.",
            "Thank you for your help. Do you know where the nearest bus stop is?",
            "Thanks. You don't have the number of a local taxi firm by any chance?",
            "Thank you anyway. I don't know how I'm going to make my onward journey."
        };
        
        public static bool BrokenDownVehicleRunning = false;
        private string[] vehiclesToSelectFrom = new string[] {"DUKES", "BALLER", "BALLER2", "BISON", "BISON2", "BJXL", "CAVALCADE", "CHEETAH", "COGCABRIO", "ASEA", "ADDER", "FELON", "FELON2", "ZENTORNO",
        "WARRENER", "RAPIDGT", "INTRUDER", "FELTZER2", "FQ2", "RANCHERXL", "REBEL", "SCHWARZER", "COQUETTE", "CARBONIZZARE", "EMPEROR", "SULTAN", "EXEMPLAR", "MASSACRO",
        "DOMINATOR", "ASTEROPE", "PRAIRIE", "NINEF", "WASHINGTON", "CHINO", "CASCO", "INFERNUS", "ZTYPE", "DILETTANTE", "VIRGO", "F620", "PRIMO", "SULTAN", "EXEMPLAR", "F620", "FELON2", "FELON", "SENTINEL",
            "WINDSOR", "DOMINATOR", "DUKES", "GAUNTLET", "VIRGO", "ADDER", "BUFFALO", "ZENTORNO", "MASSACRO" };

        public BrokenDownVehicle(bool createBlip, bool displayMessage)
        {
            foreach (Tuple<Vector3, float> tuple in CommonVariables.TrafficStopSpawnPointsWithHeadings)
            {
                if ((Vector3.Distance(tuple.Item1, Game.LocalPlayer.Character.Position) < 300f) && (Vector3.Distance(tuple.Item1, Game.LocalPlayer.Character.Position) > 140f))
                {
                    if (Rage.Native.NativeFunction.Natives.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS<float>(tuple.Item1.X, tuple.Item1.Y, tuple.Item1.Z, Game.LocalPlayer.Character.Position.X, Game.LocalPlayer.Character.Position.Y, Game.LocalPlayer.Character.Position.Z) < 500f)
                    {
                        Game.LogTrivialDebug($"Add {tuple.Item1} {tuple.Item2}");
                        ValidTrafficStopSpawnPointsWithHeadings.Add(tuple);
                    }
                }
            }
            if (ValidTrafficStopSpawnPointsWithHeadings.Count > 0)
            {
                ChosenSpawnData = ValidTrafficStopSpawnPointsWithHeadings[TrafficPolicerHandler.rnd.Next(ValidTrafficStopSpawnPointsWithHeadings.Count)];
                SpawnPoint = ChosenSpawnData.Item1;
                SpawnHeading = ChosenSpawnData.Item2;
                car = new Vehicle(vehiclesToSelectFrom[TrafficPolicerHandler.rnd.Next(vehiclesToSelectFrom.Length)], SpawnPoint, SpawnHeading);
                car.RandomiseLicencePlate();
                car.MakePersistent();
                driver = car.CreateRandomDriver();
                driver.MakeMissionPed();
                car.EngineHealth = 0;
                car.IsDriveable = false;
                car.Doors[4].Open(true);
                car.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                
                if (displayMessage) { Game.DisplayNotification("Creating Broken Down Vehicle Event."); }
                if (createBlip)
                {
                    driverBlip = driver.AttachBlip();
                    driverBlip.Color = System.Drawing.Color.Beige;
                    driverBlip.Scale = 0.7f;
                }
                MainLogic();
            }
            else
            {
#if DEBUG
                Game.LogTrivial("No currently valid traffic stop points to create a broken down vehicle.");
#endif
            }
        }

        protected override void MainLogic()
        {
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {
                try
                {
                    BrokenDownVehicleRunning = true;
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        car.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) < 65f)
                        {
                            break;
                        }
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 350f)
                        {
                            End();
                        }
                    }
                    if (eventRunning)
                    {
                        Game.DisplaySubtitle(getOfficerAttentionDialogueLines[MathHelper.GetRandomInteger(0,getOfficerAttentionDialogueLines.Length-1)], 6000);
                        if (driverBlip.Exists()) { driverBlip.Delete(); }
                        driverBlip = driver.AttachBlip();
                        driverBlip.Flash(400, 4000);
                        car.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) < 6f)
                        {
                            driver.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion(5000);
                            car.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                            break;
                        }
                        else if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 150f)
                        {
                            End();
                        }
                    }
                    if (eventRunning)
                    {
                        if (driverBlip.Exists()) { driverBlip.Delete(); }
                        Game.DisplaySubtitle(askOfficerToFixDialogueLines[MathHelper.GetRandomInteger(0, askOfficerToFixDialogueLines.Length - 1)], 5000);
                        GameFiber.Wait(4000);
                        Game.DisplayHelp("Attempt to repair the vehicle with ~b~" + TrafficPolicerHandler.kc.ConvertToString(TrafficPolicerHandler.RepairVehicleKey) + "~s~ or tow it away.");
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (Game.IsKeyDown(TrafficPolicerHandler.RepairVehicleKey))
                        {
                            if (Vector3.Distance(Game.LocalPlayer.Character.Position, car.GetOffsetPosition(Vector3.RelativeFront * (car.Length) * 0.65f)) < 3f)
                            {
                                Game.LocalPlayer.Character.Tasks.GoStraightToPosition(car.FrontPosition, 1.3f, car.Heading + 180f, 1f, 3000).WaitForCompletion();
                                Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOOR_OPEN(car, 4, false, false);
                                Game.LocalPlayer.Character.Tasks.PlayAnimation("missexile3", "ex03_dingy_search_case_base_michael", 0.8f, AnimationFlags.None).WaitForCompletion();
                                Rage.Native.NativeFunction.Natives.SET_VEHICLE_DOOR_SHUT(car, 4, false);
                                int roll = TrafficPolicerHandler.rnd.Next(5);
                                if (roll < 2)
                                {
                                    car.IsDriveable = true;
                                    car.EngineHealth = 100f;
                                    Game.DisplaySubtitle(officerFixedVehicleDialogueLines[MathHelper.GetRandomInteger(0, officerFixedVehicleDialogueLines.Length - 1)], 5000);
                                    GameFiber.Wait(5000);
                                    Game.DisplaySubtitle(officerRecommendVisitGarageDialogueLines[MathHelper.GetRandomInteger(0, officerRecommendVisitGarageDialogueLines.Length - 1)], 5000);
                                    GameFiber.Wait(5000);
                                    Game.DisplaySubtitle(thanksOfficerDialogueLines[MathHelper.GetRandomInteger(0, thanksOfficerDialogueLines.Length-1)], 4000);
                                    driver.Tasks.FollowNavigationMeshToPosition(car.GetOffsetPosition(Vector3.RelativeLeft * 2f), car.Heading, 1.4f).WaitForCompletion(5000);
                                    driver.Tasks.EnterVehicle(car, 6000, -1).WaitForCompletion();
                                    driver.Tasks.CruiseWithVehicle(18f);
                                    GameFiber.Wait(5000);
                                    End();
                                }
                                else
                                {
                                    Game.DisplaySubtitle(vehicleCannotBeRepairedDialogueLines[MathHelper.GetRandomInteger(0,vehicleCannotBeRepairedDialogueLines.Length-1)], 5000);
                                    GameFiber.Wait(5000);
                                    Game.DisplaySubtitle(officerWillCallTowTruckDialogueLines[MathHelper.GetRandomInteger(0, officerFixedVehicleDialogueLines.Length-1)], 5000);
                                    GameFiber.Wait(2000);
                                    break;
                                }
                            }
                            else
                            {
                                Game.DisplayNotification("Move to the front of the vehicle to attempt to repair it.");
                            }

                        }
                        if (car.FindTowTruck().Exists() || Vector3.Distance(SpawnPoint, car.Position) > 15f)
                        {
                            break;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 150f)
                        {
                            End();
                        }
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        Game.DisplayHelp("Call for a tow truck to take the broken down vehicle away.");
                        if (car.FindTowTruck().Exists() || Vector3.Distance(SpawnPoint, car.Position) > 15f)
                        {
                            Game.HideHelp();
                            Game.DisplaySubtitle("~b~You: ~s~You can pick up your vehicle later!", 5000);
                            GameFiber.Wait(5000);
                            int roll = TrafficPolicerHandler.rnd.Next(25);
                            if (roll < 3)
                            {
                                
                                Game.DisplaySubtitle(pedAcceptsVehicleBeingTowedDialogueLines[MathHelper.GetRandomInteger(0, pedAcceptsVehicleBeingTowedDialogueLines.Length-1)], 5000);
                                End();

                            }
                            else
                            {
                                Game.DisplaySubtitle("~b~Are you for real? You've taken my vehicle?", 5000);
                                GameFiber.Wait(5000);
                                Game.DisplaySubtitle("~r~You can take my vehicle, but I'll take your life!", 5000);
                                break;

                            }
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 150f)
                        {
                            End();
                        }
                    }
                    if (eventRunning)
                    {
                        driver.Inventory.GiveNewWeapon("WEAPON_ASSAULTSMG", -1, true);
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();
                        if (driver.Exists() && !Functions.IsPedGettingArrested(driver) && driver.IsAlive)
                        {
                            driver.Tasks.FightAgainst(Game.LocalPlayer.Character).WaitForCompletion(1000);
                        }
                        else
                        {
                            break;
                        }
                    }

                }
                catch (System.Threading.ThreadAbortException e) { throw; }
                catch (Exception e) { Game.LogTrivial(e.ToString()); Game.DisplayNotification("Broken Down Vehicle encountered an error. Please send me your log file."); }
                finally { End(); }
            });
        }

        protected override void End()
        {
        
            if (car.Exists()) { car.Dismiss(); }
            if (driver.Exists()) { driver.Dismiss(); }
            BrokenDownVehicleRunning = false;
            base.End();

        }
   
    }
}

