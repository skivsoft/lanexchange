﻿using System;
using LanExchange.SDK;

namespace LanExchange.Plugin.Users.Panel
{
    internal class UserPanelItem : PanelItemBase
    {
        private string m_Name;
        private string m_Description;

        public static void RegisterColumns(IPanelColumnManager columnManager)
        {
            columnManager.RegisterColumn(typeof(UserPanelItem), new PanelColumnHeader("User"));
            columnManager.RegisterColumn(typeof(UserPanelItem), new PanelColumnHeader("Description"));
        }

        public UserPanelItem(PanelItemBase parent)
            : base(parent)
        {
        }

        public UserPanelItem(PanelItemBase parent, string name) : base(parent)
        {
            m_Name = name;
        }

        public uint UserAccControl { get; set; }

        public string LockoutTime { get; set; }

        public bool IsAccountDisabled
        {
            get { return (UserAccControl & 2) != 0; }
        }

        public override string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Description
        {
            get { return m_Description; }
            set { m_Description = value; }
        }

        public override int CountColumns
        {
            get { return 2; }
        }

        protected override IComparable GetValue(int index)
        {
            switch (index)
            {
                case 0: return m_Name;
                case 1: return m_Description;
                default:
                    return null;
            }
        }

        public override string ImageName
        {
            get
            {
                return IsAccountDisabled ? PanelImageNames.UserDisabled : PanelImageNames.UserNormal;
            }
        }
    }
}
