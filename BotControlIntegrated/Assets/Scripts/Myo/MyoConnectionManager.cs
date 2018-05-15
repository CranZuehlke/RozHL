using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if WINDOWS_UWP
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
#endif

namespace Zuehlke.HoloLens
{
    public class MyoConnectionManager : MonoBehaviour
    {
        public MyoConnection Myo1;
        public MyoConnection Myo2;

#if WINDOWS_UWP

        private BluetoothLEAdvertisementWatcher _bleAdWatcher;

        private DeviceWatcher _bleDeviceWatcher;

        private bool _scanningForMyo = false;

        private void Start()
        {
            StartScanningForMyo();
        }

        public void StartScanningForMyo()
        {
            if (!_scanningForMyo)
            {
                Debug.Log("Starting scan for Myo");
                _scanningForMyo = true;
                _bleAdWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
                _bleAdWatcher.Received += OnBleAdvertisementReceived;
                _bleAdWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Clear();
                _bleAdWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(MyoConnection.ControlServiceUUID);
                _bleAdWatcher.Start();

                string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };
                _bleDeviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true), requestedProperties, DeviceInformationKind.AssociationEndpoint);
                _bleDeviceWatcher.Added += OnDeviceAdded;
                _bleDeviceWatcher.Updated += OnDeviceUpdated;
                _bleDeviceWatcher.Removed += OnDeviceRemoved;
                _bleDeviceWatcher.Start();
            }
        }

        public void StopScanningForMyo()
        {
            if (_scanningForMyo)
            {
                Debug.Log("Stopping scan for Myo");
                _bleAdWatcher.Stop();
                _bleDeviceWatcher.Stop();
                _scanningForMyo = false;
            }
        }

        private async void OnBleAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs advertisement)
        {
            if (_scanningForMyo)
            {
                Debug.Log("Advertisement received: " + advertisement.Advertisement.LocalName);
                var myoBle = await BluetoothLEDevice.FromBluetoothAddressAsync(advertisement.BluetoothAddress);
                var myoConnection = FindUnusedMyoConnection();
                if (myoConnection)
                {
                    var connected = await myoConnection.ConnectToMyoBle(myoBle);
                    if (connected && FindUnusedMyoConnection() == null)
                    {
                        StopScanningForMyo();
                    }
                }
            }
        }

        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            if (_scanningForMyo)
            {
                var myoConnection = FindUnusedMyoConnection();
                if (myoConnection)
                {
                    var connected = await myoConnection.ConnectKnownDevice(deviceInfo);
                    if (connected && FindUnusedMyoConnection() == null)
                    {
                        StopScanningForMyo();
                    }
                }
            }
        }

        private MyoConnection FindUnusedMyoConnection()
        {
            if (Myo1 != null && !Myo1.ConnectingOrConnected)
            {
                return Myo1;
            }
            if (Myo2 != null && !Myo2.ConnectingOrConnected)
            {
                return Myo2;
            }
            return null;
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }
#endif
    }
}