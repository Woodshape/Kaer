using System;
using UnityEngine;
using System.Collections;

public static class FurnitureActions
{
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static void Door_UpdateAction(Furniture furn, float deltaTime)
    {
        //Debug.Log("Door_UpdateAction: " + furn.furnParameters["openness"]);

        if (furn.GetFurnitureParameter("is_opening") >= 1)
        {
            furn.ChangeFurnitureParameter("openness", deltaTime * 4);   // FIXME: Maybe a door open speed parameter?
            if (furn.GetFurnitureParameter("openness") >= 1)
            {
                furn.SetFurnitureParameter("is_opening", 0);
            }
        }
        else
        {
            furn.ChangeFurnitureParameter("openness", deltaTime * -4);
        }

        furn.SetFurnitureParameter("openness", Mathf.Clamp01(furn.GetFurnitureParameter("openness")));

        if (furn.cbOnChanged != null)
        {
            furn.cbOnChanged(furn);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static void Stockpile_UpdateAction(Furniture stockpile, float deltaTime)
    {
        //  Make sure that we have a job on the queue of type:
        //  (if empty): haul any loose inventory to the stockpile
        //  (if not empty): if we are below max stack size, haul more of the same inventory
        //                  else ???
        if (stockpile.tile.Inventory == null)
        {
            //  We are empty, so haul anything

            //  Do we already have a job?
            if (stockpile.JobCount() > 0) {
                return;
            }
            
            Job j = new Job(
                stockpile.tile,
                null,
                null,
                0,
                new Inventory[1] {  //  FIXME
                    new Inventory("steel_plate", 0)
                }
            );

            stockpile.AddJob(j);
        }
        else if (stockpile.tile.inventory.stackSize < stockpile.tile.inventory.maxStackSize) {
            //  We still have space on this stockpile
            
            //  Do we already have a job?
            if (stockpile.JobCount() > 0) {
                return;
            }

            Inventory desired = stockpile.tile.inventory.Clone();
            desired.maxStackSize -= desired.stackSize;
            desired.stackSize = 0;
            
            Job j = new Job(
                stockpile.tile,
                null,
                null,
                0,
                new Inventory[1] {  
                    desired
                }
            );
            
            j.RegisterJobWorkedCallback(JobWorked_Stockpile);

            stockpile.AddJob(j);
        }
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
    public static void JobComplete_BuildFurniture(Job job)
    {
        WorldController.Instance.world.PlaceFurniture(job.jobObjectType, job.tile);

        // FIXME: I don't like having to manually and explicitly set
        // flags that preven conflicts. It's too easy to forget to set/clear them!
        job.tile.pendingFurnitureJob = null;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public static Enterability Door_IsEnterable(Furniture furn)
    {
        //Debug.Log("Door_IsEnterable");
        furn.SetFurnitureParameter("is_opening", 1);

        if (furn.GetFurnitureParameter("openness") >= 1)
        {
            return Enterability.Yes;
        }

        return Enterability.Soon;
    }
}
