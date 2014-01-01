﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;
using LanExchange.Intf;
using LanExchange.Model;
using System.ComponentModel;
using System.Security.Permissions;
using LanExchange.Presenter.Action;
using LanExchange.Properties;
using LanExchange.SDK;
using LanExchange.Utils;

namespace LanExchange.UI
{
    public partial class MainForm : RunMinimizedForm, IMainView, ITranslationable
    {
        public const int WAIT_FOR_KEYUP_MS = 500;
        public PagesView Pages;
        private readonly GlobalHotkeys m_Hotkeys;

        public MainForm()
        {
            InitializeComponent();
            // App.MainView must be set before panel view will be created
            App.MainView = this;
            SetRunMinimized(App.Config.RunMinimized);
            // setup languages in menu
            SetupMenuLanguages();
            // init main form
            SetupForm();
            // set hotkey for activate: Ctrl+Win+X
            m_Hotkeys = new GlobalHotkeys();
            App.Resolve<IDisposableManager>().RegisterInstance(m_Hotkeys);
            if (!m_Hotkeys.RegisterGlobalHotKey((int) Keys.X, GlobalHotkeys.MOD_CONTROL + GlobalHotkeys.MOD_WIN, Handle))
                mTrayOpen.ShortcutKeyDisplayString = string.Empty;
            // set lazy events
            App.Threads.DataReady += OnDataReady;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Windows.Forms.Control.set_Text(System.String)")]
        [Localizable(false)]
        private void SetupForm()
        {
            // init Pages presenter
            Pages = (PagesView)App.Resolve<IPagesView>();
            Pages.Dock = DockStyle.Fill;
            Controls.Add(Pages);
            Pages.BringToFront();
            // setup images
            App.Images.SetImagesTo(Pages.Pages);
            App.Images.SetImagesTo(Status);
            // load saved pages from config
            Pages.SetupContextMenu();
            //App.MainPages.View.SetupContextMenu();
            App.MainPages.PanelViewFocusedItemChanged += Pages_PanelViewFocusedItemChanged;
            App.MainPages.LoadSettings();
            // set mainform bounds
            var rect = App.Presenter.SettingsGetBounds();
            SetBounds(rect.Left, rect.Top, rect.Width, rect.Height);
            // set mainform title
            var aboutModel = App.Resolve<IAboutModel>();
            var text = String.Format(CultureInfo.CurrentCulture, "{0} {1}", aboutModel.Title, aboutModel.VersionShort);
            if (SystemInformation.TerminalServerSession)
                text += string.Format(" [{0}]", Resources.Terminal);
            Text = text;
            // show tray
            TrayIcon.Text = Text;
            TrayIcon.Visible = true;
            // show computer name
            lCompName.Text = SystemInformation.ComputerName;
            lCompName.ImageIndex = App.Images.IndexOf(PanelImageNames.ComputerNormal);
            // show current user
            lUserName.Text = SystemInformation.UserName;
            lUserName.ImageIndex = App.Images.IndexOf(PanelImageNames.UserNormal);
        }

        private void SetupMenuLanguages()
        {
            var nameDict = App.TR.GetLanguagesNames();
            if (nameDict.Count < 2)
            {
                mLanguage.Visible = false;
                return;
            }
            mLanguage.Visible = true;
            mLanguage.MenuItems.Clear();
            foreach(var pair in nameDict)
            {
                var menuItem = new MenuItem(pair.Value);
                menuItem.RadioCheck = true;
                menuItem.Tag = pair.Key;
                menuItem.Click += MenuItemOnClick;
                mLanguage.MenuItems.Add(menuItem);
            }
        }

        private void MarkCurrentLanguage()
        {
            foreach (MenuItem menuItem in mLanguage.MenuItems)
                menuItem.Checked = menuItem.Tag.Equals(App.TR.CurrentLanguage);
        }

        private void mLanguage_Popup(object sender, EventArgs e)
        {
            MarkCurrentLanguage();
        }

        private void MenuItemOnClick(object sender, EventArgs eventArgs)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null)
                App.Config.Language = (string)menuItem.Tag;
        }

        public void TranslateUI()
        {
            TranslationUtils.TranslateComponents(Resources.ResourceManager, this, components);
            mTrayOpen_TranslateUI();
            TranslationUtils.TranslateControls(Controls);
            // addons context menu will refresh later
            popTop.Tag = null;
            // refresh shortcut panel if present
            var foundIndex = ActionShortcutKeys.FindPanelIndex();
            if (foundIndex != -1)
            {
                App.MainPages.RenameTab(foundIndex, Resources.mHelpKeys_Text);
                var model = App.MainPages.GetItem(foundIndex);
                model.SyncRetrieveData();
            }
        }

        private void mTrayOpen_TranslateUI()
        {
            mTrayOpen.Text = Visible ? Resources.MainForm_Close : Resources.mTrayOpen_Text;
        }

        private bool m_EscDown;
        private DateTime m_EscTime;

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                var pv = Pages.ActivePanelView;
                e.Handled = true;
                if (pv != null && pv.Filter.IsVisible)
                    pv.Filter.SetFilterText(string.Empty);
                else
                {
                    var parent = pv == null || pv.Presenter.Objects.CurrentPath.IsEmptyOrRoot
                                     ? null
                                     : pv.Presenter.Objects.CurrentPath.Peek();
                    if ((parent == null) || App.PanelItemTypes.DefaultRoots.Contains(parent))
                        Hide();
                    else if (!m_EscDown)
                    {
                        m_EscTime = DateTime.UtcNow;
                        m_EscDown = true;
                    }
                    else
                    {
                        TimeSpan diff = DateTime.UtcNow - m_EscTime;
                        if (diff.TotalMilliseconds >= WAIT_FOR_KEYUP_MS)
                        {
                            Hide();
                            m_EscDown = false;
                        }
                    }
                }
            }
            // Ctrl+Up/Ctrl+Down - change number of info lines
            if (e.Control && (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down))
            {
                App.Config.NumInfoLines = App.Config.NumInfoLines + (e.KeyCode == Keys.Down ? +1 : -1);
                e.Handled = true;
            }
        }

        private void MainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (m_EscDown)
                {
                    TimeSpan diff = DateTime.UtcNow - m_EscTime;
                    var pv = Pages.ActivePanelView;
                    var presenter = pv.Presenter;
                    if (pv != null && !presenter.Objects.CurrentPath.IsEmptyOrRoot)
                    {
                        if (diff.TotalMilliseconds < WAIT_FOR_KEYUP_MS)
                            presenter.CommandLevelUp();
                        else
                            Hide();
                    }
                    m_EscDown = false;
                }
                e.Handled = true;
            }
        }

        private void popTop_Opening(object sender, CancelEventArgs e)
        {
            var pv = Pages.ActivePanelView as PanelView;
            if (pv == null)
            {
                e.Cancel = true;
                return;
            }
            if (pInfo.CurrentItem == null)
            {
                e.Cancel = true;
                return;
            }
            e.Cancel = !App.Addons.BuildMenuForPanelItemType(popTop, pInfo.CurrentItem.GetType().Name);
        }

        private void tipComps_Popup(object sender, PopupEventArgs e)
        {
            var tooltip = (sender as ToolTip);
            if (tooltip == null) return;
            if (e.AssociatedControl == pInfo.Picture)
            {
                tooltip.ToolTipTitle = Resources.MainForm_Legend;
                return;
            }
            if (e.AssociatedControl is TabControl && e.AssociatedControl == Pages.Pages)
            {
                var tab = Pages.GetTabPageByPoint(e.AssociatedControl.PointToClient(MousePosition));
                if (tab != null)
                    tooltip.ToolTipTitle = tab.Text;
                else
                    e.Cancel = true;
                return;
            }
            tooltip.ToolTipTitle = string.Empty;
        }

        private void mHelpAbout_Click(object sender, EventArgs e)
        {
            App.Presenter.ExecuteAction<ActionAbout>();
        }
        
        private void lItemsCount_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var point = Status.PointToScreen(e.Location);
                popTray.Show(point);
            }
        }

        /// <summary>
        /// This event fires when focused item of PanelView has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pages_PanelViewFocusedItemChanged(object sender, EventArgs e)
        {
            // get focused item from current PanelView
            var pv = sender as PanelView;
            if (pv == null) return;
            var panelItem = pv.Presenter.GetFocusedPanelItem(false, true);
            // check if parent item more informative than current panel item
            while (panelItem != null &&
                   panelItem.Parent != PanelItemRoot.ROOT_OF_USERITEMS &&
                   !App.PanelItemTypes.DefaultRoots.Contains(panelItem) &&
                   !App.PanelItemTypes.DefaultRoots.Contains(panelItem.Parent))
                panelItem = panelItem.Parent;
            if (panelItem == null) return;
            pInfo.CurrentItem = panelItem;
            // update info panel at top of the form
            pInfo.Picture.Image = App.Images.GetLargeImage(panelItem.ImageName);
            SetToolTip(pInfo.Picture, panelItem.ImageLegendText);
            var helper = new PanelModelCopyHelper(null);
            helper.CurrentItem = panelItem;
            int index = 0;
            foreach (var column in helper.Columns)
            {
                pInfo.SetLine(index, helper.GetColumnValue(column.Index));
                ++index;
                if (index >= pInfo.NumLines) break;
            }
            for (int i = index; i < pInfo.NumLines; i++)
                pInfo.SetLine(i, string.Empty);
        }

        public void ClearInfoPanel()
        {
            pInfo.CurrentItem = null;
            pInfo.Picture.Image = null;
            for (int index = 0; index < pInfo.NumLines; index++)
                pInfo.SetLine(index, string.Empty);
            lItemsCount.Text = string.Empty;
        }

        private void popTray_Opening(object sender, CancelEventArgs e)
        {
            mTrayOpen_TranslateUI();
        }

        private void mOpen_Click(object sender, EventArgs e)
        {
            ToggleVisible();
        }

        private void TrayIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ToggleVisible();
        }

        private void mTrayExit_Click(object sender, EventArgs e)
        {
            ApplicationExit();
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode)]
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case GlobalHotkeys.WM_HOTKEY:
                    if ((short)m.WParam == m_Hotkeys.HotkeyID)
                    {
                        ToggleVisible();
                        m.Result = new IntPtr(1);
                    }
                    break;
                case NativeMethods.WM_QUERYENDSESSION:
                    m.Result = new IntPtr(1);
                    break;
                case NativeMethods.WM_ENDSESSION:
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            App.Presenter.SettingsSetBounds(Bounds);
            //Settings.SaveIfModified();
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            Pages.FocusPanelView();
        }

        private void mReRead_Click(object sender, EventArgs e)
        {
            App.Presenter.ExecuteAction<ActionReRead>();
            popTop.Tag = null;
        }

        private void lCompName_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                var scm = new ShellContextMenu();
                scm.ShowContextMenuForCSIDL(Handle, ShellAPI.CSIDL.DRIVES, Cursor.Position);
            }
        }

        private void Status_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                Process.Start("explorer.exe", "/n,::{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
        }

        private void UpdatePanelRelatedMenu()
        {
            mViewLarge.Checked = false;
            mViewSmall.Checked = false;
            mViewList.Checked = false;
            mViewDetails.Checked = false;
            var pv = Pages.ActivePanelView;
            var enabled = pv != null;
            mViewLarge.Enabled = enabled;
            mViewSmall.Enabled = enabled;
            mViewList.Enabled = enabled;
            mViewDetails.Enabled = enabled;
            if (pv != null)
                switch (pv.ViewMode)
                {
                    case PanelViewMode.LargeIcon:
                        mViewLarge.Checked = true;
                        break;
                    case PanelViewMode.SmallIcon:
                        mViewSmall.Checked = true;
                        break;
                    case PanelViewMode.List:
                        mViewList.Checked = true;
                        break;
                    case PanelViewMode.Details:
                        mViewDetails.Checked = true;
                        break;
                }
        }

        private void mView_Popup(object sender, EventArgs e)
        {
            mViewGrid.Checked = App.Config.ShowGridLines;
            mViewInfo.Checked = App.Config.ShowInfoPanel;
            UpdatePanelRelatedMenu();
        }

        private void mViewLarge_Click(object sender, EventArgs e)
        {
            var pv = Pages.ActivePanelView;
            if (pv == null) return;
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;
            int tag;
            if (int.TryParse(menuItem.Tag.ToString(), out tag))
                pv.ViewMode = (PanelViewMode)tag;
        }

        private void mWebPage_Click(object sender, EventArgs e)
        {
            var presenter = App.Resolve<IAboutPresenter>();
            presenter.OpenHomeLink();
        }

        private void mHelpLangs_Click(object sender, EventArgs e)
        {
            var presenter = App.Resolve<IAboutPresenter>();
            presenter.OpenLocalizationLink();
        }

        private void mHelpBugs_Click(object sender, EventArgs e)
        {
            var presenter = App.Resolve<IAboutPresenter>();
            presenter.OpenBugTrackerWebLink();
        }

        private void mHelpFeedback_Click(object sender, EventArgs e)
        {
            var presenter = App.Resolve<IAboutPresenter>();
            presenter.OpenEmailLink();
        }

        private void mHelpKeys_Click(object sender, EventArgs e)
        {
            App.Presenter.ExecuteAction<ActionShortcutKeys>();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            App.Threads.Dispose();
        }

        public void OnDataReady(object sender, DataReadyArgs args)
        {
            BeginInvoke(new WaitCallback(MainForm_RefreshItem), new object[1] { args.Item });
        }

        private void MainForm_RefreshItem(object item)
        {
            var pv = Pages.ActivePanelView;
            if (pv != null)
            {
                var index = pv.Presenter.Objects.IndexOf(item as PanelItemBase);
                if (index >= 0)
                    pv.RedrawItem(index);
            }
        }

        private void lCompName_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                var label = (sender as ToolStripStatusLabel);
                if (label != null)
                {
                    var obj = new DataObject();
                    obj.SetText(label.Text, TextDataFormat.UnicodeText);
                    Status.DoDragDrop(obj, DragDropEffects.Copy);
                }
            }
        }

        public void SetToolTip(object control, string tipText)
        {
            if (control is Control)
                tipComps.SetToolTip(control as Control, tipText);
        }

        public void ShowStatusText(string format, params object[] args)
        {
            lItemsCount.Text = String.Format(CultureInfo.InvariantCulture, format, args);
        }

        private void mPanel_Popup(object sender, EventArgs e)
        {
            mReRead.Enabled = App.Presenter.IsActionEnabled<ActionReRead>();
            mCloseTab.Enabled = App.Presenter.IsActionEnabled<ActionCloseTab>();
            mCloseOther.Enabled = App.Presenter.IsActionEnabled<ActionCloseOther>();
        }

        private void mCloseTab_Click(object sender, EventArgs e)
        {
            App.Presenter.ExecuteAction<ActionCloseTab>();
        }

        private void mCloseOther_Click(object sender, EventArgs e)
        {
            App.Presenter.ExecuteAction<ActionCloseOther>();
        }

        private void mViewInfo_Click(object sender, EventArgs e)
        {
            App.Config.ShowInfoPanel = !App.Config.ShowInfoPanel;
        }

        private void mViewGrid_Click(object sender, EventArgs e)
        {
            App.Config.ShowGridLines = !App.Config.ShowGridLines;
        }

        public bool ShowInfoPanel
        {
            get { return pInfo.Visible; }
            set { pInfo.Visible = value; }
        }

        public int NumInfoLines
        {
            get { return pInfo.NumLines; }
            set { pInfo.NumLines = value; }
        }


        public void Invoke(Delegate method, object sender)
        {
            base.Invoke(method, sender);
        }
    }
}