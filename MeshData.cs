using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Stores Kinect data in a form useable for Unity meshes.
/// </summary>
public class MeshData
{
    //Data from Kinect
    public TimeSpan timeStamp;
    public Vector3[,] vertices;
    public Vector2[,] uvs;
    public int[,] depthBodyIndex;
    public int[,] colorBodyIndex;
    public Texture2D texture;
    public Texture2D bodyIndexMask;

    //Processing
    private int vWidth;
    private int vHeight;
    private int[,] verticesIndex;
    private List<int> scannedEdges;

    /// <summary>
    /// Creates the MeshData object.
    /// </summary>
    /// <param name="time"> Time stamp recieved for the relevant frame from the Kinect sensor. </param>
    /// <param name="v"> Array of vectors representing the positions of points on a body scanned by the Kinect sensor. </param>
    /// <param name="u"> Array of vectors representing the position on a 2D texture that a mesh point aligns with for rendering purposes. </param>
    /// <param name="b"> Array of integers with a value of 1 indicating that the corresponding vertex is part of a body, and 0 representing that it is not. </param>
    /// <param name="c"> Array of integers that point to the color value in the flattened 2D texture array that the corresponding vertices map to. (For Debugging Purposes) </param>
    /// <param name="t"> Unity Texture2D object created from the RGB camera of the Kinect sensor. </param>
    public MeshData(TimeSpan time, Vector3[,] v, Vector2[,] u, int[,] b, int[,] c, Texture2D t)
    {
        this.timeStamp = time;
        this.vertices = v;
        this.uvs = u;
        this.depthBodyIndex = b;
        this.colorBodyIndex = c;
        this.texture = t;
        this.vWidth = v.GetLength(0);
        this.vHeight = v.GetLength(1);
        this.verticesIndex = new int[vWidth, vHeight];
    }


    /// <summary>
    /// Manually adjustable repetive erosion and dilation of body index to eliminate outliers.
    /// </summary>
    /// <param name="erosionDilation">An array of integers representing erosion and dilation factors.
    /// Values at even indices are used as erosion factors, Values at odd indices are used as dilation factors.
    /// Even numbered arrays are recommended, as ending the process on erosion can result in points with a value of 2.</param>
    /// <example>
    /// <code>
    /// int[] edf = {3,2,2,3};
    /// removeOutliers(edf);
    /// </code>
    /// This will erode the body index image three times, dilate twice, erode twice, and dilate three times.
    /// </example>
    public void removeOutliers(int[] erosionDilation)
    {
        for (int i = 0; i < erosionDilation.Length; i++)
        {
            if (i % 2 == 0)
            {
                this.depthBodyIndex = Erode(erosionDilation[i], this.depthBodyIndex);
                for(int j = 0; j < 6; j++)
                {
                    this.colorBodyIndex = Erode(erosionDilation[i], this.colorBodyIndex);
                }
            } else
            {
                this.depthBodyIndex = Dilate(erosionDilation[i], this.depthBodyIndex);
                this.colorBodyIndex = Dilate(erosionDilation[i], this.colorBodyIndex);
            }
        }

        int[,] regionIndex = IdentifyRegions(this.depthBodyIndex);
        int maxRegion = GetMaxRegion(regionIndex);
        this.depthBodyIndex = RemoveAuxilaryRegions(regionIndex, maxRegion);

        int[,] colorRegionIndex = IdentifyRegions(this.colorBodyIndex);
        int maxColorRegion = GetMaxRegion(colorRegionIndex);
        this.colorBodyIndex = RemoveAuxilaryRegions(colorRegionIndex, maxColorRegion);
    }

    /// <summary>
    /// A function for eroding unmasked pixels in a masking image presented in the format of a 2D array where:
    /// A value of 0 represents a masked pixel.
    /// A value of 1 represents an umasked pixel.
    /// A value of 2 represents an eroded (masked) pixel.
    /// </summary>
    /// <param name="ef">Erosion factor: The minimum number of surrounding unmasked pixels a pixel must have to not be eroded</param>
    /// <param name="bodyIndex">The body index array (either color or depth) which is to be eroded.</param>
    /// <returns>The eroded body index.</returns>
    public int[,] Erode(int ef, int[,] bodyIndex)
    {
        int width = bodyIndex.GetLength(0);
        int height = bodyIndex.GetLength(1);
        int[,] newBodyIndex = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                //Checks for non-eroded values
                if (bodyIndex[x, y] == 1)
                {
                    Vector2[] sp = GetSurroundingPoints(x, y, width, height);
                    int c = 0;
                    foreach (Vector2 v in sp)
                    {
                        int vX = (int)v.x;
                        int vY = (int)v.y;
                        if (bodyIndex[vX, vY] == 1)
                        {
                            c++;
                        }
                    }
                    if (c >= ef) //Retain point
                    {
                        newBodyIndex[x, y] = 1;
                    }
                    else //Erode point
                    {
                        newBodyIndex[x, y] = 2;
                    }
                }

            }
        }
        return newBodyIndex;
    }

    /// <summary>
    /// A function for dilating unmasked pixels by restoring adjacent eroded pixels in a masking image presented in the format of a 2D array where:
    /// A value of 0 represents a masked pixel.
    /// A value of 1 represents an umasked pixel.
    /// A value of 2 represents an eroded (masked) pixel.
    /// </summary>
    /// <param name="ef">Dilating factor: The minimum number of surrounding unmasked pixels an eroded pixel must have to be restored.</param>
    /// <param name="bodyIndex">The body index array (either color or depth) which is to be dilated.</param>
    /// <returns>The dilated body index.</returns>
    public int[,] Dilate(int df, int[,] bodyIndex)
    {
        int width = bodyIndex.GetLength(0);
        int height = bodyIndex.GetLength(1);
        int[,] newBodyIndex = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            //Checks for eroded values
            for (int y = 0; y < height; y++)
            {
                if (bodyIndex[x, y] == 2)
                {
                    Vector2[] sp = GetSurroundingPoints(x, y, width, height);
                    int c = 0;
                    foreach (Vector2 v in sp)
                    {
                        int vX = (int)v.x;
                        int vY = (int)v.y;
                        if (bodyIndex[vX, vY] == 1)
                        {
                            c++;
                        }
                    }
                    if (c >= df) //Restore point
                    {
                        newBodyIndex[x, y] = 1;
                    }
                    else //Eliminate point
                    {
                        newBodyIndex[x, y] = 0;
                    }
                }

            }
        }
        return newBodyIndex;
    }

    /// <summary>
    /// Identifies regions of connected pixels in a masked image in the format of a 2D array of integers where:
    /// A value of 0 represents a masked pixel.
    /// A value of 1 represents an unmasked pixel.
    /// Values of 1 are modified to positive integers to represent different regions.
    /// </summary>
    /// <param name="bodyIndex">The body index array (either color or depth) which is to be regioned.</param>
    /// <returns>A modified body index array, where values of 0 represent masked pixel, and non-zero values represent unmasked pixels of that values region.</returns>
    public int[,] IdentifyRegions(int[,] bodyIndex)
    {
        int width = bodyIndex.GetLength(0);
        int height = bodyIndex.GetLength(1);
        int[,] regionedBodyIndex = new int[width, height];
        int counter = 1;
        List<int> correspondingRegion = new List<int>();
        correspondingRegion.Add(0);

        //Iterate through bodyIndex
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if(bodyIndex[x,y] == 1)//if body index value is a non-eroded point(value of 1)
                {                                       
                    Vector2[] sp = GetSurroundingPoints(x, y, width, height);
                    HashSet<int> regions = new HashSet<int>();
                    int region = 0;

                    //Add regions to a set to determine how many regions there are
                    regions.Add(regionedBodyIndex[x,y]);//Region for the set of pixels with 0 as a value
                    //Region for the set of pixels with non-zero values
                    foreach (Vector2 v in sp)
                    {
                        int vX = (int)v.x;
                        int vY = (int)v.y;
                        if(bodyIndex[vX, vY] == 1)
                        {
                            regions.Add(regionedBodyIndex[vX, vY]);
                        }
                    }

                    //If regions are all in set 0 (no set), add them to the next set
                    if(regions.Count == 1)
                    {
                        if(regions.Last() == 0)
                        {
                            region = counter;
                            correspondingRegion.Add(counter);
                            counter++;
                        }
                    }
                    else if(regions.Count > 1)
                    {
                        regions.Remove(0);//Remove the region which contains values of zero.
                        region = regions.Min();//Set the points belong to is the minimum value.
                        regions.Remove(region);//Remove the region with the smallest number of pixels.
                        while(regions.Count != 0)//Loop through and set other regions to correspond with smallest region number (eg. region = 2, correspondingRegion[4] = 2, etc.)
                        {
                            int firstRegion = regions.First();
                            correspondingRegion[firstRegion] = region;
                            regions.Remove(firstRegion);
                        }
                    }

                    //set point and surrounding points to chosen region
                    regionedBodyIndex[x, y] = region;
                    foreach (Vector2 v in sp)
                    {
                        int vX = (int)v.x;
                        int vY = (int)v.y;
                        if (bodyIndex[vX, vY] == 1)
                        {
                            regionedBodyIndex[vX, vY] = region;
                        }
                    }
                }
            }
        }

        //Ensure all regions are set to their corresponding region
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                regionedBodyIndex[x, y] = correspondingRegion[regionedBodyIndex[x, y]];
            }
        }

        return regionedBodyIndex;
    }

    /// <summary>
    /// Obtains the region which contains the maximum number of pixels.
    /// </summary>
    /// <param name="regionIndex"> A regioned body index (either color or depth) with non-zero values for unmasked, regioned pixels. </param>
    /// <returns> The integer value of the region which has the highest number of pixels. </returns>
    public int GetMaxRegion(int[,] regionIndex)
    {
        int width = regionIndex.GetLength(0);
        int height = regionIndex.GetLength(1);
        int maxRegion = 0;
        int[] regionCount = new int[100];

        //Loop through the regioned body index and count pixels by region
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int region = regionIndex[x, y];
                if (region < 100) regionCount[region]++;
            }
        }

        regionCount[0] = 0;
        int maxCount = regionCount.ToList().Max();
        maxRegion = regionCount.ToList().IndexOf(maxCount);
        return maxRegion;
    }

    /// <summary>
    /// Removes those regions which do not have the maximum number of pixels.
    /// </summary>
    /// <param name="regionIndex"> A regioned body index (either color or depth) with non-zero values for unmasked, regioned pixels. </param>
    /// <param name="maxRegion"> The integer value of the region which has the highest number of pixels. </param>
    /// <returns> A body index array which has had those regions which are not the maximum region removed, and it's non-zero values ajdusted to 1. </returns>
    public int[,] RemoveAuxilaryRegions(int[,] regionIndex, int maxRegion)
    {
        int width = regionIndex.GetLength(0);
        int height = regionIndex.GetLength(1);
        int[,] newBodyIndex = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int region = regionIndex[x, y];
                if (region != maxRegion)
                {
                    newBodyIndex[x, y] = 0;
                }
                else
                {
                    newBodyIndex[x, y] = 1;
                }
            }
        }
        return newBodyIndex;
    }

    /// <summary>
    /// Obtaims the array indices of the points which surround the given point in a 2D array.
    /// </summary>
    /// <param name="x"> The first index of the point. </param>
    /// <param name="y"> The second index of the point. </param>
    /// <param name="width"> The width of the array. </param>
    /// <param name="height"> The height of the array. </param>
    /// <returns> A Vector2 array in which each Vector has the first and second indices of a surrounding point stored as it's x and y value, respectively. </returns>
    public Vector2[] GetSurroundingPoints(float x, float y, int width, int height)
    {
        List<Vector2> sp = new List<Vector2>();

        if (x > 0 && y > 0) sp.Add(new Vector2(x - 1, y - 1));
        if (x > 0) sp.Add(new Vector2(x - 1, y));
        if (x > 0 && y < height-1) sp.Add(new Vector2(x - 1, y + 1));

        if (y > 0) sp.Add(new Vector2(x, y - 1));
        if (y < height-1) sp.Add(new Vector2(x, y + 1));

        if (x < width-1 && y > 0) sp.Add(new Vector2(x + 1, y - 1));
        if (x < width-1) sp.Add(new Vector2(x + 1, y));
        if (x < width-1 && y < height-1) sp.Add(new Vector2(x + 1, y + 1));

        return sp.ToArray();
    }

    /// <summary>
    /// Generates a Unity Mesh object from the MeshData object's stored data.
    /// </summary>
    /// <param name="dsr"></param>
    /// <returns></returns>
    public Mesh GenerateMesh(int dsr)
    {
        Vector3 total = Vector3.zero;
        List<Vector3> vList = new List<Vector3>();
        List<Vector2> uList = new List<Vector2>();
        int c = 0;

        for (int x = 0; x < vWidth; x++)
        {
            for (int y = 0; y < vHeight; y++)
            {
                if(this.depthBodyIndex[x,y] == 1)
                {
                    vList.Add(vertices[x, y]);
                    uList.Add(uvs[x, y]);
                    verticesIndex[x, y] = c++;
                    total += vertices[x, y];
                }
            }
        }
        
        Vector3[] vArr = vList.ToArray();
        Vector2[] uArr = uList.ToArray();
        int[] tArr = GenerateTriangles(verticesIndex);

        Mesh mesh = new Mesh();
        mesh.vertices = vArr;
        mesh.uv = uArr;
        mesh.triangles = tArr;

        return mesh;
    }

    /// <summary>
    /// Generates triangles for the creation of a mesh by using the Marching Squares method.
    /// </summary>
    /// <param name="vIndex"> A 2D array of integers which are index values for the Vector3 array which represents the vertices of the mesh. </param>
    /// <returns> An integer array of the vertex index where every set of 3 concurrent values represents a triangle between the 3 vertices at the indices of those values. </returns>
    public int[] GenerateTriangles(int[,] vIndex)
    {
        List<int> triangles = new List<int>();

        //Generate triangles via marching squares method
        for (int x = 0; x < vWidth - 1; x++)
        {
            for (int y = 0; y < vHeight - 1; y++)
            {
                var corners = new List<int>();
                int vc = 0;

                //Checks 4 corners on a group of adjacent vertices
                if (this.depthBodyIndex[x, y] == 1)
                {
                    corners.Add(vIndex[x, y]);
                    vc++;
                }
                if (depthBodyIndex[x + 1, y] == 1)
                {
                    corners.Add(vIndex[x + 1, y]);
                    vc++;
                }
                if (depthBodyIndex[x, y + 1] == 1)
                {
                    corners.Add(vIndex[x, y + 1]);
                    vc++;
                }
                if (depthBodyIndex[x + 1, y + 1] == 1)
                {
                    corners.Add(vIndex[x + 1, y + 1]);
                    vc++;
                }

                //If all corners are present, 2 triangles are created, in order to fill the square
                if (vc == 4)
                {
                    triangles.Add(corners[0]);
                    triangles.Add(corners[1]);
                    triangles.Add(corners[2]);
                    triangles.Add(corners[2]);
                    triangles.Add(corners[1]);
                    triangles.Add(corners[3]);
                }
                //If only 3 corners are present, 1 triangle is created
                else if (vc == 3)
                {
                    triangles.Add(corners[0]);
                    triangles.Add(corners[1]);
                    triangles.Add(corners[2]);
                }
                //If less than 3 "corners" are present, no triangles are create
            }

        }

        return triangles.ToArray();
    }

    /// <summary>
    /// Finds the pixels which lie at the edge of the depth body index array.
    /// </summary>
    /// <returns> Indices of those vertices which are at the edge of the mesh. </returns>
    public int[] GetScannedEdges()
    {
        this.scannedEdges = new List<int>();
        for (int x = 0; x < vWidth; x++)
        {
            for (int y = 0; y < vHeight; y++)
            {
                if (this.depthBodyIndex[x, y] == 1)
                {
                    Vector2[] sp = GetSurroundingPoints(x, y, vWidth, vHeight);
                    int c = 0;
                    foreach (Vector2 v in sp)
                    {
                        int vX = (int)v.x;
                        int vY = (int)v.y;
                        if (this.depthBodyIndex[vX, vY] == 1)
                        {
                            c++;
                        }
                    }
                    if (c < 8)
                    {
                        scannedEdges.Add(verticesIndex[x,y]);
                    }
                }
            }
        }

        scannedEdges.Sort();//Sorted by index value (x + y * vWidth)

        int count = scannedEdges.Count;
        int fr = scannedEdges[0] / vWidth;//gives y value
        int lr = scannedEdges[count - 1] / vWidth;
        int cr = 0;

        List<int> leftbottom = new List<int>();
        List<int> topright = new List<int>();

        for (int i = 0; i < count; i++)
        {
            int y = scannedEdges[i] / vWidth;
            if (y == fr)
            {
                topright.Add(scannedEdges[i]);
            }
            else if (y == lr)
            {
                leftbottom.Add(scannedEdges[i]);
            }
            else if (y == cr)
            {
                topright.Add(scannedEdges[i]);
            }
            else
            {
                leftbottom.Add(scannedEdges[i]);
            }
        }

        for (int i = topright.Count - 1; i <= 0; i--)
        {
            leftbottom.Add(topright[i]);
        }

        return leftbottom.ToArray();
    }

    /// <summary>
    /// (Debugging) Creates Unity Texture2D  from masked pixel array.
    /// </summary>
    /// <param name="mask"> The body index array to be displayed. </param>
    /// <returns> An image which displays the masking array. (Masked pixels in black, unmasked pixels in blue if they have a value of 1, or in red if higher.) </returns>
    public Texture2D GetMaskImage(int[,] mask)
    {        
        int width = mask.GetLength(0);
        int height = mask.GetLength(1);
        Color[] maskColors = new Color[width * height];

        for(int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color maskPixel = Color.black;
                if (mask[x, y] == 1) maskPixel = Color.blue;
                else if (mask[x, y] > 1) maskPixel = Color.red;
                maskColors[x + y * width] = maskPixel;
            }
        }

        Texture2D maskImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
        maskImage.SetPixels(maskColors);
        maskImage.Apply();
        return maskImage;
    }

    /// <summary>
    /// (Debugging) Applies mask to texture image.
    /// </summary>
    /// <param name="mask"> The body index array to be used for masking.. </param>
    /// <returns> An image which displays the color data stream with the mask applied. (Masked pixels in green, unmasked pixels in the original if they have a value of 1, or in red if higher.) </returns>
    public Texture2D GetMaskedImage()
    {
        int width = this.texture.width;
        int height = this.texture.height;
        Color[] maskedColors = this.texture.GetPixels();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int i = x + y * width;
                if (this.colorBodyIndex[x, y] == 0) maskedColors[i] = Color.green;
                if (this.colorBodyIndex[x, y] == 2) maskedColors[i] = Color.red;
            }
        }

        Texture2D maskedImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
        maskedImage.SetPixels(maskedColors);
        maskedImage.Apply();
        return maskedImage;
    }

    /// <summary>
    /// (Debugging) Applies mask to texture image and gridlines for determining image segments.
    /// </summary>
    /// <param name="mask"> The body index array to be used for masking.. </param>
    /// <returns> An image which displays the color data stream with the mask applied. (Masked pixels in green, unmasked pixels in the original if they have a value of 1, or in red if higher.) </returns>
    public Texture2D GetGriddedMaskedImage()
    {
        int width = this.texture.width;
        int height = this.texture.height;
        Color[] maskedColors = this.texture.GetPixels();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int i = x + y * width;
                if (this.colorBodyIndex[x, y] == 0) maskedColors[i] = Color.green;
                if ((x % 30) == 0 || (y % 30) == 0) maskedColors[i] = Color.red;
            }
        }

        Texture2D maskedImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
        maskedImage.SetPixels(maskedColors);
        maskedImage.Apply();
        return maskedImage;
    }

    /// <summary>
    /// "Stretches" the unmasked portions of the texture over the masked portions so that skinning of the combined meshes does not show background colours at seams.
    /// </summary>
    /// <returns> Texture with masked pixels with colour values extrapolated from unmasked pixels </returns>
    public Texture2D GetExtrapolatedImage()
    {
        int width = this.texture.width;
        int height = this.texture.height;
        Color[] maskedColors = this.texture.GetPixels();

        //For each row, move past masked points until an umasked point is reached
        for(int x = 0; x < width; x++)
        {
            int ty = 0;
            Color extrap = Color.clear;
            while (extrap == Color.clear && ty < height)
            {
                if(this.colorBodyIndex[x,ty] == 0)
                {
                    ty++;
                }
                else
                {
                    extrap = maskedColors[(x + ty * width)];//Store colour of unmasked point
                }
            }

            for(int y = 0; y < height; y++)
            {
                if((y < ty)) maskedColors[(x + y * width)] = extrap; //If above unmasked point, match color.
                else if(this.colorBodyIndex[x,y] == 0) maskedColors[(x + y * width)] = Color.clear; //If not, remain clear.
            }
        }

        //For each column, move past masked points until an umasked point is reached
        for (int y = 0; y < height; y++)
        {
            int lx = 0;
            int hx = width-1;
            Color extrapL = Color.clear;
            Color extrapH = Color.clear;

            //Moving left to right
            while (extrapL == Color.clear && lx < width)
            {
                if (this.colorBodyIndex[lx, y] == 0)
                {
                    lx++;
                }
                else
                {
                    extrapL = maskedColors[(lx + y * width)];//Store colour of unmasked points on left extreme side
                }
            }

            //Moving right to left
            while (extrapH == Color.clear && hx >= 0)
            {
                if (this.colorBodyIndex[hx, y] == 0)
                {
                    hx--;
                }
                else
                {
                    extrapH = maskedColors[(hx + y * width)];//Store colour of unmasked points on right extreme side
                }
            }


            for (int x = 0; x < width; x++)
            {
                if (x < lx) maskedColors[(x + y * width)] = extrapL; //If to left of left extreme, match color.
                else if (x < hx) maskedColors[(x + y * width)] = extrapH; //If to right of right extreme, match color.
                else if (this.colorBodyIndex[x, y] == 0) maskedColors[(x + y * width)] = Color.clear; //Otherwise leave clear.
            }
        }

        Texture2D extrapolatedImage = new Texture2D(width, height, TextureFormat.RGBA32, false);
        extrapolatedImage.SetPixels(maskedColors);
        extrapolatedImage.Apply();
        return extrapolatedImage;
    }

    /// <summary>
    /// Flips the texture of the mesh. (For application on the back of a combined mesh)
    /// </summary>
    public void ReverseTexture()
    {
        for(int x = 0; x < this.uvs.GetLength(0); x++)
        {
            for(int y = 0; y < this.uvs.GetLength(1); y++)
            {
                this.uvs[x, y] = Vector2.one - uvs[x, y];
            }
        }

        Color[] colors = this.texture.GetPixels();
        Color[] newColors = new Color[colors.Length];
        for(int i = 0; i < colors.Length; i++)
        {
            newColors[i] = colors[colors.Length - 1 - i];
        }
        this.texture.SetPixels(newColors);
        this.texture.Apply();

        int width = this.colorBodyIndex.GetLength(0);
        int height = this.colorBodyIndex.GetLength(1);
        int[,] newColorBodyIndex = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                newColorBodyIndex[x, y] = this.colorBodyIndex[width - 1 - x, height - 1 - y];
            }
        }
        this.colorBodyIndex = newColorBodyIndex;
    }

    /// <summary>
    /// Finds the minimum and maximum points of an array of UV vectors.
    /// </summary>
    /// <param name="uvs"> The array of UV vectors for which minimums and maximums are to be determined. </param>
    /// <returns> A float array representing the minimum and maximum UV values. {0: minX, 1: maxX, 2:minY, 3:maxY} </returns>
    public float[] GetUVMinMax(Vector2[] uvs)
    {
        float maxX = 0;
        float maxY = 0;
        float minX = 1;
        float minY = 1;

        foreach(Vector2 uv in uvs)
        {
            float x = uv.x;
            float y = uv.y;

            if (x > maxX && x < 1) maxX = x;

            if (x < minX && x > 0) minX = x;

            if (y > maxY && y < 1) maxY = y;

            if (y < minY && y > 0) minY = y;
        }

        if (minX == 1) minX = 0;
        if (maxX == 0) maxX = 1;
        if (minY == 1) minY = 0;
        if (maxY == 0) maxY = 1;
        
        float[] xyMinMax = { minX, maxX, minY, maxY };

        return xyMinMax;
    }

    /// <summary>
    /// From a set of minimum and maximum values for UVs, finds the pixel indices for those values on an image.
    /// </summary>
    /// <param name="uvMinMax"> A float array representing the minimum and maximum UV values. {0: minX, 1: maxX, 2:minY, 3:maxY} </param>
    /// <param name="interval"> The number to which a nearest multiple must be rounded to. Minimum value of 1. </param>
    /// <returns> A int array representing the minimum and maximum pixel indices which bound a set of UVs, rounded to the neareast multiple of the interval. {0: minX, 1: maxX, 2:minY, 3:maxY} </returns>
    public int[] GetXYMinMax(float[] uvMinMax, int interval)
    {
        int wFactor = this.texture.width / interval;
        int hFactor = this.texture.height / interval;

        int[] xyMinMax = new int[4];

        xyMinMax[0] = (int) Math.Floor(uvMinMax[0] * wFactor) * interval;
        xyMinMax[1] = (int) Math.Ceiling(uvMinMax[1] * wFactor) * interval;
        xyMinMax[2] = (int) Math.Floor(uvMinMax[2] * hFactor) * interval;
        xyMinMax[3] = (int) Math.Ceiling(uvMinMax[3] * hFactor) * interval;

        return xyMinMax;
    }

    /// <summary>
    /// Returns a segment of the texture as indicated by the boundaries indicated by the input pixel indices.
    /// </summary>
    /// <param name="xyMinMax"> An int array represent the maximum and minimum x and y indices of the desired segment {0: minX, 1: maxX, 2:minY, 3:maxY} </param>
    /// <returns></returns>
    public Texture2D GetTextureSegment(int[] xyMinMax)
    {
        int x = xyMinMax[0];
        int y = xyMinMax[2];
        int blockWidth = xyMinMax[1] - x;
        int blockHeight = xyMinMax[3] - y;

        Color[] segmentColors = this.GetMaskedImage().GetPixels(x, y, blockWidth, blockHeight);
        Texture2D segmentImage = new Texture2D(blockWidth, blockHeight, TextureFormat.RGBA32, false);
        segmentImage.SetPixels(segmentColors);
        segmentImage.Apply();
        return segmentImage;
    }

}




