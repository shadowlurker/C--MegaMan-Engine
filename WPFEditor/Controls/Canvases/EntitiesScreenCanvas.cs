﻿using System.Windows.Controls;
using MegaMan.Editor.Tools;

namespace MegaMan.Editor.Controls
{
    public class EntitiesScreenCanvas : ScreenCanvas
    {
        private EntityScreenLayer _entityLayer;

        public EntitiesScreenCanvas(IToolProvider toolProvider)
            : base(toolProvider)
        {
            _entityLayer = new EntityScreenLayer();

            this.Children.Insert(1, _entityLayer);

            Canvas.SetZIndex(_entityLayer, 10000);
        }

        protected override void ScreenChanged()
        {
            base.ScreenChanged();

            _entityLayer.Screen = Screen;
        }
    }
}
