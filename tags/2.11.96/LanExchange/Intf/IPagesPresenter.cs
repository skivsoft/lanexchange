﻿using System;

namespace LanExchange.Intf
{
    public interface IPagesPresenter : IPresenter<IPagesView>
    {
        event EventHandler PanelViewFocusedItemChanged;
        event EventHandler PanelViewFilterTextChanged;

        int Count { get; }

        void CommandCloseTab();

        int SelectedIndex { get; set; }

        void SaveDeffered();

        void SaveInstant();

        string GetTabName(int index);

        void SetupPanelViewEvents(IPanelView PV);

        void LoadSettings();

        IPanelModel GetItem(int index);

        bool CanSendToNewTab();

        bool CanPasteItems();

        bool AddTab(IPanelModel info);

        void CommandDeleteItems();

        void CommandPasteItems();

        void CommandSendToNewTab();

        void DoPanelViewFocusedItemChanged(object sender, EventArgs e);
        void DoPanelViewFilterTextChanged(object sender, EventArgs e);

        bool SelectTabByName(string tabName);

        void CommanCloseOtherTabs();
    }
}
