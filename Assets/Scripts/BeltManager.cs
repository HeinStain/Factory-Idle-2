using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.IO;
using Unity.VisualScripting;

public class BeltManager : MonoBehaviour
{

    public static BeltManager Instance { get; private set; }

    private List<List<Belt>> pathList = new List<List<Belt>>();

    [SerializeField] GameObject listVisualText;
    
    private void Awake()
    {
        Instance = this;
    }

#region Main

    // Internal Call
    public void UpdateBeltPath(Belt belt)
    {
        StartCoroutine(UpdateBelt());
        IEnumerator UpdateBelt()
        {   
            yield return new WaitForEndOfFrame();

            // Check Neighbours
            belt.GetNeighbourBelts(); 

            // Remove belt from any paths it was in
            if (GetBeltPath(belt) != null) { HandleDeletedBelt(belt); }

            // No Input
            if (!belt.RearInput() && !belt.RightInput() && !belt.LeftInput())
            {
                if (belt.NextAvailable()) 
                { 
                    CreateNewPath(belt); 
                    UpdateBeltPath(belt.GetNextBelt()); 
                }
                else { CreateNewPath(belt); }
            }
            // Rear Input Only
            else if (belt.RearInput() && !belt.RightInput() && !belt.LeftInput())
            {
                List<Belt> rearBeltPath = GetBeltPath(belt.GetRearBelt());
                if (rearBeltPath != null) { JoinBeltPath(belt, rearBeltPath); belt.GetRearBelt().UpdateVisuals(); }
                if (belt.NextAvailable()) { UpdateBeltPath(belt.GetNextBelt()); }
            }
            // Right Input Only
            else if (!belt.RearInput() && belt.RightInput() && !belt.LeftInput())
            {
                List<Belt> rightBeltPath = GetBeltPath(belt.GetRightBelt());
                if (rightBeltPath != null) { JoinBeltPath(belt, rightBeltPath); belt.GetRightBelt().UpdateVisuals(); } 
                if (belt.NextAvailable()) { UpdateBeltPath(belt.GetNextBelt()); }
            }
            // Left Input Only
            else if (!belt.RearInput() && !belt.RightInput() && belt.LeftInput())
            {
                List<Belt> leftBeltPath = GetBeltPath(belt.GetLeftBelt());
                if (leftBeltPath != null) { JoinBeltPath(belt, leftBeltPath); belt.GetLeftBelt().UpdateVisuals(); }
                if (belt.NextAvailable()) { UpdateBeltPath(belt.GetNextBelt()); }
            }
            // Multi Input
            else if (( belt.RearInput() &&  belt.RightInput() && !belt.LeftInput())
                 ||  ( belt.RearInput() && !belt.RightInput() &&  belt.LeftInput())
                 ||  (!belt.RearInput() &&  belt.RightInput() &&  belt.LeftInput())
                 ||  ( belt.RearInput() &&  belt.RightInput() &&  belt.LeftInput()))
            {
                CreateNewPath(belt);  
                if (belt.NextAvailable()) { UpdateBeltPath(belt.GetNextBelt()); }
                if (belt.RearInput()) { belt.GetRearBelt().UpdateVisuals(); }
                if (belt.RightInput()) { belt.GetRightBelt().UpdateVisuals(); }
                if (belt.LeftInput()) { belt.GetLeftBelt().UpdateVisuals(); }
            }

            belt.UpdateVisuals();
            UpdateDebugVisual();
        }
    }


#endregion

#region Belt Path
    private void CreateNewPath(Belt belt)
    {
        List<Belt> path = new List<Belt>();
        path.Add(belt);
        pathList.Add(path);
        Debug.Log(belt.GetMachineOrigin() + "Created New Path");
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

        // Update next belt if it exists
        if (deletedBelt.NextAvailable())
        {
            deletedBelt.GetNextBelt().GetNeighbourBelts();
            UpdateBeltPath(deletedBelt.GetNextBelt());
        }
        
        // Update other belts in this path
        foreach (Belt belt in newBeltPath)
        {
            belt.GetNeighbourBelts();
            UpdateBeltPath(belt); 
        }

        Debug.Log(deletedBelt.GetMachineOrigin() + " Removed From Path");
    }

    private void JoinBeltPath(Belt belt, List<Belt> path)
    {
        path.Add(belt);
        Debug.Log(belt.GetMachineOrigin() + " Joined Path");
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

    public Vector2 GetBeltCentreOffset(Belt belt)
    {
        Vector2 offset = new Vector2(0,0);
        if (belt.GetDirection() == MachineData.Direction.Up)    { offset = new Vector2(0.5f, 0.5f);   }
        if (belt.GetDirection() == MachineData.Direction.Right) { offset = new Vector2(0.5f, -0.5f);  }
        if (belt.GetDirection() == MachineData.Direction.Down)  { offset = new Vector2(-0.5f, -0.5f); }
        if (belt.GetDirection() == MachineData.Direction.Left)  { offset = new Vector2(-0.5f, 0.5f);  }
        
        return offset;
    }

    public Belt GetNextBeltPath(Belt belt)
    {
        // Check if the last belt has a next belt
        if (belt.NextAvailable())
        {
            Belt nextBelt = belt.GetNextBelt();

            // Check if the next belt is part of another path
            List<Belt> nextBeltPath = GetBeltPath(nextBelt);
            if (nextBeltPath != null && nextBeltPath.Count > 0)
            {
                // Return the first belt of the next path
                return nextBeltPath[0];
            }
        }

        // No next belt path found
        return null;
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
                                                                             