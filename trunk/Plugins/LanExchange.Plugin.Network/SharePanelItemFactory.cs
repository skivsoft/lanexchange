﻿using LanExchange.SDK;

namespace LanExchange.Plugin.Network
{
    public sealed class SharePanelItemFactory : PanelItemFactoryBase
    {
        public override PanelItemBase CreatePanelItem(PanelItemBase parent, string name)
        {
            return new SharePanelItem(parent, name);
        }

        public override PanelItemBase CreateDefaultRoot()
        {
            return null;
        }
    }
}