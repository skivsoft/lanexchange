﻿using System;
using System.Windows.Forms;
using LanExchange.View;
using LanExchange.Model;
using LanExchange.Presenter;
using NLog;
using System.Drawing;
using System.Reflection;
using System.ComponentModel;
using System.Text;
using System.Security.Cryptography;
using LanExchange.Windows;

namespace LanExchange.UI
{
    public partial class MainForm : RunMinimizedForm, IMainView
    {
        /// <summary>
        /// Logger object.
        /// </summary>
        private readonly static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Default width of MainForm.
        /// </summary>
        private const int MAINFORM_DEFAULTWIDTH = 450;
        /// <summary>
        /// Presenter for MainForm.
        /// </summary>
        private static MainPresenter m_Presenter;
        /// <summary>
        /// ManiForm single instance.
        /// </summary>
        public static MainForm Instance;

        public MainForm()
        {
            InitializeComponent();
            Instance = this;
            // load settings from cfg-file
            Settings.LoadSettings();
            RunMinimized = Settings.Instance.RunMinimized;
            SetupForm();
            // init MainForm presenter
            m_Presenter = new MainPresenter(this);
            m_Presenter.Pages = Pages.GetPresenter();
            m_Presenter.Pages.PanelViewFocusedItemChanged += Pages_PanelViewFocusedItemChanged;
            m_Presenter.Pages.GetModel().LoadSettings();
            // here we call event for update items count in statusline
            //Pages.UpdateSelectedTab();
            //mSendToNewTab.Click += new EventHandler(TabController.mSendToNewTab_Click);
            
            // setup images
            MainForm.Instance.tipComps.SetToolTip(Pages.Pages, " ");
            Pages.Pages.ImageList = LanExchangeIcons.SmallImageList;
            Status.ImageList = LanExchangeIcons.SmallImageList;
            // init network scanner
            ServerListSubscription.Instance.RefreshInterval = (int)Settings.Instance.RefreshTimeInSec * 1000;
        }

        private void SetupForm()
        {
            // set mainform bounds
            var Rect = Settings.Instance.GetBounds();
            SetBounds(Rect.Left, Rect.Top, Rect.Width, Rect.Height);
            // set mainform title
            var Ver = Assembly.GetExecutingAssembly().GetName().Version;
            Text = String.Format("{0} {1}.{2}", Application.ProductName, Ver.Major, Ver.Minor);
            // show tray
            TrayIcon.Text = Text;
            TrayIcon.Visible = true;
            // show computer name
            lCompName.Text = SystemInformation.ComputerName;
            lCompName.ImageIndex = LanExchangeIcons.imgCompDefault;
            // show current user
            lUserName.Text = Settings.GetCurrentUserName();
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
                PanelView PV = Pages.GetActivePanelView();
                if (PV != null && PV.Filter.Visible)
                    PV.Filter.FilterText = "";
                else
                    MainForm.Instance.IsFormVisible = false;
                e.Handled = true;
            }
#if DEBUG
            // Ctrl+R - restart application
            if (e.Control && e.KeyCode == Keys.R)
            {
                Restart();
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

        public static string GetMD5FromString(string str)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(System.Text.ASCIIEncoding.ASCII.GetBytes(str));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
                sBuilder.Append(data[i].ToString("x2"));
            return sBuilder.ToString();
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
            PanelView PV = Pages.GetActivePanelView();
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
            //PanelView PV = Pages.GetActivePanelView();
            //if (PV != null)
            //    PV.GotoFavoriteComp(SystemInformation.ComputerName);
        }

        private void tipComps_Popup(object sender, PopupEventArgs e)
        {
            //logger.Info("tipComps_Popup: {0}", e.AssociatedControl.GetType().Name);
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
            if (e.AssociatedControl is TabControl && e.AssociatedControl == Pages.Pages)
            {
                Point P = e.AssociatedControl.PointToClient(Control.MousePosition);
                TabPage Tab = Pages.GetTabPageByPoint(P);
                if (Tab != null)
                    (sender as ToolTip).ToolTipTitle = Tab.Text;
                else
                    e.Cancel = true;
                return;
            }
            (sender as ToolTip).ToolTipTitle = "";
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

        private void panel1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
        }
        
        public void Restart()
        {
            Application.Restart();
        }

        private void lItemsCount_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Point P = Status.PointToScreen(e.Location);
                popTray.Show(P);
            }
        }

        /// <summary>
        /// This event fires when focused item of PanelView has been changed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Pages_PanelViewFocusedItemChanged(object sender, EventArgs e)
        {
            // get focused item from current PanelView
            PanelView PV = sender as PanelView;
            ComputerPanelItem Comp = null;
            if (PV != null) 
            {
                PanelItem PItem = PV.GetPresenter().GetFocusedPanelItem(false, false);
                if (PItem != null)
                    Comp = PItem as ComputerPanelItem;
            }
            // is focused item a computer?
            if (Comp == null)
            {
                pInfo.Picture.Image = LanExchangeIcons.LargeImageList.Images[LanExchangeIcons.imgCompDefault];
                pInfo.InfoComp = "";
                pInfo.InfoDesc = "";
                pInfo.InfoOS = "";
                return;
            }
            // update info panel at top of the form
            pInfo.InfoComp = Comp.Name;
            pInfo.InfoDesc = Comp.Comment;
            pInfo.InfoOS = Comp.SI.Version();
            pInfo.Picture.Image = LanExchangeIcons.LargeImageList.Images[Comp.ImageIndex];
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

        public void ShowStatusText(string format, params object[] args)
        {
            lItemsCount.Text = String.Format(format, args);
        }

        private void popTray_Opening(object sender, CancelEventArgs e)
        {
            mOpen.Text = IsFormVisible ? "Скрыть" : "Открыть";
        }

        //private void MainForm_Shown(object sender, EventArgs e)
        //{
        //    m_RunMinimized.Form_Shown();
        //}

        //private void MainForm_Resize(object sender, EventArgs e)
        //{
        //    m_RunMinimized.Form_Resize();
        //    logger.Info("MainForm_Resize: {0}", Width);
        //}

        //private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        //{
        //    logger.Info("MainForm is closing with reason {0}", e.CloseReason.ToString());
        //    if (e.CloseReason == CloseReason.None || e.CloseReason == CloseReason.UserClosing)
        //    {
        //        e.Cancel = true;
        //        IsFormVisible = false;
        //        logger.Info("Closing is canceled");
        //    }
        //}

        private void mOpen_Click(object sender, EventArgs e)
        {
            IsFormVisible = !IsFormVisible;
        }

        private void TrayIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                IsFormVisible = !IsFormVisible;
        }

        public void mExit_Click(object sender, EventArgs e)
        {
            ApplicationExit();
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_QUERYENDSESSION:
                    logger.Info("WM_QUERYENDSESSION: {0}", m.LParam.ToString("X"));
                    m.Result = new IntPtr(1);
                    break;
                case NativeMethods.WM_ENDSESSION:
                    logger.Info("WM_ENDSESSION: {0}", m.LParam.ToString("X"));
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            Settings.Instance.SetBounds(Bounds);
            Settings.SaveSettingsIfModified();
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            Pages.FocusPanelView();
        }


    }
}