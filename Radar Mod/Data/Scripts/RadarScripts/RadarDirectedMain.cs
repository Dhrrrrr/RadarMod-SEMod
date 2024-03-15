using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.ObjectBuilders;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;

namespace Dhr.HEAmmo
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_RadioAntenna), false, "DirectedRadarLarge", "DirectedRadarSmall")]
    class RadarDirectedMain : MyGameLogicComponent
    {
        // Countermeasure configs
        const float countermeasureEffectiveRange = 1200;

        // Note these stack on top of a 1, so 0.2 in the final will be 1.2, just needs to be this way for the math to work correctly
        const float chaffEffectiveness = 0.15f;
        const float flareEffectiveness = 0.05f;

        // Module config
        const float powerMultiplierPerModule = 1.7f;

        // Base modules 
        const float rangeMultiplierSmall = 1.3f;
        const float rangeMultiplier = 1.3f;

        const float angleMultiplierSmall = 1.2f;
        const float angleMultiplier = 1.3f;

        const float accuracyMultiplierSmall = 0.5f;
        const float accuracyMultiplier = 0.2f;

        const float countermeasureMultiplierSmall = 0.5f;
        const float countermeasureMultiplier = 0.3f;

        const float stealthMultiplierSmall = 2.2f;
        const float stealthMultiplier = 3f;

        const float standPowerRequired = 4;
        const float standDetectionDistance = 12000;
        const float standDetectionAngle = 30;
        const float standInaccuracy = 8;

        const float smallGridDetectionMultiplier = 0.5f;
        const float smallGridAngleMultiplier = 0.5f;
        const float smallGridPowerMultiplier = 0.25f;
        const float smallGridInaccuracyMultiplier = 1.5f;




        float powerUsage;

        float currentPowerUsageMultipler = 1;
        float currentRangeUpgradeMultiplier = 1;
        float currentAngleUpgradeMultiplier = 1;
        float currentAccuracyUpgradeMultiplier = 1;
        float currentCountermeasureUpgradeMultiplier = 1;
        float currentStealthUpgradeMultiplier = 1;


        // Special modifiers
        bool FriendOrFoeModule = false;
        bool AdvancedInfoModule = false;

        IMyFunctionalBlock radarBlock;
        IMyRadioAntenna castedBlock;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            // Setup on initialisation
            radarBlock = (IMyFunctionalBlock)Entity;
            castedBlock = (IMyRadioAntenna)Entity;
            
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();


            // Check if the radar exists, in case of ghost grids
            if (radarBlock?.CubeGrid?.Physics == null)
                return;

            if (radarBlock.Enabled == true || radarBlock.IsFunctional)
            {
                // Get radar position and forward for calculating if it is within boundries
                Vector3D currentRadarPosition = radarBlock.WorldMatrix.Translation;
                Vector3D radarForward = radarBlock.WorldMatrix.Forward;

                // Get all entities in the ession
                var entities = new HashSet<VRage.ModAPI.IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities);

                // Get final angle and distance
                float detectionAngle = standDetectionAngle * currentAngleUpgradeMultiplier;
                float detectionRange = standDetectionDistance * currentRangeUpgradeMultiplier;
                float innaccuracy = standInaccuracy * currentAccuracyUpgradeMultiplier;
                powerUsage = standPowerRequired * currentPowerUsageMultipler;

                if (radarBlock.CubeGrid.GridSize == 0.5)
                {
                    detectionAngle *= smallGridAngleMultiplier;
                    detectionRange *= smallGridDetectionMultiplier;
                    powerUsage *= smallGridPowerMultiplier;
                    innaccuracy *= smallGridInaccuracyMultiplier;
                }

                // Set some variables on the block
                castedBlock.Radius = detectionRange;
                castedBlock.EnableBroadcasting = false;

                // Detect entities and give their info
                string finalData = "";

                // Get owner identity of the radar
                IMyFaction ownerFaction = null;
                var radarOwner = radarBlock.CubeGrid.BigOwners;

                if (radarOwner != null && radarOwner.Count != 0)
                {
                    ownerFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(radarOwner[0]);
                }

                foreach (var entity in entities)
                {
                    if (entity is VRage.Game.ModAPI.IMyCubeGrid)
                    {
                        // Cast entity to grid
                        VRage.Game.ModAPI.IMyCubeGrid gridEntity = (VRage.Game.ModAPI.IMyCubeGrid)entity;
                        Vector3D gridCentre = gridEntity.WorldVolume.Center;

                        // Get angle from entity to radar forward
                        Vector3D radarToEntity = gridCentre - currentRadarPosition;
                        double angle = MathHelper.ToDegrees(Math.Acos(MathHelper.Clamp(radarToEntity.Dot(radarForward) / Math.Sqrt(radarToEntity.LengthSquared() * radarForward.LengthSquared()), -1, 1)));

                        float gridDetectionDistance = detectionRange * StealthInfoClass.CalculateDetectionSignature(gridEntity, currentStealthUpgradeMultiplier);

                        // Check if the object is within boundaries
                        if (angle < detectionAngle && radarToEntity.Length() < gridDetectionDistance && gridEntity != radarBlock.CubeGrid)
                        {

                            // Makes the target accuracy better the closer they are
                            int finalInaccuracy = (int)(innaccuracy * CalculateInnacuracy(gridEntity, gridDetectionDistance, (float)radarToEntity.Length()));

                            Random rand = new Random();
                            Vector3D targetLocation = gridCentre + new Vector3D(rand.Next(-finalInaccuracy, finalInaccuracy), rand.Next(-finalInaccuracy, finalInaccuracy), rand.Next(-finalInaccuracy, finalInaccuracy));

                            string line = gridEntity.DisplayName + "," + targetLocation;
                            
                            line += ",";

                            // Get advanced info 
                            if (AdvancedInfoModule)
                            {
                                line += gridEntity.LinearVelocity;
                            }
                            else
                            {
                                line += "null";
                            }

                            line += ",";

                            // Get friendly info
                            if (FriendOrFoeModule)
                            {
                                // Get opposite grid main owner
                                var owners = gridEntity.BigOwners;

                                // Check if either side has null owners
                                if (owners != null && owners.Count != 0 && ownerFaction != null)
                                {
                                    // Set their friendleyness status
                                    IMyFaction opFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owners[0]);

                                    if (opFaction == ownerFaction)
                                    {
                                        line += "Same";
                                    }
                                    else
                                    {

                                        MyRelationsBetweenFactions factionRelations = MyAPIGateway.Session.Factions.GetRelationBetweenFactions(opFaction.FactionId, ownerFaction.FactionId);

                                        if (factionRelations == MyRelationsBetweenFactions.Friends)
                                        {
                                            line += "Friendly";
                                        }
                                        else if (factionRelations == MyRelationsBetweenFactions.Enemies)
                                        {
                                            line += "Hostile";
                                        }
                                        else if (factionRelations == MyRelationsBetweenFactions.Neutral)
                                        {
                                            line += "Neutral";
                                        }
                                    }

                                }
                                else
                                {
                                    line += "Neutral";
                                }

                            }
                            else
                            {
                                line += "null";
                            }

                            finalData += line + "\n";
                        }
                    }
                }

                // Print targetting info to custom data
                radarBlock.CustomData = finalData;

                
            }
            else
            {
                radarBlock.CustomData = "";
            }

            // Set powerusage
            var powerResource = Entity.Components.Get<MyResourceSinkComponent>();

            if (powerResource != null)
            {
                powerResource.SetRequiredInputFuncByType(MyResourceDistributorComponent.ElectricityId, GetPowerUsage);
                powerResource.Update();
            }

        }

        public override void UpdateAfterSimulation100()
        {
            base.UpdateAfterSimulation100();

            // Checks for new modules
            if (radarBlock?.CubeGrid?.Physics != null)
            {
                CalculatePowerUsage(radarBlock?.CubeGrid);

            }
        }

        private float GetPowerUsage()
        {
            // Check if the block is functional
            if (radarBlock.IsFunctional && radarBlock.Enabled)
            {
                return powerUsage;
            }
            else
            {
                return 0;
            }
        }

        public void CalculatePowerUsage(VRage.Game.ModAPI.IMyCubeGrid grid)
        {
            // Get all modules and put them in lists
            var rangeUpgrades = new List<IMyTerminalBlock>();
            var angleUpgrades = new List<IMyTerminalBlock>();
            var accuracyUpgrades = new List<IMyTerminalBlock>();
            var FOFUpgrades = new List<IMyTerminalBlock>();
            var advancedTargettingUpgrades = new List<IMyTerminalBlock>();
            var countermeasureUpgrades = new List<IMyTerminalBlock>();
            var stealthUpgrades = new List<IMyTerminalBlock>();



            int totalUpgradeModules = 0;

            // Get all terminal blocks
            foreach (var block in grid.GetFatBlocks<IMyFunctionalBlock>())
            {
                // Check if block is active
                if (block.IsFunctional && block.Enabled == true)
                {
                    // Check if the ID is an existing upgrade
                    string blockID = block.BlockDefinition.SubtypeId;

                    if (blockID == "DirectedRadarRangeUpgradeSmall" || blockID == "DirectedRadarRangeUpgrade")
                    {
                        rangeUpgrades.Add(block);

                    }
                    else if (blockID == "DirectedRadarAngleUpgradeSmall" || blockID == "DirectedRadarAngleUpgrade")
                    {
                        angleUpgrades.Add(block);

                    }
                    else if (blockID == "DirectedRadarAccuracyUpgradeSmall" || blockID == "DirectedRadarAccuracyUpgrade")
                    {
                        accuracyUpgrades.Add(block);

                    }
                    else if (blockID == "DirectedRadarFOFSmall" || blockID == "DirectedRadarFOF")
                    {
                        FOFUpgrades.Add(block);
                    }
                    else if (blockID == "DirectedRadarAdvancedTargettingSmall" || blockID == "DirectedRadarAdvancedTargetting")
                    {
                        advancedTargettingUpgrades.Add(block);
                    }
                    else if (blockID == "DirectedRadarCountermeasureUpgradeSmall" || blockID == "DirectedRadarCountermeasureUpgrade")
                    {
                        countermeasureUpgrades.Add(block);
                    }
                    else if (blockID == "DirectedRadarStealthUpgradeSmall" || blockID == "DirectedRadarStealthUpgrade")
                    {
                        stealthUpgrades.Add(block);
                    }
                }
            }

            if (FOFUpgrades.Count != 0)
            {
                totalUpgradeModules += 1;
                FriendOrFoeModule = true;
            }
            else
            {
                FriendOrFoeModule = false;
            }

            if (advancedTargettingUpgrades.Count != 0)
            {
                totalUpgradeModules += 1;
                AdvancedInfoModule = true;
            }
            else
            {
                AdvancedInfoModule = false;
            }

            // Calculate power usage multipliers
            totalUpgradeModules = rangeUpgrades.Count + angleUpgrades.Count + accuracyUpgrades.Count + countermeasureUpgrades.Count + stealthUpgrades.Count;    
            currentPowerUsageMultipler = (float)Math.Pow(powerMultiplierPerModule, totalUpgradeModules);

            // Use modules to set other multipliers
            ProcessUpgrade(rangeUpgrades, ref currentRangeUpgradeMultiplier, rangeMultiplierSmall, rangeMultiplier);
            ProcessUpgrade(angleUpgrades, ref currentAngleUpgradeMultiplier, angleMultiplierSmall, angleMultiplier); ;
            ProcessUpgrade(accuracyUpgrades, ref currentAccuracyUpgradeMultiplier, accuracyMultiplierSmall, accuracyMultiplier);
            ProcessUpgrade(countermeasureUpgrades, ref currentCountermeasureUpgradeMultiplier, countermeasureMultiplierSmall, countermeasureMultiplier);
            ProcessUpgrade(stealthUpgrades, ref currentStealthUpgradeMultiplier, stealthMultiplierSmall, stealthMultiplier);

        }

        public void ProcessUpgrade(List<IMyTerminalBlock> processList, ref float modifier, float changeSmall, float changeLarge)
        {
            // Set up processing number for storing the output of all the blocks
            float processingNum = 1;


            // Sort through list and check if they are small or large grid
            foreach (var block in processList)
            {
    
                if (block.CubeGrid.GridSize == 0.5)
                {
                    processingNum *= changeSmall;
                }
                else
                {
                    processingNum *= changeLarge;
                }
            }

            modifier = processingNum;
        }

        public float CalculateInnacuracy(VRage.Game.ModAPI.IMyCubeGrid grid, float maxDetection, float currentDistance)
        {
            float multiplier = 1;

            // Create bounding area for identifying the locations of missiles, centred around the grid
            var missileList = new List<MyEntity>();
            BoundingSphereD missileSphere = new BoundingSphereD(grid.WorldVolume.Center, countermeasureEffectiveRange);

            MyAPIGateway.Missiles.GetAllMissilesInSphere(ref missileSphere, missileList);
            
            // Get all missiles within radius and calculate total effectiviness of countermeasures
            foreach (var missile in missileList)
            {
                IMyMissile castedMissile = (IMyMissile)missile;

                // Get dist effectiveness multiplier based on formula y = 1.6^x - 0.6
                float distMultiplier = (float)(Math.Pow(1.6, (1 - ((missile.PositionComp.GetPosition() - grid.WorldVolume.Center).Length() / countermeasureEffectiveRange))) - 0.6);

                string missileName = castedMissile.AmmoDefinition.Id.SubtypeName;
                
                if (missileName == "Flare")
                {
                    multiplier *= 1 + (flareEffectiveness * distMultiplier * currentCountermeasureUpgradeMultiplier);
                }
                else if (missileName == "Chaff")
                {
                    multiplier *= 1 + (chaffEffectiveness * distMultiplier * currentCountermeasureUpgradeMultiplier);
                }

                MyAPIGateway.Utilities.CreateNotification(castedMissile.GetObjectBuilder().SubtypeName);
            }

            // Get multipler based on distance using formula m = (-5^(-(current / max)) + 1.3)
            multiplier *= (float)(-Math.Pow(5, -(currentDistance / maxDetection)) + 1.3);

            return multiplier;
        }
    }
}
