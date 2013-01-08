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

namespace WarOfTheSeas.Graphics
{
    /// <summary>
    /// Represents a point light positioned in the XY plane.
    /// </summary>
    public class Light
    {
        #region Variables
        /// <summary>
        /// A reference to the renderer.
        /// </summary>
        private Renderer renderer = null;

        /// <summary>
        /// The light's position in the XY plane.
        /// </summary>
        private Vector2 position = new Vector2();

        /// <summary>
        /// The attenuation map. This is produced procedurally upon construction of the light.
        /// </summary>
        private Texture attenuationMap = null;

        /// <summary>
        /// The range of the light. This represents the maximum distance an object can be from this light
        /// and still be lit by it.
        /// </summary>
        private float range = 0.0f;

        /// <summary>
        /// The color of light emmitted by this light source.
        /// </summary>
        private Color color = Color.White;

        /// <summary>
        /// A value in the range [0, 1] representing the intensity of this light.
        /// </summary>
        private float intensity = 1.0f;

        /// <summary>
        /// The geometry of the attenuation circle. Used to render the attenuation map.
        /// </summary>
        private Mesh attenuationCircle = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets and sets the light's position.
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
            }
        }

        /// <summary>
        /// Gets and sets the light color.
        /// </summary>
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
            }
        }

        /// <summary>
        /// Gets and sets the range of the light. This represents the maximum distance 
        /// an object can be from this light and still be lit by it. 
        /// 
        /// If this is modified, the attenuation map must be recreated. This is a potentially 
        /// costly operation.
        /// </summary>
        public float Range
        {
            get
            {
                return range;
            }
            set
            {
                range = value;

                attenuationMap.Dispose();
                attenuationMap = null;

                CreateAttenuationCircle();
                CreateAttenuationMap();
            }
        }

        /// <summary>
        /// Gets and sets the range of the light. This represents the maximum distance 
        /// an object can be from this light and still be lit by it.
        /// 
        /// If this is modified, the attenuation map must be recreated. This is a potentially 
        /// costly operation.
        /// </summary>
        public float Intensity
        {
            get
            {
                return intensity;
            }
            set
            {
                intensity = value;

                attenuationMap.Dispose();
                attenuationMap = null;

                CreateAttenuationCircle();
                CreateAttenuationMap();
            }
        }

        /// <summary>
        /// Gets the attenuation map.
        /// </summary>
        public Texture AttenuationMap
        {
            get
            {
                return attenuationMap;
            }
        }
        #endregion

        #region Constructor
        public Light(Renderer renderer, float range, float intensity)
        {
            if (renderer == null)
            {
                Log.Write("'renderer' is null.");
                throw new ArgumentNullException("renderer", "Can't create a Light with a null " +
                    "renderer reference.");
            }

            if (intensity < 0.0f || intensity > 1.0f)
            {
                Log.Write("'intensity' is out of range.");
                throw new ArgumentOutOfRangeException("intensity", intensity,
                    "'intensity' is outside of the range [0,1].");
            }

            this.renderer = renderer;
            this.range = range;
            this.intensity = intensity;

            // Allocate memory for the attenuation map
            attenuationMap = new Texture(renderer, renderer.FullscreenSize.Width,
                renderer.FullscreenSize.Height, true);

            CreateAttenuationCircle();
            CreateAttenuationMap();
        }

        public Light(Renderer renderer, float range, float intensity, Vector2 position, Color color)
        {
            if (renderer == null)
            {
                Log.Write("'renderer' is null.");
                throw new ArgumentNullException("renderer", "Can't create a Light with a null " +
                    "renderer reference.");
            }

            if (intensity < 0.0f || intensity > 1.0f)
            {
                Log.Write("'intensity' is out of range.");
                throw new ArgumentOutOfRangeException("intensity", intensity,
                    "'intensity' is outside of the range [0,1].");
            }

            this.renderer = renderer;
            this.range = range;
            this.intensity = intensity;
            this.position = position;
            this.color = Color;

            // Allocate memory for the attenuation map
            attenuationMap = new Texture(renderer, renderer.FullscreenSize.Width,
                renderer.FullscreenSize.Height, true);

            CreateAttenuationCircle();
            CreateAttenuationMap();
        }
        #endregion

        #region Create attenuation map
        private void CreateAttenuationCircle()
        {
            if (attenuationCircle != null)
                attenuationCircle.Dispose();

            attenuationCircle = Mesh.Circle(renderer, Color.FromArgb((int)(intensity * 255), 0, 0, 0),
                Color.FromArgb(0, 0, 0, 0), range, 32);
        }

        /// <summary>
        /// This method creates the attenuation map for the light based on its range.
        /// </summary>
        public void CreateAttenuationMap()
        {
            renderer.SaveRenderTarget();
            attenuationMap.SetAsRenderTarget();

            // Render the attenuation geometry to the attenuation map
            renderer.WorldMatrix = Matrix.Translation(new Vector3(-position.X, position.Y, 1.0f));

            renderer.Begin(null);

            attenuationCircle.Render();

            renderer.End();

            // Revert back to the original render target
            renderer.RestoreRenderTarget();
        }
        #endregion
    }
}