using UnityEngine;

public class BlockHighlighter : MonoBehaviour
{
    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = new Color(0f, 0f, 0f, 0.8f);
    [SerializeField] private float lineWidth = 0.02f;
    [SerializeField] private Material lineMaterial;
    
    private LineRenderer lineRenderer;
    private bool isActive = false;
    private Vector3 blockPosition;
    private Vector3 blockSize = Vector3.one;
    
    private void Awake()
    {
        Debug.Log("BlockHighlighter Awake called");
        
        // Create line renderer if it doesn't exist
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.Log("No LineRenderer found - adding one");
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        
        // Configure the line renderer
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 24; // 12 lines, 2 points each
        
        // Create a material if none provided
        if (lineMaterial == null)
        {
            Debug.LogWarning("No line material assigned - creating a default one");
            // Try to find a suitable shader
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            if (shader == null)
            {
                Debug.LogError("Could not find a suitable shader for line renderer");
                shader = Shader.Find("Standard");
            }
            
            lineMaterial = new Material(shader);
            lineMaterial.color = highlightColor;
        }
        
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = highlightColor;
        lineRenderer.endColor = highlightColor;
        lineRenderer.enabled = false;
        
        Debug.Log("BlockHighlighter setup complete - line renderer configured");
    }
    
    public void ShowHighlight(Vector3 position)
    {
        Debug.Log($"ShowHighlight called at position {position}");
        blockPosition = new Vector3(
            Mathf.Floor(position.x) + 0.5f,
            Mathf.Floor(position.y) + 0.5f, 
            Mathf.Floor(position.z) + 0.5f
        );
        
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer is null in ShowHighlight!");
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                Debug.LogError("Still can't find LineRenderer - adding one now");
                lineRenderer = gameObject.AddComponent<LineRenderer>();
                // Configure the line renderer again
                lineRenderer.useWorldSpace = true;
                lineRenderer.startWidth = lineWidth;
                lineRenderer.endWidth = lineWidth;
                lineRenderer.positionCount = 24;
                lineRenderer.material = lineMaterial;
                lineRenderer.startColor = highlightColor;
                lineRenderer.endColor = highlightColor;
            }
        }
        
        UpdateLineRenderer();
        lineRenderer.enabled = true;
        isActive = true;
    }
    
    public void HideHighlight()
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        else
        {
            Debug.LogError("LineRenderer is null in HideHighlight!");
        }
        isActive = false;
    }
    
    private void UpdateLineRenderer()
    {
        if (!isActive) return;
        
        // Calculate the eight corners of the block
        Vector3 halfSize = blockSize / 2f;
        Vector3[] corners = new Vector3[8];
        
        corners[0] = blockPosition + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        corners[1] = blockPosition + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        corners[2] = blockPosition + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        corners[3] = blockPosition + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        
        corners[4] = blockPosition + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        corners[5] = blockPosition + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        corners[6] = blockPosition + new Vector3(halfSize.x, halfSize.y, halfSize.z);
        corners[7] = blockPosition + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        
        // Bottom face
        lineRenderer.SetPosition(0, corners[0]);
        lineRenderer.SetPosition(1, corners[1]);
        
        lineRenderer.SetPosition(2, corners[1]);
        lineRenderer.SetPosition(3, corners[2]);
        
        lineRenderer.SetPosition(4, corners[2]);
        lineRenderer.SetPosition(5, corners[3]);
        
        lineRenderer.SetPosition(6, corners[3]);
        lineRenderer.SetPosition(7, corners[0]);
        
        // Top face
        lineRenderer.SetPosition(8, corners[4]);
        lineRenderer.SetPosition(9, corners[5]);
        
        lineRenderer.SetPosition(10, corners[5]);
        lineRenderer.SetPosition(11, corners[6]);
        
        lineRenderer.SetPosition(12, corners[6]);
        lineRenderer.SetPosition(13, corners[7]);
        
        lineRenderer.SetPosition(14, corners[7]);
        lineRenderer.SetPosition(15, corners[4]);
        
        // Connecting edges
        lineRenderer.SetPosition(16, corners[0]);
        lineRenderer.SetPosition(17, corners[4]);
        
        lineRenderer.SetPosition(18, corners[1]);
        lineRenderer.SetPosition(19, corners[5]);
        
        lineRenderer.SetPosition(20, corners[2]);
        lineRenderer.SetPosition(21, corners[6]);
        
        lineRenderer.SetPosition(22, corners[3]);
        lineRenderer.SetPosition(23, corners[7]);
    }
} 