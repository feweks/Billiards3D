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

    public bool Culling { get; set; } = true;
    public bool Visible { get; set; } = true;
    public bool Active { get; set; } = true;
    public Color Tint = Color.White;
    public string ModelPath { get; }
    public string? Name { get; }
    public string? Map { get; set; }

    public BoundingBox BoundingBox { get => boundingBox; }

    private Model modelData;
    private BoundingBox boundingBox;

    public GameEntity(string modelPath, Vector3 pos, string? name = null)
    {
        Position = pos;
        modelData = Resources.GetModel(modelPath);
        ModelPath = modelPath;
        Name = name;
    }

    public virtual void Update(float dt)
    {
        if (!Active)
            return;

        if (Raylib.IsModelValid(modelData))
        {
            modelData.Transform = UpdateMatrix();
            boundingBox = UpdateBoundingBox();
        }

        Position += Velocity * dt;
    }

    public unsafe RayCollision CheckCollisionRay(Ray ray)
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

    private Matrix4x4 UpdateMatrix()
    {
        var matScale = Raymath.MatrixScale(Scale.X, Scale.Y, Scale.Z);
        var matRot = Raymath.MatrixRotateXYZ(Rotation * Raylib.DEG2RAD);
        var matTrans = Raymath.MatrixTranslate(Position.X, Position.Y, Position.Z);

        return Raymath.MatrixMultiply(Raymath.MatrixMultiply(matScale, matRot), matTrans);
    }

    private BoundingBox UpdateBoundingBox()
    {
        var box = new BoundingBox();

        unsafe
        {
            for (int i = 0; i < modelData.MeshCount; i++)
            {
                var mesh = modelData.Meshes[i];
                BoundingBox meshBox = Raylib.GetMeshBoundingBox(mesh);

                Vector3 min = Raymath.Vector3Min(box.Min, meshBox.Min);
                Vector3 max = Raymath.Vector3Max(box.Max, meshBox.Max);

                box.Min = min;
                box.Max = max;
            }
        }

        Matrix4x4 trans = Raymath.MatrixMultiply(Raymath.MatrixTranslate(Position.X, Position.Y, Position.Z), Raymath.MatrixScale(Scale.X, Scale.Y, Scale.Z));
        box.Min = Raymath.Vector3Transform(box.Min, trans);
        box.Max = Raymath.Vector3Transform(box.Max, trans);

        return box;
    }
}
