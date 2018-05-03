using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.VR.WSA.Input;
using Zuehlke.HoloLens;

namespace Zuehlke.HoloLens
{
    public class MyoSync : MonoBehaviour
    {
        public GameObject DragHint;

        public GameObject TargetPrefab;
        private GameObject _target;

        private readonly Vector3 ArmOffsetRightHanded = new Vector3(0.25f, -0.5f, 0.05f);
        public Transform ArmOrientationTransform;

        private bool _connected;
        private Vector3 _myoVelocityGlobal;
        private Transform _gravityPoint;

        private Vector3[] _accelerationHistory = new Vector3[128];
        private int _accelerationHistoryIndex = 0;
        private int _accelerationHistoryCount = 0;
        private Vector3 _averageAcceleration;
        private Vector3 _currentAcceleration;

        private bool _synchronizing;
        private Vector3 _myoPositionGlobal;


        void Start()
        {
            MyoConnection.Instance.OnMyoConnected += OnMyoConnected;
            var gravityPoint = new GameObject("GravityPoint");
            gravityPoint.transform.SetParent(transform, false);
            _gravityPoint = gravityPoint.transform;

            InitGestureRecognizer();
        }

        private void InitGestureRecognizer()
        {
        }

        public void ResyncMyo()
        {
            if (_connected && !_synchronizing)
            {
                StartMyoSync();
            }
        }

        private void OnMyoConnected()
        {
            _connected = true;
            MyoConnection.Instance.OnPoseChanged += OnHandPoseChanged;
            _target = Instantiate(TargetPrefab);
            StartMyoSync();
        }

        private void OnHandPoseChanged(MyoConnection.Pose newpose)
        {
            if (_synchronizing && newpose == MyoConnection.Pose.Fist)
            {
                PerformMyoSync();
            }
        }

        private void StartMyoSync()
        {
            _synchronizing = true;
            DragHint.transform.position = CameraCache.Main.transform.position + CameraCache.Main.transform.forward * 2;
            DragHint.SetActive(true);
            ResetTarget();
        }

        private void ResetTarget()
        {
            _target.SetActive(true);
            Guide.Instance.SetTarget(_target);
            var targetDirection = CameraCache.Main.transform.forward;
            targetDirection.y = 0;
            targetDirection.Normalize();
            targetDirection *= 3;
            _target.transform.position = CameraCache.Main.transform.position + targetDirection;
            _target.GetComponent<MeshRenderer>().material.color = Color.white;
        }

        private void PerformMyoSync()
        {
            var myoEstimateX = MyoConnection.Instance.MyoArm == MyoConnection.Arm.Left ? -0.2f : 0.2f;
            var myoPositionEstimate = CameraCache.Main.transform.TransformPoint(myoEstimateX, -0.4f, 0.2f);
            var myoToTarget = _target.transform.position - myoPositionEstimate;
            myoToTarget.y = 0;

            var armDirection = transform.forward;
            armDirection.y = 0;

            var rotationAngleY = Vector3.SignedAngle(armDirection, myoToTarget, Vector3.up);
            ArmOrientationTransform.rotation = Quaternion.Euler(0, rotationAngleY, 0);

            _target.GetComponent<MeshRenderer>().material.color = Color.green;

            Invoke("HideDraggables", 1f);

            _synchronizing = false;
        }

        private void HideDraggables()
        {
            _target.SetActive(false);
            Guide.Instance.SetTarget(null);
        }

        void Update ()
        {
            UpdateMyoArmPosition();
            if (_connected)
            {
//                UpdateAccelerometer();
            }
        }

        private void UpdateMyoArmPosition()
        {
            var forward = CameraCache.Main.transform.forward;
            forward.y = 0;
            forward.Normalize();
            var right = Vector3.Cross(Vector3.up, forward);
            var armOffset =
                right * ArmOffsetRightHanded.x * ((MyoConnection.Instance.MyoArm == MyoConnection.Arm.Left) ? -1 : 1)
                + Vector3.up * ArmOffsetRightHanded.y
                + forward * ArmOffsetRightHanded.z;
            ArmOrientationTransform.position = CameraCache.Main.transform.position + armOffset;
        }

        #region Accelerometer Handling

        private void UpdateAccelerometer()
        {
            var myoAcceleration = MyoConnection.Instance.Accelerometer;

            if (myoAcceleration.sqrMagnitude >= 0.0001f)
            {
//                    Debug.Log("Myo acceleration: " + myoAcceleration + "; magnitude: " + myoAcceleration.magnitude);
                var down = transform.InverseTransformVector(Vector3.down);
//                    Debug.Log("Down: " + down);
                myoAcceleration += down;
                UpdateAverageAcceleration(myoAcceleration);
                myoAcceleration -= _averageAcceleration;
                myoAcceleration *= 9.81f;
//                Debug.Log("Without gravity: (" + myoAcceleration.x + ", " + myoAcceleration.y + ", " + myoAcceleration.z +
//                          "); magnitude: " + myoAcceleration.magnitude);
                _gravityPoint.localPosition = myoAcceleration;
                var accelerationGlobal = _gravityPoint.position - transform.position;
                _currentAcceleration = accelerationGlobal;
                _myoVelocityGlobal += accelerationGlobal * Time.deltaTime;
                if (_myoVelocityGlobal.sqrMagnitude < 0.01f)
                {
                    _myoVelocityGlobal *= 0.9f;
                }
                _myoPositionGlobal += _myoVelocityGlobal * Time.deltaTime;
            }
        }

        private void UpdateAverageAcceleration(Vector3 myoAcceleration)
        {
//            if (_accHistoryCount == _accelerationHistory.Length &&
//                (myoAcceleration - _averageAcceleration).sqrMagnitude > 0.0001)
//            {
//                return;
//            }
            _accelerationHistory[_accelerationHistoryIndex] = myoAcceleration;
            _accelerationHistoryIndex++;
            if (_accelerationHistoryIndex == _accelerationHistory.Length)
            {
                _accelerationHistoryIndex = 0;
            }
            _accelerationHistoryCount = Mathf.Min(_accelerationHistoryCount + 1, _accelerationHistory.Length);
            var average = Vector3.zero;
            for (var i = 0; i < _accelerationHistoryCount; i++)
            {
                average += _accelerationHistory[i];
            }
            average /= _accelerationHistoryCount;
            _averageAcceleration = average;
        }

#endregion
    }
}