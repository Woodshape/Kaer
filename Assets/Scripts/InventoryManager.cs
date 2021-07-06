using UnityEngine;
using System.Collections.Generic;

public class InventoryManager {
    // This is a list of all "live" inventories.
    // Later on this will likely be organized by rooms instead
    // of a single master list. (Or in addition to.)
    public Dictionary<string, List<Inventory>> inventories;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public InventoryManager() {
        inventories = new Dictionary<string, List<Inventory>>();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void CleanupInventory(Inventory inv) {
        if (inv.stackSize == 0) {
            if (inventories.ContainsKey(inv.objectType)) {
                inventories[inv.objectType].Remove(inv);
            }

            if (inv.tile != null) {
                inv.tile.inventory = null;
                inv.tile = null;
            }

            if (inv.character != null) {
                inv.character.inventory = null;
                inv.character = null;
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool PlaceInventoryOnTile(Tile tile, Inventory inv) {
        bool tileWasEmpty = tile.inventory == null;

        if (tile.PlaceInventory(inv) == false) {
            // The tile did not accept the inventory for whatever reason, therefore stop.
            return false;
        }

        CleanupInventory(inv);

        // We may also created a new stack on the tile, if the tile was previously empty.
        if (tileWasEmpty) {
            if (inventories.ContainsKey(tile.inventory.objectType) == false) {
                inventories[tile.inventory.objectType] = new List<Inventory>();
            }

            inventories[tile.inventory.objectType].Add(tile.inventory);
        }

        return true;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool PlaceInventoryOnJob(Job job, Inventory inv) {
        if (job.inventoryRequirements.ContainsKey(inv.objectType) == false) {
            Debug.LogError("Trying to add inventory to a job that it doesn't want.");
            return false;
        }

        job.inventoryRequirements[inv.objectType].stackSize += inv.stackSize;

        if (job.inventoryRequirements[inv.objectType].maxStackSize < job.inventoryRequirements[inv.objectType].stackSize) {
            inv.stackSize = job.inventoryRequirements[inv.objectType].stackSize -
                            job.inventoryRequirements[inv.objectType].maxStackSize;

            job.inventoryRequirements[inv.objectType].stackSize = job.inventoryRequirements[inv.objectType].maxStackSize;
        }
        else {
            inv.stackSize = 0;
        }

        CleanupInventory(inv);

        return true;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public bool PlaceInventoryOnCharacter(Character character, Inventory sourceInventory, int amount = -1) {
        if (amount < 0) {
            //  If we don't specify an amount (rather, amount is default), just pick up however much we need.
            amount = sourceInventory.stackSize;
        }
        else {
            //  Make sure not to load more stuff on the character that is available in the inventory.
            amount = Mathf.Min(amount, sourceInventory.stackSize);
        }

        //  If we aren't already carrying anything, make sure to get (clone) whatever we want to carry
        //  and add it to the newly created inventory of the character.
        if (character.inventory == null) {
            character.inventory = sourceInventory.Clone();
            character.inventory.stackSize = 0;
            inventories[character.inventory.objectType].Add(character.inventory);
        }
        else if (character.inventory.objectType != sourceInventory.objectType) {
            Debug.LogError("Character is trying to pick up a mismatched inventory object type.");
            return false;
        }

        character.inventory.stackSize += amount;

        if (character.inventory.stackSize > character.inventory.maxStackSize) {
            sourceInventory.stackSize = character.inventory.stackSize - character.inventory.maxStackSize;
            character.inventory.stackSize = character.inventory.maxStackSize;
        }
        else {
            sourceInventory.stackSize -= amount;
        }

        CleanupInventory(sourceInventory);

        return true;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// Gets the type of the closest inventory of.
    /// </summary>
    /// <returns>The closest inventory of type.</returns>
    /// <param name="objectType">Object type.</param>
    /// <param name="t">T.</param>
    /// <param name="desiredAmount">Desired amount. If no stack has enough, it instead returns the largest</param>
    public Inventory GetClosestInventoryOfType(string objectType, Tile t, int desiredAmount) {
        // FIXME:
        //   a) We are LYING about returning the closest item.
        //   b) There's no way to return the closest item in an optimal manner
        //      until our "inventories" database is more sophisticated.
        //		(i.e. seperate tile inventory from character inventory and maybe
        //		 has room content optimization.)

        if (inventories.ContainsKey(objectType) == false) {
            Debug.LogError("GetClosestInventoryOfType -- no items of desired type.");
            return null;
        }

        foreach (Inventory inv in inventories[objectType]) {
            if (inv.tile != null) {
                return inv;
            }
        }

        return null;
    }
}
