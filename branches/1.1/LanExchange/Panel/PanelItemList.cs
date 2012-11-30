﻿using System;
using System.Collections.Generic;
using System.Text;
using OSTools;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LanExchange.Network;
#if DEBUG
using NLog;
#endif

namespace LanExchange
{
    public enum PanelItemListScope
    {
        DONT_SCAN = 0,
        ALL_GROUPS = 1,
        SELECTED_GROUPS = 2
    }

    public enum PanelItemType
    {
        COMPUTERS = 0,
        SHARES = 1,
        FILES = 2
    }

    public class PanelItemList : ISubscriber
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // items from NetworkScanner for each domain
        private Dictionary<string, IList<ServerInfo>> m_Results = null;
        // items added by user
        private SortedDictionary<string, PanelItem> m_Items = null;
        // merged all results and user items
        private SortedDictionary<string, PanelItem> m_Data = null;
        // keys for filtering
        private List<string> m_Keys = null;
        private String m_Filter = "";

        private string m_TabName = "";
        private View m_CurrentView = View.Details;
        private PanelItemListScope m_Scope = PanelItemListScope.DONT_SCAN;
        private List<string> m_Groups = null;

        public event EventHandler Changed;

        //private ListView m_LV = null;
        //private PanelItemType m_CurrentType = PanelItemType.COMPUTERS;
        //private string m_Path = null;

        private string m_FocusedItem;

        public static PanelItemList GetObject(ListView LV)
        {
            return LV.Tag as PanelItemList;
        }

        public void AttachObjectTo(ListView LV)
        {
            PanelItemList List = PanelItemList.GetObject(LV);
            LV.Tag = this;
            List = null;
        }

        public PanelItemList(string name)
        {
            m_Results = new Dictionary<string, IList<ServerInfo>>();
            m_Items = new SortedDictionary<string, PanelItem>();
            m_Data = new SortedDictionary<string, PanelItem>();
            m_Keys = new List<string>();
            m_TabName = name;
        }

        public string TabName
        {
            get { return m_TabName; }
            set { m_TabName = value; }
        }

        public View CurrentView
        {
            get { return m_CurrentView; }
            set { m_CurrentView = value; }
        }

        public IDictionary<string, PanelItem> Items
        {
            get { return m_Items; }
        }

        public PanelItemListScope Scope
        {
            get { return m_Scope; }
            set { m_Scope = value; }
        }

        public List<string> Groups
        {
            get { return m_Groups; }
            set { m_Groups = value; }
        }

        public IList<string> Keys
        {
            get { return m_Keys; }
        }

        public string FocusedItem
        {
            get { return m_FocusedItem; }
            set { m_FocusedItem = value; }
        }

        public void UpdateSubsctiption()
        {
            // оформляем подписку на получение списка компов
            switch (Scope)
            {
                case PanelItemListScope.ALL_GROUPS:
                    NetworkScanner.GetInstance().SubscribeToAll(this);
                    break;
                case PanelItemListScope.SELECTED_GROUPS:
                    NetworkScanner.GetInstance().UnSubscribe(this);
                    foreach (var group in Groups)
                        NetworkScanner.GetInstance().SubscribeToSubject(this, group);
                    break;
                default:
                    NetworkScanner.GetInstance().UnSubscribe(this);
                    break;
            }
        }


        public void Add(PanelItem Comp)
        {
            if (Comp != null)
                if (!m_Data.ContainsKey(Comp.Name))
                    m_Data.Add(Comp.Name, Comp);
        }

        public void Delete(PanelItem Comp)
        {
            m_Data.Remove(Comp.Name);
        }

        public PanelItem Get(string key)
        {
            PanelItem Result = null;
            if (m_Data.TryGetValue(key, out Result))
            {
                Result.Name = key;
                return Result;
            }
            else
                return null;
        }

        public void Clear()
        {
            m_Data.Clear();
        }

        private bool GoodForFilter(string[] A, string Filter1, string Filter2)
        {
            for (int i = 0; i < A.Length; i++)
            {
                if (i == 0)
                {
                    if (PuntoSwitcher.RussianContains(A[i], Filter1) || (PuntoSwitcher.RussianContains(A[i], Filter2)))
                        return true;
                } else
                if (Filter1 != null && A[i].Contains(Filter1) || Filter2 != null && A[i].Contains(Filter2))
                    return true;
            }
            return false;
        }

        public void ApplyFilter()
        {
            bool bFiltered = IsFiltered;
            m_Keys.Clear();
            string Filter1 = FilterText.ToUpper();
            string Filter2 = PuntoSwitcher.Change(FilterText);
            if (Filter2 != null) Filter2 = Filter2.ToUpper();
            foreach (var Pair in m_Data)
            {
                string[] A = Pair.Value.getStrings();
                if (!bFiltered || String.IsNullOrEmpty(Pair.Value.Name) || GoodForFilter(A, Filter1, Filter2))
                    m_Keys.Add(Pair.Value.Name);
            }
        }

        public bool IsFiltered
        {
            get { return !String.IsNullOrEmpty(m_Filter); }
        }

        // Возвращает количество компов в списке
        public int Count
        {
            get { return m_Data.Count; }
        }

        // Возвращает число записей в фильтре
        public int FilterCount
        {
            get { return m_Keys.Count; }
        }

        public String FilterText
        {
            get { return m_Filter; }
            set
            {
                m_Filter = value;
                ApplyFilter();
            }
        }

        public static List<PanelItem> EnumNetShares(string Server)
        {
            List<PanelItem> Result = new List<PanelItem>();
            Result.Add(new SharePanelItem("", "", 0, Server));
            int entriesread = 0;
            int totalentries = 0;
            int resume_handle = 0;
            int nStructSize = Marshal.SizeOf(typeof(NetApi32.SHARE_INFO_1));
            IntPtr bufPtr = IntPtr.Zero;
            StringBuilder server = new StringBuilder(Server);
            logger.Info("WINAPI NetShareEnum");
            int ret = NetApi32.NetShareEnum(server, 1, ref bufPtr, NetApi32.MAX_PREFERRED_LENGTH, ref entriesread, ref totalentries, ref resume_handle);
            if (ret == NetApi32.NERR_Success)
            {
                logger.Info("WINAPI NetServerEnum result: entriesread={0}, totalentries={1}", entriesread, totalentries);
                IntPtr currentPtr = bufPtr;
                for (int i = 0; i < entriesread; i++)
                {
                    NetApi32.SHARE_INFO_1 shi1 = (NetApi32.SHARE_INFO_1)Marshal.PtrToStructure(currentPtr, typeof(NetApi32.SHARE_INFO_1));
                    if ((shi1.shi1_type & (uint)NetApi32.SHARE_TYPE.STYPE_IPC) != (uint)NetApi32.SHARE_TYPE.STYPE_IPC)
                        Result.Add(new SharePanelItem(shi1.shi1_netname, shi1.shi1_remark, shi1.shi1_type, Server));
                    else
                        logger.Info("Skiping IPC$ share");
                    currentPtr = new IntPtr(currentPtr.ToInt32() + nStructSize);
                }
                NetApi32.NetApiBufferFree(bufPtr);
            }
            else
            {
                logger.Info("WINAPI NetServerEnum error: {0}", ret);
            }

            PanelItemComparer comparer = new PanelItemComparer();
            Result.Sort(comparer);
            return Result;
        }

        public List<string> ListView_GetSelected(ListView LV, bool bAll)
        {
            List<string> Result = new List<string>();
            if (LV.FocusedItem != null)
                Result.Add(LV.FocusedItem.Text);
            else
                Result.Add("");
            if (bAll)
                for (int index = 0; index < LV.Items.Count; index++)
                    Result.Add(m_Keys[index]);
            else
                foreach (int index in LV.SelectedIndices)
                    Result.Add(m_Keys[index]);
            return Result;
        }
        
        public void ListView_SetSelected(ListView LV, List<string> SaveSelected)
        {
            LV.SelectedIndices.Clear();
            LV.FocusedItem = null;
            if (LV.VirtualListSize > 0)
            {
                for (int i = 0; i < SaveSelected.Count; i++)
                {
                    int index = m_Keys.IndexOf(SaveSelected[i]);
                    if (index == -1) continue;
                    if (i == 0)
                    {
                        LV.FocusedItem = LV.Items[index];
                        //LV.EnsureVisible(index);
                    }
                    else
                        LV.SelectedIndices.Add(index);
                }
            }
        }

        // <summary>
        // Выбор компьютера по имени в списке.
        // </summary>
        public void ListView_SelectComputer(ListView LV, string CompName)
        {
            int index = -1;
            // пробуем найти запомненный элемент
            if (CompName != null)
            {
                index = this.m_Keys.IndexOf(CompName);
                if (index == -1) index = 0;
            }
            else
                index = 0;
            // установка текущего элемента
            if (LV.VirtualListSize > 0)
            {
                LV.SelectedIndices.Add(index);
                LV.FocusedItem = LV.Items[index];
                LV.EnsureVisible(index);
            }
        }

        public List<string> ToList()
        {
            List<string> Result = new List<string>();
            foreach (var Pair in m_Data)
                Result.Add(Pair.Value.Name);
            return Result;
        }

        public void DataChanged(ISubscriptionProvider sender, DataChangedEventArgs e)
        {
            IList<ServerInfo> List = (IList<ServerInfo>)e.Data;
            lock (m_Results)
            {
                if (m_Results.ContainsKey(e.Subject))
                    m_Results[e.Subject] = List;
                else
                    m_Results.Add(e.Subject, List);
                lock (m_Data)
                {
                    m_Data.Clear();
                    if (m_Scope != PanelItemListScope.DONT_SCAN)
                        foreach (var Pair in m_Results)
                        {
                            if (m_Scope == PanelItemListScope.ALL_GROUPS || m_Groups.Contains(Pair.Key))
                                foreach (var SI in Pair.Value)
                                    if (!m_Data.ContainsKey(SI.Name))
                                        m_Data.Add(SI.Name, new ComputerPanelItem(SI.Name, SI));
                        }
                    foreach (var Pair in m_Items)
                        m_Data.Add(Pair.Key, Pair.Value);
                    lock (m_Keys)
                    {
                        ApplyFilter();
                    }
                }
            }
            if (Changed != null)
                Changed(this, new EventArgs());
        }
        private void RebuildColumns()
        {
            /*
            if (LV == null) return;
            LV.Columns.Clear();
            switch (CurrentType)
            {
                case LVType.COMPUTERS:
                    LV.Columns.Add("Сетевое имя", 130);
                    LV.Columns.Add("Описание", 250);

                    break;
                case LVType.SHARES:
                    LV.Columns.Add("Общий ресурс", 130);
                    LV.Columns.Add("*:", 20);
                    LV.Columns.Add("Описание", 250);
                    break;
                case LVType.FILES:
                    LV.Columns.Add("Имя", 100);
                    LV.Columns.Add("Дата изменения", 100);
                    LV.Columns.Add("Тип", 100);
                    LV.Columns.Add("Размер", 100);
                    break;
            }
            */
        }

        /// <summary>
        /// Возвращает список элементов с верхнего уровня из стека переходов.
        /// В частности это будет список копьютеров, даже если мы находимся на уровне списка ресуров.
        /// </summary>
        /// <returns></returns>
        public IList<PanelItem> GetTopItemList()
        {
            return null;
            /*
            if (InternalStack.Count == 0)
                return InternalItems;
            else
            {
                IList<PanelItem>[] Arr = InternalStack.ToArray();
                return Arr[0];
            }
             */
        }

        public void LevelDown()
        {
            /*
            if (LV == null || LV.FocusedItem == null)
                return;
            string FocusedText = LV.FocusedItem.Text;
            if (String.IsNullOrEmpty(FocusedText))
            {
                LevelUp();
                return;
            }

            switch (ViewType)
            {
                case LVType.COMPUTERS:
                    if (LV.FocusedItem == null)
                        break;
                    // останавливаем поток пингов
                    MainForm.GetInstance().CancelCompRelatedThreads();
                    // сбрасываем фильтр
                    MainForm.GetInstance().UpdateFilter(MainForm.GetInstance().GetActiveListView(), "", false);
                    // текущий список добавляем в стек
                    //if (InternalItems == null)
                    //    InternalItems = InternalItemList.ToList();
                    InternalStack.Push(InternalItems);
                    // получаем новый список объектов, в данном случае список ресурсов компа
                    InternalItems = PanelItemList.EnumNetShares(FocusedText);
                    // устанавливаем новый список для визуального компонента
                    CurrentDataTable = InternalItems;
                    if (LV.VirtualListSize > 0)
                    {
                        LV.FocusedItem = LV.Items[0];
                        LV.SelectedIndices.Add(0);
                    }
                    // меняем колонки в ListView
                    Path = @"\\" + FocusedText;
                    ViewType = LVType.SHARES;
                    break;
                case LVType.SHARES:
                    MainForm.GetInstance().mFolderOpen_Click(MainForm.GetInstance().mFolderOpen, new EventArgs());
                    break;
                case LVType.FILES:
                    break;
            }
             */
        }

        public void LevelUp()
        {
            /*
            if (InternalStack.Count == 0)
                return;

            //TPanelItem PItem = null;
            string CompName = null;
            if (InternalItemList.Count > 0)
            {
                CompName = Path;
                if (CompName.Length > 2 && CompName[0] == '\\' && CompName[1] == '\\')
                    CompName = CompName.Remove(0, 2);
            }

            InternalItems = InternalStack.Pop();

            
            switch (CurrentType)
            {
                case LVType.COMPUTERS:
                    break;
                case LVType.SHARES:
                    ViewType = LVType.COMPUTERS;
                    break;
                case LVType.FILES:
                    ViewType = LVType.SHARES;
                    break;
            }
            CurrentDataTable = InternalItems;
            InternalItemList.ListView_SelectComputer(MainForm.GetInstance().lvComps, CompName);

            MainForm.GetInstance().UpdateFilter(MainForm.GetInstance().GetActiveListView(), MainForm.GetInstance().eFilter.Text, true);
             */
        }

        public void Select(int ItemIndex, bool IsSelected, bool IsFocused)
        {

        }
    }
}