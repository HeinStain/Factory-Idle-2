using UnityEngine;

public interface IRecipeMenu
{
    Machine GetMachineScript();
    void SelectItem(ItemData item);
    ItemData GetSelectedRecipe();
    // Add anything else ProducerRecipeBlock needs
}
