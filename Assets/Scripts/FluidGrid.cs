using UnityEngine;

public class FluidGrid
{
    // Grid dimensions
    public int width;
    public int height;
    public float cellSize;

    // Physics arrays
    public float[] u; 
    public float[] v; 
    public float[] density; 
    public float[] pressure;

    // Helper arrays
    public float[] prevU;
    public float[] prevV;
    public float[] prevDensity;

    // divergence array for pressure solve (cell-centered)
    private float[] divergence;

    public FluidGrid(int width, int height, float cellSize)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;

        int cellCount = width * height;

        // Velocity arrays are larger (Staggered Grid)
        u = new float[(width + 1) * height];       // u at vertical edges (x in [0..width])
        v = new float[width * (height + 1)];      // v at horizontal edges (y in [0..height])
        
        density = new float[cellCount];
        pressure = new float[cellCount];
        divergence = new float[cellCount];
        
        Debug.Log($"FluidGrid Initialized: {width}x{height} (Cells: {cellCount})");

        prevU = new float[u.Length];
        prevV = new float[v.Length];
        prevDensity = new float[density.Length];
    }
    
    public void Step(float dt, float viscosity, float diffusion)
    {
        if (dt <= 0f) return;

        // 1. Prepare: Copy current state to "Previous" state
        System.Array.Copy(u, prevU, u.Length);
        System.Array.Copy(v, prevV, v.Length);
        System.Array.Copy(density, prevDensity, density.Length);

        // 2. Advect Velocity (Self-Advection)
        AdvectU(u, prevU, prevV, dt);
        AdvectV(v, prevU, prevV, dt);

        // 3. Advect Density (cell-centered)
        AdvectScalar(density, prevDensity, prevU, prevV, dt);

        // --- Velocity Decay (damping) ---
        for (int i = 0; i < u.Length; i++) u[i] *= 0.99f; // Less aggressive damping is usually fine with stable code
        for (int i = 0; i < v.Length; i++) v[i] *= 0.99f;

        // 4. Solve Pressure
        Project(dt);
    }

    // Advect for u component (staggered in x). 
    // FIXED: Account for the fact that u lives at y + 0.5
    void AdvectU(float[] destU, float[] uArr, float[] vArr, float dt)
    {
        float dt0 = dt / cellSize;

        for (int j = 0; j < height; j++)
        {
            for (int i = 1; i < width; i++)
            {
                float u_vel = uArr[GetIndexU(i, j)];
                
                // Average V to the U-face location
                int j0v = Mathf.Clamp(j,     0, height);
                int j1v = Mathf.Clamp(j + 1, 0, height);
                int i0u = Mathf.Clamp(i - 1, 0, width - 1);
                int i1u = Mathf.Clamp(i,     0, width - 1);

                float v_vel = 0.25f * (
                    vArr[GetIndexV(i0u, j0v)] +
                    vArr[GetIndexV(i1u, j0v)] +
                    vArr[GetIndexV(i0u, j1v)] +
                    vArr[GetIndexV(i1u, j1v)]
                );

                // Backtrace
                // u is located physically at (i, j + 0.5)
                float x = i - dt0 * u_vel;
                float y = (j + 0.5f) - dt0 * v_vel;

                // Clamp
                if (x < 0.5f) x = 0.5f; if (x > width - 0.5f) x = width - 0.5f;
                if (y < 0.5f) y = 0.5f; if (y > height - 0.5f) y = height - 0.5f; // Keep within valid grid range

                // --- KEY FIX ---
                // Convert physical coordinate 'y' to 'index space' for u-array.
                // Since u[i,j] is at y=j+0.5, the index j = y - 0.5
                float y_index = y - 0.5f; 
                if (y_index < 0) y_index = 0;
                if (y_index > height - 1) y_index = height - 1;

                int i0 = (int)x;
                int j0 = (int)y_index; // Use the shifted index
                int i1 = Mathf.Min(i0 + 1, width);
                int j1 = Mathf.Min(j0 + 1, height - 1);

                float s1 = x - i0; float s0 = 1 - s1;
                float t1 = y_index - j0; float t0 = 1 - t1; // Use shifted t1

                int idxTL = GetIndexU(i0, j0);
                int idxTR = GetIndexU(i1, j0);
                int idxBL = GetIndexU(i0, j1);
                int idxBR = GetIndexU(i1, j1);

                destU[GetIndexU(i, j)] = s0 * (t0 * uArr[idxTL] + t1 * uArr[idxBL]) +
                                          s1 * (t0 * uArr[idxTR] + t1 * uArr[idxBR]);
            }
        }
    }

    // Advect for v component (staggered in y).
    // FIXED: Account for the fact that v lives at x + 0.5
    void AdvectV(float[] destV, float[] uArr, float[] vArr, float dt)
    {
        float dt0 = dt / cellSize;

        for (int j = 1; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                // Average U to the V-face location
                float u_vel = 0.25f * (uArr[GetIndexU(i, j - 1)] + uArr[GetIndexU(i + 1, j - 1)] +
                                       uArr[GetIndexU(i, j)] + uArr[GetIndexU(i + 1, j)]);
                float v_vel = vArr[GetIndexV(i, j)];

                // Backtrace
                // v is located physically at (i + 0.5, j)
                float x = (i + 0.5f) - dt0 * u_vel;
                float y = j - dt0 * v_vel;

                // Clamp
                if (x < 0.5f) x = 0.5f; if (x > width - 0.5f) x = width - 0.5f;
                if (y < 0.5f) y = 0.5f; if (y > height - 0.5f) y = height - 0.5f;

                // --- KEY FIX ---
                // Convert physical coordinate 'x' to 'index space' for v-array.
                // Since v[i,j] is at x=i+0.5, the index i = x - 0.5
                float x_index = x - 0.5f;
                if (x_index < 0) x_index = 0;
                if (x_index > width - 1) x_index = width - 1;

                int i0 = (int)x_index; // Use shifted index
                int i1 = Mathf.Min(i0 + 1, width - 1);
                int j0 = (int)y;
                int j1 = Mathf.Min(j0 + 1, height);

                float s1 = x_index - i0; float s0 = 1 - s1; // Use shifted s1
                float t1 = y - j0; float t0 = 1 - t1;

                int idxTL = GetIndexV(i0, j0);
                int idxTR = GetIndexV(i1, j0);
                int idxBL = GetIndexV(i0, j1);
                int idxBR = GetIndexV(i1, j1);

                destV[GetIndexV(i, j)] = s0 * (t0 * vArr[idxTL] + t1 * vArr[idxBL]) +
                                          s1 * (t0 * vArr[idxTR] + t1 * vArr[idxBR]);
            }
        }
    }

    // Advect scalar (density) - cell-centered
    // This was largely correct, just minor cleanup
    void AdvectScalar(float[] d, float[] d0, float[] uArr, float[] vArr, float dt)
    {
        float dt0 = dt / cellSize;

        for (int j = 1; j < height - 1; j++)
        {
            for (int i = 1; i < width - 1; i++)
            {
                // Average U and V to cell center
                float u_vel = 0.5f * (uArr[GetIndexU(i, j)] + uArr[GetIndexU(i + 1, j)]);
                float v_vel = 0.5f * (vArr[GetIndexV(i, j)] + vArr[GetIndexV(i, j + 1)]);

                // Backtrace from cell center (i + 0.5, j + 0.5)
                // BUT: Since density array indices align with integer coordinates in our mental model (0..width),
                // we treat the "center" as simply (i,j) for interpolation purposes relative to the density grid itself.
                float x = i - dt0 * u_vel;
                float y = j - dt0 * v_vel;

                if (x < 0.5f) x = 0.5f; if (x > width - 1.5f) x = width - 1.5f;
                if (y < 0.5f) y = 0.5f; if (y > height - 1.5f) y = height - 1.5f;

                int i0 = (int)x; int i1 = i0 + 1;
                int j0 = (int)y; int j1 = j0 + 1;

                float s1 = x - i0; float s0 = 1 - s1;
                float t1 = y - j0; float t0 = 1 - t1;

                int idx_TL = GetIndex(i0, j0);
                int idx_TR = GetIndex(i1, j0);
                int idx_BL = GetIndex(i0, j1);
                int idx_BR = GetIndex(i1, j1);

                d[GetIndex(i, j)] = s0 * (t0 * d0[idx_TL] + t1 * d0[idx_BL]) +
                                    s1 * (t0 * d0[idx_TR] + t1 * d0[idx_BR]);
            }
        }
    }

    void Project(float dt)
    {
        float invH = 1.0f / cellSize;

        // 1. Compute divergence
        for (int j = 1; j < height - 1; j++)
        {
            for (int i = 1; i < width - 1; i++)
            {
                float du = u[GetIndexU(i + 1, j)] - u[GetIndexU(i, j)];
                float dv = v[GetIndexV(i, j + 1)] - v[GetIndexV(i, j)];

                // FIX 1: Negative sign and multiply by cellSize
                divergence[GetIndex(i, j)] = -(du + dv) * cellSize / dt;
            
                pressure[GetIndex(i, j)] = 0f;
            }
        }

        // 2. Solve Pressure (Jacobi)
        for (int iter = 0; iter < 40; iter++)
        {
            // FIX 2: Explicitly apply boundaries to the ghost cells (0 and max)
            // This ensures the solver 'sees' the walls correctly during iteration
            ApplyPressureBoundaries();

            for (int j = 1; j < height - 1; j++)
            {
                for (int i = 1; i < width - 1; i++)
                {
                    // Now we can just safely access neighbors because we set them in ApplyPressureBoundaries
                    float pL = pressure[GetIndex(i - 1, j)];
                    float pR = pressure[GetIndex(i + 1, j)];
                    float pB = pressure[GetIndex(i, j - 1)];
                    float pT = pressure[GetIndex(i, j + 1)];

                    pressure[GetIndex(i, j)] = (divergence[GetIndex(i, j)] + pL + pR + pB + pT) * 0.25f;
                }
            }
        }
    
        // Apply one last time before calculating gradient so the wall subtraction is correct
        ApplyPressureBoundaries();

        // 3. Subtract gradient
        for (int j = 1; j < height - 1; j++)
        {
            for (int i = 1; i < width - 1; i++)
            {
                float gradPx = (pressure[GetIndex(i, j)] - pressure[GetIndex(i - 1, j)]) * invH;
                float gradPy = (pressure[GetIndex(i, j)] - pressure[GetIndex(i, j - 1)]) * invH;

                u[GetIndexU(i, j)] -= gradPx * dt;
                v[GetIndexV(i, j)] -= gradPy * dt;
            }
        }
    }    
    
    // Add this method to FluidGrid class
    void ApplyPressureBoundaries()
    {
        for (int j = 0; j < height; j++)
        {
            // Left wall: P[0] = P[1]
            pressure[GetIndex(0, j)] = pressure[GetIndex(1, j)];
            // Right wall: P[width-1] = P[width-2]
            pressure[GetIndex(width - 1, j)] = pressure[GetIndex(width - 2, j)];
        }
        for (int i = 0; i < width; i++)
        {
            // Bottom wall: P[0] = P[1]
            pressure[GetIndex(i, 0)] = pressure[GetIndex(i, 1)];
            // Top wall: P[height-1] = P[height-2]
            pressure[GetIndex(i, height - 1)] = pressure[GetIndex(i, height - 2)];
        }
    }
    public int GetIndex(int x, int y) => x + y * width;
    public int GetIndexU(int x, int y) => x + y * (width + 1);
    public int GetIndexV(int x, int y) => x + y * width;
}