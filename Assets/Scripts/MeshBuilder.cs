using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder {
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();

    // Cube face directions
    private static Vector3[] faceDirections = {
        Vector3.forward,  // Front
        Vector3.back,     // Back
        Vector3.left,     // Left
        Vector3.right,    // Right
        Vector3.up,       // Top
        Vector3.down      // Bottom
    };

    // Vertex positions for one face (quad)
    private static Vector3[,] faceVertices = {
        // Front
        { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) },
        // Back
        { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) },
        // Left
        { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0) },
        // Right
        { new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1) },
        // Top
        { new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) },
        // Bottom
        { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) }
    };

    public void AddCube(Vector3 position) {
        for (int face = 0; face < 6; face++) {
            int vertexIndex = vertices.Count;

            // Add the 4 vertices for this face
            for (int i = 0; i < 4; i++) {
                vertices.Add(position + faceVertices[face, i]);
            }

            // Add two triangles (0, 1, 2) and (2, 3, 0)
            triangles.Add(vertexIndex + 0);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);
            triangles.Add(vertexIndex + 0);
        }
    }

    public Mesh Build() {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // if mesh gets large
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}