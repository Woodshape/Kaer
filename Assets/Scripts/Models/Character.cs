using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Character : IXmlSerializable {
    public float X {
        get { return Mathf.Lerp(currTile.X, nextTile.X, movementPercentage); }
    }

    public float Y {
        get { return Mathf.Lerp(currTile.Y, nextTile.Y, movementPercentage); }
    }

    public Tile currTile { get; protected set; }

    // If we aren't moving, then destTile = currTile
    Tile _destTile;
    Tile destTile {
        get { return _destTile; }
        set {
            if (_destTile != value) {
                _destTile = value;
                pathAStar = null; // If this is a new destination, then we need to invalidate pathfinding.
            }
        }
    }

    Tile nextTile; // The next tile in the pathfinding sequence
    Path_AStar pathAStar;
    float movementPercentage; // Goes from 0 to 1 as we move from currTile to destTile

    float speed = 5f; // Tiles per second

    Action<Character> cbCharacterChanged;

    Job myJob;

    // The item we are carrying (not gear/equipment)
    public Inventory inventory;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Character() {
        // Use only for serialization
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Character(Tile tile) {
        currTile = destTile = nextTile = tile;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    private void GetNewJob() {
        myJob = currTile.world.jobQueue.Dequeue();
        if (myJob == null)
            return;

        destTile = myJob.tile;
        myJob.RegisterJobCompleteCallback(OnJobEnded);
        myJob.RegisterJobCancelCallback(OnJobEnded);

        // Immediately check to see if the job tile is reachable.
        // NOTE: We might not be pathing to it right away (due to 
        // requiring materials), but we still need to verify that the
        // final location can be reached.

        pathAStar = new Path_AStar(currTile.world, currTile, destTile); // This will calculate a path from curr to dest.
        if (pathAStar.Length() == 0) {
            Debug.LogError("Path_AStar returned no path to target job tile!");
            AbandonJob();
            destTile = currTile;
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Update_DoJob(float deltaTime) {
        // Do I have a job?
        if (myJob == null) {
            GetNewJob();

            if (myJob == null) {
                // There was no job on the queue for us, so just return.
                destTile = currTile;
                return;
            }
        }

        // We have a job! (And the job tile is reachable)

        // STEP 1: Does the job have all the materials it needs?
        if (myJob.HasAllMaterial() == false) {
            // No, we are missing something!

            // STEP 2: Are we CARRYING anything that the job location wants?
            if (inventory != null) {
                if (myJob.DesiresInventoryType(inventory) > 0) {
                    // If so, deliver the goods.
                    //  Walk to the job tile, then drop off the stack into the job.
                    if (currTile == myJob.tile) {
                        // We are at the job's site, so drop the inventory
                        currTile.world.inventoryManager.PlaceInventoryOnJob(myJob, inventory);
                        
                        // This will call all cbJobWorked callbacks, because even though
                        // we aren't progressing, it might want to do something with the fact
                        // that the requirements are being met.
                        myJob.DoWork(0);

                        // Are we still carrying things?
                        if (inventory.stackSize == 0) {
                            inventory = null;
                        }
                        else {
                            Debug.LogError(
                                "[Character::Update_DoJob] Character is still carrying inventory, which shouldn't be. Just setting to NULL for now, but this means we are LEAKING inventory.");

                            inventory = null;
                        }
                    }
                    else {
                        // We still need to walk to the job site.
                        destTile = myJob.tile;
                        return;
                    }
                }
                else {
                    // We are carrying something, but the job doesn't want it!
                    // Dump the inventory at our feet
                    // TODO: Actually, walk to the nearest empty tile and dump it there.
                    if (currTile.world.inventoryManager.PlaceInventoryOnTile(currTile, inventory) == false) {
                        Debug.LogError(
                            "Character tried to dump inventory into an invalid tile (maybe there's already something here.");

                        // FIXME: For the sake of continuing on, we are still going to dump any
                        // reference to the current inventory, but this means we are "leaking"
                        // inventory.  This is permanently lost now.
                        inventory = null;
                    }
                }
            }
            else {
                // At this point, the job still requires inventory, but we aren't carrying it!

                // Are we standing on a tile with goods that are desired by the job?
                if (currTile.inventory != null && myJob.DesiresInventoryType(currTile.inventory) > 0) {
                    // Pick up the stuff!

                    currTile.world.inventoryManager.PlaceInventoryOnCharacter(
                        this,
                        currTile.inventory,
                        myJob.DesiresInventoryType(currTile.inventory)
                    );
                }
                else {
                    // Walk towards a tile containing the required goods.
                    // Find the first thing in the Job that isn't satisfied.
                    Inventory desired = myJob.GetFirstDesiredInventory();

                    Inventory supplier = currTile.world.inventoryManager.GetClosestInventoryOfType(
                        desired.objectType,
                        currTile,
                        desired.maxStackSize - desired.stackSize
                    );

                    if (supplier == null) {
                        Debug.Log("No tile contains objects of type '" + desired.objectType + "' to satisfy job requirements.");
                        AbandonJob();
                        return;
                    }

                    destTile = supplier.tile;
                    return;
                }
            }

            return; // We can't continue until all materials are satisfied.
        }

        // If we get here, then the job has all the material that it needs.
        // Lets make sure that our destination tile is the job site tile.
        destTile = myJob.tile;

        // Are we there yet?
        if (currTile == myJob.tile) {
            // We are at the correct tile for our job, so 
            // execute the job's "DoWork", which is mostly
            // going to countdown jobTime and potentially
            // call its "Job Complete" callback.
            myJob.DoWork(deltaTime);
        }

        // TODO: Nothing left for us to do here, we mostly just need Update_DoMovement to
        // get us where we want to go.
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void AbandonJob() {
        nextTile = destTile = currTile;
        currTile.world.jobQueue.Enqueue(myJob);
        myJob = null;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    private void Update_DoMovement(float deltaTime) {
        if (currTile == destTile) {
            pathAStar = null;
            return; // We're already were we want to be.
        }

        // currTile = The tile I am currently in (and may be in the process of leaving)
        // nextTile = The tile I am currently entering
        // destTile = Our final destination -- we never walk here directly, but instead use it for the pathfinding

        if (nextTile == null || nextTile == currTile) {
            // Get the next tile from the pathfinder.
            if (pathAStar == null || pathAStar.Length() == 0) {
                // Generate a path to our destination
                pathAStar = new Path_AStar(currTile.world, currTile, destTile); // This will calculate a path from curr to dest.
                if (pathAStar.Length() == 0) {
                    Debug.LogError("[Character::Update_DoMovement] Path_AStar returned no path to destination!");
                    AbandonJob();
                    return;
                }

                // Let's ignore the first tile, because that's the tile we're currently in.
                nextTile = pathAStar.Dequeue();
            }


            // Grab the next waypoint from the pathing system!
            nextTile = pathAStar.Dequeue();

            if (nextTile == currTile) {
                Debug.LogError("[Character::Update_DoMovement] nextTile is currTile?");
            }
        }

        /*		if(pathAStar.Length() == 1) {
                    return;
                }
        */
        // At this point we should have a valid nextTile to move to.

        // What's the total distance from point A to point B?
        // We are going to use Euclidean distance FOR NOW...
        // But when we do the pathfinding system, we'll likely
        // switch to something like Manhattan or Chebyshev distance
        float distToTravel = Mathf.Sqrt(
            Mathf.Pow(currTile.X - nextTile.X, 2) +
            Mathf.Pow(currTile.Y - nextTile.Y, 2)
        );

        if (nextTile.DetermineEnterability() == Enterability.Never) {
            // Most likely a wall got built, so we just need to reset our pathfinding information.
            // FIXME: Ideally, when a wall gets spawned, we should invalidate our path immediately,
            //		  so that we don't waste a bunch of time walking towards a dead end.
            //		  To save CPU, maybe we can only check every so often?
            //		  Or maybe we should register a callback to the OnTileChanged event?
            Debug.LogError("[Character::Update_DoMovement] FIXME: A character was trying to enter an unwalkable tile.");
            nextTile = null; // our next tile is a no-go
            pathAStar = null; // clearly our pathfinding info is out of date.
            return;
        }
        else if (nextTile.DetermineEnterability() == Enterability.Soon) {
            // We can't enter the NOW, but we should be able to in the
            // future. This is likely a DOOR.
            // So we DON'T bail on our movement/path, but we do return
            // now and don't actually process the movement.
            return;
        }

        // How much distance can be travel this Update?
        float distThisFrame = speed / nextTile.movementCost * deltaTime;

        // How much is that in terms of percentage to our destination?
        float percThisFrame = distThisFrame / distToTravel;

        // Add that to overall percentage travelled.
        movementPercentage += percThisFrame;

        if (movementPercentage >= 1) {
            // We have reached our destination

            // TODO: Get the next tile from the pathfinding system.
            //       If there are no more tiles, then we have TRULY
            //       reached our destination.

            currTile = nextTile;
            movementPercentage = 0;
            // FIXME: ???  Do we actually want to retain any overshot movement?
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Update(float deltaTime) {
        //Debug.Log("Character Update");

        Update_DoJob(deltaTime);

        Update_DoMovement(deltaTime);

        if (cbCharacterChanged != null)
            cbCharacterChanged(this);
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void SetDestination(Tile tile) {
        if (currTile.IsNeighbour(tile, true) == false) {
            Debug.Log("[Character::SetDestination] Our destination tile isn't actually our neighbour.");
        }

        destTile = tile;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterOnChangedCallback(Action<Character> cb) {
        cbCharacterChanged += cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterOnChangedCallback(Action<Character> cb) {
        cbCharacterChanged -= cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    void OnJobEnded(Job j) {
        // Job completed or was cancelled.

        if (j != myJob) {
            Debug.LogError("[Character::OnJobEnded] Character being told about job that isn't his. You forgot to unregister something.");
            return;
        }

        myJob = null;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public XmlSchema GetSchema() {
        return null;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void WriteXml(XmlWriter writer) {
        writer.WriteAttributeString("X", currTile.X.ToString());
        writer.WriteAttributeString("Y", currTile.Y.ToString());
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void ReadXml(XmlReader reader) { }
}
