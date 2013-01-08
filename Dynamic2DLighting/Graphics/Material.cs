using System;
using System.Drawing;
using System.IO;
using System.Xml;
using NUnit.Framework;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D = Microsoft.DirectX.Direct3D;
using System.Windows.Forms;
using WarOfTheSeas.Helpers;

namespace WarOfTheSeas.Graphics
{
    /// <summary>
    /// Represents a material. Stores the usual material information using Direct3D.Material, 
    /// as well as diffuse, normal and height maps, used for various pixel shader effects.
    /// </summary>
    public class Material : IGraphicsResource
    {
        #region Variables
        public static readonly Color
            DefaultAmbientColor = Color.FromArgb(40, 40, 40),
            DefaultDiffuseColor = Color.FromArgb(210, 210, 210),
            DefaultSpecularColor = Color.FromArgb(255, 255, 255);

        public const float DefaultShininess = 24.0f;

        private Direct3D.Material d3dMaterial;

        private Texture diffuseMap = null;
        private Texture normalMap = null;
        private Texture heightMap = null;

        private Renderer renderer = null;
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the internal Direct3D.Material object used by WarOfTheSeas.Graphics.Material.
        /// </summary>
        public Direct3D.Material D3DMaterial
        {
            get
            {
                return d3dMaterial;
            }
            set
            {
                d3dMaterial = value;
            }
        }

        /// <summary>
        /// Gets and sets the ambient color.
        /// </summary>
        public Color Ambient
        {
            get
            {
                return d3dMaterial.Ambient;
            }
            set
            {
                d3dMaterial.Ambient = value;
            }
        }

        /// <summary>
        /// Gets and sets the diffuse color.
        /// </summary>
        public Color Diffuse
        {
            get
            {
                return d3dMaterial.Diffuse;
            }
            set
            {
                d3dMaterial.Diffuse = value;
            }
        }

        /// <summary>
        /// Gets and sets the specular color.
        /// </summary>
        public Color Specular
        {
            get
            {
                return d3dMaterial.Specular;
            }
            set
            {
                d3dMaterial.Specular = value;
            }
        }

        /// <summary>
        /// Gets and sets the shininess.
        /// </summary>
        public float Shininess
        {
            get
            {
                return d3dMaterial.SpecularSharpness;
            }
            set
            {
                d3dMaterial.SpecularSharpness = value;
            }
        }

        /// <summary>
        /// Does the diffuse map half alpha information?
        /// </summary>
        public bool HasAlpha
        {
            get
            {
                if (diffuseMap != null)
                    return diffuseMap.HasAlphaPixels;

                return false;
            }
        }

        /// <summary>
        /// Gets and sets the diffuse map.
        /// </summary>
        public Texture DiffuseMap
        {
            get
            {
                return diffuseMap;
            }
            set
            {
                diffuseMap = value;
            }
        }

        /// <summary>
        /// Gets and sets the normal map.
        /// </summary>
        public Texture NormalMap
        {
            get
            {
                return normalMap;
            }
            set
            {
                normalMap = value;
            }
        }

        /// <summary>
        /// Gets and sets the height map.
        /// </summary>
        public Texture HeightMap
        {
            get
            {
                return heightMap;
            }
            set
            {
                heightMap = value;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Builds the material using default parameters.
        /// </summary>
        public Material(Renderer renderer)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer",
                    "Cannot create a material with a null renderer reference.");

            this.renderer = renderer;

            d3dMaterial = new Direct3D.Material();
            d3dMaterial.Ambient = DefaultAmbientColor;
            d3dMaterial.Diffuse = DefaultDiffuseColor;
            d3dMaterial.Specular = DefaultSpecularColor;
            d3dMaterial.SpecularSharpness = DefaultShininess;
        }

        public Material(Renderer renderer, string diffuseMapFilename)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer",
                    "Cannot create a material with a null renderer reference.");

            this.renderer = renderer;

            d3dMaterial = new Direct3D.Material();
            d3dMaterial.Ambient = DefaultAmbientColor;
            d3dMaterial.Diffuse = DefaultDiffuseColor;
            d3dMaterial.Specular = DefaultSpecularColor;
            d3dMaterial.SpecularSharpness = DefaultShininess;

            diffuseMap = new Texture(this.renderer, diffuseMapFilename);
        }

        public Material(Renderer renderer, Direct3D.Material material,
            string diffuseMapFilename)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer",
                    "Cannot create a material with a null renderer reference.");

            this.renderer = renderer;
            d3dMaterial = material;
            diffuseMap = new Texture(renderer, diffuseMapFilename);
        }

        public Material(Renderer renderer, Color ambientColor, Color diffuseColor,
            string diffuseMapFilename)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer",
                    "Cannot create a material with a null renderer reference.");

            this.renderer = renderer;

            d3dMaterial = new Direct3D.Material();
            d3dMaterial.Ambient = ambientColor;
            d3dMaterial.Diffuse = diffuseColor;
            d3dMaterial.Specular = DefaultSpecularColor;
            d3dMaterial.SpecularSharpness = DefaultShininess;

            diffuseMap = new Texture(renderer, diffuseMapFilename);
        }
        #endregion

        #region Select without shaders
        /// <summary>
        /// Selects this material for Direct3D to use, and places the diffuse map
        /// on texture stage 0. Use this method when rendering without shaders.
        /// </summary>
        public void SelectWithoutShaders()
        {
            renderer.Device.Material = d3dMaterial;

            if (diffuseMap != null)
                diffuseMap.Select();
            else
                renderer.Device.SetTexture(0, null);
        }
        #endregion

        #region IGraphicsObject members
        public virtual void OnDeviceReset()
        {
        }

        public virtual void OnDeviceLost()
        {
        }

        public virtual void Dispose()
        {
        }
        #endregion

        #region Load from Xml file
        public static Material FromFile(Renderer renderer, string xmlFilename)
        {
            if (renderer == null)
                throw new ArgumentNullException("renderer",
                    "Cannot create a material with a null renderer reference.");

            if (String.IsNullOrEmpty(xmlFilename))
                throw new ArgumentNullException("xmlFilename",
                    "Cannot load a material without a valid filename.");

            if (!File.Exists(xmlFilename))
                throw new FileNotFoundException(xmlFilename);

            Material material = new Material(renderer);
            XmlTextReader reader = new XmlTextReader(xmlFilename);

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.LocalName == "Ambient")
                    {
                        int a = int.Parse(reader.GetAttribute(0));
                        int r = int.Parse(reader.GetAttribute(1));
                        int g = int.Parse(reader.GetAttribute(2));
                        int b = int.Parse(reader.GetAttribute(3));

                        material.Ambient = Color.FromArgb(a, r, g, b);
                    }
                    else if (reader.LocalName == "Diffuse")
                    {
                        int a = int.Parse(reader.GetAttribute(0));
                        int r = int.Parse(reader.GetAttribute(1));
                        int g = int.Parse(reader.GetAttribute(2));
                        int b = int.Parse(reader.GetAttribute(3));

                        material.Diffuse = Color.FromArgb(a, r, g, b);
                    }
                    else if (reader.LocalName == "Specular")
                    {
                        int a = int.Parse(reader.GetAttribute(0));
                        int r = int.Parse(reader.GetAttribute(1));
                        int g = int.Parse(reader.GetAttribute(2));
                        int b = int.Parse(reader.GetAttribute(3));

                        material.Specular = Color.FromArgb(a, r, g, b);
                    }
                    else if (reader.LocalName == "Shininess")
                    {
                        float shininess = float.Parse(reader.ReadString());
                        material.Shininess = shininess;
                    }
                    else if (reader.LocalName == "DiffuseMap")
                    {
                        string filename = reader.ReadString();
                        material.DiffuseMap = GlobalResourceCache.CreateTextureFromFile(renderer,
                            filename);
                    }
                    else if (reader.LocalName == "NormalMap")
                    {
                        string filename = reader.ReadString();
                        material.NormalMap = GlobalResourceCache.CreateTextureFromFile(renderer,
                            filename);
                    }
                    else if (reader.LocalName == "HeightMap")
                    {
                        string filename = reader.ReadString();
                        material.HeightMap = GlobalResourceCache.CreateTextureFromFile(renderer,
                            filename);
                    }
                }
            }

            return material;
        }
        #endregion
    }
}