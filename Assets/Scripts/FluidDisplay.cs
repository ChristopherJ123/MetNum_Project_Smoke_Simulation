using UnityEngine;

public class FluidDisplay : MonoBehaviour
{
    public enum DrawMode { Density, Divergence, Pressure, Velocity }

    [Header("Settings")]
    public DrawMode drawMode = DrawMode.Density;
    [Range(0, 1)] public float opacity = 1.0f;
    public bool showVectors = false;
    [Range(0.1f, 2f)] public float vectorScale = 0.5f;

    [Header("Color Schemes")]
    public Gradient densityColor;
    public Color divergencePositive = Color.red;
    public Color divergenceNegative = Color.cyan;
    public Color pressurePositive = new Color(1f, 0.5f, 0f); // Orange
    public Color pressureNegative = new Color(0f, 0.5f, 1f); // Blue

    // Internal Graphics
    private Texture2D texture;
    private Color[] pixels;
    private FluidGrid grid;
    private MeshRenderer meshRenderer;

    public void Setup(FluidGrid grid)
    {
        // --- FIX: CLEANUP OLD GRAPHICS ---
        // If we already have a texture, destroy it to free memory
        if (texture) Destroy(texture);
    
        // If we already created a Quad child object, destroy it
        foreach (Transform child in transform) Destroy(child.gameObject);
        // ---------------------------------
        
        this.grid = grid;

        // 1. Create the Texture
        texture = new Texture2D(grid.width, grid.height);
        texture.filterMode = FilterMode.Point; // Keep pixelated look (or Bilinear for smooth)
        texture.wrapMode = TextureWrapMode.Clamp;
        
        pixels = new Color[grid.width * grid.height];

        // 2. Create a Quad to display it
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.parent = transform;
        quad.transform.localPosition = new Vector3(grid.width * grid.cellSize * 0.5f, grid.height * grid.cellSize * 0.5f, 0);
        quad.transform.localScale = new Vector3(grid.width * grid.cellSize, grid.height * grid.cellSize, 1);
        
        // 3. Setup Material (Unlit is fastest/cleanest)
        meshRenderer = quad.GetComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        meshRenderer.material.mainTexture = texture;
        
        // Remove collider so it doesn't block mouse clicks
        Destroy(quad.GetComponent<Collider>());
    }

    public void Draw()
    {
        if (grid == null) return;

        switch (drawMode)
        {
            case DrawMode.Density:    DrawDensity(); break;
            case DrawMode.Divergence: DrawDivergence(); break;
            case DrawMode.Pressure:   DrawPressure(); break;
            case DrawMode.Velocity:   DrawVelocityMag(); break;
        }

        // Upload pixels to GPU
        texture.SetPixels(pixels);
        texture.Apply();
    }

    void DrawDensity()
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            float d = grid.density[i];
            // Simple grayscale or use a gradient if you want smoke colors
            Color c = Color.white * d; 
            c.a = 1f; // Always opaque background usually looks better, or use d for alpha
            pixels[i] = c;
        }
    }

    void DrawDivergence()
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            float div = grid.divergence[i];
            // Visualize error: Red = Expansion (+), Cyan = Compression (-)
            Color c = (div > 0) ? divergencePositive : divergenceNegative;
            c *= Mathf.Abs(div) * 50f; // Scale up to see small errors
            c.a = 1f;
            pixels[i] = c;
        }
    }

    void DrawPressure()
    {
        for (int i = 0; i < pixels.Length; i++)
        {
            float p = grid.pressure[i];
            Color c = (p > 0) ? pressurePositive : pressureNegative;
            c *= Mathf.Abs(p) * 2f; // Scale pressure visualization
            c.a = 1f;
            pixels[i] = c;
        }
    }

    void DrawVelocityMag()
    {
        for (int y = 0; y < grid.height; y++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                // Average staggered velocity to center
                float u = (grid.VelocitiesX[grid.GetIndexU(x, y)] + grid.VelocitiesX[grid.GetIndexU(x + 1, y)]) * 0.5f;
                float v = (grid.VelocitiesY[grid.GetIndexV(x, y)] + grid.VelocitiesY[grid.GetIndexV(x, y + 1)]) * 0.5f;
                float mag = Mathf.Sqrt(u*u + v*v);
                
                pixels[grid.GetIndex(x, y)] = new Color(mag, mag, mag, 1f);
            }
        }
    }
    
    public void SetDrawMode(int mode)
    {
        drawMode = (DrawMode)mode;
        Debug.Log("Draw mode changed to: " + drawMode);
    }

    // Unity callback for immediate mode drawing (GL)
    // This draws lines on top of the scene
    void OnRenderObject()
    {
        if (grid == null || !showVectors) return;

        // Setup material for drawing lines
        Material lineMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        lineMat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);

        for (int y = 0; y < grid.height; y++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                // Center position
                float px = (x + 0.5f) * grid.cellSize;
                float py = (y + 0.5f) * grid.cellSize;

                // Average velocity
                float u = (grid.VelocitiesX[grid.GetIndexU(x, y)] + grid.VelocitiesX[grid.GetIndexU(x + 1, y)]) * 0.5f;
                float v = (grid.VelocitiesY[grid.GetIndexV(x, y)] + grid.VelocitiesY[grid.GetIndexV(x, y + 1)]) * 0.5f;

                // Draw Line
                GL.Color(new Color(1, 1, 1, 0.3f));
                GL.Vertex3(px, py, 0);
                GL.Vertex3(px + u * vectorScale, py + v * vectorScale, 0);
            }
        }

        GL.End();
        GL.PopMatrix();
    }
}