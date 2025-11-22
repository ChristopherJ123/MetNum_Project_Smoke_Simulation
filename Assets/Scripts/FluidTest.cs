using UnityEngine;

public class FluidTest : MonoBehaviour
{
    public int width = 64;
    public int height = 32;
    public float cellSize = 0.2f;

    // Simulation parameters
    public float viscosity = 0.0001f; // Unused currently but good to have
    public float diffusion = 0.0001f;

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
        // Simulation logic will go here (Simulate Step) in the next phase!
    }
}