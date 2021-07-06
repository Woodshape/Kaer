using UnityEngine;
using System.Collections.Generic;
using System;

public class JobQueue {
    Queue<Job> jobQueue;

    Action<Job> cbJobCreated;

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public JobQueue() {
        jobQueue = new Queue<Job>();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Enqueue(Job job) {
        if (job.jobTime < 0) {
            //  A job time of less than 0 means it should be insta-completed.
            job.DoWork(0);
            return;
        }

        jobQueue.Enqueue(job);

        if (cbJobCreated != null) {
            cbJobCreated(job);
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public Job Dequeue() {
        if (jobQueue.Count == 0)
            return null;

        return jobQueue.Dequeue();
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void RegisterJobCreationCallback(Action<Job> cb) {
        cbJobCreated += cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void UnregisterJobCreationCallback(Action<Job> cb) {
        cbJobCreated -= cb;
    }

    //////////////////////////////////////////////////////////////////////////////////////////////////////
    public void Remove(Job j) {
        // TODO: Check docs to see if there's a less memory/swappy solution
        List<Job> jobs = new List<Job>(jobQueue);

        if (jobs.Contains(j) == false) {
            Debug.LogError("Trying to remove a job that doesn't exist on the queue.");
            return;
        }

        jobs.Remove(j);
        jobQueue = new Queue<Job>(jobs);
    }
}
