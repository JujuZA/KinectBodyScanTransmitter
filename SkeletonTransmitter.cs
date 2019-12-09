using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Class for transmitting skeleton data over network.
/// Accessed and controlled by DisplayManager.
/// </summary>
public class SkeletonTransmitter : MonoBehaviour
{

    public int port;
    public string server;

    public ScanSerializer scanSerial;

    public TcpClient client;
    public NetworkStream stream;

    /// <summary>
    /// Connect to server at ip and port specified in the Unity Editors GameObject inspector for SkeletonTransmitter GameObject.
    /// </summary>
    public void Connect()
    {
        try
        {
            this.client = new TcpClient(server, port);
            Debug.Log("Connected to server at " + server + " Port : " + port);
        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Sends the specified SkeletonData over the network as a serializable skeleton (SSkleton) object.
    /// </summary>
    /// <param name="skeleton"> The SkeletonData object that must be sent. </param>
    public void SendSkeletonData(SkeletonData skeleton)
    {
        SJoint[] serialJoints = ScanSerializer.SerializeJoints(skeleton.jointPositions, skeleton.jointRotations);
        SBone[] serialBones = ScanSerializer.SerializeBones(skeleton.bonePositions, skeleton.boneRotations, skeleton.boneScales);
        SSkeleton serialSkeleton = new SSkeleton(serialJoints, serialBones);

        Debug.Log("SkSending");
        try
        {
            stream = client.GetStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, serialSkeleton);
            stream.Flush();
            Debug.Log("SkSent");
        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Closes the stream and disconnects the client.
    /// </summary>
    public void Close()
    {
        try
        {
            stream.Close();
            client.Close();
        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
    }
}
