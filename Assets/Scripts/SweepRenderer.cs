using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sketch {

[ExecuteInEditMode]
public sealed class SweepRenderer : MonoBehaviour
{
    #region Editable properties

    [field:SerializeField]
    public Material Material { get; set; }

    #endregion

    #region MonoBehaviour implementation

    void OnValidate()
       => Mesh?.Clear();

    void OnDestroy()
       => TearDownMesh();

    void Update()
    {
        if (Mesh == null) SetUpMesh();

        using (var varray = CreateVertexArray())
        {
            if (Mesh.vertexCount == 0)
                InitializeMesh(varray);
            else
                UpdateVertexBuffer(varray);
        }

        Mesh.bounds = Builder.BoundingBox;
        Renderer.sharedMaterial = Material;
    }

    #endregion

    #region Helper properties

    T GetLiveComponent<T>() where T : class
    {
        var c = GetComponent<T>();
        return c != null ? c : null;
    }

    Mesh Mesh => _mesh != null ? _mesh : null;
    MeshFilter Filter => GetLiveComponent<MeshFilter>();
    MeshRenderer Renderer => GetLiveComponent<MeshRenderer>();
    IMeshBuilder Builder => GetLiveComponent<IMeshBuilder>();

    #endregion

    #region Mesh object and companion components

    Mesh _mesh;

    void SetUpMesh()
    {
        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.DontSave;

        var mf = gameObject.AddComponent<MeshFilter>();
        mf.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
        mf.sharedMesh = _mesh;

        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
    }

    void TearDownMesh()
    {
        CoreUtils.Destroy(Mesh);
        CoreUtils.Destroy(Filter);
        CoreUtils.Destroy(Renderer);
    }

    #endregion

    #region Mesh builder

    void InitializeMesh(NativeArray<Vertex> varray)
    {
        var attr_p = new VertexAttributeDescriptor
          (VertexAttribute.Position, VertexAttributeFormat.Float32, 3);

        var attr_n = new VertexAttributeDescriptor
          (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        var vcount = Builder.GetTotalVertexCount();
        var icount = Builder.GetTotalIndexCount();

        _mesh.SetVertexBufferParams(vcount, attr_p, attr_n);
        _mesh.SetVertexBufferData(varray, 0, 0, vcount);

        using (var iarray = CreateIndexArray())
        {
            _mesh.SetIndexBufferParams(icount, IndexFormat.UInt32);
            _mesh.SetIndexBufferData(iarray, 0, 0, icount);
        }

        _mesh.SetSubMesh(0, new SubMeshDescriptor(0, icount));
    }

    void UpdateVertexBuffer(NativeArray<Vertex> varray)
      => _mesh.SetVertexBufferData(varray, 0, 0, Builder.GetTotalVertexCount());

    NativeArray<Vertex> CreateVertexArray()
    {
        var array = new NativeArray<Vertex>(
            Builder.GetTotalVertexCount(), Allocator.Temp,
            NativeArrayOptions.UninitializedMemory
        );

        for (var (i, count) = (0, 0); i < Builder.InstanceCount; i++)
        {
            var slice = new NativeSlice<Vertex>(array, count, Builder.VertexPerInstance);
            Builder.WriteVertexArray(i, slice);
            count += Builder.VertexPerInstance;
        }

        return array;
    }

    NativeArray<uint> CreateIndexArray()
    {
        var array = new NativeArray<uint>(
            Builder.GetTotalIndexCount(), Allocator.Temp,
            NativeArrayOptions.UninitializedMemory
        );

        var offs = 0u;
        for (var (i, count) = (0, 0); i < Builder.InstanceCount; i++)
        {
            var slice = new NativeSlice<uint>(array, count, Builder.IndexPerInstance);
            Builder.WriteIndexArray(i, slice, offs);
            offs += (uint)Builder.VertexPerInstance;
            count += Builder.IndexPerInstance;
        }

        return array;
    }

    #endregion
}

/*
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
*/

} // namespace Sketch
