using Game.Client.Data;
using Game.Client.States;
using Game.Common.Enums;
using Raylib_cs;
using System.Numerics;

namespace Game.Client.Entities;

class GameEntity
{
    public static bool DrawWired { get; set; } = false;

    public Vector3 Position;
    public Vector3 Rotation = Vector3.Zero;
    public Vector3 Scale = Vector3.One;
    public Vector3 Velocity = Vector3.Zero;
    public LightingShaderData? LightingShader { get; internal set; }

    public bool Culling { get; set; } = true;
    public bool HasShadow { get; set; } = false;
    public bool Visible { get; set; } = true;
    public bool Active { get; set; } = true;
    public Color Tint = Color.White;
    public string? ModelPath { get; internal set; }
    public string? Name { get; set; }
    public string? Map { get; set; }

    public BoundingBox BoundingBox { get => boundingBox; }

    private Model modelData;
    private BoundingBox boundingBox;

    private Texture2D shadowTex;

    public GameEntity(string? modelPath, Vector3 pos, string? name = null)
    {
        Position = pos;
        Name = name;
        LoadModelData(modelPath);

        shadowTex = Resources.GetTexture("resources/gfx/shadow.png");
    }

    public void LoadModelData(string? path)
    {
        modelData = Resources.GetModel(path);
        ModelPath = path;

        UpdateBoundingBox();
    }

    public void SetLightingShader(LightingShaderData shader)
    {
        LightingShader = shader;

        if (!Raylib.IsModelValid(modelData))
            return;

        for (int i = 0; i < modelData.MaterialCount; i++)
        {
            Raylib.SetMaterialShader(ref modelData, i, ref shader.Shader);
        }
    }

    public virtual void Update(float dt)
    {
        if (!Active)
            return;

        if (Raylib.IsModelValid(modelData))
        {
            modelData.Transform = Utils.CalculateMatrix(Position, Rotation, Scale);
            boundingBox = UpdateBoundingBox();
        }

        Position += Velocity * dt;
    }

    public unsafe virtual RayCollision CheckCollisionRay(Ray ray)
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

    public virtual void Draw()
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

    private BoundingBox UpdateBoundingBox()
    {
        // hack to get bounding box without any transformations done to it
        var prevTrans = modelData.Transform;
        modelData.Transform = Matrix4x4.Identity;
        var box = Raylib.GetModelBoundingBox(modelData);
        modelData.Transform = prevTrans;

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
            Min = Raymath.Vector3Transform(corners[0], prevTrans),
            Max = Raymath.Vector3Transform(corners[0], prevTrans)
        };

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 p = Raymath.Vector3Transform(corners[i], prevTrans);

            result.Min.X = MathF.Min(result.Min.X, p.X);
            result.Min.Y = MathF.Min(result.Min.Y, p.Y);
            result.Min.Z = MathF.Min(result.Min.Z, p.Z);

            result.Max.X = MathF.Max(result.Max.X, p.X);
            result.Max.Y = MathF.Max(result.Max.Y, p.Y);
            result.Max.Z = MathF.Max(result.Max.Z, p.Z);
        }

        return result;
    }
}
