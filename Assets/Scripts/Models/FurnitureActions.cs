using System;
using UnityEngine;
using System.Collections;

public static class FurnitureActions {
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static void Door_UpdateAction(Furniture furn, float deltaTime) {
        //Debug.Log("Door_UpdateAction: " + furn.furnParameters["openness"]);

        if (furn.GetFurnitureParameter("is_opening") >= 1) {
            furn.ChangeFurnitureParameter("openness", deltaTime * 4); // FIXME: Maybe a door open speed parameter?
            if (furn.GetFurnitureParameter("openness") >= 1) {
                furn.SetFurnitureParameter("is_opening", 0);
            }
        }
        else {
            furn.ChangeFurnitureParameter("openness", deltaTime * -4);
        }

        furn.SetFurnitureParameter("openness", Mathf.Clamp01(furn.GetFurnitureParameter("openness")));

        if (furn.cbOnChanged != null) {
            furn.cbOnChanged(furn);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static void Stockpile_UpdateAction(Furniture stockpile, float deltaTime) {
        //  TODO: function doesn't need to run every update tick...
        //  It only needs to run when:
        //  --- It get's created
        //  --- An item get's delivered (reset job here)
        //  --- An item get's picked up (reset job here)
        //  --- The UI filter of allowed items changes

        if (stockpile.tile.inventory != null && stockpile.tile.inventory.stackSize >= stockpile.tile.inventory.maxStackSize) {
            //  We are full
            stockpile.ClearJobs();
            return;
        }

        //  Do we already have a job?
        if (stockpile.JobCount() > 0) {
            return;
        }

        if (stockpile.tile.inventory != null && stockpile.tile.inventory.stackSize <= 0) {
            Debug.LogError("[FurnitureActions::Stockpile_UpdateAction] Stockpile has an invalid stack size!");

            stockpile.ClearJobs();
            return;
        }

        //  At this point, we are NOT full, but we don't have a job either

        Inventory[] desiredItems;

        //  Make sure that we have a job on the queue of type:
        //  (if empty): haul any loose inventory to the stockpile
        //  (if not empty): if we are below max stack size, haul more of the same inventory
        //                  else ???
        if (stockpile.tile.inventory == null) {
            //  We are empty, so haul anything

            desiredItems = GetItemsFromFilter();
        }
        else {
            //  We still have space on this stockpile

            Inventory desired = stockpile.tile.inventory.Clone();
            desired.maxStackSize -= desired.stackSize;
            desired.stackSize = 0;

            desiredItems = new Inventory[] {desired};
        }

        Job j = new Job(
            stockpile.tile,
            null,
            null,
            0,
            desiredItems
        );

        j.canTakeInventoryFromStockpile = false;
        j.RegisterJobWorkedCallback(JobWorked_Stockpile);

        stockpile.AddJob(j);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static void JobWorked_Stockpile(Job job) {
        job.tile.furniture.RemoveJob(job);

        //  TODO: change me
        foreach (var inventory in job.inventoryRequirements.Values) {
            if (inventory.stackSize > 0) {
                job.tile.world.inventoryManager.PlaceInventoryOnTile(job.tile, inventory);
                return;
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static void JobComplete_BuildFurniture(Job job) {
        WorldController.Instance.world.PlaceFurniture(job.jobObjectType, job.tile);

        // FIXME: I don't like having to manually and explicitly set
        // flags that preven conflicts. It's too easy to forget to set/clear them!
        job.tile.pendingFurnitureJob = null;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static Enterability Door_IsEnterable(Furniture furn) {
        //Debug.Log("Door_IsEnterable");
        furn.SetFurnitureParameter("is_opening", 1);

        if (furn.GetFurnitureParameter("openness") >= 1) {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    private static Inventory[] GetItemsFromFilter() {
        //  TODO: this should be reading from some kind of UI filter later on

        return new Inventory[] {
            new Inventory("steel_plate", 0)
        };
    }
}
