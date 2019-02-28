﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Edison.Mobile.Common.WiFi
{
    public interface IWifiService
    {
        Task<WifiNetwork> GetCurrentlyConnectedWifiNetwork();
        Task<bool> ConnectToWifiNetwork(string ssid);
        Task<bool> ConnectToWifiNetwork(string ssid, string passphrase);
        Task DisconnectFromWifiNetwork(WifiNetwork wifiNetwork);
    }
}
