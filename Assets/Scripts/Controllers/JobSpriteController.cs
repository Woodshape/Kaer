using UnityEngine;
using System.Collections.Generic;

public class JobSpriteController : MonoBehaviour
{

    // This bare-bones controller is mostly just going to piggyback
    // on FurnitureSpriteController because we don't yet fully know
    // what our job system is going to look like in the end.

    FurnitureSpriteController fsc;
    Dictionary<Job, GameObject> jobGameObjectMap;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    // Use this for initialization
    void Start()
    {
        jobGameObjectMap = new Dictionary<Job, GameObject>();
        fsc = GameObject.FindObjectOfType<FurnitureSpriteController>();

        // FIXME: No such thing as a job queue yet!
        WorldController.Instance.world.jobQueue.RegisterJobCreationCallback(OnJobCreated);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void OnJobCreated(Job job)
    {
        if (job.jobObjectType == null)
        {
            //  This job doesn't have a sprite we need to render.
            return;
        }

        // FIXME: We can only do furniture-building jobs.

        // TODO: Sprite


        if (jobGameObjectMap.ContainsKey(job))
        {
            Debug.LogError("OnJobCreated for a jobGO that already exists -- most likely a job being RE-QUEUED, as opposed to created.");
            return;
        }

        GameObject jobGO = new GameObject();

        // Add our tile/GO pair to the dictionary.
        jobGameObjectMap.Add(job, jobGO);

        jobGO.name = "JOB_" + job.jobObjectType + "_" + job.tile.X + "_" + job.tile.Y;
        jobGO.transform.position = new Vector3(job.tile.X, job.tile.Y, 0);
        jobGO.transform.SetParent(this.transform, true);

        //  Set "ghost image" to the job's object type
        SpriteRenderer sr = jobGO.AddComponent<SpriteRenderer>();
        sr.sprite = fsc.GetSpriteForFurniture(job.jobObjectType);
        sr.color = new Color(0.5f, 1f, 0.5f, 0.25f);
        sr.sortingLayerName = "Jobs";

        // FIXME: This hardcoding is not ideal!
        if (job.jobObjectType == "Door")
        {
            // By default, the door graphic is meant for walls to the east & west
            // Check to see if we actually have a wall north/south, and if so
            // then rotate this GO by 90 degrees

            Tile northTile = job.tile.North();
            Tile southTile = job.tile.South();

            if (northTile != null && southTile != null && northTile.furniture != null && southTile.furniture != null &&
                northTile.furniture.objectType == "Wall" && southTile.furniture.objectType == "Wall")
            {
                jobGO.transform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }


        job.RegisterJobCompleteCallback(OnJobEnded);
        job.RegisterJobCancelCallback(OnJobEnded);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void OnJobEnded(Job job)
    {
        // This executes whether a job was COMPLETED or CANCELLED

        // FIXME: We can only do furniture-building jobs.

        GameObject jobGO = jobGameObjectMap[job];

        job.UnregisterJobCompleteCallback(OnJobEnded);
        job.UnregisterJobCancelCallback(OnJobEnded);

        Destroy(jobGO);

    }
}
