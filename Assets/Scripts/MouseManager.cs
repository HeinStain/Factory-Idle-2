using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MouseManager : MonoBehaviour
{

    [Header("Icons")]
    [SerializeField] private GameObject mouseObj;
    [SerializeField] private Image mouseImage;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Sprite interactIcon;
    [SerializeField] private Sprite buildIcon;
    [SerializeField] private Sprite panningIcon;
    [SerializeField] private Sprite buyIcon;


    [Header("Position Data")]
    [SerializeField] private Vector2 mousePosition;
    [SerializeField] private Vector2 canvasMousePosition;

    [Header("Raycast")]
    private RaycastHit2D hit;
    [SerializeField] private LayerMask machineLayer;


    #region Event Triggers

    private bool buyModeActive = false;
    private bool buildModeActive = false;
    private bool menuOpen = false;
    private bool dragPanActive = false;

    private void OnEnable() 
    { 
        GameManager.OnBuyModeTriggered += BuyModeTriggered; 
        GameManager.OnBuildModeTriggered += BuildModeTriggered;
        GameManager.OnMenuTriggered += MenuOpenTriggered; 
        CameraSystem.DragPanTriggered += DragPanTriggered;
    }
    private void OnDisable() 
    { 
        GameManager.OnBuyModeTriggered -= BuyModeTriggered; 
        GameManager.OnBuildModeTriggered -= BuildModeTriggered; 
        GameManager.OnMenuTriggered -= MenuOpenTriggered; 
        CameraSystem.DragPanTriggered -= DragPanTriggered;
    }
    
    private void BuyModeTriggered(bool buyMode) { buyModeActive = buyMode; }
    private void BuildModeTriggered(bool buildMode) { buildModeActive = buildMode; }
    private void MenuOpenTriggered(bool menuOpenStatus) { menuOpen = menuOpenStatus; }
    private void DragPanTriggered(bool dragPanning) { dragPanActive = dragPanning; }

    #endregion 

    #region Main

    void Start()
    {
        Cursor.visible = false;
    }

    void Update()
    {
        //Mouse Position Calculations
        mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        canvasMousePosition = Input.mousePosition;
        mouseObj.transform.position = canvasMousePosition;


        if (!menuOpen)
        {
            if (dragPanActive)
            {
                SetMouseIcon(Icon.PANNING);
            }
            else if (!dragPanActive)
            {
                if (buildModeActive)
                {
                    SetMouseIcon(Icon.BUILD);
                }
                else if (buyModeActive)
                {
                    SetMouseIcon(Icon.BUY);
                }
                else if (!buildModeActive && !buyModeActive)
                {
                    hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, machineLayer);

                    if (hit.collider != null && hit.collider.tag == "Interactable")
                    {
                        SetMouseIcon(Icon.INTERACT);

                        // On Left Mouse Click
                        if (Input.GetMouseButtonDown(0))
                        {
                            hit.transform.SendMessage("OpenMenu", SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    else
                    {
                        SetMouseIcon(Icon.DEFAULT);
                    }
                }
            }
        }   
        else if (menuOpen)
        {
            SetMouseIcon(Icon.DEFAULT);
            ///
            ///
            //Logic for interacting with UI buttons goes here
            ///
            ///
        }
    }

    #endregion 

    #region Visual Icon

    public enum Icon
    {
        DEFAULT, BUILD, BUY, INTERACT, PANNING
    }

    public Icon currentIcon = Icon.DEFAULT;
    private bool iconChanged;
    
    private void SetMouseIcon(Icon icon)
    {
        currentIcon = icon;
        iconChanged = true;

        switch(currentIcon)
        {
            case Icon.DEFAULT:   ChangeIcon(defaultIcon);    break;
            case Icon.BUILD:     ChangeIcon(buildIcon);      break;
            case Icon.BUY:       ChangeIcon(buyIcon);        break;
            case Icon.INTERACT:  ChangeIcon(interactIcon);   break;
            case Icon.PANNING:   ChangeIcon(panningIcon);    break;
            default: Debug.LogError("Icon not implemented"); break;
        }

        void ChangeIcon(Sprite selected)
        {
            if (iconChanged)
            {
                mouseImage.SetNativeSize();
                mouseImage.sprite = selected;
                iconChanged = false;
            }
        }
    }

    #endregion Visuals
}
