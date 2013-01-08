using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using WarOfTheSeas.Helpers;

namespace WarOfTheSeas.Graphics
{
    #region Helpers
    /// <summary>
    /// Stores the indices of a triple of vertices that together constitute a triangle of a Mesh.
    /// </summary>
    struct Triangle
    {
        public int v1, v2, v3;
    }

    /// <summary>
    /// Stores vertex data for a single edge of a Mesh.
    /// </summary>
    public class Edge
    {
        #region Variables
        private int v1Index = 0, v2Index = 0;
        private Vector2 v1 = new Vector2(), v2 = new Vector2();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the index of the first vertex comprising the edge.
        /// </summary>
        public int Vertex1Index
        {
            get
            {
                return v1Index;
            }
        }

        /// <summary>
        /// Gets the index of the second vertex comprising the edge.
        /// </summary>
        public int Vertex2Index
        {
            get
            {
                return v2Index;
            }
        }

        /// <summary>
        /// Gets the position of the first vertex comprising the edge.
        /// </summary>
        public Vector2 Vertex1Pos
        {
            get
            {
                return v1;
            }
        }

        /// <summary>
        /// Gets the position of the second vertex comprising the edge.
        /// </summary>
        public Vector2 Vertex2Pos
        {
            get
            {
                return v2;
            }
        }

        /// <summary>
        /// Gets the vector normal to the edge.
        /// </summary>
        public Vector2 Normal
        {
            get
            {
                return new Vector2(v2.Y - v1.Y, v1.X - v2.X);
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Builds an Edge given two vertex indices.
        /// </summary>
        public Edge(int v1Index, int v2Index, Vector2 v1, Vector2 v2)
        {
            this.v1Index = v1Index;
            this.v2Index = v2Index;
            this.v1 = v1;
            this.v2 = v2;
        }
        #endregion
    }

    /// <summary>
    /// Specifies how triangles are rendered by a Mesh.
    /// </summary>
    public enum TriRenderingMode
    {
        /// <summary>
        /// This is the most general rendering mode. The triangles to be rendered are specified 
        /// explicitly using the Mesh.AddTriangle() method.
        /// </summary>
        TriangleList,
        /// <summary>
        /// Triangles are rendered as a strip.
        /// </summary>
        TriangleStrip
    }
    #endregion

    /// <summary>
    /// Represents a 2D mesh. Stores vertex, triangle, and index data, and also provides
    /// functionality for rendering the mesh.
    /// </summary>
    public class Mesh : IGraphicsResource, IRenderable
    {
        #region Variables
        Renderer renderer = null;

        private CustomVertex.PositionColoredTextured[] vertices = null;
        private Triangle[] triangles = null;

        private int numVertices = 0;
        private int numTriangles = 0;

        private bool isVertexBufferUpdated = false;
        private bool isIndexBufferUpdated = false;

        VertexBuffer vertexBuffer = null;
        IndexBuffer indexBuffer = null;

        TriRenderingMode triRenderingMode = TriRenderingMode.TriangleList;
        #endregion

        #region Properties
        /// <summary>
        /// Returns the vertex list.
        /// </summary>
        public CustomVertex.PositionColoredTextured[] Vertices
        {
            get
            {
                return vertices;
            }
        }

        /// <summary>
        /// Gets the number of vertices in the mesh.
        /// </summary>
        public int NumVertices
        {
            get
            {
                return numVertices;
            }
        }

        /// <summary>
        /// Gets the number of triangles in the mesh.
        /// </summary>
        public int NumTriangles
        {
            get
            {
                return numTriangles;
            }
        }

        /// <summary>
        /// Gets the number of edges in the mesh.
        /// </summary>
        public int NumEdges
        {
            get
            {
                // The number of edges is the same as the number of vertices.
                return numVertices;
            }
        }

        /// <summary>
        /// Gets and sets the triangle rendering mode.
        /// </summary>
        public TriRenderingMode TriRenderingMode
        {
            get
            {
                return triRenderingMode;
            }
            set
            {
                triRenderingMode = value;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Creates the mesh and allocates all the memory it will need.
        /// </summary>
        public Mesh(Renderer renderer, int numVertices, int numTriangles)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer", "Can't create mesh with an invalid renderer.");

            if (numVertices <= 0)
                throw new ArgumentOutOfRangeException("numVertices", numVertices, "Can't create mesh with zero or fewer " +
                    "vertices.");

            if (numTriangles <= 0)
                throw new ArgumentOutOfRangeException("numTriangles", numTriangles, "Can't create mesh with zero or fewer " +
                    "triangles.");

            this.renderer = renderer;
            this.numVertices = numVertices;
            this.numTriangles = numTriangles;

            vertices = new CustomVertex.PositionColoredTextured[numVertices];
            if (vertices == null)
                throw new OutOfMemoryException("Unable to allocate vertex array for Mesh.");

            triangles = new Triangle[numTriangles];
            if (triangles == null)
                throw new OutOfMemoryException("Unable to allocate triangle array for Mesh.");

            vertexBuffer = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), numVertices,
                renderer.Device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColoredTextured.Format,
                Pool.Default);

            if (vertexBuffer == null)
                throw new DirectXException("Unable to create vertex buffer for Mesh.");

            indexBuffer = new IndexBuffer(typeof(short), 3 * numTriangles, renderer.Device,
                Usage.WriteOnly, Pool.Default);

            if (indexBuffer == null)
                throw new Direct3DXException("Unable to create index buffer for Mesh.");

            renderer.AddGraphicsObject(this);
        }
        #endregion

        #region IGraphicsObject members
        public void Dispose()
        {
            if (vertexBuffer != null)
                vertexBuffer.Dispose();
            vertexBuffer = null;

            if (indexBuffer != null)
                indexBuffer.Dispose();
            indexBuffer = null;
        }

        public void OnDeviceLost()
        {
            Dispose();
        }

        public void OnDeviceReset()
        {
            // Recreate buffers
            vertexBuffer = new VertexBuffer(typeof(CustomVertex.PositionColoredTextured), numVertices,
                renderer.Device, Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionColoredTextured.Format,
                Pool.Default);

            if (vertexBuffer == null)
                throw new DirectXException("Unable to create vertex buffer for Mesh.");

            indexBuffer = new IndexBuffer(typeof(short), 3 * numTriangles, renderer.Device,
                Usage.WriteOnly, Pool.Default);

            if (indexBuffer == null)
                throw new Direct3DXException("Unable to create index buffer for Mesh.");

            // Flag the buffers as unupdated so they're refilled with data when Render() is called again.
            isVertexBufferUpdated = false;
            isIndexBufferUpdated = false;
        }
        #endregion

        #region Methods for building a Mesh
        /// <summary>
        /// Adds a vertex to the mesh.
        /// </summary>
        public void AddVertex(int index, Vector3 position, Color color, Vector2 texCoords)
        {
            if (index >= numVertices)
            {
                Log.Write(
                    "'index' is greater than or equal to the maximum number of vertices allowed for this Mesh." +
                    " Max allowed vertices: " + numVertices.ToString() + " Actual value: " + index.ToString());
                throw new ArgumentOutOfRangeException("index", index, "'index' is greater than or "
                    + "equal to the maximum number of vertices allowed for this Mesh.");
            }

            vertices[index] = new CustomVertex.PositionColoredTextured(position, color.ToArgb(),
                texCoords.X, texCoords.Y);

            isVertexBufferUpdated = false;
        }

        /// <summary>
        /// Informs the Mesh that a certain triple of vertices are to be treated together as a triangle.
        /// </summary>
        /// <param name="index">The index of this triangle in the triangle list.</param>
        /// <param name="v1">The index of the first vertex making up this triangle.</param>
        /// <param name="v2">The index of the second vertex making up this triangle.</param>
        /// <param name="v3">The index of the third vertex making up this triangle.</param>
        public void AddTriangle(int index, int v1, int v2, int v3)
        {
            if (index >= numTriangles)
            {
                Log.Write(
                    "'index' is greater than or equal to the maximum number of triangles allowed for this Mesh." +
                    " Max allowed triangles: " + numTriangles.ToString() + " Actual value: " + index.ToString());
                throw new ArgumentOutOfRangeException("index", index, "'index' is greater than or "
                    + "equal to the maximum number of triangles allowed for this Mesh.");
            }

            triangles[index].v1 = v1;
            triangles[index].v2 = v2;
            triangles[index].v3 = v3;

            isIndexBufferUpdated = false;
        }
        #endregion

        #region Rendering
        /// <summary>
        /// Renders the mesh.
        /// </summary>
        public void Render()
        {
            // Update the buffers if they aren't already up-to-date
            if (!isVertexBufferUpdated)
            {
                vertexBuffer.SetData(vertices, 0, LockFlags.None);
                isVertexBufferUpdated = true;
            }

            if (!isIndexBufferUpdated)
            {
                short[] indices = new short[3 * numTriangles];

                for (int i = 0; i < numTriangles; ++i)
                {
                    indices[i * 3 + 0] = (short)triangles[i].v1;
                    indices[i * 3 + 1] = (short)triangles[i].v2;
                    indices[i * 3 + 2] = (short)triangles[i].v3;
                }

                indexBuffer.SetData(indices, 0, LockFlags.None);

                isIndexBufferUpdated = true;
            }

            // Render the mesh
            renderer.Device.VertexFormat = CustomVertex.PositionColoredTextured.Format;
            renderer.Device.SetStreamSource(0, vertexBuffer, 0);
            renderer.Device.Indices = indexBuffer;

            switch (triRenderingMode)
            {
                case TriRenderingMode.TriangleList:
                    renderer.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices,
                        0, numTriangles);
                    break;

                case TriRenderingMode.TriangleStrip:
                    renderer.Device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, numTriangles);
                    break;
            }
        }
        #endregion

        #region Methods to create common meshes
        /// <summary>
        /// Builds a rectangular mesh, centered around the origin.
        /// </summary>
        public static Mesh Rectangle(Renderer renderer, Color color, float width, float height)
        {
            return Mesh.Rectangle(renderer, color, width, height, 1.0f);
        }

        /// <summary>
        /// Builds a rectangular mesh, centered around the origin.
        /// </summary>
        public static Mesh Rectangle(Renderer renderer, Color color, float width, float height,
            float textureMapTiling)
        {
            Mesh rectMesh = new Mesh(renderer, 4, 2);

            rectMesh.AddVertex(0, new Vector3(-width / 2, height / 2, 1.0f), color, 
                new Vector2(0.0f, 0.0f));
            rectMesh.AddVertex(1, new Vector3(width / 2, height / 2, 1.0f), color, 
                new Vector2(textureMapTiling, 0.0f));
            rectMesh.AddVertex(2, new Vector3(width / 2, -height / 2, 1.0f), color, 
                new Vector2(textureMapTiling, textureMapTiling));
            rectMesh.AddVertex(3, new Vector3(-width / 2, -height / 2, 1.0f), color, 
                new Vector2(0.0f, textureMapTiling));

            rectMesh.AddTriangle(0, 0, 1, 2);
            rectMesh.AddTriangle(1, 0, 2, 3);

            return rectMesh;
        }

        /// <summary>
        /// Builds a circular mesh centered around the origin.
        /// </summary>
        public static Mesh Circle(Renderer renderer, Color centerColor, Color rimColor, float radius,
            int numSubdivisions)
        {
            Mesh circleMesh = new Mesh(renderer, numSubdivisions, numSubdivisions - 2);

            float angleStep = (2 * (float)Math.PI) / numSubdivisions;
            for (int i = 0; i < numSubdivisions; ++i)
            {
                circleMesh.AddVertex(i, new Vector3(radius * (float)Math.Cos(angleStep * i),
                    radius * (float)Math.Sin(angleStep * i), 1.0f), rimColor, new Vector2());
            }

            for (int i = 2, count = 0; i < numSubdivisions - 1; ++i, ++count)
            {
                circleMesh.AddTriangle(count, 0, i, i - 1);
            }

            circleMesh.AddTriangle(numSubdivisions - 3, 0, numSubdivisions - 2, numSubdivisions - 1);

            return circleMesh;
        }
        #endregion

        #region Helper methods
        /// <summary>
        /// Gets an edge of the mesh. For N less than NumVertices - 1, the Nth edge consists of vertex
        /// indices {N, N+1}. If N == NumVertices - 1, then the Nth edge is {N, 0}. Thus the number of edges
        /// is the same as the number of vertices.
        /// </summary>
        public Edge GetEdge(int index)
        {
            if (index < 0 || index >= numVertices)
            {
                Log.Write("In Mesh.GetEdge(), 'index' is out of the acceptable range [0, NumVertices - 1].");
                throw new ArgumentOutOfRangeException("index", index, "In Mesh.GetEdge(), 'index' is out of" +
                    " the acceptable range [0, NumVertices - 1].");
            }

            if (index == numVertices - 1)
                return new Edge(index, 0, GetVertexPosition(index), GetVertexPosition(0));

            return new Edge(index, index + 1, GetVertexPosition(index), GetVertexPosition(index + 1));
        }

        /// <summary>
        /// Gets the position of the Nth vertex in model space.
        /// </summary>
        public Vector2 GetVertexPosition(int index)
        {
            if (index < 0 || index >= numVertices)
            {
                Log.Write("In Mesh.GetVertexPosition(), 'index'"
                    + " is out of the acceptable range [0, NumVertices - 1].");
                throw new ArgumentOutOfRangeException("index", index, "In Mesh.GetVertexPosition(), 'index'"
                    + " is out of the acceptable range [0, NumVertices - 1].");
            }

            return new Vector2(vertices[index].Position.X, vertices[index].Position.Y);
        }
        #endregion
    }
}