using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Permissions;

/// <summary>
/// Serializable struct based on the Unity Vector2 object.
/// </summary>
[System.Serializable]
public struct SVector2
{
    public float x;
    public float y;

    public SVector2(float vX, float vY)
    {
        x = vX;
        y = vY;
    }

    public override string ToString()
    {
        return string.Format("[{0}, {1}]", x, y);
    }

    public static implicit operator Vector2(SVector2 v)
    {
        return new Vector2(v.x, v.y);
    }

    public static implicit operator SVector2(Vector2 v)
    {
        return new SVector2(v.x, v.y);
    }
}

/// <summary>
/// Serializable struct based on the unity Vector 3 object.
/// </summary>
[System.Serializable]
public struct SVector3
{
    public float x;
    public float y;
    public float z;

    public SVector3(float vX, float vY, float vZ)
    {
        x = vX;
        y = vY;
        z = vZ;
    }

    public override string ToString()
    {
        return string.Format("[{0}, {1}, {2}]", x, y, z);
    }

    public static implicit operator Vector3(SVector3 v)
    {
        return new Vector3(v.x, v.y, v.z);
    }

    public static implicit operator SVector3(Vector3 v)
    {
        return new SVector3(v.x, v.y, v.z);
    }
}

/// <summary>
/// Serializable struct based on Unity Quaternion object.
/// </summary>
[System.Serializable]
public struct SQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SQuaternion(float vX, float vY, float vZ, float vW)
    {
        x = vX;
        y = vY;
        z = vZ;
        w = vW;
    }

    public override string ToString()
    {
        return string.Format("[{0}, {1}, {2}, {3}]", x, y, z, w);
    }

    public static implicit operator Quaternion(SQuaternion v)
    {
        return new Quaternion(v.x, v.y, v.z, v.w);
    }

    public static implicit operator SQuaternion(Quaternion v)
    {
        return new SQuaternion(v.x, v.y, v.z, v.w);
    }
}

/// <summary>
/// Serializable struct representing a joint of the skeleton.
/// </summary>
[System.Serializable]
public struct SJoint
{
    public SVector3 position;
    public SQuaternion rotation;

    public SJoint(SVector3 p, SQuaternion r)
    {
        this.position = p;
        this.rotation = r;
    }

    public SJoint(GameObject boneObject)
    {
        this.position = boneObject.transform.localPosition;
        this.rotation = boneObject.transform.localRotation;
    }
}

/// <summary>
/// Serializable struct representing a bone of the skeleton.
/// </summary>
[System.Serializable]
public struct SBone
{
    public SVector3 position;
    public SQuaternion rotation;
    public SVector3 scale;

    public SBone(SVector3 p, SQuaternion r, SVector3 s)
    {
        this.position = p;
        this.rotation = r;
        this.scale = s;
    }

    public SBone(GameObject boneObject)
    {
        this.position = boneObject.transform.localPosition;
        this.rotation = boneObject.transform.localRotation;
        this.scale = boneObject.transform.localScale;
    }
}

/// <summary>
/// Serializable struct representing the skeleton.
/// </summary>
[System.Serializable]
public struct SSkeleton
{
    public SJoint[] joints;
    public SBone[] bones;

    public SSkeleton(SJoint[] j, SBone[] b)
    {
        this.joints = j;
        this.bones = b;
    }
}

/// <summary>
/// Class for serializing a processed body scan including cage meshes and skeleton heirarchy for animation.
/// </summary>
[System.Serializable]
public class ScanSerializer : ISerializable
{
    //Positions and rotations of containers.
    public SVector3[] positions;
    public SQuaternion[] rotations;

    //Data of each submesh needed to construct Unity mesh objects.
    public SVector3[][] vertices;
    public SVector2[][] uvs;
    public int[][] triangles;

    //Information for Created Edge "hooks" for linking meshes.
    public SVector3[][] ceHookPositions;

    public int[][] ceHookGroups;
    public int[][] ceHookIndices;

    public int[] ceHookStructure;

    //Data of each linking mesh needed to construct Unity mesh objects.
    public SVector3[][] ceLinkMeshVertices;
    public SVector2[][] ceLinkMeshUVs;
    public int[][] ceLinkMeshTriangles;

    //Texture data.
    public int[] texHeights;
    public int[] texWidths;
    public byte[][] textures;

    /// <summary>
    /// Creats serializable scan object.
    /// </summary>
    /// <param name="aligner">Top level container of the scan. </param>
    /// <param name="ehGroups"> Edge hook groups. </param>
    /// <param name="ehIndices"> Edge hook indices. </param>
    /// <param name="groupNames"> Names of groups. </param>
    public ScanSerializer(GameObject aligner, List<List<int>> ehGroups, List<List<int>> ehIndices, string[] groupNames)
    {
        positions = new SVector3[groupNames.Length];
        rotations = new SQuaternion[groupNames.Length];

        vertices = new SVector3[groupNames.Length][];
        uvs = new SVector2[groupNames.Length][];
        triangles = new int[groupNames.Length][];

        ceHookPositions = new SVector3[groupNames.Length][];
        ceHookGroups = new int[groupNames.Length][];
        ceHookIndices = new int[groupNames.Length][];

        int[] ceHS = { 0, 0, 0, 0, 0 };

        for (int i = 0; i < groupNames.Length; i++)
        {
            Transform containerTransform = aligner.transform.Find(groupNames[i]);
            Transform meshTransform = containerTransform.Find(groupNames[i] + "Combined");

            positions[i] = containerTransform.localPosition;
            rotations[i] = containerTransform.localRotation;

            Mesh mesh = meshTransform.gameObject.GetComponent<MeshFilter>().sharedMesh;

            SVector3[] v = new SVector3[mesh.vertexCount];
            SVector2[] u = new SVector2[mesh.vertexCount];
            for (int j = 0; j < mesh.vertexCount; j++)
            {
                v[j] = mesh.vertices[j];
                u[j] = mesh.uv[j];
            }

            vertices[i] = v;
            uvs[i] = u;
            triangles[i] = mesh.triangles;

            SVector3[] s = new SVector3[meshTransform.childCount];
            for (int j = 0; j < meshTransform.childCount; j++)
            {
                s[j] = meshTransform.GetChild(j).localPosition;
                ceHS[ehGroups[i][j]]++;
            }
            ceHookPositions[i] = s;
            ceHookGroups[i] = ehGroups[i].ToArray();
            ceHookIndices[i] = ehIndices[i].ToArray();
        }

        this.ceHookStructure = ceHS;

        ceLinkMeshVertices = new SVector3[5][];
        ceLinkMeshUVs = new SVector2[5][];
        ceLinkMeshTriangles = new int[5][];

        texHeights = new int[5];
        texWidths = new int[5];
        textures = new byte[5][];
        
        for (int i = 0; i < 5; i++)
        {
            string name = "CELinker" + i;
            
            Transform meshTransform = aligner.transform.Find(name);

            Mesh mesh = meshTransform.gameObject.GetComponent<MeshFilter>().sharedMesh;

            SVector3[] v = new SVector3[mesh.vertexCount];
            SVector2[] u = new SVector2[mesh.vertexCount];
            for (int j = 0; j < mesh.vertexCount; j++)
            {
                v[j] = mesh.vertices[j];
                u[j] = mesh.uv[j];
            }

            ceLinkMeshVertices[i] = v;
            ceLinkMeshUVs[i] = u;
            ceLinkMeshTriangles[i] = mesh.triangles;

            Texture2D tex = (Texture2D) meshTransform.gameObject.GetComponent<MeshRenderer>().material.mainTexture;

            texHeights[i] = tex.height;
            texWidths[i] = tex.width;
            textures[i] = tex.GetRawTextureData();
        }
    }

    /// <summary>
    /// Serializing function.
    /// </summary>
    [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("positions", positions);
        info.AddValue("rotations", rotations);


        info.AddValue("ceHookPoisitions", ceHookPositions);

        info.AddValue("ceHookGroups", ceHookGroups);
        info.AddValue("ceHookIndices", ceHookIndices);

        info.AddValue("ceHookStructure", ceHookStructure);

        info.AddValue("vertices", vertices);
        info.AddValue("uvs", uvs);
        info.AddValue("triangles", triangles);

        info.AddValue("ceLinkMeshVertices", ceLinkMeshVertices);
        info.AddValue("ceLinkMeshUVs", ceLinkMeshUVs);
        info.AddValue("ceLinkMeshTriangles", ceLinkMeshTriangles);

        info.AddValue("texHeights", texHeights);
        info.AddValue("texWidths", texWidths);
        info.AddValue("textures", textures);
    }

    /// <summary>
    /// Deseriaizing function.
    /// </summary>
    public ScanSerializer(SerializationInfo info, StreamingContext context)
    {
        positions = (SVector3[]) info.GetValue("positions", typeof(SVector3[]));
        rotations = (SQuaternion[]) info.GetValue("rotations", typeof(SQuaternion[]));
        
        ceHookPositions = (SVector3[][]) info.GetValue("ceHookPoisitions", typeof(SVector3[][]));

        ceHookGroups = (int[][]) info.GetValue("ceHookGroups", typeof(int[][]));
        ceHookIndices = (int[][])info.GetValue("ceHookIndices", typeof(int[][]));

        ceHookStructure = (int[])info.GetValue("ceHookStructure", typeof(int[]));

        vertices = (SVector3[][]) info.GetValue("vertices", typeof(SVector3[][]));
        uvs = (SVector2[][]) info.GetValue("uvs", typeof(SVector2[][]));
        triangles = (int[][]) info.GetValue("triangles", typeof(int[][]));

        ceLinkMeshVertices = (SVector3[][]) info.GetValue("ceLinkMeshVertices", typeof(SVector3[][]));
        ceLinkMeshUVs = (SVector2[][]) info.GetValue("ceLinkMeshUVs", typeof(SVector2[][]));
        ceLinkMeshTriangles = (int[][])info.GetValue("ceLinkMeshTriangles", typeof(int[][]));

        texHeights = (int[])info.GetValue("texHeights", typeof(int[]));
        texWidths = (int[])info.GetValue("texWidths", typeof(int[]));
        textures = (byte[][]) info.GetValue("textures", typeof(byte[][]));
    }

    /// <summary>
    /// Returns an array of serializable joints from a GameObject array. 
    /// </summary>
    /// <param name="jointObjects"> Unity GameObject array of joints. </param>
    /// <returns> An array of serilizable joints. </returns>
    public static SJoint[] SerializeJoints(GameObject[] jointObjects)
    {
        SJoint[] joints = new SJoint[jointObjects.Length];
        for (int i = 0; i < jointObjects.Length; i++) joints[i] = new SJoint(jointObjects[i]);
        return joints;
    }

    /// <summary>
    /// Returns an array of serializable joints from matching position and rotation arrays. 
    /// </summary>
    /// <param name="jointPositions"> Unity Vector3 array of the positions of the joints. </param>
    /// <param name="jointRotations"> Unity Quaternion array of the rotations of the joints. </param>
    /// <returns> An array of serilizable joints. </returns>
    public static SJoint[] SerializeJoints(Vector3[] jointPositions, Quaternion[] jointRotations)
    {
        SJoint[] joints = new SJoint[jointPositions.Length];
        for (int i = 0; i < jointPositions.Length; i++) joints[i] = new SJoint(jointPositions[i], jointRotations[i]);
        return joints;
    }

    /// <summary>
    /// Returns an array of serializable bones from a GameObject array.
    /// </summary>
    /// <param name="boneObjects"> Unity GameObject array of bones. </param>
    /// <returns> An array of serilizable bones. </returns>
    public static SBone[] SerializeBones(GameObject[] boneObjects)
    {
        SBone[] bones = new SBone[boneObjects.Length];
        for (int i = 0; i < boneObjects.Length; i++) bones[i] = new SBone(boneObjects[i]);
        return bones;
    }

    /// <summary>
    /// Returns an array of serializable bones from matching position and rotation arrays. 
    /// </summary>
    /// <param name="bonePositions"> Unity Vector3 array of the positions of the bones. </param>
    /// <param name="boneRotations"> Unity Quaternion array of the rotations of the bones. </param>
    /// <param name="boneScales"> Unity Quaternion array of the relative scale of the bones. </param>
    /// <returns> An array of serilizable bones. </returns>
    public static SBone[] SerializeBones(Vector3[] bonePositions, Quaternion[] boneRotations, Vector3[] boneScales)
    {
        SBone[] bones = new SBone[bonePositions.Length];
        for (int i = 0; i < bonePositions.Length; i++) bones[i] = new SBone(bonePositions[i], boneRotations[i], boneScales[i]);
        return bones;
    }

    /// <summary>
    /// Creates combined array of the positions of serializable bones and joints.
    /// </summary>
    /// <param name="joints"> The array of serializable joints. </param>
    /// <param name="bones"> The array of serializable bones. </param>
    /// <returns> A combined array of the positions of serializable bones and joints. </returns>
    public static Vector3[] GetCombinedSerialPositions(SJoint[] joints, SBone[] bones)
    {
        List<Vector3> combinedPositions = new List<Vector3>();
        for (int i = 0; i < joints.Length; i++) combinedPositions.Add(joints[i].position);
        for (int i = 0; i < bones.Length; i++) combinedPositions.Add(bones[i].position);
        return combinedPositions.ToArray();
    }

    /// <summary>
    /// Creates combined array of the rotations of serializable bones and joints.
    /// </summary>
    /// <param name="joints"> The array of serializable joints. </param>
    /// <param name="bones"> The array of serializable bones. </param>
    /// <returns> A combined array of the rotations of serializable bones and joints. </returns>
    public static Quaternion[] GetCombinedSerialRotations(SJoint[] joints, SBone[] bones)
    {
        List<Quaternion> combinedRotations = new List<Quaternion>();
        for (int i = 0; i < joints.Length; i++) combinedRotations.Add(joints[i].rotation);
        for (int i = 0; i < bones.Length; i++) combinedRotations.Add(bones[i].rotation);
        return combinedRotations.ToArray();
    }
}
