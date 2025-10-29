using System;
using UnityEngine;
using UnityEngine.Rendering;

public class PerlinNoise : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private int[] p; // local permutation array

    // base permutation
    private static readonly int[] basePermutation =
    {
        151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225,
        140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148,
        247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32,
        57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175,
        74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122,
        60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54,
        65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169,
        200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64,
        52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212,
        207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213,
        119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9,
        129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104,
        218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241,
        81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157,
        184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93,
        222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180
    };

    // generate a random number based on seed
    public static double random(int seed)
    {
        double x = Math.Sin(seed++) * 10000.0;
        return x - Math.Floor(x);
    }

    // Find gradient vectors for cells
    public static double grad(int hash, double x, double y, double z)
    {
        switch (hash & 0xF)
        {
            case 0x0: return x + y;
            case 0x1: return -x + y;
            case 0x2: return x - y;
            case 0x3: return -x - y;
            case 0x4: return x + z;
            case 0x5: return -x + z;
            case 0x6: return x - z;
            case 0x7: return -x - z;
            case 0x8: return y + z;
            case 0x9: return -y + z;
            case 0xA: return y - z;
            case 0xB: return -y - z;
            case 0xC: return y + x;
            case 0xD: return -y + z;
            case 0xE: return y - x;
            case 0xF: return -y - z;
            default: return 0; // never happens
        }
    }


    // shuffles permutation based on seed
    private static void shuffle(int[] arr, int seed)
    {
        System.Random num = new System.Random(seed);
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = num.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }

    public void pnMain(int seedRes)
    {
        p = new int[512];
        int[] perm = (int[])basePermutation.Clone();
        shuffle(perm, seedRes);
        for (int i = 0; i < 512; i++)
        {
            p[i] = perm[i % 256];
        }
    }

    // fade function
    public static double fade(double t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    // Returns pseudorandom gradient index for coordinates using the permutation array to generate consistent pseudorandom values
    private int hash(int x, int y, int z)
    {
        return p[p[p[x] + y] + z];
    }

    public static double lerp(double a, double b, double t)
    {
        return a + t * (b - a);
    }

    // Main noise function, comes up with 8 points and smoothly combines them with a line
    public double noise(double x, double y, double z)
    {
        // used to find the gradients
        int X = (int)Math.Floor(x) & 255;
        int Y = (int)Math.Floor(y) & 255;
        int Z = (int)Math.Floor(z) & 255;

        // For points inside unit cube
        x -= Math.Floor(x);
        y -= Math.Floor(y);
        z -= Math.Floor(z);

        double u = fade(x);
        double v = fade(y);
        double w = fade(z);

        int A = p[X] + Y;
        int AA = p[A] + Z;
        int AB = p[A + 1] + Z;
        int B = p[X + 1] + Y;
        int BA = p[B] + Z;
        int BB = p[B + 1] + Z;

        // Do dot product and lerping stuff
        double res = lerp(
            lerp(
                lerp(
                    grad(p[AA], x, y, z),
                    grad(p[BA], x - 1, y, z),
                    u),
                lerp(
                    grad(p[AB], x, y - 1, z),
                    grad(p[BB], x - 1, y - 1, z),
                    u),
                v),
            lerp(
                lerp(
                    grad(p[AA + 1], x, y, z - 1),
                    grad(p[BA + 1], x - 1, y, z - 1),
                    u),
                lerp(
                    grad(p[AB + 1], x, y - 1, z - 1),
                    grad(p[BB + 1], x - 1, y - 1, z - 1),
                    u),
                v),
            w);

        return (res + 1.0) / 2.0; // Normalise result to [0,1]
    }

    public Mesh GenerateNoiseMesh(int resolution, float size, float frequency, float heightMultiplier, float uvScale)
    {
        int vertsPerAxis = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerAxis * vertsPerAxis];
        Vector2[] uvs = new Vector2[vertsPerAxis * vertsPerAxis];
        int[] triangles = new int[resolution * resolution * 6];

        int vertexIndex = 0;
        for (int y = 0; y < vertsPerAxis; y++)
        {
            float percentY = y / (float)resolution;
            for (int x = 0; x < vertsPerAxis; x++)
            {
                float percentX = x / (float)resolution;

                double noiseValue = noise(percentX * frequency, percentY * frequency, 0.0);
                float height = ((float)noiseValue - 0.5f) * heightMultiplier;

                float posX = (percentX - 0.5f) * size;
                float posZ = (percentY - 0.5f) * size;

                vertices[vertexIndex] = new Vector3(posX, height, posZ);
                uvs[vertexIndex] = new Vector2(percentX * uvScale, percentY * uvScale);
                vertexIndex++;
            }
        }

        int triangleIndex = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int bottomLeft = y * vertsPerAxis + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + vertsPerAxis;
                int topRight = topLeft + 1;

                triangles[triangleIndex++] = bottomLeft;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = bottomRight;

                triangles[triangleIndex++] = bottomRight;
                triangles[triangleIndex++] = topLeft;
                triangles[triangleIndex++] = topRight;
            }
        }

        Mesh mesh = new Mesh
        {
            indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16, // switch to 32-bit indices if vertex count exceeds 16-bit limit
            vertices = vertices,
            triangles = triangles,
            uv = uvs // store generated UV coordinates for texturing
        };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }


    void Start()
    {
        int seedRes = 100000; // (int)Math.Floor(random(100));
        pnMain(seedRes);

        int meshResolution = 256;
        float planeSize = 500f;
        float noiseFrequency = 8f;
        float heightMultiplier = 20f;
        float uvScale = 50f;

        Mesh mesh = GenerateNoiseMesh(meshResolution, planeSize, noiseFrequency, heightMultiplier, uvScale);

        GameObject plane = new GameObject("PerlinNoisePlane", typeof(MeshFilter), typeof(MeshRenderer));
        plane.transform.position = Vector3.zero;

        MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = Color.white;
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}