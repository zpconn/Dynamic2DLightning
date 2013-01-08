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
    /// Describes how projections from view space to screen space are to be performed.
    /// </summary>
    public enum ProjectionMode
    {
        /// <summary>
        /// Perspective projection. Used for true 3D graphics.
        /// </summary>
        Perspective,
        /// <summary>
        /// Orthogonal projection. Used for doing 2D graphics without resorting to
        /// transformed coordinates.
        /// </summary>
        Orthogonal
    }
    #endregion

    /// <summary>
    /// Initializes and manages Direct3D. Also provides comprehensive functionality 
    /// for rendering the scene. The renderer uses the programmable pipeline exclusively
    /// by way of the DirectX Effect framework.
    /// </summary>
    public class Renderer
    {
        #region Variables
        private D3DEnum d3dEnum = new D3DEnum();
        private DeviceSettings windowedSettings = null;
        private DeviceSettings fullscreenSettings = null;
        private DeviceSettings currentSettings = null;

        private Device device = null;
        private Control renderTarget = null;
        private DisplayMode displayMode;
        private bool windowed;

        private List<IGraphicsResource> graphicsObjects = new List<IGraphicsResource>();

        private bool canDoPS11 = false, canDoPS20 = false, canDoPS30 = false;
        private bool canDoVS11 = false, canDoVS20 = false, canDoVS30 = false;

        private float fieldOfView = (float)Math.PI / 2.0f,
            nearPlane = 1.0f, farPlane = 100.0f;

        private Matrix worldMatrix, viewMatrix, projectionMatrix;

        private ProjectionMode projectionMode = ProjectionMode.Perspective;

        private Effect currentEffect = null;

        private Surface savedRenderTarget = null;
        #endregion

        #region Events
        public event EventHandler WindowedChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Gets whether the render device supports vertex shader version 1.0.
        /// </summary>
        public bool CanDoVS11
        {
            get
            {
                return canDoVS11;
            }
        }

        /// <summary>
        /// Gets whether the render device supports vertex shader version 2.0.
        /// </summary>
        public bool CanDoVS20
        {
            get
            {
                return canDoVS20;
            }
        }

        /// <summary>
        /// Gets whetehr the render device supports vertex shader version 3.0.
        /// </summary>
        public bool CanDoVS30
        {
            get
            {
                return canDoVS30;
            }
        }

        /// <summary>
        /// Gets whether the render device supports pixel shader version 1.1.
        /// </summary>
        public bool CanDoPS11
        {
            get
            {
                return canDoPS11;
            }
        }

        /// <summary>
        /// Gets whether the render device supports pixel shader version 2.0.
        /// </summary>
        public bool CanDoPS20
        {
            get
            {
                return canDoPS20;
            }
        }

        /// <summary>
        /// Gets whether the render device supports pixel shader version 3.0.
        /// </summary>
        public bool CanDoPS30
        {
            get
            {
                return canDoPS30;
            }
        }

        /// <summary>
        /// Gets and sets whether the Device is in windowed mode
        /// </summary>
        public bool Windowed
        {
            get
            { 
                return windowed;
            }
            set
            {
                windowed = value;

                WindowedChanged.Invoke(this, null);

                if (!windowed)
                {
                    // Going to fullscreen mode
                    ChangeDevice(fullscreenSettings);
                }
                else
                {
                    // Going to window mode
                    ChangeDevice(windowedSettings);
                }
            }
        }

        /// <summary>
        /// Gets the Direct3D device.
        /// </summary>
        public Device Device
        {
            get
            {
                return device;
            }
        }

        /// <summary>
        /// Gets the resolution for fullscreen mode.
        /// </summary>
        public Size FullscreenSize
        {
            get
            {
                return new Size(displayMode.Width, displayMode.Height);
            }
        }

        /// <summary>
        /// Gets the current settings
        /// </summary>
        public DeviceSettings CurrentSettings
        {
            get
            {
                return (DeviceSettings)currentSettings.Clone();
            }
        }

        /// <summary>
        /// Gets and sets the windowed settings
        /// </summary>
        public DeviceSettings WindowedSettings
        {
            get
            {
                return (DeviceSettings)windowedSettings.Clone();
            }
            set
            {
                windowedSettings = value;
            }
        }

        /// <summary>
        /// Gets and sets the fullscreen settings
        /// </summary>
        public DeviceSettings FullscreenSettings
        {
            get
            {
                return (DeviceSettings)fullscreenSettings.Clone();
            }
            set
            {
                fullscreenSettings = value;
            }
        }

        /// <summary>
        /// Gets and sets the world matrix.
        /// </summary>
        public Matrix WorldMatrix
        {
            get
            {
                return worldMatrix;
            }
            set
            {
                if (worldMatrix != value)
                {
                    worldMatrix = value;
                    device.Transform.World = worldMatrix;
                }
            }
        }

        /// <summary>
        /// Gets and sets the view matrix.
        /// </summary>
        public Matrix ViewMatrix
        {
            get
            {
                return viewMatrix;
            }
            set
            {
                viewMatrix = value;
                device.Transform.View = viewMatrix;
            }
        }

        /// <summary>
        /// Gets the inverse of the view matrix.
        /// </summary>
        public Matrix InverseViewMatrix
        {
            get
            {
                return Matrix.Invert(viewMatrix);
            }
        }

        /// <summary>
        /// Gets or sets the projection matrix.
        /// </summary>
        public Matrix ProjectionMatrix
        {
            get
            {
                return projectionMatrix;
            }
            set
            {
                projectionMatrix = value;
                device.Transform.Projection = projectionMatrix;
            }
        }

        /// <summary>
        /// Gets the world-view-projection matrix.
        /// </summary>
        public Matrix WorldViewProjectionMatrix
        {
            get
            {
                return worldMatrix * viewMatrix * projectionMatrix;
            }
        }

        /// <summary>
        /// Gets the field of view.
        /// </summary>
        public float FieldOfView
        {
            get
            {
                return fieldOfView;
            }
        }

        /// <summary>
        /// Gets or sets the projection mode.
        /// </summary>
        public ProjectionMode ProjectionMode
        {
            get
            {
                return projectionMode;
            }
            set
            {
                projectionMode = value;
                BuildProjectionMatrix(FullscreenSize);
            }
        }

        /// <summary>
        /// Gets the current Effect.
        /// </summary>
        public Effect CurrentEffect
        {
            get
            {
                return currentEffect;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes Direct3D using the passed in settings.
        /// </summary>
        public Renderer(bool windowed, Control renderTarget, int desiredWidth, int desiredHeight)
        {
            InitializeDirect3D(windowed, renderTarget, desiredWidth, desiredHeight);
        }
        #endregion

        #region Misc. graphics methods
        /// <summary>
        /// Adds an IGraphicsObject to the internal list of graphics objects.
        /// </summary>
        public void AddGraphicsObject(IGraphicsResource graphicsObject)
        {
            graphicsObjects.Add(graphicsObject);
        }

        /// <summary>
        /// Saves a copy of the render target so that it can be restored later.
        /// </summary>
        public void SaveRenderTarget()
        {
            savedRenderTarget = device.GetRenderTarget(0);
        }

        /// <summary>
        /// Restores the render target to one saved with SaveRenderTarget().
        /// </summary>
        public void RestoreRenderTarget()
        {
            if (savedRenderTarget != null)
                device.SetRenderTarget(0, savedRenderTarget);
            else
                Log.Write("Attempted to restore a null render target.");
        }

        /// <summary>
        /// Builds the projection matrix according to the projection mode.
        /// </summary>
        private void BuildProjectionMatrix(Size renderTargetSize)
        {
            switch (projectionMode)
            {
                case ProjectionMode.Perspective:
                    float aspectRatio = 1.0f;

                    if (renderTargetSize.Height != 0)
                        aspectRatio = (float)renderTargetSize.Width / (float)renderTargetSize.Height;

                    ProjectionMatrix = Matrix.PerspectiveFovLH(fieldOfView, aspectRatio, nearPlane, farPlane);

                    break;

                case ProjectionMode.Orthogonal:
                    ProjectionMatrix = Matrix.OrthoLH(renderTargetSize.Width,
                        renderTargetSize.Height, nearPlane, farPlane);
                    break;
            }
        }

        /// <summary>
        /// Clears optionally the target, depth buffer and/or stencil.
        /// </summary>
        public void Clear(ClearFlags clearFlags, Color targetColor,
            float zClear, int stencilClear)
        {
            device.Clear(clearFlags, targetColor, zClear, stencilClear);
        }

        /// <summary>
        /// Clears the target to black and the z-buffer to 1.0f.
        /// </summary>
        public void Clear()
        {
            Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
        }

        /// <summary>
        /// Binds a texture to an Effect parameter.
        /// </summary>
        public void BindTexture(string effectParameterName, Texture texture)
        {
            if (currentEffect != null)
                currentEffect.SetValue(effectParameterName, texture);
        }

        /// <summary>
        /// Begins rendering with an effect. If 'effect' is null, then the fixed-function pipeline is used.
        /// </summary>
        public void Begin(Effect effect)
        {
            //device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();

            if (effect != null)
            {
                currentEffect = effect;
                currentEffect.BeginTechnique();
            }
        }

        /// <summary>
        /// Sets the Effect pass to render. Must be called between Begin() and End() calls.
        /// </summary>
        public void SetPass(int passNumber)
        {
            if (currentEffect != null)
                currentEffect.Pass(passNumber);
            else
                Log.Write("Attempted to render a pass with a null Effect.");
        }

        /// <summary>
        /// Ends rendering with an effect.
        /// </summary>
        public void End()
        {
            if (currentEffect != null)
                currentEffect.EndTechnique();

            device.EndScene();
        }

        /// <summary>
        /// Presents the scene to the front buffer.
        /// </summary>
        public void Present()
        {
            device.Present();
        }
        #endregion

        #region Direct3D Initialization
        /// <summary>
        /// Initializes Direct3D using passed in settings.
        /// </summary>
        private void InitializeDirect3D(bool windowed, Control renderTarget, int desiredWidth, int desiredHeight)
        {
            this.windowed = windowed;
            this.renderTarget = renderTarget;
            this.displayMode = Manager.Adapters[0].CurrentDisplayMode;

            d3dEnum.EnumerateAdapters();

            // Create the device settings
            windowedSettings = FindBestWindowedSettings();
            fullscreenSettings = FindBestFullscreenSettings();
            currentSettings = windowed ? windowedSettings : fullscreenSettings;

            try
            {
                device = new Device((int)currentSettings.AdapterOrdinal, currentSettings.DeviceType,
                    renderTarget, currentSettings.BehaviorFlags, currentSettings.PresentParameters);
            }
            catch (DirectXException)
            {
                throw new DirectXException("Unable to create the Direct3D device.");
            }

            // Cancel automatic device reset on resize
            device.DeviceResizing += new System.ComponentModel.CancelEventHandler(CancelResize);

            device.DeviceLost += new EventHandler(OnDeviceLost);
            device.DeviceReset += new EventHandler(OnDeviceReset);
            device.Disposing += new EventHandler(OnDeviceDisposing);

            // What vertex and pixel shader versions are supported?
            Caps caps = Manager.GetDeviceCaps((int)currentSettings.AdapterOrdinal, currentSettings.DeviceType);

            canDoPS11 = caps.PixelShaderVersion >= new Version(1, 1);
            canDoPS20 = caps.PixelShaderVersion >= new Version(2, 0);
            canDoPS30 = caps.PixelShaderVersion >= new Version(3, 0);

            canDoVS11 = caps.VertexShaderVersion >= new Version(1, 1);
            canDoVS20 = caps.VertexShaderVersion >= new Version(2, 0);
            canDoVS30 = caps.VertexShaderVersion >= new Version(3, 0);

            BuildProjectionMatrix(new Size(desiredWidth, desiredHeight));
        }

        /// <summary>
        /// Disposes of all IGraphicsObjects.
        /// </summary>
        void OnDeviceDisposing(object sender, EventArgs e)
        {
            foreach (IGraphicsResource graphicsObject in graphicsObjects)
                graphicsObject.Dispose();
        }

        /// <summary>
        /// Handles a device reset. Informs all IGraphicsObjects of the event.
        /// </summary>
        void OnDeviceReset(object sender, EventArgs e)
        {
            BuildProjectionMatrix(FullscreenSize);

            foreach (IGraphicsResource graphicsObject in graphicsObjects)
                graphicsObject.OnDeviceReset();
        }

        /// <summary>
        /// Handles a lost device. Informs all IGraphicsObjects of the event.
        /// </summary>
        void OnDeviceLost(object sender, EventArgs e)
        {
            foreach (IGraphicsResource graphicsObject in graphicsObjects)
                graphicsObject.OnDeviceLost();
        }

        /// <summary>
        /// Changes the device with the new settings.
        /// </summary>
        public void ChangeDevice(DeviceSettings newSettings)
        {
            windowed = newSettings.PresentParameters.Windowed;

            if (newSettings.PresentParameters.Windowed)
                windowedSettings = (DeviceSettings)newSettings.Clone();
            else
                fullscreenSettings = (DeviceSettings)newSettings.Clone();

            if (device != null)
            {
                device.Dispose();
                device = null;
            }

            try
            {
                device = new Device((int)newSettings.AdapterOrdinal, newSettings.DeviceType, renderTarget,
                    newSettings.BehaviorFlags, newSettings.PresentParameters);

                // Cancel automatic device reset on resize
                device.DeviceResizing += new System.ComponentModel.CancelEventHandler(CancelResize);

                device.DeviceLost += new EventHandler(OnDeviceLost);
                device.DeviceReset += new EventHandler(OnDeviceReset);
                device.Disposing += new EventHandler(OnDeviceDisposing);

                OnDeviceReset(this, null);
            }
            catch (DirectXException)
            {
                throw new DirectXException("Unable to recreate the Direct3D device while changing settings");
            }

            currentSettings = windowed ? windowedSettings : fullscreenSettings; 
        }

        /// <summary>
        /// Rebuilds present parameters in preparation for a device reset.
        /// </summary>
        public void ResetPresentParameters()
        {
            if (windowed)
            {
                currentSettings.PresentParameters.BackBufferWidth = 0;
                currentSettings.PresentParameters.BackBufferHeight = 0;
            }
        }

        /// <summary>
        /// Resets the device.
        /// </summary>
        public void Reset()
        {
            if (device != null)
            {
                ResetPresentParameters();
                device.Reset(currentSettings.PresentParameters);
            }
        }

        /// <summary>
        /// Finds the best windowed Device settings supported by the system.
        /// </summary>
        /// <returns>
        /// A DeviceSettings class full with the best supported windowed settings.
        /// </returns>
        private DeviceSettings FindBestWindowedSettings()
        {
            DeviceSettingsEnum bestSettings = null;
            bool foundBest = false;
            // Loop through each adapter
            foreach (AdapterEnum a in d3dEnum.Adapters)
            {
                // Loop through each device
                foreach (DeviceEnum d in a.DeviceEnumList)
                {
                    // Loop through each device settings configuration
                    foreach (DeviceSettingsEnum s in d.SettingsList)
                    {
                        // Must be windowed mode and the AdapterFormat must match current DisplayMode Format
                        if (!s.Windowed || (s.AdapterFormat != displayMode.Format))
                        {
                            continue;
                        }

                        // The best DeviceSettingsEnum is a DeviceType.Hardware Device
                        // where its BackBufferFormat is the same as the AdapterFormat
                        if ((bestSettings == null) ||
                             ((s.DeviceType == DeviceType.Hardware) && (s.AdapterFormat == s.BackBufferFormat)) ||
                             ((bestSettings.DeviceType != DeviceType.Hardware) &&
                             (s.DeviceType == DeviceType.Hardware)))
                        {
                            if (!foundBest)
                            {
                                bestSettings = s;
                            }

                            if ((s.DeviceType == DeviceType.Hardware) && (s.AdapterFormat == s.BackBufferFormat))
                            {
                                foundBest = true;
                            }
                        }
                    }
                }
            }

            if (bestSettings == null)
            {
                throw new DirectXException("Unable to find any supported window mode settings.");
            }

            // Store the best settings
            DeviceSettings windowedSettings = new DeviceSettings();

            windowedSettings.AdapterFormat = bestSettings.AdapterFormat;
            windowedSettings.AdapterOrdinal = bestSettings.AdapterOrdinal;
            windowedSettings.BehaviorFlags = (CreateFlags)bestSettings.VertexProcessingTypeList[0];
            windowedSettings.Caps = bestSettings.DeviceInformation.Caps;
            windowedSettings.DeviceType = bestSettings.DeviceType;

            windowedSettings.PresentParameters = new PresentParameters();

            windowedSettings.PresentParameters.AutoDepthStencilFormat =
                (DepthFormat)bestSettings.DepthStencilFormatList[0];
            windowedSettings.PresentParameters.BackBufferCount = 1;
            windowedSettings.PresentParameters.BackBufferFormat = bestSettings.AdapterFormat;
            windowedSettings.PresentParameters.BackBufferHeight = 0;
            windowedSettings.PresentParameters.BackBufferWidth = 0;
            windowedSettings.PresentParameters.DeviceWindow = renderTarget;
            windowedSettings.PresentParameters.EnableAutoDepthStencil = true;
            windowedSettings.PresentParameters.FullScreenRefreshRateInHz = 0;
            windowedSettings.PresentParameters.MultiSample = (MultiSampleType)bestSettings.MultiSampleTypeList[0];
            windowedSettings.PresentParameters.MultiSampleQuality = 0;
            windowedSettings.PresentParameters.PresentationInterval =
                (PresentInterval)bestSettings.PresentIntervalList[0];
            windowedSettings.PresentParameters.PresentFlag = PresentFlag.DiscardDepthStencil;
            windowedSettings.PresentParameters.SwapEffect = SwapEffect.Discard;
            windowedSettings.PresentParameters.Windowed = true;

            return windowedSettings;
        }

        /// <summary>
        /// Finds the best fullscreen Device settings supported by the system.
        /// </summary>
        /// <returns>
        /// A DeviceSettings class full with the best supported fullscreen settings.
        /// </returns>
        private DeviceSettings FindBestFullscreenSettings()
        {
            DeviceSettingsEnum bestSettings = null;
            bool foundBest = false;

            // Loop through each adapter
            foreach (AdapterEnum a in d3dEnum.Adapters)
            {
                // Loop through each device
                foreach (DeviceEnum d in a.DeviceEnumList)
                {
                    // Loop through each device settings configuration
                    foreach (DeviceSettingsEnum s in d.SettingsList)
                    {
                        // Must be fullscreen mode
                        if (s.Windowed)
                        {
                            continue;
                        }

                        // To make things easier, we'll say the best DeviceSettingsEnum 
                        // is a DeviceType.Hardware Device whose AdapterFormat is the same as the
                        // current DisplayMode Format and whose BackBufferFormat matches the
                        // AdapterFormat
                        if ((bestSettings == null) ||
                             ((s.DeviceType == DeviceType.Hardware) && (s.AdapterFormat == displayMode.Format)) ||
                             ((bestSettings.DeviceType != DeviceType.Hardware) &&
                             (s.DeviceType == DeviceType.Hardware)))
                        {
                            if (!foundBest)
                            {
                                bestSettings = s;
                            }

                            if ((s.DeviceType == DeviceType.Hardware) &&
                                (s.AdapterFormat == displayMode.Format) &&
                                (s.BackBufferFormat == s.AdapterFormat))
                            {
                                foundBest = true;
                            }
                        }
                    }
                }
            }
            if (bestSettings == null)
            {
                throw new DirectXException("Unable to find any supported fullscreen mode settings.");
            }

            // Store the best settings
            DeviceSettings fullscreenSettings = new DeviceSettings();
            fullscreenSettings.AdapterFormat = bestSettings.AdapterFormat;
            fullscreenSettings.AdapterOrdinal = bestSettings.AdapterOrdinal;
            fullscreenSettings.BehaviorFlags = (CreateFlags)bestSettings.VertexProcessingTypeList[0];
            fullscreenSettings.Caps = bestSettings.DeviceInformation.Caps;
            fullscreenSettings.DeviceType = bestSettings.DeviceType;

            fullscreenSettings.PresentParameters = new PresentParameters();
            fullscreenSettings.PresentParameters.AutoDepthStencilFormat =
                (DepthFormat)bestSettings.DepthStencilFormatList[0];
            fullscreenSettings.PresentParameters.BackBufferCount = 1;
            fullscreenSettings.PresentParameters.BackBufferFormat = bestSettings.AdapterFormat;
            fullscreenSettings.PresentParameters.BackBufferHeight = displayMode.Height;
            fullscreenSettings.PresentParameters.BackBufferWidth = displayMode.Width;
            fullscreenSettings.PresentParameters.DeviceWindow = renderTarget;
            fullscreenSettings.PresentParameters.EnableAutoDepthStencil = true;
            fullscreenSettings.PresentParameters.FullScreenRefreshRateInHz = displayMode.RefreshRate;
            fullscreenSettings.PresentParameters.MultiSample =
                (MultiSampleType)bestSettings.MultiSampleTypeList[0];
            fullscreenSettings.PresentParameters.MultiSampleQuality = 0;
            fullscreenSettings.PresentParameters.PresentationInterval =
                (PresentInterval)bestSettings.PresentIntervalList[0];
            fullscreenSettings.PresentParameters.PresentFlag = PresentFlag.DiscardDepthStencil;
            fullscreenSettings.PresentParameters.SwapEffect = SwapEffect.Discard;
            fullscreenSettings.PresentParameters.Windowed = false;

            return fullscreenSettings;
        }

        /// <summary>
        /// Cancels the automatic device reset on resize
        /// </summary>
        private void CancelResize(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }
        #endregion
    }
}