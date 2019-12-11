using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiteAnimationController : MonoBehaviour
{
    [SerializeField, Range(0, 1)]
    float progress;

    Material mat;
    // Start is called before the first frame update
    void Awake()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    private void OnValidate()
    {
        if (mat!=null)
            mat.SetFloat("Vector1_B9E0B5DE", progress);
    }
}
