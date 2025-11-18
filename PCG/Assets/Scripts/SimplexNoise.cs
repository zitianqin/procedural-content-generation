using UnityEngine;
using System;

public class SimplexNoise : MonoBehaviour
{
    private int[] perm;
    private static readonly int[][] grad3 = {
        new[]{1,1,0}, new[]{-1,1,0}, new[]{1,-1,0}, new[]{-1,-1,0},
        new[]{1,0,1}, new[]{-1,0,1}, new[]{1,0,-1}, new[]{-1,0,-1},
        new[]{0,1,1}, new[]{0,-1,1}, new[]{0,1,-1}, new[]{0,-1,-1}
    };

    private static readonly int[] basePermutation = {
        151,160,137,91,90,15,
        131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
        190,6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
        88,237,149,56,87,174,20,125,136,171,168,68,175,74,165,71,134,139,48,27,166,
        77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
        102,143,54,65,25,63,161,1,216,80,73,209,76,132,187,208,89,18,169,200,196,
        135,130,116,188,159,86,164,100,109,198,173,186,3,64,52,217,226,250,124,123,
        5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
        223,183,170,213,119,248,152,2,44,154,163,70,221,153,101,155,167,43,172,9,
        129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,218,246,97,228,
        251,34,242,193,238,210,144,12,191,179,162,241,81,51,145,235,249,14,239,107,
        49,192,214,31,181,199,106,157,184,84,204,176,115,121,50,45,127,4,150,254,
        138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    };

    void Start()
    {
        int seed = 100000;
        Init(seed);

        int size = 32;
        float scale = 4f;

        Texture3D cubeTex = GenerateNoiseCube(size, scale);

        for (int z = 0; z < size; z += 4)
        {
            Texture2D slice = new Texture2D(size, size, TextureFormat.RFloat, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float val = cubeTex.GetPixel(x, y, z).r;
                    slice.SetPixel(x, y, new Color(val, val, val, 1));
                }
            }

            slice.Apply();

            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.transform.position = new Vector3(0, 0, z * 0.2f);
            plane.GetComponent<MeshRenderer>().material =
                new Material(Shader.Find("Unlit/Texture"));
            plane.GetComponent<MeshRenderer>().material.mainTexture = slice;
        }
    }

    // Scramble the perms array after making a copy
    public void Init(int seed)
    {
        int[] permBase = (int[])basePermutation.Clone();

        System.Random rand = new System.Random(seed);

        for (int i = permBase.Length - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            (permBase[i], permBase[j]) = (permBase[j], permBase[i]);
        }

        // double number elements
        perm = new int[512];
        for (int i = 0; i < 512; i++)
            perm[i] = permBase[i & 255];
    }


    // Dot product for gradients
    private static double Dot(int[] g, double x, double y, double z)
    {
        return g[0] * x + g[1] * y + g[2] * z;
    }

    // Main simplex noise function
    public double Noise(double x, double y, double z)
    {
        // skew constants
        const double F3 = 1.0 / 3.0; // 1/3 for 3D, comes from (sqrt(4) - 1)/3, as the formula is Fn = (sqrt(n+1) - 1)/n
        const double G3 = 1.0 / 6.0; // Formula is Fn/(n+1), from a bit of trial and error though, 1/6 works better than 1/12.

        // Skew the input space
        double s = (x + y + z) * F3;
        int i = Mathf.FloorToInt((float)(x + s));
        int j = Mathf.FloorToInt((float)(y + s));
        int k = Mathf.FloorToInt((float)(z + s));

        double t = (i + j + k) * G3;
        double X0 = i - t;
        double Y0 = j - t;
        double Z0 = k - t;

        double x0 = x - X0;
        double y0 = y - Y0;
        double z0 = z - Z0;

        // Determine simplex corners
        int i1, j1, k1; // 2nd corner
        int i2, j2, k2; // 3rd corner

        if (x0 >= y0)
        {
            if (y0 >= z0)
            {
                i1 = 1;
                j1 = 0;
                k1 = 0;
                i2 = 1;
                j2 = 1;
                k2 = 0;
            }
            else if (x0 >= z0)
            {
                i1 = 1;
                j1 = 0;
                k1 = 0;
                i2 = 1;
                j2 = 0;
                k2 = 1;
            }
            else
            {
                i1 = 0;
                j1 = 0;
                k1 = 1;
                i2 = 1;
                j2 = 0;
                k2 = 1;
            }
        }
        else
        {
            if (y0 < z0)
            {
                i1 = 0;
                j1 = 0;
                k1 = 1;
                i2 = 0;
                j2 = 1;
                k2 = 1;
            }
            else if (x0 < z0)
            {
                i1 = 0;
                j1 = 1;
                k1 = 0;
                i2 = 0;
                j2 = 1;
                k2 = 1;
            }
            else
            {
                i1 = 0;
                j1 = 1;
                k1 = 0;
                i2 = 1;
                j2 = 1;
                k2 = 0;
            }
        }

        // Offsets
        double x1 = x0 - i1 + G3;
        double y1 = y0 - j1 + G3;
        double z1 = z0 - k1 + G3;

        double x2 = x0 - i2 + 2 * G3;
        double y2 = y0 - j2 + 2 * G3;
        double z2 = z0 - k2 + 2 * G3;

        double x3 = x0 - 1 + 3 * G3;
        double y3 = y0 - 1 + 3 * G3;
        double z3 = z0 - 1 + 3 * G3;

        // Hash gradients
        int grad0 = perm[i + perm[j + perm[k]]] % 12;
        int grad1 = perm[i + i1 + perm[j + j1 + perm[k + k1]]] % 12;
        int grad2 = perm[i + i2 + perm[j + j2 + perm[k + k2]]] % 12;
        int grad3 = perm[i + 1 + perm[j + 1 + perm[k + 1]]] % 12;

        // Contribution from corners
        double n0, n1, n2, n3;

        double t0 = 0.6 - x0*x0 - y0*y0 - z0*z0; // 0.6 was chosen by Ken Perlin as the maximum squared distance from the center of a simplex corner's influence sphere, basically how much contribution a corner has.
        n0 = (t0 < 0) ? 0 : (t0 * t0 * t0 * t0 * Dot(grad3[grad0], x0, y0, z0));

        double t1 = 0.6 - x1*x1 - y1*y1 - z1*z1;
        n1 = (t1 < 0) ? 0 : (t1 * t1 * t1 * t1 * Dot(grad3[grad1], x1, y1, z1));

        double t2 = 0.6 - x2*x2 - y2*y2 - z2*z2;
        n2 = (t2 < 0) ? 0 : (t2 * t2 * t2 * t2 * Dot(grad3[grad2], x2, y2, z2));

        double t3 = 0.6 - x3*x3 - y3*y3 - z3*z3;
        n3 = (t3 < 0) ? 0 : (t3 * t3 * t3 * t3 * Dot(grad3[grad3], x3, y3, z3));

        // Combines all 4 corner contributions and scales to the range [0, 1]
        return 32.0 * (n0 + n1 + n2 + n3) * 0.5 + 0.5;
    }

    public Texture3D GenerateNoiseCube(int size, float scale)
    {
        Texture3D tex = new Texture3D(size, size, size, TextureFormat.RFloat, false);
        Color[] colors = new Color[size * size * size];

        int index = 0;
        for (int z = 0; z < size; z++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double nx = x / (double)size * scale;
                    double ny = y / (double)size * scale;
                    double nz = z / (double)size * scale;

                    double val = Noise(nx, ny, nz);
                    colors[index++] = new Color((float)val, 0, 0, 1);
                }
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        return tex;
    }
}