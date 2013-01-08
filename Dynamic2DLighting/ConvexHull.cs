using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Microsoft.DirectX;
using Direct3D = Microsoft.DirectX.Direct3D;
using WarOfTheSeas.Graphics;
using WarOfTheSeas.Helpers;
using WarOfTheSeas.Input;
using Graphics = WarOfTheSeas.Graphics;

namespace Dynamic2DLighting
{
    /// <summary>
    /// Represents a convex polygon. Stores world transform information, uses a Mesh for rendering,
    /// and provides functionality for rendering 2D hard-edged shadows given a light position.
    /// </summary>
    public class ConvexHull : IRenderable
    {
        #region Variables
        private Renderer renderer = null;
        private Effect effect = null;

        private Mesh polygonGeometry = null;

        private Vector2 position = new Vector2();
        private float rotation = 0.0f;
        private float size = 1.0f;

        private Matrix worldMatrix = Matrix.Identity;
        #endregion

        #region Properties
        /// <summary>
        /// Gets and sets the position.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                CalculateWorldMatrix();
            }
        }

        /// <summary>
        /// Gets and sets the rotation, in radians.
        /// </summary>
        public float Rotation
        {
            get
            {
                return rotation;
            }
            set
            {
                rotation = value;
                CalculateWorldMatrix();
            }
        }

        /// <summary>
        /// Gets and sets the size scaling factor.
        /// </summary>
        public float Size
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
                CalculateWorldMatrix();
            }
        }
        #endregion

        #region Constructor
        public ConvexHull(Renderer renderer, Mesh polygonGeometry, Vector2 position,
            float rotation, float size)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer", "Can't create a ConvexHull with a null renderer.");

            if (polygonGeometry == null)
                throw new ArgumentNullException("polygonGeometry", "Can't create a ConvexHull without a valid " +
                    "mesh.");

            this.renderer = renderer;
            this.polygonGeometry = polygonGeometry;
            this.position = position;
            this.rotation = rotation;
            this.size = size;

            effect = GlobalResourceCache.CreateEffectFromFile(renderer,
                "Effect Files\\Dynamic2DLightingEffect.fx");

            CalculateWorldMatrix();
        }

        public ConvexHull(Renderer renderer, Mesh polygonGeometry)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer", "Can't create a ConvexHull with a null renderer.");

            if (polygonGeometry == null)
                throw new ArgumentNullException("polygonGeometry", "Can't create a ConvexHull without a valid " +
                    "mesh.");

            this.renderer = renderer;
            this.polygonGeometry = polygonGeometry;

            effect = GlobalResourceCache.CreateEffectFromFile(renderer,
                "Effect Files\\Dynamic2DLightingEffect.fx");
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Used for shadow rendering.
        /// </summary>
        private class Quad
        {
            public Direct3D.CustomVertex.PositionColoredTextured[] Vertices =
                new Microsoft.DirectX.Direct3D.CustomVertex.PositionColoredTextured[4];
        }

        /// <summary>
        /// Calculates the world matrix based on transform information set by the user.
        /// </summary>
        private void CalculateWorldMatrix()
        {
            worldMatrix = Matrix.Scaling(new Vector3(size, size, 1.0f)) * Matrix.RotationZ(rotation) *
                Matrix.Translation(position.X, position.Y, 0.0f);
        }

        /// <summary>
        /// Calculates the approximate center of the mesh's polygonal geometry by taking the
        /// mean average of the vertices.
        /// </summary>
        private Vector2 CalculateMeshCenter()
        {
            Vector3 sum = new Vector3();

            foreach (Direct3D.CustomVertex.PositionColoredTextured vertex in polygonGeometry.Vertices)
            {
                sum += vertex.Position;
            }

            Vector3 center = sum * (1.0f / (float)polygonGeometry.NumVertices);
            return new Vector2(center.X, center.Y);
        }
        #endregion

        #region Rendering
        /// <summary>
        /// Renders the polygonal mesh constituting the ConvexHull's visual representation. Use
        /// the method RenderShadow() to render dynamic 2D shadows cast by the ConvexHull's
        /// geometry.
        /// </summary>
        public void Render()
        {
            renderer.WorldMatrix = worldMatrix;
            effect.SetValue("world", renderer.WorldMatrix);
            effect.SetValue("worldViewProj", renderer.WorldMatrix * renderer.ViewMatrix *
                renderer.ProjectionMatrix);
            renderer.SetPass(0);
            polygonGeometry.Render();
        }

        /// <summary>
        /// Renders the 2D hard-edged shadow that would be cast from this ConvexHull's polygonal
        /// geometry by a light positioned at lightPosWS.
        /// </summary>
        /// <param name="lightPosWS">The position of the light in world coordinates.</param>
        public void RenderShadow(Vector2 lightPosWS)
        {
            Vector3 UVOffset = new Vector3(0.0f, -0.5f, 0.0f);

            // Transform the light position into model space
            Vector2 lightPos = Vector2.TransformCoordinate(lightPosWS, Matrix.Invert(worldMatrix));

            List<Edge> contourEdges = new List<Edge>();

            for (int edgeIndex = 0; edgeIndex < polygonGeometry.NumEdges; ++edgeIndex)
            {
                Edge edge = polygonGeometry.GetEdge(edgeIndex);
                Vector2 edgeCenter = (edge.Vertex1Pos + edge.Vertex2Pos) * 0.5f;
                Vector2 incidentLightDir = edgeCenter - lightPos;

                // If the edge faces away from the light source
                if (Vector2.Dot(incidentLightDir, edge.Normal) >= 0.0f)
                {
                    contourEdges.Add(edge);
                }
            }

            if (contourEdges.Count < 1 || contourEdges.Count == polygonGeometry.NumEdges)
            {
                return;
            }

            const float ExtrudeMagnitude = 1000.0f;

            List<Quad> quads = new List<Quad>();

            int quadIndex = 0;
            foreach (Edge edge in contourEdges)
            {
                Vector3 lightPosVec3 = new Vector3(lightPos.X, lightPos.Y, 1.0f);

                Vector3 vertex1 = new Vector3(
                    edge.Vertex1Pos.X, edge.Vertex1Pos.Y, 1.0f);
                Vector3 vertex2 = new Vector3(
                    edge.Vertex2Pos.X, edge.Vertex2Pos.Y, 1.0f);

                // Transform the position data from model space to world space
                vertex1.TransformCoordinate(worldMatrix);
                vertex2.TransformCoordinate(worldMatrix);
                lightPosVec3.TransformCoordinate(worldMatrix);

                Quad quad = new Quad();
                Color shadowColor = Color.FromArgb((int)(1 * 255.0f), 0, 0, 0);

                quad.Vertices[2 * quadIndex + 0].Position = vertex1 + UVOffset;
                quad.Vertices[2 * quadIndex + 0].Color = shadowColor.ToArgb();

                quad.Vertices[2 * quadIndex + 1].Position = vertex1 + ExtrudeMagnitude * (vertex1 - lightPosVec3)
                    + UVOffset;
                quad.Vertices[2 * quadIndex + 1].Color = shadowColor.ToArgb();

                quad.Vertices[2 * quadIndex + 2].Position = vertex2 + UVOffset;
                quad.Vertices[2 * quadIndex + 2].Color = shadowColor.ToArgb();

                quad.Vertices[2 * quadIndex + 3].Position = vertex2 + ExtrudeMagnitude * (vertex2 - lightPosVec3)
                    + UVOffset;
                quad.Vertices[2 * quadIndex + 3].Color = shadowColor.ToArgb();

                quads.Add(quad);
            }

            renderer.Begin(effect);

            renderer.WorldMatrix = Matrix.Identity;
            effect.SetValue("world", renderer.WorldMatrix);
            effect.SetValue("worldViewProj", renderer.WorldViewProjectionMatrix);
            renderer.SetPass(3);
            renderer.Device.VertexFormat = Direct3D.CustomVertex.PositionColoredTextured.Format;

            foreach (Quad quad in quads)
            {
                renderer.Device.DrawUserPrimitives(Direct3D.PrimitiveType.TriangleStrip, 2, quad.Vertices);
            }

            renderer.End();
        }
        #endregion
    }
}