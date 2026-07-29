using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using Unity.Collections;
using System.Data;
using UnityEngine.Pool;

public class RecipeBlock : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject notUnlockedBlock;
    [SerializeField] private Button selectButton;
    [SerializeField] private GameObject selectionSquare;

        // Data
    private IRecipeMenu menu;          // owning menu (producer or furnace)
    private Machine machine;           // underlying machine (base class)

    public ItemData itemData { get; private set; }
    public bool isSelected => menu?.GetSelectedRecipe() == itemData;



#region Public API

    /// <summary>
    /// Called by the menu after instantiating the block.
    /// </summary>
    public void UpdateBlock(IRecipeMenu menu, ItemData item)
    {
        this.menu    = menu;
        this.machine = menu.GetMachineScript();

        itemData = item;

        SetupInitialState();
        UpdateBlockVisuals();
    }

#endregion


#region Setup

    private void SetupInitialState()
    {
        if (machine == null || itemData == null) return;

        titleText.text = itemData.displayName;
        itemIcon.sprite = itemData.icon;
        itemIcon.SetNativeSize();

        // Buttons
        selectButton.onClick.RemoveAllListeners();

        // if (IsItemUnlocked())
            // Click = tell menu to select this recipe
            selectButton.onClick.AddListener(() => menu.SelectItem(itemData));
        /*
        else
            // Locked – disable actions
            selectButton.onClick.AddListener(() => 
                NotificationManager.Instance.DisplayNotification("Recipe locked"));
        */
    }
    

#endregion

#region Visual Updates

    public void UpdateBlockVisuals()
    {        
        if (itemData == null) return;

        selectionSquare.SetActive(isSelected);
        
        notUnlockedBlock.SetActive(false);
        //if (!isUnlocked) levelReqDisplay.text = $"Unlock at Level {itemData.unlockLevel}";
    }

#endregion
}