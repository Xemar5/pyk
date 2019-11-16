using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField]
    LayerMask groundLayer;

    public static PlayerInput Singleton;
    public Vector3 MouseHitPosition;
    public bool IsPositionHit;

    Camera cam;
    private void Awake()
    {
        if(Singleton == null)
        {
            Singleton = this;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 screenPos = Input.mousePosition;
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (plane.Raycast(ray, out float enter) == true)
            {
                IsPositionHit = true;
                MouseHitPosition = ray.GetPoint(enter);
            }
            else
            {
                IsPositionHit = false;
            }
        }
        else
        {
            IsPositionHit = false;
        }
    }
}
