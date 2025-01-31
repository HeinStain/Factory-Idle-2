using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class GridBuildGhost : MonoBehaviour
{
    
    private Transform visual;
    private MachineData machineData;

    private bool buildMode;

    private void Start()
    {
        RefreshVisual();

        buildMode = false;
        Instance_OnBuildModeChanged(buildMode);

        BuildManager.Instance.OnSelectedChanged += RefreshVisual;
        BuildManager.Instance.OnBuildModeChanged += Instance_OnBuildModeChanged;
    }

    private void RefreshVisual()
    {
        if (visual != null) 
        {
            Destroy(visual.gameObject);
            visual = null;
        }

        MachineData machineData = BuildManager.Instance.GetHeldMachine();

        if (machineData != null)
        {
            visual = Instantiate(machineData.visual, Vector2.zero, Quaternion.identity);
            visual.parent = transform;
            visual.localPosition = Vector2.zero;
            visual.localEulerAngles = Vector2.zero;
            int ghostLayer = LayerMask.NameToLayer("Build Ghost");
            SetLayerRecursive(visual.gameObject, ghostLayer);
        }
    }
    
    private void Instance_OnBuildModeChanged(bool arg)
    {
        buildMode = arg;
        if (buildMode == false)
        {
            visual.localScale = new Vector2(0,0);
        }
        else if (buildMode == true)
        {
            visual.localScale = new Vector2(1,1);
        }
    }


    private void LateUpdate()
    {
        if (buildMode)
        {
            Vector2 targetPosition = BuildManager.Instance.GetMouseWorldSnappedPosition();
            transform.position = Vector2.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
            transform.rotation = Quaternion.Lerp(transform.rotation, BuildManager.Instance.GetMachineRotation(), Time.deltaTime * 15f);
        }

    }



    
    // Set all visuals to "Build Ghost" layer (necessary for renderer tricks in the future)
    private void SetLayerRecursive(GameObject targetObj, int layer)
    {
        targetObj.layer = layer;
        foreach (Transform child in targetObj.transform)
        {
            SetLayerRecursive(child.gameObject, layer);
        }
    }    
}

