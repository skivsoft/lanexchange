﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using LanExchange.Base;
using LanExchange.Helpers;
using LanExchange.Interfaces;
using LanExchange.SDK;
using LanExchange.Interfaces.Factories;
using System.Diagnostics.Contracts;
using LanExchange.Application.Interfaces;
using LanExchange.Presentation.Interfaces;
using LanExchange.Presentation.Interfaces.Factories;

namespace LanExchange.Plugin.WinForms.Impl
{
    [Localizable(false)]
    public class AddonManagerImpl : IAddonManager
    {
        private readonly IDictionary<string, AddonProgram> programs;
        private readonly IDictionary<string, AddOnItemTypeRef> panelItems;
        private readonly IFolderManager folderManager;
        private readonly IAddonProgramFactory programFactory;
        private readonly IImageManager imageManager;
        private readonly IPagesPresenter pagesPresenter;
        private readonly ITranslationService translationService;
        private readonly IPanelItemFactoryManager factoryManager;
        private readonly IWindowFactory windowFactory;

        private bool isLoaded;

        public AddonManagerImpl(
            IFolderManager folderManager, 
            IAddonProgramFactory programFactory,
            IImageManager imageManager,
            IPagesPresenter pagesPresenter,
            ITranslationService translationService,
            IPanelItemFactoryManager factoryManager,
            IWindowFactory windowFactory)
        {
            Contract.Requires<ArgumentNullException>(folderManager != null);
            Contract.Requires<ArgumentNullException>(programFactory != null);
            Contract.Requires<ArgumentNullException>(imageManager != null);
            Contract.Requires<ArgumentNullException>(pagesPresenter != null);
            Contract.Requires<ArgumentNullException>(translationService != null);
            Contract.Requires<ArgumentNullException>(factoryManager != null);
            Contract.Requires<ArgumentNullException>(windowFactory != null);

            this.folderManager = folderManager;
            this.programFactory = programFactory;
            this.imageManager = imageManager;
            this.pagesPresenter = pagesPresenter;
            this.translationService = translationService;
            this.factoryManager = factoryManager;
            this.windowFactory = windowFactory;

            programs = new Dictionary<string, AddonProgram>();
            panelItems = new Dictionary<string, AddOnItemTypeRef>();
        }

        public IDictionary<string, AddonProgram> Programs 
        { 
            get
            {
                LoadAddons();
                return programs;
            }
        }
        
        public IDictionary<string, AddOnItemTypeRef> PanelItems
        {
            get
            {
                LoadAddons();
                return panelItems;
            }
        }

        public void LoadAddons()
        {
            if (isLoaded) return;
            foreach (var fileName in folderManager.GetAddonsFiles())
                try
                {
                    LoadAddon(fileName);
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
            // register programs images
            foreach (var pair in programs)
                if (pair.Value.ProgramImage != null)
                {
                    var imageName = string.Format(CultureInfo.InvariantCulture, PanelImageNames.ADDON_FMT, pair.Key);
                    imageManager.RegisterImage(imageName, pair.Value.ProgramImage, pair.Value.ProgramImage);
                }
            isLoaded = true;
        }

        private void LoadAddon(string fileName)
        {
            // load addon from xml
            var addon = (AddOn)SerializeUtils.DeserializeObjectFromXmlFile(fileName, typeof(AddOn));
            // process programs
            foreach (var item in addon.Programs)
                if (!programs.ContainsKey(item.Id))
                {
                    item.Info = programFactory.CreateAddonProgramInfo(item);
                    programs.Add(item.Id, item);
                }
            // process protocols
            foreach (var item in addon.ItemTypes)
                foreach(var menuItem in item.ContextMenu)
                    if (ProtocolHelper.IsProtocol(menuItem.ProgramRef.Id))
                    {
                        var itemProgram = programFactory.CreateFromProtocol(menuItem.ProgramRef.Id);
                        if (itemProgram != null)
                            programs.Add(itemProgram.Id, itemProgram);
                    }
            // process menu items
            foreach (var item in addon.ItemTypes)
            {
                AddOnItemTypeRef found;
                if (panelItems.ContainsKey(item.Id))
                {
                    found = panelItems[item.Id];
                    panelItems.Remove(item.Id);
                }
                else
                    found = new AddOnItemTypeRef();
                if (item.CountVisible == 0) continue;

                // add separator to split menuItem groups
                if (found.ContextMenu.Count > 0)
                    found.ContextMenu.Add(new AddonMenuItem());
                foreach (var menuItem in item.ContextMenu)
                    if (menuItem.Visible)
                    {
                        if (menuItem.IsSeparator)
                            found.ContextMenu.Add(menuItem);
                        else
                        {
                            if (programs.ContainsKey(menuItem.ProgramRef.Id))
                                menuItem.ProgramValue = programs[menuItem.ProgramRef.Id];
                            if (!found.ContextMenu.Contains(menuItem) && 
                                menuItem.ProgramRef != null && 
                                menuItem.ProgramValue != null)
                                found.ContextMenu.Add(menuItem);
                        }
                    }
                panelItems.Add(item.Id, found);
            }
        }

        private void InternalBuildMenu(ToolStripItemCollection items, string id)
        {
            items.Clear();
            ToolStripMenuItem defaultItem = null;
            foreach (var item in PanelItems[id].ContextMenu)
            {
                if (item.IsSeparator)
                    items.Add(new ToolStripSeparator());
                else
                {
                    var menuItem = new ToolStripMenuItem();
                    menuItem.Tag = item;
                    menuItem.Text = translationService.Translate(item.Text);
                    menuItem.ShortcutKeyDisplayString = item.ShortcutKeys;
                    menuItem.Click += MenuItemOnClick;
                    if (item.ProgramValue != null)
                        menuItem.Image = item.ProgramValue.ProgramImage;
                    menuItem.Enabled = item.Enabled;
                    // lookup last default menuItem
                    if (item.Default)
                        defaultItem = menuItem;
                    items.Add(menuItem);
                }
            }
            if (defaultItem != null)
                defaultItem.Font = new Font(defaultItem.Font, FontStyle.Bold);
        }

        public bool BuildMenuForPanelItemType(object popTop, string id)
        {
            if (!PanelItems.ContainsKey(id))
                return false;

            var subMenu = SubMenuAdapter.CreateFrom(popTop);
            if (subMenu == null)
                return false;

            var tag = subMenu.Tag;
            if (tag == null || !tag.Equals(id))
            {
                InternalBuildMenu(subMenu.Items, id);
                subMenu.Tag = id;
            }
            return subMenu.Items.Count > 0;
        }

        public void SetupMenuForPanelItem(object popTop, PanelItemBase panelItem)
        {
            var subMenu = SubMenuAdapter.CreateFrom(popTop);
            if (subMenu == null) return;

            foreach (var menuItem in subMenu.Items)
            {
                var menuItem1 = menuItem as ToolStripMenuItem;
                if (menuItem1 == null) continue;

                var addonMenuItem = menuItem1.Tag as AddonMenuItem;
                if (addonMenuItem == null) continue;

                menuItem1.ToolTipText = string.Join(" ", AddonCommandStarter.BuildCmdLine(panelItem, addonMenuItem));

                var item = (AddonMenuItem)menuItem1.Tag;
                if (item != null)
                    item.CurrentItem = panelItem;
            }
        }

        /// <summary>
        /// Executes external program associated with menu menuItem.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void MenuItemOnClick(object sender, EventArgs eventArgs)
        {
            var menuItem = (sender as ToolStripMenuItem);
            if (menuItem == null) return;

            var item = (AddonMenuItem) menuItem.Tag;
            if (item == null || !item.Enabled) return;

            new AddonCommandStarter(factoryManager, windowFactory, item, item.CurrentItem).Start();
        }

        public void ProcessKeyDown(object args)
        {
            // TODO hide model
            //var pv = pagesPresenter.ActivePanelView;
            //var e = args as KeyEventArgs;
            //if (pv == null || e == null) return;
            //var panelItem = pv.Presenter.GetFocusedPanelItem(true);
            //if (panelItem == null) return;
            //var typeId = panelItem.GetType().Name;
            //if (!PanelItems.ContainsKey(typeId))
            //    return;
            //var item = PanelItems[typeId];
            //var shortcut = KeyboardUtils.KeyEventToString(e);
            //foreach (var menuItem in item.ContextMenu)
            //    if (menuItem.ShortcutPresent && menuItem.ShortcutKeys.Equals(shortcut) && menuItem.Enabled)
            //    {
            //        new AddonCommandStarter(factoryManager, windowFactory, menuItem, panelItem).Start();
            //        e.Handled = true;
            //        break;
            //    }
        }

        /// <summary>
        /// Run addon command marked with Default flag for current panelItem.
        /// </summary>
        public void RunDefaultCmdLine()
        {
            var pv = pagesPresenter.ActivePanelView;
            if (pv == null) return;

            // TODO hide model
            //var panelItem = pv.Presenter.GetFocusedPanelItem(true);
            //if (panelItem == null) return;

            //var typeId = panelItem.GetType().Name;
            //if (!PanelItems.ContainsKey(typeId))
            //    return;

            //var item = PanelItems[typeId];
            //AddonMenuItem defaultItem = null;
            //foreach (var menuItem in item.ContextMenu)
            //    if (menuItem.Default && menuItem.Enabled)
            //        defaultItem = menuItem;

            //if (defaultItem != null)
            //    new AddonCommandStarter(factoryManager, windowFactory, defaultItem, panelItem).Start();
        }
    }
}