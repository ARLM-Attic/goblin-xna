/************************************************************************************ 
 * Copyright (c) 2008, Columbia University
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Columbia University nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY COLUMBIA UNIVERSITY ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <copyright holder> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 * 
 * ===================================================================================
 * Author: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Graphics;
using GoblinXNA.Helpers;
using GoblinXNA.Graphics.ParticleEffects;
using GoblinXNA.SceneGraph;

namespace GoblinXNA.Shaders
{
    /// <summary>
    /// An implementation of a simple shader that uses the BasicEffect class.
    /// </summary>
    /// <remarks>
    /// Since BasicEffect class can only include upto three light sources, if more than three light
    /// sources are passed to this class, then only the first three light sources are used for calculating
    /// the lighting equation. In the current implementation of the GoblinXNA.SceneGraph.Scene class,
    /// this means the first three lights encountered in the preorder tree-traversal of the scene graph.
    /// </remarks>
    public class SimpleEffectShader : IShader
    {
        #region Member Fields
        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        BasicEffect basicEffect;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a simple shader to render 3D meshes using the BasicEffect class.
        /// </summary>
        /// <exception cref="GoblinException"></exception>
        public SimpleEffectShader()
        {
            if (!State.Initialized)
                throw new GoblinException("Goblin XNA needs to be initialized first using State.InitGoblin(..)");

            worldMatrix = Matrix.Identity;
            projectionMatrix = State.ProjectionMatrix;
            viewMatrix = State.ViewMatrix;

            basicEffect = new BasicEffect(State.Device, null);
        }
        #endregion

        #region Properties
        public int MaxLights
        {
            get { return 3; }
        }

        public Effect CurrentEffect
        {
            get { return basicEffect; }
        }

        /// <summary>
        /// Indicates whether to prefer using per-pixel lighting if applicable.
        /// </summary>
        public bool PreferPerPixelLighting
        {
            get { return basicEffect.PreferPerPixelLighting; }
            set { basicEffect.PreferPerPixelLighting = value; }
        }
        #endregion

        #region IShader implementations
        public void SetParameters(Material material)
        {
            if (material.InternalEffect != null)
            {
                if (material.InternalEffect is BasicEffect)
                {
                    // maintein the previous lighting effects
                    BasicDirectionalLight[] lights = {basicEffect.DirectionalLight0,
                        basicEffect.DirectionalLight1, basicEffect.DirectionalLight2};

                    Vector3 ambientLightColor = basicEffect.AmbientLightColor;
                    bool lightEnabled = basicEffect.LightingEnabled;
                    bool perPixelLighting = basicEffect.PreferPerPixelLighting;

                    basicEffect = (BasicEffect)material.InternalEffect;
                    basicEffect.Alpha = material.Diffuse.W;

                    BasicDirectionalLight[] lights2 = {basicEffect.DirectionalLight0,
                        basicEffect.DirectionalLight1, basicEffect.DirectionalLight2};
                    for (int i = 0; i < lights2.Length; i++)
                    {
                        lights2[i].DiffuseColor = lights[i].DiffuseColor;
                        lights2[i].SpecularColor = lights[i].SpecularColor;
                        lights2[i].Direction = lights[i].Direction;
                        lights2[i].Enabled = lights[i].Enabled;
                    }

                    basicEffect.LightingEnabled = lightEnabled;
                    basicEffect.PreferPerPixelLighting = perPixelLighting;
                    basicEffect.AmbientLightColor = ambientLightColor;
                }
                else
                    Log.Write("Passed internal effect is not BasicEffect, so we can not apply the " +
                        "effect to this shader", Log.LogLevel.Warning);
            }
            else
            {
                basicEffect.Alpha = material.Diffuse.W;
                basicEffect.DiffuseColor = Vector3Helper.GetVector3(material.Diffuse);
                basicEffect.SpecularColor = Vector3Helper.GetVector3(material.Specular);
                basicEffect.EmissiveColor = Vector3Helper.GetVector3(material.Emissive);
                basicEffect.SpecularPower = material.SpecularPower;
                basicEffect.TextureEnabled = material.HasTexture;
                basicEffect.Texture = material.Texture;
            }
        }

        public void SetParameters(List<LightNode> globalLights, List<LightNode> localLights)
        {
            bool ambientSet = false;
            List<LightSource> lightSources = new List<LightSource>();
            LightNode lNode = null;
            Vector4 ambientLightColor = new Vector4(0, 0, 0, 1);

            // traverse the local lights in reverse order in order to get closest light sources
            // in the scene graph
            for (int i = localLights.Count - 1; i >= 0; i--)
            {
                lNode = localLights[i];
                // only set the ambient light color if it's not set yet and not the default color (0, 0, 0, 1)
                if (!ambientSet && (!lNode.AmbientLightColor.Equals(ambientLightColor)))
                {
                    ambientLightColor = lNode.AmbientLightColor;
                    ambientSet = true;
                }

                if (lightSources.Count < MaxLights)
                {
                    foreach (LightSource light in lNode.LightSources)
                    {
                        // skip the light source if not enabled or not a directional light
                        if (!light.Enabled || (light.Type != LightType.Directional))
                            continue;

                        LightSource source = new LightSource();
                        source.Diffuse = light.Diffuse;
                        source.Direction = ((Matrix)(Matrix.CreateTranslation(light.Direction) *
                            MatrixHelper.GetRotationMatrix(lNode.WorldTransformation))).Translation;
                        source.Specular = light.Specular;

                        lightSources.Add(source);

                        // If there are already 3 lights, then skip other lights
                        if (lightSources.Count >= MaxLights)
                            break;
                    }
                }
            }

            // Next, traverse the global lights in normal order
            for (int i = 0; i < globalLights.Count; i++)
            {
                lNode = globalLights[i];
                // only set the ambient light color if it's not set yet and not the default color (0, 0, 0, 1)
                if (!ambientSet && (!lNode.AmbientLightColor.Equals(ambientLightColor)))
                {
                    ambientLightColor = lNode.AmbientLightColor;
                    ambientSet = true;
                }

                if (lightSources.Count < MaxLights)
                {
                    foreach (LightSource light in lNode.LightSources)
                    {
                        // skip the light source if not enabled or not a directional light
                        if (!light.Enabled || (light.Type != LightType.Directional))
                            continue;

                        LightSource source = new LightSource();
                        source.Diffuse = light.Diffuse;
                        source.Direction = ((Matrix)(Matrix.CreateTranslation(light.Direction) *
                            MatrixHelper.GetRotationMatrix(lNode.WorldTransformation))).Translation;
                        source.Specular = light.Specular;

                        lightSources.Add(source);

                        // If there are already 3 lights, then skip other lights
                        if (lightSources.Count >= MaxLights)
                            break;
                    }
                }
            }
            
            basicEffect.AmbientLightColor = Vector3Helper.GetVector3(ambientLightColor);

            if (lightSources.Count > 0)
            {
                BasicDirectionalLight[] lights = {basicEffect.DirectionalLight0,
                    basicEffect.DirectionalLight1, basicEffect.DirectionalLight2};

                bool atLeastOneLight = false;
                int numLightSource = lightSources.Count;
                for (int i = 0; i < numLightSource; i++)
                {
                    lights[i].Enabled = true;
                    lights[i].DiffuseColor = Vector3Helper.GetVector3(lightSources[i].Diffuse);
                    lights[i].Direction =
                        lightSources[i].Direction;
                    lights[i].SpecularColor = Vector3Helper.GetVector3(lightSources[i].Specular);
                    atLeastOneLight = true;
                }

                basicEffect.LightingEnabled = atLeastOneLight;
            }
        }

        /// <summary>
        /// This shader does not support special camera effect.
        /// </summary>
        /// <param name="camera"></param>
        public virtual void SetParameters(Camera camera)
        {
        }

        public virtual void SetParameters(GoblinXNA.Graphics.Environment environment)
        {
            basicEffect.FogEnabled = environment.FogEnabled;
            if (environment.FogEnabled)
            {
                basicEffect.FogStart = environment.FogStartDistance;
                basicEffect.FogEnd = environment.FogEndDistance;
                basicEffect.FogColor = Vector3Helper.GetVector3(environment.FogColor);
            }
        }

        /// <summary>
        /// This shader does not support particle effect.
        /// </summary>
        /// <param name="particleEffect"></param>
        public void SetParameters(ParticleEffect particleEffect)
        {
        }

        public void Render(Matrix worldMatrix, string techniqueName, RenderHandler renderDelegate)
        {
            basicEffect.View = State.ViewMatrix;
            basicEffect.Projection = State.ProjectionMatrix;
            basicEffect.World = worldMatrix;

            try
            {
                basicEffect.Begin();
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    if (renderDelegate != null)
                        renderDelegate();
                    pass.End();
                }
            }
            catch (Exception exp)
            {
                Log.Write("SimpleEffectShader exception: " + exp.Message);
            }
            finally
            {
                basicEffect.End();
            }
        }

        #endregion
    }
}
