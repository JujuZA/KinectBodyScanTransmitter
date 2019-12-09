using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using Windows.Kinect;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

/// <summary>
/// Class that manages the display, as well as the processing that uses the Unity Engine's GameObject nesting.
/// </summary>
public class DisplayManager : MonoBehaviour {

    //Networking
    public bool networked;

    public GameObject scanRec;
    private ScanReciever scanReciever;
    public GameObject scanTrans;
    private ScanTransmitter scanTransmitter;
    public ScanSerializer scanSerial;

    public GameObject skeletonRec;
    private SkeletonReciever skeletonReciever;
    public GameObject skeletonTrans;
    private SkeletonTransmitter skeletonTransmitter;

    //Kinect Data
    public GameObject kinectSource;
    private KinectSource kinectSourceManager;
    private DataManager dataManager;

    //Feet and Hands prefabricated assets
    public GameObject footSource;
    public GameObject handSource;

    public Material footMaterial;
    public Material handMaterial;

    private Mesh footMesh;
    private Mesh handMesh;

    private GameObject rightHand;
    private GameObject leftHand;
    private GameObject rightFoot;
    private GameObject leftFoot;

    private int[] handEdgeHooks;
    private int[] footEdgeHooks;

    //Display Options
    public bool showMesh;
    public bool showSkeleton;
    private bool updatingPartialMeshes;
    private bool updatingNetworkMeshes;

    //Snapshot Times
    private TimeSpan showingTime;
    private TimeSpan frontTime;
    private TimeSpan backTime;

    //Object for combined body scan
    private BodyModel bodyModel;

    //Meshes for live display, back and front scans
    public GameObject bodyMesh;
    public GameObject frontMesh;
    public GameObject backMesh;

    //Skeletons for live display, back and front scans
    public GameObject skeleton;
    private GameObject[] skeletonJoints;
    private GameObject[] skeletonBones;

    public GameObject frontSkeleton;
    private GameObject[] frontJoints;
    private GameObject[] frontBones;

    public GameObject backSkeleton;
    private GameObject[] backJoints;
    private GameObject[] backBones;

    //The body scan for reanimation
    public GameObject bodyScan;
    public GameObject[] partialMeshHolders;

    //Mesh cages constructed from combined scans and the skeleton that moves them
    public GameObject[] animationSkeleton;
    public GameObject[] animationMeshes;

    //Meshes that link the mesh-cages and are deformed to follow movement
    private List<GameObject> linkingMeshes;
    private List<List<GameObject>> linkingMeshHooks;
    private List<List<int>> linkingMeshGroups;
    private List<List<int>> linkingMeshIndices;

    //Submesh variables (Alignment purposes)
    private bool boneMeshUpdates = false;
    private GameObject[] frontMeshes;

    //Retrieved Serial Data
    public GameObject[] serialPartialMeshHolders;
    public SJoint[] serialJoints;
    public SBone[] serialBones;
    public GameObject[][] serialLinkingMeshHooks;
    public GameObject[] serialLinkingMeshes;

    public GameObject[] spineDir;

    /// <summary>
    /// Called when the DisplayManager GameObect is initialised by the Unity engine.
    /// 
    /// Connects to the DataManager and KinectSource, Sets up storage arrays for mesh and skeleton data, and Processes prefabricated hand and feet objects.
    /// </summary>
    void Start()
    {
        Application.runInBackground = true;

        this.scanReciever = scanRec.GetComponent<ScanReciever>();
        this.scanTransmitter = scanTrans.GetComponent<ScanTransmitter>();
        this.skeletonReciever = skeletonRec.GetComponent<SkeletonReciever>();
        this.skeletonTransmitter = skeletonTrans.GetComponent<SkeletonTransmitter>();

        this.kinectSourceManager = kinectSource.GetComponent<KinectSource>();
        this.dataManager = kinectSourceManager.dataManager;

        if (!networked)
        {
            //Joint and Bone object arrays for reference
            this.skeletonJoints = new GameObject[SkeletonData.jointCount];
            this.skeletonBones = new GameObject[SkeletonData.boneCount];
            this.frontJoints = new GameObject[SkeletonData.jointCount];
            this.frontBones = new GameObject[SkeletonData.boneCount];
            this.backJoints = new GameObject[SkeletonData.jointCount];
            this.backBones = new GameObject[SkeletonData.boneCount];

            //Creating mesh for display
            if (showMesh == true)
            {
                bodyMesh = CreateMesh(Vector3.zero, "Scan", "Standard");
            }

            //Creating skeleton (joints and bones) for display
            if (showSkeleton == true)
            {
                this.skeleton = CreateSkeleton(Vector3.zero, PrimitiveType.Sphere, PrimitiveType.Capsule, "Skeleton", skeletonJoints, skeletonBones);
            }

        }

        //Load the prefabricated meshes for hand and feet
        LoadHandsAndFeet();

        //Denies the condition for updating the partial meshes (which don't exist yet)
        this.updatingPartialMeshes = false;
    }

    /// <summary>
    /// Called every frame (display) by the Unity engine.
    /// 
    /// Allows for user input to select front and back meshes, process combined mesh, and send body scan data over the network.
    /// </summary>
    void Update()
    {
        //Select front mesh
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown("joystick 1 button 0"))
        {
            SetFrontMesh(this.showingTime);
        }

        //Select back mesh
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown("joystick 1 button 2")) 
        {
            SetBackMesh(this.showingTime);
        }

        //Process selected meshes into a body model
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown("joystick 1 button 3"))
        {
            this.bodyModel = new BodyModel(dataManager, frontTime, backTime);
            UpdateBodyModel();
            BuildBodyScan();

            this.updatingPartialMeshes = true;
        }

        //Send body scan over network and initiates animation transmission
        if (Input.GetKeyDown(KeyCode.S))
        {
            this.scanTransmitter.Connect();
            this.scanTransmitter.scanSerial = this.scanSerial;
            this.scanTransmitter.SendScanData();
            this.scanTransmitter.Close();

            this.skeletonTransmitter.Connect();
            this.updatingNetworkMeshes = true;
        }
    }

    /// <summary>
    /// Called approximately every 0.02 seconds by the Unity Engine.
    /// 
    /// Updates the display of the meshes and skeletons (and transmits skeletondata) if client side, and checks for network recieved data on server side.
    /// </summary>
    void FixedUpdate()
    {
        //Updating Mesh and Skeleton from live kinect data
        if ((showingTime != this.dataManager.currentTime) && !networked) //Client side, not for general user interaction.
        {
                if (showMesh)//Selectable in the Unity Inspector for the DisplayManager GameObject.
                {
                    UpdateMesh(dataManager.currentTime, this.bodyMesh);
                }

                if (showSkeleton)//Selectable in the Unity Inspector for the DisplayManager GameObject.
                {
                    UpdateSkeleton(dataManager.currentTime, this.skeletonJoints, this.skeletonBones);
                }

                if (updatingPartialMeshes)//True once meshes have been procesed
                {
                    UpdatePartialMeshes(dataManager.currentTime);
                }

                if (updatingNetworkMeshes)//True once scan data has been transmitted
                {
                    this.skeletonTransmitter.SendSkeletonData(this.dataManager.GetSkeletonAtTime(dataManager.currentTime));
                }

            showingTime = this.dataManager.currentTime;
        }

        if (networked) //Server side, networked display of scan.
        {
            if (this.scanReciever.scanReady)//Set when a scan is recieved.
            {
                CreateBodyScanFromSerial(this.scanReciever.scanSerial);
                this.scanReciever.scanReady = false;
            }

            if (this.skeletonReciever.skeletonReady)//Set whenever a skeleton is recieved.
            {
                UpdateNetworkPartialMeshes(this.skeletonReciever.skeletonSerial);
                this.skeletonReciever.skeletonReady = false;
            }
        }
    }

    /// <summary>
    /// Loads the prefabricated hands and feet and sets their edge hooks.
    /// </summary>
    public void LoadHandsAndFeet()
    {
        ResetMesh(this.handSource);

        //Create right and left copies of hand
        this.rightHand = GameObject.Instantiate(handSource);
        this.leftHand = GameObject.Instantiate(handSource);
        this.rightHand.name = "RightHand";
        this.leftHand.name = "LeftHand";

        SetToBase(this.rightHand, true);//Sets the right hands position and alignment, which is flipped
        SetToBase(this.leftHand, false);//Sets the left hands position and alignment, which is not flipped

        ResetMesh(this.rightHand);
        SetToBase(this.rightHand, false);

        Vector3[] rightHandVertices = this.rightHand.GetComponent<MeshFilter>().sharedMesh.vertices;
        List<int> newHandEdgeHooks = new List<int>();
        for (int i = 0; i < rightHandVertices.Length; i += 1)
        {
            if (rightHandVertices[i].y < -0.067 && rightHandVertices[i].y > -0.079)
            {
                newHandEdgeHooks.Add(i);
            }
        }
        this.handEdgeHooks = newHandEdgeHooks.ToArray();
        
        ResetMesh(this.footSource);
        GenerateUVs(this.footSource);

        this.rightFoot = GameObject.Instantiate(footSource);
        this.leftFoot = GameObject.Instantiate(footSource);
        this.rightFoot.name = "RightFoot";
        this.leftFoot.name = "LeftFoot";

        SetToBase(this.rightFoot, true);//Sets the right foots position and alignment, which is flipped
        SetToBase(this.leftFoot, false);//Sets the left foots position and alignment, which is not flipped

        ResetMesh(this.rightFoot);
        SetToBase(rightFoot, false);

        Vector3[] rightFootVertices = this.rightFoot.GetComponent<MeshFilter>().sharedMesh.vertices;
        List<int> newFootEdgeHooks = new List<int>();
        for (int i = 0; i < rightFootVertices.Length; i += 1)
        {
            if (rightFootVertices[i].y > 0.085)
            {
                newFootEdgeHooks.Add(i);
            }
        }
        this.footEdgeHooks = newFootEdgeHooks.ToArray();

        //Remove source objects
        Destroy(handSource);
        Destroy(footSource);

        //Hide created objects until scan is ready
        this.rightHand.SetActive(false);
        this.leftHand.SetActive(false);
        this.rightFoot.SetActive(false);
        this.leftFoot.SetActive(false);
    }

    /// <summary>
    /// Uses unity's combine mesh feature to produce a mesh with vertices reset to their global location, based on the Meshes GameObject's global rotation and position.
    /// </summary>
    /// <param name="meshObject"></param>
    public void ResetMesh(GameObject meshObject)
    {
        CombineInstance[] combiner = new CombineInstance[1];
        combiner[0] = GetCombineInstance(meshObject);//Create combine instance for handsource (in order to relocate mesh points)

        Mesh resetMesh = new Mesh();
        resetMesh.CombineMeshes(combiner);
        meshObject.GetComponent<MeshFilter>().sharedMesh = resetMesh;
    }

    /// <summary>
    /// Creates UVs for the prefabricated object based on vertex position. (Only used for foot object)
    /// </summary>
    /// <param name="meshObject"> The prefabricated object. </param>
    public void GenerateUVs(GameObject meshObject)
    {
        MeshFilter mf = meshObject.GetComponent<MeshFilter>();
        Mesh mesh = mf.sharedMesh;

        Vector2[] uvs = new Vector2[mesh.vertexCount];
        float[] xUV = new float[mesh.vertexCount];
        float[] yUV = new float[mesh.vertexCount];

        for(int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 v = mesh.vertices[i];
            xUV[i] = v.x * v.z;
            yUV[i] = v.y;
        }

        float maxX = xUV.Max();
        float minX = xUV.Min();

        float maxY = yUV.Max();
        float minY = yUV.Min();

        for (int i = 0; i < mesh.vertexCount; i++) uvs[i] = new Vector2(((xUV[i] - minX) / (maxX - minX)), ((yUV[i] - minY) / (maxY - minY)));

        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    /// <summary>
    /// Removes child object from parent and deletes it.
    /// </summary>
    /// <param name="childObject"> THe child object to be removed and deleted. </param>
    public void ExciseChild(GameObject childObject)
    {
        childObject.name = "Done";
        childObject.transform.parent = null;
        Destroy(childObject);
    }

    /// <summary>
    /// Creates a Unity CombineInstance object for the CombineMeshes method.
    /// </summary>
    /// <param name="meshObject"> The mesh object to be combined. (Used for position reset in this system.) </param>
    /// <returns> Unity CombineInstance object for the CombineMeshes method. </returns>
    public CombineInstance GetCombineInstance(GameObject meshObject)
    {
        CombineInstance combiner = new CombineInstance();

        MeshFilter mf = meshObject.GetComponent<MeshFilter>();
        combiner.subMeshIndex = 0;
        combiner.mesh = mf.mesh;
        combiner.transform = mf.transform.localToWorldMatrix;

        return combiner;
    }

    /// <summary>
    /// Sets the specified obect to it's base position, rotation and scale. Reflects along x-axis if required to obtain left/right hands/feet.
    /// </summary>
    /// <param name="obj"> The object to be set to base position. </param>
    /// <param name="reverse"> Wheter a reflection is required. </param>
    public void SetToBase(GameObject obj, bool reverse)
    {
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        Vector3 scale = reverse ? Vector3.one - 2 * Vector3.right : Vector3.one;
        obj.transform.localScale = scale;
    }

    /// <summary>
    /// Sets GameObject's rotation to match that of specified GameObject with the same parent transform.
    /// </summary>
    /// <param name="obj"> The speficied GameObject. </param>
    /// <param name="aligner"> The name of the other GameObject with the same parent.</param>
    public void AlignToFellowChild(GameObject obj, String aligner)
    {
        Transform alignTrans= obj.transform.parent.Find(aligner);
        obj.transform.localRotation = alignTrans.localRotation;
    }

    /// <summary>
    /// Creates an empty GameObject for a mesh at a given position.
    /// </summary>
    /// <param name="pos"> Position of mesh GameObject. </param>
    /// <param name="name"> Name of mesh GameObject. </param>
    /// <param name="shader"> Type of shader for mesh to use. </param>
    /// <returns> GameObject for mesh display. </returns>
    public GameObject CreateMesh(Vector3 pos, string name, string shader)
    {
        GameObject meshObject = new GameObject();
        meshObject.name = name;
        meshObject.AddComponent<MeshFilter>();
        meshObject.AddComponent<MeshRenderer>();

        meshObject.transform.localPosition = pos;

        Material mat = new Material(Shader.Find(shader));
        meshObject.GetComponent<Renderer>().material = mat;
        meshObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(1, 1));

        return meshObject;
    }

    /// <summary>
    /// Updates a mesh GameObject to display specified MeshData.
    /// </summary>
    /// <param name="time"> The time stamp of the MeshData object to display. </param>
    /// <param name="meshObject"> The GameObject created to display the mesh. </param>
    void UpdateMesh(TimeSpan time, GameObject meshObject)
    {
        //Retrieve and clear current Mesh
        MeshFilter mf = meshObject.GetComponent<MeshFilter>();
        mf.mesh.Clear();

        //Load new meshData and generate mesh
        MeshData m = dataManager.GetMeshAtTime(time);
        Mesh newMesh = m.GenerateMesh(1);

        //Recalculate normals and assign mesh
        newMesh.RecalculateNormals();
        mf.mesh = newMesh;

        //Load texture and apply
        Texture tex = m.texture;
        meshObject.GetComponent<Renderer>().material.SetTexture("_MainTex", tex);
    }

    /// <summary>
    /// Creates a skeleton GameObject at a specified position, and the joints and bones it contains.
    /// </summary>
    /// <param name="pos"> The position of the skeleton GameObject. </param>
    /// <param name="jointShape"> The type of 3D primitive GameObject to represent the joints. </param>
    /// <param name="boneShape"> The type of 3D primitive GameObject to represent the bones. </param>
    /// <param name="name"> The name for the skeleton GameObject. </param>
    /// <param name="joints"> The global array for the storage of joints. </param>
    /// <param name="bones"> The global array for the storage of bones. </param>
    /// <returns> GameObject for Skeleton Display. </returns>
    GameObject CreateSkeleton(Vector3 pos, PrimitiveType jointShape, PrimitiveType boneShape, string name, GameObject[] joints, GameObject[] bones)
    {
        GameObject newSkeleton = new GameObject();
        CreateJoints(pos, jointShape, joints, newSkeleton);
        CreateBones(pos, boneShape, bones, newSkeleton);
        newSkeleton.name = name;
        return newSkeleton;
    }

    /// <summary>
    /// Updates a skeleton GameObject to display specified SkeletonData.
    /// </summary>
    /// <param name="time"> The time stamp of the SkeletonData object to display. </param>
    /// <param name="joints"> The array of joints to be updated. </param>
    /// <param name="bones"> The array of bones to be updated. </param>
    void UpdateSkeleton(TimeSpan time, GameObject[] joints, GameObject[] bones)
    {
        SkeletonData skeleton = dataManager.GetSkeletonAtTime(time);
        UpdateJoints(skeleton, joints);
        UpdateBones(skeleton, bones);
    }

    /// <summary>
    /// Creates a set of 3D primitive GameObjects to represent joints.
    /// </summary>
    /// <param name="pos"> The default starting position of the joints. </param>
    /// <param name="jointShape"> The type of 3D primitive GameObject that is to represent the joints. </param>
    /// <param name="joints"> The global array to keep track of joints. </param>
    /// <param name="parentSkeleton"> The skeleton GameObject which is to be the parent of the joint container. </param>
    void CreateJoints(Vector3 pos, PrimitiveType jointShape, GameObject[] joints, GameObject parentSkeleton)
    {
        GameObject j = new GameObject();

        string[] jointNames = SkeletonData.jointNames;
        Color[] jointColors = SkeletonData.jointColors;

        for (int i = 0; i < SkeletonData.jointCount; i++)
        {
            GameObject joint = GameObject.CreatePrimitive(jointShape);
            joints[i] = joint;
            joint.name = jointNames[i];
            joint.GetComponent<Renderer>().material.color = jointColors[i];
            joint.transform.parent = j.transform;
        }

        j.name = "Joints";
        j.transform.position = pos;
        j.transform.parent = parentSkeleton.transform;
    }

    /// <summary>
    /// Updates the joints to display joint positions and rotations from specified SkeletonData.
    /// </summary>
    /// <param name="skeleton"> The SkeletonData object to display. </param>
    /// <param name="joints"> The global array to keep track of joints. </param>
    void UpdateJoints(SkeletonData skeleton, GameObject[] joints)
    {
        Vector3[] positions = skeleton.jointPositions;
        Quaternion[] rotations = skeleton.jointRotations;
        Vector3[] scales = skeleton.jointScales;
        for(int i = 0; i < SkeletonData.jointCount; i++)
        {
            joints[i].transform.localPosition = positions[i];
            joints[i].transform.rotation = rotations[i];
            joints[i].transform.localScale = scales[i];
        }
    }

    /// <summary>
    /// Creates a set of 3D primitive GameObjects to represent bones. 
    /// </summary>
    /// <param name="pos"> The default starting position of the bones. </param>
    /// <param name="boneShape"> The type of 3D primitive GameObject that is to represent the bones. </param>
    /// <param name="bones"> The global array to keep track of bones. </param>
    /// <param name="parentSkeleton"> The skeleton GameObject which is to be the parent of the bone container. </param>
    void CreateBones(Vector3 pos, PrimitiveType boneShape, GameObject[] bones, GameObject parentSkeleton)
    {
        GameObject b = new GameObject();

        string[] boneNames = SkeletonData.boneNames;
        Color[] boneColors = SkeletonData.boneColors;

        for (int i = 0; i < SkeletonData.boneCount; i++)
        {
            GameObject bone = GameObject.CreatePrimitive(boneShape);
            bones[i] = bone;
            bone.name = boneNames[i];
            bone.GetComponent<Renderer>().material.color = boneColors[i];
            bone.transform.parent = b.transform;
        }

        b.name = "Bones";
        b.transform.position = pos;
        b.transform.parent = parentSkeleton.transform;
    }

    /// <summary>
    /// Updates the joints to display bone scales, positions and rotations from specified SkeletonData.
    /// </summary>
    /// <param name="skeleton"> The SkeletonData object to display. </param>
    /// <param name="bones"> The global array to keep track of bones. </param>
    void UpdateBones(SkeletonData skeleton, GameObject[] bones)
    {
        Vector3[] positions = skeleton.bonePositions;
        Quaternion[] rotations = skeleton.boneRotations;
        Vector3[] scales = skeleton.boneScales;
        
        for (int i = 0; i < SkeletonData.boneCount; i++)
        {
            bones[i].transform.localPosition = positions[i];
            bones[i].transform.rotation = rotations[i];
            bones[i].transform.localScale = scales[i];
        }
    }

    /// <summary>
    /// Stores the mesh selected by operator as the front mesh.
    /// </summary>
    /// <param name="now"> The time stamp at the time of user selection. </param>
    public void SetFrontMesh(TimeSpan now)
    {
        this.frontTime = now;

        this.frontSkeleton = CreateSkeleton(Vector3.left * 3, PrimitiveType.Sphere, PrimitiveType.Capsule, "Front", frontJoints, frontBones);
        UpdateSkeleton(now, frontJoints, frontBones);

        this.frontMesh = CreateMesh(Vector3.left * 3, "Front", "Standard");//"Particles/VertexLit Blended");
        UpdateMesh(now, this.frontMesh);
    }

    /// <summary>
    /// Stores the mesh selected by the operator as the back mesh.
    /// </summary>
    /// <param name="now"> The time stamp at the time of user selection. </param>
    public void SetBackMesh(TimeSpan now)
    {
        this.backTime = now;

        this.backSkeleton = CreateSkeleton(Vector3.right * 3, PrimitiveType.Sphere, PrimitiveType.Capsule, "Back", backJoints, backBones);

        //Reverses Skeleton to align joints with front skeleton
        this.dataManager.GetSkeletonAtTime(now).ReverseSkeleton(dataManager.GetSkeletonAtTime(frontTime).GetReverseOffsetVectors());
        this.dataManager.GetMeshAtTime(now).ReverseTexture();

        UpdateSkeleton(now, backJoints, backBones);

        this.backMesh = CreateMesh(Vector3.right * 3, "Back", "Standard");//"Particles/VertexLit Blended");
        UpdateMesh(now, this.backMesh);
    }

    /// <summary>
    /// Processes the two chosen mesh by creating and updating the body model and creating a combined mesh.
    /// </summary>
    public void UpdateBodyModel()
    {
        this.bodyModel.PrepareMeshes();
        UpdateMesh(this.bodyModel.frontMesh.timeStamp, this.frontMesh);
        UpdateMesh(this.bodyModel.backMesh.timeStamp, this.backMesh);

        UpdateSkeleton(this.bodyModel.frontSkeleton.timeStamp, this.skeletonJoints, this.skeletonBones);

        this.bodyModel.JoinTextures();
        this.bodyModel.CalcGroupedSubMeshes(true);
        this.bodyModel.CalcGroupedSubMeshes(false);
        this.bodyModel.CalculateGroupTextures();
    }
    
    /// <summary>
    /// Segments, cleans, combines and links front and back meshes to make a skeleton animatable body scan.
    /// </summary>
    public void BuildBodyScan()
    {
        //Disable updating of main skeleton
        this.showMesh = false;
        this.showSkeleton = false;

        //Destroy Original Scan
        Destroy(this.bodyMesh);
        Destroy(this.skeleton);
        this.boneMeshUpdates = true;

        //Visual aid (to what extend joints are distanced from eacher 1 for default/to scale view, bigger for debugging)
        int scale = 1;

        //Retrieve submeshes from bodyModel
        Mesh[] frontGroupMeshes = bodyModel.GetGroupedSubMeshes(true);
        Mesh[] backGroupMeshes = bodyModel.GetGroupedSubMeshes(false);

        //Concatenated lists of joint and bone objects, and their names
        GameObject[] frontPOIs = frontJoints.Concat(frontBones).ToArray();
        GameObject[] backPOIs = backJoints.Concat(backBones).ToArray();
        string[] groupNames = BodyModel.groupNames;
        int[] textureGroups = BodyModel.textureGroups;
        int gc = this.bodyModel.gc;
        GameObject[] frontGroupPOIs = GetGroupedPOIs(frontPOIs, BodyModel.groupPOIs);
        GameObject[] backGroupPOIs = GetGroupedPOIs(backPOIs, BodyModel.groupPOIs);

        //Container hierarchy for partial meshes
        GameObject frontAligner = new GameObject("FrontAligner");
        GameObject backAligner = new GameObject("BackAligner");
        GameObject aligner = new GameObject("Aligner");
        GameObject[] frontPMHolders = new GameObject[frontGroupMeshes.Length];
        GameObject[] backPMHolders = new GameObject[backGroupMeshes.Length];
        GameObject[] PMHolders = new GameObject[frontGroupMeshes.Length];

        //Array for storing the submesh objects
        GameObject[] frontMeshObjects = new GameObject[frontGroupMeshes.Length];
        GameObject[] backMeshObjects = new GameObject[backGroupMeshes.Length];
        GameObject[] combinedMeshObjects = new GameObject[frontGroupMeshes.Length];

        for (int i = 0; i < frontGroupMeshes.Length; i++)
        {
            //Create containers for bones/joints and corresponding meshes
            frontPMHolders[i] = new GameObject(groupNames[i]);
            frontPMHolders[i].transform.parent = frontAligner.transform;

            //Create mesh game object
            frontMeshObjects[i] = CreateMesh(Vector3.zero, groupNames[i], "Standard");//"Particles/VertexLit Blended");

            //Populate mesh game object with mesh data from body model
            MeshFilter mf = frontMeshObjects[i].GetComponent<MeshFilter>();
            mf.mesh = frontGroupMeshes[i];
            Texture tex = bodyModel.frontMesh.texture;
            frontMeshObjects[i].GetComponent<Renderer>().material.SetTexture("_MainTex", tex);

            //Obtain position and rotaion of relevant bone/joint
            Vector3 pos = frontGroupPOIs[i].transform.position;
            Quaternion rot = frontGroupPOIs[i].transform.localRotation;

            //Move joint to origin to align with mesh
            frontGroupPOIs[i].transform.position = Vector3.zero;

            //Place bone/joint and mesh objects in the container object
            frontGroupPOIs[i].transform.parent = frontPMHolders[i].transform;
            frontMeshObjects[i].transform.parent = frontPMHolders[i].transform;

            //Move container back to recentered location
            frontPMHolders[i].transform.localPosition = (pos - (Vector3.left * 3)) * scale;

        }

        for (int i = 0; i < backGroupMeshes.Length; i++)
        {
            //Create containers for bones/joints and corresponding meshes
            backPMHolders[i] = new GameObject(groupNames[i]);
            backPMHolders[i].transform.parent = backAligner.transform;

            //Create mesh game object
            backMeshObjects[i] = CreateMesh(Vector3.zero, groupNames[i], "Standard");//"Particles/VertexLit Blended");


            //Populate mesh game object with mesh data from body model
            MeshFilter mf = backMeshObjects[i].GetComponent<MeshFilter>();
            mf.mesh = backGroupMeshes[i];
            Texture tex = bodyModel.frontMesh.texture;
            backMeshObjects[i].GetComponent<Renderer>().material.SetTexture("_MainTex", tex);

            //Obtain position and rotaion of relevant bone / joint
            Vector3 pos = backGroupPOIs[i].transform.position;
            Quaternion rot = backGroupPOIs[i].transform.localRotation;

            //Move joint to origin to align with mesh
            backGroupPOIs[i].transform.position = Vector3.zero;

            //Place bone/joint and mesh objects in the container object
            backGroupPOIs[i].transform.parent = backPMHolders[i].transform;
            backMeshObjects[i].transform.parent = backPMHolders[i].transform;

            //Move container back to recentered location
            backPMHolders[i].transform.localPosition = (pos - (Vector3.right * 3)) * scale;

            backPMHolders[i].transform.Rotate(new Vector3(0, 180, 0));
        }

        for (int i = 0; i < PMHolders.Length; i++)
        {
            //Create combined partial mesh holder
            PMHolders[i] = new GameObject(groupNames[i]);
            PMHolders[i].transform.parent = aligner.transform;

            //Get old positions and rotations of front POI's
            Vector3 oldPos = frontGroupPOIs[i].transform.position;
            Quaternion oldRot = frontGroupPOIs[i].transform.rotation;

            //Align back and combined pmHolders with front pmholders
            backPMHolders[i].transform.localPosition = frontPMHolders[i].transform.localPosition;
            PMHolders[i].transform.localPosition = frontPMHolders[i].transform.localPosition;

            //Move front and back mesh objects into combined PMHolder
            frontMeshObjects[i].transform.parent = PMHolders[i].transform;
            backMeshObjects[i].transform.parent = PMHolders[i].transform;
            frontGroupPOIs[i].transform.parent = PMHolders[i].transform;

            //Adjust for head (remove if better scan removes problem)
            if (i == 1)
            {
                //Save original position, and move to adjustment position
                Vector3 resetPos = backMeshObjects[i].transform.position;
                backMeshObjects[i].transform.position = backMeshObjects[i].transform.position + AlignByAverages(frontMeshObjects[i], backMeshObjects[i], 0.15f);

                //Save original orientation, and move to adjustment orentation
                Quaternion obr = backMeshObjects[i].transform.localRotation;
                backMeshObjects[i].transform.localRotation = Quaternion.Euler(0f, 180f, 0);

                //Calculate new points at position
                Vector3[] newPoints = GetGlobalVertices(backMeshObjects[i], PMHolders[i].transform.position).ToArray();

                //Reset position of meshObject to original location
                backMeshObjects[i].transform.position = resetPos;
                backMeshObjects[i].transform.localRotation = obr;

                //Calculate the newPoints in Global space
                newPoints = GetGlobalVertices(newPoints, backMeshObjects[i], PMHolders[i].transform.position).ToArray();

                //Refill mesh with realigned points
                backMeshObjects[i].GetComponent<MeshFilter>().sharedMesh.vertices = newPoints;
                backMeshObjects[i].GetComponent<MeshFilter>().sharedMesh.RecalculateNormals();
            }
            
            //Remove overlapping parts
            PMHolders[i].transform.position = Vector3.zero;
            RemoveOverlapAndGetGroupScannedEdges(frontMeshObjects[i], backMeshObjects[i], frontGroupPOIs[i], i, true);
            RemoveOverlapAndGetGroupScannedEdges(backMeshObjects[i], frontMeshObjects[i], frontGroupPOIs[i], i, false);
            //List<int> frontEdges = RemoveOverlapAndGetGroupHardEdges(frontMeshObjects[i], backMeshObjects[i], frontGroupPOIs[i], i, true);
            //List<int> backEdges = RemoveOverlapAndGetGroupHardEdges(backMeshObjects[i], frontMeshObjects[i], frontGroupPOIs[i], i, false);

            //Move partial mesh holders to original positions
            PMHolders[i].transform.localPosition = frontPMHolders[i].transform.localPosition;

            //Set meshobjects to zero rotations
            frontMeshObjects[i].transform.localRotation = Quaternion.identity;
            backMeshObjects[i].transform.localRotation = Quaternion.identity;

            //Create Combined mesh from reoriented meshobjects
            string combinedName = groupNames[i] + "Combined";
            GameObject combinedMesh = CombineSubMeshes(frontMeshObjects[i], backMeshObjects[i], combinedName, i);
            combinedMesh.transform.parent = PMHolders[i].transform;

            //DisplayHardEdges(frontMeshObjects[i], backMeshObjects[i], frontEdges, backEdges, this.bodyModel.groupPOIs[i]);
            //DisplaySoftEdges(frontMeshObjects[i], backMeshObjects[i], combinedMesh, this.bodyModel.groupedFrontSoftEdges[i], this.bodyModel.groupedBackSoftEdges[i], i);

            //Link the combined mesh along the hard edges (points where scan ended)
            MeshFilter cmf = combinedMesh.GetComponent<MeshFilter>();
            Mesh cm = cmf.sharedMesh;
            int fmvc = frontMeshObjects[i].GetComponent<MeshFilter>().sharedMesh.vertexCount;

            List<List<int>> groupedFrontHardEdges = GroupScannedEdges(QuarterScannedEdges(frontMeshObjects[i], this.bodyModel.groupedFrontScannedEdges[i]), BodyModel.groupPOIs[i]);
            List<List<int>> groupedBackHardEdges = NewBackScannedEdges(GroupScannedEdges(QuarterScannedEdges(backMeshObjects[i], this.bodyModel.groupedBackScannedEdges[i]), BodyModel.groupPOIs[i]), fmvc);

            //Smooth hard linked edges for softer lines along the linking edges
            cm.vertices = SmoothLinkedEdges(cm.vertices, groupedFrontHardEdges[0], groupedBackHardEdges[0], fmvc, i, 3);
            cm.vertices = SmoothLinkedEdges(cm.vertices, groupedFrontHardEdges[1], groupedBackHardEdges[1], fmvc, i, 3);

            //Calculate new triangles which now include those for linking meshes between hard edges
            List<int> newTrianglesOne = LinkMeshes(cm.vertices, cm.triangles, groupedFrontHardEdges[0], groupedBackHardEdges[0], fmvc, BodyModel.groupPOIs[i], false);
            List<int> newTrianglesTwo = LinkMeshes(cm.vertices, cm.triangles, groupedFrontHardEdges[1], groupedBackHardEdges[1], fmvc, BodyModel.groupPOIs[i], true);

            cm.triangles = cm.triangles.ToList().Concat(newTrianglesOne).Concat(newTrianglesTwo).ToArray();
            cm.RecalculateNormals();

            combinedMeshObjects[i] = combinedMesh;

            //Destroy original mesh objects
            Destroy(frontMeshObjects[i]);
            Destroy(backMeshObjects[i]);
        }

        this.rightHand.SetActive(true);
        this.leftHand.SetActive(true);
        this.rightFoot.SetActive(true);
        this.leftFoot.SetActive(true);

        this.rightHand.transform.SetParent(PMHolders[7].transform);
        this.leftHand.transform.SetParent(PMHolders[4].transform);
        this.rightFoot.transform.SetParent(PMHolders[11].transform);//20
        this.leftFoot.transform.SetParent(PMHolders[9].transform);//18

        SetToBase(this.rightHand, false);
        SetToBase(this.leftHand, false);
        SetToBase(this.rightFoot, false);
        SetToBase(this.leftFoot, false);

        AlignToFellowChild(this.rightHand, "HandRightToWristRight");
        AlignToFellowChild(this.leftHand, "HandLeftToWristLeft");

        //Center meshes to containers
        for (int i = 0; i < PMHolders.Length; i++)
        {
            Vector3 oldPos = PMHolders[i].transform.position;
            PMHolders[i].transform.position = Vector3.zero;
            switch (i)
            {
                case 4: ResetMesh(this.leftHand); SetToBase(this.leftHand, false); break;
                case 7: ResetMesh(this.rightHand); SetToBase(this.rightHand, false); break;
                case 9: ResetMesh(this.leftFoot); SetToBase(this.leftFoot, false); break;
                case 11: ResetMesh(this.rightFoot); SetToBase(this.rightFoot, false); break;
                default: ResetMesh(combinedMeshObjects[i]); SetToBase(combinedMeshObjects[i], false); break;
            }
            PMHolders[i].transform.position = oldPos;
        }

        //Use data to create linking meshes
        this.linkingMeshes = CombineCreatedEdgeLinkingMeshes(frontMeshObjects, backMeshObjects, combinedMeshObjects, aligner);

        ExciseChild(this.rightHand.transform.parent.Find("RightHandCombined").gameObject);
        ExciseChild(this.leftHand.transform.parent.Find("LeftHandCombined").gameObject);
        ExciseChild(this.rightFoot.transform.parent.Find("RightFootCombined").gameObject);
        ExciseChild(this.leftFoot.transform.parent.Find("LeftFootCombined").gameObject);

        combinedMeshObjects[4] = this.leftHand;
        combinedMeshObjects[7] = this.rightHand;
        combinedMeshObjects[9] = this.leftFoot;
        combinedMeshObjects[11] = this.rightFoot;

        this.rightHand.name = "RightHandCombined";
        this.leftHand.name = "LeftHandCombined";
        this.rightFoot.name = "RightFootCombined";
        this.leftFoot.name = "LeftFootCombined";

        //Adjust containers to bone angles (While keeping orientation of meshes and POIs)
        for (int i = 0; i < PMHolders.Length; i++)
        {
            //Move combined meshes and POIs out of container
            combinedMeshObjects[i].transform.parent = null;
            frontGroupPOIs[i].transform.parent = null;
            //Set PMHolder rotations to that of the POI(joint or bone)
            PMHolders[i].transform.rotation = frontGroupPOIs[i].transform.rotation;
            //Move combined meshes and POIs back into container
            combinedMeshObjects[i].transform.parent = PMHolders[i].transform;
            frontGroupPOIs[i].transform.parent = PMHolders[i].transform;
            //Reset mesh rotations
            Transform[] children = new Transform[combinedMeshObjects[i].transform.childCount];
            for (int j = 0; j < children.Length; j++)
            {
                children[j] = combinedMeshObjects[i].transform.GetChild(j);
            }
            combinedMeshObjects[i].transform.DetachChildren();
            Vector3 oldPos = PMHolders[i].transform.position;
            Quaternion oldRot = PMHolders[i].transform.rotation;
            PMHolders[i].transform.position = Vector3.zero;
            PMHolders[i].transform.rotation = Quaternion.identity;
            ResetMesh(combinedMeshObjects[i]);
            SetToBase(combinedMeshObjects[i], false);
            PMHolders[i].transform.position = oldPos;
            PMHolders[i].transform.rotation = oldRot;
            for (int j = 0; j < children.Length; j++)
            {
                children[j].transform.parent = combinedMeshObjects[i].transform;
            }

        }

        this.bodyScan = aligner;
        this.partialMeshHolders = PMHolders;
        foreach (GameObject lm in linkingMeshes) lm.transform.parent = aligner.transform;

        Destroy(frontAligner);
        Destroy(backAligner);

        updatingPartialMeshes = true;

        //Serialises scan and stores in global variable for transmission
        this.scanSerial = new ScanSerializer(aligner, this.linkingMeshGroups, this.linkingMeshIndices, BodyModel.groupNames);
    }

    /// <summary>
    /// Identifies areas where the scanned edges of the front and back meshes overlap, and removes that overlap.
    /// </summary>
    /// <param name="current"> Front or back mesh segment GameObject to have overlap removed. </param>
    /// <param name="counter"> Back or front mesh segment GameObject (respectively) that current GameObject may be overlapping. </param>
    /// <param name="groupPOI"> The Point of Interest  GameObject (bone or joint) that controls the current group. </param>
    /// <param name="groupNum"> The group number. </param>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    void RemoveOverlapAndGetGroupScannedEdges(GameObject current, GameObject counter, GameObject groupPOI, int groupNum, bool front)
    {
        Mesh newMesh = new Mesh();
        Mesh currentMesh = current.GetComponent<MeshFilter>().sharedMesh;

        //Get position of the bone/joint that the meshes are positioned around
        Vector3 offset = groupPOI.transform.position;

        //Get the global locations of the mesh points
        List<Vector3> currentGlobalPoints = GetGlobalVertices(current, offset);
        List<Vector3> counterGlobalPoints = GetGlobalVertices(counter, offset);

        List<int> overlap = IdentifyOverlap(currentGlobalPoints, counterGlobalPoints, offset);
        
        List<int> hardEdgesList = front ? this.bodyModel.groupedFrontScannedEdges[groupNum] : this.bodyModel.groupedBackScannedEdges[groupNum];

        List<Vector3> newMeshPoints = bodyModel.RemovePoints(currentGlobalPoints, overlap, groupNum, front);
        List<Color> newMeshColors = bodyModel.UpdateMeshColors(currentMesh.colors.ToList<Color>(), overlap);
        List<Vector2> newMeshUVs = bodyModel.UpdateMeshUVs(currentMesh.uv.ToList<Vector2>(), overlap);
        int[] newLocalIndices = bodyModel.CalculateNewLocalIndices(currentMesh.vertexCount, overlap);
        List<List<int>> trianglesByPoint = bodyModel.TrianglesByPoint(currentMesh.vertexCount, currentMesh.triangles.ToList<int>());
        List<int> removedTriangles = bodyModel.IdentifyRemovedTriangles(currentMesh.triangles.ToList<int>(), overlap);
        List<int> newMeshTriangles = bodyModel.RemoveTriangles(currentMesh.triangles.ToList<int>(), removedTriangles, newLocalIndices);

        bodyModel.UpdateScannedEdges(hardEdgesList, trianglesByPoint, removedTriangles, newLocalIndices, groupNum, front);

        newMesh.vertices = newMeshPoints.ToArray();
        newMesh.triangles = newMeshTriangles.ToArray();
        newMesh.colors = newMeshColors.ToArray();
        newMesh.uv = newMeshUVs.ToArray();
        newMesh.RecalculateNormals();

        current.GetComponent<MeshFilter>().mesh = newMesh;
    }
    
    /// <summary>
    /// Creates linking meshes from the created edges and their original triangles to link the combined mesh segments.
    /// </summary>
    /// <param name="frontMeshObjects"> Array of the front mesh segment GameObjects.</param>
    /// <param name="backMeshObjects"> Array of the back mesh segment GameObjects. </param>
    /// <param name="combinedMeshObjects"> Array of the combined mesh segment GameObjects. </param>
    /// <param name="container"> The container of the combined meshes. </param>
    /// <returns> A list of linking mesh GameObjects. </returns>
    List<GameObject> CombineCreatedEdgeLinkingMeshes(GameObject[] frontMeshObjects, GameObject[] backMeshObjects, GameObject[] combinedMeshObjects, GameObject container)
    {
        List<GameObject> createdEdgeLinkingMeshes = new List<GameObject>();
        List<List<Vector3>> linkMeshVertices = new List<List<Vector3>>();
        List<List<Vector2>> linkMeshUVs = new List<List<Vector2>>();
        List<int> frontVertexCount = new List<int>();
        List<int> backVertexCount = new List<int>();
        List<List<Color>> linkMeshColors = new List<List<Color>>();
        List<List<int>> frontCreatedEdgesOI = new List<List<int>>();
        List<List<int>> backCreatedEdgesOI = new List<List<int>>();
        List<List<GameObject>> edgeHooks = new List<List<GameObject>>();
        List<List<int>> ehGroups = new List<List<int>>();
        List<List<int>> ehIndices = new List<List<int>>();

        //Create link mesh component arrays for each texture group.
        for (int i = 0; i < 5; i++)
        {
            linkMeshVertices.Add(new List<Vector3>());
            linkMeshUVs.Add(new List<Vector2>());
            frontVertexCount.Add(0);
            backVertexCount.Add(0);
            frontCreatedEdgesOI.Add(new List<int>());
            backCreatedEdgesOI.Add(new List<int>());
            edgeHooks.Add(new List<GameObject>());

        }

        //Populate front link mesh data and create edge hook objects.
        //Edge hook: Game objects fixed to the created edges of partial meshes, whose positions are used to update the ceLinking mesh.
        for (int i = 0; i < frontMeshObjects.Length; i++)
        {
            ehGroups.Add(new List<int>());
            ehIndices.Add(new List<int>());

            //Get Mesh, vertices, UVs and texture
            Mesh frontMesh = frontMeshObjects[i].GetComponent<MeshFilter>().sharedMesh;
            Vector3[] frontVertices = frontMesh.vertices;
            Vector2[] frontUVs = frontMesh.uv;

            List<int> frontCreatedEdges = this.bodyModel.groupedFrontCreatedEdges[i];
            List<int> originalIndices = this.bodyModel.originalFrontIndicesByGroup[i];
            List<int> textureGroup = this.bodyModel.groupedFrontCreatedEdgesTextureGroup[i];

            //for each soft edge
            for (int j = 0; j < frontCreatedEdges.Count; j++)
            {
                frontVertexCount[textureGroup[j]] += 1;

                Vector3 newVertex = Vector3.zero;
                if (i == 7 || i == 4 || i == 11 || i == 9) newVertex = GetGameObjectReplacement(i).transform.TransformPoint(FindNewEdgeHook(i, frontMeshObjects[i].transform.TransformPoint(frontVertices[frontCreatedEdges[j]])));
                else newVertex = frontMeshObjects[i].transform.TransformPoint(frontVertices[frontCreatedEdges[j]]);//Get vertex by global position
                linkMeshVertices[textureGroup[j]].Add(newVertex);

                ehGroups[i].Add(textureGroup[j]);
                ehIndices[i].Add(linkMeshVertices[textureGroup[j]].Count - 1);

                GameObject edgeHookParent = (i == 7 || i == 4 || i == 11 || i == 9) ? GetGameObjectReplacement(i) : combinedMeshObjects[i];
                edgeHooks[textureGroup[j]].Add(CreateEdgeHook(edgeHookParent.transform, newVertex));

                linkMeshUVs[textureGroup[j]].Add(bodyModel.NewGroupUV(frontUVs[frontCreatedEdges[j]], bodyModel.newFrontUVBounds[BodyModel.textureGroups[i]], true));//Get corresponding UV
                frontCreatedEdgesOI[textureGroup[j]].Add(originalIndices[frontCreatedEdges[j]]);
            }
        }

        //Populate back link mesh data and create edge hook objects.
        //Edge hook: Game objects fixed to the soft edges of partial meshes, whose positions are used to update the SE linking mesh.
        for (int i = 0; i < backMeshObjects.Length; i++)
        {
            //Get Mesh, vertices, UVs and texture
            Mesh backMesh = backMeshObjects[i].GetComponent<MeshFilter>().sharedMesh;
            Vector3[] backVertices = backMesh.vertices;
            Vector2[] backUVs = backMesh.uv;

            List<int> backCreatedEdges = this.bodyModel.groupedBackCreatedEdges[i];
            List<int> originalIndices = this.bodyModel.originalBackIndicesByGroup[i];
            List<int> textureGroup = this.bodyModel.groupedBackCreatedEdgesTextureGroup[i];
            
            for (int j = 0; j < backCreatedEdges.Count; j++)
            {
                backVertexCount[textureGroup[j]] += 1;

                Vector3 newVertex = Vector3.zero;
                if (i == 7 || i == 4 || i == 11 || i == 9) newVertex = GetGameObjectReplacement(i).transform.TransformPoint(FindNewEdgeHook(i, backMeshObjects[i].transform.TransformPoint(backVertices[backCreatedEdges[j]])));
                else newVertex = backMeshObjects[i].transform.TransformPoint(backVertices[backCreatedEdges[j]]);//Get vertex by global position
                linkMeshVertices[textureGroup[j]].Add(newVertex);

                ehGroups[i].Add(textureGroup[j]);
                ehIndices[i].Add(linkMeshVertices[textureGroup[j]].Count - 1);

                GameObject edgeHookParent = (i == 7 || i == 4 || i == 11 || i == 9) ? GetGameObjectReplacement(i) : combinedMeshObjects[i];
                edgeHooks[textureGroup[j]].Add(CreateEdgeHook(edgeHookParent.transform, newVertex));

                linkMeshUVs[textureGroup[j]].Add(bodyModel.NewGroupUV(backUVs[backCreatedEdges[j]], bodyModel.newBackUVBounds[BodyModel.textureGroups[i]], false));//Get corresponding UV
                backCreatedEdgesOI[textureGroup[j]].Add(originalIndices[backCreatedEdges[j]]);

            }
        }

        for (int i = 0; i < 5; i++)
        {
            //Obtain triangles and UVs for the linking mesh.
            List<int> frontMeshLinkingTriangles = this.bodyModel.GetLinkingTriangles(true, frontCreatedEdgesOI[i], 0);
            List<int> backMeshLinkingTriangles = this.bodyModel.GetLinkingTriangles(false, backCreatedEdgesOI[i], frontCreatedEdgesOI[i].Count);

            //Create and populate linking mesh.
            string name = "CELinker" + i;
            GameObject ceLinkMesh = CreateMesh(Vector3.zero, name, "Standard");
            Texture tex = this.bodyModel.groupTextures[i];
            ceLinkMesh.GetComponent<Renderer>().material.SetTexture("_MainTex", tex);
            MeshFilter mf = ceLinkMesh.GetComponent<MeshFilter>();
            Mesh m = new Mesh();
            m.vertices = linkMeshVertices[i].ToArray();
            m.uv = linkMeshUVs[i].ToArray();
            m.triangles = frontMeshLinkingTriangles.Concat(backMeshLinkingTriangles).ToArray();
            m.RecalculateNormals();
            mf.sharedMesh = m;
            createdEdgeLinkingMeshes.Add(ceLinkMesh);
        }      

        //Store edge hooks.
        this.linkingMeshHooks = edgeHooks;
        this.linkingMeshGroups = ehGroups;
        this.linkingMeshIndices = ehIndices;

        return createdEdgeLinkingMeshes;
    }

    /// <summary>
    /// Retrieves replacement prefabricated object for hands and feet.
    /// </summary>
    /// <param name="groupNum"> The group number of the object to be replaced. </param>
    /// <returns> The prefabricated replacement. </returns>
    GameObject GetGameObjectReplacement(int groupNum)
    {
        GameObject replacement = null;
        switch (groupNum)
        {
            case (7):
                replacement = this.rightHand;
                break;
            case (4):
                replacement = this.leftHand;
                break;
            case (11):
                replacement = this.rightFoot;
                break;
            case (9):
                replacement = this.leftFoot;
                break;
        }
        return replacement;
    }
    
    /// <summary>
    /// Finds new positions of edgehooks for the prefabricated objects.
    /// Old edgehooks are moved to nearest vertex on prefabricated object's pretermined set of edge hooks.
    /// </summary>
    /// <param name="groupNum"> The group number of the prefabricated object. </param>
    /// <param name="oldGlobalPos"> The old global position of the edge hook. </param>
    /// <returns> The new global positon of the edge hook. </returns>
    Vector3 FindNewEdgeHook(int groupNum, Vector3 oldGlobalPos)
    {
        List<Vector3> edgeVertices = new List<Vector3>();

        GameObject prefab = GetGameObjectReplacement(groupNum);
        Vector3[] pfVertices = prefab.GetComponent<MeshFilter>().sharedMesh.vertices;

        int[] edgeHookIndex = groupNum < 8 ? this.handEdgeHooks : this.footEdgeHooks;

        for (int i = 0; i < edgeHookIndex.Length; i++)
        {
            edgeVertices.Add(pfVertices[edgeHookIndex[i]]);
        }

        if (edgeVertices.Count  != 0)
        {
            float[] distances = new float[edgeVertices.Count];
            for(int i = 0; i < edgeVertices.Count; i++)
            {
                Vector3 newGlobalPos = GetGameObjectReplacement(groupNum).transform.TransformPoint(edgeVertices[i]);
                distances[i] = Vector3.Distance(oldGlobalPos, newGlobalPos);
            }
            int closest = Array.IndexOf(distances,distances.Min());
            return edgeVertices[closest];
        }
        else
        {
            return oldGlobalPos;
        }
    }
    
    /// <summary>
    /// Creates a new edge hook. 
    /// </summary>
    /// <param name="container"> The container which holds the object that the edge hook sits on. </param>
    /// <param name="position"> The position of the edge hook. </param>
    /// <returns> The edge hook GameObject.</returns>
    GameObject CreateEdgeHook(Transform container, Vector3 position)
    {
        GameObject hook = new GameObject("ceHook");
        hook.transform.position = position;
        hook.transform.parent = container;
        return hook;            
    }

    /// <summary>
    /// Identifies overlap between two sets of mesh vertices, using their shared point of interest (joint or bone) to determine boundary plane position and orienttion. 
    /// </summary>
    /// <param name="currentMeshPoints"> Position of mesh vertices to have overlap removed. </param>
    /// <param name="counterMeshPoints"> Position of mesh vertices that current mesh points may be overlapping. </param>
    /// <param name="POIposition"> The position of the point of interest. </param>
    /// <returns> A list of points int the currentMeshPoints list to be removed. </returns>
    List<int> IdentifyOverlap(List<Vector3> currentMeshPoints, List<Vector3> counterMeshPoints, Vector3 POIposition)
    {
        List<int> overlap = new List<int>();
        
        Vector3 currentAvg = CalculateAverage(currentMeshPoints.ToArray());
        Vector3 counterAvg = CalculateAverage(counterMeshPoints.ToArray());
        Vector3 diffAvg = currentAvg - counterAvg;
        int axis = LargestAxis(diffAvg);

        for (int i = 0; i < currentMeshPoints.Count; i++)
        {
            float v = 0.0f;
            if (axis == 0) v = currentMeshPoints[i].x * Math.Sign(diffAvg.x);
            if (axis == 1) v = currentMeshPoints[i].y * Math.Sign(diffAvg.y);
            if (axis == 2) v = currentMeshPoints[i].z * Math.Sign(diffAvg.z);
            
            if(v < 0.0f)
            {
                overlap.Add(i);
            }
        }
        return overlap;
    }

    /// <summary>
    /// Determines required relative alignment adjustment needed for two mesh GameObjects based on the average positioning of their mesh vertices. 
    /// </summary>
    /// <param name="frontMeshObject"> The front mesh GameObject to be aligned. </param>
    /// <param name="backMeshObject"> The back mesh GameObject to be aligned. </param>
    /// <param name="zDistance"> Manually set z-distance to account for kinect scan missing depth. </param>
    /// <returns> The vector of the required adjustment. </returns>
    Vector3 AlignByAverages(GameObject frontMeshObject, GameObject backMeshObject, float zDistance)
    {
        Mesh frontMesh = frontMeshObject.GetComponent<MeshFilter>().sharedMesh;
        Mesh backMesh = backMeshObject.GetComponent<MeshFilter>().sharedMesh;

        Vector3 fAvg = CalculateAverage(GetGlobalVertices(frontMeshObject, Vector3.zero).ToArray());
        Vector3 bAvg = CalculateAverage(GetGlobalVertices(backMeshObject, Vector3.zero).ToArray());

        Vector3 diff = fAvg - bAvg;
        diff.z = fAvg.z - bAvg.z + zDistance;

        return diff;
    }

    /// <summary>
    /// Scanned edges split into for groups based on their position around centroid. Used to calculate linking mesh triangle normals.
    /// </summary>
    /// <param name="MeshObject"> The mesh GameObject. </param>
    /// <param name="scannedEdges"> The identified scanned edges. </param>
    /// <returns> Scanned edges, organised into 4 quadrant groups. </returns>
    List<List<int>> QuarterScannedEdges(GameObject MeshObject, List<int> scannedEdges)
    {
        Mesh mesh = MeshObject.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = GetGlobalVertices(MeshObject, MeshObject.transform.position).ToArray();
        Vector3 avg = CalculateGroupAverage(vertices, scannedEdges);
        List<List<int>> quarteredScanEdges = new List<List<int>>();
        quarteredScanEdges.Add(new List<int>());
        quarteredScanEdges.Add(new List<int>());
        quarteredScanEdges.Add(new List<int>());
        quarteredScanEdges.Add(new List<int>());

        Vector3 avgDiff = CalculateAverage(vertices);
        int la = LargestAxis(avgDiff);

        foreach (int i in scannedEdges)
        {
            int eg = 0;
            if (vertices[i].x > avg.x) { eg += 1; }
            if (vertices[i].y > avg.y) { eg += 2; }
            quarteredScanEdges[eg].Add(i);
        }

        return quarteredScanEdges;
    }

    /// <summary>
    /// Scanned edges grouped by the direction their normals should face.
    /// </summary>
    /// <param name="quarteredScannedEdges"> Scanned edges, organised into 4 quadrant groups.</param>
    /// <param name="poiNum"> The point of interest (joints and bones) number. </param>
    /// <returns> Scanned edges, organised into groups of matching normal directions. </returns>
    List<List<int>> GroupScannedEdges(List<List<int>> quarteredScannedEdges, int poiNum)
    {
        List<List<int>> groupedHardEdges = new List<List<int>>();
        groupedHardEdges.Add(new List<int>());
        groupedHardEdges.Add(new List<int>());

        bool vertical = poiNum < SkeletonData.jointCount ? SkeletonData.jointMeshVertical[poiNum] : SkeletonData.boneMeshVertical[poiNum - SkeletonData.jointCount];
        
        groupedHardEdges[0].AddRange(quarteredScannedEdges[0]);
        groupedHardEdges[1].AddRange(quarteredScannedEdges[3]);

        if (vertical)
        {
            groupedHardEdges[0].AddRange(quarteredScannedEdges[2]);
            groupedHardEdges[1].AddRange(quarteredScannedEdges[1]);
        }
        else
        {
            groupedHardEdges[0].AddRange(quarteredScannedEdges[1]);
            groupedHardEdges[1].AddRange(quarteredScannedEdges[2]);
        }

        return groupedHardEdges;
    }

    /// <summary>
    /// Calculates new indices for the back scanned edges, after concatenation of back edges results in offset.
    /// </summary>
    /// <param name="backScannedEdges"> The original back scanned edges indices. </param>
    /// <param name="frontVertexCount"> The front mesh vertex count. </param>
    /// <returns> The new indices for the back scanned edges. </returns>
    List<List<int>> NewBackScannedEdges(List<List<int>> backScannedEdges, int frontVertexCount)
    {
        for (int i = 0; i < backScannedEdges.Count; i++)
        {
            for(int j = 0; j < backScannedEdges[i].Count; j++)
            {
                backScannedEdges[i][j] += frontVertexCount;
            }
        }

        return backScannedEdges;
    }
    
    /// <summary>
    /// Obtains the colors of the edges. (Debugging)
    /// </summary>
    /// <param name="vertexCount"> The number of vertices in the mesh. </param>
    /// <param name="groups"> The edges group. </param>
    /// <returns> The color values for the edges. </returns>
    Color[] GetEdgeColorsByGroup(int vertexCount, List<List<int>> groups)
    {
        Color[] edgeColors = new Color[vertexCount];
        Color[] pallet = { Color.red, Color.blue, Color.green, Color.yellow };
        for(int i = 0; i < groups.Count; i++)
        {
            for(int j = 0; j < groups[i].Count; j++)
            {
                edgeColors[groups[i][j]] = pallet[i];
            }
        }
        return edgeColors;
    }

    /// <summary>
    /// Combines front and back mesh segments.
    /// </summary>
    /// <param name="frontSubMeshObject"> The front mesh segment GameObject. </param>
    /// <param name="backSubMeshObject"> The back mesh segment GameObject. </param>
    /// <param name="name"> The name of the new combined mesh segment. </param>
    /// <param name="groupNum"> The group number of the combined mesh. </param>
    /// <returns> The combined mesh segment. </returns>
    GameObject CombineSubMeshes(GameObject frontSubMeshObject, GameObject backSubMeshObject, string name, int groupNum)
    {
        //GameObject combinedMesh = CreateMesh(Vector3.zero, name, "Particles/VertexLit Blended");//DEBUGGING
        GameObject combinedMesh = CreateMesh(Vector3.zero, name, "Standard");

        CombineInstance[] combiners = new CombineInstance[2];

        MeshFilter frontMF = frontSubMeshObject.GetComponent<MeshFilter>();
        combiners[0].subMeshIndex = 0;
        combiners[0].mesh = frontMF.mesh;
        combiners[0].transform = frontMF.transform.localToWorldMatrix;

        MeshFilter backMF = backSubMeshObject.GetComponent<MeshFilter>();
        combiners[1].subMeshIndex = 0;
        combiners[1].mesh = backMF.mesh;
        combiners[1].transform = backMF.transform.localToWorldMatrix;

        MeshFilter cmf = combinedMesh.GetComponent<MeshFilter>();
        Mesh finalMesh = new Mesh();
        finalMesh.CombineMeshes(combiners);
        finalMesh.uv = this.bodyModel.NewGroupUVs(finalMesh.uv, frontMF.mesh.vertexCount, groupNum);
        cmf.mesh = finalMesh;
        
        Texture tex = this.bodyModel.groupTextures[BodyModel.textureGroups[groupNum]];

        combinedMesh.GetComponent<Renderer>().material.SetTexture("_MainTex", tex);

        return combinedMesh;
    }    
    
    /// <summary>
    /// Creates triangles for the cleaned scanned edges from the front and back mesh segments in the combined mesh segment.
    /// </summary>
    /// <param name="vertices"> The vertices of the combined mesh. </param>
    /// <param name="triangles"> The triangles of the combined mesh. </param>
    /// <param name="frontScannedEdges"> The scanned edges indices for the front mesh segment. </param>
    /// <param name="backScannedEdges"> The scanned edges indices for the back mesh segment. </param>
    /// <param name="frontVertexCount"> The number of vertices in the front mesh segment. </param>
    /// <param name="poiNum"> The index of the point of interest. (joint or bone) </param>
    /// <param name="greater"> True inverted normal directions, false if otherwise. </param>
    /// <returns> The new triangles to concatenate to the mesh triangles list. </returns>
    List<int> LinkMeshes(Vector3[] vertices, int[] triangles, List<int> frontScannedEdges, List<int> backScannedEdges, int frontVertexCount, int poiNum, bool greater)
    {
        List<int> newTriangles = new List<int>();
        bool clockwise = true;

        if(frontScannedEdges.Count != 0 && backScannedEdges.Count != 0)
        {
            bool vertical = poiNum < SkeletonData.jointCount ? SkeletonData.jointMeshVertical[poiNum] : SkeletonData.boneMeshVertical[poiNum - SkeletonData.jointCount];
            
            //Sort edges by vertical/horizontal values
            if (vertical) //Sort by y values
            {
                frontScannedEdges.Sort((a, b) => vertices[a].y.CompareTo(vertices[b].y));
                backScannedEdges.Sort((a, b) => vertices[a].y.CompareTo(vertices[b].y));

                if (greater) clockwise = false;
            }
            else //Sort by x values
            {
                frontScannedEdges.Sort((a, b) => vertices[a].x.CompareTo(vertices[b].x));
                backScannedEdges.Sort((a, b) => vertices[a].x.CompareTo(vertices[b].x));

                if (!greater) clockwise = false;
            }
            
            bool llr = true;

            while ((frontScannedEdges.Count + backScannedEdges.Count) > 3)
            {
                //Choose 2 starting points for triangle
                int a1 = frontScannedEdges[0];
                int b1 = backScannedEdges[0];
                int c1 = -1;

                int fhec = frontScannedEdges.Count;
                int bhec = backScannedEdges.Count;

                if(fhec > 1 && bhec > 1)
                {
                    c1 = llr ? frontScannedEdges[1] : backScannedEdges[1];
                    if (llr) frontScannedEdges.Remove(a1);
                    else backScannedEdges.Remove(b1);
                    llr = !llr;
                }
                else if (fhec == 1)
                {
                    c1 = backScannedEdges[1];
                    backScannedEdges.Remove(b1);
                }
                else if (bhec == 1)
                {
                    c1 = frontScannedEdges[1];
                    frontScannedEdges.Remove(a1);
                }
                
                //Add triangle based on display direction
                if (clockwise)
                {
                    newTriangles.Add(a1);
                    newTriangles.Add(b1);
                    newTriangles.Add(c1);
                }
                else
                {
                    newTriangles.Add(a1);
                    newTriangles.Add(c1);
                    newTriangles.Add(b1);
                }
            }
        }

        return newTriangles;
    }
    
    /// <summary>
    /// Smooths the scanned edges along the 
    /// </summary>
    /// <param name="vertices"> The vertices of the combined mesh. </param>
    /// <param name="frontScannedEdges"> The scanned edges indices for the front mesh segment. </param>
    /// <param name="backScannedEdges"> The scanned edges indices for the back mesh segment. </param>
    /// <param name="frontVertexCount"> The number of vertices in the front mesh segment. </param>
    /// <param name="poiNum"> The index of the point of interest. (joint or bone) </param>
    /// <param name="iterations"> The number of times to perform a smoothing operation. </param>
    /// <returns> The new positions for the scanned edge vertices in the mesh. </returns>
    Vector3[] SmoothLinkedEdges(Vector3[] vertices, List<int> frontScannedEdges, List<int> backScannedEdges, int frontVertexCount, int poiNum, int iterations)
    {
        Vector3[] newVertices = vertices;
        Vector3 vertAvg = CalculateAverage(vertices);

        bool vertical = poiNum < SkeletonData.jointCount ? SkeletonData.jointMeshVertical[poiNum] : SkeletonData.boneMeshVertical[poiNum - SkeletonData.jointCount];

        Vector3 edgeAvg = Vector3.zero;
        for (int i = 0; i < frontScannedEdges.Count; i++)
        {
            edgeAvg += vertices[frontScannedEdges[i]];
        }
        for (int i = 0; i < backScannedEdges.Count; i++)
        {
            edgeAvg += vertices[backScannedEdges[i]];
        }
        edgeAvg /= (frontScannedEdges.Count + backScannedEdges.Count);

        //Sort edges by vertical/horizontal values
        if (vertical) //Sort edges by y values
        {
            frontScannedEdges.Sort((a, b) => vertices[a].y.CompareTo(vertices[b].y));
            backScannedEdges.Sort((a, b) => vertices[a].y.CompareTo(vertices[b].y));
        }
        else //Sort edges by x values
        {            
            frontScannedEdges.Sort((a, b) => vertices[a].x.CompareTo(vertices[b].x));
            backScannedEdges.Sort((a, b) => vertices[a].x.CompareTo(vertices[b].x));
        }

        for(int j = 0; j < iterations; j++)
        {
            for (int i = 1; i < frontScannedEdges.Count - 1; i++)
            {
                Vector3 avg = (vertices[frontScannedEdges[i - 1]] + vertices[frontScannedEdges[i + 1]]) / 2;
                newVertices[frontScannedEdges[i]] = avg;
            }

            for (int i = 1; i < backScannedEdges.Count - 1; i++)
            {
                Vector3 avg = (vertices[backScannedEdges[i - 1]] + vertices[backScannedEdges[i + 1]]) / 2;
                newVertices[backScannedEdges[i]] = avg;
            }
        }
        return newVertices;
    }

    /// <summary>
    /// Obtains the global positions of a set of vertices for the mesh object, with a given offset.
    /// </summary>
    /// <param name="meshObject"> The mesh GameObject. </param>
    /// <param name="offset"> The offset of the GameObject. If none, Vector3.zero can be used. </param>
    /// <returns> A list of the global positions of the vertices. </returns>
    List<Vector3> GetGlobalVertices(GameObject meshObject, Vector3 offset)
    {
        List<Vector3> globalVertices = new List<Vector3>();
        Mesh mesh = meshObject.GetComponent<MeshFilter>().sharedMesh;
        for(int i = 0; i < mesh.vertexCount; i++)
        {
            globalVertices.Add(meshObject.transform.TransformPoint(mesh.vertices[i]) - offset);
        }
        return globalVertices;
    }

    /// <summary>
    /// Obtains the global positions of a set of points, with reference to a GameObject, with a given offset.
    /// </summary>
    /// <param name="points"> A set of points in 3D space. </param>
    /// <param name="relevantObject"> The relevant GameObject. </param>
    /// <param name="offset"> The offset of the GameObject. If none, Vector3.zero can be used. </param>
    /// <returns> A list of the global positions of the points. </returns>
    List<Vector3> GetGlobalVertices(Vector3[] points, GameObject relevantObject, Vector3 offset)
    {
        List<Vector3> globalVertices = new List<Vector3>();
        for (int i = 0; i < points.Length; i++)
        {
            globalVertices.Add(relevantObject.transform.TransformPoint(points[i]) - offset);
        }
        return globalVertices;
    }

    /// <summary>
    /// Calculates the average position of a set of points. 
    /// </summary>
    /// <param name="points"> A set of points in 3D space. </param>
    /// <returns> A point in 3D space representing the average. </returns>
    Vector3 CalculateAverage(Vector3[] points)
    {
        Vector3 avg = Vector3.zero;
        foreach(Vector3 point in points)
        {
            avg += point;
        }
        if(points.Length != 0) avg /= points.Length;
        return avg;
    }

    /// <summary>
    /// Calculates the average for a selected number of points in an array. 
    /// </summary>
    /// <param name="points"> A set of points in 3D space. </param>
    /// <param name="group"> A list of points in the group. </param>
    /// <returns> A point in 3D space representing the average of the group. </returns>
    Vector3 CalculateGroupAverage(Vector3[] points, List<int> group)
    {
        Vector3 avg = Vector3.zero;
        foreach (int i in group)
        {
            avg += points[i];
        }
        if (points.Length != 0) avg /= points.Length;
        return avg;
    }

    /// <summary>
    /// Determines the largest axis in a Unity Vector3 value.
    /// </summary>
    /// <param name="diff"> The value to be examined. </param>
    /// <returns> An integer representing the largest axis 0=x, 1=y, 2=z. </returns>
    int LargestAxis(Vector3 diff)
    {
        List<float> axSizes = new List<float>();
        axSizes.Add(Math.Abs(diff.x));
        axSizes.Add(Math.Abs(diff.y));
        axSizes.Add(Math.Abs(diff.z));
        return axSizes.IndexOf(axSizes.Max());
    }

    /// <summary>
    /// Groups points of interest by their hardcoded groups in the BodyModel class.
    /// </summary>
    /// <param name="POIs"> The points of interest to be grouped. </param>
    /// <param name="groupPOIs"> The groups for the points of interest. </param>
    /// <returns> A grouped array of points of interest. </returns>
    GameObject[] GetGroupedPOIs(GameObject[] POIs, int[] groupPOIs)
    {
        GameObject[] GroupedPOIs = new GameObject[groupPOIs.Length];
        for(int i = 0; i < groupPOIs.Length; i++)
        {
            GroupedPOIs[i] = POIs[groupPOIs[i]];
        }
        return GroupedPOIs;
    }

    /// <summary>
    /// Updates the position and rotation of the partial meshes based on current skeleton data.
    /// </summary>
    /// <param name="time"> Time stamp of the SkeletonData object to be used for update. </param>
    void UpdatePartialMeshes(TimeSpan time)
    {
        SkeletonData skeleton = this.dataManager.GetSkeletonAtTime(time);
        Vector3[] movingPOIPositions = GetGroupedPOIPositions(skeleton.jointPositions.Concat(skeleton.bonePositions).ToArray(), BodyModel.groupPOIs);
        Quaternion[] movingPOIRotations = GetGroupedPOIRotations(skeleton.jointRotations.Concat(skeleton.boneRotations).ToArray(), BodyModel.groupPOIs);
        for(int i = 0; i < this.partialMeshHolders.Length; i++)
        {
            this.partialMeshHolders[i].transform.position = movingPOIPositions[i];
            this.partialMeshHolders[i].transform.rotation = movingPOIRotations[i];
        }
        UpdateCreatedEdgeLinkingMesh();
    }

    /// <summary>
    /// Updates the linking meshes to match the new locations of the edge hooks after partial meshes have been updated.
    /// </summary>
    void UpdateCreatedEdgeLinkingMesh()
    {
        for (int i = 0; i < this.linkingMeshes.Count; i++)
        {
            List<GameObject> edgeHooks = this.linkingMeshHooks[i];
            List<Vector3> newVertices = new List<Vector3>();
            for (int j = 0; j < edgeHooks.Count; j++)
            {
                newVertices.Add(edgeHooks[j].transform.position);
            }

            MeshFilter mf = this.linkingMeshes[i].GetComponent<MeshFilter>();
            Mesh m = mf.sharedMesh;

            m.SetVertices(newVertices);
            mf.mesh.Clear();
            m.RecalculateNormals();
            mf.mesh = m;
        }
    }

    /// <summary>
    /// Creates a body scan mesh array from serial body scan data.
    /// </summary>
    /// <param name="serial"> Serialisable body scan object. </param>
    void CreateBodyScanFromSerial(ScanSerializer serial)
    {
        Texture2D[] textures = GetTextureFromSerial(5, serial.texWidths, serial.texHeights, serial.textures);

        int[] textureGroups = BodyModel.textureGroups;
        string[] groupNames = BodyModel.groupNames;
        int groups = groupNames.Length;

        GameObject newAligner = new GameObject("NewAligner");
        GameObject[] newPMHolders = new GameObject[groups];
        GameObject[] newLinkingMeshes = new GameObject[5];
        GameObject[][] newLinkingMeshHooks = new GameObject[5][];

        for (int i = 0; i < 5; i++)
        {
            newLinkingMeshHooks[i] = new GameObject[serial.ceHookStructure[i]];
            newLinkingMeshes[i] = CreateMesh(Vector3.zero, "NewCELinker" + i, "Standard");
            newLinkingMeshes[i].GetComponent<MeshFilter>().sharedMesh = GetMeshFromSerial(serial.ceLinkMeshVertices[i], serial.ceLinkMeshUVs[i], serial.ceLinkMeshTriangles[i]);
            newLinkingMeshes[i].GetComponent<MeshRenderer>().material.mainTexture = textures[i];
            newLinkingMeshes[i].transform.parent = newAligner.transform;
        }

        for (int i = 0; i < groups; i++)
        {
            newPMHolders[i] = new GameObject("New" + groupNames[i]);
            newPMHolders[i].transform.parent = newAligner.transform;

            GameObject mesh = CreateMesh(Vector3.zero, groupNames[i], "Standard");
            mesh.GetComponent<MeshFilter>().sharedMesh = GetMeshFromSerial(serial.vertices[i], serial.uvs[i], serial.triangles[i]);
            switch (i)
            {
                case 4: mesh.GetComponent<MeshRenderer>().material = this.handMaterial; break;
                case 7: mesh.GetComponent<MeshRenderer>().material = this.handMaterial; break;
                case 9: mesh.GetComponent<MeshRenderer>().material = this.footMaterial; break;
                case 11: mesh.GetComponent<MeshRenderer>().material = this.footMaterial; break;
                default: mesh.GetComponent<MeshRenderer>().material.mainTexture = textures[textureGroups[i]]; ; break;
            }
            mesh.transform.parent = newPMHolders[i].transform;

            for (int j = 0; j < serial.ceHookPositions[i].Length; j++)
            {
                GameObject ceHook = new GameObject();
                ceHook.transform.localPosition = serial.ceHookPositions[i][j];
                ceHook.transform.parent = mesh.transform;
                newLinkingMeshHooks[serial.ceHookGroups[i][j]][serial.ceHookIndices[i][j]] = ceHook;
            }

            newPMHolders[i].transform.localPosition = serial.positions[i];
            newPMHolders[i].transform.localRotation = serial.rotations[i];
        }

        this.serialPartialMeshHolders = newPMHolders;
        this.serialLinkingMeshes = newLinkingMeshes;
        this.serialLinkingMeshHooks = newLinkingMeshHooks;
    }

    /// <summary>
    /// Obtains texture from serialisable texture data.
    /// </summary>
    /// <param name="length"> Number of textures avaialble in byte data array. </param>
    /// <param name="texWidth"> Array of texture widths. </param>
    /// <param name="texHeight"> Array of texture heights. </param>
    /// <param name="textureData"> Textures data in byte format. </param>
    /// <returns> Array of Unity Texture2D objects. </returns>
    Texture2D[] GetTextureFromSerial(int length, int[] texWidth, int[] texHeight, byte[][] textureData)
    {
        Texture2D[] textures = new Texture2D[length];
        for (int i = 0; i < length; i++)
        {
            textures[i] = new Texture2D(texWidth[i], texHeight[i], TextureFormat.RGBA32, false);
            textures[i].LoadRawTextureData(textureData[i]);
            textures[i].Apply();
        }
        return textures;
    }

    /// <summary>
    /// Obtains mesh object from serialisable data.
    /// </summary>
    /// <param name="vertices"> Serialisable vertices array of the mesh. </param>
    /// <param name="uvs"> Serialisable UVs array of the mesh. </param>
    /// <param name="triangles"> Integer array of triangles of the mesh. </param>
    /// <returns> Unity mesh object. </returns>
    Mesh GetMeshFromSerial(SVector3[] vertices, SVector2[] uvs, int[] triangles)
    {
        Mesh mesh = new Mesh();
        Vector3[] v = new Vector3[vertices.Length];
        Vector2[] u = new Vector2[uvs.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            v[i] = vertices[i];
            u[i] = uvs[i];
        }
        mesh.vertices = v;
        mesh.uv = u;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        return mesh;
    }

    /// <summary>
    /// Updates the position and rotation of the partial meshes based on recieved network skeleton data.
    /// </summary>
    /// <param name="skeleton">The serializable skeleton struct recieved over the network. </param>
    void UpdateNetworkPartialMeshes(SSkeleton skeleton)
    {
        Vector3[] movingPOIPositions = GetGroupedPOIPositions(ScanSerializer.GetCombinedSerialPositions(skeleton.joints, skeleton.bones), BodyModel.groupPOIs);
        Quaternion[] movingPOIRotations = GetGroupedPOIRotations(ScanSerializer.GetCombinedSerialRotations(skeleton.joints, skeleton.bones), BodyModel.groupPOIs);
        for (int i = 0; i < this.serialPartialMeshHolders.Length; i++)
        {
            this.serialPartialMeshHolders[i].transform.localPosition = movingPOIPositions[i];
            this.serialPartialMeshHolders[i].transform.localRotation = movingPOIRotations[i];
        }
        UpdateNetworkCreatedEdgeLinkingMesh();
    }

    /// <summary>
    /// Updates the linking meshes to match the new locations of the edge hooks after partial meshes have been updated by network data.
    /// </summary>
    void UpdateNetworkCreatedEdgeLinkingMesh()
    {
        for (int i = 0; i < this.serialLinkingMeshes.Length; i++)
        {
            GameObject[] edgeHooks = this.serialLinkingMeshHooks[i];
            List<Vector3> newVertices = new List<Vector3>();
            for (int j = 0; j < edgeHooks.Length; j++)
            {
                newVertices.Add(edgeHooks[j].transform.position);
            }

            MeshFilter mf = this.serialLinkingMeshes[i].GetComponent<MeshFilter>();
            Mesh m = mf.sharedMesh;

            m.SetVertices(newVertices);
            mf.mesh.Clear();
            m.RecalculateNormals();
            mf.mesh = m;
        }
    }

    /// <summary>
    /// Obtains the positions of the grouped points of interest (joints or bones) from the ungrouped points of interest positions. 
    /// </summary>
    /// <param name="POIPositions"> The ungrouped point of interest positions. </param>
    /// <param name="groupPOIs"> The point of interest groups. </param>
    /// <returns> The grouped point of interest positions. </returns>
    Vector3[] GetGroupedPOIPositions(Vector3[] POIPositions, int[] groupPOIs)
    {
        Vector3[] GroupedPOIPositions = new Vector3[groupPOIs.Length];
        for (int i = 0; i < groupPOIs.Length; i++)
        {
            GroupedPOIPositions[i] = POIPositions[groupPOIs[i]];
        }
        return GroupedPOIPositions;
    }

    /// <summary>
    /// Obtains the rotations of the grouped points of interest (joints or bones) from the ungrouped points of interest rotations. 
    /// </summary>
    /// <param name="POIRotations"> The ungrouped points of interest rotations. </param>
    /// <param name="groupPOIs"> The point of interest groups. </param>
    /// <returns> The grouped point of interest rotations. </returns>
    Quaternion[] GetGroupedPOIRotations(Quaternion[] POIRotations, int[] groupPOIs)
    {
        Quaternion[] GroupedPOIRotations = new Quaternion[groupPOIs.Length];
        for (int i = 0; i < groupPOIs.Length; i++)
        {
            GroupedPOIRotations[i] = POIRotations[groupPOIs[i]];
        }
        return GroupedPOIRotations;
    }

//####################################################################################################################################################################
//#          DEBUGGING SECTION                                                                                                                                       
//####################################################################################################################################################################

    //DEBUGGING 01
    public void ColorMeshes()
    {
        Color[] frontColors = this.bodyModel.GetPointColors(true);
        Color[] backColors = this.bodyModel.GetPointColors(false);

        this.frontMesh.GetComponent<MeshFilter>().mesh.colors = frontColors;
        this.backMesh.GetComponent<MeshFilter>().mesh.colors = backColors;
    }

    //DEBUGGING 02
    void DisplayMask(TimeSpan maskTime, Vector3 offset)
    {
        MeshData fm = this.dataManager.GetMeshAtTime(maskTime);
        Texture2D colorMask = fm.GetMaskImage(fm.colorBodyIndex);
        //Texture2D colorMask = fm.GetExtrapolatedImage();
        GameObject mask = CreateMesh(offset, "Mask", "Standard");
        MeshFilter mmf = mask.GetComponent<MeshFilter>();
        Vector3[] vertices = { Vector3.zero, Vector3.up, Vector3.right, (Vector3.up + Vector3.right) };
        int[] triangles = { 0, 1, 2, 1, 3, 2 };
        Vector2[] uvs = { Vector2.up, Vector2.zero, (Vector2.up + Vector2.right), Vector2.right };

        Mesh display = new Mesh();
        display.vertices = vertices;
        display.triangles = triangles;
        display.uv = uvs;
        display.RecalculateNormals();

        mmf.sharedMesh = display;
        mask.GetComponent<Renderer>().material.SetTexture("_MainTex", colorMask);
    }

    //DEBUGGING 03
    void DisplayMasked(TimeSpan maskTime, Vector3 offset)
    {
        MeshData fm = this.dataManager.GetMeshAtTime(maskTime);
        Texture2D colorMask = fm.GetGriddedMaskedImage();
        GameObject mask = CreateMesh(offset, "Mask", "Standard");
        MeshFilter mmf = mask.GetComponent<MeshFilter>();
        Vector3[] vertices = { Vector3.zero, Vector3.up, Vector3.right, (Vector3.up + Vector3.right) };
        int[] triangles = { 0, 1, 2, 1, 3, 2 };
        Vector2[] uvs = { Vector2.up, Vector2.zero, (Vector2.up + Vector2.right), Vector2.right };

        Mesh display = new Mesh();
        display.vertices = vertices;
        display.triangles = triangles;
        display.uv = uvs;
        display.RecalculateNormals();

        mmf.sharedMesh = display;
        mask.GetComponent<Renderer>().material.SetTexture("_MainTex", colorMask);
    }

    //DEBUGGING 04
    void DisplaySegment(TimeSpan maskTime, Vector3 offset, Vector2[] uvs)
    {
        MeshData fm = this.dataManager.GetMeshAtTime(maskTime);
        //Debug.Log(uvs.Length);
        float[] uvMinMax = fm.GetUVMinMax(uvs);
        //Debug.Log(String.Join(", ", uvMinMax.Select(f => f.ToString()).ToArray()));
        int[] xyMinMax = fm.GetXYMinMax(uvMinMax, 30);
        Debug.Log(String.Join(", ", xyMinMax.Select(f => f.ToString()).ToArray()));

        Texture2D segment = fm.GetTextureSegment(xyMinMax);

        GameObject displayObject = CreateMesh(offset, "Mask", "Standard");
        MeshFilter mmf = displayObject.GetComponent<MeshFilter>();
        float scale = 0.1f;
        Vector3[] vertices = { Vector3.zero * scale, Vector3.up * scale, Vector3.right * scale, (Vector3.up + Vector3.right) * scale };
        int[] triangles = { 0, 1, 2, 1, 3, 2 };
        Vector2[] segmentuvs = { Vector2.up, Vector2.zero, (Vector2.up + Vector2.right), Vector2.right };

        Mesh displayMesh = new Mesh();
        displayMesh.vertices = vertices;
        displayMesh.triangles = triangles;
        displayMesh.uv = segmentuvs;
        displayMesh.RecalculateNormals();

        mmf.sharedMesh = displayMesh;
        displayObject.GetComponent<Renderer>().material.SetTexture("_MainTex", segment);
    }

    //DEBUGGING 05
    void DisplayAverages(GameObject frontMeshObject, GameObject backMeshObject, GameObject PMHolder)
    {
        Mesh frontMesh = frontMeshObject.GetComponent<MeshFilter>().sharedMesh;
        Mesh backMesh = backMeshObject.GetComponent<MeshFilter>().sharedMesh;
        GameObject fAvgPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        GameObject bAvgPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Vector3 fAvg = Vector3.zero;
        Vector3 bAvg = Vector3.zero;

        if (frontMesh.vertexCount != 0)
        {
            fAvg = CalculateAverage(GetGlobalVertices(frontMeshObject, Vector3.zero).ToArray());
            fAvgPoint.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            fAvgPoint.transform.parent = PMHolder.transform;
            fAvgPoint.transform.localPosition = fAvg;
        }
        else { fAvgPoint.SetActive(false); }

        if (backMesh.vertexCount != 0)
        {
            bAvg = CalculateAverage(GetGlobalVertices(backMeshObject, Vector3.zero).ToArray());
            bAvgPoint.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            bAvgPoint.transform.parent = PMHolder.transform;
            bAvgPoint.transform.localPosition = bAvg;
        }
        else { bAvgPoint.SetActive(false); }

        Color ac = GetAxisColor(LargestAxis(fAvg - bAvg));
        fAvgPoint.GetComponent<Renderer>().material.color = ac;
        bAvgPoint.GetComponent<Renderer>().material.color = ac;
    }

    //DEBUGGING 06
    Color GetAxisColor(int a)
    {
        Color ac = Color.black;
        if (a == 0) ac = Color.red;
        if (a == 1) ac = Color.green;
        if (a == 2) ac = Color.blue;
        return ac;
    }

    //DEBUGGING 07
    Color GetColorByUV(Vector2 uv, Texture2D tex)
    {
        int x = (int)Math.Round(uv.x * tex.width);
        int y = (int)Math.Round(uv.y * tex.height);
        return tex.GetPixel(x, y);
    }

    //DEBUGGING 08
    void DisplayScannedEdges(GameObject frontMeshObject, GameObject backMeshObject, List<int> frontScannedEdges, List<int> backScannedEdges, int poiNum)
    {

        Mesh frontMesh = frontMeshObject.GetComponent<MeshFilter>().sharedMesh;
        Mesh backMesh = backMeshObject.GetComponent<MeshFilter>().sharedMesh;

        List<List<int>> frontEdgeGroups = GroupScannedEdges(QuarterScannedEdges(frontMeshObject, frontScannedEdges), poiNum);
        List<List<int>> backEdgeGroups = GroupScannedEdges(QuarterScannedEdges(backMeshObject, backScannedEdges), poiNum);

        Color[] frontHEColors = GetEdgeColorsByGroup(frontMesh.vertexCount, frontEdgeGroups);
        Color[] backHEColors = GetEdgeColorsByGroup(backMesh.vertexCount, backEdgeGroups);

        if (frontMesh.vertexCount > 0)
        {
            foreach (int e in frontScannedEdges)
            {
                Vector3 edge = frontMeshObject.transform.TransformPoint(frontMesh.vertices[e]);
                GameObject edgePoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                edgePoint.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                edgePoint.transform.localPosition = edge;
                edgePoint.GetComponent<Renderer>().material.color = frontHEColors[e];
                edgePoint.transform.parent = frontMeshObject.transform.parent;
            }
        }

        if (backMesh.vertexCount > 0)
        {
            foreach (int e in backScannedEdges)
            {
                Vector3 edge = backMeshObject.transform.TransformPoint(backMesh.vertices[e]);
                GameObject edgePoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                edgePoint.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                edgePoint.transform.localPosition = edge;
                edgePoint.GetComponent<Renderer>().material.color = backHEColors[e];
                edgePoint.transform.parent = backMeshObject.transform.parent;
            }
        }
    }

    //DEBUGGING 09
    void DisplayCreatedEdges(GameObject frontMeshObject, GameObject backMeshObject, GameObject combinedMeshObject, List<int> frontCreatedEdges, List<int> backCreatedEdges, int groupNum)
    {

        Mesh frontMesh = frontMeshObject.GetComponent<MeshFilter>().sharedMesh;
        Mesh backMesh = backMeshObject.GetComponent<MeshFilter>().sharedMesh;

        Mesh combinedMesh = combinedMeshObject.GetComponent<MeshFilter>().sharedMesh;
        Texture2D tex = (Texture2D)combinedMeshObject.GetComponent<Renderer>().material.GetTexture("_MainTex");

        if (frontMesh.vertexCount > 0)
        {
            foreach (int e in frontCreatedEdges)
            {
                Vector3 edge = frontMeshObject.transform.TransformPoint(frontMesh.vertices[e]);
                GameObject edgePoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                edgePoint.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                edgePoint.transform.localPosition = edge;
                edgePoint.transform.parent = frontMeshObject.transform.parent;
                //edgePoint.GetComponent<Renderer>().material.color = GetColorByUV(combinedMesh.uv[e], tex);
                edgePoint.GetComponent<Renderer>().material.color = this.bodyModel.textureColors[BodyModel.textureGroups[groupNum]];
            }
        }

        if (backMesh.vertexCount > 0)
        {
            foreach (int e in backCreatedEdges)
            {
                Vector3 edge = backMeshObject.transform.TransformPoint(backMesh.vertices[e]);
                GameObject edgePoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                edgePoint.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                edgePoint.transform.localPosition = edge;
                edgePoint.transform.parent = backMeshObject.transform.parent;
                //edgePoint.GetComponent<Renderer>().material.color = GetColorByUV(combinedMesh.uv[e + frontMesh.vertexCount], tex);
                edgePoint.GetComponent<Renderer>().material.color = this.bodyModel.textureColors[BodyModel.textureGroups[groupNum]];
            }
        }
    }

    //DEBUGGING 10
    void DisplayTextureGroups()
    {
        List<List<Vector2>> frontTextureGroupUVs = this.bodyModel.GetGroupTextureGroupUVs(true);
        List<List<Vector2>> backTextureGroupUVs = this.bodyModel.GetGroupTextureGroupUVs(false);
        for (int i = 0; i < frontTextureGroupUVs.Count; i++)
        {
            DisplayTextureGroup(this.frontTime, this.backTime, ((Vector3.up * i + Vector3.left) * 0.2f), frontTextureGroupUVs[i].ToArray(), backTextureGroupUVs[i].ToArray());
        }
    }

    //DEBUGGING 11
    void DisplayTextureGroup(TimeSpan frontTime, TimeSpan backTime, Vector3 offset, Vector2[] frontUVs, Vector2[] backUVs)
    {
        MeshData fm = this.dataManager.GetMeshAtTime(frontTime);
        MeshData bm = this.dataManager.GetMeshAtTime(backTime);
        float[] frontUVMinMax = fm.GetUVMinMax(frontUVs);
        float[] backUVMinMax = bm.GetUVMinMax(backUVs);
        int[] frontXYMinMax = fm.GetXYMinMax(frontUVMinMax, 30);
        int[] backXYMinMax = bm.GetXYMinMax(backUVMinMax, 30);

        Texture2D frontSegment = fm.GetTextureSegment(frontXYMinMax);
        Texture2D backSegment = bm.GetTextureSegment(backXYMinMax);

        Texture2D jointSegment = this.bodyModel.JoinTextureSegment(frontSegment, backSegment);

        GameObject displayObject = CreateMesh(offset, "Mask", "Standard");
        MeshFilter mmf = displayObject.GetComponent<MeshFilter>();
        float scale = 0.1f;
        Vector3[] vertices = { Vector3.zero * scale, Vector3.up * scale, Vector3.right * scale, (Vector3.up + Vector3.right) * scale };
        int[] triangles = { 0, 1, 2, 1, 3, 2 };
        Vector2[] segmentuvs = { Vector2.up, Vector2.zero, (Vector2.up + Vector2.right), Vector2.right };

        Mesh displayMesh = new Mesh();
        displayMesh.vertices = vertices;
        displayMesh.triangles = triangles;
        displayMesh.uv = segmentuvs;
        displayMesh.RecalculateNormals();

        mmf.sharedMesh = displayMesh;
        displayObject.GetComponent<Renderer>().material.SetTexture("_MainTex", jointSegment);
    }

    //DEBUGGING 12
    public void NumberHandsAndFeet()
    {
        Vector3[] rightHandVertices = this.rightHand.GetComponent<MeshFilter>().sharedMesh.vertices;
        Vector3[] leftHandVertices = this.leftHand.GetComponent<MeshFilter>().sharedMesh.vertices;
        Vector3[] rightFootVertices = this.rightFoot.GetComponent<MeshFilter>().sharedMesh.vertices;
        Vector3[] leftFootVertices = this.leftFoot.GetComponent<MeshFilter>().sharedMesh.vertices;


        GameObject handV = new GameObject("handVertices");
        GameObject rightHand = new GameObject("Right");
        GameObject leftHand = new GameObject("Left");
        rightHand.transform.parent = handV.transform;
        leftHand.transform.parent = handV.transform;

        for (int i = 0; i < rightHandVertices.Length; i += 1)
        {
            if (rightHandVertices[i].y < -0.067 && rightHandVertices[i].y > -0.079)
            {
                GameObject rightHandVertex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rightHandVertex.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                rightHandVertex.transform.position = rightHandVertices[i];
                rightHandVertex.name = "HandVertex " + i;
                rightHandVertex.transform.parent = rightHand.transform;

                GameObject leftHandVertex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leftHandVertex.transform.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                leftHandVertex.transform.position = leftHandVertices[i];
                leftHandVertex.name = "HandVertex " + i;
                leftHandVertex.transform.parent = leftHand.transform;
            }
        }

        handV.transform.localPosition = new Vector3(0f, 20f, 0f);
        handV.transform.localScale = new Vector3(20f, 20f, 20f);

        GameObject footV = new GameObject("footVertices");
        GameObject rightFoot = new GameObject("Right");
        GameObject leftFoot = new GameObject("Left");
        rightFoot.transform.parent = footV.transform;
        leftFoot.transform.parent = footV.transform;

        for (int i = 0; i < rightFootVertices.Length; i++)
        {
            if (rightFootVertices[i].y > 0.085)
            {
                GameObject rightFootVertex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rightFootVertex.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
                rightFootVertex.transform.position = rightFootVertices[i];
                rightFootVertex.name = "FootVertex " + i;
                rightFootVertex.transform.parent = rightFoot.transform;

                GameObject leftFootVertex = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leftFootVertex.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
                leftFootVertex.transform.position = leftFootVertices[i];
                leftFootVertex.name = "FootVertex " + i;
                leftFootVertex.transform.parent = leftFoot.transform;
            }
        }

        footV.transform.localPosition = new Vector3(0f, 20f, 0f);
        footV.transform.localScale = new Vector3(20f, 20f, 20f);
    }

    //DEBUGGING 13
    void ExplodeGroups(int scale)
    {
        foreach(GameObject pm in this.partialMeshHolders)
        {
            pm.transform.position = new Vector3(pm.transform.position.x * scale, pm.transform.position.y * scale, pm.transform.position.z);
        }
    }
}