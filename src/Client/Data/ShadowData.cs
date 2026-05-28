using System.Numerics;
using Raylib_cs;

namespace Game.Client.Data;

class ShadowData
{
    private Texture2D shadowTex;
    private Mesh shadowMesh;
    private Material shadowMat;

    public ShadowData()
    {
        shadowTex = GenerateTexture();
        shadowMesh = Raylib.GenMeshPlane(1, 1, 1, 1);
        shadowMat = Raylib.LoadMaterialDefault();
        Raylib.SetMaterialTexture(ref shadowMat, MaterialMapIndex.Diffuse, shadowTex);
        unsafe
        {
            shadowMat.Maps[(int)MaterialMapIndex.Diffuse].Color = Raylib.ColorAlpha(Color.White, 0.25f);
        }
    }

    private Texture2D GenerateTexture()
    {
        const int SHADOW_SIZE = 64;

        Image result = Raylib.GenImageColor(SHADOW_SIZE, SHADOW_SIZE, Color.Blank);

        float center = SHADOW_SIZE / 2f;
        float maxRadius = SHADOW_SIZE / 2f;
        for (int y = 0; y < SHADOW_SIZE; y++)
        {
            for (int x = 0; x < SHADOW_SIZE; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distance = MathF.Sqrt(dx * dx + dy * dy);

                if (distance < maxRadius)
                {
                    float alpha = 1f - (distance / maxRadius);
                    alpha *= alpha;

                    var col = new Color(0, 0, 0, alpha * 45);
                    Raylib.ImageDrawPixel(ref result, x, y, col);
                }
            }
        }

        Texture2D tex = Raylib.LoadTextureFromImage(result);
        Raylib.UnloadImage(result);
        return tex;
    }

    public void Draw(Vector3 pos, float sizeX, float sizeZ)
    {
        var mat = Utils.CalculateMatrix(pos, Vector3.Zero, new Vector3(sizeX, 1, sizeZ));

        Raylib.DrawMesh(shadowMesh, shadowMat, mat);
    }

    public void Destroy()
    {
        Raylib.UnloadTexture(shadowTex);
        Raylib.UnloadMesh(shadowMesh);
        Raylib.UnloadMaterial(shadowMat);
    }
}
