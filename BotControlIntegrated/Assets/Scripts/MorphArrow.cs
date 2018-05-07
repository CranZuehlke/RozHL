using System.Collections;
using System.Collections.Generic;
using HoloToolkit.Unity;
using UnityEngine;

public class MorphArrow : MonoBehaviour
{

    private SkinnedMeshRenderer _meshRenderer;

    public float morphSpeed = 2.0f;

    public bool ArrowState;

    private bool _hover = false;

    private float _morphProgress = 0;

    private float MorphProgress
    {
        get { return _morphProgress; }
        set
        {
            _morphProgress = Mathf.Clamp(value, 0, 100);
            _hover = _morphProgress <= 0.001;
        }
    }

    void Start()
    {
        _meshRenderer = GetComponent<SkinnedMeshRenderer>();
    }

    private void Morph()
    {
        MorphProgress += ArrowState ? -morphSpeed : morphSpeed;
        _meshRenderer.SetBlendShapeWeight(0, _morphProgress);
    }

    // Update is called once per frame
    void Update()
    {
        Morph();
        Hover();
    }

    private void Hover()
    {
        if (_hover)
        {
            var target = new Vector3(0, 0.1f * Mathf.Sin(Time.time * 5f), 0);
            transform.localPosition = Vector3.Lerp(transform.localPosition, target, Time.deltaTime * 10f);

            transform.Rotate(Vector3.right, Time.deltaTime * 100f);

        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, Time.deltaTime);
        }
    }
}
