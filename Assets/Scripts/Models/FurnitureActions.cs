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
            //  empty
            Job j = new Job(
                stockpile.tile,
                null,
                null,
                0,
                new Inventory[1] { new Inventory("steel_plates", 0) }
            );
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
    public static ENTERABILITY Door_IsEnterable(Furniture furn)
    {
        //Debug.Log("Door_IsEnterable");
        furn.SetFurnitureParameter("is_opening", 1);

        if (furn.GetFurnitureParameter("openness") >= 1)
        {
            return ENTERABILITY.Yes;
        }

        return ENTERABILITY.Soon;
    }
}
