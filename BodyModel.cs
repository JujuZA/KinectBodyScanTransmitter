using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
//using TextureScale;

/// <summary>
/// Model of the body scan containing front and back meshes and skeletons.
/// </summary>
public class BodyModel {

    public Texture2D jointTexture;

    public TimeSpan frontTime;
    public TimeSpan backTime;

    public MeshData frontMesh;
    public MeshData backMesh;

    public SkeletonData frontSkeleton;
    public SkeletonData backSkeleton;

    public int[] frontClosestPOI;
    public int[] backClosestPOI;
    
    public int[] frontCreatedEdgeLinkTriangles;
    public int[] backCreatedEdgeLinkTriangles;

    public List<HashSet<int>> frontCreatedEdgeLinkTriangleReference;
    public List<HashSet<int>> bacCreatedEdgeLinkTriangleReference;

    public Mesh[] frontMeshes;
    public Mesh[] backMeshes;

    public List<List<int>> frontScannedEdges;
    public List<List<int>> backScannedEdges;
    public List<List<int>> frontCreatedEdges;
    public List<List<int>> backCreatedEdges;

    public Mesh[] groupedFrontMeshes;
    public Mesh[] groupedBackMeshes;

    public List<List<int>> groupedFrontScannedEdges;
    public List<List<int>> groupedBackScannedEdges;

    public List<List<int>> groupedFrontCreatedEdges;
    public List<List<int>> groupedBackCreatedEdges;

    public List<List<int>> originalFrontIndicesByGroup;
    public List<List<int>> originalBackIndicesByGroup;

    public List<int> removedFrontTrianglesByOriginalIndex;
    public List<int> removedBackTrianglesByOriginalIndex;

    public List<int> removedFrontPointsByOriginalIndex;
    public List<int> removedBackPointsByOriginalIndex;

    public List<List<int>> groupedFrontCreatedEdgesOriginalIndex;
    public List<List<int>> groupedBackCreatedEdgesOriginalIndex;

    public List<List<int>> groupedFrontCreatedEdgesTextureGroup;
    public List<List<int>> groupedBackCreatedEdgesTextureGroup;

    public List<Texture2D> groupTextures;
    public List<int[]> newFrontUVBounds;
    public List<int[]> newBackUVBounds;
    List<List<Vector2>> frontPOIuvs;
    List<List<Vector2>> backPOIuvs;

    public int gc = 21;
    public int[] subMeshGroups = { 0, 12, 1, 1, 2, 3, 4, 4, 5, 6, 7, 7, 0, 8, 9, 9, 0, 10, 11, 11, 12, 4, 4, 7, 7, 0, 12, 1, 12, 13, 14, 4, 12, 15, 16, 7, 0, 17, 18, 9, 0, 19, 20, 11, 12, 4, 4, 7, 7 };
    public static int[] groupPOIs = { 25, 27, 4, 5, 31, 8, 9, 35, 13, 39, 17, 43, 44, 29, 30, 33, 34, 37, 38, 41, 42 };
    public static int[] textureGroups = { 0, 1, 2, 2, 2, 3, 3, 3, 4, 4, 4, 4, 0, 2, 2, 3, 3, 4, 4, 4, 4 };

    public Color[] textureColors = { Color.black, Color.white, Color.red, Color.blue, Color.yellow, Color.green };
    public static String[] groupNames = {  "LowerTorso", "Head", "LeftShoulder", "LeftElbow", "LeftHand", "RightShoulder", "RightElbow", "RightHand", //0-7
                                    "LeftKnee", "LeftFoot", "RightKnee", "RightFoot", "UpperTorso",//8-12
                                    "LeftShoulderToElbow", "LeftElbowToHand", "RightShoulderToElbow", "RightElbowToHand", //13-16
                                    "LeftHipToKnee", "LeftKneeToAnkle", "RightHipToKnee", "RightKneeToAnkle"};//17-20
    public bool[] groupMeshVertical = { true, true, false, false, false, false, false, false, true, true, true, true, true, true, true, false, false, false, false, true, true, true, true };

    //Texture groupings
    public static int[] mainBody = { 0, 1, 12, 16, 20, 25, 26, 28, 36, 40, 44, 32 };
    public static int[] headAndNeck = { 2, 3, 26, 27 };
    public static int[] leftArm = { 4, 5, 6, 7, 21, 22, 28, 29, 30, 31, 45, 46 };
    public static int[] rightArm = { 8, 9, 10, 11, 23, 24, 32, 33, 34, 35, 47, 48 };
    public static int[] lefttLeg = { 12, 13, 14, 15, 37, 38, 39 };
    public static int[] rightLeg = { 16, 17, 19, 18, 40, 41, 42, 43 };
    public static int[] legs = { 12, 13, 14, 15, 16, 17, 18, 19, 37, 38, 40, 41, 42, 43 };

    /// <summary>
    /// Creates the Body Model object.
    /// </summary>
    /// <param name="dataManager">The data manager of the system. </param>
    /// <param name="frontTime"> The time stamp of the chosen front mesh. </param>
    /// <param name="backTime"> The time stamp of the chosen back mesh. </param>
    public BodyModel(DataManager dataManager, TimeSpan frontTime, TimeSpan backTime)
    {
        MeshData frontM = dataManager.GetMeshAtTime(frontTime);
        MeshData backM = dataManager.GetMeshAtTime(backTime);

        SkeletonData frontS = dataManager.GetSkeletonAtTime(frontTime);
        SkeletonData backS = dataManager.GetSkeletonAtTime(backTime);

        this.frontMesh = frontM;
        this.backMesh = backM;

        this.frontSkeleton = frontS;
        this.backSkeleton = backS;

        this.groupTextures = new List<Texture2D>();
        this.newFrontUVBounds = new List<int[]>();
        this.newBackUVBounds = new List<int[]>();

        this.removedFrontPointsByOriginalIndex = new List<int>();
        this.removedBackPointsByOriginalIndex = new List<int>();
    }

    /// <summary>
    /// Removes outliers from front and back meshes
    /// </summary>
    public void PrepareMeshes()
    {
        //int[] erosionDilation = { 4, 4, 4 };
        int[] erosionDilation = { 8 };
        frontMesh.removeOutliers(erosionDilation);
        backMesh.removeOutliers(erosionDilation);
    }

    /// <summary>
    /// Creates a single texture to be used on the combined mesh
    /// </summary>
    public void JoinTextures()
    {     
        Texture2D frontTex = this.frontMesh.GetMaskedImage();
        Texture2D backText = this.backMesh.GetMaskedImage();

        List<Color> frontPixels = frontTex.GetPixels().ToList();
        List<Color> backPixels = backText.GetPixels().ToList();

        Color[] allPixels = backPixels.Concat(frontPixels).ToArray();

        int width = frontTex.width;
        int height = frontTex.height * 2;

        Texture2D jointTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        jointTex.SetPixels(allPixels);
        jointTex.Apply();

        this.jointTexture = jointTex;
    }

    /// <summary>
    /// Creates a single texture to be used on a combined mesh segment.
    /// </summary>
    /// <param name="frontSegment"> The chosen segment from the front mesh. </param>
    /// <param name="backSegment"> The corresponding segment from the back mesh. </param>
    /// <returns> A single texture for the chosen combined mesh segment. </returns>
    public Texture2D JoinTextureSegment(Texture2D frontSegment, Texture2D backSegment)
    {        
        TextureScale.Bilinear(backSegment, frontSegment.width, frontSegment.height);
        backSegment.Apply();

        List<Color> frontPixels = frontSegment.GetPixels().ToList();
        List<Color> backPixels = backSegment.GetPixels().ToList();

        Color[] allPixels = backPixels.Concat(frontPixels).ToArray();

        int width = frontSegment.width;
        int height = frontSegment.height * 2;

        Texture2D jointTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        jointTex.SetPixels(allPixels);
        jointTex.Apply();

        return jointTex;
    }

    /// <summary>
    /// Find the distance from a vertex to the centre point of a set of bones. 
    /// </summary>
    /// <param name="vertex"> The position in 3D space of the vertex. </param>
    /// <param name="bones"> The set of bones. </param>
    /// <returns> A set of distances between the vertex and each corresponding bone. </returns>
    public float[] FindBoneDistances(Vector3 vertex, Vector3[] bones)
    {
        float[] boneDistances = new float[bones.Length];
        for(int i = 0; i < bones.Length; i++)
        {
            Vector3 diff = vertex - bones[i];
            boneDistances[i] = diff.magnitude;
        }
        return boneDistances;
    }

    /// <summary>
    /// Find the distance from a vertex to the centre point of a set of joints.
    /// </summary>
    /// <param name="vertex"> The position in 3D space of the vertex. </param>
    /// <param name="joints"> The set of joints. </param>
    /// <returns> A set of distances between the vertex and each corresponding joint. </returns>
    public float[] FindJointDistances(Vector3 vertex, Vector3[] joints)
    {
        float[] jointDistances = new float[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            Vector3 diff = vertex - joints[i];
            jointDistances[i] = diff.magnitude;
        }

        return jointDistances;
    }

    /// <summary>
    /// Finds the closest "Point of interest" (A bone or a joint) to each vertex on the mesh.
    /// </summary>
    /// <param name="m"> The mesh data. </param>
    /// <param name="s"> The corresponding skeleton data. </param>
    /// <returns> An integer array that contains an index for each corresponding vertex's closest bone/joint in a concatenated list of joints and bones. (Joints appearing first.) </returns>
    public int[] FindClosestPOIs(MeshData m, SkeletonData s)
    {
        Vector3[] bones = s.bonePositions;
        Vector3[] joints = s.jointPositions;
        Vector3[] vertices = m.GenerateMesh(1).vertices;
        int[] POIs = new int[vertices.Length];

        for (int i = 0; i < vertices.Length; i++)
        {
            float[] boneDistances = FindBoneDistances(vertices[i], bones);
            float[] jointDistances = FindJointDistances(vertices[i], joints);

            float closestJointDistance = jointDistances.Min();
            float closestBoneDistance = boneDistances.Min();

            int closestJoint = jointDistances.ToList<float>().IndexOf(closestJointDistance);
            int closestBone = boneDistances.ToList<float>().IndexOf(closestBoneDistance) + SkeletonData.jointCount;

            if (closestBoneDistance < closestJointDistance)
            {
                POIs[i] = closestBone;
            }
            else
            {
                POIs[i] = closestJoint;
            }
        }
        return POIs;
    }

    /// <summary>
    /// Obtains a colour for each point of the mesh directly from a pixel/
    /// </summary>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <returns> An aray of color values for a Unity mesh object. </returns>
    public Color[] GetPointColors(bool front)
    {
        int[] closestPOIs = front ? FindClosestPOIs(this.frontMesh, this.frontSkeleton) : FindClosestPOIs(this.backMesh, this.backSkeleton);
        Color[] pointColors = new Color[closestPOIs.Length];

        for (int i = 0; i < closestPOIs.Length; i++)
        {
            Color[] POIColors = SkeletonData.jointColors.Concat(SkeletonData.boneColors).ToArray();
            pointColors[i] = POIColors[closestPOIs[i]];
        }

        int[] scannedEdges = front ? frontMesh.GetScannedEdges() : backMesh.GetScannedEdges();

        foreach(int edge in scannedEdges)
        {
            pointColors[edge] = Color.black;
        }

        int[] createdEdges = front ? frontCreatedEdgeLinkTriangles : backCreatedEdgeLinkTriangles;

        foreach (int edge in createdEdges)
        {
            pointColors[edge] = Color.white;
        }

        return pointColors;
    }

    /// <summary>
    /// Splits meshes into submeshes which consist of points with closest POIs (joints/bones) in a certain group.
    /// </summary>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    public void CalcGroupedSubMeshes(bool front)
    {
        //Get Mesh and Skeleton Data for appropriate side.
        MeshData m = front ? this.frontMesh : this.backMesh;
        SkeletonData s = front ? this.frontSkeleton : this.backSkeleton;

        //Generate new mesh from MeshData model for appropriate side.
        Mesh mesh = m.GenerateMesh(1);

        //Get every Mesh points closest "Point of Interest". (Skeleton joint or bone)
        int[] closestPOIs = FindClosestPOIs(m, s);

        //Get triangles for the Mesh
        int[] triangles = mesh.triangles;

        //Keep track of the new indices that points have within their groups.
        int[] newIndices = new int[mesh.vertexCount];

        //Create a mesh array of all the group Meshes.
        Mesh[] subMeshes = new Mesh[gc];

        //Find the position of the POI's, and their color. (Color for visual debugging)
        Vector3[] POIpos = s.jointPositions.Concat(s.bonePositions).ToArray();
        Color[] POIColors = SkeletonData.jointColors.Concat(SkeletonData.boneColors).ToArray();

        //Create Lists of lists for group mesh points, triangles, colors(debugging), uvs and their POIUVs.
        List<List<Vector3>> meshPoints = new List<List<Vector3>>();
        List<List<int>> meshTriangles = new List<List<int>>();
        List<List<Color>> meshColors = new List<List<Color>>();
        List<List<Vector2>> meshUVs = new List<List<Vector2>>();
        List<List<Vector2>> POIUVs = new List<List<Vector2>>();

        //Create Lists of lists for the scanned and created edges for the groups, as well as references to their original points, and points that their triangles point to.
        List<List<int>> scannedEdgesList = new List<List<int>>();
        List<List<int>> createdEdgesList = new List<List<int>>();
        List<List<int>> originalIndicesByGroup = new List<List<int>>();
        List<int> removedTrianglesByOriginalIndex = new List<int>();
        List<List<int>> createdEdgesTexGroups = new List<List<int>>();

        int[] scannedEdges = front ? frontMesh.GetScannedEdges() : backMesh.GetScannedEdges();

        
        for (int i = 0; i < subMeshes.Length; i++)
        {
            //Create Mesh and lists that contain its data.
            subMeshes[i] = new Mesh();
            meshPoints.Add(new List<Vector3>());
            meshTriangles.Add(new List<int>());
            meshColors.Add(new List<Color>());
            meshUVs.Add(new List<Vector2>());

            //Create lists to keep track of scanned and created edges and relevant indices.
            scannedEdgesList.Add(new List<int>());
            createdEdgesList.Add(new List<int>());
            originalIndicesByGroup.Add(new List<int>());
            createdEdgesTexGroups.Add(new List<int>());

        }

        for (int i = 0; i < POIpos.Length; i++) POIUVs.Add(new List<Vector2>());//Add POIUVs List.
        
        //Copy vertices to submeshes, create new indices reference for meshpoints. (An int array where values point to new indices in submeshes)
        for (int i = 0; i < mesh.vertexCount; i++)
        {
            int groupSubMeshIndex = this.subMeshGroups[closestPOIs[i]];//Group number is the mesh point's closest POI/
            Color groupColor = POIColors[groupPOIs[groupSubMeshIndex]];
            
            newIndices[i] = meshPoints[groupSubMeshIndex].Count; //A vertex's new index is the count of vertices in a submesh collection.
            //The new index (newIndices[i]) are not unique, and are used in combination with the poi reference. (closestsPOI[i])

            originalIndicesByGroup[groupSubMeshIndex].Add(i);//Stores original index for mesh points in group.

            meshPoints[groupSubMeshIndex].Add(mesh.vertices[i] - POIpos[groupPOIs[groupSubMeshIndex]]);//Add the vertex to the it's closest poi submesh.
            meshColors[groupSubMeshIndex].Add(groupColor);//Add the vertex color to color list. (Debugging)
            meshUVs[groupSubMeshIndex].Add(mesh.uv[i]);//Add the vertex UV to UV list.
            POIUVs[closestPOIs[i]].Add(mesh.uv[i]);//Add the vertex UV to POIUV list.
            if (scannedEdges.ToList<int>().Contains(i)) scannedEdgesList[groupSubMeshIndex].Add(newIndices[i]);// If the vertex is in the scanned edges list, add it's new index to the list.
        }

        //Iterate through triangles, if all points in triangle dont share closeset poi, then remove that triangle and add points to created edges list.
        for (int i = 0; i < triangles.Length; i += 3)
        {
            //Find points in triangle.
            int p1 = triangles[i];
            int p2 = triangles[i + 1];
            int p3 = triangles[i + 2];

            //Find their closest mesh group by POI.
            int c1 = this.subMeshGroups[closestPOIs[p1]];
            int c2 = this.subMeshGroups[closestPOIs[p2]];
            int c3 = this.subMeshGroups[closestPOIs[p3]];

            //Find their closest texture group by POI.
            HashSet<int> t = new HashSet<int>();
            t.Add(textureGroups[this.subMeshGroups[closestPOIs[p1]]]);
            t.Add(textureGroups[this.subMeshGroups[closestPOIs[p2]]]);
            t.Add(textureGroups[this.subMeshGroups[closestPOIs[p3]]]);

            //If all points share a closestPOI, copy the triangle across.
            if (c1 == c2 && c1 == c3)
            {
                meshTriangles[c1].Add(newIndices[p1]);
                meshTriangles[c1].Add(newIndices[p2]);
                meshTriangles[c1].Add(newIndices[p3]);
            }
            else //Otherwise, mark points as created edges.
            {
                //Store "removed" triangles by index that they originally referenced.
                removedTrianglesByOriginalIndex.Add(p1);
                removedTrianglesByOriginalIndex.Add(p2);
                removedTrianglesByOriginalIndex.Add(p3);

                //Store created edges in grouped list of lists, by submesh groups.
                createdEdgesList[c1].Add(newIndices[p1]);
                createdEdgesList[c2].Add(newIndices[p2]);
                createdEdgesList[c3].Add(newIndices[p3]);

                //Group created edges to determine texture segment.
                int tg = 0;
                if (t.Count == 1) tg = t.First();
                else if (t.Count == 2 && t.Contains(0) && t.Contains(4)) tg = 4;

                //Stores texture group references for created edges
                createdEdgesTexGroups[c1].Add(tg);
                createdEdgesTexGroups[c2].Add(tg);
                createdEdgesTexGroups[c3].Add(tg);

            }
        }

        //Popoulate each submeshes mesh with calculated data. (Points, triangles, colours)
        for (int i = 0; i < subMeshes.Length; i++)
        {
            subMeshes[i].vertices = meshPoints[i].ToArray();
            subMeshes[i].triangles = meshTriangles[i].ToArray();
            subMeshes[i].colors = meshColors[i].ToArray();
            subMeshes[i].uv = meshUVs[i].ToArray();
            subMeshes[i].RecalculateNormals();
        }

        //Store submesh arrays, created edges and scanned edges to bodymodel global variables.
        if (front)
        {
            this.groupedFrontMeshes = subMeshes;
            this.groupedFrontScannedEdges = scannedEdgesList;
            this.groupedFrontCreatedEdges = createdEdgesList;
            this.groupedFrontCreatedEdgesTextureGroup = createdEdgesTexGroups;
            this.originalFrontIndicesByGroup = originalIndicesByGroup;
            this.removedFrontTrianglesByOriginalIndex = removedTrianglesByOriginalIndex;
            this.frontClosestPOI = closestPOIs;
            this.frontPOIuvs = POIUVs;
        }
        else
        {
            this.groupedBackMeshes = subMeshes;
            this.groupedBackScannedEdges = scannedEdgesList;
            this.groupedBackCreatedEdges = createdEdgesList;
            this.groupedBackCreatedEdgesTextureGroup = createdEdgesTexGroups;
            this.originalBackIndicesByGroup = originalIndicesByGroup;
            this.removedBackTrianglesByOriginalIndex = removedTrianglesByOriginalIndex;
            this.backClosestPOI = closestPOIs;
            this.backPOIuvs = POIUVs;
        }
    }

    /// <summary>
    /// Removes a set of points from the specified submesh and updates affected body model variables.
    /// </summary>
    /// <param name="vertices"> Vertices in the specified submesh. </param>
    /// <param name="removables"> Vertices to be removed from the specified submesh. </param>
    /// <param name="groupNum"> Group number of the submesh being modified. </param>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <returns> A list of vertices with the specified points removed. </returns>
    public List<Vector3> RemovePoints(List<Vector3> vertices, List<int> removables, int groupNum, bool front)
    {
        List<int> groupedCreatedEdges = front ? groupedFrontCreatedEdges[groupNum] : groupedBackCreatedEdges[groupNum];
        List<int> groupedCreatedEdgesTextureGroup = front ? groupedFrontCreatedEdgesTextureGroup[groupNum] : groupedBackCreatedEdgesTextureGroup[groupNum];
        List<int> originalIndices = front ? originalFrontIndicesByGroup[groupNum] : originalBackIndicesByGroup[groupNum];
        
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newGroupedCreatedEdges = new List<int>();
        List<int> newCreatedEdgesTextureGroup = new List<int>();
        List<int> newOriginalIndices = new List<int>();

        for (int i = 0; i < vertices.Count; i++)
        {
            if (!removables.Contains(i))
            {
                newVertices.Add(vertices[i]); //Keep un-removed vertices
                newOriginalIndices.Add(originalIndices[i]); //Keep reference to original index point

                if (groupedCreatedEdges.Contains(i))
                {
                    newGroupedCreatedEdges.Add(newVertices.Count - 1);// Update created edge reference to new vertex index value
                    newCreatedEdgesTextureGroup.Add(groupedCreatedEdgesTextureGroup[groupedCreatedEdges.IndexOf(i)]);
                }
            }
        }

        if (front)
        {
            this.groupedFrontCreatedEdges[groupNum] = newGroupedCreatedEdges;
            this.groupedFrontCreatedEdgesTextureGroup[groupNum] = newCreatedEdgesTextureGroup;
            this.originalFrontIndicesByGroup[groupNum] = newOriginalIndices;
        }
        else
        {
            this.groupedBackCreatedEdges[groupNum] = newGroupedCreatedEdges;
            this.groupedBackCreatedEdgesTextureGroup[groupNum] = newCreatedEdgesTextureGroup;
            this.originalBackIndicesByGroup[groupNum] = newOriginalIndices;
        }            

        return newVertices;
    }

    /// <summary>
    /// Removes the UVs that correspond to removed vertices, leaving a UV array that corresponds properly to the updated vertex array.
    /// </summary>
    /// <param name="UVs"> The UV array that correspond to the set of vertices from which the points are removed.  </param>
    /// <param name="outliers"> A list of the indices of the points to be removed.  </param>
    /// <returns> An updated list of UVs, with specified points removed. </returns>
    public List<Vector2> UpdateMeshUVs(List<Vector2> UVs, List<int> outliers)
    {
        List<Vector2> newUVs = new List<Vector2>();
        for (int i = 0; i < UVs.Count; i++)
        {
            if (!outliers.Contains(i))
            {
                newUVs.Add(UVs[i]);
            }
        }
        return newUVs;
    }

    /// <summary>
    /// Removes the Color values that correspond to removed vertices, leaving a Color array that corresponds properly to the updated vertex array.
    /// </summary>
    /// <param name="meshColors"> The Color array that correspond to the set of vertices from which the points are removed </param>
    /// <param name="outliers"> A list of the indices of the points to be removed. </param>
    /// <returns> An updated list of Color values, with specified points removed. </returns>
    public List<Color> UpdateMeshColors(List<Color> meshColors, List<int> outliers)
    {
        List<Color> newColors = new List<Color>();
        for (int i = 0; i < meshColors.Count; i++)
        {
            if (!outliers.Contains(i))
            {
                newColors.Add(meshColors[i]);
            }
        }
        return newColors;
    }

    /// <summary>
    /// Updates the scanned edges of a submesh to remove any edges which have had all their triangles removed.
    /// </summary>
    /// <param name="edges"> The set of edges to be updated. </param>
    /// <param name="tbp"> Triangles by point - A list of lists of triangles that connect to each corresponding vertex. </param>
    /// <param name="removedTriangles"> The triangles that have been removed. </param>
    /// <param name="newLocalIndices"> The new indices for the vertices of the submesh, with removed points set to -1. </param>
    /// <param name="groupNum"> The group number of the submesh in question. </param>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    public void UpdateScannedEdges(List<int> edges, List<List<int>> tbp, List<int> removedTriangles, int[] newLocalIndices, int groupNum, bool front)
    {
        List<int> newScannedEdges = new List<int>();
        for (int i = 0; i < tbp.Count; i++)
        {
            if (newLocalIndices[i] != -1)//If a point hasn't been removed.
            {
                if (edges.Contains(i))//If an unremoved point was already a scanned edge.
                {
                    newScannedEdges.Add(newLocalIndices[i]);//Add by the new index reference.
                }
                else
                {
                    int tr = 0;
                    foreach (int t in tbp[i])
                    {
                        if (removedTriangles.Contains(t))//If any of the triangles a point connected are removed.
                        {
                            tr++;//Count removed triangles
                        }
                    }

                    if ((tr > 0) && (tr < tbp[i].Count))//If at least one triangle (but not all triangles) have been removed.
                    {
                        newScannedEdges.Add(newLocalIndices[i]);//Add by the new index reference.
                    }
                }
            }
        }

        if (front) this.groupedFrontScannedEdges[groupNum] = newScannedEdges;
        else this.groupedBackScannedEdges[groupNum] = newScannedEdges;
    }

    /// <summary>
    /// Updates the created edges to their new index positions.
    /// </summary>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <param name="groupNum"> The group number of the submesh in question. </param>
    /// <param name="newLocalIndices"> The new indices for the vertices of the submesh, with removed points set to -1. </param>
    public void UpdateCreatedEdges(bool front, int groupNum, int[] newLocalIndices)
    {
        List<int> createdEdges = front ? groupedFrontCreatedEdges[groupNum] : groupedBackCreatedEdges[groupNum];
        List<int> newCreatedEdges = new List<int>();

        foreach (int i in createdEdges)
        {
            if (newLocalIndices[i] != -1)//if a point hasn't been removed
            {
                newCreatedEdges.Add(newLocalIndices[i]);//Add by the new index reference
            }
        }

        if (front) groupedFrontCreatedEdges[groupNum] = newCreatedEdges;
        else groupedBackCreatedEdges[groupNum] = newCreatedEdges;
    }

    /// <summary>
    /// Updates the created edges to their new index positions.
    /// </summary>
    /// <param name="groupNum"> The group number of the submesh in question. </param>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <param name="tbp"> Triangles by point - A list of lists of triangles that connect to each corresponding vertex. </param>
    /// <param name="removedTriangles"> The triangles that have been removed. </param>
    /// <param name="newLocalIndices"> The new indices for the vertices of the submesh, with removed points set to -1. </param>
    public void UpdateGroupCreatedEdges(int groupNum, bool front, List<List<int>> tbp, List<int> removedTriangles, int[] newLocalIndices)
    {
        List<int> newCreatedEdges = new List<int>();
        List<int> newOriginalIndex = new List<int>();
        List<int> newTexGroups = new List<int>();
        List<int> edges = front ? this.groupedFrontCreatedEdges[groupNum] : this.groupedBackCreatedEdges[groupNum];
        List<int> edgeIndex = front ? this.groupedFrontCreatedEdgesOriginalIndex[groupNum] : this.groupedBackCreatedEdgesOriginalIndex[groupNum];
        List<int> texGroups = front ? this.groupedFrontCreatedEdgesTextureGroup[groupNum] : this.groupedBackCreatedEdgesTextureGroup[groupNum];

        for (int i = 0; i < tbp.Count; i++)
        {
            if (newLocalIndices[i] != -1)//if a point hasn't been removed
            {
                if (edges.Contains(i))//if an unremoved point was already a scanned edge
                {
                    newCreatedEdges.Add(newLocalIndices[i]);//Add by the new index reference
                    int indOf = edges.IndexOf(i);
                    newOriginalIndex.Add(edgeIndex[indOf]);
                    newTexGroups.Add(texGroups[indOf]);
                }
            }
        }

        if (front)
        {
            this.groupedFrontCreatedEdges[groupNum] = newCreatedEdges;
            this.groupedFrontCreatedEdgesOriginalIndex[groupNum] = newOriginalIndex;
            this.groupedFrontCreatedEdgesTextureGroup[groupNum] = newTexGroups;
        }
        else
        {
            this.groupedBackCreatedEdges[groupNum] = newCreatedEdges;
            this.groupedBackCreatedEdgesOriginalIndex[groupNum] = newOriginalIndex;
            this.groupedBackCreatedEdgesTextureGroup[groupNum] = newTexGroups;
        }
    }

    /// <summary>
    /// Calculates the new indices for the vertices of the submesh which has had points removed.
    /// </summary>
    /// <param name="vc"> Vertex count - the number of vertices in the array. </param>
    /// <param name="outliers"> A list of the indices of the removed points. </param>
    /// <returns> The new indices for the vertices of the submesh, with removed points set to -1. </returns>
    public int[] CalculateNewLocalIndices(int vc, List<int> outliers)
    {
        int[] newLocalIndices = new int[vc];
        int c = 0;
        for (int i = 0; i < vc; i++)
        {
            if (!outliers.Contains(i)) //If point has not been removed, store it's new index value in array
            {
                newLocalIndices[i] = c++;
            }
            else // Set index value for removed indices to -1
            {
                newLocalIndices[i] = -1;
            }
        }
        return newLocalIndices;
    }

    /// <summary>
    /// Updates the original indices by removing index references of points that have been removed.
    /// </summary>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <param name="groupNum"> The group number of the submesh in question. </param>
    /// <param name="vc"> Vertex count - the number of vertices in the array. </param>
    /// <param name="outliers"> A list of the indices of the removed points. </param>
    public void UpdateOriginalIndices(bool front, int groupNum, int vc, List<int> outliers)
    {
        List<int> originalIndices = front ? originalFrontIndicesByGroup[groupNum] : originalBackIndicesByGroup[groupNum];
        List<int> newOriginalIndices = new List<int>();
        for (int i = 0; i < vc; i++)
        {
            if (!outliers.Contains(i)) //If point has not been removed, store it's original index value in new list
            {
                newOriginalIndices.Add(i);
            }
        }

        if (front) originalFrontIndicesByGroup[groupNum] = newOriginalIndices;
        else originalBackIndicesByGroup[groupNum] = newOriginalIndices;
    }

    /// <summary>
    /// For a set of vertices, provides a list of lists of which triangles connect to each vertex.
    /// </summary>
    /// <param name="vc"> Vertex count - the number of vertices in the array. </param>
    /// <param name="triangles"> A list of the triangles in the Unity mesh object, where every three consecutive values are the indices of the vertices that define the triangle. </param>
    /// <returns> Triangles by point - A list of lists of triangles that connect to each corresponding vertex. </returns>
    public List<List<int>> TrianglesByPoint(int vc, List<int> triangles)
    {
        List<List<int>> trianglesBP = new List<List<int>>();
        for (int i = 0; i < vc; i++) { trianglesBP.Add(new List<int>()); }
        for (int i = 0; i < (triangles.Count / 3); i++)
        {
            trianglesBP[triangles[i * 3]].Add(i);
            trianglesBP[triangles[i * 3 + 1]].Add(i);
            trianglesBP[triangles[i * 3 + 2]].Add(i);
        }
        return trianglesBP;
    }

    /// <summary>
    /// Identifies which triangles contain vertices that are to be removed.
    /// </summary>
    /// <param name="triangles"> A list of the triangles in the Unity mesh object, where every three consecutive values are the indices of the vertices that define the triangle. </param>
    /// <param name="outliers"> A list of the indices of the removed points. </param>
    /// <returns> Those triangles affected by the removal of vertices, and which must also be removed. </returns>
    public List<int> IdentifyRemovedTriangles(List<int> triangles, List<int> outliers)
    {
        List<int> removedTriangles = new List<int>();
        for (int i = 0; i < (triangles.Count / 3); i++)
        {
            int p1 = triangles[i * 3];
            int p2 = triangles[i * 3 + 1];
            int p3 = triangles[i * 3 + 2];

            bool o1 = outliers.Contains(p1);
            bool o2 = outliers.Contains(p2);
            bool o3 = outliers.Contains(p3);

            if (o1 || o2 || o3)
            {
                removedTriangles.Add(i);
            }
        }
        return removedTriangles;
    }

    /// <summary>
    /// Removed triangles that are affected by the removal of outliers.
    /// </summary>
    /// <param name="triangles"> A list of the triangles in the Unity mesh object, where every three consecutive values are the indices of the vertices that define the triangle. </param>
    /// <param name="removedTriangles">  Those triangles affected by the removal of vertices, and which must also be removed. </param>
    /// <param name="newLocalIndices"></param>
    /// <returns> The new indices for the vertices of the submesh, with removed points set to -1. </returns>
    public List<int> RemoveTriangles(List<int> triangles, List<int> removedTriangles, int[] newLocalIndices)
    {
        List<int> newTriangles = new List<int>();
        for (int i = 0; i < (triangles.Count / 3); i++)
        {
            if (!removedTriangles.Contains(i))
            {
                newTriangles.Add(newLocalIndices[triangles[i * 3]]);
                newTriangles.Add(newLocalIndices[triangles[i * 3 + 1]]);
                newTriangles.Add(newLocalIndices[triangles[i * 3 + 2]]);
            }
        }
        return newTriangles;
    }

    /// <summary>
    /// Calculates the new UVs for a combined submesh based on it's new combined texture segment.
    /// </summary>
    /// <param name="uv">The original uv. </param>
    /// <param name="newUVBounds"> The new lower x, upper x, lower y and upper y bounding values of the new UV. (in pixels) </param>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <returns> The updated UV relative to it's new combined texture segment. </returns>
    public Vector2 NewGroupUV(Vector2 uv, int[] newUVBounds, bool front)
    {
        Texture2D tex = front ? this.frontMesh.texture : this.backMesh.texture;
        float minX = (newUVBounds[0] * 1f) / tex.width;
        float maxX = (newUVBounds[1] * 1f) / tex.width;
        float minY = (newUVBounds[2] * 1f) / tex.height;
        float maxY = (newUVBounds[3] * 1f) / tex.height;
        float xWidth = maxX - minX;
        float yHeight = maxY - minY;        
        float x = (uv.x - minX) / xWidth;
        float y = ((uv.y - minY) / yHeight) / 2;
        if (front) y += 0.5f;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Updates the UVs of the combined submesh to refer to their new combined texture segment.
    /// </summary>
    /// <param name="uvs"> The original uvs from the uncombined Unity mesh objects. </param>
    /// <param name="fmvc"> Front mesh vertex count - used to identify whether the UV to be updated originally refers to the front texture or the back texture. </param>
    /// <param name="groupNum"> The group number of the submesh in question. </param>
    /// <returns> The updated UVs for a submesh relative to it's new combined texture segment. </returns>
    public Vector2[] NewGroupUVs(Vector2[] uvs, int fmvc, int groupNum)
    {
        Vector2[] newUVs = new Vector2[uvs.Length];
        for (int i = 0; i < uvs.Length; i++)
        {
            bool front = (i < fmvc);
            int[] newUVBounds = front ? newFrontUVBounds[textureGroups[groupNum]] : newBackUVBounds[textureGroups[groupNum]];
            newUVs[i] = NewGroupUV(uvs[i], newUVBounds, front);
        }
        return newUVs;
    }

    /// <summary>
    /// Retrieves the grouped uncombined submeshes.
    /// </summary>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <returns></returns>
    public Mesh[] GetGroupedSubMeshes(bool front)
    {
        if (front)
        {
            return this.groupedFrontMeshes;
        }
        else
        {
            return this.groupedBackMeshes;
        }
    }

    /// <summary>
    /// Retrieves a list of UV lists for each texture group.
    /// </summary>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <returns></returns>
    public List<List<Vector2>> GetGroupTextureGroupUVs(bool front)
    {
        List<List<Vector2>> POIUVs = front ? this.frontPOIuvs : this.backPOIuvs;
        List<List<Vector2>> textureGroupsUVs = new List<List<Vector2>>();
        List<int[]> textureGroups = new List<int[]>();

        textureGroups.Add(mainBody);
        textureGroups.Add(headAndNeck);
        textureGroups.Add(leftArm);
        textureGroups.Add(rightArm);
        textureGroups.Add(legs);

        foreach (int[] group in textureGroups)
        {
            List<Vector2> groupUVs = new List<Vector2>();
            foreach (int i in group)
            {
                groupUVs = groupUVs.Concat(POIUVs[i]).ToList();
            }
            textureGroupsUVs.Add(groupUVs);
        }

        return textureGroupsUVs;
    }

    /// <summary>
    /// Segments texture into multiple group textures.
    /// Bounding values are calculated based on uvs of points at the far edges of certain groups.
    /// Texture segments overlap to avoid distortion or missing texture data.
    /// </summary>
    public void CalculateGroupTextures()
    {
        List<Texture2D> groupTextures = new List<Texture2D>();
        List<int[]> newFrontXYMinMax = new List<int[]>();
        List<int[]> newBackXYMinMax = new List<int[]>();

        List<List<Vector2>> frontUVs = GetGroupTextureGroupUVs(true);
        List<List<Vector2>> backUVs = GetGroupTextureGroupUVs(false);

        for (int i = 0; i < frontUVs.Count; i++)
        {
            //Calculates UV bounds for each group
            float[] frontUVMinMax = this.frontMesh.GetUVMinMax(frontUVs[i].ToArray());
            float[] backUVMinMax = this.backMesh.GetUVMinMax(backUVs[i].ToArray());

            //Calculates pixel values of boundaries
            int[] frontXYMinMax = this.frontMesh.GetXYMinMax(frontUVMinMax, 30);
            int[] backXYMinMax = this.backMesh.GetXYMinMax(backUVMinMax, 30);

            //Creates texture segment and extrapolates edges to fill blank background space
            Texture2D frontSegment = ExtrapolateTexture(this.frontMesh.GetTextureSegment(frontXYMinMax), Color.green, 2);
            Texture2D backSegment = ExtrapolateTexture(this.backMesh.GetTextureSegment(backXYMinMax), Color.green, 2);

            //Joins segments for combined mesh
            Texture2D jointSegment = JoinTextureSegment(frontSegment, backSegment);

            groupTextures.Add(jointSegment);
            newFrontXYMinMax.Add(frontXYMinMax);
            newBackXYMinMax.Add(backXYMinMax);
        }

        this.newFrontUVBounds = newFrontXYMinMax;
        this.newBackUVBounds = newBackXYMinMax;
        this.groupTextures = groupTextures;
    }

    /// <summary>
    /// Extrapolates colours on the edge of the unmasked portion of the image to fill in the masked background.
    /// </summary>
    /// <param name="texture"> The texture to have edge values extrapolated. </param>
    /// <param name="blankColor"> The color that represents masked background. </param>
    /// <param name="iterations"> The number of iterations for extrapolation. </param>
    /// <returns> The extrapolated texture. </returns>
    public Texture2D ExtrapolateTexture(Texture2D texture, Color blankColor, int iterations)
    {
        int width = texture.width;
        int height = texture.height;
        Color[] colors = texture.GetPixels();
        bool vertical = height > width;

        for (int i = 0; i < iterations; i++)
        {
            if (vertical)
            {
                int s = 1;
                int y = height / 2;
                Vector3 strength = new Vector3(2, 4, 1);

                for (int x = width / 2; x < width; x++)
                {
                    List<Vector2> square = GetSquare(width, height, s);
                    foreach (Vector2 sq in square)
                    {
                        int sx = (int)sq.x;
                        int sy = (int)sq.y;
                        int pixIndex = sx + (sy * width);
                        if (colors[pixIndex] == blankColor) colors[pixIndex] = ExtrapolateColor(sx, sy, width, height, colors, blankColor, strength);
                    }
                    y++;
                    s++;
                }

                while (y < height)
                {
                    int[] xs = GetRange(width);
                    foreach (int sx in xs)
                    {
                        int bottomLineIndex = sx + (y * width);
                        if (colors[bottomLineIndex] == blankColor) colors[bottomLineIndex] = ExtrapolateColor(sx, y, width, height, colors, blankColor, strength);

                        int yRev = height - 1 - y;
                        int topLineIndex = sx + (yRev * width);
                        if (colors[topLineIndex] == blankColor) colors[topLineIndex] = ExtrapolateColor(sx, yRev, width, height, colors, blankColor, strength);
                    }
                    y++;
                }
            }
            else
            {
                int s = 1;
                int x = width / 2;
                Vector3 strength = new Vector3(0, 4, 1);

                for (int y = height / 2; y < height; y++)
                {
                    List<Vector2> square = GetSquare(width, height, s);
                    foreach (Vector2 sq in square)
                    {
                        int sx = (int)sq.x;
                        int sy = (int)sq.y;
                        int pixIndex = sx + (sy * width);
                        if (colors[pixIndex] == blankColor) colors[pixIndex] = ExtrapolateColor(sx, sy, width, height, colors, blankColor, strength);
                    }
                    x++;
                    s++;
                }

                while (x < width)
                {
                    int[] ys = GetRange(height);
                    foreach (int sy in ys)
                    {
                        int rightLineIndex = x + (sy * width);
                        if (colors[rightLineIndex] == blankColor) colors[rightLineIndex] = ExtrapolateColor(x, sy, width, height, colors, blankColor, strength);

                        int xRev = width - 1 - x;
                        int pixIndex = xRev + (sy * width);
                        if (colors[pixIndex] == blankColor) colors[pixIndex] = ExtrapolateColor(xRev, sy, width, height, colors, blankColor, strength);
                    }
                    x++;
                }
            }
        }

        Texture2D newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        newTexture.SetPixels(colors);
        newTexture.Apply();
        return newTexture;
    }

    /// <summary>
    /// Gets a list of Vector2s represent the x and y values of the order in which the extrapolation must move through the image.
    /// </summary>
    /// <param name="width"> Width of the texture. </param>
    /// <param name="height"> Height of the texture. </param>
    /// <param name="s">Size of square to be provided</param>
    /// <returns> A list of Vector2s represent the x and y values of the order in which the extrapolation must move through the image.</returns>
    public List<Vector2> GetSquare(int width, int height, int s)
    {
        List<Vector2> square = new List<Vector2>();
        int lowX = width / 2 - 1;
        int highX = width / 2;
        int lowY = height / 2 - 1;
        int highY = height / 2;

        for(int i = 0; i < s - 1; i++)
        {
            //Up
            square.Add(new Vector2(lowX - i, highY - s));
            square.Add(new Vector2(highX + i, highY - s));

            //Down
            square.Add(new Vector2(lowX - i, lowY + s));
            square.Add(new Vector2(highX + i, lowY + s));

            //Left
            square.Add(new Vector2(highX - s, lowY - i));
            square.Add(new Vector2(highX - s, highY + i));

            //Right
            square.Add(new Vector2(lowX + s, lowY - i));
            square.Add(new Vector2(lowX + s, highY + i));
        }

        square.Add(new Vector2(highX - s, highY - s));
        square.Add(new Vector2(highX - s, lowY + s));
        square.Add(new Vector2(lowX + s, lowY + s));
        square.Add(new Vector2(lowX + s, highY - s));

        return square;
    }

    /// <summary>
    /// Obtains a range that moves from the centre of the range outward, first ascending, then descending.
    /// </summary>
    /// <param name="hw"> The height/width. </param>
    /// <returns> Ascending and then descending range beginning from centre value. </returns>
    public int[] GetRange(int hw)
    {
        List<int> range = new List<int>();
        for(int i = hw/2; i < hw; i++)
        {
            range.Add(i);
        }
        for(int i = hw/2; i > 0; i--)
        {
            range.Add(i - 1);
        }
        return range.ToArray();
    }

    /// <summary>
    /// Extrapolates a color value from it's surrounding points.
    /// </summary>
    /// <param name="x"> The horizontal pixel value. </param>
    /// <param name="y"> The vertical pixel value. </param>
    /// <param name="width"> The width of the texture. </param>
    /// <param name="height"> The height of the texture. </param>
    /// <param name="colors"> The flattened array of the color values of the textures. </param>
    /// <param name="blankColor"> The color that represents masked background. </param>
    /// <param name="strength"> The "strength" of the extrapolation: x = up/down, y=left/right, z=diagonally. </param>
    /// <returns> A color value extrapolated from it's surrounding points. </returns>
    public Color ExtrapolateColor(int x, int y, int width, int height, Color[] colors, Color blankColor, Vector3 strength)
    {
        List<Color> adjColors = new List<Color>();
        
        Vector4 totalColor = Vector4.zero;
        int colorCount = 0;

        int ud = (int) strength.x;
        int lr = (int) strength.y;
        int dg = (int) strength.z;

        //Updown
        for (int i = 0; i < lr; i++)
        {
            if (y > 0) adjColors.Add(colors[x + ((y - 1) * width)]);
            if (y < height - 1) adjColors.Add(colors[x + ((y + 1) * width)]);
        }

        //Left-Right
        for (int i = 0; i < ud; i++)
        {
            if (x > 0) adjColors.Add(colors[(x - 1) + (y * width)]);
            if (x < width - 1) adjColors.Add(colors[(x + 1) + (y * width)]);
        }

        //Diag
        for(int i = 0; i < dg; i++)
        {
            if (x > 0 && y > 0) adjColors.Add(colors[(x - 1) + ((y - 1) * width)]);
            if (x > 0 && y < height - 1) adjColors.Add(colors[(x - 1) + ((y + 1) * width)]);
            if (x < width - 1 && y > 0) adjColors.Add(colors[(x + 1) + ((y - 1) * width)]);
            if (x < width - 1 && y < height - 1) adjColors.Add(colors[(x + 1) + ((y + 1) * width)]);
        }

        foreach (Color c in adjColors)
        {
            if(c != blankColor)
            {
                totalColor += (Vector4) c;
                colorCount++;
            }
        }

        if(colorCount != 0)
        {
            totalColor /= colorCount;
        }
        else
        {
            totalColor = (Vector4) colors[x + y * width];
        }        
        
        return (Color) totalColor;
    }

    /// <summary>
    /// Calculates triangles that link adjacent combined meshes. 
    /// </summary>
    /// <param name="frontOI"> Front original indices. </param>
    /// <param name="backOI"> Back original indices. </param>
    /// <returns> Triangles that linked adjacent combined meshes. </returns>
    public List<int> GetCreatedEdgeLinkTriangles(List<int> frontOI, List<int> backOI)
    {
        int[] originalToNewFrontIndex = new int[this.frontClosestPOI.Length];
        int[] originalToNewBackIndex = new int[this.backClosestPOI.Length];
        List<int> meshTriangles = new List<int>();

        for (int i = 0; i < originalToNewFrontIndex.Length; i++) originalToNewFrontIndex[i] = -1;
        for (int i = 0; i < originalToNewBackIndex.Length; i++) originalToNewBackIndex[i] = -1;

        for(int i = 0; i < frontOI.Count; i++)
        {
            originalToNewFrontIndex[frontOI[i]] = i;
        }

        for (int i = 0; i < backOI.Count; i++)
        {
            originalToNewBackIndex[backOI[i]] = i + frontOI.Count;
        }

        for (int i = 0; i < this.frontCreatedEdgeLinkTriangles.Length;  i += 3)
        {
            //Find points in triangle
            int p1 = this.frontCreatedEdgeLinkTriangles[i];
            int p2 = this.frontCreatedEdgeLinkTriangles[i + 1];
            int p3 = this.frontCreatedEdgeLinkTriangles[i + 2];

            int c1 = originalToNewFrontIndex[p1];
            int c2 = originalToNewFrontIndex[p2];
            int c3 = originalToNewFrontIndex[p3];

            if(c1 != -1 && c2 != -1 && c3 != -1)
            {
                meshTriangles.Add(c1);
                meshTriangles.Add(c2);
                meshTriangles.Add(c3);
            }
        }

        for (int i = 0; i < this.backCreatedEdgeLinkTriangles.Length; i += 3)
        {
            //Find points in triangle
            int p1 = this.backCreatedEdgeLinkTriangles[i];
            int p2 = this.backCreatedEdgeLinkTriangles[i + 1];
            int p3 = this.backCreatedEdgeLinkTriangles[i + 2];

            int c1 = originalToNewBackIndex[p1];
            int c2 = originalToNewBackIndex[p2];
            int c3 = originalToNewBackIndex[p3];

            if (c1 != -1 && c2 != -1 && c3 != -1)
            {
                meshTriangles.Add(c1);
                meshTriangles.Add(c2);
                meshTriangles.Add(c3);
            }
        }

        return meshTriangles;
    }

    /// <summary>
    /// Retrieves the triangles that link created edges.
    /// </summary>
    /// <param name="front"> True if front mesh, false if back mesh. </param>
    /// <param name="pointsByOriginalIndices"> A list corresponding to original indices that lists the new vertices. </param>
    /// <param name="offset"> The index offset of the new vertices. (0 if front, front mesh vertex count if back.) </param>
    /// <returns> The triangles that link created edges. </returns>
    public List<int> GetLinkingTriangles(bool front, List<int> pointsByOriginalIndices, int offset)
    {
        List<int> removedTriangles = front ? this.removedFrontTrianglesByOriginalIndex : this.removedBackTrianglesByOriginalIndex;
        //Debug.Log("Removed Triangles: " + removedTriangles.Count/3);
        List<int> linkingTriangles = new List<int>();
        for(int i = 0; i < removedTriangles.Count; i+=3)
        {
            int p1 = pointsByOriginalIndices.IndexOf(removedTriangles[i]);
            int p2 = pointsByOriginalIndices.IndexOf(removedTriangles[i + 1]);
            int p3 = pointsByOriginalIndices.IndexOf(removedTriangles[i + 2]);

            if((p1 != -1) && (p2 != -1) && (p3 != -1))
            {
                linkingTriangles.Add(p1+offset);
                linkingTriangles.Add(p2 + offset);
                linkingTriangles.Add(p3 + offset);
            }
        }
        return linkingTriangles;
    }
}
