using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField]
    LayerMask groundLayer;

    public static PlayerInput Singleton;
    public Vector3 MouseHitPosition;
    //public BoidsState State;

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
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            Vector2 screenPos = Input.mousePosition;
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(screenPos);

            if (plane.Raycast(ray, out float enter) == true)
            {
                MouseHitPosition = ray.GetPoint(enter);
            }
        }
    }
}
