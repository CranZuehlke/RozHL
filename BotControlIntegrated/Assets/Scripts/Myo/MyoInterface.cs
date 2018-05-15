using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zuehlke.HoloLens;
using Vector3 = UnityEngine.Vector3;

namespace Zuehlke.HoloLens
{
    [RequireComponent(typeof(ThalmicMyo), typeof(MyoConnection))]
    public class MyoInterface : MonoBehaviour
    {
        private ThalmicMyo _thalmicMyo;
        private MyoConnection _holoLensMyo;

        // True if and only if Myo has detected that it is on an arm.
        public bool ArmSynced
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.Synced; }
#else            
            get { return _thalmicMyo.armSynced; }
#endif
        }

        public bool Unlocked
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.Unlocked; }
#else            
            get { return _thalmicMyo.unlocked; }
#endif
        }

        // The current arm that Myo is being worn on. An arm of Unknown means that Myo is unable to detect the arm
        // (e.g. because it's not currently being worn).
        public MyoConnection.Arm Arm
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.MyoArm; }
#else            
            get
            {
                switch (_thalmicMyo.arm)
                {
                    case Thalmic.Myo.Arm.Left:
                        return MyoConnection.Arm.Left;
                    case Thalmic.Myo.Arm.Right:
                        return MyoConnection.Arm.Right;
                    default:
                        return MyoConnection.Arm.Unknown;
                }
            }
#endif
        }

        // The current direction of Myo's +x axis relative to the user's arm. A xDirection of Unknown means that Myo is
        // unable to detect the direction (e.g. because it's not currently being worn).
        public MyoConnection.XDirection XDirection
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.XAxisDirection; }
#else            
            get
            {
                switch (_thalmicMyo.xDirection)
                {
                    case Thalmic.Myo.XDirection.TowardElbow:
                        return MyoConnection.XDirection.TowardElbow;
                    case Thalmic.Myo.XDirection.TowardWrist:
                        return MyoConnection.XDirection.TowardWrist;
                    default:
                        return MyoConnection.XDirection.Unknown;
                }
            }
#endif
        }

        // The current pose detected by Myo. A pose of Unknown means that Myo is unable to detect the pose (e.g. because
        // it's not currently being worn).
        public MyoConnection.Pose Pose
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.HandPose; }
#else            
            get {
                switch (_thalmicMyo.pose)
                {
                    case Thalmic.Myo.Pose.Unknown:
                        return MyoConnection.Pose.Unknown;
                    case Thalmic.Myo.Pose.DoubleTap:
                        return MyoConnection.Pose.DoubleTap;
                    case Thalmic.Myo.Pose.FingersSpread:
                        return MyoConnection.Pose.FingersSpread;
                    case Thalmic.Myo.Pose.Fist:
                        return MyoConnection.Pose.Fist;
                    case Thalmic.Myo.Pose.WaveIn:
                        return MyoConnection.Pose.WaveIn;
                    case Thalmic.Myo.Pose.WaveOut:
                        return MyoConnection.Pose.WaveOut;
                    default:
                        return MyoConnection.Pose.Rest;
                }
            }
#endif
        }

        // Myo's current accelerometer reading, representing the acceleration due to force on the Myo armband in units of
        // g (roughly 9.8 m/s^2) and following Unity coordinate system conventions.
        public Vector3 Accelerometer
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.Accelerometer; }
#else            
            get { return _thalmicMyo.accelerometer; }
#endif
        }

        // Myo's current gyroscope reading, representing the angular velocity about each of Myo's axes in degrees/second
        // following Unity coordinate system conventions.
        public Vector3 Gyroscope
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.RotationRate; }
#else            
            get { return _thalmicMyo.gyroscope; }
#endif
        }

        // True if and only if this Myo armband has paired successfully, at which point it will provide data and a
        // connection with it will be maintained when possible.
        public bool Connected
        {
#if WINDOWS_UWP
            get { return _holoLensMyo.Connected; }
#else            
            get { return _thalmicMyo.isPaired; }
#endif
        }

        private readonly Dictionary<MyoConnection.VibrationDuration, Thalmic.Myo.VibrationType> _thalmicVibrationTypes =
            new Dictionary<MyoConnection.VibrationDuration, Thalmic.Myo.VibrationType>
            {
                {MyoConnection.VibrationDuration.None, Thalmic.Myo.VibrationType.Short},
                {MyoConnection.VibrationDuration.Short, Thalmic.Myo.VibrationType.Short},
                {MyoConnection.VibrationDuration.Medium, Thalmic.Myo.VibrationType.Medium},
                {MyoConnection.VibrationDuration.Long, Thalmic.Myo.VibrationType.Long}
            };

        public void Vibrate(MyoConnection.VibrationDuration type)
        {
#if WINDOWS_UWP
            _holoLensMyo.Vibrate(type);
#else
            _thalmicMyo.Vibrate(_thalmicVibrationTypes[type]);
#endif
        }

        // Cause the Myo to unlock with the provided type of unlock. e.g. UnlockType.Timed or UnlockType.Hold.
        public void Unlock(MyoConnection.UnlockType type)
        {
#if WINDOWS_UWP
            _holoLensMyo.Unlock(type);
#else
            if (type == MyoConnection.UnlockType.Lock)
            {
                Debug.LogError("Locking Myo is not supported");
            }
            else
            {
                _thalmicMyo.Unlock(type == MyoConnection.UnlockType.Hold ? Thalmic.Myo.UnlockType.Hold : Thalmic.Myo.UnlockType.Timed);
            }
#endif
        }

        // Cause the Myo to re-lock immediately.
        public void Lock()
        {
#if WINDOWS_UWP
            _holoLensMyo.Lock();
#else
            _thalmicMyo.Lock();
#endif
        }

        /// Notify the Myo that a user action was recognized.
        public void NotifyUserAction()
        {
#if WINDOWS_UWP
            _holoLensMyo.NotifyUserAction();
#else
            _thalmicMyo.NotifyUserAction();
#endif
        }

        void Start ()
        {
            _thalmicMyo = GetComponent<ThalmicMyo>();
            _holoLensMyo = GetComponent<MyoConnection>();
        }
    }
}