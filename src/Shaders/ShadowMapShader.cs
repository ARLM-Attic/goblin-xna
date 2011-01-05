/************************************************************************************ 
 *
 * Microsoft XNA Community Game Platform
 * Copyright (C) Microsoft Corporation. All rights reserved.
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using GoblinXNA.Helpers;
using Texture = GoblinXNA.Graphics.Texture;

namespace GoblinXNA.Shaders
{
    /// <summary>
    /// Shadow map shader
    /// </summary>
    public class ShadowMapShader : Shader
    {
        #region Variables
        /// <summary>
        /// Shadow mapping shader filename
        /// </summary>
        const string ShaderFilename = "ShadowMap.fx";

        /// <summary>
        /// Shadow map texture we render to.
        /// </summary>
        internal RenderToTexture
            shadowMapTexture = null;

        /// <summary>
        /// Restrict near and far plane for much better depth resolution!
        /// </summary>
        protected float
            shadowNearPlane = 1.0f,//1.0f,//2.0f,
            shadowFarPlane = 1.0f * 28;//42.0f;//32.0f;//16.0f;//6.0f;

        /// <summary>
        /// Virtual point light parameters for directional shadow map lighting.
        /// Used to create a point light position for the directional light.
        /// </summary>
        protected float
            virtualLightDistance = 24,
            virtualVisibleRange = 23.5f;

        /// <summary>
        /// Shadow distance
        /// </summary>
        /// <returns>Float</returns>
        public float ShadowDistance
        {
            get
            {
                return virtualLightDistance;
            } // get
        } // ShadowDistance

        private Vector3 shadowLightPos = new Vector3();

        /// <summary>
        /// Shadow light position
        /// </summary>
        /// <returns>Vector 3</returns>
        public Vector3 ShadowLightPos
        {
            get
            {
                return shadowLightPos;
            } // get
        } // ShadowLightPos

        /// <summary>
        /// Texel width and height and offset for texScaleBiasMatrix,
        /// this way we can directly access the middle of each texel.
        /// </summary>
        float
            texelWidth = 1.0f / 1024.0f,
            texelHeight = 1.0f / 1024.0f,
            texOffsetX = 0.5f,
            texOffsetY = 0.5f;

        /// <summary>
        /// Compare depth bias
        /// </summary>
        protected float compareDepthBias = 0.00025f;

        /// <summary>
        /// Tex extra scale
        /// </summary>
        /// <returns>1.0f</returns>
        protected float texExtraScale = 1.0f;//1.0015f;//1.0075f;

        /// <summary>
        /// Shadow map depth bias value
        /// </summary>
        /// <returns>+</returns>
        protected float shadowMapDepthBiasValue = 0.00025f;

        /// <summary>
        /// The matrix to convert proj screen coordinates in the -1..1 range
        /// to the shadow depth map texture coordinates.
        /// </summary>
        Matrix texScaleBiasMatrix;

        /// <summary>
        /// Used matrices for the light casting the shadows.
        /// </summary>
        protected Matrix lightProjectionMatrix, lightViewMatrix;

        /// <summary>
        /// Additional effect handles
        /// </summary>
        protected EffectParameter
            shadowTexTransform,
            worldViewProjLight,
            nearPlane,
            farPlane,
            depthBias,
            shadowMapDepthBias,
            shadowMap,
            shadowMapTexelSize,
            shadowDistanceFadeoutTexture;

        /// <summary>
        /// Shadow map blur post screen shader, used in RenderShadows
        /// to blur the shadow results.
        /// </summary>
        internal ShadowMapBlur shadowMapBlur = null;
        #endregion

        #region Calc shadow map bias matrix
        /// <summary>
        /// Calculate the texScaleBiasMatrix for converting proj screen
        /// coordinates in the -1..1 range to the shadow depth map
        /// texture coordinates.
        /// </summary>
        internal void CalcShadowMapBiasMatrix()
        {
            texelWidth = 1.0f / (float)shadowMapTexture.Width;
            texelHeight = 1.0f / (float)shadowMapTexture.Height;
            texOffsetX = 0.5f + (0.5f / (float)shadowMapTexture.Width);
            texOffsetY = 0.5f + (0.5f / (float)shadowMapTexture.Height);

            texScaleBiasMatrix = new Matrix(
                0.5f * texExtraScale, 0.0f, 0.0f, 0.0f,
                0.0f, -0.5f * texExtraScale, 0.0f, 0.0f,
                0.0f, 0.0f, texExtraScale, 0.0f,
                texOffsetX, texOffsetY, 0.0f, 1.0f);
        } // CalcShadowMapBiasMatrix()
        #endregion

        #region Constructors
        /// <summary>
        /// Shadow map shader
        /// </summary>
        public ShadowMapShader() : base(ShaderFilename)
        {
            if (State.CanUsePS20)
                // We use R32F, etc. and have a lot of precision
                compareDepthBias = 0.0001f;
            else
                // A8R8G8B8 isn't very precise, increase comparing depth bias
                compareDepthBias = 0.0075f;// 0.025f;

            // Ok, time to create the shadow map render target
            shadowMapTexture = new RenderToTexture(
                RenderToTexture.SizeType.ShadowMap);

            CalcShadowMapBiasMatrix();

            shadowMapBlur = new ShadowMapBlur();
        } // ShadowMapShader(tryToUsePS20, setBackFaceCheck, setShadowMapSize)
        #endregion

        #region Get parameters
        /// <summary>
        /// Get parameters
        /// </summary>
        protected override void GetParameters()
        {
            // Can't get parameters if loading failed!
            if (effect == null)
                return;

            worldViewProj = effect.Parameters["worldViewProj"];
            viewProj = effect.Parameters["viewProj"];
            world = effect.Parameters["world"];
            viewInverse = effect.Parameters["viewInverse"];
            projection = effect.Parameters["projection"];

            // Get additional parameters
            shadowTexTransform = effect.Parameters["shadowTexTransform"];
            worldViewProjLight = effect.Parameters["worldViewProjLight"];
            nearPlane = effect.Parameters["nearPlane"];
            farPlane = effect.Parameters["farPlane"];
            depthBias = effect.Parameters["depthBias"];
            shadowMapDepthBias = effect.Parameters["shadowMapDepthBias"];
            shadowMap = effect.Parameters["shadowMap"];
            shadowMapTexelSize = effect.Parameters["shadowMapTexelSize"];
            shadowDistanceFadeoutTexture =
                effect.Parameters["shadowDistanceFadeoutTexture"];
            // Load shadowDistanceFadeoutTexture
            if (shadowDistanceFadeoutTexture != null)
                shadowDistanceFadeoutTexture.SetValue(
                    new Texture("ShadowDistanceFadeoutMap").XnaTexture);

            shadowNearPlane = 1.0f;
            shadowFarPlane = 6.25f * 28 * 1.25f * 8;
            virtualLightDistance = 5.5f * 24 * 1.3f;
            virtualVisibleRange = 5.5f * 23.5f * 2;

            if (State.CanUsePS20)
            {
                compareDepthBias = 0.00085f;//0.00025f;
                shadowMapDepthBiasValue = 0.00085f;// 0.00025f;
            } // if (BaseGame.CanUsePS20)
            else
            {
                // Ps 1.1 isn't as percise!
                shadowFarPlane = 4.75f * 28;
                virtualLightDistance = 4.0f * 24;
                virtualVisibleRange = 4.0f * 23.5f;
                compareDepthBias = 0.0065f;//0.0045f;//0.0025f;
                shadowMapDepthBiasValue = 0.0035f;//0.0025f;
            } // else

            // Set all extra parameters for this shader
            depthBias.SetValue(compareDepthBias);
            shadowMapDepthBias.SetValue(shadowMapDepthBiasValue);
            shadowMapTexelSize.SetValue(
                new Vector2(texelWidth, texelHeight));
            nearPlane.SetValue(shadowNearPlane);
            farPlane.SetValue(shadowFarPlane);
        } // GetParameters()
        #endregion

        #region Update parameters

        public override void SetParameters(List<LightNode> globalLights, List<LightNode> localLights)
        {
            base.SetParameters(globalLights, localLights);

            Vector3 lightDir = new Vector3();
            LightNode lNode = null;

            // traverse the local lights in reverse order in order to get closest light sources
            // in the scene graph
            for (int i = localLights.Count - 1; i >= 0; i--)
            {
                lNode = localLights[i];

                if (lightDir.Equals(Vector3.Zero))
                {
                    // skip the light source if not enabled or not a direction light
                    if (!lNode.LightSource.Enabled || (lNode.LightSource.Type != LightType.Directional))
                        continue;
                    lightDir = Vector3.Negate(((Matrix)(Matrix.CreateTranslation(lNode.LightSource.Direction) *
                        MatrixHelper.GetRotationMatrix(lNode.WorldTransformation))).Translation);

                    break;
                }
            }

            // Next, traverse the global lights in normal order
            for (int i = 0; i < globalLights.Count; i++)
            {
                lNode = globalLights[i];

                if (lightDir.Equals(Vector3.Zero))
                {
                    // skip the light source if not enabled or not a point light
                    if (!lNode.LightSource.Enabled || (lNode.LightSource.Type != LightType.Directional))
                        continue;

                    lightDir = Vector3.Negate(((Matrix)(Matrix.CreateTranslation(lNode.LightSource.Direction) *
                        MatrixHelper.GetRotationMatrix(lNode.WorldTransformation))).Translation);

                    break;
                }
            }

            if (lightDir.Equals(Vector3.Zero))
                return;

            Vector3 lightLookPos = new Vector3();

            lightViewMatrix = Matrix.CreateLookAt(
                lightLookPos + lightDir * virtualVisibleRange,
                lightLookPos,
                Vector3Helper.Get(0, 0, 1));

            // Update light pos
            Matrix invView = Matrix.Invert(lightViewMatrix);
            shadowLightPos = Vector3Helper.Get(invView.M41, invView.M42, invView.M43);
        }
        #endregion

        #region Create simple directional shadow mapping matrix
        /// <summary>
        /// Calc simple directional shadow mapping matrix
        /// </summary>
        private void CalcSimpleDirectionalShadowMappingMatrix()
        {
            // Put light for directional mode away from origin (create virutal point
            // light). But adjust field of view to see enough of the visible area.
            float virtualFieldOfView = (float)Math.Atan2(
                virtualVisibleRange, virtualLightDistance);

            // Set projection matrix for light
            lightProjectionMatrix = Matrix.CreatePerspective(
                // Don't use graphics fov and aspect ratio in directional lighting mode
                virtualFieldOfView,
                1.0f,
                shadowNearPlane,
                shadowFarPlane);
        } // CalcSimpleDirectionalShadowMappingMatrix()
        #endregion

        #region Generate shadow
        /// <summary>
        /// Update shadow world matrix.
        /// Calling this function is important to keep the shaders
        /// WorldMatrix and WorldViewProjMatrix up to date.
        /// </summary>
        /// <param name="setWorldMatrix">World matrix</param>
        public void UpdateGenerateShadowWorldMatrix(Matrix setWorldMatrix)
        {
            world.SetValue(setWorldMatrix);
            WorldViewProjMatrix =
                setWorldMatrix * lightViewMatrix * lightProjectionMatrix;
            effect.CommitChanges();
        } // UpdateGenerateShadowWorldMatrix(setWorldMatrix)

        /// <summary>
        /// Generate shadow
        /// </summary>
        internal void GenerateShadows(RenderHandler renderObjects)
        {
            // Can't generate shadow if loading failed!
            if (effect == null)
                return;

            // This method sets all required shader variables.
            this.SetParameters(new Material());
            Matrix remViewMatrix = State.ViewMatrix;
            Matrix remProjMatrix = State.ProjectionMatrix;
            CalcSimpleDirectionalShadowMappingMatrix();

            // Time to generate the shadow texture
            DepthStencilBuffer remBackBufferSurface = null;
            // Start rendering onto the shadow map
            shadowMapTexture.SetRenderTarget();
            if (shadowMapTexture.ZBufferSurface != null)
            {
                remBackBufferSurface = State.Device.DepthStencilBuffer;
                State.Device.DepthStencilBuffer =
                    shadowMapTexture.ZBufferSurface;
            } // if (shadowMapTexture.ZBufferSurface)

            // Make sure depth buffer is on
            State.Device.RenderState.DepthBufferEnable = true;
            // Disable alpha
            State.Device.RenderState.AlphaBlendEnable = false;

            // Clear render target
            shadowMapTexture.Clear(Color.White);

            if (State.CanUsePS20)
                effect.CurrentTechnique = effect.Techniques["GenerateShadowMap20"];
            else
                effect.CurrentTechnique = effect.Techniques["GenerateShadowMap"];

            // Render shadows with help of the GenerateShadowMap shader
            RenderSinglePassShader(renderObjects);

            // Resolve the render target to get the texture (required for Xbox)
            shadowMapTexture.Resolve();

            // Set render target back to default
            State.ResetRenderTarget(false);

            if (shadowMapTexture.ZBufferSurface != null)
                State.Device.DepthStencilBuffer = remBackBufferSurface;

            State.ViewMatrix = remViewMatrix;
            State.ProjectionMatrix = remProjMatrix;
        } // GenerateShadows()
        #endregion

        #region Use shadow
        /// <summary>
        /// Update calc shadow world matrix, has to be done for each object
        /// we want to render in CalcShadows.
        /// </summary>
        /// <param name="setWorldMatrix">Set world matrix</param>
        public void UpdateCalcShadowWorldMatrix(Matrix setWorldMatrix)
        {
            world.SetValue(setWorldMatrix);
            this.WorldViewProjMatrix =
                setWorldMatrix * State.ViewMatrix * State.ProjectionMatrix;

            // Compute the matrix to transform from view space to light proj:
            // inverse of view matrix * light view matrix * light proj matrix
            Matrix lightTransformMatrix =
                setWorldMatrix *
                lightViewMatrix *
                lightProjectionMatrix *
                texScaleBiasMatrix;
            shadowTexTransform.SetValue(lightTransformMatrix);

            Matrix worldViewProjLightMatrix =
                setWorldMatrix *
                lightViewMatrix *
                lightProjectionMatrix;
            worldViewProjLight.SetValue(worldViewProjLightMatrix);

            effect.CommitChanges();
        } // UpdateCalcShadowWorldMatrix(setWorldMatrix)

        /// <summary>
        /// Calc shadows with help of generated light depth map,
        /// all objects have to be rendered again for comparing.
        /// We could save a pass when directly rendering here, but this
        /// has 2 disadvantages: 1. we can't post screen blur the shadow
        /// and 2. we can't use any other shader, especially bump and specular
        /// mapping shaders don't have any instructions left with ps_1_1.
        /// This way everything is kept simple, we can do as complex shaders
        /// as we want, the shadow shaders work seperately.
        /// </summary>
        /// <param name="renderObjects">Render objects</param>
        public void RenderShadows(RenderHandler renderObjects)
        {
            // Can't calc shadows if loading failed!
            if (effect == null)
                return;

            // Make sure z buffer and writing z buffer is on
            State.Device.RenderState.DepthBufferEnable = true;
            State.Device.RenderState.DepthBufferWriteEnable = true;

            // Render shadows into our shadowMapBlur render target
            shadowMapBlur.RenderShadows(
                delegate
                {
                    if (State.CanUsePS20)
                        effect.CurrentTechnique = effect.Techniques["UseShadowMap20"];
                    else
                        effect.CurrentTechnique = effect.Techniques["UseShadowMap"];

                    // This method sets all required shader variables.
                    this.SetParameters(new Material());

                    // Use the shadow map texture here that was generated in
                    // GenerateShadows().
                    shadowMap.SetValue(shadowMapTexture.XnaTexture);

                    // Render shadows with help of the UseShadowMap shader
                    RenderSinglePassShader(renderObjects);
                });

            // Start rendering the shadow map blur (pass 1, that messes up our
            // background), pass 2 can be done below without any render targets.
            shadowMapBlur.RenderShadows();

            // Kill background z buffer (else glass will not be rendered correctly)
            State.Device.Clear(ClearOptions.DepthBuffer, Color.Black, 1, 0);
        } // RenderShadows(renderObjects)
        #endregion

        #region ShowShadows
        /// <summary>
        /// Show Shadows
        /// </summary>
        public void ShowShadows()
        {
            shadowMapBlur.ShowShadows();
        } // ShowShadows()
        #endregion

        /// <summary>
        /// Render single pass shader
        /// </summary>
        /// <param name="renderDelegate">Render delegate</param>
        public void RenderSinglePassShader(RenderHandler renderCode)
        {
            if (renderCode == null)
                throw new ArgumentNullException("renderCode");

            // Start effect (current technique should be set)
            try
            {
                effect.Begin(SaveStateMode.None);
                // Start first pass
                effect.CurrentTechnique.Passes[0].Begin();

                // Render
                renderCode();

                // End pass and shader
                effect.CurrentTechnique.Passes[0].End();
            }
            finally
            {
                effect.End();
            }
        }
    } // class ShadowMapShader
}
