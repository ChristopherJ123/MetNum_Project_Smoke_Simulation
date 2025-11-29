using UnityEngine;

public class FluidDisplay : MonoBehaviour
{
    public enum DrawMode { Density, Divergence, Pressure, Velocity }

    [Header("Settings")]
    public DrawMode drawMode = DrawMode.Density;
    [Range(0, 1)] public float opacity = 1.0f;
    
    [Header("Grid Visualization")]
    public bool showGridLines = true;
    public Color gridLineColor = new Color(1, 1, 1, 0.1f);
    
    [Header("Vectors")]
    public bool showVectors = true;
    [Range(0.1f, 2f)] public float vectorScale = 0.5f;

    [Header("Color Schemes")]
    public Color divergencePositive = Color.red;
    public Color divergenceNegative = Color.cyan;
    public Color pressurePositive = new Color(1f, 0.5f, 0f); // Orange
    public Color pressureNegative = new Color(0f, 0.5f, 1f); // Blue
    
    // References
    public Material fluidMaterial; 
    public Material lineMaterial;

    // Internal Graphics
    private Texture2D texture;
    private Color[] pixels;
    private FluidGrid grid;
    private MeshRenderer meshRenderer;
    private GameObject quadObject;

    public void Setup(FluidGrid grid)
    {
        // Cleanup old
        if (texture != null) Destroy(texture);
        if (quadObject != null) Destroy(quadObject);
        
        this.grid = grid;

        // 1. Create the Texture
        texture = new Texture2D(grid.width, grid.height);
        texture.filterMode = FilterMode.Point; // Keep pixelated look (or Bilinear for smooth)
        texture.wrapMode = TextureWrapMode.Clamp;
        
        pixels = new Color[grid.width * grid.height];

        // 2. Create a Quad to display it
        quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quadObject.transform.parent = transform;
        quadObject.name = "FluidQuad";
        Destroy(quadObject.GetComponent<Collider>()); // Remove collider
        
        // 3. Setup Material (Unlit is fastest/cleanest)
        meshRenderer = quadObject.GetComponent<MeshRenderer>();
        if (fluidMaterial)
        {
            meshRenderer.material = new Material(fluidMaterial);
        }
        else
        {
            meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        }        meshRenderer.material.mainTexture = texture;
        
        // 4. Scale Quad to Match Simulation Dimensions
        // We center it at (width*cell/2, height*cell/2)
        float simWidth = grid.width * grid.cellSize;
        float simHeight = grid.height * grid.cellSize;

        quadObject.transform.localPosition = new Vector3(simWidth * 0.5f, simHeight * 0.5f, 0);
        quadObject.transform.localScale = new Vector3(simWidth, simHeight, 1);
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
    
    // Immediate Mode Drawing for Grid Lines & Vectors
    void OnRenderObject()
    {
        if (grid == null || lineMaterial == null) return;
        
        // Use the assigned material (Must support Vertex Colors!)
        lineMaterial.SetPass(0);

        GL.PushMatrix();
        // Multiply by transform matrix AND shift Z by -0.1 to draw in front of the quad
        GL.MultMatrix(transform.localToWorldMatrix * Matrix4x4.Translate(new Vector3(0, 0, -0.1f)));
        
        // --- DRAW GRID LINES ---
        if (showGridLines)
        {
            GL.Begin(GL.LINES);
            GL.Color(gridLineColor);

            float w = grid.width * grid.cellSize;
            float h = grid.height * grid.cellSize;

            // Vertical lines
            for (int i = 0; i <= grid.width; i++)
            {
                float x = i * grid.cellSize;
                GL.Vertex3(x, 0, 0);
                GL.Vertex3(x, h, 0);
            }

            // Horizontal lines
            for (int j = 0; j <= grid.height; j++)
            {
                float y = j * grid.cellSize;
                GL.Vertex3(0, y, 0);
                GL.Vertex3(w, y, 0);
            }
            GL.End();
        }

        // --- DRAW VECTORS ---
        if (showVectors)
        {
            GL.Begin(GL.LINES);
            for (int y = 0; y < grid.height; y++)
            {
                for (int x = 0; x < grid.width; x++)
                {
                    float px = (x + 0.5f) * grid.cellSize;
                    float py = (y + 0.5f) * grid.cellSize;

                    float u = (grid.VelocitiesX[grid.GetIndexU(x, y)] + grid.VelocitiesX[grid.GetIndexU(x + 1, y)]) * 0.5f;
                    float v = (grid.VelocitiesY[grid.GetIndexV(x, y)] + grid.VelocitiesY[grid.GetIndexV(x, y + 1)]) * 0.5f;

                    // Scale vector color alpha by magnitude so small vectors are invisible
                    float mag = Mathf.Sqrt(u*u + v*v);
                    if (mag > 0.01f)
                    {
                        GL.Color(new Color(1, 1, 1, 0.5f));
                        GL.Vertex3(px, py, 0);
                        GL.Vertex3(px + u * vectorScale, py + v * vectorScale, 0);
                    }
                }
            }
            GL.End();
        }

        GL.PopMatrix();
    }
    
    public void ToggleVectors() { showVectors = !showVectors; }
    public void ToggleGridLines() { showGridLines = !showGridLines; }
}