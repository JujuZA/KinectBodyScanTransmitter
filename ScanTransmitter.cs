using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Class for transmitting scan data over network.
/// Accessed and controlled by DisplayManager.
/// </summary>
public class ScanTransmitter : MonoBehaviour {

    public int port;
    public string server;

    public ScanSerializer scanSerial;

    public TcpClient client;
    public NetworkStream stream;
	
    /// <summary>
    /// Connect to server at ip and port specified in the Unity Editors GameObject inspector for ScanTransmitter GameObject.
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
    /// Sends the scanSerial object over the network.
    /// </summary>
    public void SendScanData()
    {
        Debug.Log("Sending");
        try
        {
            stream = client.GetStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(stream, scanSerial);
            stream.Flush();
            Debug.Log("Sent");
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
