using UnityEngine;
using UnityEngine.UI;

public class FluidTest : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 32;
    public int height = 32;
    public float cellSize = 0.5f;

    [Header("Simulation Settings")]
    public float timeStep = 0.02f; // Keep this small!
    public float viscosity = 0.00001f;
    public float diffusion = 0.00001f;
    
    [Header("Obstacle")]
    public bool enableObstacle = false; // Toggle this in Inspector!
    [Range(1, 20)] public int obstacleSize = 10;
    
    [Header("UI References")]
    public Scrollbar obstacleSizeScrollbar; // Assign this in Inspector

    private FluidGrid grid;
    private FluidDrawer drawer;
    private FluidDisplay display; // <--- The new library

    void Start()
    {
        // 1. Initialize the Data
        grid = new FluidGrid(width, height, cellSize);

        // 2. Setup the Visualizer
        drawer = GetComponent<FluidDrawer>();
        drawer.SetGrid(grid);
        drawer.drawDensity = false; // Disable Gizmos
        drawer.drawVelocity = false; // Disable Gizmos
        
        // 3. Setup the Display
        display = gameObject.GetComponent<FluidDisplay>();
        display.Setup(grid);

        // 4. Setup Camera
        // Camera.main.transform.position = new Vector3(width * cellSize / 2f, height * cellSize / 2f, -10);
        // Camera.main.orthographic = true; 
        // Camera.main.orthographicSize = height * cellSize / 2f + 1;
        
        ResizeSimulation();
        
        // --- UI HOOKUP ---
        if (obstacleSizeScrollbar != null)
        {
            // Set initial value based on current obstacleSize (map 1..20 to 0..1)
            float normalizedVal = Mathf.InverseLerp(1, 20, obstacleSize);
            obstacleSizeScrollbar.value = normalizedVal;

            // Add listener for changes
            obstacleSizeScrollbar.onValueChanged.AddListener(OnObstacleSizeChanged);
        }
    }
    
    // Called automatically when scrollbar moves
    public void OnObstacleSizeChanged(float val)
    {
        // Map 0..1 back to 1..20
        obstacleSize = Mathf.RoundToInt(Mathf.Lerp(1, 20, val));
    }

    void Update()
    {
        // 1. Check for resize input (e.g., if you changed width/height in Inspector)
        if (grid != null && (grid.width != width || grid.height != height || grid.cellSize != cellSize))
        {
            ResizeSimulation();
        }
        
        // --- UPDATE OBSTACLE ---
        UpdateObstacle();
        
        // 4. The Simulation Step
        // We divide by 'width' here to normalize the time step for the grid resolution
        // (Standard trick in fluid solvers like Jos Stam's)
        grid.Step(timeStep, viscosity, diffusion);
        
        display.Draw(); // <--- Draw the new library
    }
    
    void UpdateObstacle()
    {
        // Reset solid array
        System.Array.Clear(grid.s, 0, grid.s.Length);

        if (enableObstacle)
        {
            int cx = width / 2;
            int cy = height / 2;
            int r = obstacleSize;

            for (int y = cy - r; y <= cy + r; y++)
            {
                for (int x = cx - r; x <= cx + r; x++)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        grid.s[grid.GetIndex(x, y)] = 1.0f; // Mark as solid
                        grid.density[grid.GetIndex(x, y)] = 0f; // Clear smoke inside
                    }
                }
            }
        }
    }
    
    void ResizeSimulation()
    {
        // A. Resize the Physics Grid
        grid.Resize(width, height, cellSize);

        // B. Re-initialize the Graphics (Texture/Quad) matches new size
        if (display) display.Setup(grid);

        // C. Adjust Camera (Optional, keeps grid centered)
        // --- FULLSCREEN CAMERA SETUP ---
        float gridWidthWorld = width * cellSize;
        float gridHeightWorld = height * cellSize;
        
        Camera.main.transform.position = new Vector3(width * cellSize / 2f, height * cellSize / 2f, -10);
        
        // Scale camera to fit HEIGHT exactly
        Camera.main.orthographic = true;
        Camera.main.orthographicSize = gridHeightWorld * 0.5f;    
    }

    public void ClearSimulation()
    {
        grid.Clear();
    }
    
    public void ToggleObstacle() { enableObstacle = !enableObstacle; }
    
    public void ChangeObstacleSize(int size) { obstacleSize = size; }
}