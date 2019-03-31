﻿using System;
using System.Threading.Tasks;
using Edison.Core.Common.Models;
using Edison.Mobile.Admin.Client.Core.Models;
using Edison.Mobile.Admin.Client.Core.Network;
using Edison.Mobile.Admin.Client.Core.Services;
using Edison.Mobile.Admin.Client.Core.Shared;
using Edison.Mobile.Common.Shared;
using Edison.Mobile.Common.WiFi;
using Edison.Mobile.Admin.Client.Core.Ioc;

namespace Edison.Mobile.Admin.Client.Core.ViewModels
{
    public class RegisterDeviceViewModel : DeviceSetupBaseViewModel
    {
        readonly IDeviceRestService deviceRestService;

        public event ViewNotification OnBeginDevicePairing;
        public event EventHandler<OnFinishDevicePairingEventArgs> OnFinishDevicePairing;
        public event EventHandler<string> OnPairingStatusTextChanged;

        public class OnFinishDevicePairingEventArgs : EventArgs
        {
            public bool IsSuccess { get; set; }
        }

        public string MockDeviceId => "BA27EB910E94";

        public RegisterDeviceViewModel(
            DeviceSetupService deviceSetupService,
            IDeviceRestService deviceRestService,
            IWifiService wifiService,
            IOnboardingRestService onboardingRestService,
            DeviceProvisioningRestService deviceProvisioningRestService
        ) : base(deviceSetupService, deviceProvisioningRestService, onboardingRestService, wifiService)
        {
            this.deviceRestService = deviceRestService;
            this.wifiService.ConnectionFailed += WifiService_ConnectionFailed;
        }

        private void WifiService_ConnectionFailed(object sender, Common.WiFi.ConnectionFailedEventArgs e)
        {
            OnFinishDevicePairing?.Invoke(this, new OnFinishDevicePairingEventArgs() { IsSuccess = false });
        }
        
        async Task<bool> ProvisionDeviceFail()
        {
            await wifiService.DisconnectFromWifiNetwork(deviceSetupService.CurrentDeviceHotspotNetwork);

            OnFinishDevicePairing?.Invoke(this, new OnFinishDevicePairingEventArgs
            {
                IsSuccess = false,
            });

            return false;
        }

        public async Task<DeviceModel> GetDevice(Guid deviceId)
        {
            return await deviceRestService.GetDevice(deviceId);
        }

        public async Task<bool> ProvisionDevice(WifiNetwork wifiNetwork)
        {
            OnBeginDevicePairing?.Invoke();
            this.deviceSetupService.CurrentDeviceModel.SSID = wifiNetwork.SSID;
            // connect to device
            SetPairingStatusText("Connecting to device...");

            var defaultWifiNetwork = await wifiService.GetCurrentlyConnectedWifiNetwork();
            await wifiService.DisconnectFromWifiNetwork(defaultWifiNetwork);
            
            var success = await wifiService.ConnectToWifiNetwork(wifiNetwork.SSID, deviceSetupService.DefaultPassword);

            if (!success) return await ProvisionDeviceFail();
                       

            // get stuff from device
            SetPairingStatusText("Grabbing some information from the device...");

            deviceSetupService.CurrentDeviceHotspotNetwork = success ? wifiNetwork : null;
            deviceSetupService.OriginalSSID = defaultWifiNetwork.SSID;
              
            onboardingRestService.SetBasicAuthentication(deviceSetupService.DefaultPortalPassword); 

            var deviceIdResponse = await onboardingRestService.GetDeviceId();
            
            if (deviceIdResponse == null) return await ProvisionDeviceFail();
            
            deviceSetupService.CurrentDeviceModel.DeviceId = deviceIdResponse.DeviceId;

            //return deviceIdResponse?.DeviceId; // shortcut for testing

            var csrResult = await onboardingRestService.GetGeneratedCSR();

            //await wifiService.DisconnectFromWifiNetwork(wifiNetwork);
            var connected = await wifiService.ConnectToWifiNetwork(deviceSetupService.OriginalSSID);

            if (csrResult == null || !connected) return await ProvisionDeviceFail();

            // provision device with azure
            SetPairingStatusText("Provisioning device with the mothership...");

            var certificateResponse = await deviceProvisioningRestService.GenerateDeviceCertificate(new DeviceCertificateRequestModel
            {
                Csr = csrResult?.Csr ?? "MIIBbjCB2AIBADAvMS0wKwYDVQQDEyQ4OTJlYWM5YS1iOWFkLTQ0NDgtYWEwYS0wOTI0MDE1YWMwMWEwgZ8wDQYJKoZIhvcNAQEBBQADgY0AMIGJAoGBALeqOH+XoeXXERg8neKzr3IumxTDMKsPzKjZ/kfE1gu/FHmr1ugPuRTtQzP5WFVD5lWqtEKJyX+YDCjNevKeHBSpHTAAdVR8GbpDdvRvij0k6yrmrjTRVohO5bTaE611KNzXOW5K4Y8PhoTHasNnMEydfAh4ysut92lWObmg2CG1AgMBAAGgADANBgkqhkiG9w0BAQsFAAOBgQCg8dbM4gMxChp4MF67B/0ARv5Ezq3423v/Tkj5KOMxFql+NeYtM9JpIWABMw2xlARl+agp9e8eaj503grhHjYeGV0afC2/8AA2o/PyZOrS80QViDK6Z4cY+zUO5hp3darGCEH14fuAHKwrokSQxYReqdBELyT3r4ZnCdbi+NUx7A==",
                DeviceType = deviceSetupService.DeviceTypeAsString,
            });            
            
            if (certificateResponse == null) return await ProvisionDeviceFail();

            var generateKeysResponse = await deviceProvisioningRestService.GenerateDeviceKeys(deviceSetupService.CurrentDeviceModel.DeviceId, wifiNetwork.SSID);

            if (generateKeysResponse == null) return await ProvisionDeviceFail();

            await Task.Delay(2000);

            SetPairingStatusText("Reconnecting to device and finishing up! Sit tight...");


            //await wifiService.DisconnectFromWifiNetwork(defaultWifiNetwork);
            // reconnect to device to set device type
            var reconnectSuccess = await wifiService.ConnectToWifiNetwork(deviceSetupService.CurrentDeviceHotspotNetwork.SSID, deviceSetupService.DefaultPassword);
                       
            if (!reconnectSuccess) return await ProvisionDeviceFail();

            var provisionSuccess = await onboardingRestService.ProvisionDevice(new RequestCommandProvisionDevice
            {
                DeviceCertificateInformation = new DeviceCertificateModel
                {
                    DeviceType = certificateResponse.DeviceType,
                    Certificate = certificateResponse.Certificate,
                    DpsIdScope = certificateResponse.DpsIdScope,
                    DpsInstance = certificateResponse.DpsInstance,
                },
            });

            SetPairingStatusText("Updating secrets on the device! Sit tight...");

            if (provisionSuccess == null || !provisionSuccess.IsSuccess) return await ProvisionDeviceFail();
            SetPairingStatusText(generateKeysResponse.SSIDPassword);

            var setDeviceKeysResponse = await onboardingRestService.SetDeviceSecretKeys(new RequestCommandSetDeviceSecretKeys
            {
                AccessPointPassword = generateKeysResponse.SSIDPassword,
                EncryptionKey = generateKeysResponse.EncryptionKey,
                PortalPassword = generateKeysResponse.PortalPassword,
            });
            
            if (setDeviceKeysResponse == null || !setDeviceKeysResponse.IsSuccess) return await ProvisionDeviceFail();

            deviceSetupService.PortalPassword = generateKeysResponse.PortalPassword;
            deviceSetupService.WiFiPassword = generateKeysResponse.SSIDPassword;

            onboardingRestService.SetBasicAuthentication(deviceSetupService.PortalPassword);

            SetPairingStatusText("Setting the Device Type");
            var setDeviceTypeResult = await onboardingRestService.SetDeviceType(new RequestCommandSetDeviceType
            {
                DeviceType = certificateResponse.DeviceType,
            });

            if (setDeviceTypeResult == null || !setDeviceTypeResult.IsSuccess) return await ProvisionDeviceFail(); 

            OnFinishDevicePairing?.Invoke(this, new OnFinishDevicePairingEventArgs
            {
                IsSuccess = setDeviceTypeResult.IsSuccess,
            });

            SetPairingStatusText("Pairing Successful!");

            deviceSetupService.CurrentDeviceModel.DeviceType = certificateResponse.DeviceType;

            // stay connected to device to get available wifi networks on the next screen....

            return true;
        }

        void SetPairingStatusText(string statusText)
        {
            OnPairingStatusTextChanged?.Invoke(this, statusText);
        }
    }
}
