using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{

    private Grid grid;

    private void Start()
    {
        grid = new Grid(20, 10, 1, new Vector3 (-10,-5));
    }

    private void Update()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        if (Input.GetMouseButtonDown(0)) 
        {
            grid.SetValue(mousePosition, 69);
            
        }

        if (Input.GetMouseButtonDown(1)) 
        {
            Debug.Log(grid.GetValue(mousePosition));
            
        }
    }

    
}
