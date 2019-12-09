using UnityEngine;
using Windows.Kinect;
using System.Collections.Generic;
using System;

/// <summary>
/// Connects to the Kinect sensor, retrieves relevant data streams,
/// processes those streams into the forms required for MeshData and SkeletonData objects,
/// and sends those objects to the DataManager object for storage.
/// </summary>
public class KinectSource : MonoBehaviour
{
    public DataManager dataManager;

    private const int bytesPerPixel = 4;
    private KinectSensor kinectSensor = null;
    private CoordinateMapper coordinateMapper = null;
    private MultiSourceFrameReader multiSourceReader = null;

    private ushort[] depthFrameData = null;
    private byte[] colorFrameData = null;
    private byte[] bodyIndexFrameData = null;
    private Body[] bodies = null;

    public TimeSpan bodyTimeStamp;
    Vector3[,] vertices;
    Vector2[,] uvs;
    int[,] dbIndex;
    int[,] cbIndex;

    private ColorSpacePoint[] colorPoints = null;
    private CameraSpacePoint[] cameraPoints = null;

    private byte[] colorData;

    public int depthWidth = 0;
    public int depthHeight = 0;

    private int colorWidth = 0;
    private int colorHeight = 0;

    private DepthSpacePoint[] depthPoints;
    Color[] cIndex;

    private int bodyIndexWidth = 0;
    private int bodyIndexHeight = 0;

    public bool meshReady;
    public bool colorTextureReady;
    public bool bodyReady;
    public bool bodyIndexReady;

    public GameObject depthMap;

    public Texture2D colorTexture;
    public Texture2D colorBodyIndex;
    public int[] bodyIndexFrameArray;

    /// <summary>
    /// Called when KinectSource GameObject is initialised by the Unity engine.
    /// 
    /// Sets up the sensor to read multiple sources, obtains coordinate maps that are necessary to use different types of Kinect data together, 
    /// and initialises the arrays that store the retrieved data.
    /// </summary>
    void Start()
    {
        kinectSensor = KinectSensor.GetDefault();

        if (kinectSensor != null)
        {
            //Enable reading of multiple sources from kinect data.
            multiSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);

            //Obtain coordinate mappings and depth data dimensions.
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
            FrameDescription depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;

            //Create arrays of the required size to hold scanned data.
            this.depthFrameData = new ushort[depthWidth * depthHeight];
            this.bodyIndexFrameData = new byte[depthWidth * depthHeight];
            this.colorPoints = new ColorSpacePoint[depthWidth * depthHeight];
            this.cameraPoints = new CameraSpacePoint[depthWidth * depthHeight];

            //Create Unity Texture2D in which to store the ColorFrame data.
            FrameDescription colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;
            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;
            colorData = new byte[colorWidth * colorHeight * bytesPerPixel];

            this.depthPoints = new DepthSpacePoint[colorWidth * colorHeight];

            //Create array for body pixel indicators.
            this.bodyIndexFrameArray = new int[depthWidth * depthHeight];

            //Accesses Kinect Sensor
            if (!kinectSensor.IsOpen)
            {
                kinectSensor.Open();
            }
        }
    }

    /// <summary>
    /// Called every frame (display) by the Unity engine.
    /// 
    /// Checks if there is Kinect data available, stores it in the initialised arrays, and tores results by TimeSpan in the DataManager object.
    /// </summary>            
    void Update()
    {
        //Clears flags for previous data.
        bool colorFrameProcessed = false;
        bool depthFrameProcessed = false;
        bool bodyIndexFrameProcessed = false;
        bool bodyFrameProcessed = false;
        bool meshDataProcessed = false;
        
        //Conditional access for Kinect data streams, nested to match interdependencies of streams.
        if (multiSourceReader != null)
        {
            var multiSourceFrame = multiSourceReader.AcquireLatestFrame();
            if (multiSourceFrame != null)
            {
                using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                {
                    using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        using (BodyIndexFrame bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
                        {
                            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                            {
                                //Obtains the depth data stream from the Kinect, if avaiable.
                                //Depth is a set of values showing the relative distance of each point (when mapped on to other sources) in the image from the camera.
                                if (depthFrame != null)
                                {
                                    FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                                    depthWidth = depthFrameDescription.Width;
                                    depthHeight = depthFrameDescription.Height;

                                    if ((depthWidth * depthHeight) == this.depthFrameData.Length)
                                    {
                                        depthFrame.CopyFrameDataToArray(this.depthFrameData);
                                        depthFrameProcessed = true;
                                    }
                                }

                                //Obtains the color data stream from the Kinect, if available.
                                //Color is an array of the RGB values for an HD (1920x1080p) image. 
                                if (colorFrame != null)
                                {
                                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                                    colorWidth = colorFrameDescription.Width;
                                    colorHeight = colorFrameDescription.Height;

                                    if ((colorWidth * colorHeight * bytesPerPixel) == this.colorData.Length)
                                    {
                                        colorTexture = new Texture2D(colorFrameDescription.Width, colorFrameDescription.Height, TextureFormat.RGBA32, false);
                                        colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Rgba);
                                        colorTexture.LoadRawTextureData(colorData);
                                        colorTexture.Apply();
                                        colorFrameProcessed = true;
                                    }
                                }

                                //Obtains the body index data stream from the Kinect, if available.
                                //Body index shows which points (when mapped onto other sources) contain a person as recognised by the Kinect.
                                if (bodyIndexFrame != null)
                                {
                                    FrameDescription bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;
                                    bodyIndexWidth = bodyIndexFrameDescription.Width;
                                    bodyIndexHeight = bodyIndexFrameDescription.Height;

                                    if ((bodyIndexWidth * bodyIndexHeight) == this.bodyIndexFrameData.Length)
                                    {
                                        bodyIndexFrame.CopyFrameDataToArray(this.bodyIndexFrameData);
                                        
                                        for (int i = 0; i < bodyIndexFrameData.Length; i++)
                                        {
                                            bodyIndexFrameArray[i] = (bodyIndexFrameData[i] != 0xff) ? 1 : 0;
                                        }
                                        bodyIndexFrameProcessed = true;
                                    }
                                }

                                //Obtains the body data stream from the Kinect, if available.
                                //Body data stream is the locations and rotations of a set of joints in a representation of a human body, as recognised by the kinect,
                                //as well as other representational data (hand gestures, confidence, etc) that is not used here.
                                if (bodyFrame != null)
                                {                                    
                                    
                                    if (this.bodies == null)
                                    {
                                        this.bodies = new Body[bodyFrame.BodyCount];
                                    }

                                    bodyTimeStamp = bodyFrame.RelativeTime;
                                    bodyFrame.GetAndRefreshBodyData(this.bodies);                               
                                    
                                    bodyFrameProcessed = true;
                                }

                                //When the three data streams necessary to create a mesh are present, the data is processed to be stored as MeshData.
                                if (depthFrameProcessed && colorFrameProcessed && bodyIndexFrameProcessed)
                                {
                                    vertices = new Vector3[depthWidth, depthHeight];
                                    uvs = new Vector2[depthWidth, depthHeight];
                                    dbIndex = new int[depthWidth, depthHeight];
                                    cbIndex = new int[colorWidth, colorHeight];
                                    cIndex = new Color[colorWidth * colorHeight];

                                    //Populate coordinate mapping arrays between depth/color and depth/camera spaces.
                                    //(Camera space being coordinate system with camera at the origin.)
                                    this.coordinateMapper.MapDepthFrameToColorSpace(this.depthFrameData, this.colorPoints);
                                    this.coordinateMapper.MapDepthFrameToCameraSpace(this.depthFrameData, this.cameraPoints);

                                    //Mapping is done between data streams, and non-body depth data is discarded.
                                    //Depth points are stored as vertices.
                                    //Color mappings to depth points are stored as uvs.
                                    //BodyIndex is kept for use in further processing.
                                    for (int x = 0; x < depthWidth; x++)
                                    {
                                        for (int y = 0; y < depthHeight; y++)
                                        {
                                            int depthIndex = x + y * depthWidth;
                                            byte bodyPixel = this.bodyIndexFrameData[depthIndex];

                                            CameraSpacePoint cameraPoint = this.cameraPoints[depthIndex];
                                            ColorSpacePoint colorPoint = this.colorPoints[depthIndex];

                                            Vector3 vertex = new Vector3(cameraPoint.X, cameraPoint.Y, cameraPoint.Z);
                                            Vector2 uv = new Vector2(colorPoint.X / colorWidth, colorPoint.Y / colorHeight);
                                            int bodyPixelVal = 0;

                                            if (bodyPixel != 0xff)
                                            {
                                                bodyPixelVal = 1;
                                            }
                                            else
                                            {
                                                vertex = Vector3.zero;
                                            }

                                            vertices[x, y] = vertex;
                                            uvs[x, y] = uv;
                                            dbIndex[x, y] = bodyPixelVal;                                            
                                        }
                                    }

                                    //Populate coordinate mapping arrays between depth and color spaces.
                                    this.coordinateMapper.MapColorFrameToDepthSpace(this.depthFrameData, depthPoints);

                                    //Maps the body index stream into a masking map for the color stream.
                                    for (int x = 0; x < colorWidth; x++)
                                    {
                                        for (int y = 0; y < colorHeight; y++)
                                        {
                                            int colorIndex = x + y * colorWidth;
                                            int depthX = (int)depthPoints[colorIndex].X;
                                            int depthY = (int)depthPoints[colorIndex].Y;
                                            int depthIndex = depthX + depthY * depthWidth;

                                            int colorIndexVal = 0;

                                            if ((depthIndex > 0) && (depthIndex < depthWidth * depthHeight))
                                            {
                                                byte bodyPixel = this.bodyIndexFrameData[depthIndex];
                                                if (bodyPixel != 0xff)
                                                {
                                                    colorIndexVal = 1;
                                                }
                                            }

                                            cbIndex[x, y] = colorIndexVal;
                                        }
                                    }

                                    meshDataProcessed = true;
                                }

                                // If mesh has been processed and the body frame (required to create skeleton model) are both available,
                                // MeshData and SkeletonData objects are created, and passed to the data manager to be stored under the corresponding TimeSpan.
                                // flags are reset.
                                if (meshDataProcessed && bodyFrameProcessed)
                                {
                                    MeshData meshData = new MeshData(bodyTimeStamp, vertices, uvs, dbIndex, cbIndex, colorTexture);
                                    SkeletonData skeletonData = new SkeletonData(bodyTimeStamp, bodies);
                                    dataManager.AddToList(bodyTimeStamp, meshData, skeletonData);
                                    meshDataProcessed = false;
                                    depthFrameProcessed = false;
                                    colorFrameProcessed = false;
                                    bodyIndexFrameProcessed = false;
                                    bodyFrameProcessed = false;
                                }
                                else
                                {
                                    string s = "Proccessing Error Occurred. The following failed : ";
                                    if (!depthFrameProcessed) s = s + "Depth, "; 
                                    if (!colorFrameProcessed) s = s + "Color, "; 
                                    if (!bodyIndexFrameProcessed) s = s + "Body Index, "; 
                                    if (!bodyFrameProcessed) s = s + "Body (Skeleton), ";
                                    if (!meshDataProcessed) s = s + "Mesh Data";
                                    //Debug.Log(s);
                                }
                                
                            }
                        }
                    }
                }
                multiSourceFrame = null;
            }
        }
    }

    /// <summary>
    /// Called when Unity application or build program is closed.
    /// 
    /// Closes the data streams from the Kinects and stops the application accessing the sensor.
    /// </summary>
    void OnApplicationQuit()
    {
        if (multiSourceReader != null)
        {
            multiSourceReader.Dispose();
            multiSourceReader = null;
        }

        if (kinectSensor != null)
        {
            if (kinectSensor.IsOpen)
            {
                kinectSensor.Close();
            }

            kinectSensor = null;
        }
    }

    //DEBUGGING
    void DrawDepthMap()
    {
            Texture2D texture = new Texture2D(depthWidth, depthHeight, TextureFormat.BGRA32, false);
            byte[] RawData = new byte[depthWidth * depthHeight * 4];
            int index = 0;

            for (int x = 0; x < depthWidth; x++)
            {
                for (int y = 0; y < depthHeight; y++)
                {
                    int depthIndex = x + y * depthWidth;

                    byte intensity = (byte)(depthFrameData[depthIndex] >> 8);
                    RawData[index++] = intensity;
                    RawData[index++] = intensity;
                    RawData[index++] = intensity;
                    RawData[index++] = 255;

                    texture.LoadRawTextureData(RawData);
                    texture.Apply();
                    depthMap.GetComponent<Renderer>().material.SetTexture("_MainTex", texture);

            }
        }
    }
}
