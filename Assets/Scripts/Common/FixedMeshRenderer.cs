using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Sketch {

[ExecuteInEditMode]
public sealed class FixedMeshRenderer : MonoBehaviour, ITimeControl, IPropertyPreview
{
    #region Editable properties

    [field:SerializeField]
    public Material Material { get; set; }

    [field:SerializeField]
    public float Time { get; set; }

    #endregion

    #region Associated objects

    Mesh _mesh;
    Mesh OwnedMesh => _mesh != null ? _mesh : null;

    MeshFilter FilterComponent => this.GetLiveComponent<MeshFilter>();
    MeshRenderer RendererComponent => this.GetLiveComponent<MeshRenderer>();
    IFixedMeshBuilder BuilderComponent => this.GetLiveComponent<IFixedMeshBuilder>();

    #endregion

    #region ITimeControl / IPropertyPreview implementation

    public void OnControlTimeStart() {}
    public void OnControlTimeStop() {}
    public void SetTime(double time) => Time = (float)time;
    public void GatherProperties(PlayableDirector dir, IPropertyCollector drv)
      => drv.AddFromName<FixedMeshRenderer>(gameObject, "<Time>k__BackingField");

    #endregion

    #region MonoBehaviour implementation

    void OnDestroy()
       => TearDownAssociatedObjects();

    void LateUpdate()
    {
        if (OwnedMesh == null) SetUpAssociatedObjects();

        using (var varray = CreateVertexArray())
        {
            if (OwnedMesh.vertexCount != BuilderComponent.VertexCount)
                ResetMesh(varray);
            else
                UpdateVertexBuffer(varray);
        }

        OwnedMesh.bounds = BuilderComponent.BoundingBox;
        RendererComponent.sharedMaterial = Material;
    }

    #endregion

    #region Associated object handlers

    void SetUpAssociatedObjects()
    {
        _mesh = new Mesh();
        _mesh.hideFlags = HideFlags.DontSave;

        var mf = gameObject.AddComponent<MeshFilter>();
        mf.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
        mf.sharedMesh = _mesh;

        var mr = gameObject.AddComponent<MeshRenderer>();
        mr.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
        mr.rayTracingMode = RayTracingMode.DynamicGeometry;
    }

    void TearDownAssociatedObjects()
    {
        CoreUtils.Destroy(OwnedMesh);
        CoreUtils.Destroy(FilterComponent);
        CoreUtils.Destroy(RendererComponent);
    }

    #endregion

    #region Mesh builder

    void ResetMesh(NativeArray<Vertex> varray)
    {
        _mesh.Clear();

        var attr_p = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
        var attr_n = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3);

        var vcount = BuilderComponent.VertexCount;
        var icount = BuilderComponent.IndexCount;

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
      => _mesh.SetVertexBufferData(varray, 0, 0, BuilderComponent.VertexCount);

    NativeArray<Vertex> CreateVertexArray()
    {
        var array = SketchUtils.NewTempJobArray<Vertex>(BuilderComponent.VertexCount);
        BuilderComponent.ScheduleVertexJob(array).Complete();
        return array;
    }

    NativeArray<uint> CreateIndexArray()
    {
        var array = SketchUtils.NewTempJobArray<uint>(BuilderComponent.IndexCount);
        BuilderComponent.ScheduleIndexJob(array).Complete();
        return array;
    }

    #endregion
}

} // namespace Sketch
