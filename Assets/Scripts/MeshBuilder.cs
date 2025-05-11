using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder {
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<float> textureIDs = new List<float>(); // UV1 for texture index

    private int faceCount = 0;

    // Cube face vertex layout (clockwise)
    private static readonly Vector3[,] faceVertices = new Vector3[6, 4] {
        { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) }, // Front
        { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0) }, // Back
        { new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0), new Vector3(0, 1, 0) }, // Top
        { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) }, // Bottom
        { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0) }, // Left
        { new Vector3(1, 0, 1), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1) }, // Right
    };

    // Direction vectors for each face
    private static readonly Vector3[] faceDirections = new Vector3[6] {
        new Vector3(0, 0, 1),  // Front (Z+)
        new Vector3(0, 0, -1), // Back (Z-)
        new Vector3(0, 1, 0),  // Top (Y+)
        new Vector3(0, -1, 0), // Bottom (Y-)
        new Vector3(-1, 0, 0), // Left (X-)
        new Vector3(1, 0, 0)   // Right (X+)
    };

    // Simple square UVs (0–1 range)
    private static readonly Vector2[] defaultUVs = new Vector2[4] {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
    };

    /// <summary>
    /// Returns the direction vector for a given face index.
    /// </summary>
    /// <param name="faceIndex">Face index (0-5)</param>
    /// <returns>Direction vector for the face</returns>
    public static Vector3 FaceDirection(int faceIndex) {
        return faceDirections[faceIndex];
    }

    /// <summary>
    /// Adds a face to the mesh with a specific texture index.
    /// </summary>
    /// <param name="blockPos">Block world position</param>
    /// <param name="faceDir">Face index (0–5)</param>
    /// <param name="textureID">Index into Texture2DArray</param>
    public void AddFace(Vector3 blockPos, int faceDir, int textureID) {
        for (int i = 0; i < 4; i++) {
            vertices.Add(blockPos + faceVertices[faceDir, i]);
            uvs.Add(defaultUVs[i]);
            textureIDs.Add(textureID); // store texture index per vertex
        }

        // Two triangles per quad
        triangles.Add(faceCount * 4 + 0);
        triangles.Add(faceCount * 4 + 1);
        triangles.Add(faceCount * 4 + 2);

        triangles.Add(faceCount * 4 + 0);
        triangles.Add(faceCount * 4 + 2);
        triangles.Add(faceCount * 4 + 3);

        faceCount++;
    }

    /// <summary>
    /// Builds the mesh from all added faces.
    /// </summary>
    public Mesh Build() {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);       // Base UVs
        mesh.SetUVs(1, textureIDs.ConvertAll(id => new Vector2(id, 0))); // Texture ID as UV1.x
        mesh.RecalculateNormals();

        return mesh;
    }

    public void Clear() {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        textureIDs.Clear();
        faceCount = 0;
    }
}