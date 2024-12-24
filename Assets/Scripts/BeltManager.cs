using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.IO;

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

    // Called from BuildManager on belt placed
    public void UpdateBeltPath(Belt belt)
    {
        // Remove belt from any paths it was in
        if (GetBeltPath(belt) != null) { HandleDeletedBelt(belt); }

        // Single Input
        if (CheckInputs(belt) == 1)
        {
            //Check which neighbour exists and join path
            if (belt.GetRearBelt() != null)
            {
                List<Belt> rearBeltPath = GetBeltPath(belt.GetRearBelt());
                if (rearBeltPath != null) { JoinBeltPath(belt, rearBeltPath); }
            }
            else if (belt.GetRightBelt() != null)
            {
                List<Belt> rightBeltPath = GetBeltPath(belt.GetRightBelt());
                if (rightBeltPath != null) { JoinBeltPath(belt, rightBeltPath); }
            }
            else if (belt.GetLeftBelt() != null)
            {
                List<Belt> leftBeltPath = GetBeltPath(belt.GetLeftBelt());
                if (leftBeltPath != null) { JoinBeltPath(belt, leftBeltPath); }
            }

            if (belt.GetNextBelt() != null)
            { 
                UpdateNextBelt(belt);
            }
        }
        // Multi 
        else if (CheckInputs(belt) > 1)
        {
            Debug.Log("SHIIIEIEET");
            CreateNewPath(belt);
            HandleDeletedBelt(belt);
            CreateNewPath(belt);
        }
        // No Input
        else if (CheckInputs(belt) == 0)
        {
            if (belt.GetNextBelt() != null)
            { 
                CreateNewPath(belt);
                UpdateNextBelt(belt);
            }
            else
            {
                CreateNewPath(belt);
            }
        }

        UpdateDebugVisual();
        
    }

    private void UpdateNextBelt(Belt belt)
    {
        StartCoroutine(UpdateNextBelt());
        IEnumerator UpdateNextBelt()
        {   
            yield return new WaitForEndOfFrame();
            belt.GetNeighbourBelts();
            UpdateBeltPath(belt.GetNextBelt());
        }   
    }
#region Belt Path

    private void JoinBeltPath(Belt belt, List<Belt> path)
    {
        path.Add(belt);
        Debug.Log(belt.GetMachineOrigin() + " Joined Path");
    }

    public void HandleDeletedBelt(Belt deletedBelt)
    {
        // Copy other belts in this path to a new list
        List<Belt> path = GetBeltPath(deletedBelt);
        List<Belt> newBeltPath = path.Where(x => x != deletedBelt).ToList();
  
        // Delete the belt
        path.Remove(deletedBelt);

        // Delete the path
        pathList.Remove(path);

        // Update the remaining belts
        StartCoroutine(UpdateRemainingBelts());

        // Update new paths
        IEnumerator UpdateRemainingBelts()
        {   
            yield return new WaitForEndOfFrame();
            
            foreach (Belt belt in newBeltPath)
            {
                belt.GetNeighbourBelts();
                UpdateBeltPath(belt);
            }
        }
        
        Debug.Log(deletedBelt.GetMachineOrigin() + " Removed From Path");
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
                    return path;
                }
            }
        }
        return null;
    }

#endregion

#region Check Inputs

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

#endregion

#region Debug Visuals


    public void UpdateDebugVisual()
    {
        // Destroy all previous lines
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Clear the List Visualizer
        foreach (Transform child in listVisualText.transform)
        {
            Destroy(child.gameObject);
        }

        // For each path
        foreach (List<Belt> path in pathList)
        {
            // Choose a random color for this path
            Color randColor = Random.ColorHSV(0f, 1f, 0.5f, 1f, 1f, 1f);

            // Create a child with a LineRenderer
            GameObject lineObject = new GameObject("Path");
            lineObject.transform.parent = this.transform;
            LineRenderer lr = lineObject.AddComponent<LineRenderer>();

            // Set the LineRenderer
            lr.positionCount = path.Count;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.endColor = randColor;
            lr.startColor = randColor;
            float linetWidth = 0.1f;
            lr.startWidth = linetWidth;
            lr.endWidth = linetWidth;
            lr.sortingLayerName = "Debug";


            // Calculate the centre of all belts on the path
            for (int i = 0; i < path.Count; i++)
            {
                Vector2 offset = new Vector2(0,0);
                if (path[i].GetDirection() == MachineData.Direction.Up)    { offset = new Vector2(0.5f, 0.5f);   }
                if (path[i].GetDirection() == MachineData.Direction.Right) { offset = new Vector2(0.5f, -0.5f);  }
                if (path[i].GetDirection() == MachineData.Direction.Down)  { offset = new Vector2(-0.5f, -0.5f); }
                if (path[i].GetDirection() == MachineData.Direction.Left)  { offset = new Vector2(-0.5f, 0.5f);  }
                
                // Set the positions of the line renderer
                lr.SetPosition(i, new Vector3(path[i].transform.position.x + offset.x, path[i].transform.position.y + offset.y, -1));
            }
            
            // Display each list with text
            for (int i = 0; i < path.Count; i++)
            {
                GameObject textObject = new GameObject("TextObject");
                textObject.transform.parent = listVisualText.transform;
                textObject.AddComponent<TextMeshProUGUI>();
                string toPrint = i + " " + path[i].GetMachineOrigin().ToString();
                textObject.GetComponent<TextMeshProUGUI>().text = toPrint;
                textObject.GetComponent<TextMeshProUGUI>().color = randColor;
            } 
        }
    }

#endregion
}
                                                                             