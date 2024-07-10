using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CameraSystem : MonoBehaviour
{
    
    [SerializeField] private bool useWASD = false;
    [SerializeField] private bool useEdgeScroll = false;
    [SerializeField] private bool useDragPan = false;

    private bool dragPanActive;
    private Vector2 lastMousePosition;

    public static event Action<bool> DragPanTriggered;

    private void Update()
    {
        if (useWASD) MovementWASD();
        if (useEdgeScroll) EdgScrolling();
        if (useDragPan) DragPan();
    }

    private void MovementWASD()
    {
        Vector2 inputDir = new Vector2(0, 0);

        if (Input.GetKey(KeyCode.W)) inputDir.y = +1f;
        if (Input.GetKey(KeyCode.A)) inputDir.x = -1f;
        if (Input.GetKey(KeyCode.S)) inputDir.y = -1f;
        if (Input.GetKey(KeyCode.D)) inputDir.x = +1f;

        Vector3 moveDir = transform.up * inputDir.y + transform.right * inputDir.x;

        float moveSpeed = 10f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
   
    private void EdgScrolling()
    {
        Vector2 inputDir = new Vector2(0, 0);

        int edgeScrollBuffer = 20;

        if (Input.mousePosition.x < edgeScrollBuffer) 
        {
            inputDir.x = -1f;
        }
        if (Input.mousePosition.y < edgeScrollBuffer) 
        {
            inputDir.y = -1f;
        }
        if (Input.mousePosition.x > Screen.width - edgeScrollBuffer) 
        {
            inputDir.x = +1f;
        }
        if (Input.mousePosition.y > Screen.height - edgeScrollBuffer) 
        {
            inputDir.y = +1f;
        }

        Vector3 moveDir = transform.up * inputDir.y + transform.right * inputDir.x;

        float moveSpeed = 10f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }



    private void DragPan()
    {
        Vector2 inputDir = new Vector2(0, 0);

        if (Input.GetMouseButtonDown(2))
        {
            dragPanActive = true;
            lastMousePosition = Input.mousePosition;
            DragPanTriggered?.Invoke(dragPanActive);
        }
        if (Input.GetMouseButtonUp(2))
        {
            dragPanActive = false;
            DragPanTriggered?.Invoke(dragPanActive);
        }

        if (dragPanActive)
        {
            Vector2 mouseMovementDelta = -((Vector2)Input.mousePosition - lastMousePosition);

            float dragPanSpeed = 0.35f;
            inputDir.x = mouseMovementDelta.x * dragPanSpeed;
            inputDir.y = mouseMovementDelta.y * dragPanSpeed;

            lastMousePosition = Input.mousePosition;
        }

        Vector3 moveDir = transform.up * inputDir.y + transform.right * inputDir.x;

        float moveSpeed = 10f;
        transform.position += moveDir * moveSpeed * Time.deltaTime;
    }
}
