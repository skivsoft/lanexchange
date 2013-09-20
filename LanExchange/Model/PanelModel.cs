﻿using System;
using System.Collections.Generic;
using LanExchange.Intf;
using LanExchange.Model.Settings;
using LanExchange.SDK;

namespace LanExchange.Model
{
    public class PanelModel : IPanelModel
    {       
        // items added by user
        private readonly IList<PanelItemBase> m_Items;
        // merged all results and user items
        private readonly List<PanelItemBase> m_Data;
        // keys for filtering
        private readonly IList<PanelItemBase> m_Keys;
        // current path for item list
        private readonly ObjectPath<PanelItemBase> m_CurrentPath;
        // column sorter
        private readonly ColumnComparer m_Comparer;
        // punto switcher service
        private readonly IPuntoSwitcherService m_Punto;

        private Type m_DataType;

        public event EventHandler Changed;
        
        public PanelModel(IPuntoSwitcherService puntoService)
        {
            m_Punto = puntoService;
            m_Items = new List<PanelItemBase>();
            m_Data = new List<PanelItemBase>();
            m_Keys = new List<PanelItemBase>();
            m_CurrentPath = new ObjectPath<PanelItemBase>();
            m_Comparer = new ColumnComparer(0, PanelSortOrder.Ascending);
            CurrentView = PanelViewMode.Details;
        }

        public ObjectPath<PanelItemBase> CurrentPath
        {
            get { return m_CurrentPath; }
        }

        public IList<PanelItemBase> Items
        {
            get { return m_Items; }
        }

        public Tab Settings
        {
            get
            {
                var page = new Tab
                {
                    Name = TabName,
                    Path = m_CurrentPath,
                    Filter = FilterText,
                    View = CurrentView,
                    Focused = FocusedItem
                };
                page.Items = new PanelItemBase[m_Items.Count];
                int i = 0;
                foreach (var panelItem in m_Items)
                    page.Items[i++] = panelItem;
                page.DataType = DataType == null ? string.Empty : DataType.Name;
                return page;
            }
            set
            {
                TabName = value.Name;
                // build path from loaded items
                var items = value.Path.Item;
                m_CurrentPath.Clear();
                for (int index = items.Length - 1; index >= 0; index--)
                {
                    var item = items[index];
                    if (index < items.Length - 1)
                        item.Parent = items[index + 1];
                    m_CurrentPath.Push(item);
                }
                // set filter, currentview and focused
                FilterText = value.Filter;
                CurrentView = value.View;
                FocusedItem = value.Focused;
                // add loaded items
                m_Items.Clear();
                foreach (var panelItem in value.Items)
                    Items.Add(panelItem);
                // set DataType by thier name
                var types = App.PanelItemTypes.ToArray();
                foreach (var tp in types)
                    if (tp.Name.Equals(value.DataType))
                    {
                        DataType = tp;
                        break;
                    }
            }

        }

        public string GetImageName()
        {
            var items = m_CurrentPath.Item;
            if (items != null)
                for (int index = items.Length-1; index >= 0; index--)
                {
                    var item = items[index];
                    if (item.Parent is PanelItemRoot)
                        return item.ImageName;
                }
            return DataType.Name + PanelImageNames.NORMAL_POSTFIX;
        }

        public string TabName { get; set; }
        public PanelViewMode CurrentView { get; set; }
        public PanelItemBase FocusedItem { get; set; }

        //TODO: add delete item
        //public void Delete(PanelItem comp)
        //{
        //    m_Data.Remove(comp.Name);
        //}

        public PanelItemBase GetItemAt(int index)
        {
            return m_Keys[index];
        }

        //public PanelItemBase GetItem(string key)
        //{
        //    if (key == null) return null;
        //    var tempComp = new CustomPanelItem(null, key);
        //    int index = m_Data.BinarySearch(tempComp);
        //    if (index >= 0)
        //        return m_Data[index];
        //    return null;
        //}

        public int IndexOf(PanelItemBase key)
        {
            return m_Keys.IndexOf(key);
        }

        private bool GoodForFilter(string[] strList, string filter1, string filter2)
        {
            for (int i = 0; i < strList.Length; i++)
            {
                if (i == 0)
                {
                    if (m_Punto.SpecificContains(strList[i], filter1) ||
                        m_Punto.SpecificContains(strList[i], filter2))
                        return true;
                } else
                if (filter1 != null && strList[i].Contains(filter1) || filter2 != null && strList[i].Contains(filter2))
                    return true;
            }
            return false;
        }

        public Type DataType
        {
            get { return m_DataType; }
            set { m_DataType = value; }
        }

        public ColumnComparer Comparer
        {
            get { return m_Comparer; }
        }

        public void Sort(IComparer<PanelItemBase> sorter)
        {
            m_Data.Sort(sorter);
            ApplyFilter();
            OnChanged();
        }

        /// <summary>
        /// IFilterModel.FilterText
        /// </summary>
        public String FilterText { get; set; }

        
        /// <summary>
        /// IFilterModel.AppliFilter()
        /// </summary>
        public void ApplyFilter()
        {
            if (FilterText == null) 
                FilterText = string.Empty;
            var filtered = FilterText != string.Empty;
            //if (filtered && !CurrentPath.IsEmptyOrRoot)
            //    filtered = false;
            m_Keys.Clear();
            var filter1 = FilterText.ToUpper();
            var filter2 = m_Punto.Change(FilterText);
            if (filter2 != null) filter2 = filter2.ToUpper();
            var helper = new PanelModelCopyHelper(this);
            var upperValues = new List<string>();
            foreach (var value in m_Data)
            {
                if (value is PanelItemDoubleDot)
                {
                    m_Keys.Add(value);
                    continue;
                }
                helper.CurrentItem = value;
                upperValues.Clear();
                if (helper.Columns != null)
                    for (int i = 0; i < helper.ColumnsCount; i++)
                    {
                        var column = helper.GetColumnValue(i);
                        if (!string.IsNullOrEmpty(column))
                            upperValues.Add(column.ToUpper());
                    }
                if (!filtered || GoodForFilter(upperValues.ToArray(), filter1, filter2))
                    m_Keys.Add(value);
            }
        }

        public int Count
        {
            get { return m_Data.Count; }
        }

        public int FilterCount
        {
            get { return m_Keys.Count; }
        }

        public bool HasBackItem
        {
            get
            {
                if (m_Data.Count > 0)
                    if (m_Data[0] is PanelItemDoubleDot)
                        return true;
                return false;
            }
        }

        //    PanelItemComparer comparer = new PanelItemComparer();
        //    Result.Sort(comparer);
        //    return Result;
        //}

        // TODO uncomment ListView_GetSelected
        //public List<string> ListView_GetSelected(ListView LV, bool bAll)
        //{
        //    List<string> Result = new List<string>();
        //    if (LV.FocusedItem != null)
        //        Result.Add(LV.FocusedItem.Text);
        //    else
        //        Result.Add("");
        //    if (bAll)
        //        for (int index = 0; index < LV.Items.Count; index++)
        //            Result.Add(m_Keys[index]);
        //    else
        //        foreach (int index in LV.SelectedIndices)
        //            Result.Add(m_Keys[index]);
        //    return Result;
        //}
        
        // TODO uncomment ListView_SetSelected
        //public void ListView_SetSelected(ListView LV, List<string> SaveSelected)
        //{
        //    LV.SelectedIndices.Clear();
        //    LV.FocusedItem = null;
        //    if (LV.VirtualListSize > 0)
        //    {
        //        for (int i = 0; i < SaveSelected.Count; i++)
        //        {
        //            int index = m_Keys.IndexOf(SaveSelected[i]);
        //            if (index == -1) continue;
        //            if (i == 0)
        //            {
        //                LV.FocusedItem = LV.Items[index];
        //                //LV.EnsureVisible(index);
        //            }
        //            else
        //                LV.SelectedIndices.Add(index);
        //        }
        //    }
        //}


        //public void Add(AbstractPanelItem comp)
        //{
        //    if (comp == null)
        //        throw new ArgumentNullException("comp");
        //    if (!m_Data.Contains(comp))
        //        m_Data.Add(comp);
        //}

        //public List<string> ToList()
        //{
        //    List<string> Result = new List<string>();
        //    foreach (var Pair in m_Data)
        //        Result.Add(Pair.Value.Name);
        //    return Result;
        //}

        /// <summary>
        /// Sync retrieving panel items using appropriate filler strategy.
        /// </summary>
        public void SyncRetrieveData(bool clearFilter = false)
        {
            // get parent
            var parent = m_CurrentPath.IsEmpty ? null : m_CurrentPath.Peek();
            // retrieve items
            var items = App.PanelFillers.RetrievePanelItems(parent);
            // set items
            InternalSetData(items, clearFilter);
        }

        private void InternalSetData(PanelFillerResult fillerResult, bool clearFilter)
        {
            lock (m_Data)
            {
                m_Data.Clear();
                // add ".." item
                if (!m_CurrentPath.IsEmptyOrRoot)
                    m_Data.Add(new PanelItemDoubleDot(m_CurrentPath.Peek()));
                // add items from filler
                m_Data.AddRange(fillerResult.Items);
                // set current items DataType and filter
                if (fillerResult.ItemsType != null)
                    m_DataType = fillerResult.ItemsType;
                // add custom items created by user
                foreach(var panelItem in Items)
                    if (panelItem.GetType() == m_DataType)
                        m_Data.Add(panelItem);
                // sort 
                m_Data.Sort(m_Comparer);
                if (clearFilter)
                    FilterText = string.Empty;
                ApplyFilter();
            }
            OnChanged();
        }

        private void OnChanged()
        {
            if (Changed != null)
                Changed(this, EventArgs.Empty);
        }

        public bool Equals(IPanelModel other)
        {
            return String.Compare(TabName, other.TabName, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public string ToolTipText
        {
            get { return string.Empty; }
        }

        public bool IsCacheable
        {
            get { return false; }
        }

        public PanelItemFactoryBase ItemFactory { get; set; }

        public bool Contains(PanelItemBase panelItem)
        {
            if (m_Data.Contains(panelItem))
                return true;
            return m_Items.Contains(panelItem);
        }

        public void SetDefaultRoot(PanelItemBase root)
        {
            if (root.Parent != null)
                SetDefaultRoot(root.Parent);
            CurrentPath.Push(root);
        }

        public string ImageName { get; set; }
    }
}