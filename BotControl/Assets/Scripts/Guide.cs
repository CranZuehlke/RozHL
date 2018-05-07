using UnityEngine;
using System.Collections;
using HoloToolkit.Sharing.Utilities;
using HoloToolkit.Unity;

namespace Zuehlke.HoloLens
{
    public class Guide : MonoBehaviour
    {
        public static Guide Instance { get; private set; }

        public GameObject Target;
        public GameObject Indicator;
        public float IndicatorDistance = 2;
        public float Speed = 2;

        private Camera _camera;
        private BoxCollider _targetCollider;
        private MeshRenderer _targetRenderer;
        private Material[] _indicatorMaterials;
        private Renderer[] _ownRenderers;

        private bool _covering;

        private Vector3 TargetWorldCenter
        {
            get
            {
                if (_targetCollider != null)
                {
                    return Target.transform.TransformPoint(_targetCollider.center);
                }
                if (_targetRenderer != null)
                {
                    return _targetRenderer.bounds.center;
                }
                return Vector3.zero;
            }
        }

        public Guide()
        {
            Instance = this;
        }

        // Use this for initialization
        void Start () {
            _camera = Camera.main;
            _ownRenderers = gameObject.GetComponentsInChildren<Renderer>();
            _indicatorMaterials = new Material[_ownRenderers.Length];
            for (var i = 0; i < _ownRenderers.Length; i++)
            {
                _indicatorMaterials[i] = _ownRenderers[i].material;
            }
            OnUpdateTarget();
        }

        // Update is called once per frame
        void Update () {
            if (Target == null)
            {
                return;
            }

            if (!IsInFrustum())
            {
                var shouldInterpolate = _ownRenderers[0].enabled;
                for (var i = 0; i < _ownRenderers.Length; i++)
                {
                    _ownRenderers[i].enabled = true;
                }
            
                var cameraTransformation = _camera.transform;
                var vectorInZ = new Vector3(0, 0, IndicatorDistance);
                var rotatedVector = vectorInZ.RotateAround(Vector3.zero, cameraTransformation.rotation);
            
                var frontOfCameraPosition = cameraTransformation.position + rotatedVector;
                var correctedPosition = shouldInterpolate
                    ? Vector3.Lerp(gameObject.transform.position, frontOfCameraPosition, Time.deltaTime * Speed)
                    : frontOfCameraPosition;
                gameObject.transform.position = frontOfCameraPosition;

                // Project the vector "from in front of user to the target" on the camera plane, 
                // then calculate the angle from "camera up" to the projected vector
                Vector3 gazePositionToTargetPosition = TargetWorldCenter - gameObject.transform.position;
                var cameraToGuideArrow = (gameObject.transform.position - _camera.transform.position).normalized;
                Vector3 guidanceDirectionProjected = Vector3.ProjectOnPlane(gazePositionToTargetPosition, cameraToGuideArrow);
                float angle = Vector3.Angle(_camera.transform.up, guidanceDirectionProjected);

                // When the target is behind the user, prevent the arrow from pointing up or down
                var angleToTarget = Vector3.Angle(cameraTransformation.forward,
                    TargetWorldCenter - cameraTransformation.position);
                if (angleToTarget > 90)
                {
                    var maximumDeviationFromHorizon = 180f - angleToTarget;
                    var angleFromHorizon = angle - 90;
                    angle = 90 + (angleFromHorizon * maximumDeviationFromHorizon / 90f);
                }

                // Angle is clamped to [0..180], so flip angle depending on whether the arrow is left or right of the target
                Vector3 cameraLocalTargetDirection = _camera.transform.InverseTransformVector(TargetWorldCenter - _camera.transform.position);
                Vector3 cameraLocalGuideDirection = _camera.transform.InverseTransformVector(cameraToGuideArrow);
                if (cameraLocalTargetDirection.x - cameraLocalGuideDirection.x > 0)
                {
                    angle *= -1;
                }
                Quaternion localRotation = Quaternion.Euler(0, 0, angle);


                // Apply the rotation to the guidance arrow
                gameObject.transform.position = correctedPosition;
                Indicator.transform.localRotation = shouldInterpolate
                    ? Quaternion.Slerp(Indicator.transform.localRotation, localRotation, Time.deltaTime*Speed)
                    : localRotation;
            }
            else
            {
                for (var i = 0; i < _ownRenderers.Length; i++)
                {
                    _ownRenderers[i].enabled = false;
                }
                gameObject.transform.position = TargetWorldCenter;
            }

        }

        bool IsInFrustum()
        {
            if (_targetCollider != null)
            {
                var bounds = _targetCollider.enabled ? _targetCollider.bounds : new Bounds(Target.transform.TransformPoint(_targetCollider.center), Target.transform.TransformVector(_targetCollider.size));
                var frustum = GeometryUtility.CalculateFrustumPlanes(_camera);
                return GeometryUtility.TestPlanesAABB(frustum, bounds);
            }
            if (_targetRenderer != null)
            {
                var frustum = GeometryUtility.CalculateFrustumPlanes(_camera);
                return GeometryUtility.TestPlanesAABB(frustum, _targetRenderer.bounds);
            }
            return true;
        }

        public void SetTarget(GameObject target)
        {
            Target = target;
            OnUpdateTarget();
        }

        private void OnUpdateTarget()
        {
            _targetCollider = Target != null ? Target.GetComponent<BoxCollider>() : null;
            _targetRenderer = Target != null ? Target.GetComponent<MeshRenderer>() : null;
            if (Target != null)
            {
                gameObject.transform.position = TargetWorldCenter;
            }
            else
            {
                if (_ownRenderers != null)
                {
                    for (var i = 0; i < _ownRenderers.Length; i++)
                    {
                        _ownRenderers[i].enabled = false;
                    }
                }
            }
        }

        void OnCoveringObject(float centerDistanceRelative)
        {
            if (centerDistanceRelative < 1)
            {
                _covering = true;
                for (var i = 0; i < _indicatorMaterials.Length; i++)
                {
                    var material = _indicatorMaterials[i];
                    var col = material.color;
                    col.a = centerDistanceRelative;
                    material.color = col;
                }
            }
            else if (_covering)
            {
                _covering = false;
                for (var i = 0; i < _indicatorMaterials.Length; i++)
                {
                    var material = _indicatorMaterials[i];
                    var col = material.color;
                    col.a = 1;
                    material.color = col;
                }
            }
        }
    }
}