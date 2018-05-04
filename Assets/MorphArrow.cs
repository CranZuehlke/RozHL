using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MorphArrow : MonoBehaviour
{

    private SkinnedMeshRenderer _meshRenderer;

    public float morphSpeed = 2.0f;

    void Start()
    {
        _meshRenderer = GetComponent<SkinnedMeshRenderer>();
    }

    private void StartMorphing()
    {
        float morph = 100f * (Mathf.Sin(Time.time * morphSpeed) * 0.5f + 0.5f);

        _meshRenderer.SetBlendShapeWeight(0, morph);
    }

    // Update is called once per frame
    void Update()
    {
        StartMorphing();
    }
}
