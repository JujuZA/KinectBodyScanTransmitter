using UnityEngine;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

/// <summary>
/// Class for recieving scan data over network.
/// </summary>
public class ScanReciever : MonoBehaviour {

    private Thread serverThread;
    private TcpListener server;
    private TcpClient client;
    private NetworkStream stream;

    public int port;

    public ScanSerializer scanSerial;
    public bool scanReady; //For access by Display manager, to created network recieved scan.

    /// <summary>
    /// Called when ScanReciever GameObject is initialised by Unity Engine.
    /// 
    /// Starts a thread for the server Listen method.
    /// </summary>
    public void Start()
    {
        Application.runInBackground = true;
        scanReady = false;

        serverThread = new Thread(new ThreadStart(Listen));
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    /// <summary>
    /// Server listens for a client sending a scan, recieves the scan and shuts down.
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
                this.scanSerial = (ScanSerializer) bf.Deserialize(stream);
                Debug.Log("Scan is ready!");
                scanReady = true;
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
