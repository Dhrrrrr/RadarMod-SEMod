using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.ObjectBuilders;
using Sandbox;
using Sandbox.Definitions;
using VRage.Game.GUI.TextPanel;
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
using VRage.Game.Components.Interfaces;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;


namespace Dhr.HEAmmo
{
    [MyTextSurfaceScript("DhrsRadarInfoScript", "Radar Grid Information")]
    public class RadarGridTextPanelScript : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update100;

        private readonly IMyTerminalBlock TerminalBlock;

        private VRage.ModAPI.IMyEntity terminalGrid;



        public RadarGridTextPanelScript(IMyTextSurface surface, VRage.Game.ModAPI.IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block;
            TerminalBlock.OnMarkForClose += BlockMarkedForClose; 

            terminalGrid = block.CubeGrid;
        }


        public override void Run()
        {
            try
            {
                Draw();
            }
            catch (Exception e) 
            {
                DrawError(e);
            }
        }

        void Draw()
        {
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

            string infoString = "Stealth Signature: " + StealthInfoClass.gridInfo[terminalGrid.EntityId].stealthSignature + "\n" + "Size Signature: " + StealthInfoClass.gridInfo[terminalGrid.EntityId].sizeSignature;

            var frame = Surface.DrawFrame();

            var text = new MySprite()
            {
                Type = SpriteType.TEXT,
                Data = infoString,
                Position = screenCorner + screenSize / 2 - new Vector2(0, 8),
                RotationOrScale = 0.8f,
                Color = Surface.ScriptForegroundColor,
                Alignment = TextAlignment.CENTER,
                FontId = "Monospace"
            };

            frame.Add(text);

            frame.Dispose();
        }

        void DrawError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

            try
            {
                Vector2 screenSize = Surface.SurfaceSize;
                Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

                var frame = Surface.DrawFrame();

                var text = new MySprite()
                {
                    Type = SpriteType.TEXT,
                    Data = "ERROR",
                    Position = screenCorner + screenSize / 2 - new Vector2(0, 6),
                    RotationOrScale = 0.8f,
                    Color = Surface.ScriptForegroundColor,
                    Alignment = TextAlignment.CENTER,
                    FontId = "Monospace"
                };

                frame.Add(text);

                frame.Dispose();
            }
            catch (Exception e2)
            {
                MyLog.Default.WriteLineAndConsole($"Also failed to draw error on screen: {e2.Message}\n{e2.StackTrace}");

                if (MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
            }
        }


        public override void Dispose()
        {
            base.Dispose(); // do not remove
            TerminalBlock.OnMarkForClose -= BlockMarkedForClose;

            // Called when script is removed for any reason, so that you can clean up stuff if you need to.
        }

        void BlockMarkedForClose(VRage.ModAPI.IMyEntity ent)
        {
            Dispose();
        }

    }
}
