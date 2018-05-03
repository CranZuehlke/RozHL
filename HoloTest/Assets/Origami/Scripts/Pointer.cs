using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR.WSA.Input;

public class Pointer : MonoBehaviour
{
    public GameObject Cursor;
    public static Pointer Instance { get; private set; }

    GestureRecognizer recognizer;

    void Awake()
    {
        Instance = this;

        // Set up a GestureRecognizer to detect Select gestures.
        recognizer = new GestureRecognizer();
        recognizer.TappedEvent += OnTappedEvent;
        recognizer.StartCapturingGestures();
    }

    private void OnTappedEvent(InteractionSourceKind source, int tapCount, Ray headRay)
    {
        var headPosition = Camera.main.transform.position;
        var gazeDirection = Camera.main.transform.forward;

        RaycastHit hitInfo;
        if (Physics.Raycast(headPosition, gazeDirection, out hitInfo, 30.0f, SpatialMapping.PhysicsRaycastMask))
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            sphere.transform.position = Cursor.transform.position;

            Object.Destroy(sphere, 2);
        }
    }
}