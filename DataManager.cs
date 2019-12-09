using UnityEngine;
using Windows.Kinect;
using System.Collections.Generic;
using System;

/// <summary>
/// Stores dictionaries of MeshData and SkeletonData objects created by the KinectSource object.
/// </summary>
public class DataManager : MonoBehaviour {

    public Dictionary<TimeSpan, MeshData> meshes;
    public Dictionary<TimeSpan, SkeletonData> skeletons;

    public TimeSpan currentTime;
    public MeshData currentMesh;
    public SkeletonData currentSkeleton;

    public bool meshReady;
    public bool skeletonReady;

    /// <summary>
    /// Called when DataManager GameObject is initialised by the Unity engine.
    /// 
    /// Sets up the Dictionaries for MeshData and Skeleton data storage, accessible by TimeSpan.
    /// Sets intial value of the flags that indicate to the DisplayManager whether meshes and skeletons are ready.
    /// </summary>
    void Start()
    {
        this.meshes = new Dictionary<TimeSpan, MeshData>();
        this.skeletons = new Dictionary<TimeSpan, SkeletonData>();
        this.meshReady = false;
        this.skeletonReady = false;
    }

    /// <summary>
    /// Assigns current time, mesh and skeleton to global variables to be accessed by DisplayManager.
    /// Adds current MeshData and SkeletonData to their dictionaries.
    /// </summary>
    /// <param name="time">Time stamp for the mesh and skeleton data. </param>
    /// <param name="m"> The data for the mesh. </param>
    /// <param name="s"> The data for the skeleton. </param>
    public void AddToList(TimeSpan time, MeshData m, SkeletonData s)
    {
        currentTime = time;

        currentMesh = m;
        this.meshes.Add(currentTime, currentMesh);
        this.meshReady = true;

        currentSkeleton = s;
        this.skeletons.Add(currentTime, currentSkeleton);
        this.skeletonReady = true;
    }

    /// <summary>
    /// Obtains the latest mesh stored in the DataManager.
    /// </summary>
    /// <returns>The latest mesh stored in the DataManager.</returns>
    public MeshData GetMeshCurrent()
    {
        return currentMesh;
    }

    /// <summary>
    /// Obtains the latest skeleton stored in the DataManager.
    /// </summary>
    /// <returns> The latest skeleton stored in the DataManager. </returns>
    public SkeletonData GetSkeletonCurrent()
    {
        return currentSkeleton;
    }

    /// <summary>
    /// Obtains the mesh specified by the timestamp stored in the DataManager mesh dictionary.
    /// </summary>
    /// <param name="time"> The time stamp for the desired mesh.</param>
    /// <returns> The mesh at the given time stamp.</returns>
    public MeshData GetMeshAtTime(TimeSpan time)
    {
        return meshes[time];
    }

    /// <summary>
    /// Obtains the skeleton specified by the timestamp stored in the DataManager skeleton dictionary.
    /// </summary>
    /// <param name="time"> The time stamp for the desired skeleton.</param>
    /// <returns> The skeleton at the given time stamp.</returns>
    public SkeletonData GetSkeletonAtTime(TimeSpan time)
    {
        return skeletons[time];
    }

}