﻿using System;
using System.ComponentModel;
using System.Text;

//using System.Xml.Serialization;

namespace LanExchange.Plugin.Network
{
    [Serializable]
    public class ServerInfo
    {
        private string m_Name;
        private string m_Comment;
        private readonly OSVersion m_Version;

        private DateTime m_UtcUpdated;

        /// <summary>
        /// Constructor without params is required for XML-serialization.
        /// </summary>
        //public ServerInfo()
        //{
        //}

        public static ServerInfo FromNetApi32(NativeMethods.SERVER_INFO_101 info)
        {
            var result = new ServerInfo();
            result.m_Name = info.sv101_name;
            result.m_Version.PlatformID = info.sv101_platform_id;
            result.m_Version.Major = info.sv101_version_major;
            result.m_Version.Minor = info.sv101_version_minor;
            result.m_Version.Type = info.sv101_type;
            result.m_Comment = info.sv101_comment;
            return result;
        }

        public ServerInfo()
        {
            m_Version = new OSVersion();
        }

        public string Name
        {
            get { return m_Name; }
            set { m_Name = value; }
        }

        public string Comment
        {
            get { return m_Comment; }
            set { m_Comment = value; }
        }

        public OSVersion Version
        {
            get { return m_Version; }
        }

        public DateTime UtcUpdated
        {
            get { return m_UtcUpdated; }
            set { m_UtcUpdated = value; }
        }

        public void ResetUtcUpdated()
        {
            m_UtcUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// This method is virtual for unit-tests only.
        /// </summary>
        /// <returns></returns>
        public virtual TimeSpan GetTopicality()
        {
            return DateTime.UtcNow - UtcUpdated;
        }

        [Localizable(false)]
        public string GetTopicalityText()
        {
            TimeSpan diff = GetTopicality();
            var sb = new StringBuilder();
            bool showSeconds = true;
            if (diff.Days > 0)
            {
                sb.Append(diff.Days);
                sb.Append("d");
                showSeconds = false;
            }
            if (diff.Hours > 0)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(diff.Hours);
                sb.Append("h");
                showSeconds = false;
            }
            if (diff.Minutes > 0)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(diff.Minutes);
                sb.Append("m");
            }
            if (showSeconds && diff.Seconds > 0)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(diff.Seconds);
                sb.Append("s");
            }
            return sb.ToString();
        }
    }
}
