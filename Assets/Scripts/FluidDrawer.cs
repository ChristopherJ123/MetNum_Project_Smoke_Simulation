using UnityEngine;

public class FluidDrawer : MonoBehaviour
{
    private FluidGrid grid;

    // Settings
    [Header("Visualization")]
    public bool drawDensity = true;
    public bool drawVelocity = true;
    [Range(0, 1)] public float opacity = 1f;

    [Header("Interaction")]
    public float interactionRadius = 2f;
    public float interactionStrength = 10f;

    // Assign the grid from the Test script
    public void SetGrid(FluidGrid grid)
    {
        this.grid = grid;
    }

    void Update()
    {
        if (grid == null) return;
        HandleInteraction();
    }

    // Basic Mouse Interaction
    void HandleInteraction()
    {
        if (Input.GetMouseButton(0)) // Left Click
        {
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; // distance from camera to scene
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            
            // Convert mouse world position to grid coordinates (local to grid origin at 0,0)
            // Assuming grid starts at (0,0). If you offset the grid, subtract position here.
            int centerX = (int)(worldPos.x / grid.cellSize);
            int centerY = (int)(worldPos.y / grid.cellSize);
            
            Debug.Log($"Mouse Position: ({worldPos.x}, {worldPos.y})");
            Debug.Log($"Mouse Position to Grid: ({centerX}, {centerY})");

            // Add "Smoke" (Density) and Velocity at mouse position
            AddDensity(centerX, centerY);
            AddVelocity(centerX, centerY);
        }
    }

    void AddDensity(int cx, int cy)
    {
        // Simple "Brush" loop
        int r = (int)(interactionRadius / grid.cellSize);
        
        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x >= 0 && x < grid.width && y >= 0 && y < grid.height)
                {
                    // Add density to the cell
                    grid.density[grid.GetIndex(x, y)] = 1.0f; // Max density
                }
            }
        }
    }

    void AddVelocity(int cx, int cy)
    {
        // Calculate mouse movement direction
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        Vector2 force = new Vector2(mouseX, mouseY) * interactionStrength;

        int idx = grid.GetIndex(Mathf.Clamp(cx, 0, grid.width-1), Mathf.Clamp(cy, 0, grid.height-1));
        
        // Apply force to velocity arrays (simplified for center)
        // Note: In a real staggered grid, you'd interpolate to edges.
        // Here we just hack it to the nearest u/v indices for testing.
        if (cx < grid.width && cy < grid.height && cx >=0 && cy >= 0) {
            grid.u[grid.GetIndexU(cx, cy)] += force.x;
            grid.v[grid.GetIndexV(cx, cy)] += force.y;
        }
    }

    // Unity's Debug Drawing Loop
    void OnDrawGizmos()
    {
        if (grid == null) return;

        // Draw Grid Bounds
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(new Vector3(grid.width * grid.cellSize / 2f, grid.height * grid.cellSize / 2f, 0), 
                            new Vector3(grid.width * grid.cellSize, grid.height * grid.cellSize, 1));

        for (int y = 0; y < grid.height; y++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                Vector3 cellCenter = new Vector3((x + 0.5f) * grid.cellSize, (y + 0.5f) * grid.cellSize, 0);

                // 1. Draw Density (Smoke)
                if (drawDensity)
                {
                    float d = grid.density[grid.GetIndex(x, y)];
                    if (d > 0.01f)
                    {
                        Gizmos.color = new Color(1, 1, 1, d * opacity);
                        Gizmos.DrawCube(cellCenter, Vector3.one * grid.cellSize);
                    }
                }

                // 2. Draw Velocity Vector
                if (drawVelocity)
                {
                    // Sample staggered velocity at the center (average of edges)
                    float uL = grid.u[grid.GetIndexU(x, y)];
                    float uR = grid.u[grid.GetIndexU(x + 1, y)];
                    float vB = grid.v[grid.GetIndexV(x, y)];
                    float vT = grid.v[grid.GetIndexV(x, y + 1)];

                    Vector3 vel = new Vector3((uL + uR) * 0.5f, (vB + vT) * 0.5f, 0);

                    if (vel.sqrMagnitude > 0.01f)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawLine(cellCenter, cellCenter + vel);
                    }
                }
            }
        }
    }
}