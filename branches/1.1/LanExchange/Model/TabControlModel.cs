﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanExchange.Utils;
using System.IO;

namespace LanExchange.Model
{
    public class PanelItemListEventArgs : EventArgs
    {
        private readonly PanelItemList m_Info;

        public PanelItemListEventArgs(PanelItemList Info)
        {
            m_Info = Info;
        }

        public PanelItemList Info { get { return m_Info; } }
    }

    public class IndexEventArgs : EventArgs
    {
        private readonly int m_Index;

        public IndexEventArgs(int Index)
        {
            m_Index = Index;
        }

        public int Index { get { return m_Index; } }
    }

    public delegate void PanelItemListEventHandler(object sender, PanelItemListEventArgs e);
    public delegate void IndexEventHandler(object sender, IndexEventArgs e);

    // 
    // Модель предоставляет знания: данные и методы работы с этими данными, 
    // реагирует на запросы, изменяя своё состояние. 
    // Не содержит информации, как эти знания можно визуализировать.
    // 

    public class TabControlModel
    {
        private readonly List<PanelItemList> m_List;
        private readonly string m_Name;
        private int m_SelectedIndex;

        public event PanelItemListEventHandler AfterAppendTab;
        public event IndexEventHandler AfterRemove;
        public event PanelItemListEventHandler AfterRename;
        public event IndexEventHandler SelectedIndexChanged;

        private LanExchangeTabs m_PagesSettings;

        public TabControlModel(string name)
        {
            m_List = new List<PanelItemList>();
            m_PagesSettings = new LanExchangeTabs();
            m_Name = name;
            m_SelectedIndex = -1;
        }

        public int Count { get { return m_List.Count; }  }

        public int SelectedIndex 
        {
            get
            {
                return m_SelectedIndex;
            }
            set
            {
                m_SelectedIndex = value;
                DoSelectedIndexChanged(value);
            }
        }

        public PanelItemList GetItem(int Index)
        {
            return m_List[Index];
        }

        public void DoAfterAppendTab(PanelItemList Info)
        {
            if (AfterAppendTab != null)
               AfterAppendTab(this, new PanelItemListEventArgs(Info));
        }

        public void DoAfterRemove(int Index)
        {
            if (AfterRemove != null)
                AfterRemove(this, new IndexEventArgs(Index));
        }

        public void DoAfterRename(PanelItemList Info)
        {
            if (AfterRename != null)
                AfterRename(this, new PanelItemListEventArgs(Info));
        }

        public void DoSelectedIndexChanged(int Index)
        {
            if (SelectedIndexChanged != null)
                SelectedIndexChanged(this, new IndexEventArgs(Index));
        }

        public bool Contains(PanelItemList Info)
        {
            return m_List.Contains(Info);
        }
        
        public void AddTab(PanelItemList Info)
        {
            // ommit duplicates
            if (!m_List.Contains(Info))
            {
                m_List.Add(Info);
                Info.UpdateSubsctiption();
                DoAfterAppendTab(Info);
            }
        }

        public void DelTab(int Index)
        {
            if (Index >= 0 && Index < m_List.Count)
            {
                ServerListSubscription.Instance.UnSubscribe(m_List[Index]);
                m_List.RemoveAt(Index);
                DoAfterRemove(Index);
            }
        }

        public void RenameTab(int Index, string NewTabName)
        {
            var Info = m_List[Index];
            Info.TabName = NewTabName;
            DoAfterRename(Info);
        }

        public string GetTabName(int i)
        {
            return i >= 0 && i <= Count - 1 ? m_List[i].TabName : null;
        }

        internal void Clear()
        {
            m_List.Clear();
        }

        public static string GetConfigFileName()
        {
            return Path.Combine(Path.GetDirectoryName(Settings.GetExecutableFileName()), "Pages.cfg");
        }

        public void StoreSettings()
        {
            m_PagesSettings.SelectedIndex = SelectedIndex;
            TabSettings[] pages = new TabSettings[Count];
            for (int i = 0; i < Count; i++)
                pages[i] = GetItem(i).Settings;
            m_PagesSettings.Items = pages;
            SerializeUtils.SerializeObjectToXMLFile(GetConfigFileName(), m_PagesSettings);
        }

        public void LoadSettings()
        {
            Clear();
            try
            {
                m_PagesSettings = (LanExchangeTabs)SerializeUtils.DeserializeObjectFromXMLFile(GetConfigFileName(), typeof(LanExchangeTabs));
            }
            catch { }
            if (m_PagesSettings.Items.Length > 0)
                Array.ForEach(m_PagesSettings.Items, Page =>
                {
                    var Info = new PanelItemList(Page.Name);
                    Info.Settings = Page;
                    AddTab(Info);
                });
            else
            {
                string domain = NetApi32Utils.GetMachineNetBiosDomain(null);
                var Info = new PanelItemList(domain)
                {
                    ScanMode = true
                };
                Info.Groups.Add(domain);
                AddTab(Info);
            }

            SelectedIndex = m_PagesSettings.SelectedIndex;
        }
    }
}
