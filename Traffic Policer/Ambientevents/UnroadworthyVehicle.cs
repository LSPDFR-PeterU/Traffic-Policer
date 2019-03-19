using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rage;
using LSPD_First_Response.Mod.API;

namespace Traffic_Policer.Ambientevents
{
    
    internal class UnroadworthyVehicle : AmbientEvent
    {
        
        /// <summary>
        /// All possible events that might make the vehicle unroadworthy.
        /// </summary>
        [Flags]
        internal enum UnroadworthyVehicleEventFlags
        {
            BurstTire = 1,
            FuelLeaking = 2,
            EngineSmoking = 4,
            SmashedWindow = 8
        }

        /// <summary>
        /// Set to the maximum sum value of UnroadworthyVehicleEventFlags flags, for generating
        /// a random number that will be some combination of those flags.
        /// </summary>
        protected const int UnroadworthyVehicleEventFlagsMaxValue = 15;


        public UnroadworthyVehicle(Ped Driver, bool createBlip, bool showMessage) : base (Driver, createBlip, showMessage, "Creating unroadworthy vehicle event.")
        {
            MainLogic();
        }


        protected override void MainLogic()
        {
            AmbientEventMainFiber = GameFiber.StartNew(delegate
            {

                try {

                    UnroadworthyVehicleEventFlags eventFlags = (UnroadworthyVehicleEventFlags)MathHelper.GetRandomInteger(0, UnroadworthyVehicleEventFlagsMaxValue);
                    
                    if ((eventFlags & UnroadworthyVehicleEventFlags.SmashedWindow) != 0)
                    {
                        Game.LogTrivial($"Unroadworthy vehicle event chose to have smashed window. Event flags: {eventFlags}");
                        car.Windows[TrafficPolicerHandler.rnd.Next(3)].Remove();
                    }

                    if ((eventFlags & UnroadworthyVehicleEventFlags.BurstTire) != 0)
                    {
                        Game.LogTrivial($"Unroadworthy vehicle event chose to have burst tire. Event flags: {eventFlags}");
                        car.Wheels[0].BurstTire();

                        // maybe the driver puts on hazards and slows down
                        if (MathHelper.GetRandomInteger(0, 3) > 1)
                        {
                            car.IndicatorLightsStatus = VehicleIndicatorLightsStatus.Both;
                            Rage.Native.NativeFunction.Natives.SetDriveTaskCruiseSpeed(car.Driver, MathHelper.ConvertMilesPerHourToMetersPerSecond(18f));
                        }

                    }
                    
                    if ((eventFlags & UnroadworthyVehicleEventFlags.FuelLeaking) != 0)
                    {
                        Game.LogTrivial($"Unroadworthy vehicle event chose to have fuel leaking. Event flags: {eventFlags}");
                        car.FuelTankHealth = 51f; // no effect on non player vehicle?
                    }
                    
                    if ((eventFlags & UnroadworthyVehicleEventFlags.EngineSmoking) != 0)
                    {
                        Game.LogTrivial($"Unroadworthy vehicle event chose to have engine smoking. Event flags: {eventFlags}");
                        car.EngineHealth = 60f;
                    }


                    if (TrafficPolicerHandler.IsLSPDFRPlusRunning)
                    {
                        //TODO make this specific to event type
                        API.LSPDFRPlusFunctions.AddQuestionToTrafficStop(driver, "What happened to your vehicle?", new List<string> { "It needs a trip to the garage, officer.", "It's getting a bit old.", "Someone slashed my tyres!", "What's wrong with it?" });
                    }
                    while (eventRunning)
                    {
                        GameFiber.Yield();

                        if (Game.IsPaused)
                        {
                            GameFiber.Yield();
                            continue;
                        }

                        if ((eventFlags & UnroadworthyVehicleEventFlags.FuelLeaking) != 0 && FuelPtfxHandle == 0)
                        {
                            Game.LogTrivial("Start fuel leak ptfx");
                            if (!HasLoadedFuelPtfx)
                            {
                                LoadFuelLeakPtfx();
                            }
                            Rage.Native.NativeFunction.Natives.x6C38AF3693A69A91("core"); //_SET_PTFX_ASSET_NEXT_CALL
                            FuelPtfxHandle = Rage.Native.NativeFunction.Natives.xC6EB449E33977F0B<uint>("veh_petrol_leak", car, 0f, 0f, 0f, 0f, 0f, 0f, 1, 1f, false, false, false);
                            Game.LogTrivial($"Started fuel leak ptfx -- {FuelPtfxHandle}");
                        }

                        if (performingPullover && !Functions.IsPlayerPerformingPullover())
                        {
                            //pullover no longer active
                            eventRunning = false;
                            break;
                        }

                        if (Functions.IsPlayerPerformingPullover())
                        {
                            // only stop actions if a pullover did exist and now doesn't
                            performingPullover = true;
                        }
                        if (Vector3.Distance(Game.LocalPlayer.Character.Position, driver.Position) > 300f)
                        {
                            eventRunning = false;

                            break;
                        }
                    }

                    
                   
                    
                } catch (Exception e)
                {
                    if (driverBlip.Exists())
                    {
                        driverBlip.Delete();
                    }
                    if (car.Exists())
                    {
                        car.IsPersistent = false;
                    }
                    
                }
                finally
                {
                    End();
                }
            }, GetType().UnderlyingSystemType.Name);

           
        }

        /// <summary>
        /// Load the particle effect for the fuel leak.
        /// </summary>
        protected void LoadFuelLeakPtfx()
        {
            try
            {
                Rage.Native.NativeFunction.Natives.RequestNamedPtfxAsset("core");
                while (!Rage.Native.NativeFunction.Natives.HasNamedPtfxAssetLoaded<bool>("core"))
                {
                    GameFiber.Yield();
                }


                Game.LogTrivial("Loaded ptfx asset core");
                HasLoadedFuelPtfx = true;
            }
            catch (Exception e)
            {
                Game.LogTrivial($"{e}");
            }
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        protected override void End()
        {
            base.End();
            try
            {
                if (FuelPtfxHandle > 0 && Rage.Native.NativeFunction.Natives.DoesParticleFxLoopedExist<bool>(FuelPtfxHandle.Value))
                {
                    Game.LogTrivial($"Clean up ambient fuel ptfx {FuelPtfxHandle}");
                    Rage.Native.NativeFunction.Natives.StopParticleFxLooped(FuelPtfxHandle.Value, false);
                    Rage.Native.NativeFunction.Natives.RemoveParticleFx(FuelPtfxHandle.Value, false);
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception e)
            {
                Game.LogTrivial($"{e}");
            }
        }

        /// <summary>
        /// Handle to the fuel leak particle effect.
        /// </summary>
        protected PoolHandle FuelPtfxHandle;

        /// <summary>
        /// Whether or not we have loaded the particle effect.
        /// </summary>
        protected bool HasLoadedFuelPtfx = false;

    }
}
