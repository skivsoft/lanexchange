﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LanExchange.Utils
{
    static class NetApi32Utils
    {
        /// <summary>
        /// Get domain name for specified machine.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static string GetMachineNetBiosDomain(string server)
        {
            IntPtr pBuffer;

            int retval = NetApi32.NetWkstaGetInfo(server, 100, out pBuffer);
            if (retval != 0)
                throw new Win32Exception(retval);

            NetApi32.WKSTA_INFO_100 info = (NetApi32.WKSTA_INFO_100)Marshal.PtrToStructure(pBuffer, typeof(NetApi32.WKSTA_INFO_100));
            string domainName = info.wki100_langroup;
            NetApi32.NetApiBufferFree(pBuffer);
            return domainName;
        }

        /// <summary>
        /// Get list of serves of specified domain and specified types.
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public static NetApi32.SERVER_INFO_101[] NetServerEnumArray(string domain, NetApi32.SV_101_TYPES types)
        {
            NetApi32.SERVER_INFO_101[] Result = new NetApi32.SERVER_INFO_101[0];
            IntPtr pInfo;
            int entriesread = 0;
            int totalentries = 0;
            NetApi32.NERR err = NetApi32.NetServerEnum(null, 101, out pInfo, -1, ref entriesread, ref totalentries, types, domain, 0);
            try
            {
                if ((err == NetApi32.NERR.NERR_SUCCESS || err == NetApi32.NERR.ERROR_MORE_DATA) && pInfo != IntPtr.Zero)
                {
                    int ptr = pInfo.ToInt32();
                    Result = new NetApi32.SERVER_INFO_101[entriesread];
                    for (int i = 0; i < entriesread; i++)
                    {
                        Result[i] = (NetApi32.SERVER_INFO_101)Marshal.PtrToStructure(new IntPtr(ptr), typeof(NetApi32.SERVER_INFO_101));
                        ptr += Marshal.SizeOf(typeof(NetApi32.SERVER_INFO_101));
                    }
                }
            }
            finally
            {
                if (pInfo != IntPtr.Zero)
                    NetApi32.NetApiBufferFree(pInfo);
            }
            return Result;
        }

        /// <summary>
        /// Get domain list.
        /// </summary>
        /// <returns></returns>
        public static NetApi32.SERVER_INFO_101[] GetDomainsArray()
        {
            return NetServerEnumArray(null, NetApi32.SV_101_TYPES.SV_TYPE_DOMAIN_ENUM);
        }

        /// <summary>
        /// Get computer list of specified domain.
        /// </summary>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static NetApi32.SERVER_INFO_101[] GetComputersOfDomainArray(string domain)
        {
            return NetServerEnumArray(domain, NetApi32.SV_101_TYPES.SV_TYPE_ALL);
        }

        // TODO: uncomment EnumNetShares
        //public static List<PanelItem> EnumNetShares(string Server)
        //{
        //    List<PanelItem> Result = new List<PanelItem>();
        //    Result.Add(new SharePanelItem("", "", 0, Server));
        //    int entriesread = 0;
        //    int totalentries = 0;
        //    int resume_handle = 0;
        //    int nStructSize = Marshal.SizeOf(typeof(NetApi32.SHARE_INFO_1));
        //    IntPtr bufPtr = IntPtr.Zero;
        //    StringBuilder server = new StringBuilder(Server);
        //    logger.Info("WINAPI NetShareEnum");
        //    int ret = NetApi32.NetShareEnum(server, 1, ref bufPtr, NetApi32.MAX_PREFERRED_LENGTH, ref entriesread, ref totalentries, ref resume_handle);
        //    if (ret == NetApi32.NERR_Success)
        //    {
        //        logger.Info("WINAPI NetServerEnum result: entriesread={0}, totalentries={1}", entriesread, totalentries);
        //        IntPtr currentPtr = bufPtr;
        //        for (int i = 0; i < entriesread; i++)
        //        {
        //            NetApi32.SHARE_INFO_1 shi1 = (NetApi32.SHARE_INFO_1)Marshal.PtrToStructure(currentPtr, typeof(NetApi32.SHARE_INFO_1));
        //            if ((shi1.shi1_type & (uint)NetApi32.SHARE_TYPE.STYPE_IPC) != (uint)NetApi32.SHARE_TYPE.STYPE_IPC)
        //                Result.Add(new SharePanelItem(shi1.shi1_netname, shi1.shi1_remark, shi1.shi1_type, Server));
        //            else
        //                logger.Info("Skiping IPC$ share");
        //            currentPtr = new IntPtr(currentPtr.ToInt32() + nStructSize);
        //        }
        //        NetApi32.NetApiBufferFree(bufPtr);
        //    }
        //    else
        //    {
        //        logger.Info("WINAPI NetServerEnum error: {0}", ret);
        //    }

    }
}