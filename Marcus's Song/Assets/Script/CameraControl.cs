using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform target;
    public float distance = 15f;
    public float rotationSpeed = 5f;

    private Vector3 lastMousePos;

    void Start()
    {
        if (target == null) target = new GameObject("CubeCenter").transform;

        transform.position = target.position + new Vector3(-distance, distance, -distance);
        transform.LookAt(target);
    }

    void Update()
    {
        if (Input.GetMouseButton(2))
        {
            Vector3 delta = Input.mousePosition - lastMousePos;
            float rotX = delta.x * rotationSpeed * Time.deltaTime;
            float rotY = -delta.y * rotationSpeed * Time.deltaTime;

            transform.RotateAround(target.position, Vector3.up, rotX);
            transform.RotateAround(target.position, transform.right, rotY);
        }

        lastMousePos = Input.mousePosition;
    }
}
