using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BeltManager : MonoBehaviour
{

    public static BeltManager Instance { get; private set; }
    
    private Grid<GridMap.GridTile> gridArray;

    private List<List<Belt>> pathList = new List<List<Belt>>();

    [SerializeField] GameObject listVisualText;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        gridArray = GameManager.Instance.GetGrid();
    }

    public void UpdateBeltPath(Belt belt)
    {
        if (CheckInputs(belt) == 1)
        {
            if (belt.GetRearBelt() != null)
            {
                List<Belt> rearBeltPath = GetBeltPath(belt.GetRearBelt());
                if (rearBeltPath != null)
                {
                    JoinBeltPath(belt, rearBeltPath);
                }
            } 
            else if (belt.GetRightBelt() != null)
            {
                List<Belt> rightBeltPath = GetBeltPath(belt.GetRightBelt());
                if (rightBeltPath != null)
                {
                    JoinBeltPath(belt, rightBeltPath);
                }
            }
            else if (belt.GetLeftBelt() != null)
            {
                List<Belt> leftBeltPath = GetBeltPath(belt.GetLeftBelt());
                if (leftBeltPath != null)
                {
                    JoinBeltPath(belt, leftBeltPath);
                } 
            }
        }
        else if (CheckInputs(belt) != 1)
        {
            //if not already in a path
            if (GetBeltPath(belt) == null)
            {
                CreateNewPath(belt);
            }
            else
            {
                RemoveFromPath(belt, GetBeltPath(belt));
                CreateNewPath(belt);
            }
        }

        /* 
        if (belt.GetNextBelt() != null)
        {
            List<Belt> nextBeltPath = GetBeltPath(belt.GetNextBelt());
            if (nextBeltPath != null)
            {
                RemoveFromPath(belt, nextBeltPath);
            }
            
            UpdateBeltPath(belt.GetNextBelt());
        }
        else
        {
            //set path ending to end of this belt
        }
        */

        // UpdateDebugVisuals(belt);

        UpdateDebugPathVisual();
        UpdateDebugListVisual();
        
    }

    public void UpdateDebugPathVisual()
    {
        // Destroy all previous lines
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Make new lines
        foreach (List<Belt> path in pathList)
        {
            // Create a new GameObject as a child of the current object
            GameObject lineObject = new GameObject("LineRendererObject");
            lineObject.transform.parent = this.transform;

            // Add a LineRenderer component to the new child object
            LineRenderer lr = lineObject.AddComponent<LineRenderer>();
            lr.positionCount = path.Count;

            // Set the width of the LineRenderer
            float lineWidth = 0.1f;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;

            for (int i = 0; i < path.Count; i++)
            {
                Vector2 offset = new Vector2(0,0);
                if (path[i].GetDirection() == MachineData.Direction.Up)    { offset = new Vector2(0.5f, 0.5f);   }
                if (path[i].GetDirection() == MachineData.Direction.Right) { offset = new Vector2(0.5f, -0.5f);  }
                if (path[i].GetDirection() == MachineData.Direction.Down)  { offset = new Vector2(-0.5f, -0.5f); }
                if (path[i].GetDirection() == MachineData.Direction.Left)  { offset = new Vector2(-0.5f, 0.5f);  }

                lr.SetPosition(i, new Vector3(path[i].transform.position.x + offset.x, path[i].transform.position.y + offset.y, -1));
            }
            Debug.Log("Buidling Debug Line");
        }
    }

    public void UpdateDebugListVisual()
    {
        foreach (Transform child in listVisualText.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (List<Belt> path in pathList)
        {
            for (int i = 0; i < path.Count; i++)
            {
                GameObject textObject = new GameObject("TextObject");
                textObject.transform.parent = listVisualText.transform;
                textObject.AddComponent<TextMeshProUGUI>();
                string toPrint = i + " " + path[i].GetMachineOrigin().ToString();
                textObject.GetComponent<TextMeshProUGUI>().text = toPrint;
            } 
        }
    }

    private void JoinBeltPath(Belt belt, List<Belt> path)
    {
        path.Add(belt);
        Debug.Log(belt.GetMachineOrigin() + " Joined Path");
    }

    public void RemoveFromPath(Belt belt, List<Belt> path)
    {
        Belt n1 = belt.GetNextBelt();
        Belt n2 = belt.GetRearBelt();
        Belt n3 = belt.GetRightBelt();
        Belt n4 = belt.GetLeftBelt();


        path.Remove(belt);

        if (n1 != null) { UpdateBeltPath(n1); }
        if (n2 != null) { UpdateBeltPath(n2); }
        if (n3 != null) { UpdateBeltPath(n3); }
        if (n4 != null) { UpdateBeltPath(n4); }
        
        // when removed, the paths needs to update
        Debug.Log(belt.GetMachineOrigin() + " Removed From Path");
    }

    private void CreateNewPath(Belt belt)
    {
        List<Belt> path = new List<Belt>();
        path.Add(belt);
        pathList.Add(path);
        Debug.Log(belt.GetMachineOrigin() + "Created New Path");
    }
    
    public List<Belt> GetBeltPath(Belt belt)
    {
        foreach (List<Belt> path in pathList)
        {
            foreach (Belt b in path)
            {
                if (b == belt)
                {
                    Debug.Log("Path found");
                    return path;
                }
            }
        }
        return null;
    }


    private int CheckInputs(Belt belt)
    {
        int inputs = 0;

        if (belt.GetRearBelt() != null )  
        { 
            if (belt.GetDirection() == MachineData.Direction.Right)
            {
                if (belt.GetRearBelt().GetDirection() == MachineData.Direction.Right)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Down)
            {
                if (belt.GetRearBelt().GetDirection() == MachineData.Direction.Down)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Left)
            {
                if (belt.GetRearBelt().GetDirection() == MachineData.Direction.Left)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Up)
            {
                if (belt.GetRearBelt().GetDirection() == MachineData.Direction.Up)
                {
                    inputs++; 
                }
            }
        }

        if (belt.GetRightBelt() != null) 
        { 
            if (belt.GetDirection() == MachineData.Direction.Right)
            {
                if (belt.GetRightBelt().GetDirection() == MachineData.Direction.Up)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Down)
            {
                if (belt.GetRightBelt().GetDirection() == MachineData.Direction.Right)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Left)
            {
                if (belt.GetRightBelt().GetDirection() == MachineData.Direction.Down)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Up)
            {
                if (belt.GetRightBelt().GetDirection() == MachineData.Direction.Left)
                {
                    inputs++; 
                }
            }
        }

        if (belt.GetLeftBelt() != null)  
        { 
            if (belt.GetDirection() == MachineData.Direction.Right)
            {
                if (belt.GetLeftBelt().GetDirection() == MachineData.Direction.Down)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Down)
            {
                if (belt.GetLeftBelt().GetDirection() == MachineData.Direction.Left)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Left)
            {
                if (belt.GetLeftBelt().GetDirection() == MachineData.Direction.Up)
                {
                    inputs++; 
                }
            }
            else if (belt.GetDirection() == MachineData.Direction.Up)
            {
                if (belt.GetLeftBelt().GetDirection() == MachineData.Direction.Right)
                {
                    inputs++; 
                }
            }
        }
        return inputs;
    }


    private void UpdateDebugVisuals(Belt belt)
    {
        LineRenderer lr = belt.gameObject.transform.Find("Line").GetComponent<LineRenderer>();

        if (belt.GetNextBelt() != null)
        {
            Debug.Log(belt.GetMachineOrigin() + " Skibidi");
            lr.enabled = true;

            lr.SetPosition(1, new Vector3(0,1.25f,0));
        }
        else
        {
            Debug.Log(belt.GetMachineOrigin() + " Toilet");
            lr.enabled = false;
        }

    }
}
                                                                             