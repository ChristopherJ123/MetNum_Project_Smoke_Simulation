using UnityEngine;

public class FluidGrid
{
    // Grid dimensions
    public int width;
    public int height;
    public float cellSize;

    // Physics arrays
    // 'u' is horizontal velocity, 'v' is vertical velocity
    public float[] u; 
    public float[] v; 
    public float[] density; // The visible smoke
    public float[] pressure;

    // Helper arrays for the solver (prev values)
    public float[] prevU;
    public float[] prevV;
    public float[] prevDensity;

    // Constructor to initialize the grid
    public FluidGrid(int width, int height, float cellSize)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        int cellCount = width * height;

        // Initialize arrays
        // Note: Velocity arrays are slightly larger due to the staggered grid structure
        // Horizontal velocities (u) have one extra column
        // Vertical velocities (v) have one extra row
        u = new float[(width + 1) * height];
        v = new float[width * (height + 1)];
        
        density = new float[cellCount];
        pressure = new float[cellCount];

        // Initialize 'previous' arrays for the solving steps
        prevU = new float[u.Length];
        prevV = new float[v.Length];
        prevDensity = new float[density.Length];
    }
    
    // Helper to get 1D index for cell center arrays (density, pressure)
    public int GetIndex(int x, int y)
    {
        return x + y * width;
    }

    // Helper to get 1D index for U (horizontal velocity)
    public int GetIndexU(int x, int y)
    {
        return x + y * (width + 1);
    }

    // Helper to get 1D index for V (vertical velocity)
    public int GetIndexV(int x, int y)
    {
        return x + y * width;
    }
}