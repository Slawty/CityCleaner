using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SplitMeshGridEditor : EditorWindow
{
    const float ClipEpsilon = 0.0001f;
    const float MinTileSizeMeters = 0.1f;

    enum GridSplitMode
    {
        CellCount,
        FixedTileSize
    }

    enum MeshBuildApproach
    {
        ClipTriangles,
        ResampleGrid
    }

    enum ResampleDensityMode
    {
        SubdivisionsPerTile,
        VertexSpacing
    }

    enum GridOriginMode
    {
        MeshMin,
        WorldZero,
        CustomWorld
    }

    [SerializeField] GridSplitMode splitMode = GridSplitMode.FixedTileSize;
    [SerializeField] MeshBuildApproach meshBuildApproach = MeshBuildApproach.ResampleGrid;
    [SerializeField] int columns = 4;
    [SerializeField] int rows = 4;
    [SerializeField] float tileWidthMeters = 4f;
    [SerializeField] float tileHeightMeters = 4f;
    [SerializeField] GridOriginMode gridOriginMode = GridOriginMode.WorldZero;
    [SerializeField] Vector2 customGridOriginWorld;
    [SerializeField] ResampleDensityMode resampleDensityMode = ResampleDensityMode.SubdivisionsPerTile;
    [SerializeField] int subdivisionsPerTile = 4;
    [SerializeField] float vertexSpacingMeters = 0.5f;
    [SerializeField] bool addMeshCollider = true;
    [SerializeField] bool addGpuPaintable = true;
    [SerializeField] bool disableSourceRenderer = true;
    [SerializeField] bool saveMeshAssets = true;

    struct SampledGridVertex
    {
        public Vector3 localPosition;
        public Vector3 localNormal;
        public Vector2 uv;
        public bool valid;
    }

    struct MeshSampleContext
    {
        public Vector3[] localVertices;
        public Vector3[] localNormals;
        public Vector2[] uvs;
        public int[] triangles;
        public bool hasNormals;
        public bool hasUvs;
        public GridPlane gridPlane;
        public Transform sourceTransform;
        public bool useWorldPlaneCoords;
        public float maxHeight;
    }

    struct ClipVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;
        public float planeA;
        public float planeB;
    }

    struct GridPlane
    {
        public int heightAxis;
        public int axisA;
        public int axisB;

        public static GridPlane FromBounds(Bounds bounds)
        {
            Vector3 size = bounds.size;
            if (size.y <= size.x && size.y <= size.z)
                return new GridPlane { heightAxis = 1, axisA = 0, axisB = 2 };

            if (size.x <= size.y && size.x <= size.z)
                return new GridPlane { heightAxis = 0, axisA = 1, axisB = 2 };

            return new GridPlane { heightAxis = 2, axisA = 0, axisB = 1 };
        }

        public string HeightAxisName => heightAxis switch { 0 => "X", 1 => "Y", 2 => "Z", _ => "?" };
    }

    struct GridLayout
    {
        public float gridMinA;
        public float gridMinB;
        public float cellWidth;
        public float cellHeight;
        public int columns;
        public int rows;
        public bool useWorldPlaneCoords;
    }

    struct GridCell
    {
        public float minA;
        public float maxA;
        public float minB;
        public float maxB;
        public bool maxAInclusive;
        public bool maxBInclusive;
    }

    sealed class TileMeshBuilder
    {
        readonly List<Vector3> positions = new();
        readonly List<Vector3> normals = new();
        readonly List<Vector2> uvs = new();
        readonly List<int> triangles = new();
        readonly bool hasNormals;
        readonly bool hasUvs;

        public TileMeshBuilder(bool hasNormals, bool hasUvs)
        {
            this.hasNormals = hasNormals;
            this.hasUvs = hasUvs;
        }

        public bool HasGeometry => triangles.Count > 0;

        public void AddPolygon(IReadOnlyList<ClipVertex> polygon)
        {
            if (polygon.Count < 3)
                return;

            int baseIndex = positions.Count;
            for (int vertexIndex = 0; vertexIndex < polygon.Count; vertexIndex++)
            {
                ClipVertex vertex = polygon[vertexIndex];
                positions.Add(vertex.position);
                if (hasNormals)
                    normals.Add(vertex.normal);
                if (hasUvs)
                    uvs.Add(vertex.uv);
            }

            for (int vertexIndex = 1; vertexIndex < polygon.Count - 1; vertexIndex++)
            {
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + vertexIndex);
                triangles.Add(baseIndex + vertexIndex + 1);
            }
        }

        public Mesh BuildMesh(string meshName)
        {
            Mesh pieceMesh = new Mesh { name = meshName, indexFormat = positions.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            pieceMesh.SetVertices(positions);
            pieceMesh.SetTriangles(triangles, 0);

            if (hasNormals)
                pieceMesh.SetNormals(normals);
            else
                pieceMesh.RecalculateNormals();

            if (hasUvs)
                pieceMesh.SetUVs(0, uvs);

            pieceMesh.RecalculateBounds();
            return pieceMesh;
        }
    }

    [MenuItem("City Cleaner/Mesh/Split Into Grid")]
    static void OpenWindow()
    {
        SplitMeshGridEditor window = GetWindow<SplitMeshGridEditor>("Split Mesh Grid");
        window.minSize = new Vector2(340f, 340f);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Builds grid tiles from the source mesh. Resample creates clean rectangular tiles; Clip cuts source triangles exactly.",
            EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(6f);

        meshBuildApproach = (MeshBuildApproach)EditorGUILayout.EnumPopup("Build Approach", meshBuildApproach);
        splitMode = (GridSplitMode)EditorGUILayout.EnumPopup("Grid Layout", splitMode);

        if (splitMode == GridSplitMode.CellCount)
        {
            columns = EditorGUILayout.IntField("Columns", columns);
            rows = EditorGUILayout.IntField("Rows", rows);
        }
        else
        {
            tileWidthMeters = EditorGUILayout.FloatField("Tile Width (m)", tileWidthMeters);
            tileHeightMeters = EditorGUILayout.FloatField("Tile Height (m)", tileHeightMeters);
            gridOriginMode = (GridOriginMode)EditorGUILayout.EnumPopup("Grid Origin", gridOriginMode);

            if (gridOriginMode == GridOriginMode.CustomWorld)
                customGridOriginWorld = EditorGUILayout.Vector2Field("Origin On Plane", customGridOriginWorld);
        }

        if (meshBuildApproach == MeshBuildApproach.ResampleGrid)
        {
            resampleDensityMode = (ResampleDensityMode)EditorGUILayout.EnumPopup("Density Mode", resampleDensityMode);

            if (resampleDensityMode == ResampleDensityMode.SubdivisionsPerTile)
                subdivisionsPerTile = EditorGUILayout.IntField("Subdivisions Per Tile", subdivisionsPerTile);
            else
                vertexSpacingMeters = EditorGUILayout.FloatField("Vertex Spacing (m)", vertexSpacingMeters);
        }

        addMeshCollider = EditorGUILayout.Toggle("Add Mesh Collider", addMeshCollider);
        addGpuPaintable = EditorGUILayout.Toggle("Add GPUPaintableObject", addGpuPaintable);
        disableSourceRenderer = EditorGUILayout.Toggle("Disable Source Renderer", disableSourceRenderer);
        saveMeshAssets = EditorGUILayout.Toggle("Save Mesh Assets", saveMeshAssets);

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Split Selected Mesh"))
            SplitSelectedMesh();
    }

    void SplitSelectedMesh()
    {
        if (splitMode == GridSplitMode.CellCount && (columns < 1 || rows < 1))
        {
            EditorUtility.DisplayDialog("Split Mesh Grid", "Columns and rows must be at least 1.", "OK");
            return;
        }

        if (splitMode == GridSplitMode.FixedTileSize && (tileWidthMeters < MinTileSizeMeters || tileHeightMeters < MinTileSizeMeters))
        {
            EditorUtility.DisplayDialog("Split Mesh Grid", $"Tile size must be at least {MinTileSizeMeters} m.", "OK");
            return;
        }

        if (meshBuildApproach == MeshBuildApproach.ResampleGrid)
        {
            if (resampleDensityMode == ResampleDensityMode.SubdivisionsPerTile && subdivisionsPerTile < 1)
            {
                EditorUtility.DisplayDialog("Split Mesh Grid", "Subdivisions per tile must be at least 1.", "OK");
                return;
            }

            if (resampleDensityMode == ResampleDensityMode.VertexSpacing && vertexSpacingMeters < MinTileSizeMeters)
            {
                EditorUtility.DisplayDialog("Split Mesh Grid", $"Vertex spacing must be at least {MinTileSizeMeters} m.", "OK");
                return;
            }
        }

        GameObject sourceObject = Selection.activeGameObject;
        if (sourceObject == null)
        {
            EditorUtility.DisplayDialog("Split Mesh Grid", "Select a GameObject with a MeshFilter and MeshRenderer.", "OK");
            return;
        }

        MeshFilter meshFilter = sourceObject.GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = sourceObject.GetComponent<MeshRenderer>();
        if (meshFilter == null || meshRenderer == null)
        {
            EditorUtility.DisplayDialog("Split Mesh Grid", "Selected object needs a MeshFilter and MeshRenderer.", "OK");
            return;
        }

        Mesh sourceMesh = meshFilter.sharedMesh;
        if (sourceMesh == null)
        {
            EditorUtility.DisplayDialog("Split Mesh Grid", "Selected MeshFilter has no mesh assigned.", "OK");
            return;
        }

        if (!sourceMesh.isReadable)
        {
            EditorUtility.DisplayDialog(
                "Split Mesh Grid",
                "Mesh is not readable. Enable Read/Write in the mesh import settings, then try again.",
                "OK");
            return;
        }

        string meshAssetFolder = null;
        if (saveMeshAssets)
        {
            string selectedPath = AssetDatabase.GetAssetPath(sourceMesh);
            string defaultFolder = string.IsNullOrEmpty(selectedPath)
                ? "Assets"
                : Path.GetDirectoryName(selectedPath).Replace('\\', '/');

            meshAssetFolder = EditorUtility.SaveFolderPanel("Save Grid Meshes", defaultFolder, sourceObject.name + "_Grid");
            if (string.IsNullOrEmpty(meshAssetFolder))
                return;

            if (!meshAssetFolder.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/')))
            {
                EditorUtility.DisplayDialog("Split Mesh Grid", "Choose a folder inside the project's Assets folder.", "OK");
                return;
            }

            meshAssetFolder = "Assets" + meshAssetFolder.Replace('\\', '/').Substring(Application.dataPath.Length);
        }

        Vector3[] vertices = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;
        Vector2[] uvs = sourceMesh.uv;
        int[] triangles = sourceMesh.triangles;
        bool hasNormals = normals != null && normals.Length == vertices.Length;
        bool hasUvs = uvs != null && uvs.Length == vertices.Length;

        Transform sourceTransform = sourceObject.transform;
        bool useWorldPlaneCoords = splitMode == GridSplitMode.FixedTileSize;
        GridPlane gridPlane = ResolveGridPlane(vertices, sourceTransform, useWorldPlaneCoords);

        if (!TryResolveGridLayout(
                vertices,
                sourceTransform,
                gridPlane,
                out GridLayout gridLayout))
        {
            EditorUtility.DisplayDialog("Split Mesh Grid", "Mesh is too flat to split on its surface plane.", "OK");
            return;
        }

        Material sharedMaterial = meshRenderer.sharedMaterial;

        Undo.SetCurrentGroupName("Split Mesh Grid");
        int undoGroup = Undo.GetCurrentGroup();

        GameObject tilesRoot = new GameObject(sourceObject.name + "_Tiles");
        Undo.RegisterCreatedObjectUndo(tilesRoot, "Create Grid Tiles Root");
        tilesRoot.transform.SetParent(sourceTransform, worldPositionStays: false);
        tilesRoot.transform.localPosition = Vector3.zero;
        tilesRoot.transform.localRotation = Quaternion.identity;
        tilesRoot.transform.localScale = Vector3.one;

        int createdTileCount = 0;

        if (meshBuildApproach == MeshBuildApproach.ResampleGrid)
        {
            MeshSampleContext sampleContext = CreateSampleContext(
                vertices,
                normals,
                uvs,
                triangles,
                hasNormals,
                hasUvs,
                gridPlane,
                sourceTransform,
                gridLayout.useWorldPlaneCoords);

            for (int row = 0; row < gridLayout.rows; row++)
            {
                for (int column = 0; column < gridLayout.columns; column++)
                {
                    int subdivisions = ResolveSubdivisionsPerTile(gridLayout.cellWidth, gridLayout.cellHeight);
                    if (!TryBuildResampledTileMesh(
                            sourceObject.name,
                            gridLayout,
                            column,
                            row,
                            subdivisions,
                            sampleContext,
                            out Mesh pieceMesh))
                        continue;

                    createdTileCount += CreateTileObject(
                        sourceObject.name,
                        tilesRoot.transform,
                        column,
                        row,
                        pieceMesh,
                        sharedMaterial,
                        meshAssetFolder,
                        saveMeshAssets);
                }
            }
        }
        else
        {
            TileMeshBuilder[,] tileBuilders = PartitionTrianglesIntoGrid(
                vertices,
                normals,
                uvs,
                triangles,
                hasNormals,
                hasUvs,
                gridPlane,
                sourceTransform,
                gridLayout);

            for (int row = 0; row < gridLayout.rows; row++)
            {
                for (int column = 0; column < gridLayout.columns; column++)
                {
                    TileMeshBuilder tileBuilder = tileBuilders[column, row];
                    if (!tileBuilder.HasGeometry)
                        continue;

                    string tileName = $"{sourceObject.name}_Tile_{column}_{row}";
                    Mesh pieceMesh = tileBuilder.BuildMesh(tileName);

                    createdTileCount += CreateTileObject(
                        sourceObject.name,
                        tilesRoot.transform,
                        column,
                        row,
                        pieceMesh,
                        sharedMaterial,
                        meshAssetFolder,
                        saveMeshAssets);
                }
            }
        }

        if (createdTileCount == 0)
        {
            Undo.DestroyObjectImmediate(tilesRoot);
            EditorUtility.DisplayDialog(
                "Split Mesh Grid",
                $"No geometry landed in any grid cell. Height axis: {gridPlane.HeightAxisName}.",
                "OK");
            return;
        }

        if (disableSourceRenderer)
        {
            Undo.RecordObject(meshRenderer, "Disable Source Renderer");
            meshRenderer.enabled = false;
        }

        if (saveMeshAssets)
            AssetDatabase.SaveAssets();

        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = tilesRoot;
        int emptyCellCount = gridLayout.columns * gridLayout.rows - createdTileCount;
        string emptyMessage = emptyCellCount > 0 ? $" ({emptyCellCount} empty cell(s) skipped)" : string.Empty;
        string sizeMessage = splitMode == GridSplitMode.FixedTileSize
            ? $" {gridLayout.cellWidth:0.###}m x {gridLayout.cellHeight:0.###}m grid."
            : string.Empty;
        string approachMessage = meshBuildApproach == MeshBuildApproach.ResampleGrid
            ? $" Resampled at {ResolveSubdivisionsPerTile(gridLayout.cellWidth, gridLayout.cellHeight)} subdivisions/tile."
            : string.Empty;
        EditorUtility.DisplayDialog(
            "Split Mesh Grid",
            $"Created {createdTileCount} tile(s) under {tilesRoot.name}.{sizeMessage}{approachMessage}{emptyMessage}",
            "OK");
    }

    int CreateTileObject(
        string sourceObjectName,
        Transform tilesRoot,
        int column,
        int row,
        Mesh pieceMesh,
        Material sharedMaterial,
        string meshAssetFolder,
        bool saveAssets)
    {
        string tileName = $"{sourceObjectName}_Tile_{column}_{row}";

        if (saveAssets && !string.IsNullOrEmpty(meshAssetFolder))
        {
            string assetPath = $"{meshAssetFolder}/{tileName}.asset";
            AssetDatabase.CreateAsset(pieceMesh, assetPath);
        }

        GameObject tileObject = new GameObject($"Tile_{column}_{row}");
        Undo.RegisterCreatedObjectUndo(tileObject, "Create Grid Tile");
        tileObject.transform.SetParent(tilesRoot, worldPositionStays: false);
        tileObject.transform.localPosition = Vector3.zero;
        tileObject.transform.localRotation = Quaternion.identity;
        tileObject.transform.localScale = Vector3.one;

        MeshFilter tileFilter = tileObject.AddComponent<MeshFilter>();
        tileFilter.sharedMesh = pieceMesh;

        MeshRenderer tileRenderer = tileObject.AddComponent<MeshRenderer>();
        tileRenderer.sharedMaterial = sharedMaterial;

        if (addMeshCollider)
            tileObject.AddComponent<MeshCollider>().sharedMesh = pieceMesh;

        if (addGpuPaintable)
            tileObject.AddComponent<GPUPaintableObject>();

        return 1;
    }

    int ResolveSubdivisionsPerTile(float cellWidth, float cellHeight)
    {
        if (resampleDensityMode == ResampleDensityMode.SubdivisionsPerTile)
            return subdivisionsPerTile;

        int subdivisionsA = Mathf.Max(Mathf.RoundToInt(cellWidth / vertexSpacingMeters), 1);
        int subdivisionsB = Mathf.Max(Mathf.RoundToInt(cellHeight / vertexSpacingMeters), 1);
        return Mathf.Max(subdivisionsA, subdivisionsB);
    }

    static MeshSampleContext CreateSampleContext(
        Vector3[] localVertices,
        Vector3[] localNormals,
        Vector2[] uvs,
        int[] triangles,
        bool hasNormals,
        bool hasUvs,
        GridPlane gridPlane,
        Transform sourceTransform,
        bool useWorldPlaneCoords)
    {
        float maxHeight = float.MinValue;
        for (int vertexIndex = 0; vertexIndex < localVertices.Length; vertexIndex++)
        {
            Vector3 samplePosition = useWorldPlaneCoords
                ? sourceTransform.TransformPoint(localVertices[vertexIndex])
                : localVertices[vertexIndex];
            maxHeight = Mathf.Max(maxHeight, GetAxis(samplePosition, gridPlane.heightAxis));
        }

        return new MeshSampleContext
        {
            localVertices = localVertices,
            localNormals = localNormals,
            uvs = uvs,
            triangles = triangles,
            hasNormals = hasNormals,
            hasUvs = hasUvs,
            gridPlane = gridPlane,
            sourceTransform = sourceTransform,
            useWorldPlaneCoords = useWorldPlaneCoords,
            maxHeight = maxHeight
        };
    }

    static bool TryBuildResampledTileMesh(
        string sourceObjectName,
        GridLayout gridLayout,
        int column,
        int row,
        int subdivisions,
        MeshSampleContext sampleContext,
        out Mesh pieceMesh)
    {
        pieceMesh = null;
        float cellMinA = gridLayout.gridMinA + column * gridLayout.cellWidth;
        float cellMinB = gridLayout.gridMinB + row * gridLayout.cellHeight;
        int vertexColumns = subdivisions + 1;
        int vertexRows = subdivisions + 1;
        SampledGridVertex[] gridVertices = new SampledGridVertex[vertexColumns * vertexRows];
        int validVertexCount = 0;

        for (int gridRow = 0; gridRow < vertexRows; gridRow++)
        {
            float tB = gridRow / (float)subdivisions;
            float planeB = Mathf.Lerp(cellMinB, cellMinB + gridLayout.cellHeight, tB);

            for (int gridColumn = 0; gridColumn < vertexColumns; gridColumn++)
            {
                float tA = gridColumn / (float)subdivisions;
                float planeA = Mathf.Lerp(cellMinA, cellMinA + gridLayout.cellWidth, tA);
                int vertexIndex = gridRow * vertexColumns + gridColumn;

                if (TrySampleMeshAtPlanePoint(planeA, planeB, sampleContext, out SampledGridVertex sampledVertex))
                {
                    gridVertices[vertexIndex] = sampledVertex;
                    validVertexCount++;
                }
            }
        }

        if (validVertexCount == 0)
            return false;

        List<Vector3> positions = new List<Vector3>();
        List<Vector3> meshNormals = sampleContext.hasNormals ? new List<Vector3>() : null;
        List<Vector2> meshUvs = sampleContext.hasUvs ? new List<Vector2>() : null;
        List<int> meshTriangles = new List<int>();
        int[] vertexRemap = new int[gridVertices.Length];

        for (int gridRow = 0; gridRow < subdivisions; gridRow++)
        {
            for (int gridColumn = 0; gridColumn < subdivisions; gridColumn++)
            {
                int corner00 = gridRow * vertexColumns + gridColumn;
                int corner10 = corner00 + 1;
                int corner01 = corner00 + vertexColumns;
                int corner11 = corner01 + 1;

                SampledGridVertex v00 = gridVertices[corner00];
                SampledGridVertex v10 = gridVertices[corner10];
                SampledGridVertex v01 = gridVertices[corner01];
                SampledGridVertex v11 = gridVertices[corner11];

                if (!v00.valid || !v10.valid || !v01.valid || !v11.valid)
                    continue;

                int index00 = AddResampledVertex(v00, positions, meshNormals, meshUvs, vertexRemap, corner00);
                int index10 = AddResampledVertex(v10, positions, meshNormals, meshUvs, vertexRemap, corner10);
                int index11 = AddResampledVertex(v11, positions, meshNormals, meshUvs, vertexRemap, corner11);
                int index01 = AddResampledVertex(v01, positions, meshNormals, meshUvs, vertexRemap, corner01);

                meshTriangles.Add(index00);
                meshTriangles.Add(index01);
                meshTriangles.Add(index10);
                meshTriangles.Add(index10);
                meshTriangles.Add(index01);
                meshTriangles.Add(index11);
            }
        }

        if (meshTriangles.Count == 0)
            return false;

        pieceMesh = new Mesh
        {
            name = $"{sourceObjectName}_Tile_{column}_{row}",
            indexFormat = positions.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
        };
        pieceMesh.SetVertices(positions);
        pieceMesh.SetTriangles(meshTriangles, 0);

        if (meshNormals != null)
            pieceMesh.SetNormals(meshNormals);
        else
            pieceMesh.RecalculateNormals();

        if (meshUvs != null)
            pieceMesh.SetUVs(0, meshUvs);

        pieceMesh.RecalculateBounds();
        return true;
    }

    static int AddResampledVertex(
        SampledGridVertex vertex,
        List<Vector3> positions,
        List<Vector3> meshNormals,
        List<Vector2> meshUvs,
        int[] vertexRemap,
        int gridVertexIndex)
    {
        if (vertexRemap[gridVertexIndex] > 0)
            return vertexRemap[gridVertexIndex] - 1;

        int meshVertexIndex = positions.Count;
        positions.Add(vertex.localPosition);
        if (meshNormals != null)
            meshNormals.Add(vertex.localNormal);
        if (meshUvs != null)
            meshUvs.Add(vertex.uv);

        vertexRemap[gridVertexIndex] = meshVertexIndex + 1;
        return meshVertexIndex;
    }

    static bool TrySampleMeshAtPlanePoint(float planeA, float planeB, MeshSampleContext sampleContext, out SampledGridVertex sampledVertex)
    {
        if (TrySampleMeshOnPlane(planeA, planeB, sampleContext, out sampledVertex))
            return true;

        return TrySampleMeshByRay(planeA, planeB, sampleContext, out sampledVertex);
    }

    static bool TrySampleMeshOnPlane(float planeA, float planeB, MeshSampleContext sampleContext, out SampledGridVertex sampledVertex)
    {
        sampledVertex = default;
        bool found = false;
        float bestHeight = float.MinValue;
        Vector2 point = new Vector2(planeA, planeB);
        MeshSampleContext context = sampleContext;

        for (int triangleIndex = 0; triangleIndex < context.triangles.Length; triangleIndex += 3)
        {
            int index0 = context.triangles[triangleIndex];
            int index1 = context.triangles[triangleIndex + 1];
            int index2 = context.triangles[triangleIndex + 2];

            Vector3 local0 = context.localVertices[index0];
            Vector3 local1 = context.localVertices[index1];
            Vector3 local2 = context.localVertices[index2];
            Vector3 sample0 = ToSampleSpace(local0, context);
            Vector3 sample1 = ToSampleSpace(local1, context);
            Vector3 sample2 = ToSampleSpace(local2, context);

            if (!TryGetBarycentric2D(point, sample0, sample1, sample2, context.gridPlane, out Vector3 barycentric))
                continue;

            Vector3 samplePosition = sample0 * barycentric.x + sample1 * barycentric.y + sample2 * barycentric.z;
            float height = GetAxis(samplePosition, context.gridPlane.heightAxis);
            if (found && height <= bestHeight)
                continue;

            bestHeight = height;
            found = true;
            sampledVertex.localPosition = local0 * barycentric.x + local1 * barycentric.y + local2 * barycentric.z;
            sampledVertex.valid = true;

            if (context.hasNormals)
            {
                Vector3 normal0 = context.localNormals[index0];
                Vector3 normal1 = context.localNormals[index1];
                Vector3 normal2 = context.localNormals[index2];
                sampledVertex.localNormal = (normal0 * barycentric.x + normal1 * barycentric.y + normal2 * barycentric.z).normalized;
            }

            if (context.hasUvs)
            {
                Vector2 uv0 = context.uvs[index0];
                Vector2 uv1 = context.uvs[index1];
                Vector2 uv2 = context.uvs[index2];
                sampledVertex.uv = uv0 * barycentric.x + uv1 * barycentric.y + uv2 * barycentric.z;
            }
        }

        return found;
    }

    static bool TrySampleMeshByRay(float planeA, float planeB, MeshSampleContext sampleContext, out SampledGridVertex sampledVertex)
    {
        sampledVertex = default;
        MeshSampleContext context = sampleContext;

        Vector3 rayOrigin = Vector3.zero;
        SetAxis(ref rayOrigin, context.gridPlane.axisA, planeA);
        SetAxis(ref rayOrigin, context.gridPlane.axisB, planeB);
        SetAxis(ref rayOrigin, context.gridPlane.heightAxis, context.maxHeight + 1f);

        Vector3 rayDirection = Vector3.zero;
        SetAxis(ref rayDirection, context.gridPlane.heightAxis, -1f);

        bool found = false;
        float closestDistance = float.MaxValue;

        for (int triangleIndex = 0; triangleIndex < context.triangles.Length; triangleIndex += 3)
        {
            int index0 = context.triangles[triangleIndex];
            int index1 = context.triangles[triangleIndex + 1];
            int index2 = context.triangles[triangleIndex + 2];

            Vector3 local0 = context.localVertices[index0];
            Vector3 local1 = context.localVertices[index1];
            Vector3 local2 = context.localVertices[index2];
            Vector3 v0 = ToSampleSpace(local0, context);
            Vector3 v1 = ToSampleSpace(local1, context);
            Vector3 v2 = ToSampleSpace(local2, context);

            if (!RayIntersectsTriangle(rayOrigin, rayDirection, v0, v1, v2, out float distance, out Vector3 barycentric))
                continue;

            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            found = true;
            sampledVertex.localPosition = local0 * barycentric.x + local1 * barycentric.y + local2 * barycentric.z;
            sampledVertex.valid = true;

            if (context.hasNormals)
            {
                Vector3 normal0 = context.localNormals[index0];
                Vector3 normal1 = context.localNormals[index1];
                Vector3 normal2 = context.localNormals[index2];
                sampledVertex.localNormal = (normal0 * barycentric.x + normal1 * barycentric.y + normal2 * barycentric.z).normalized;
            }

            if (context.hasUvs)
            {
                Vector2 uv0 = context.uvs[index0];
                Vector2 uv1 = context.uvs[index1];
                Vector2 uv2 = context.uvs[index2];
                sampledVertex.uv = uv0 * barycentric.x + uv1 * barycentric.y + uv2 * barycentric.z;
            }
        }

        return found;
    }

    static Vector3 ToSampleSpace(Vector3 localPosition, MeshSampleContext sampleContext)
    {
        if (!sampleContext.useWorldPlaneCoords)
            return localPosition;

        return sampleContext.sourceTransform.TransformPoint(localPosition);
    }

    static bool TryGetBarycentric2D(Vector2 point, Vector3 v0, Vector3 v1, Vector3 v2, GridPlane gridPlane, out Vector3 barycentric)
    {
        Vector2 a = new Vector2(GetAxis(v0, gridPlane.axisA), GetAxis(v0, gridPlane.axisB));
        Vector2 b = new Vector2(GetAxis(v1, gridPlane.axisA), GetAxis(v1, gridPlane.axisB));
        Vector2 c = new Vector2(GetAxis(v2, gridPlane.axisA), GetAxis(v2, gridPlane.axisB));

        float denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Mathf.Abs(denominator) <= 0.000001f)
        {
            barycentric = default;
            return false;
        }

        float weight0 = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / denominator;
        float weight1 = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / denominator;
        float weight2 = 1f - weight0 - weight1;

        const float epsilon = -0.02f;
        if (weight0 < epsilon || weight1 < epsilon || weight2 < epsilon)
        {
            barycentric = default;
            return false;
        }

        barycentric = new Vector3(
            Mathf.Clamp01(weight0),
            Mathf.Clamp01(weight1),
            Mathf.Clamp01(weight2));
        return true;
    }

    static bool RayIntersectsTriangle(Vector3 rayOrigin, Vector3 rayDirection, Vector3 v0, Vector3 v1, Vector3 v2, out float distance, out Vector3 barycentric)
    {
        distance = 0f;
        barycentric = default;

        Vector3 edge1 = v1 - v0;
        Vector3 edge2 = v2 - v0;
        Vector3 pVector = Vector3.Cross(rayDirection, edge2);
        float determinant = Vector3.Dot(edge1, pVector);
        if (Mathf.Abs(determinant) <= 0.000001f)
            return false;

        float inverseDeterminant = 1f / determinant;
        Vector3 tVector = rayOrigin - v0;
        float u = Vector3.Dot(tVector, pVector) * inverseDeterminant;
        if (u < -0.02f || u > 1.02f)
            return false;

        Vector3 qVector = Vector3.Cross(tVector, edge1);
        float v = Vector3.Dot(rayDirection, qVector) * inverseDeterminant;
        if (v < -0.02f || u + v > 1.02f)
            return false;

        float hitDistance = Vector3.Dot(edge2, qVector) * inverseDeterminant;
        if (hitDistance <= 0f)
            return false;

        distance = hitDistance;
        barycentric = new Vector3(1f - u - v, u, v);
        return true;
    }

    static void SetAxis(ref Vector3 value, int axis, float axisValue)
    {
        switch (axis)
        {
            case 0: value.x = axisValue; break;
            case 1: value.y = axisValue; break;
            case 2: value.z = axisValue; break;
        }
    }

    static GridPlane ResolveGridPlane(Vector3[] vertices, Transform sourceTransform, bool useWorldSpace)
    {
        if (!useWorldSpace)
            return GridPlane.FromBounds(GetLocalBounds(vertices));

        return GridPlane.FromBounds(GetWorldBounds(vertices, sourceTransform));
    }

    bool TryResolveGridLayout(Vector3[] vertices, Transform sourceTransform, GridPlane gridPlane, out GridLayout gridLayout)
    {
        gridLayout = default;

        if (splitMode == GridSplitMode.CellCount)
        {
            GetPlaneExtents(vertices, sourceTransform, gridPlane, useWorldPlaneCoords: false, out float minA, out float maxA, out float minB, out float maxB);

            if (maxA - minA <= 0.0001f || maxB - minB <= 0.0001f)
                return false;

            gridLayout.gridMinA = minA;
            gridLayout.gridMinB = minB;
            gridLayout.cellWidth = (maxA - minA) / columns;
            gridLayout.cellHeight = (maxB - minB) / rows;
            gridLayout.columns = columns;
            gridLayout.rows = rows;
            gridLayout.useWorldPlaneCoords = false;
            return true;
        }

        return TryResolveFixedTileLayout(vertices, sourceTransform, gridPlane, out gridLayout);
    }

    bool TryResolveFixedTileLayout(Vector3[] vertices, Transform sourceTransform, GridPlane gridPlane, out GridLayout gridLayout)
    {
        gridLayout = default;
        GetPlaneExtents(vertices, sourceTransform, gridPlane, useWorldPlaneCoords: true, out float meshMinA, out float meshMaxA, out float meshMinB, out float meshMaxB);

        if (meshMaxA - meshMinA <= 0.0001f || meshMaxB - meshMinB <= 0.0001f)
            return false;

        float gridMinA;
        float gridMinB;

        switch (gridOriginMode)
        {
            case GridOriginMode.MeshMin:
                gridMinA = meshMinA;
                gridMinB = meshMinB;
                break;

            case GridOriginMode.WorldZero:
                gridMinA = Mathf.Floor(meshMinA / tileWidthMeters) * tileWidthMeters;
                gridMinB = Mathf.Floor(meshMinB / tileHeightMeters) * tileHeightMeters;
                break;

            case GridOriginMode.CustomWorld:
                gridMinA = customGridOriginWorld.x;
                gridMinB = customGridOriginWorld.y;
                break;

            default:
                gridMinA = meshMinA;
                gridMinB = meshMinB;
                break;
        }

        if (gridOriginMode != GridOriginMode.MeshMin)
        {
            if (gridMinA > meshMinA)
                gridMinA -= tileWidthMeters;
            if (gridMinB > meshMinB)
                gridMinB -= tileHeightMeters;
        }

        float gridMaxA = gridMinA + Mathf.Ceil((meshMaxA - gridMinA) / tileWidthMeters) * tileWidthMeters;
        float gridMaxB = gridMinB + Mathf.Ceil((meshMaxB - gridMinB) / tileHeightMeters) * tileHeightMeters;

        int resolvedColumns = Mathf.Max(Mathf.RoundToInt((gridMaxA - gridMinA) / tileWidthMeters), 1);
        int resolvedRows = Mathf.Max(Mathf.RoundToInt((gridMaxB - gridMinB) / tileHeightMeters), 1);

        gridLayout.gridMinA = gridMinA;
        gridLayout.gridMinB = gridMinB;
        gridLayout.cellWidth = tileWidthMeters;
        gridLayout.cellHeight = tileHeightMeters;
        gridLayout.columns = resolvedColumns;
        gridLayout.rows = resolvedRows;
        gridLayout.useWorldPlaneCoords = true;
        return true;
    }

    static TileMeshBuilder[,] PartitionTrianglesIntoGrid(
        Vector3[] vertices,
        Vector3[] normals,
        Vector2[] uvs,
        int[] triangles,
        bool hasNormals,
        bool hasUvs,
        GridPlane gridPlane,
        Transform sourceTransform,
        GridLayout gridLayout)
    {
        TileMeshBuilder[,] tileBuilders = new TileMeshBuilder[gridLayout.columns, gridLayout.rows];
        for (int row = 0; row < gridLayout.rows; row++)
        {
            for (int column = 0; column < gridLayout.columns; column++)
                tileBuilders[column, row] = new TileMeshBuilder(hasNormals, hasUvs);
        }

        List<ClipVertex> clippedPolygon = new();
        ClipVertex[] triangleVertices = new ClipVertex[3];

        for (int triangleIndex = 0; triangleIndex < triangles.Length; triangleIndex += 3)
        {
            int index0 = triangles[triangleIndex];
            int index1 = triangles[triangleIndex + 1];
            int index2 = triangles[triangleIndex + 2];

            triangleVertices[0] = CreateClipVertex(index0, vertices, normals, uvs, hasNormals, hasUvs, gridPlane, sourceTransform, gridLayout.useWorldPlaneCoords);
            triangleVertices[1] = CreateClipVertex(index1, vertices, normals, uvs, hasNormals, hasUvs, gridPlane, sourceTransform, gridLayout.useWorldPlaneCoords);
            triangleVertices[2] = CreateClipVertex(index2, vertices, normals, uvs, hasNormals, hasUvs, gridPlane, sourceTransform, gridLayout.useWorldPlaneCoords);

            GetTriangleCellRange(
                triangleVertices,
                gridLayout.gridMinA,
                gridLayout.gridMinB,
                gridLayout.cellWidth,
                gridLayout.cellHeight,
                gridLayout.columns,
                gridLayout.rows,
                out int minColumn,
                out int maxColumn,
                out int minRow,
                out int maxRow);

            for (int row = minRow; row <= maxRow; row++)
            {
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    GridCell cell = new GridCell
                    {
                        minA = gridLayout.gridMinA + column * gridLayout.cellWidth,
                        maxA = gridLayout.gridMinA + (column + 1) * gridLayout.cellWidth,
                        minB = gridLayout.gridMinB + row * gridLayout.cellHeight,
                        maxB = gridLayout.gridMinB + (row + 1) * gridLayout.cellHeight,
                        maxAInclusive = column == gridLayout.columns - 1,
                        maxBInclusive = row == gridLayout.rows - 1
                    };

                    if (!ClipTriangleToCell(triangleVertices, cell, clippedPolygon))
                        continue;

                    tileBuilders[column, row].AddPolygon(clippedPolygon);
                }
            }
        }

        return tileBuilders;
    }

    static ClipVertex CreateClipVertex(
        int vertexIndex,
        Vector3[] vertices,
        Vector3[] normals,
        Vector2[] uvs,
        bool hasNormals,
        bool hasUvs,
        GridPlane gridPlane,
        Transform sourceTransform,
        bool useWorldPlaneCoords)
    {
        Vector3 localPosition = vertices[vertexIndex];
        Vector3 planePosition = useWorldPlaneCoords ? sourceTransform.TransformPoint(localPosition) : localPosition;

        ClipVertex vertex = new ClipVertex
        {
            position = localPosition,
            planeA = GetAxis(planePosition, gridPlane.axisA),
            planeB = GetAxis(planePosition, gridPlane.axisB),
            normal = Vector3.up,
            uv = Vector2.zero
        };

        if (hasNormals)
            vertex.normal = normals[vertexIndex];
        if (hasUvs)
            vertex.uv = uvs[vertexIndex];

        return vertex;
    }

    static void GetTriangleCellRange(
        ClipVertex[] triangleVertices,
        float gridMinA,
        float gridMinB,
        float cellWidth,
        float cellHeight,
        int columns,
        int rows,
        out int minColumn,
        out int maxColumn,
        out int minRow,
        out int maxRow)
    {
        float triMinA = triangleVertices[0].planeA;
        float triMaxA = triangleVertices[0].planeA;
        float triMinB = triangleVertices[0].planeB;
        float triMaxB = triangleVertices[0].planeB;

        for (int vertexIndex = 1; vertexIndex < triangleVertices.Length; vertexIndex++)
        {
            triMinA = Mathf.Min(triMinA, triangleVertices[vertexIndex].planeA);
            triMaxA = Mathf.Max(triMaxA, triangleVertices[vertexIndex].planeA);
            triMinB = Mathf.Min(triMinB, triangleVertices[vertexIndex].planeB);
            triMaxB = Mathf.Max(triMaxB, triangleVertices[vertexIndex].planeB);
        }

        minColumn = Mathf.Clamp(Mathf.FloorToInt((triMinA - gridMinA) / cellWidth), 0, columns - 1);
        maxColumn = Mathf.Clamp(Mathf.FloorToInt((triMaxA - gridMinA) / cellWidth), 0, columns - 1);
        minRow = Mathf.Clamp(Mathf.FloorToInt((triMinB - gridMinB) / cellHeight), 0, rows - 1);
        maxRow = Mathf.Clamp(Mathf.FloorToInt((triMaxB - gridMinB) / cellHeight), 0, rows - 1);
    }

    static bool ClipTriangleToCell(ClipVertex[] triangleVertices, GridCell cell, List<ClipVertex> clippedPolygon)
    {
        clippedPolygon.Clear();
        clippedPolygon.Add(triangleVertices[0]);
        clippedPolygon.Add(triangleVertices[1]);
        clippedPolygon.Add(triangleVertices[2]);

        ClipPolygonAgainstMin(ref clippedPolygon, cell.minA, clipPlaneA: true);
        if (clippedPolygon.Count == 0)
            return false;

        ClipPolygonAgainstMax(ref clippedPolygon, cell.maxA, clipPlaneA: true, inclusive: cell.maxAInclusive);
        if (clippedPolygon.Count == 0)
            return false;

        ClipPolygonAgainstMin(ref clippedPolygon, cell.minB, clipPlaneA: false);
        if (clippedPolygon.Count == 0)
            return false;

        ClipPolygonAgainstMax(ref clippedPolygon, cell.maxB, clipPlaneA: false, inclusive: cell.maxBInclusive);
        return clippedPolygon.Count >= 3;
    }

    static void ClipPolygonAgainstMin(ref List<ClipVertex> polygon, float boundary, bool clipPlaneA)
    {
        if (polygon.Count == 0)
            return;

        List<ClipVertex> output = new List<ClipVertex>(polygon.Count + 2);
        ClipVertex previous = polygon[polygon.Count - 1];
        bool previousInside = IsInsideMin(previous, boundary, clipPlaneA);

        for (int vertexIndex = 0; vertexIndex < polygon.Count; vertexIndex++)
        {
            ClipVertex current = polygon[vertexIndex];
            bool currentInside = IsInsideMin(current, boundary, clipPlaneA);

            if (currentInside)
            {
                if (!previousInside)
                    output.Add(InterpolateClipVertex(previous, current, boundary, clipPlaneA));

                output.Add(current);
            }
            else if (previousInside)
            {
                output.Add(InterpolateClipVertex(previous, current, boundary, clipPlaneA));
            }

            previous = current;
            previousInside = currentInside;
        }

        polygon = output;
    }

    static void ClipPolygonAgainstMax(ref List<ClipVertex> polygon, float boundary, bool clipPlaneA, bool inclusive)
    {
        if (polygon.Count == 0)
            return;

        List<ClipVertex> output = new List<ClipVertex>(polygon.Count + 2);
        ClipVertex previous = polygon[polygon.Count - 1];
        bool previousInside = IsInsideMax(previous, boundary, clipPlaneA, inclusive);

        for (int vertexIndex = 0; vertexIndex < polygon.Count; vertexIndex++)
        {
            ClipVertex current = polygon[vertexIndex];
            bool currentInside = IsInsideMax(current, boundary, clipPlaneA, inclusive);

            if (currentInside)
            {
                if (!previousInside && inclusive)
                    output.Add(InterpolateClipVertex(previous, current, boundary, clipPlaneA));

                output.Add(current);
            }
            else if (previousInside)
            {
                if (inclusive)
                    output.Add(InterpolateClipVertex(previous, current, boundary, clipPlaneA));
                else
                    output.Add(SnapInsideExclusiveBoundary(previous, current, boundary, clipPlaneA));
            }

            previous = current;
            previousInside = currentInside;
        }

        polygon = output;
    }

    static ClipVertex SnapInsideExclusiveBoundary(ClipVertex start, ClipVertex end, float boundary, bool clipPlaneA)
    {
        float target = boundary - ClipEpsilon;
        float startValue = clipPlaneA ? start.planeA : start.planeB;
        float endValue = clipPlaneA ? end.planeA : end.planeB;
        float delta = endValue - startValue;
        float t = Mathf.Abs(delta) <= ClipEpsilon ? 0.5f : (target - startValue) / delta;
        t = Mathf.Clamp01(t);

        ClipVertex snapped = new ClipVertex
        {
            position = Vector3.Lerp(start.position, end.position, t),
            normal = Vector3.Lerp(start.normal, end.normal, t).normalized,
            uv = Vector2.Lerp(start.uv, end.uv, t),
            planeA = Mathf.Lerp(start.planeA, end.planeA, t),
            planeB = Mathf.Lerp(start.planeB, end.planeB, t)
        };

        if (clipPlaneA)
            snapped.planeA = target;
        else
            snapped.planeB = target;

        return snapped;
    }

    static bool IsInsideMin(ClipVertex vertex, float boundary, bool clipPlaneA)
    {
        float value = clipPlaneA ? vertex.planeA : vertex.planeB;
        return value >= boundary;
    }

    static bool IsInsideMax(ClipVertex vertex, float boundary, bool clipPlaneA, bool inclusive)
    {
        float value = clipPlaneA ? vertex.planeA : vertex.planeB;
        if (inclusive)
            return value <= boundary + ClipEpsilon;

        return value < boundary;
    }

    static ClipVertex InterpolateClipVertex(ClipVertex start, ClipVertex end, float boundary, bool clipPlaneA)
    {
        float startValue = clipPlaneA ? start.planeA : start.planeB;
        float endValue = clipPlaneA ? end.planeA : end.planeB;
        float delta = endValue - startValue;

        float t = Mathf.Abs(delta) <= ClipEpsilon ? 0.5f : (boundary - startValue) / delta;
        t = Mathf.Clamp01(t);

        ClipVertex interpolated = new ClipVertex
        {
            position = Vector3.Lerp(start.position, end.position, t),
            normal = Vector3.Lerp(start.normal, end.normal, t).normalized,
            uv = Vector2.Lerp(start.uv, end.uv, t),
            planeA = Mathf.Lerp(start.planeA, end.planeA, t),
            planeB = Mathf.Lerp(start.planeB, end.planeB, t)
        };

        if (clipPlaneA)
            interpolated.planeA = boundary;
        else
            interpolated.planeB = boundary;

        return interpolated;
    }

    static void GetPlaneExtents(
        Vector3[] vertices,
        Transform sourceTransform,
        GridPlane gridPlane,
        bool useWorldPlaneCoords,
        out float minA,
        out float maxA,
        out float minB,
        out float maxB)
    {
        minA = maxA = GetPlaneAxis(vertices[0], sourceTransform, gridPlane, useWorldPlaneCoords);
        minB = maxB = GetPlaneAxisB(vertices[0], sourceTransform, gridPlane, useWorldPlaneCoords);

        for (int vertexIndex = 1; vertexIndex < vertices.Length; vertexIndex++)
        {
            float a = GetPlaneAxis(vertices[vertexIndex], sourceTransform, gridPlane, useWorldPlaneCoords);
            float b = GetPlaneAxisB(vertices[vertexIndex], sourceTransform, gridPlane, useWorldPlaneCoords);
            minA = Mathf.Min(minA, a);
            maxA = Mathf.Max(maxA, a);
            minB = Mathf.Min(minB, b);
            maxB = Mathf.Max(maxB, b);
        }
    }

    static float GetPlaneAxis(Vector3 localVertex, Transform sourceTransform, GridPlane gridPlane, bool useWorldPlaneCoords)
    {
        Vector3 position = useWorldPlaneCoords ? sourceTransform.TransformPoint(localVertex) : localVertex;
        return GetAxis(position, gridPlane.axisA);
    }

    static float GetPlaneAxisB(Vector3 localVertex, Transform sourceTransform, GridPlane gridPlane, bool useWorldPlaneCoords)
    {
        Vector3 position = useWorldPlaneCoords ? sourceTransform.TransformPoint(localVertex) : localVertex;
        return GetAxis(position, gridPlane.axisB);
    }

    static Bounds GetLocalBounds(Vector3[] vertices)
    {
        Bounds bounds = new Bounds(vertices[0], Vector3.zero);
        for (int vertexIndex = 1; vertexIndex < vertices.Length; vertexIndex++)
            bounds.Encapsulate(vertices[vertexIndex]);

        return bounds;
    }

    static Bounds GetWorldBounds(Vector3[] vertices, Transform sourceTransform)
    {
        Bounds bounds = new Bounds(sourceTransform.TransformPoint(vertices[0]), Vector3.zero);
        for (int vertexIndex = 1; vertexIndex < vertices.Length; vertexIndex++)
            bounds.Encapsulate(sourceTransform.TransformPoint(vertices[vertexIndex]));

        return bounds;
    }

    static float GetAxis(Vector3 value, int axis)
    {
        return axis switch
        {
            0 => value.x,
            1 => value.y,
            2 => value.z,
            _ => value.y
        };
    }
}
