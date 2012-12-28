﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using LanExchange.Model;
using LanExchange.View;
using LanExchange.Presenter;
using NLog;
using System.Diagnostics;
using GongSolutions.Shell.Interop;
using GongSolutions.Shell;
using vbAccelerator.Components.ImageList;

namespace LanExchange.UI
{
    public partial class MainForm : Form, IMainView
    {
        // logger object 
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();
        // controller for Pages (MVC-style)

        private static MainPresenter m_Presenter;
        
        public static MainForm Instance;
        
        FormWindowState  LastWindowState = FormWindowState.Normal;

        public MainForm()
        {
            InitializeComponent();
            Instance = this;
            // load settings from cfg-file
            Settings.LoadSettings();
            // init MainForm presenter
            m_Presenter = new MainPresenter(this);
            // init Pages presenter
            m_Presenter.Pages = new TabControlPresenter(Pages);
            //mSendToNewTab.Click += new EventHandler(TabController.mSendToNewTab_Click);
            
            // init main form
            SetupForm();
            SetupImages();
            SetupMenu();
            // init network scanner
#if DEBUG
            ServerListSubscription.Instance.RefreshInterval = 10 * 1000; // refresh every 5 sec in debug mode
#else
            ServerListSubscription.Instance.RefreshInterval = (int)Settings.Instance.RefreshTimeInSec * 1000;
#endif
            // set admin mode
            AdminMode = Settings.Instance.AdvancedMode;
        }

        public static void OnApplicationExit(object sender, EventArgs e)
        {
            Settings.SaveSettings();
            m_Presenter.Pages.GetModel().StoreSettings();
        }

        private void SetupRunMinimized()
        {
            if (Settings.Instance.RunMinimized)
            {
                WindowState = FormWindowState.Minimized;
                ShowInTaskbar = false;
                Visible = false;
            }
        }

        private void SetupForm()
        {
            // form pos at right
            var Rect = new Rectangle();
            Rect.Size = new Size(450, Screen.PrimaryScreen.WorkingArea.Height);
            Rect.Location = new Point(Screen.PrimaryScreen.WorkingArea.Left + (Screen.PrimaryScreen.WorkingArea.Width - Rect.Width),
                                      Screen.PrimaryScreen.WorkingArea.Top + (Screen.PrimaryScreen.WorkingArea.Height - Rect.Height));
            SetBounds(Rect.X, Rect.Y, Rect.Width, Rect.Height);
            // set mainform title
            var Ver = Assembly.GetExecutingAssembly().GetName().Version;
            Text = String.Format("{0} {1}.{2}", Application.ProductName, Ver.Major, Ver.Minor);
            // show tray
            TrayIcon.Visible = true;
            // show computer name
            lCompName.Text = SystemInformation.ComputerName;
            lCompName.ImageIndex = LanExchangeIcons.imgCompDefault;
                // show current user
            lUserName.Text = Settings.GetCurrentUserName();
        }

        private void SetupImages()
        {
            Pages.ImageList = LanExchangeIcons.SmallImageList;
            Status.ImageList = LanExchangeIcons.SmallImageList;
        }

        private void SetupMenu()
        {
            //ToolStripItem[] MyItems = new ToolStripItem[mComp.DropDownItems.Count];
            //for (int i = 0; i < MyItems.Length; i++)
            //{
            //    var TI = mComp.DropDownItems[i];
            //    if (TI is ToolStripSeparator)
            //        MyItems[i] = new ToolStripSeparator();
            //    else
            //        if (TI is ToolStripMenuItem)
            //            MyItems[i] = (ToolStripItem)MenuUtils.Clone(TI as ToolStripMenuItem);
            //}
            //popTop.Items.Clear();
            //popTop.Items.AddRange(MyItems);
        }

        public string StatusText
        {
            get { return lItemsCount.Text; }
            set { lItemsCount.Text = value; }
        }


        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            logger.Info("MainForm is closing with reason {0}", e.CloseReason.ToString());
            if (e.CloseReason == CloseReason.None || e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                IsFormVisible = false;
                logger.Info("Closing is canceled");
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                if (LastWindowState != WindowState)
                    logger.Info("WindowState is {0}", WindowState.ToString());
                LastWindowState = WindowState;
            }
            else
                logger.Info("WindowState is {0}", WindowState.ToString());
        }

        public bool bReActivate;

        public bool IsFormVisible
        {
            get { return WindowState != FormWindowState.Minimized && Visible; }
            set
            {
                bool bMinimized = WindowState == FormWindowState.Minimized;
                if (bMinimized)
                {
                    ShowInTaskbar = true;
                    WindowState = LastWindowState;
                }
                else
                {
                    if (bReActivate)
                    {
                        bReActivate = false;
                        ShowInTaskbar = true;
                        WindowState = FormWindowState.Minimized;
                        WindowState = LastWindowState;
                    }
                }
                Visible = value;
                if (Visible)
                {
                    Activate();
                    //this.ActiveControl = lvComps;
                    //lvComps.Focus();
                }
            }
        }

        private void popTray_Opening(object sender, CancelEventArgs e)
        {
            mOpen.Text = IsFormVisible ? "Скрыть" : "Открыть";
        }

        private void mOpen_Click(object sender, EventArgs e)
        {
            IsFormVisible = !IsFormVisible;
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                IsFormVisible = !IsFormVisible;
        }

#if DEBUG
        public static void Debug_ShowProperties(object obj)
        {
            var F = new Form();
            F.Text = obj.ToString();
            var Grid = new PropertyGrid();
            Grid.Dock = DockStyle.Fill;
            Grid.SelectedObject = obj;
            F.Controls.Add(Grid);
            F.Show();
        }

        public static void Debug_ShowSubscribers()
        {
            var S = new StringBuilder();
            foreach (var Pair in ServerListSubscription.Instance.GetSubjects())
                S.AppendLine(String.Format("{0} - {1}", Pair.Key, Pair.Value.Count));
            MessageBox.Show(S.ToString());
        }
#endif

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                m_Presenter.CancelCurrentFilter();
                e.Handled = true;
            }
#if DEBUG
            // Ctrl+Alt+C - show properties of current page in debug mode
            if (e.Control && e.Alt && e.KeyCode == Keys.C)
            {
                Debug_ShowProperties(Pages.SelectedTab);
                e.Handled = true;
            }
            // Ctrl+Alt+S - show subscibers in debug mode
            if (e.Control && e.Alt && e.KeyCode == Keys.S)
            {
                Debug_ShowSubscribers();
                e.Handled = true;
            }
#endif
        }

        public string GetMD5FromString(string str)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(str));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            return sBuilder.ToString();
        }

        public PanelView GetActivePanelView()
        {
            if (Pages.TabCount == 0 || Pages.SelectedTab == null)
                return null;
            else
            {
                Control.ControlCollection ctrls = Pages.SelectedTab.Controls;
                return ctrls.Count > 0 ? ctrls[0] as PanelView : null;
            }
        }

        private void popTop_Opened(object sender, EventArgs e)
        {
            //for (int i = 0; i < Math.Min(mComp.DropDownItems.Count, popTop.Items.Count); i++)
            //{
            //    ToolStripItem Src = mComp.DropDownItems[i];
            //    ToolStripItem Dest = popTop.Items[i];
            //    if (Src is ToolStripMenuItem && Dest is ToolStripMenuItem)
            //        (Dest as ToolStripMenuItem).ShowShortcutKeys = (Src as ToolStripMenuItem).ShowShortcutKeys;
            //}
        }

        private void popTop_Opening(object sender, CancelEventArgs e)
        {
            PanelView PV = GetActivePanelView();
            if (PV != null)
            {
                PV.popComps_Opening(sender, e);
                e.Cancel = !PV.mComp.Enabled;
            }
        }

        private void lCompName_Click(object sender, EventArgs e)
        {
            // Open MyComputer
            //Process.Start("explorer.exe", "/n, /e,::{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
            // Network
            //Process.Start("explorer.exe", "/n, ::{208D2C60-3AEA-1069-A2D7-08002B30309D},FERMAK");
            PanelView PV = GetActivePanelView();
            if (PV != null)
                PV.GotoFavoriteComp(SystemInformation.ComputerName);
        }

        private void tipComps_Popup(object sender, PopupEventArgs e)
        {
            if (e.AssociatedControl == pInfo.Picture)
            {
                (sender as ToolTip).ToolTipIcon = ToolTipIcon.Info;
                (sender as ToolTip).ToolTipTitle = "Легенда";
                return;
            }
            if (e.AssociatedControl is ListView)
            {
                ListView LV = (ListView)e.AssociatedControl;
                Point P = LV.PointToClient(Control.MousePosition);
                ListViewHitTestInfo Info = LV.HitTest(P);
                if (Info != null && Info.Item != null)
                    (sender as ToolTip).ToolTipTitle = Info.Item.Text;
                else
                    (sender as ToolTip).ToolTipTitle = "Информация";
                return;
            }
            (sender as ToolTip).ToolTipTitle = "";
        }

        private void Pages_Selected(object sender, TabControlEventArgs e)
        {
            if (Pages.TabCount > 0 && Pages.SelectedTab != null)
            {
                // synchronize selected page index with Pages model
                m_Presenter.Pages.GetModel().SelectedIndex = Pages.SelectedIndex;
                Control.ControlCollection ctrls = Pages.SelectedTab.Controls;
                if (ctrls.Count > 0)
                {
                    ActiveControl = Pages.SelectedTab.Controls[0];
                    ActiveControl.Focus();
                    // update top info panel
                    PV_FocusedItemChanged(ActiveControl, new EventArgs());
                    //lvComps_SelectedIndexChanged(ActiveControl, new EventArgs());
                    //UpdateFilterPanel();
                }
            }
        }

        private void mSettings_Click(object sender, EventArgs e)
        {
            if (ParamsForm.Instance != null)
                return;
            using (ParamsForm.Instance = new ParamsForm())
            {
                ParamsForm.Instance.ShowDialog();
            }
            ParamsForm.Instance = null;
        }

        private void mAbout_Click(object sender, EventArgs e)
        {
            if (AboutForm.Instance != null)
                return;
            using (AboutForm.Instance = new AboutForm())
            {
                AboutForm.Instance.ShowDialog();
            }
            AboutForm.Instance = null;
        }

        private void panel1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        void SetControlsVisible(bool bVisible)
        {
            foreach (Control control in Controls)
                control.Visible = bVisible;
        }

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        }
        
  
        private void mNewTab_Click(object sender, EventArgs e)
        {
            m_Presenter.Pages.NewTab();
        }

        private void mCloseTab_Click(object sender, EventArgs e)
        {
            m_Presenter.Pages.CloseTab();
        }

        private void mRenameTab_Click(object sender, EventArgs e)
        {
            m_Presenter.Pages.RenameTab();
        }

        private void popPages_Opened(object sender, EventArgs e)
        {
            mSelectTab.DropDownItems.Clear();
            m_Presenter.Pages.AddTabsToMenuItem(mSelectTab, m_Presenter.Pages.mSelectTab_Click, false);
            mCloseTab.Enabled = m_Presenter.Pages.CanCloseTab(Pages.SelectedIndex);
        }

        private bool m_AdminMode;

        public bool AdminMode
        {
            get { return m_AdminMode; }
            set
            {
                if (m_AdminMode != value)
                {
                    logger.Info("AdminMode is {0}", value ? "ON" : "OFF");
                    m_AdminMode = value;
                    //popComps_Opened(popComps, new EventArgs());
                }
            }
        }

        public void mExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void mTabParams_Click(object sender, EventArgs e)
        {
            using (TabParamsForm Form = new TabParamsForm())
            {
                TabControlModel M = m_Presenter.Pages.GetModel();
                PanelItemList Info = M.GetItem(Pages.SelectedIndex);
                Form.ScanMode = Info.ScanMode;
                Form.Groups = Info.Groups;
                if (Form.ShowDialog() == DialogResult.OK)
                {
                    Info.ScanMode = Form.ScanMode;
                    Info.Groups = Form.Groups;
                    Info.UpdateSubsctiption();
                }
            }
        }

        public void Restart()
        {
            Application.Restart();
        }

        private void lItemsCount_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Point P = Status.PointToScreen(new Point(0, 0));
                popTray.Show(P);
            }
        }

        /// <summary>
        /// This event fires when focused item of PanelView has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PV_FocusedItemChanged(object sender, EventArgs e)
        {
            // get focused item from current PanelView
            PanelItem PItem = (sender as PanelView).GetFocusedPanelItem(false, false);
            if (PItem == null)
                return;
            // is focused item a computer?
            ComputerPanelItem Comp = PItem as ComputerPanelItem;
            if (Comp == null)
                return;
            // update info panel at top of the form
            pInfo.InfoComp = Comp.Name;
            pInfo.InfoDesc = Comp.Comment;
            pInfo.InfoOS = Comp.SI.Version();
            pInfo.Picture.Image = LanExchangeIcons.ExtraLargeImageList.Images[PItem.ImageIndex];
            switch (Comp.ImageIndex)
            {
                case LanExchangeIcons.imgCompDefault:
                    tipComps.SetToolTip(pInfo.Picture, "Компьютер найден в результате обзора сети.");
                    break;
                case LanExchangeIcons.imgCompDisabled:
                    tipComps.SetToolTip(pInfo.Picture, "Компьютер не доступен посредством PING.");
                    break;
                /*
                case LanExchangeIcons.imgCompGreen:
                    tipComps.SetToolTip(pInfo.Picture, "Компьютер с запущенной программой LanExchange.");
                    break;
                 */
                default:
                    tipComps.SetToolTip(pInfo.Picture, "");
                    break;
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            SetupRunMinimized();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}