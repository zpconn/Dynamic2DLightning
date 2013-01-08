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
    #region Old code
    public partial class Dynamic2DLightingForm : GraphicsForm
    {
        private Config config = new Config("Config.txt");

        private Material material = null;
        private Effect effect = null;
        private Mesh rectMesh = null;
        private MouseDevice mouse = null;
        private Light light = null;
        private Mesh lightMesh = null;

        private Texture sceneImage = null;
        private BloomPostProcessor bloomPostProcessor = null;

        private Vector3 pos = new Vector3();

        ConvexHull poly1 = null;
        ConvexHull poly2 = null;
        ConvexHull poly3 = null;

        private float angle = 0.0f;

        private Vector2 scrollVec = new Vector2();

        private const float AngularVelocity = 0.1f * (float)Math.PI;
        private const float MovementRadius = 1000.0f;

        public Config Config
        {
            get
            {
                return config;
            }
        }

        public Dynamic2DLightingForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes the game. Loads all resources.
        /// </summary>
        protected override void InitializeGame()
        {
            Cursor.Hide();

            mouse = new MouseDevice(this);

            renderer.ProjectionMode = ProjectionMode.Orthogonal;
            renderer.ViewMatrix = Matrix.LookAtLH(new Vector3(0, 0, 5.0f), new Vector3(),
                new Vector3(0, 1, 0));

            effect = GlobalResourceCache.CreateEffectFromFile(renderer,
                "Effect Files\\Dynamic2DLightingEffect.fx");

            rectMesh = Mesh.Rectangle(renderer, Color.White, renderer.FullscreenSize.Width,
                renderer.FullscreenSize.Height, 1.0f);

            material = GlobalResourceCache.CreateMaterialFromFile(renderer, "Materials\\roughWallMaterial.xml");

            light = new Light(renderer, 350, 1.0f, new Vector2(), Color.Red);

            sceneImage = new Texture(renderer, renderer.FullscreenSize.Width, renderer.FullscreenSize.Height,
                true);

            lightMesh = Mesh.Circle(renderer, Color.Yellow, Color.Yellow, 6, 16);

            bloomPostProcessor = new BloomPostProcessor(renderer);
            bloomPostProcessor.Blur = 3.5f;
            bloomPostProcessor.BloomScale = 1.5f;
            bloomPostProcessor.BrightPassThreshold = 0.4f;

            poly1 = new ConvexHull(renderer, Mesh.Circle(renderer, Color.Blue, Color.Blue, 65, 8));
            poly1.Position = new Vector2(-150.0f, 150.0f);

            poly2 = new ConvexHull(renderer, Mesh.Circle(renderer, Color.Red, Color.Purple, 50, 4));
            poly2.Position = new Vector2(200.0f, 0.0f);

            poly3 = new ConvexHull(renderer, Mesh.Circle(renderer, Color.SaddleBrown, Color.SeaGreen,
                60, 32));
            poly3.Position = new Vector2(-250.0f, -200.0f);

            this.KeyDown += new KeyEventHandler(OnKeyDown);
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                running = false;
        }

        /// <summary>
        /// Updates the game environment.
        /// </summary>
        protected override void UpdateEnvironment()
        {
            this.Text = Properties.Settings.Default.WindowTitle + " | FPS: " +
                timer.FramesPerSecond.ToString();

            renderer.ViewMatrix = Matrix.LookAtLH(new Vector3(0, 0, 5.0f),
                new Vector3(0, 0, 0.0f),
                new Vector3(0, 1, 0));

            mouse.Update();

            angle += AngularVelocity * timer.MoveFactorPerSecond;

            float inverseWidth = 1.0f / (float)renderer.FullscreenSize.Width;
            float inverseHeight = 1.0f / (float)renderer.FullscreenSize.Height;

            //scrollVec.X += mouse.MovementVector.X * timer.MoveFactorPerSecond * 1000 * inverseWidth;
            //scrollVec.Y -= mouse.MovementVector.Y * timer.MoveFactorPerSecond * 1000 * inverseHeight;

            //pos += mouse.MovementVector * timer.MoveFactorPerSecond * 1000;

            //light.Position = new Vector2(pos.X, pos.Y);

            light.Position = new Vector2(light.Position.X + mouse.MovementVector.X * timer.MoveFactorPerSecond * 500,
                light.Position.Y + mouse.MovementVector.Y * timer.MoveFactorPerSecond * 500);
        }

        /// <summary>
        /// Renders the game environment.
        /// </summary>
        protected override void Render3DEnvironment()
        {
            #region Normal scene
            renderer.Begin(effect);

            renderer.WorldMatrix = Matrix.Translation(pos);
            renderer.ViewMatrix = Matrix.LookAtLH(new Vector3(pos.X, pos.Y, 5.0f),
                new Vector3(pos.X, pos.Y, 0.0f),
                new Vector3(0, 1, 0));

            effect.SetValue("world", renderer.WorldMatrix);
            effect.SetValue("worldViewProj", renderer.WorldMatrix * renderer.ViewMatrix *
                renderer.ProjectionMatrix);
            effect.SetValue("worldRot", Matrix.Identity);
            effect.SetValue("lightPos", new Vector4(light.Position.X, light.Position.Y, 1.0f, 1.0f));
            effect.SetValue("eyePos", new Vector4(0, 0, 5.0f, 1.0f));
            effect.SetValue("range", light.Range);
            effect.SetValue("shininess", material.Shininess);
            effect.SetValue("ambient", 0.15f);
            effect.SetValue("heightMapScale", 0.02f);

            effect.SetValue("scrollVec", new Vector4(scrollVec.X, scrollVec.Y, 0.0f, 1.0f));

            renderer.BindTexture("diffuseMap", material.DiffuseMap);
            renderer.BindTexture("normalMap", material.NormalMap);

            renderer.SaveRenderTarget();
            sceneImage.SetAsRenderTarget();
            renderer.Device.Clear(Direct3D.ClearFlags.Target | Direct3D.ClearFlags.ZBuffer, Color.Black,
                1.0f, 0);

            renderer.SetPass(1);
            rectMesh.Render();

            poly1.Render();
            poly2.Render();
            poly3.Render();

            //renderer.RestoreRenderTarget();

            renderer.End();
            #endregion

            #region Shadow
            // Blooms flops the x-axis, so we must negate the x-components.

            //poly1.Position = new Vector2(-poly1.Position.X, poly1.Position.Y);
            poly1.RenderShadow(new Vector2(light.Position.X, light.Position.Y));
            //poly1.Position = new Vector2(-poly1.Position.X, poly1.Position.Y);

            //poly2.Position = new Vector2(-poly2.Position.X, poly2.Position.Y);
            poly2.RenderShadow(new Vector2(light.Position.X, light.Position.Y));
            //poly2.Position = new Vector2(-poly2.Position.X, poly2.Position.Y);

            //poly3.Position = new Vector2(-poly3.Position.X, poly3.Position.Y);
            poly3.RenderShadow(new Vector2(light.Position.X, light.Position.Y));
            //poly3.Position = new Vector2(-poly3.Position.X, poly3.Position.Y);
            #endregion

            renderer.RestoreRenderTarget();
            renderer.WorldMatrix = Matrix.Identity;
            renderer.ViewMatrix = Matrix.Identity;
            bloomPostProcessor.SceneImage = sceneImage;
            bloomPostProcessor.Render();

            #region Light mesh
            renderer.WorldMatrix = Matrix.Translation(new Vector3(light.Position.X, light.Position.Y, 1.0f));
            renderer.ViewMatrix = Matrix.LookAtLH(new Vector3(pos.X, pos.Y, 5.0f),
                new Vector3(pos.X, pos.Y, 0.0f),
                new Vector3(0, 1, 0));
            effect.SetValue("world", renderer.WorldMatrix);
            effect.SetValue("worldViewProj", renderer.WorldViewProjectionMatrix);
            renderer.Begin(effect);
            renderer.SetPass(2);
            lightMesh.Render();
            renderer.End();
            #endregion

            renderer.Present();
        }
    }
    #endregion

    #region scene graph test
    //public partial class Dynamic2DLightingForm : GraphicsForm
    //{
    //    private Config config = new Config("Config.txt");

    //    private SceneGraph sceneGraph = null;
    //    private GeometryNode object1 = null;
    //    private GeometryNode object2 = null;
    //    private GeometryNode object3 = null;

    //    private float angle = 0.0f;
    //    private const float AngularVelocity = (2.0f * (float)Math.PI) / 5.0f;

    //    private Effect effect = null;

    //    public Config Config
    //    {
    //        get { return config; }
    //    }

    //    protected override void InitializeGame()
    //    {
    //        Cursor.Hide();

    //        renderer.ProjectionMode = ProjectionMode.Orthogonal;
    //        renderer.ViewMatrix = Matrix.LookAtLH(new Vector3(0, 0, 5.0f), new Vector3(),
    //            new Vector3(0, 1, 0));

    //        effect = GlobalResourceCache.CreateEffectFromFile(renderer,
    //            "Effect Files\\Dynamic2DLightingEffect.fx");

    //        sceneGraph = new SceneGraph(renderer);

    //        object1 = new GeometryNode(renderer, sceneGraph,
    //            Matrix.Identity,
    //            Mesh.Circle(renderer, Color.Blue, Color.Blue, 85, 32));

    //        object2 = new GeometryNode(renderer, sceneGraph,
    //            Matrix.Translation(350.0f, 0.0f, 0.0f),
    //            Mesh.Circle(renderer, Color.Red, Color.Red, 50, 32));

    //        object3 = new GeometryNode(renderer, sceneGraph,
    //            Matrix.Translation(100.0f, -100.0f, 0.0f),
    //            Mesh.Circle(renderer, Color.Green, Color.Green, 25, 32));

    //        sceneGraph.Root.AddChild(object1);
    //        object1.AddChild(object2);
    //        object2.AddChild(object3);

    //        this.KeyDown += new KeyEventHandler(OnKeyDown);
    //    }

    //    protected override void UpdateEnvironment()
    //    {
    //        angle += AngularVelocity * timer.MoveFactorPerSecond;
    //    }

    //    protected override void Render3DEnvironment()
    //    {
    //        object2.LocalTransform = Matrix.Translation(350.0f, 0.0f, 0.0f) *
    //            Matrix.RotationZ(angle);
    //        object3.LocalTransform = Matrix.Translation(100.0f, -100.0f, 0.0f) *
    //            Matrix.RotationZ(angle);

    //        renderer.Clear();
    //        renderer.Begin(effect);

    //        sceneGraph.Update();

    //        renderer.End();
    //        renderer.Present();
    //    }

    //    void OnKeyDown(object sender, KeyEventArgs e)
    //    {
    //        if (e.KeyCode == Keys.Escape)
    //            running = false;
    //    }
    //}
    #endregion
}