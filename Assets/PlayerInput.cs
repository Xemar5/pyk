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
            Vector3 mouseWorldPosition = cam.ScreenToWorldPoint(screenPos);

            RaycastHit hit;

            Physics.Raycast(mouseWorldPosition, cam.transform.forward, out hit, 100, groundLayer);
            
            if(hit.collider == null)
            {
                IsPositionHit = false;
            }
            else
            {
                MouseHitPosition = hit.point;
                IsPositionHit = true;
            }
            
        }
        else
        {
            IsPositionHit = false;
        }
    }
}
