﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Edison.Mobile.Common.WiFi;
using System.Linq;

namespace Edison.Mobile.Android.Common.WiFi
{
    public class PlatformWifiService : IWifiService
    {


        public Task<bool> ConnectToWifiNetwork(string ssid, string passphrase)
        {
            WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);

            WifiConfiguration wifiConfig = new WifiConfiguration();
            wifiConfig.Ssid = string.Format("\"{0}\"", ssid);
            wifiConfig.PreSharedKey = string.Format("\"{0}\"", passphrase);

            // Use ID
            var existing = wifiManager.ConfiguredNetworks.FirstOrDefault(i => i.Ssid == ssid);
            int netId;
            if (existing == null)
            {
                netId = wifiManager.AddNetwork(wifiConfig);
            }
            else
            {
                netId = existing.NetworkId;
            }

            wifiManager.Disconnect();
            return Task.FromResult(wifiManager.EnableNetwork(netId, true));
        }
        
        public Task<bool> ConnectToWifiNetwork(string ssid)
        {
            WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);

            // Use ID
            var existing = wifiManager.ConfiguredNetworks.FirstOrDefault(i => i.Ssid == ssid);
            int netId = existing.NetworkId;
            
            wifiManager.Disconnect();
            return Task.FromResult(wifiManager.EnableNetwork(netId, true));
        }

        public Task DisconnectFromWifiNetwork(WifiNetwork wifiNetwork)
        {
            WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);
            return Task.FromResult(wifiManager.Disconnect());
        }

        public Task<WifiNetwork> GetCurrentlyConnectedWifiNetwork()
        {
            WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);

            var network = new WifiNetwork()
            {
                SSID = wifiManager.ConnectionInfo.SSID
            };
            return Task.FromResult(network);
        }
    }
}
