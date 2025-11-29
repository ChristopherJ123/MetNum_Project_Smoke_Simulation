using UnityEngine;

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
        Camera.main.transform.position = new Vector3(width * cellSize / 2f, height * cellSize / 2f, -10);
        Camera.main.orthographic = true; 
        Camera.main.orthographicSize = height * cellSize / 2f + 1;
    }

    void Update()
    {
        // 1. Check for resize input (e.g., if you changed width/height in Inspector)
        if (grid != null && (grid.width != width || grid.height != height || grid.cellSize != cellSize))
        {
            ResizeSimulation();
        }
        
        // 4. The Simulation Step
        // We divide by 'width' here to normalize the time step for the grid resolution
        // (Standard trick in fluid solvers like Jos Stam's)
        grid.Step(timeStep, viscosity, diffusion);
        
        display.Draw(); // <--- Draw the new library
    }
    
    void ResizeSimulation()
    {
        // A. Resize the Physics Grid
        grid.Resize(width, height, cellSize);

        // B. Re-initialize the Graphics (Texture/Quad) matches new size
        if (display) display.Setup(grid);

        // C. Adjust Camera (Optional, keeps grid centered)
        Camera.main.transform.position = new Vector3(width * cellSize / 2f, height * cellSize / 2f, -10);
        Camera.main.orthographicSize = height * cellSize / 2f + 1;
    }

    public void ClearSimulation()
    {
        grid.Clear();
    }
}