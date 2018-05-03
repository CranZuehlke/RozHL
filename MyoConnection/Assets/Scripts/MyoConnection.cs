using System;
using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
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
using UnityEngine;
using UnityEngine.UI;

namespace Zuehlke.HoloLens
{
    public class MyoConnection : Singleton<MyoConnection>
    {
        // For the BLE service spec, see https://github.com/thalmiclabs/myo-bluetooth/blob/master/myohw.h

        #region Myo BLE UUIDs

        public static readonly Guid ControlServiceUUID = new Guid("D5060001-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid MyoInfoCharacteristicUUID = new Guid("D5060101-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid FirmwareVersionCharacteristicUUID = new Guid("D5060201-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid CommandCharacteristicUUID = new Guid("D5060401-A904-DEB9-4748-2C7F4A124842");

        public static readonly Guid ImuDataServiceUUID = new Guid("D5060002-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid ImuDataCharacteristicUUID = new Guid("D5060402-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid MotionEventCharacteristicUUID = new Guid("D5060502-A904-DEB9-4748-2C7F4A124842");

        public static readonly Guid ClassifierServiceUUID = new Guid("D5060003-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid ClassifierEventCharacteristicUUID = new Guid("D5060103-A904-DEB9-4748-2C7F4A124842");

        public static readonly Guid RawEmgDataServiceUUID = new Guid("D5060005-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid EmgData0CharacteristicUUID = new Guid("D5060105-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid EmgData1CharacteristicUUID = new Guid("D5060205-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid EmgData2CharacteristicUUID = new Guid("D5060305-A904-DEB9-4748-2C7F4A124842");
        public static readonly Guid EmgData3CharacteristicUUID = new Guid("D5060405-A904-DEB9-4748-2C7F4A124842");

        #endregion

        #region Enum definitions

        private enum Command : byte
        {
            SetMode = 0x01,
            Vibrate = 0x03,
            DeepSleep = 0x04,
            Vibrate2 = 0x07,
            SetSleepMode = 0x09,
            Unlock = 0x0a,
            UserAction = 0x0b
        }

        public enum VibrationDuration : byte
        {
            None = 0x00,
            Short = 0x01,
            Medium = 0x02,
            Long = 0x03
        }

        private enum UnlockType : byte
        {
            Lock = 0x00,
            Timed = 0x01,
            Hold = 0x02
        }

        private enum EmgMode : byte
        {
            None = 0x00,
            SendEmg = 0x02,
            SendEmgRaw = 0x03
        }

        private enum ImuMode : byte
        {
            None = 0x00,
            SendData = 0x01,
            SendEvents = 0x02,
            SendAll = 0x03,
            SendRaw = 0x04
        }

        private enum ClassifierMode : byte
        {
            Disabled = 0x00,
            Enabled = 0x01
        }

        public enum Pose : ushort
        {
            Rest = 0x0000,
            Fist = 0x0001,
            WaveIn = 0x0002,
            WaveOut = 0x0003,
            FingersSpread = 0x0004,
            DoubleTap = 0x0005,
            Unknown = 0xffff
        }

        public enum XDirection : byte
        {
            TowardWrist = 0x01,
            TowardElbow = 0x02,
            Unknown = 0xff
        }

        public enum Arm : byte
        {
            Right = 0x01,
            Left = 0x02,
            Unknown = 0xff
        }

        private enum ClassifierEventType : byte
        {
            ArmSynced = 0x01,
            ArmUnsynced = 0x02,
            Pose = 0x03,
            Unlocked = 0x04,
            Locked = 0x05,
            SyncFailed = 0x06
        }

        #endregion

        private object _lock = new object();
        private Queue<Action> _mainThreadQueue = new Queue<Action>();

        #region Events

        public event Action OnMyoConnected;

        public delegate void PoseChanged(Pose newPose);
        public event PoseChanged OnPoseChanged;

        #endregion

#if WINDOWS_UWP

        private BluetoothLEAdvertisementWatcher _bleAdWatcher;

        private DeviceWatcher _bleDeviceWatcher;

        private bool _scanningForMyo = false;

        private BluetoothLEDevice _myoBle;

        #region Myo BLE Services & Characteristics

        private GattDeviceService _controlService;
        private GattCharacteristic _myoInfoCharacteristic;
        private GattCharacteristic _firmwareVersionCharacteristic;
        private GattCharacteristic _commandCharacteristic;

        private GattDeviceService _imuDataService;
        private GattCharacteristic _imuDataCharacteristic;
        private GattCharacteristic _motionEventCharacteristic;

        private GattDeviceService _classifierService;
        private GattCharacteristic _classifierEventCharacteristic;

        private GattDeviceService _emgDataService;
        private GattCharacteristic _emgData0Characteristic;
        private GattCharacteristic _emgData1Characteristic;
        private GattCharacteristic _emgData2Characteristic;
        private GattCharacteristic _emgData3Characteristic;

        #endregion

#endif

        private Quaternion _myoOrientation;

        private XDirection _myoXAxisDirection = XDirection.Unknown;
        public XDirection XAxisDirection { get; private set; }

        private Arm _myoArm = Arm.Unknown;
        public Arm MyoArm { get; private set; }

        // Accelerometer data on 3 axes in g
        private Vector3 _myoAccelerometer;
        public Vector3 Accelerometer { get; private set; }

        // Rotation around axes in degrees per second
        private Vector3 _myoGyroscope;
        public Vector3 RotationRate { get; private set; }

        private Pose _myoHandPose = Pose.Unknown;
        public Pose HandPose { get; private set; }

        private Pose _lastHandPose = Pose.Unknown;

#if WINDOWS_UWP

        private void Start()
        {
            StartScanningForMyo();
        }

        public void StartScanningForMyo ()
        {
            if (!_scanningForMyo)
            {
                _scanningForMyo = true;
                _bleAdWatcher = new BluetoothLEAdvertisementWatcher { ScanningMode = BluetoothLEScanningMode.Active };
                _bleAdWatcher.Received += OnBleAdvertisementReceived;
                _bleAdWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Clear();
                _bleAdWatcher.AdvertisementFilter.Advertisement.ServiceUuids.Add(ControlServiceUUID);
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
                _bleAdWatcher.Stop();
                _bleDeviceWatcher.Stop();
                _scanningForMyo = false;
            }
        }

        private async void OnBleAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs advertisement)
        {
            if (_scanningForMyo)
            {
                StopScanningForMyo();
                Debug.Log("Advertisement received: " + advertisement.Advertisement.LocalName);
                _myoBle = await BluetoothLEDevice.FromBluetoothAddressAsync(advertisement.BluetoothAddress);
                await ConnectToMyo();
            }
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            if (_scanningForMyo)
            {
                ConnectKnownDevice(deviceInfo);
            }
        }

        private async void ConnectKnownDevice(DeviceInformation deviceInfo)
        {
            var bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            var services = await bluetoothLeDevice.GetGattServicesAsync();
            if (services.Services.Single(s => s.Uuid == ControlServiceUUID) != null)
            {
                Debug.Log("Found known Myo");
                StopScanningForMyo();
                _myoBle = bluetoothLeDevice;
                await ConnectToMyo();
            }
        }

        private async Task ConnectToMyo()
        {
            var deviceId = _myoBle.DeviceId;
            Debug.Log("Myo has ID " + deviceId);
            try
            {
                await DiscoverServices();
                Debug.Log("Services discovered");
                await DiscoverCharacteristics();
                Debug.Log("Characteristics discovered");
                await RegisterForNotifications();
                Debug.Log("Notifications registered. Ready to go!");
                EnqueueOnMainThread(() => { OnMyoConnected?.Invoke(); });
                EnableSensorUpdates();
                VibrateInternal(VibrationDuration.Medium);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }

        #region Discovery

        private async Task DiscoverServices()
        {
            var gatt = await _myoBle.GetGattServicesAsync();
            if (gatt.Services.Count >= 1)
            {
                foreach (var service in gatt.Services)
                {
                    if (service.Uuid == ControlServiceUUID)
                    {
                        _controlService = service;
                    }
                    else if (service.Uuid == ImuDataServiceUUID)
                    {
                        _imuDataService = service;
                    }
                    else if (service.Uuid == ClassifierServiceUUID)
                    {
                        _classifierService = service;
                    }
                    else if (service.Uuid == RawEmgDataServiceUUID)
                    {
                        _emgDataService = service;
                    }
                }
            }
        }

        private async Task DiscoverCharacteristics()
        {
            await DiscoverControlCharacteristics();
            await DiscoverImuDataCharacteristics();
            await DiscoverClassifierCharacteristics();
            await DiscoverEmgDataCharacteristics();
        }

        private async Task DiscoverControlCharacteristics()
        {
            if (_controlService != null)
            {
                var characteristicsResult = await _controlService.GetCharacteristicsAsync();
                foreach (var characteristic in characteristicsResult.Characteristics)
                {
                    if (characteristic.Uuid == MyoInfoCharacteristicUUID)
                    {
                        _myoInfoCharacteristic = characteristic;
                    }
                    else if (characteristic.Uuid == FirmwareVersionCharacteristicUUID)
                    {
                        _firmwareVersionCharacteristic = characteristic;
                    }
                    else if (characteristic.Uuid == CommandCharacteristicUUID)
                    {
                        _commandCharacteristic = characteristic;
                    }
                }
            }
            else
            {
                Debug.LogError("Control Service is missing!");
            }
        }

        private async Task DiscoverImuDataCharacteristics()
        {
            if (_imuDataService != null)
            {
                var characteristicsResult = await _imuDataService.GetCharacteristicsAsync();
                foreach (var characteristic in characteristicsResult.Characteristics)
                {
                    if (characteristic.Uuid == ImuDataCharacteristicUUID)
                    {
                        _imuDataCharacteristic = characteristic;
                    }
                    else if (characteristic.Uuid == MotionEventCharacteristicUUID)
                    {
                        _motionEventCharacteristic = characteristic;
                    }
                }
            }
            else
            {
                Debug.LogError("IMU Data Service is missing!");
            }
        }

        private async Task DiscoverClassifierCharacteristics()
        {
            if (_classifierService != null)
            {
                var characteristicsResult = await _classifierService.GetCharacteristicsAsync();
                foreach (var characteristic in characteristicsResult.Characteristics)
                {
                    if (characteristic.Uuid == ClassifierEventCharacteristicUUID)
                    {
                        _classifierEventCharacteristic = characteristic;
                    }
                }
            }
            else
            {
                Debug.LogError("Classifier Service is missing!");
            }
        }

        private async Task DiscoverEmgDataCharacteristics()
        {
            if (_emgDataService != null)
            {
                var characteristicsResult = await _emgDataService.GetCharacteristicsAsync();
                foreach (var characteristic in characteristicsResult.Characteristics)
                {
                    if (characteristic.Uuid == EmgData0CharacteristicUUID)
                    {
                        _emgData0Characteristic = characteristic;
                    }
                    else if (characteristic.Uuid == EmgData1CharacteristicUUID)
                    {
                        _emgData1Characteristic = characteristic;
                    }
                    else if (characteristic.Uuid == EmgData2CharacteristicUUID)
                    {
                        _emgData2Characteristic = characteristic;
                    }
                    else if (characteristic.Uuid == EmgData3CharacteristicUUID)
                    {
                        _emgData3Characteristic = characteristic;
                    }
                }
            }
            else
            {
                Debug.LogError("EMG Data Service is missing!");
            }
        }

        #endregion

        private async Task RegisterForNotifications()
        {
            await RegisterForImuUpdates();
            await RegisterForEventUpdates();
        }

        #region IMU Updates

        private async Task RegisterForImuUpdates()
        {
            var status =
                await _imuDataCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Indicate);
            if (status == GattCommunicationStatus.Success)
            {
                _imuDataCharacteristic.ValueChanged += OnImuUpdate;
            }
            else
            {
                Debug.LogError("Unable to register for IMU updates");
            }
        }

        private void OnImuUpdate(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var imuBytes = args.CharacteristicValue.ToArray();
            var byteIndex = 0;
            byteIndex = ParseOrientation(imuBytes, byteIndex);
            byteIndex = ParseAccelerometer(imuBytes, byteIndex);
            ParseGyroscope(imuBytes, byteIndex);
        }

        private int ParseOrientation(byte[] imuBytes, int byteIndex)
        {
            const float orientationScale = 16384.0f;
            var w = BitConverter.ToInt16(imuBytes, byteIndex) / orientationScale;
            byteIndex += 2;
            var x = BitConverter.ToInt16(imuBytes, byteIndex) / orientationScale;
            byteIndex += 2;
            var y = BitConverter.ToInt16(imuBytes, byteIndex) / orientationScale;
            byteIndex += 2;
            var z = BitConverter.ToInt16(imuBytes, byteIndex) / orientationScale;
            byteIndex += 2;
            lock (_lock)
            {
                _myoOrientation = new Quaternion(y, z, -x, -w);
            }
            return byteIndex;
        }

        private int ParseAccelerometer(byte[] imuBytes, int byteIndex)
        {
            const float accelerometerScale = 2048.0f;
            var x = BitConverter.ToInt16(imuBytes, byteIndex) / accelerometerScale;
            byteIndex += 2;
            var y = BitConverter.ToInt16(imuBytes, byteIndex) / accelerometerScale;
            byteIndex += 2;
            var z = BitConverter.ToInt16(imuBytes, byteIndex) / accelerometerScale;
            byteIndex += 2;
            lock (_lock)
            {
                _myoAccelerometer = new Vector3(y, z, -x);
            }
            return byteIndex;
        }

        private void ParseGyroscope(byte[] imuBytes, int byteIndex)
        {
            const float gyroscopeScale = 16.0f;
            var x = BitConverter.ToInt16(imuBytes, byteIndex) / gyroscopeScale;
            byteIndex += 2;
            var y = BitConverter.ToInt16(imuBytes, byteIndex) / gyroscopeScale;
            byteIndex += 2;
            var z = BitConverter.ToInt16(imuBytes, byteIndex) / gyroscopeScale;
            lock (_lock)
            {
                _myoGyroscope = new Vector3(y, z, -x);
            }
        }

        #endregion

        #region Hand Pose Updates

        private async Task RegisterForEventUpdates()
        {
            var status =
                await _classifierEventCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Indicate);
            if (status == GattCommunicationStatus.Success)
            {
                _classifierEventCharacteristic.ValueChanged += OnClassifierEventUpdate;
            }
            else
            {
                Debug.LogError("Unable to register for hand pose updates");
            }
        }

        private void OnClassifierEventUpdate(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var eventBytes = args.CharacteristicValue.ToArray();

            var eventType = eventBytes[0];
            if (eventType == (byte) ClassifierEventType.Pose)
            {
                lock (_lock)
                {
                    _myoHandPose = (Pose) BitConverter.ToInt16(eventBytes, 1);
                    Debug.Log("Got hand pose: " + _myoHandPose);
                }
            }
            else if (eventType == (byte) ClassifierEventType.ArmSynced)
            {
                lock (_lock)
                {
                    _myoArm = (Arm) eventBytes[1];
                    _myoXAxisDirection = (XDirection) eventBytes[2];
                    Debug.Log("Got X axis orientation: " + _myoXAxisDirection);
                }
            }
            else if (eventType == (byte) ClassifierEventType.Unlocked)
            {
                Debug.Log("Myo unlocked");
            }
        }

        #endregion

        private async void ReadSomething()
        {
            var myoInfoResult = await _firmwareVersionCharacteristic.ReadValueAsync();
            if (myoInfoResult.Status == GattCommunicationStatus.Success)
            {
                var myoInfoBytes = myoInfoResult.Value.ToArray();
                var myoInfoHex = BitConverter.ToString(myoInfoBytes).Replace("-", "");
                Debug.Log("--- Got Myo Info: " + myoInfoHex);
            }
            else
            {
                Debug.Log("--- Read unsuccessful");
            }
        }

        void Update ()
        {
            lock (_lock)
            {
                transform.localRotation = _myoOrientation;
                XAxisDirection = _myoXAxisDirection;
                MyoArm = _myoArm;
                Accelerometer = _myoAccelerometer;
                RotationRate = _myoGyroscope;
                HandPose = _myoHandPose;

                if (_myoHandPose != _lastHandPose)
                {
                    _lastHandPose = _myoHandPose;
                    var handler = OnPoseChanged;
                    handler?.Invoke(_myoHandPose);
                }
            }

            var action = DequeueMainThreadAction();
            while (action != null)
            {
                action.Invoke();
                action = DequeueMainThreadAction();
            }
        }

        #region Commands

        private void Unlock()
        {
            if (_myoBle != null)
            {
                _commandCharacteristic?.WriteValueAsync(new byte[]
                {
                    (byte) Command.Unlock,
                    1,
                    (byte) UnlockType.Hold
                }.AsBuffer());
            }
        }

        private void EnableSensorUpdates()
        {
            if (_myoBle != null)
            {
                _commandCharacteristic?.WriteValueAsync(new byte[]
                {
                    (byte) Command.SetMode,
                    3,
                    (byte) EmgMode.SendEmg,
                    (byte) ImuMode.SendAll,
                    (byte) ClassifierMode.Enabled
                }.AsBuffer());
            }
        }

        private void VibrateInternal(VibrationDuration duration)
        {
            if (_myoBle != null)
            {
                _commandCharacteristic?.WriteValueAsync(new byte[]
                {
                    (byte) Command.Vibrate,
                    1,
                    (byte) duration
                }.AsBuffer());
            }
        }

        #endregion

        #region Utility

        private void EnqueueOnMainThread(Action action)
        {
            // TODO: Enqueue data instead of actions. Filter for data that overrides each other
            lock (_mainThreadQueue)
            {
                _mainThreadQueue.Enqueue(action);
            }
        }

        private Action DequeueMainThreadAction()
        {
            lock (_mainThreadQueue)
            {
                if (_mainThreadQueue.Count > 0)
                {
                    return _mainThreadQueue.Dequeue();
                }
                return null;
            }
        }

        #endregion
#endif


        public void Vibrate(VibrationDuration duration = VibrationDuration.Medium)
        {
#if WINDOWS_UWP
            VibrateInternal(duration);
#endif
        }
    }
}