using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using LanExchange.Intf;
using LanExchange.Properties;
using LanExchange.SDK;

namespace LanExchange.Misc.Action
{
    public sealed class ShortcutFiller : IPanelFiller
    {
        private const string PANEL_ITEM_SUFFIX = "PanelItem";

        [Localizable(false)]
        public static PanelItemRoot ROOT_OF_SHORTCUTS = new PanelItemRoot("ShortcutPanelItem");

        public bool IsParentAccepted(PanelItemBase parent)
        {
            return (parent is PanelItemRoot) && (parent.Name.Equals(ROOT_OF_SHORTCUTS.Name));
        }

        public void Fill(PanelItemBase parent, ICollection<PanelItemBase> result)
        {
            result.Add(new ShortcutPanelItem(parent, Resources.KeyF1, Resources.KeyF1__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyF9, Resources.KeyF9__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyF10, Resources.KeyF10__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlT, Resources.KeyCtrlT__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlP, Resources.KeyCtrlP__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlF4, Resources.KeyCtrlF4__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlR, Resources.KeyCtrlR__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlWinX, Resources.KeyCtrlWinX__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlA, Resources.KeyCtrlA__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlC, Resources.KeyCtrlC__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlV, Resources.KeyCtrlV__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlIns, Resources.KeyCtrlIns__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlAltIns, Resources.KeyCtrlAltIns__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyDel, Resources.KeyDel__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyEsc, Resources.KeyEsc__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyEscLong, Resources.KeyEscLong__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyAnyChar, Resources.KeyAnyChar__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyBackspace, Resources.KeyBackspace__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlDown, Resources.KeyCtrlDown__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlUp, Resources.KeyCtrlUp__));
            result.Add(new ShortcutPanelItem(parent, Resources.KeyCtrlShiftT, Resources.KeyCtrlShiftT__));
            foreach (var pair in App.Addons.PanelItems)
                foreach (var menuItem in pair.Value.ContextMenuStrip)
                    if (!string.IsNullOrEmpty(menuItem.ShortcutKeys))
                    {
                        var shortcut = new ShortcutPanelItem(parent, menuItem.ShortcutKeys, menuItem.Text);
                        shortcut.Context = SuppressPostfix(pair.Key, PANEL_ITEM_SUFFIX);
                        if (menuItem.ProgramValue != null)
                            shortcut.CustomImageName = string.Format(CultureInfo.InvariantCulture, PanelImageNames.ADDON_FMT, menuItem.ProgramValue.Id);
                        result.Add(shortcut);
                    }
        }


        public Type GetFillType()
        {
            return typeof(ShortcutPanelItem);
        }

        private string SuppressPostfix(string value, string postfix)
        {
            if (value.EndsWith(postfix, StringComparison.Ordinal))
                return value.Remove(value.Length - postfix.Length);
            return value;
        }
    }
}