using System;
using WarOfTheSeas.Helpers;
using Microsoft.DirectX;

namespace WarOfTheSeas.Graphics
{
    /// <summary>
    /// Represents a local transform in the scene graph hierarchy.
    /// </summary>
    public class GeometryNode : SceneGraphNode
    {
        #region Variables
        private Matrix localTransform = Matrix.Identity;
        private IRenderable renderObject = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets and sets the local transform matrix.
        /// </summary>
        public Matrix LocalTransform
        {
            get
            {
                return localTransform;
            }
            set
            {
                localTransform = value;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of GeometryNode.
        /// </summary>
        public GeometryNode(Renderer renderer, SceneGraph sceneGraph) 
            : base(renderer, sceneGraph)
        {
        }

        /// <summary>
        /// Initializes a new instance of GeometryNode.
        /// </summary>
        public GeometryNode(Renderer renderer, SceneGraph sceneGraph, Matrix localTransform,
            IRenderable renderObject)
            : base(renderer, sceneGraph)
        {
            this.localTransform = localTransform;
            this.renderObject = renderObject;
        }
        #endregion

        #region Update
        /// <summary>
        /// Pushes the local transform onto the matrix stack, renders this node, 
        /// updates all the children of this node in the scene graph, and then 
        /// pops this local transform.
        /// </summary>
        public override void Update()
        {
            sceneGraph.MatrixStack.Push(localTransform);
            renderer.WorldMatrix = sceneGraph.MatrixStack.CompositeTransform;
            renderer.CurrentEffect.SetValue("world", renderer.WorldMatrix);
            renderer.CurrentEffect.SetValue("worldViewProj", renderer.WorldViewProjectionMatrix);

            renderer.SetPass(2);
            renderObject.Render();
            base.Update();

            sceneGraph.MatrixStack.Pop();
        }
        #endregion
    }
}