﻿using System;
using LanExchange.Plugin.Network.Properties;
using LanExchange.SDK;

namespace LanExchange.Plugin.Network
{
    public class SharePanelItem : PanelItemBase//, IComparable<SharePanelItem>
    {
        private readonly ShareInfo m_SHI;

        public static void RegisterColumns(IPanelColumnManager columnManager)
        {
            columnManager.RegisterColumn(typeof(SharePanelItem), new PanelColumnHeader(Resources.ResourceName));
            columnManager.RegisterColumn(typeof(SharePanelItem), new PanelColumnHeader(Resources.Description));
        }

        public SharePanelItem()
        {
            m_SHI = new ShareInfo();
        }

        /// <summary>
        /// Panel item for network shared resource.
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        public SharePanelItem(PanelItemBase parent, ShareInfo shi) : base(parent)
        {
            if (shi == null)
                throw new ArgumentNullException("shi");
            m_SHI = shi;
            Comment = m_SHI.Comment;
        }

        public SharePanelItem(PanelItemBase parent, string name) : base(parent)
        {
            m_SHI = new ShareInfo(new NativeMethods.SHARE_INFO_1 {shi1_netname = name});
            Comment = string.Empty;
        }

        public override string ImageName
        {
            get
            {
                if (SHI.IsPrinter)
                    return PanelImageNames.SharePrinter;
                return SHI.IsHidden ? PanelImageNames.ShareHidden : PanelImageNames.ShareNormal;
            }
        }

        public ShareInfo SHI
        {
            get { return m_SHI; }
        }

        public override int CountColumns
        {
            get { return 2; }
        }

        public override IComparable GetValue(int index)
        {
            switch (index)
            {
                case 0: return Name;
                case 1: return Comment;
                default:
                    return null;
            }
        }

        public string ComputerName
        {
            get
            {
                var comp = Parent as ComputerPanelItem;
                return comp != null ? comp.Name : string.Empty;
            }
        }

        public override string Name 
        { 
            get { return m_SHI.Name; }
            set { m_SHI.Name = value; }
        }

        public string Comment
        {
            get { return m_SHI.Comment; }
            set { m_SHI.Comment = value; }
        }

        public uint ShareType
        {
            get { return m_SHI.ShareType; }
            set { m_SHI.ShareType = value; }
        }
    }
}