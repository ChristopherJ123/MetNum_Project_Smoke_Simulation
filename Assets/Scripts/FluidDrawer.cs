using UnityEngine;

public class FluidDrawer : MonoBehaviour
{
    private FluidGrid grid;

    [Header("Visualization")]
    public bool drawDensity = true;
    public bool drawVelocity = true;
    [Range(0, 1)] public float opacity = 0.5f;

    [Header("Interaction")]
    public float interactionRadius = 2f;
    public float interactionStrength = 50f;

    public void SetGrid(FluidGrid grid)
    {
        this.grid = grid;
    }

    void Update()
    {
        if (grid == null) return;
        HandleInteraction();
    }

    void HandleInteraction()
    {
        if (Input.GetMouseButton(0)) 
        {
            // Calculate mouse position in grid coordinates
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = 10f; 
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            
            int cx = (int)(worldPos.x / grid.cellSize);
            int cy = (int)(worldPos.y / grid.cellSize);

            // CALL THE HELPER METHODS HERE
            AddDensity(cx, cy);
            AddVelocity(cx, cy);
        }
    }

    void AddDensity(int cx, int cy)
    {
        int r = (int)(interactionRadius / grid.cellSize);

        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                // Safety check: ensure we are inside the grid
                if (x >= 1 && x < grid.width - 1 && y >= 1 && y < grid.height - 1)
                {
                    // Add density
                    int idx = grid.GetIndex(x, y);
                    grid.density[idx] = Mathf.Clamp01(grid.density[idx] + 0.5f);
                }
            }
        }
    }

    void AddVelocity(int cx, int cy)
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        Vector2 force = new Vector2(mouseX, mouseY) * interactionStrength;

        // Ensure we are safely inside the grid (padding of 1 cell)
        if (cx >= 1 && cx < grid.width - 1 && cy >= 1 && cy < grid.height - 1) 
        {
            // --- SYMMETRICAL FORCE APPLICATION (Fixes the Bias) ---
            
            // Split the Horizontal force between Left and Right walls
            grid.VelocitiesX[grid.GetIndexU(cx, cy)]     += force.x * 0.5f;
            grid.VelocitiesX[grid.GetIndexU(cx + 1, cy)] += force.x * 0.5f;

            // Split the Vertical force between Bottom and Top walls
            grid.VelocitiesY[grid.GetIndexV(cx, cy)]     += force.y * 0.5f;
            grid.VelocitiesY[grid.GetIndexV(cx, cy + 1)] += force.y * 0.5f;
        }
    }

    void OnDrawGizmos()
    {
        if (grid == null) return;

        Gizmos.color = Color.grey;
        Gizmos.DrawWireCube(new Vector3(grid.width * grid.cellSize / 2f, grid.height * grid.cellSize / 2f, 0), 
            new Vector3(grid.width * grid.cellSize, grid.height * grid.cellSize, 1));

        for (int y = 0; y < grid.height; y++)
        {
            for (int x = 0; x < grid.width; x++)
            {
                Vector3 pos = new Vector3((x + 0.5f) * grid.cellSize, (y + 0.5f) * grid.cellSize, 0);
                
                if (drawDensity)
                {
                    float d = grid.density[grid.GetIndex(x, y)];
                    if (d > 0.01f)
                    {
                        Gizmos.color = new Color(1, 1, 1, d * opacity);
                        Gizmos.DrawCube(pos, Vector3.one * grid.cellSize);
                    }
                }

                if (drawVelocity)
                {
                    float u = (grid.VelocitiesX[grid.GetIndexU(x, y)] + grid.VelocitiesX[grid.GetIndexU(x + 1, y)]) * 0.5f;
                    float v = (grid.VelocitiesY[grid.GetIndexV(x, y)] + grid.VelocitiesY[grid.GetIndexV(x, y + 1)]) * 0.5f;

                    if (Mathf.Abs(u) + Mathf.Abs(v) > 0.1f)
                    {
                        Gizmos.color = Color.red;
                        Vector3 vel = new Vector3(u, v, 0) * 0.1f; 
                        Gizmos.DrawRay(pos, vel);
                    }
                }
            }
        }
    }
}