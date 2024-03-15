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
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class StealthInfoClass : MySessionComponentBase
    {
        bool firstTick = false;
        
        // Information storing dictionary
        static public Dictionary<long, GridDetectionInfo> gridInfo = new Dictionary<long, GridDetectionInfo>();

        static private List<VRage.ModAPI.IMyEntity> gridList = new List<VRage.ModAPI.IMyEntity>();
        static public Dictionary<string, float> stealthBlockInfo = new Dictionary<string, float>();

        // Load entities
        public override void LoadData()
        {
            stealthBlockInfo.Add("LargeStealthCube", 1);
            stealthBlockInfo.Add("LargeStealthSlope", 1);
            stealthBlockInfo.Add("LargeStealthCorner", 1);
            stealthBlockInfo.Add("LargeStealthInvCorner", 1);
            stealthBlockInfo.Add("LargeStealthCornerSquare", 1);
            stealthBlockInfo.Add("LargeStealthCornerSquareInv", 1);
            stealthBlockInfo.Add("SmallStealthCube", 1);
            stealthBlockInfo.Add("SmallStealthSlope", 1);
            stealthBlockInfo.Add("SmallStealthCorner", 1);
            stealthBlockInfo.Add("SmallStealthInvCorner", 1);
            stealthBlockInfo.Add("SmallStealthCornerSquare", 1);
            stealthBlockInfo.Add("SmallStealthCornerSquareInv", 1);

            stealthBlockInfo.Add("LargeStealthSlope2x1x1Base", 1);
            stealthBlockInfo.Add("LargeStealthSlope2x1x1Tip", 1);
            stealthBlockInfo.Add("LargeHalfStealth", 1);
            stealthBlockInfo.Add("LargeHalfSlopeStealth", 1);
            stealthBlockInfo.Add("LargeHalfSlopeCornerStealth", 1);
            stealthBlockInfo.Add("LargeStealthHalfCorner", 1);
            stealthBlockInfo.Add("LargeStealthHalfSlopedCorner", 1);
            stealthBlockInfo.Add("SmallStealthSlope2x1x1Base", 1);
            stealthBlockInfo.Add("SmallStealthSlope2x1x1Tip", 1);
            stealthBlockInfo.Add("SmallHalfStealth", 1);
            stealthBlockInfo.Add("SmallHalfSlopeStealth", 1);
            stealthBlockInfo.Add("SmallHalfSlopeCornerStealth", 1);
            stealthBlockInfo.Add("SmallStealthHalfCorner", 1);
            stealthBlockInfo.Add("SmallStealthHalfSlopedCorner", 1);

            stealthBlockInfo.Add("LargeStealthCorner2x1x1Base", 1);
            stealthBlockInfo.Add("LargeStealthCorner2x1x1Tip", 1);
            stealthBlockInfo.Add("LargeStealthInvCorner2x1x1Base", 1);
            stealthBlockInfo.Add("LargeStealthInvCorner2x1x1Tip", 1);
            stealthBlockInfo.Add("LargeHalfSlopeCornerInvStealth", 1);
            stealthBlockInfo.Add("LargeHalfSlopeInvStealth", 1);
            stealthBlockInfo.Add("SmallStealthCorner2x1x1Base", 1);
            stealthBlockInfo.Add("SmallStealthCorner2x1x1Tip", 1);
            stealthBlockInfo.Add("SmallStealthInvCorner2x1x1Base", 1);
            stealthBlockInfo.Add("SmallStealthInvCorner2x1x1Tip", 1);
            stealthBlockInfo.Add("SmallHalfSlopeCornerInvStealth", 1);
            stealthBlockInfo.Add("SmallHalfSlopeInvStealth", 1);

            stealthBlockInfo.Add("LargeSlopedCornerStealthTip", 1);
            stealthBlockInfo.Add("LargeStealthSlopedCornerBase", 1);
            stealthBlockInfo.Add("LargeStealthSlopedCorner", 1);
            stealthBlockInfo.Add("LargeStealthHalfSlopedCornerBase", 1);
            stealthBlockInfo.Add("SmallSlopedCornerStealthTip", 1);
            stealthBlockInfo.Add("SmallStealthSlopedCornerBase", 1);
            stealthBlockInfo.Add("SmallStealthSlopedCorner", 1);
            stealthBlockInfo.Add("SmallStealthHalfSlopedCornerBase", 1);

            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
        }
        protected override void UnloadData()
        {

            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
        }
        public override void UpdateAfterSimulation()
        {
            // Loads all grids on frist tick

            if (!firstTick)
            {
                firstTick = true;

                foreach (var entity in gridList)
                {
                    GridDetectionInfo detectInfo;

                    if (gridInfo.TryGetValue(entity.EntityId, out detectInfo))
                    {
                        detectInfo.FullRecalculate();
                    }
                }
            }
        }
        private void EntityAdded(VRage.ModAPI.IMyEntity entity)
        {
            GenerateGridStealthInfo(entity);
        }
        private void BlockAdded(VRage.Game.ModAPI.IMySlimBlock block)
        {
            GridDetectionInfo info;

            // Check if the grid exists
            if (!gridInfo.TryGetValue(block.CubeGrid.EntityId, out info))
            {
                return;
            }

            List<VRage.Game.ModAPI.IMyCubeGrid> grids = new List<VRage.Game.ModAPI.IMyCubeGrid>();
            block.CubeGrid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(grids);

            GridDetectionInfo indInfo;

            // Check stealth change
            if (CheckStealthStatus(block))
            {
                foreach (var grid in grids)
                {
                    if (grid.GridSize == block.CubeGrid.GridSize)
                    {
                        indInfo = gridInfo[grid.EntityId];
                        indInfo.stealthBlockCount += 1;

                        indInfo.CalcRadarSig();
                        indInfo.CalcStealthSig();
                    }
                }

            }
            else
            {
                foreach (var grid in grids)
                {
                    if (grid.GridSize == block.CubeGrid.GridSize)
                    {
                        indInfo = gridInfo[grid.EntityId];
                        indInfo.nonStealthCount += 1;

                        indInfo.CalcRadarSig();
                        indInfo.CalcStealthSig();
                    }
                }

            }
        }
        private void BlockRemoved(VRage.Game.ModAPI.IMySlimBlock block)
        {
            GridDetectionInfo info;

            // Check if the grid exists
            if (!gridInfo.TryGetValue(block.CubeGrid.EntityId, out info))
            {
                return;
            }

            List<VRage.Game.ModAPI.IMyCubeGrid> grids = new List<VRage.Game.ModAPI.IMyCubeGrid>();
            block.CubeGrid.GetGridGroup(GridLinkTypeEnum.Physical).GetGrids(grids);

            GridDetectionInfo indInfo;

            // Check stealth change
            if (CheckStealthStatus(block))
            {
                foreach (var grid in grids)
                {
                    if (grid.GridSize == block.CubeGrid.GridSize)
                    {
                        indInfo = gridInfo[grid.EntityId];
                        indInfo.stealthBlockCount -= 1;

                        indInfo.CalcRadarSig();
                        indInfo.CalcStealthSig();
                    }
                }
            }
            else
            {
                foreach (var grid in grids)
                {
                    if (grid.GridSize == block.CubeGrid.GridSize)
                    {
                        indInfo = gridInfo[grid.EntityId];
                        indInfo.nonStealthCount -= 1;

                        indInfo.CalcRadarSig();
                        indInfo.CalcStealthSig();
                    }
                }
            }
        }
        private bool CheckStealthStatus(VRage.Game.ModAPI.IMySlimBlock block)
        {
            if (block.FatBlock is IMyFunctionalBlock)
            {
                return false;
            }
            else
            {
                if (stealthBlockInfo.ContainsKey(block.BlockDefinition.Id.SubtypeName))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private void GridClosed(VRage.ModAPI.IMyEntity entity)
        {
            // Removes grid from stealth section
            gridInfo.Remove(entity.EntityId);
        }
        public class GridDetectionInfo
        {
            // Block counts used for calculation
            public int stealthBlockCount { get; set; }
            public int nonStealthCount { get; set; }

            // Size counts used for calculation
            public float stealthSignature { get; set; }
            public float sizeSignature { get; set; }

            // Grid entity
            public VRage.ModAPI.IMyEntity entity { get;}

            // Dictionary for stealth blocks

            public Dictionary<string, float> stealthBlockInfo;


            public GridDetectionInfo(VRage.ModAPI.IMyEntity insertEntity, Dictionary<string, float> stealthDic)
            {
                stealthBlockCount = 0;
                nonStealthCount = 0;

                entity = insertEntity;

                stealthBlockInfo = stealthDic;

                CalcRadarSig();
                CalcStealthSig();
            }

            /// <summary>
            /// Calculates the signature of the grid and stores it inside of sizeSignature
            /// </summary>
            public void CalcRadarSig()
            {
                sizeSignature = CalculateRadarSignature(entity);
            }

            /// <summary>
            /// Calculates the stealth signature of the grid
            /// </summary>
            public void CalcStealthSig()
            {
                stealthSignature = CalculateStealthSignature(stealthBlockCount, nonStealthCount);
            }

            /// <summary>
            /// Recalculate the whole grid block counts
            /// </summary>
            public void FullRecalculate()
            {
                stealthBlockCount = 0;
                nonStealthCount = 0;

                VRage.Game.ModAPI.IMyCubeGrid grid = (VRage.Game.ModAPI.IMyCubeGrid)entity;
                List<VRage.Game.ModAPI.IMyCubeGrid> grids = new List<VRage.Game.ModAPI.IMyCubeGrid>();
                var gridGroup = grid.GetGridGroup(GridLinkTypeEnum.Physical);

                if (gridGroup != null)
                {
                    gridGroup.GetGrids(grids);

                    foreach (var gridEntity in grids)
                    {
                        CalculateGrid(gridEntity);
                    }
                }
            }            
            private void CalculateGrid(VRage.ModAPI.IMyEntity insertEntity)
            {
                VRage.Game.ModAPI.IMyCubeGrid grid = (VRage.Game.ModAPI.IMyCubeGrid)insertEntity;

                if (grid.GridSize != ((VRage.Game.ModAPI.IMyCubeGrid)entity).GridSize)
                {
                    return;
                }

                List<VRage.Game.ModAPI.IMySlimBlock> blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
                grid.GetBlocks(blocks);

                foreach (var block in blocks)
                {
                    try
                    {
                        if (CheckStealthStatus(block))
                        {
                            stealthBlockCount += 1;
                        }
                        else
                        {
                            nonStealthCount += 1;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            static private float CalculateRadarSignature(VRage.ModAPI.IMyEntity entity)
            {
                VRage.Game.ModAPI.IMyCubeGrid grid = (VRage.Game.ModAPI.IMyCubeGrid)entity;

                // Calc multiplier using y = -1.09^(-x+4.705) + 1.5
                double boundingBoxSize = grid.WorldVolume.Radius;
                float radarSignatureMultiplier = (float)((Math.Pow(1.09, -boundingBoxSize + 4.705) * -1) + 1.5);

                return radarSignatureMultiplier;
            }
            static private float CalculateStealthSignature(int stealthCount, int normalCount)
            {
                float blockCount = stealthCount + normalCount;
                float StealthPercent;

                if (blockCount != 0)
                {
                    StealthPercent = (float)stealthCount / blockCount;
                }
                else
                {
                    return 0;
                }

                return (float)Math.Pow(20, -StealthPercent);
            }
            public override string ToString()
            {
                return ((VRage.Game.ModAPI.IMyCubeGrid)entity).DisplayName + ", Stealth: " + stealthSignature + ", Stealth Block: " + stealthBlockCount + ", Normal: " + nonStealthCount;
            }
            private bool CheckStealthStatus(VRage.Game.ModAPI.IMySlimBlock block)
            {
                if (block.FatBlock is IMyFunctionalBlock)
                {
                    return false;
                }
                else
                {
                    if (stealthBlockInfo.ContainsKey(block.BlockDefinition.Id.SubtypeName))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        static public float CalculateDetectionSignature(VRage.Game.ModAPI.IMyCubeGrid grid, double stealthUpgradeCount)
        {
            // Get the grid info
            GridDetectionInfo currentGridInfo = gridInfo[grid.EntityId];

            float radarSignatureMultiplier = (float)(currentGridInfo.sizeSignature * Math.Pow(currentGridInfo.stealthSignature, 1 / (stealthUpgradeCount * 2)));

            return radarSignatureMultiplier;
        }
        private void GenerateGridStealthInfo(VRage.ModAPI.IMyEntity entity)
        {
            // Detect if entity is a grid being added
            if (entity is VRage.Game.ModAPI.IMyCubeGrid)
            {
                // Get grid
                VRage.Game.ModAPI.IMyCubeGrid grid = entity as VRage.Game.ModAPI.IMyCubeGrid;

                gridInfo.Add(entity.EntityId, new GridDetectionInfo(entity, stealthBlockInfo));

                grid.OnBlockAdded += BlockAdded;
                grid.OnBlockRemoved += BlockRemoved;
                grid.OnMarkForClose += GridClosed;

                // Add to grid list
                gridList.Add(entity);

                // Calculate grid
                gridInfo[entity.EntityId].FullRecalculate();
            }
        }
    }
}
