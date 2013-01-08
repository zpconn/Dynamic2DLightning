using System;
using Microsoft.DirectX;

namespace WarOfTheSeas.Graphics
{
    /// <summary>
    /// A packet of information dispatched to the renderer. Used to separate rendering from the scene graph.
    /// </summary>
    public class RenderPacket
    {
        /// <summary>
        /// The IRenderable object to be rendered.
        /// </summary>
        public IRenderable RenderObject = null;

        /// <summary>
        /// The name of the material with which RenderObject is to be rendered.
        /// </summary>
        public string MaterialName = "NullMaterial";

        /// <summary>
        /// The local world transform for RenderObject.
        /// </summary>
        public Matrix LocalTransform = Matrix.Identity;
    }
}