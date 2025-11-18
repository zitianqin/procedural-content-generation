using UnityEngine;
using UnityEngine.Rendering;

public class ProceduralTerrain : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private int seed = 100000;
    [SerializeField] private float frequency = 8f;
    [SerializeField] private float heightMultiplier = 200f;
    [SerializeField, Min(1)] private int octaves = 4;
    [SerializeField, Range(0f, 1f)] private float persistence = 0.5f;
    [SerializeField, Min(1f)] private float lacunarity = 2f;
    [SerializeField] private Vector2 noiseOffset = Vector2.zero;

    [Header("Mesh Settings")]
    [SerializeField] private int meshResolution = 256;
    [SerializeField] private float planeSize = 500f;
    [SerializeField] private float uvScale = 50f;

    private PerlinNoise noiseGenerator;
    private Vector3[] octaveOffsets;

    private struct TerrainVertex
    {
        public float height;
        public Color color;
    }

    private void Awake()
    {
        noiseGenerator = new PerlinNoise(seed);
        BuildOctaveOffsets();
    }

    private void Start()
    {
        Mesh mesh = GenerateNoiseMesh(meshResolution, planeSize, frequency, heightMultiplier, uvScale);

        GameObject plane = new GameObject("PerlinNoisePlane", typeof(MeshFilter), typeof(MeshRenderer));
        plane.transform.position = Vector3.zero;

        MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Custom/VertexColorShader"));
        renderer.material = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Creates vertices, colours and triangles for a Perlin-based surface
    private Mesh GenerateNoiseMesh(int resolution, float size, float noiseFrequency, float heightScale, float uvTiling)
    {
        int vertsPerAxis = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerAxis * vertsPerAxis];
        Vector2[] uvs = new Vector2[vertsPerAxis * vertsPerAxis];
        Color[] colors = new Color[vertsPerAxis * vertsPerAxis];
        int[] triangles = new int[resolution * resolution * 6];

        int vertexIndex = 0;
        for (int y = 0; y < vertsPerAxis; y++)
        {
            float percentY = y / (float)resolution;
            for (int x = 0; x < vertsPerAxis; x++)
            {
                float percentX = x / (float)resolution;

                double noiseValue = noiseGenerator.FractalNoise(
                    new Vector3(percentX, percentY, 0f),
                    octaves,
                    persistence,
                    lacunarity,
                    noiseFrequency,
                    octaveOffsets);
                float originalHeight = ((float)noiseValue - 0.5f) * heightScale;

                float posX = (percentX - 0.5f) * size;
                float posZ = (percentY - 0.5f) * size;

                TerrainVertex terrainVertex = ProcessTerrainVertex(originalHeight);
                vertices[vertexIndex] = new Vector3(posX, terrainVertex.height, posZ);
                uvs[vertexIndex] = new Vector2(percentX * uvTiling, percentY * uvTiling);
                colors[vertexIndex] = terrainVertex.color;

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
            indexFormat = vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            vertices = vertices,
            triangles = triangles,
            uv = uvs,
            colors = colors
        };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    // Assign colour based on the terrain height, flattening water areas
    private TerrainVertex ProcessTerrainVertex(float originalHeight)
    {
        float oceanThreshold = -0.9f * 20f;
        float beachStartThreshold = -0.9f * 20f;
        float beachEndThreshold = -0.7f * 20f;
        float grassEndThreshold = -0.3f * 20f;
        float snowStartThreshold = 0.9f * 20f;
        // float maxHeight = 1.0f * 20f;

        float height = originalHeight;
        Color color;

        float oceanLevel = oceanThreshold;
        if (height <= oceanThreshold)
        {
            height = oceanLevel;
        }

        if (height <= oceanThreshold)
        {
            color = new Color(0.1f, 0.3f, 0.8f);
        }
        else if (height > beachStartThreshold && height <= beachEndThreshold)
        {
            color = new Color(0.9f, 0.8f, 0.5f);
        }
        else if (height > beachEndThreshold && height <= grassEndThreshold)
        {
            color = new Color(0.3f, 0.7f, 0.3f);
        }
        else if (height > grassEndThreshold && height <= snowStartThreshold)
        {
            color = new Color(0.25f, 0.25f, 0.25f);
        }
        else
        {
            color = new Color(0.98f, 0.98f, 0.96f);
        }

        return new TerrainVertex { height = height, color = color };
    }

    private void BuildOctaveOffsets()
    {
        System.Random prng = new System.Random(seed);
        int octaveCount = Mathf.Max(1, octaves);
        octaveOffsets = new Vector3[octaveCount];
        for (int i = 0; i < octaveCount; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + noiseOffset.x;
            float offsetY = prng.Next(-100000, 100000) + noiseOffset.y;
            float offsetZ = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector3(offsetX, offsetY, offsetZ);
        }
    }
}
