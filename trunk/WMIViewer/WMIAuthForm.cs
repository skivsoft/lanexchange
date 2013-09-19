﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WMIViewer
{
    public partial class WMIAuthForm : Form
    {
        private static string s_UserName;
        private static string s_UserPassword;
        
        public WMIAuthForm()
        {
            InitializeComponent();
        }

        private void WMIAuthForm_Load(object sender, EventArgs e)
        {

            picShield.Image = SystemIcons.Error.ToBitmap();
            bOK.NotifyDefault(true);
            ActiveControl = eUserName;
            UserName = s_UserName;
            UserPassword = s_UserPassword;
        }

        public bool AutoLogon()
        {
            if (!String.IsNullOrEmpty(s_UserName))
            {
                eUserName.Text = s_UserName;
                ePassword.Text = s_UserPassword;
                return true;
            }
            return false;
        }

        [Localizable(false)]
        public void SetComputerName(string computerName)
        {
            Text = String.Format(Text, computerName);
            string userName;
            if (AutoLogon())
                userName = s_UserName;
            else
                userName = string.Format(@"{0}\{1}", Environment.UserDomainName, Environment.UserName);
            lMessage.Text = String.Format(lMessage.Text, userName);
        }

        public string UserName
        {
            get { return eUserName.Text; }
            set { eUserName.Text = value; }
        }

        public string UserPassword
        {
            get { return ePassword.Text; }
            set { ePassword.Text = value; }
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(eUserName.Text.Trim()))
            {
                Error.SetError(eUserName, "User name not specified.");
                DialogResult = DialogResult.None;
                return;
            }
            s_UserName = UserName;
            s_UserPassword = UserPassword;
        }

        public static void ClearSavedPassword()
        {
            s_UserName = string.Empty;
            s_UserPassword = string.Empty;
        }

        private void WMIAuthForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                e.Handled = true;
            }
        }
    }
}
