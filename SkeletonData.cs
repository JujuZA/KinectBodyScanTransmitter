using System.Collections;
using Windows.Kinect;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Stores Kinect data reprenting joints and calculates and creates appropriate bones to form a skeleton.
/// </summary>
public class SkeletonData
{
    
    public TimeSpan timeStamp;
    public Body[] bodies;
    public Dictionary<JointType, Vector3> joints;

    public Body trackedBody;

    public Vector3[] jointPositions;
    public Quaternion[] jointRotations;
    public Vector3[] jointScales;

    public Vector3[] bonePositions;
    public Quaternion[] boneRotations;
    public Vector3[] boneScales;

    public static int jointCount = 25;
    public static int boneCount = 24;

    public float jointScale = 0.025f;
    public float boneScale = 0.0125f;

    public static string[] jointNames = {  "SpineBase", "SpineMid", "Neck", "Head",
                                           "ShoulderLeft", "ElbowLeft", "WristLeft", "HandLeft",
                                           "ShoulderRight", "ElbowRight", "WristRight", "HandRight",
                                           "HipLeft", "KneeLeft", "AnkleLeft", "FootLeft",
                                           "HipRight", "KneeRight", "AnkleRight", "FootRight",
                                           "SpineShoulder", "HandTipLeft", "ThumbLeft", "HandTipRight", "ThumbRight" };

    public static JointType[] jointTypes = {   JointType.SpineBase, JointType.SpineMid, JointType.Neck, JointType.Head,
                                               JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft,
                                               JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight,
                                               JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft,
                                               JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight,
                                               JointType.SpineShoulder,
                                               JointType.HandTipLeft, JointType.ThumbLeft, JointType.HandTipRight, JointType.ThumbRight };

    public static Color[] jointColors = {   Color.white, Color.red, Color.green, Color.blue,
                                            Color.blue, Color.green, Color.red, Color.red,
                                            Color.blue, Color.green, Color.red, Color.red,
                                            Color.green, Color.red, Color.blue, Color.green,
                                            Color.blue, Color.green, Color.blue, Color.red,
                                            Color.red, Color.blue, Color.green, Color.blue, Color.green };

    public static string[] boneNames = {    "SpineMidToSpineBase", "NeckToSpineShoulder", "HeadToNeck",
                                            "ShoulderLeftToSpineShoulder", "ElbowLeftToShoulderLeft", "WristLeftToElbowLeft", "HandLeftToWristLeft",
                                            "ShoulderRightToSpineShoulder", "ElbowRightToShoulderRight", "WristRightToElbowRight", "HandRightToWristRight",
                                            "HipLeftToSpineBase", "KneeLeftToHipLeft", "AnkleLeftToKneeLeft", "FootLeftToAnkleLeft",
                                            "HipRightToSpineBase", "KneeRightToHipRight", "AnkleRightToKneeRight", "FootRightToAnkleRight",
                                            "SpineShoulderToSpineMid",
                                            "HandTipLeftToHandLeft", "ThumbLeftToHandLeft", "HandTipRightToHandRight", "ThumbRightToHandRight" };

    public static int[,] boneJoints = { { 1, 0 }, { 2, 20 }, { 3, 2 }, { 4, 20 }, { 5, 4 }, { 6, 5 }, { 7, 6 }, { 8, 20 }, { 9, 8 }, 
                                        { 10, 9 }, { 11, 10 }, { 12, 0 }, { 13, 12 }, { 14, 13 }, { 15, 14 }, { 16, 0 }, { 17, 16 },
                                        { 18, 17 }, { 19, 18 }, { 20, 1 }, { 21, 7 }, { 22, 7 }, { 23, 11 }, { 24, 11 } };

    public static Color[] boneColors = {    Color.Lerp(jointColors[boneJoints[0, 0]], jointColors[boneJoints[0, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[1, 0]], jointColors[boneJoints[1, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[2, 0]], jointColors[boneJoints[2, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[3, 0]], jointColors[boneJoints[3, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[4, 0]], jointColors[boneJoints[4, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[5, 0]], jointColors[boneJoints[5, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[6, 0]], jointColors[boneJoints[6, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[7, 0]], jointColors[boneJoints[7, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[8, 0]], jointColors[boneJoints[8, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[9, 0]], jointColors[boneJoints[9, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[10, 0]], jointColors[boneJoints[10, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[11, 0]], jointColors[boneJoints[11, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[12, 0]], jointColors[boneJoints[12, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[13, 0]], jointColors[boneJoints[13, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[14, 0]], jointColors[boneJoints[14, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[15, 0]], jointColors[boneJoints[15, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[16, 0]], jointColors[boneJoints[16, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[17, 0]], jointColors[boneJoints[17, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[18, 0]], jointColors[boneJoints[18, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[19, 0]], jointColors[boneJoints[19, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[20, 0]], jointColors[boneJoints[20, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[21, 0]], jointColors[boneJoints[21, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[22, 0]], jointColors[boneJoints[22, 1]], 0.5f),
                                            Color.Lerp(jointColors[boneJoints[23, 0]], jointColors[boneJoints[23, 1]], 0.5f) };

    public static bool[] jointMeshVertical = { true, true, true, true, false, false, false, false, false, false, false, false,
                                        true, true, true, true, true, true, true, true, true, false, false, false, false};

    public static bool[] boneMeshVertical = { true, true, true, false, false, false, false, false, false, false, false, true,
                                       true, true, true, true, true, true, true, true, false, false, false, false};

    /// <summary>
    /// Creates the SkeletonData object, calculates joint and bone positions and rotations.
    /// </summary>
    /// <param name="time"> Time stamp recieved for the relevant frame from the Kinect sensor. </param>
    /// <param name="b"> Windows Kinect Body data object, which contains joint positions and rotations. </param>
    public SkeletonData(TimeSpan time, Body[] b)
    {
        this.timeStamp = time;
        this.bodies = b;
        this.joints = new Dictionary<JointType, Vector3>();

        this.trackedBody = null;
        SelectBody();
        
        this.jointPositions = new Vector3[jointCount];
        this.jointRotations = new Quaternion[jointCount];
        this.jointScales = new Vector3[jointCount];
        this.bonePositions = new Vector3[boneCount];
        this.boneRotations = new Quaternion[boneCount];
        this.boneScales = new Vector3[boneCount];

        CalculateJoints();
        CalculateBones();
    }

    /// <summary>
    /// Selects the first of multiple bodies the Kinect sensor detects, to avoid multiple scans.
    /// </summary>
    public void SelectBody()
    {
        foreach (Body body in this.bodies)
        {
            if (body.IsTracked && trackedBody == null)//Selects first body
            {
                this.trackedBody = body;
            }
        }
    }

    /// <summary>
    /// Calculates the location of joints in the Camera space, and retrieves locations.
    /// </summary>
    public void CalculateJoints()
    {
        if (trackedBody != null)
        {
            var jointData= trackedBody.Joints;
            var jointOrientations = trackedBody.JointOrientations;

            for(int i = 0; i < jointCount; i++)
            {
                CameraSpacePoint position = jointData[jointTypes[i]].Position;
                var rotations = jointOrientations[jointTypes[i]].Orientation;
                if (position.Z < 0)
                {
                    position.Z = 1f;
                }
                jointPositions[i] = new Vector3(position.X, position.Y, position.Z);
                jointRotations[i] = new Quaternion(rotations.X, rotations.Y, rotations.Z, rotations.W);
                jointScales[i] = Vector3.one * jointScale;
            }
        }        
    }

    /// <summary>
    /// Loops through bones to calculate their positons and rotations.
    /// </summary>
    public void CalculateBones()
    {
        for (int i = 0; i < boneCount; i++)
        {
            Vector3 jointA = jointPositions[boneJoints[i, 0]];
            Vector3 jointB = jointPositions[boneJoints[i, 1]];
            CalculateBone(i, jointA, jointB);
        }
    }

    /// <summary>
    /// Calculates the positions and rotations of a bone based on the 2 joints they are meant to connect.
    /// </summary>
    /// <param name="i"> The number of the bone to calculate values for. </param>
    /// <param name="jointA"> The first joint that the bone connects to. </param>
    /// <param name="jointB"> The second joint that the bone connects to. </param>
    public void CalculateBone(int i, Vector3 jointA, Vector3 jointB)
    {
        Vector3 diff = jointA - jointB;
        bonePositions[i] = jointB + diff / 2f;

        float length = diff.magnitude;
        boneScales[i] = new Vector3(boneScale, length/2f, boneScale);

        float newDist = Mathf.Sqrt(Mathf.Pow(diff.x, 2f) + Mathf.Pow(diff.z, 2f));
        float angX = Mathf.Atan2(newDist, diff.y) * Mathf.Rad2Deg;
        float angY = Mathf.Atan2(diff.x, diff.z) * Mathf.Rad2Deg;

        //Set spinal bones y rotation to match bottom joint (As Head joint has no rotation)
        switch (i)
        {
            case 0: //SpineMid to SpineBase (Set to SpineBase)
                angY = jointRotations[0].eulerAngles.y;
                break;
            case 1://Neck to SpineShoulder (Set to SpineShoulder)
                angY = jointRotations[20].eulerAngles.y;
                break;
            case 2://Head to Neck (Set to Neck)
                angY = jointRotations[2].eulerAngles.y;
                break;
            case 19://SpineShoulder to SpineMid (Set to SpineMid)
                angY = jointRotations[1].eulerAngles.y;
                break;
        }

        Vector3 rot = new Vector3(angX, angY, 0f);
        boneRotations[i] = Quaternion.Euler(rot);
    }

    /// <summary>
    /// Reverses the skeleton in the case that scan is selected as the scan of the BACK of the body.
    /// </summary>
    /// <param name="reverseOffsetVectors">
    /// Offset Vectors, retrieved from the GetReverseOffsetVectors called on the SkeletonData from the scan of the FRONT of the body.
    /// 0 - Offset between average of left and right hip joints and spineBase joint
    /// 1 - Offset between spineBase and spineMid
    /// 2 - Offset between spineBase and spineShoulder
    /// 3 - Offset between spineBase and Neck
    /// 4 - Offset between spineBase and head
    /// 5 - Offset between leftFoot and leftAnkle
    /// 6 - Offset between rightFoot and rightAnkle
    /// </param>
    public void ReverseSkeleton(Vector3[] reverseOffsetVectors)
    {


        int[] centreJoints = { 0, 1, 20, 2, 3};

        Vector3 hipAvg = (jointPositions[12] + jointPositions[16])/2;
        jointPositions[centreJoints[0]] = hipAvg + reverseOffsetVectors[0];

        for(int i = 1; i < centreJoints.Length; i++)
        {
            jointPositions[centreJoints[i]] = jointPositions[centreJoints[0]] + reverseOffsetVectors[i];
        }

        jointPositions[15] = jointPositions[14] + reverseOffsetVectors[reverseOffsetVectors.Length - 2];
        jointPositions[19] = jointPositions[18] + reverseOffsetVectors[reverseOffsetVectors.Length - 1];


        int[] reverseOrder = { 0, 1, 2, 3, 8, 9, 10, 11, 4, 5, 6, 7, 16, 17, 18, 19, 12, 13, 14, 15, 20, 23, 24, 21, 22 };
        Vector3[] reverseJointPositions = new Vector3[jointCount];

        for (int i = 0; i < jointCount; i++)
        {
            reverseJointPositions[i] = this.jointPositions[reverseOrder[i]];
        }

        this.jointPositions = reverseJointPositions;
        CalculateBones();
    }

    /// <summary>
    /// Returns the offset vectors for the SkeletonData of the FRONT scan of the body.
    /// </summary>
    /// <returns>
    ///Offset Vectors: 
    /// 0 - Offset between average of left and right hip joints and spineBase joint
    /// 1 - Offset between spineBase and spineMid
    /// 2 - Offset between spineBase and spineShoulder
    /// 3 - Offset between spineBase and Neck
    /// 4 - Offset between spineBase and head
    /// 5 - Offset between leftFoot and leftAnkle
    /// 6 - Offset between rightFoot and rightAnkle
    /// </returns>
    public Vector3[] GetReverseOffsetVectors()
    {
        
        int[] centreJoints = { 0, 1, 20, 2, 3 };
        Vector3 negative = new Vector3(-1, 1, -1);
        Vector3[] reverseOffsetVectors = new Vector3[7];

        Vector3 hipAvg = (jointPositions[12] + jointPositions[16]) / 2;
        reverseOffsetVectors[0] = ReverseVector(jointPositions[0] - hipAvg);

        for(int i = 1; i < 5; i++)
        {
            reverseOffsetVectors[i] = ReverseVector(jointPositions[centreJoints[i]] - jointPositions[0]);
        }

        reverseOffsetVectors[5] = ReverseVector(jointPositions[15] - jointPositions[14]);
        reverseOffsetVectors[6] = ReverseVector(jointPositions[19] - jointPositions[18]);

        return reverseOffsetVectors;
    }

    /// <summary>
    /// Reverses a vector in the x/z plane, leaving the y value unchanged.
    /// </summary>
    /// <param name="input"> The original vector. </param>
    /// <returns> The reversed vector. </returns>
    public Vector3 ReverseVector(Vector3 input)
    {
        Vector3 output = -input;
        output.y = input.y;
        return output;
    }
}
