using UnityEngine;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Class for recieving skeleton data over network.
/// </summary>
public class SkeletonReciever : MonoBehaviour
{

    private Thread serverThread;
    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;

    public int port;

    public SSkeleton skeletonSerial;
    public bool skeletonReady;//For access by Display manager, to update network recieved scan's animation and reset to false when updated.

    /// <summary>
    /// Called when ScanReciever GameObject is initialised by Unity Engine.
    /// 
    /// Starts a thread for the server Listen method.
    /// </summary>
    public void Start()
    {
        Application.runInBackground = true;
        skeletonReady = false;

        serverThread = new Thread(new ThreadStart(Listen));
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    /// <summary>
    /// Server listens for incoming skelton data, and sets skeletonReady value to true when recieved.
    /// </summary>
    private void Listen()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Debug.Log("Listening...");

            while (true)
            {
                this.client = server.AcceptTcpClient();
                Debug.Log("Client accepted");
                this.stream = client.GetStream();
                BinaryFormatter bf = new BinaryFormatter();

                while (client.Connected)
                {
                    this.skeletonSerial = (SSkeleton)bf.Deserialize(stream);
                    Debug.Log("Skeleton is ready!");
                    skeletonReady = true;
                }
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Called when Unity application or build program is closed.
    /// 
    /// Closes the stream, disconnects the client and stops the server.
    /// </summary>
    public void Close()
    {
        stream.Close();
        client.Close();
        server.Stop();
    }
}
