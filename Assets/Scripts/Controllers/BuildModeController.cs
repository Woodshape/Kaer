using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class BuildModeController : MonoBehaviour
{

    bool buildModeIsObjects = false;
    TileType buildModeTile = TileType.Floor;
    string buildModeObjectType;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Use this for initialization
    void Start()
    {
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void SetMode_BuildFloor()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Floor;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void SetMode_Bulldoze()
    {
        buildModeIsObjects = false;
        buildModeTile = TileType.Empty;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void SetMode_BuildFurniture(string objectType)
    {
        // Wall is not a Tile!  Wall is an "Furniture" that exists on TOP of a tile.
        buildModeIsObjects = true;
        buildModeObjectType = objectType;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void DoPathfindingTest()
    {
        WorldController.Instance.world.SetupPathfindingExample();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void DoBuild(Tile t)
    {
        if (buildModeIsObjects == true)
        {
            // Create the Furniture and assign it to the tile

            // FIXME: This instantly builds the furnite:
            //WorldController.Instance.World.PlaceFurniture( buildModeObjectType, t );

            // Can we build the furniture in the selected tile?
            // Run the ValidPlacement function!

            string furnitureType = buildModeObjectType;

            if (
                WorldController.Instance.world.IsFurniturePlacementValid(furnitureType, t) &&
                t.pendingFurnitureJob == null
            )
            {
                // This tile position is valid for this furniture
                // Create a job for it to be build
                Job j;

                //  We want to make sure that we have a job prototype for the furniture at that tile
                //  FIXME: otherwise we just use a dummy-job for now
                if (WorldController.Instance.world.furnitureJobPrototypes.ContainsKey(furnitureType))
                {
                    j = WorldController.Instance.world.furnitureJobPrototypes[furnitureType].Clone();

                    //  We need to assign a tile for that job
                    j.tile = t;
                }
                else
                {
                    Debug.LogError("There is no furniture job prototype for " + furnitureType);
                    j = new Job(t, furnitureType, FurnitureActions.JobComplete_BuildFurniture, 0.1f, null);
                }

                // FIXME: I don't like having to manually and explicitly set
                // flags that preven conflicts. It's too easy to forget to set/clear them!
                t.pendingFurnitureJob = j;
                j.RegisterJobCancelCallback((job) => { job.tile.pendingFurnitureJob = null; });

                // Add the job to the queue
                WorldController.Instance.world.jobQueue.Enqueue(j);

            }

        }
        else
        {
            // We are in tile-changing mode.
            t.Type = buildModeTile;
        }

    }
}
