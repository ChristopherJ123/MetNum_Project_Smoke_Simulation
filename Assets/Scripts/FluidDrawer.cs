using UnityEngine;

public class FluidDrawer : MonoBehaviour
{
    private FluidGrid grid;

    [Header("Visualization")]
    public bool drawDensity = true;
    public bool drawVelocity = true;
    [Range(0, 1)] public float opacity = 0.5f;

    [Header("Interaction")]
    public float interactionRadius = 3f;      // Ukuran awal kuas
    public float scrollSensitivity = 0.01f;    // Kecepatan membesar/mengecil
    public float minRadius = 0.1f;              // Batas terkecil
    public float maxRadius = 10f;             // Batas terbesar

    public float densityAmount = 100f;        // Intensitas asap
    public float velocityStrength = 10f;      // Intensitas dorongan
    
        
    [Header("Cursor Indicator")]
    public LineRenderer cursorLineRenderer; // Drag komponen LineRenderer ke sini
    public int circleResolution = 40;       // Semakin tinggi semakin halus lingkarannya

    public void SetGrid(FluidGrid grid)
    {
        this.grid = grid;
    }

    void Update()
    {
        if (grid == null) return;
        HandleInteraction();
        // Panggil fungsi ini setiap frame
        UpdateCursorIndicator();
    }

    void HandleInteraction()
    {
        // --- 1. LOGIKA SCROLL WHEEL ---
        // Input.mouseScrollDelta.y akan bernilai positif (up) atau negatif (down)
        float scroll = Input.mouseScrollDelta.y;
    
        if (scroll != 0)
        {
            interactionRadius += scroll * scrollSensitivity;
        
            // Clamp agar ukuran tidak menjadi negatif atau terlalu besar
            interactionRadius = Mathf.Clamp(interactionRadius, minRadius, maxRadius);
        
            // Optional: Print ke console untuk debug
            // Debug.Log("Brush Size: " + interactionRadius);
        }

        // --- 2. LOGIKA MOUSE POSITION ---
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; 
        Vector2 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
    
        // Kita konversi ke grid index (cx, cy)
        int cx = (int)(worldPos.x / grid.cellSize);
        int cy = (int)(worldPos.y / grid.cellSize);

        // --- 3. EKSEKUSI ---
        // Kirim 'interactionRadius' ke fungsi penambahan
        if (Input.GetMouseButton(0)) 
        {
            AddDensity(cx, cy, interactionRadius); // Klik Kiri: Tambah Asap
        }

        if (Input.GetMouseButton(1))
        {
            AddVelocity(cx, cy, interactionRadius); // Klik Kanan: Tambah Kecepatan
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

    // Fungsi menambahkan Density (Asap) dengan ukuran dinamis
    void AddDensity(int cx, int cy, float radius)
    {
        // Hitung berapa cell yang tercakup dalam radius ini
        int r = Mathf.CeilToInt(radius / grid.cellSize);

        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                // Cek batas grid agar tidak error
                if (x < 0 || x >= grid.width || y < 0 || y >= grid.height) continue;

                // Cek jarak agar bentuknya lingkaran (bukan kotak)
                float dist = Vector2.Distance(new Vector2(cx, cy), new Vector2(x, y));
                if (dist <= r)
                {
                    // Opsional: Semakin ke pinggir, intensitas semakin kecil (Smooth Brush)
                    // float falloff = 1f - (dist / r); 
                    // grid.density[grid.GetIndex(x, y)] += densityAmount * falloff;
                    
                    // Atau Flat Brush (Rata):
                    grid.density[grid.GetIndex(x, y)] += densityAmount;
                }
            }
        }
    }

    // Fungsi menambahkan Velocity (Dorongan) dengan ukuran dinamis
    void AddVelocity(int cx, int cy, float radius)
    {
        int r = Mathf.CeilToInt(radius / grid.cellSize);
        
        // Hitung arah gerak mouse (Velocity Mouse)
        // Untuk hasil terbaik, simpan posisi mouse frame sebelumnya (prevMousePos) di Update
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        Vector2 dir = new Vector2(mouseX, mouseY).normalized;

        for (int y = cy - r; y <= cy + r; y++)
        {
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 1 || x >= grid.width - 1 || y < 1 || y >= grid.height - 1) continue;

                float dist = Vector2.Distance(new Vector2(cx, cy), new Vector2(x, y));
                if (dist <= r)
                {
                    int idxU = grid.GetIndexU(x, y);
                    int idxV = grid.GetIndexV(x, y);

                    grid.VelocitiesX[idxU] += dir.x * velocityStrength;
                    grid.VelocitiesY[idxV] += dir.y * velocityStrength;
                }
            }
        }
    }
    
    void UpdateCursorIndicator()
    {
        if (cursorLineRenderer == null) return;

        // 1. Ambil posisi mouse di World Space
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Jarak dari kamera
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;  // Pastikan Z sejajar dengan grid (2D)

        // 2. Gambar lingkaran
        DrawCircle(worldPos, interactionRadius);
    }

    void DrawCircle(Vector3 center, float radius)
    {
        cursorLineRenderer.positionCount = circleResolution;

        float angleStep = 360f / circleResolution;
        
        for (int i = 0; i < circleResolution; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            Vector3 pos = center + new Vector3(x, y, 0);
            cursorLineRenderer.SetPosition(i, pos);
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