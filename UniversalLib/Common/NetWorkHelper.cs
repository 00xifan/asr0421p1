// Copyright (c) 2025-present Lenovo.  All rights reserverd
// Confidential and Restricted
using System;
using System.Net.NetworkInformation;

namespace UniversalLib.Common
{
    public enum NetWorkMode
    {
        /// <summary>
        /// No network
        /// </summary>
        UnAvailable,
        /// <summary>
        /// have network
        /// </summary>
        Available
    }
    public class NetWorkHelper
    {

        public event Action<NetWorkMode> OnNetworkStatusChanged;

        private static readonly Lazy<NetWorkHelper> _instanceLock = new Lazy<NetWorkHelper>(() => new NetWorkHelper());

        public static NetWorkHelper Instance
        {
            get
            {
                return _instanceLock.Value;
            }
        }

        public NetWorkHelper()
        {
            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
        }

        public NetWorkMode GetNetWorkStatus()
        {
            return NetTypeChange(NetworkInterface.GetIsNetworkAvailable());
        }

        private NetWorkMode NetTypeChange(bool isAvailable)
        {
            if (isAvailable)
                return NetWorkMode.Available;
            else
                return NetWorkMode.UnAvailable;
        }

        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (e == null)
                return;

            OnNetworkStatusChanged?.Invoke(NetTypeChange(e.IsAvailable));
        }
    }
}
