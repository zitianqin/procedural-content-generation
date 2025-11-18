using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ProceduralTerrain : MonoBehaviour
{
    private enum NoiseType
    {
        Perlin,
        Simplex,
        Value
    }

    [Header("Noise Settings")]
    [SerializeField] private NoiseType noiseType = NoiseType.Perlin;
    [SerializeField] private int seed = 100000;
    [SerializeField] private float frequency = 8f;
    [SerializeField] private float heightMultiplier = 200f;
    [SerializeField] private AnimationCurve heightCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 1f, 1f),
        new Keyframe(0.5f, 0.3f, 0.5f, 0.5f),
        new Keyframe(0.8f, 0.7f, 0.5f, 0.5f),
        new Keyframe(0.9f, 0.85f, 2f, 2f),
        new Keyframe(1f, 1f, 3f, 3f));
    [SerializeField, Min(1)] private int octaves = 4;
    [SerializeField, Range(0f, 1f)] private float persistence = 0.5f;
    [SerializeField, Min(1f)] private float lacunarity = 2f;
    [SerializeField] private Vector2 noiseOffset = Vector2.zero;

    [Header("Mesh Settings")]
    [SerializeField] private int meshResolution = 256;
    [SerializeField] private float planeSize = 500f;
    [SerializeField] private float uvScale = 50f;

    [Header("Biome Settings")]
    [SerializeField, Range(0f, 1f)] private float oceanLevelNormalised = 0.41f;
    [SerializeField, Range(0f, 1f)] private float beachEndNormalised = 0.43f;
    [SerializeField, Range(0f, 1f)] private float grassEndNormalised = 0.47f;
    [SerializeField, Range(0f, 1f)] private float snowStartNormalised = 0.59f;
    [SerializeField, Range(0f, 0.2f)] private float beachGrassBlendNormalised = 0.005f;
    [SerializeField, Range(0f, 0.2f)] private float grassRockBlendNormalised = 0.005f;
    [SerializeField, Range(0f, 0.2f)] private float snowBlendNormalised = 0.005f;

    [Header("Color Settings")]
    [SerializeField] private Color oceanColour = new Color(0.1f, 0.3f, 0.8f);
    [SerializeField] private Color beachColour = new Color(0.9f, 0.8f, 0.5f);
    [SerializeField] private Color grassColour = new Color(0.3f, 0.7f, 0.3f);
    [SerializeField] private Color rockColour = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color snowColour = new Color(0.98f, 0.98f, 0.96f);
    private const string ImportedWaterMaterialResource = "Water_mat_01";
    private const string ImportedWaterMeshResource = "WaterBlock_50m";

    private PerlinNoise perlinNoiseGenerator;
    private SimplexNoise simplexNoiseGenerator;
    private ValueNoise valueNoiseGenerator;
    private Vector3[] octaveOffsets;

    private struct TerrainVertex
    {
        public float height;
        public Color colour;
    }

    private void Awake()
    {
        perlinNoiseGenerator = new PerlinNoise(seed);
        simplexNoiseGenerator = new SimplexNoise(seed);
        valueNoiseGenerator = new ValueNoise(seed);
        BuildOctaveOffsets();
    }

    private void Start()
    {
        Mesh mesh = GenerateNoiseMesh(meshResolution, planeSize, frequency, heightMultiplier, uvScale, out Color[] vertexColors);

        GameObject plane = new GameObject("PerlinNoisePlane", typeof(MeshFilter), typeof(MeshRenderer));
        plane.transform.position = Vector3.zero;

        MeshFilter meshFilter = plane.GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;

        MeshRenderer renderer = plane.GetComponent<MeshRenderer>();
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Texture2D vertexColorTexture = BuildVertexColorTexture(vertexColors, meshResolution + 1);
        material.SetTexture("_BaseMap", vertexColorTexture);
        float minUvScale = Mathf.Max(1e-4f, uvScale);
        material.SetTextureScale("_BaseMap", Vector2.one / minUvScale);
        material.SetTextureOffset("_BaseMap", Vector2.zero);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.1f);
        material.SetFloat("_Metallic", 0f);
        renderer.material = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;

        float minHeight = -heightMultiplier * 0.5f;
        float maxHeight = heightMultiplier * 0.5f;
        float oceanHeight = Mathf.Lerp(minHeight, maxHeight, oceanLevelNormalised);
        CreateWaterPlane(planeSize, oceanHeight);
    }

    // Creates vertices, colours and triangles for a Perlin-based surface
    private Mesh GenerateNoiseMesh(int resolution, float size, float noiseFrequency, float heightScale, float uvTiling, out Color[] vertexColours)
    {
        int vertsPerAxis = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerAxis * vertsPerAxis];
        Vector2[] uvs = new Vector2[vertsPerAxis * vertsPerAxis];
        Color[] colours = new Color[vertsPerAxis * vertsPerAxis];
        int[] triangles = new int[resolution * resolution * 6];

        int vertexIndex = 0;
        for (int y = 0; y < vertsPerAxis; y++)
        {
            float percentY = y / (float)resolution;
            for (int x = 0; x < vertsPerAxis; x++)
            {
                float percentX = x / (float)resolution;

                Vector3 samplePoint = new Vector3(percentX, percentY, 0f);
                double noiseValue;

                switch (noiseType)
                {
                    case NoiseType.Simplex:
                        noiseValue = simplexNoiseGenerator.FractalNoise(
                            samplePoint,
                            octaves,
                            persistence,
                            lacunarity,
                            noiseFrequency,
                            octaveOffsets);
                        break;
                    case NoiseType.Value:
                        noiseValue = valueNoiseGenerator.FractalNoise(
                            samplePoint,
                            octaves,
                            persistence,
                            lacunarity,
                            noiseFrequency,
                            octaveOffsets);
                        break;
                    default:
                        noiseValue = perlinNoiseGenerator.FractalNoise(
                            samplePoint,
                            octaves,
                            persistence,
                            lacunarity,
                            noiseFrequency,
                            octaveOffsets);
                        break;
                }
                float originalHeight = ((float)noiseValue - 0.5f) * heightScale;

                float posX = (percentX - 0.5f) * size;
                float posZ = (percentY - 0.5f) * size;

                TerrainVertex terrainVertex = ProcessTerrainVertex(originalHeight);
                vertices[vertexIndex] = new Vector3(posX, terrainVertex.height, posZ);
                uvs[vertexIndex] = new Vector2(percentX * uvTiling, percentY * uvTiling);
                colours[vertexIndex] = terrainVertex.colour;

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
            colors = colours
        };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        vertexColours = colours;
        return mesh;
    }

    private Texture2D BuildVertexColorTexture(Color[] colours, int vertsPerAxis)
    {
        Texture2D texture = new Texture2D(vertsPerAxis, vertsPerAxis, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "TerrainVertexColors"
        };

        for (int y = 0; y < vertsPerAxis; y++)
        {
            for (int x = 0; x < vertsPerAxis; x++)
            {
                int index = y * vertsPerAxis + x;
                texture.SetPixel(x, y, colours[index]);
            }
        }

        texture.Apply(false, false);
        return texture;
    }

    private void CreateWaterPlane(float size, float oceanHeight)
    {
        GameObject waterPlane = new GameObject("WaterPlane", typeof(MeshFilter), typeof(MeshRenderer));
        waterPlane.transform.position = new Vector3(0f, oceanHeight, 0f);

        // Scale the water plane
        MeshFilter meshFilter = waterPlane.GetComponent<MeshFilter>();
        Mesh importedMesh = Resources.Load<Mesh>(ImportedWaterMeshResource);
        meshFilter.sharedMesh = importedMesh;
        Vector3 meshSize = importedMesh.bounds.size;
        float scaleX = size / Mathf.Max(1e-4f, meshSize.x);
        float scaleZ = size / Mathf.Max(1e-4f, meshSize.z);
        waterPlane.transform.localScale = new Vector3(scaleX, 1f, scaleZ);

        // Configure material
        MeshRenderer renderer = waterPlane.GetComponent<MeshRenderer>();
        Material importedWaterMaterial = Resources.Load<Material>(ImportedWaterMaterialResource);
        renderer.material = Instantiate(importedWaterMaterial);

        // Configure shadows
        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbesAndSkybox;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    // Assign colour based on the terrain height
    private TerrainVertex ProcessTerrainVertex(float originalHeight)
    {
        float minHeight = -heightMultiplier * 0.5f;
        float maxHeight = heightMultiplier * 0.5f;
        float heightRange = Mathf.Max(1e-3f, maxHeight - minHeight);

        float height = ApplyHeightCurve(originalHeight, minHeight, maxHeight);

        float beachEndThreshold = Mathf.Lerp(minHeight, maxHeight, beachEndNormalised);
        float grassEndThreshold = Mathf.Lerp(minHeight, maxHeight, grassEndNormalised);
        float snowStartThreshold = Mathf.Lerp(minHeight, maxHeight, snowStartNormalised);

        Color colour;

        float beachGrassBlendRange = beachGrassBlendNormalised * heightRange;
        float grassRockBlendRange = grassRockBlendNormalised * heightRange;
        float snowBlendRange = snowBlendNormalised * heightRange;

        float beachGrassBlendStart = beachEndThreshold - beachGrassBlendRange;
        float beachGrassBlendEnd = beachEndThreshold + beachGrassBlendRange;

        float grassRockBlendStart = grassEndThreshold - grassRockBlendRange;
        float grassRockBlendEnd = grassEndThreshold + grassRockBlendRange;

        float snowBlendStart = snowStartThreshold - snowBlendRange;
        float snowBlendEnd = snowStartThreshold + snowBlendRange;

        if (height <= beachGrassBlendStart)
        {
            colour = beachColour;
        }
        else if (height < beachGrassBlendEnd)
        {
            float blend = Mathf.InverseLerp(beachGrassBlendStart, beachGrassBlendEnd, height);
            colour = Color.Lerp(beachColour, grassColour, Mathf.Clamp01(blend));
        }
        else if (height <= grassRockBlendStart)
        {
            colour = grassColour;
        }
        else if (height < grassRockBlendEnd)
        {
            float blend = Mathf.InverseLerp(grassRockBlendStart, grassRockBlendEnd, height);
            colour = Color.Lerp(grassColour, rockColour, Mathf.Clamp01(blend));
        }
        else if (height < snowBlendStart)
        {
            colour = rockColour;
        }
        else
        {
            float blend = Mathf.InverseLerp(snowBlendStart, snowBlendEnd, height);
            colour = Color.Lerp(rockColour, snowColour, Mathf.Clamp01(blend));
        }

        return new TerrainVertex { height = height, colour = colour };
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

    private float ApplyHeightCurve(float originalHeight, float minHeight, float maxHeight)
    {
        if (heightCurve == null)
        {
            return originalHeight;
        }

        float normalisedHeight = Mathf.InverseLerp(minHeight, maxHeight, originalHeight);
        float curvedNormalised = Mathf.Clamp01(heightCurve.Evaluate(normalisedHeight));
        return Mathf.Lerp(minHeight, maxHeight, curvedNormalised);
    }
}
