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

    void Start()
    {
        // 1. Initialize the Data
        grid = new FluidGrid(width, height, cellSize);

        // 2. Setup the Visualizer
        drawer = GetComponent<FluidDrawer>();
        drawer.SetGrid(grid);

        // 3. Setup Camera
        Camera.main.transform.position = new Vector3(width * cellSize / 2f, height * cellSize / 2f, -10);
    
        // FORCE Orthographic mode
        Camera.main.orthographic = true; 
    
        Camera.main.orthographicSize = height * cellSize / 2f + 1;
    }

    void Update()
    {
        // 4. The Simulation Step
        // We divide by 'width' here to normalize the time step for the grid resolution
        // (Standard trick in fluid solvers like Jos Stam's)
        grid.Step(timeStep, viscosity, diffusion);    
    }
}