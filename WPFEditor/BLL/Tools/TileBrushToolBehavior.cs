﻿using MegaMan.Common;
using MegaMan.Common.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace MegaMan.Editor.Bll.Tools
{
    public class TileBrushToolBehavior : IToolBehavior
    {
        private ITileBrush _brush;
        private bool held;
        private Point currentTilePos;
        private int?[,] startTiles;
        private int?[,] endTiles;

        public TileBrushToolBehavior(ITileBrush brush)
        {
            _brush = brush;
        }

        public void Click(ScreenDocument screen, Point location)
        {
            Point tilePos = new Point(location.X / screen.Tileset.TileSize, location.Y / screen.Tileset.TileSize);

            var selection = screen.Selection;
            if (selection != null)
            {
                // only paint inside selection
                if (!selection.Value.Contains(tilePos))
                {
                    startTiles = null;
                    endTiles = null;
                    return;
                }
            }

            startTiles = new int?[screen.Width, screen.Height];
            endTiles = new int?[screen.Width, screen.Height];

            // check for line drawing
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
            {
                var xdist = Math.Abs(tilePos.X - currentTilePos.X);
                var ydist = Math.Abs(tilePos.Y - currentTilePos.Y);

                if (xdist >= ydist)
                {
                    var min = Math.Min(currentTilePos.X, tilePos.X);
                    var max = Math.Max(currentTilePos.X, tilePos.X);
                    for (int i = min; i <= max; i++)
                    {
                        Draw(screen, i, currentTilePos.Y);
                    }
                }
                else
                {
                    var min = Math.Min(currentTilePos.Y, tilePos.Y);
                    var max = Math.Max(currentTilePos.Y, tilePos.Y);
                    for (int i = min; i <= max; i ++)
                    {
                        Draw(screen, currentTilePos.X, i);
                    }
                }
            }
            else
            {
                Draw(screen, tilePos.X, tilePos.Y);
                held = true;
            }

            currentTilePos = tilePos;
        }

        public void Move(ScreenDocument screen, Point location)
        {
            if (!held) return;

            Point pos = new Point(location.X / screen.Tileset.TileSize, location.Y / screen.Tileset.TileSize);
            if (pos == currentTilePos) return; // don't keep drawing on the same spot

            Draw(screen, pos.X, pos.Y);
        }

        public void Release(ScreenDocument surface)
        {
            if (startTiles == null) return;

            held = false;
        }

        public void RightClick(ScreenDocument surface, Point location)
        {
            throw new NotImplementedException();
        }

        private void Draw(ScreenDocument screen, int tile_x, int tile_y)
        {
            _brush.DrawOn(screen, tile_x, tile_y);
        }
    }
}
