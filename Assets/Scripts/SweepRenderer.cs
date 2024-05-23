using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sketch {

[System.Serializable]
public struct SweepConfig
{
    public float Radius;
    public uint ArcCount;
    public uint Subdivision;
    public uint Seed;

    public uint CalculatedIndexCount
      => ArcCount * Subdivision * 3 * 2;

    public uint CalculatedVertexCount
      => ArcCount * (Subdivision + 1) * 2;

    public static SweepConfig Default()
      => new SweepConfig()
        { Subdivision = 64,
          Seed = 1 };
}

[ExecuteInEditMode, RequireComponent(typeof(MeshRenderer))]
public sealed class SweepRenderer : MonoBehaviour
{
    #region Editable properties

    [field:SerializeField] SweepConfig Config = SweepConfig.Default();

    #endregion

    #region Private objects

    Mesh _mesh;

    #endregion

    #region MonoBehaviour implementation

    void OnValidate()
      => _mesh.Clear();

    void OnDestroy()
    {
        CoreUtils.Destroy(_mesh);
        _mesh = null;
    }

    void Update()
    {
        if (_mesh == null) SetUpMeshFilter();

        using (var varray = CreateVertexArray())
        {
            if (_mesh.subMeshCount == 0)
                InitializeMesh(varray);
            else
                UpdateVertexBuffer(varray);
        }

        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * Config.Radius);
    }

    #endregion

    #region Mesh object handlers

    void SetUpMeshFilter()
    {
        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.DontSave;

        var mf = GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = gameObject.AddComponent<MeshFilter>();
            mf.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
        }

        mf.sharedMesh = _mesh;
    }

    void InitializeMesh(NativeArray<Vertex> varray)
    {
        var attr_p = new VertexAttributeDescriptor
          (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

        var attr_n = new VertexAttributeDescriptor
          (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        var icount = (int)Config.CalculatedIndexCount;
        var vcount = (int)Config.CalculatedVertexCount;

        _mesh.SetVertexBufferParams(vcount, attr_p, attr_n);
        _mesh.SetVertexBufferData(varray, 0, 0, vcount);

        using (var iarray = CreateIndexArray())
        {
            _mesh.SetIndexBufferParams(vcount, IndexFormat.UInt32);
            _mesh.SetIndexBufferData(iarray, 0, 0, icount);
        }

        _mesh.SetSubMesh(0, new SubMeshDescriptor(0, icount));
    }

    void UpdateVertexBuffer(NativeArray<Vertex> varray)
      => _mesh.SetVertexBufferData(varray, 0, 0, varray.Length);

    #endregion

    #region Index array operations

    NativeArray<uint> CreateIndexArray(int length)
    {
        var buffer = new NativeArray<uint>(
            length, Allocator.Temp,
            NativeArrayOptions.UninitializedMemory
        );

        for (var i = 0; i < length; i++) buffer[i] = (uint)i;

        return buffer;
    }

    #endregion

    #region Mesh object operations

    struct Vertex
    {
        public float3 position;
        public float3 normal;
    }

    NativeArray<Vertex> CreateVertexArray()
    {
        return new NativeArray<Vertex>(
            8, Allocator.Temp,
            NativeArrayOptions.UninitializedMemory);
    }

    void UpdateVerticesOnMesh(NativeArray<Vertex> vertexArray)
    {
    }

    #endregion
}

[Unity.Burst.BurstCompile(CompileSynchronously = true)]
struct ArcBuilderJob : IJobParallelFor
{
    [ReadOnly] public SweepConfig Config;

    [WriteOnly] public NativeSlice<uint> ISlice;
    [WriteOnly] public NativeSlice<float3> VSlice;

    public void Execute(int i)
    {
    }
}

} // namespace Sketch
