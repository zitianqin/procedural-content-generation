// start with white noise
// separate into grids
// smooth it out, as well as along the edges

using UnityEngine;

public class valueNoise
{
    public static Texture3D GenerateTexture(int resolution, float timeOffset)
    {
        Texture3D tex = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBA32, false);

        Color[] cols = new Color[resolution * resolution * resolution];
        int index = 0;

        for (int z = 0; z < resolution; z++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (float)x / resolution;
                    float v = (float)y / resolution;
                    float w = (float)z / resolution;

                    Vector3 uvw = new Vector3(u, v, w + timeOffset / 10f);

                    float vn = Value(uvw * 4f) * 1f;
                    vn += Value(uvw * 8f) * 0.5f;
                    vn += Value(uvw * 16f) * 0.25f;
                    vn += Value(uvw * 32f) * 0.125f;
                    vn += Value(uvw * 64f) * 0.0625f;
                    vn /= 2f;

                    cols[index++] = new Color(vn, vn, vn, 1f);
                }
            }
        }

        tex.SetPixels(cols);
        tex.Apply();
        return tex;
    }

    static float Value(Vector3 p)
    {
        // Fractional component
        Vector3 gridUV = new Vector3(
            Mathf.Repeat(p.x, 1f),
            Mathf.Repeat(p.y, 1f),
            Mathf.Repeat(p.z, 1f)
        );

        // Grid cell integer coordinates
        Vector3 gridID = new Vector3(
            Mathf.Floor(p.x),
            Mathf.Floor(p.y),
            Mathf.Floor(p.z)
        );

        gridUV = Quintic(gridUV);

        // 8 corner samples
        float c000 = WhiteNoise(gridID);
        float c100 = WhiteNoise(gridID + new Vector3(1, 0, 0));
        float c010 = WhiteNoise(gridID + new Vector3(0, 1, 0));
        float c110 = WhiteNoise(gridID + new Vector3(1, 1, 0));

        float c001 = WhiteNoise(gridID + new Vector3(0, 0, 1));
        float c101 = WhiteNoise(gridID + new Vector3(1, 0, 1));
        float c011 = WhiteNoise(gridID + new Vector3(0, 1, 1));
        float c111 = WhiteNoise(gridID + new Vector3(1, 1, 1));

        // Interpolate along X
        float x00 = Mathf.Lerp(c000, c100, gridUV.x);
        float x10 = Mathf.Lerp(c010, c110, gridUV.x);
        float x01 = Mathf.Lerp(c001, c101, gridUV.x);
        float x11 = Mathf.Lerp(c011, c111, gridUV.x);

        // Interpolate along Y
        float y0 = Mathf.Lerp(x00, x10, gridUV.y);
        float y1 = Mathf.Lerp(x01, x11, gridUV.y);

        // Interpolate along Z
        return Mathf.Lerp(y0, y1, gridUV.z);
    }

    // Pseudo-Random generation of white noise
    static float WhiteNoise(Vector3 p)
    {
        float dotVal = p.x * 12f + p.y * 78f + p.z * 37f;
        float s = Mathf.Sin(dotVal);
        return Mathf.Repeat(s * 43758.5453f, 1f);
    }

    // Quintic Function for interpolation
    static Vector3 Quintic(Vector3 p)
    {
        return new Vector3(
        p.x * p.x * p.x * (10f + p.x * (-15f + 6f * p.x)),
        p.y * p.y * p.y * (10f + p.y * (-15f + 6f * p.y)),
        p.z * p.z * p.z * (10f + p.z * (-15f + 6f * p.z))
    );
    }
}