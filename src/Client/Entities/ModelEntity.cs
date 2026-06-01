using System.Numerics;
using Game.Client.Data;
using Game.Client.Data.Files;
using Raylib_cs;

namespace Game.Client.Entities;

class ModelEntity : GameEntity
{
    public static bool DrawWired { get; set; } = false;

    public bool Culling { get; set; } = true;
    public bool CastsShadow { get; set; } = false;
    public bool BoundingBoxRotation { get; set; } = true;
    public string? Path { get; internal set; }

    private Model modelData;
    private BoundingBox modelBoundingBox;

    public ModelEntity(string? map) : base(Vector3.Zero, Vector3.Zero, Vector3.One, null, map)
    {
        LoadModelData(null);
    }

    public ModelEntity(string modelPath, Vector3 pos, string? map, string? name = null) : base(pos, Vector3.Zero, Vector3.One, name, map)
    {
        LoadModelData(modelPath);
    }

    public ModelEntity(ModelEntity mdlEnt) : base(mdlEnt)
    {
        Culling = mdlEnt.Culling;
        CastsShadow = mdlEnt.CastsShadow;
        LoadModelData(mdlEnt.Path);
    }

    public ModelEntity(MapModelFileData fileData, string? map) : base(fileData, map)
    {
        Culling = fileData.Culling;
        CastsShadow = fileData.CastsShadow;
        LoadModelData(fileData.Path);
    }

    public void LoadModelData(string? path)
    {
        modelData = Resources.GetModel(path);
        Path = path;

        modelData.Transform = Matrix4x4.Identity;
        modelBoundingBox = Raylib.GetModelBoundingBox(modelData);

        UpdateBoundingBox();
    }

    public override void SetLightingShader(LightingShaderData shader)
    {
        base.SetLightingShader(shader);

        if (!Raylib.IsModelValid(modelData))
            return;

        for (int i = 0; i < modelData.MaterialCount; i++)
        {
            Raylib.SetMaterialShader(ref modelData, i, ref shader.Shader);
        }
    }

    public unsafe override RayCollision CheckCollisionRay(Ray ray)
    {
        var result = new RayCollision()
        {
            Hit = false,
            Distance = float.MaxValue
        };

        if (!Raylib.IsModelValid(modelData))
            return result;

        for (int i = 0; i < modelData.MeshCount; i++)
        {
            Mesh mesh = modelData.Meshes[i];
            RayCollision meshCol = Raylib.GetRayCollisionMesh(ray, mesh, modelData.Transform);

            if (meshCol.Hit && meshCol.Distance < result.Distance)
                result = meshCol;
        }

        return result;
    }

    public override BoundingBox UpdateBoundingBox()
    {
        var box = modelBoundingBox;

        var trans = modelData.Transform;
        if (!BoundingBoxRotation)
            trans = Utils.CalculateMatrix(Position, Vector3.Zero, Scale);

        Vector3[] corners = [
            new Vector3(box.Min.X, box.Min.Y, box.Min.Z),
            new Vector3(box.Max.X, box.Min.Y, box.Min.Z),
            new Vector3(box.Min.X, box.Max.Y, box.Min.Z),
            new Vector3(box.Max.X, box.Max.Y, box.Min.Z),
            new Vector3(box.Min.X, box.Min.Y, box.Max.Z),
            new Vector3(box.Max.X, box.Min.Y, box.Max.Z),
            new Vector3(box.Min.X, box.Max.Y, box.Max.Z),
            new Vector3(box.Max.X, box.Max.Y, box.Max.Z)
        ];

        var result = new BoundingBox()
        {
            Min = Raymath.Vector3Transform(corners[0], trans),
            Max = Raymath.Vector3Transform(corners[0], trans)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 p = Raymath.Vector3Transform(corners[i], trans);

            result.Min.X = MathF.Min(result.Min.X, p.X);
            result.Min.Y = MathF.Min(result.Min.Y, p.Y);
            result.Min.Z = MathF.Min(result.Min.Z, p.Z);

            result.Max.X = MathF.Max(result.Max.X, p.X);
            result.Max.Y = MathF.Max(result.Max.Y, p.Y);
            result.Max.Z = MathF.Max(result.Max.Z, p.Z);
        }

        return result;
    }

    public override ModelEntity Copy() => new ModelEntity(this);

    public override void Update(float dt)
    {
        base.Update(dt);

        if (Raylib.IsModelValid(modelData))
        {
            modelData.Transform = Utils.CalculateMatrix(Position, Rotation, Scale);
        }
    }

    public override void Draw()
    {
        if (!Visible)
            return;

        if (!Culling)
            Rlgl.DisableBackfaceCulling();

        if (!DrawWired)
            Raylib.DrawModel(modelData, Vector3.Zero, 1, Tint);
        else
            Raylib.DrawModelWires(modelData, Vector3.Zero, 1, Tint);

        if (!Culling)
            Rlgl.EnableBackfaceCulling();
    }
}
