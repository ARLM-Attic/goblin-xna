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
using GoblinXNA.Graphics.ParticleEffects;
using GoblinXNA.SceneGraph;
using GoblinXNA.Helpers;
using Material = GoblinXNA.Graphics.Material;

using XNAnimation;
using XNAnimation.Effects;

namespace GoblinXNA.Shaders
{
    public class AnimatedModelShader : IShader
    {
        #region Member Fields
        Matrix worldMatrix;
        Matrix viewMatrix;
        Matrix projectionMatrix;
        SkinnedModelBasicEffect effect;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a simple shader to render 3D meshes using the BasicEffect class.
        /// </summary>
        /// <exception cref="GoblinException"></exception>
        public AnimatedModelShader()
        {
            if (!State.Initialized)
                throw new GoblinException("Goblin XNA needs to be initialized first using State.InitGoblin(..)");

            worldMatrix = Matrix.Identity;
            projectionMatrix = State.ProjectionMatrix;
            viewMatrix = State.ViewMatrix;

            effect = new SkinnedModelBasicEffect(State.Device, null);
        }
        #endregion

        #region Properties
        public int MaxLights
        {
            get { return 8; }
        }

        public Effect CurrentEffect
        {
            get { return effect; }
        }

        /// <summary>
        /// Indicates whether to prefer using per-pixel lighting if applicable.
        /// </summary>
        public bool PreferPerPixelLighting
        {
            get { return false; }
            set { /* do nothing */ }
        }
        #endregion

        #region IShader implementations
        public void SetParameters(Material material)
        {
            if (material.InternalEffect != null)
            {
                if (material.InternalEffect is SkinnedModelBasicEffect)
                {
                    // maintein the previous lighting effects
                    Vector3 ambientLightColor = effect.AmbientLightColor;
                    bool lightEnabled = effect.LightEnabled;
                    EnabledLights enableLights = effect.EnabledLights;
                    PointLight[] lights = new PointLight[8];
                    effect.PointLights.CopyTo(lights, 0);

                    effect = (SkinnedModelBasicEffect)material.InternalEffect;

                    int count = 0;
                    switch (enableLights)
                    {
                        case EnabledLights.One: count = 1; break;
                        case EnabledLights.Two: count = 2; break;
                        case EnabledLights.Four: count = 4; break;
                        case EnabledLights.Six: count = 6; break;
                        case EnabledLights.Eight: count = 8; break;
                    }

                    for (int i = 0; i < count; i++)
                    {
                        effect.PointLights[i].Color = lights[i].Color;
                        effect.PointLights[i].Position = lights[i].Position;
                    }

                    effect.LightEnabled = lightEnabled;
                    effect.AmbientLightColor = ambientLightColor;
                    effect.EnabledLights = enableLights;

                    effect.Material.DiffuseColor = Vector3Helper.GetVector3(material.Diffuse);
                    effect.Material.SpecularColor = Vector3Helper.GetVector3(material.Specular);
                    effect.Material.EmissiveColor = Vector3Helper.GetVector3(material.Emissive);
                    effect.Material.SpecularPower = material.SpecularPower;
                }
                else
                    Log.Write("Passed internal effect is not SkinnedModelBasicEffect, so we can not apply the " +
                        "effect to this shader", Log.LogLevel.Warning);
            }
            else
            {
                effect.Material.DiffuseColor = Vector3Helper.GetVector3(material.Diffuse);
                effect.Material.SpecularColor = Vector3Helper.GetVector3(material.Specular);
                effect.Material.EmissiveColor = Vector3Helper.GetVector3(material.Emissive);
                effect.Material.SpecularPower = material.SpecularPower;
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
                        // skip the light source if not enabled or not a point light
                        if (!light.Enabled || (light.Type != LightType.Point))
                            continue;

                        LightSource source = new LightSource();
                        source.Diffuse = light.Diffuse;
                        source.Position = ((Matrix)(lNode.WorldTransformation *
                            Matrix.CreateTranslation(light.Position))).Translation;
                        source.Specular = light.Specular;

                        lightSources.Add(source);

                        // If there are already maximum number of lights, then skip other lights
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
                        // skip the light source if not enabled or not a point light
                        if (!light.Enabled || (light.Type != LightType.Point))
                            continue;

                        LightSource source = new LightSource();
                        source.Diffuse = light.Diffuse;
                        source.Position = ((Matrix)(lNode.WorldTransformation *
                            Matrix.CreateTranslation(light.Position))).Translation;
                        source.Specular = light.Specular;

                        lightSources.Add(source);

                        // If there are already maximum number of lights, then skip other lights
                        if (lightSources.Count >= MaxLights)
                            break;
                    }
                }
            }

            effect.AmbientLightColor = Vector3Helper.GetVector3(ambientLightColor);

            if (lightSources.Count > 0)
            {
                int numLightSource = lightSources.Count;
                int count = 0;
                for (int i = 0; i < numLightSource; i++)
                {
                    effect.PointLights[count].Color = 
                        Vector3Helper.GetVector3(lightSources[i].Diffuse) * 5;
                    effect.PointLights[count].Position = lightSources[i].Position;
                    count++;
                }

                effect.LightEnabled = (count > 0);
                if (count > 0)
                {
                    switch (count)
                    {
                        case 1: 
                            effect.EnabledLights = EnabledLights.One; 
                            break;
                        case 2:
                            effect.EnabledLights = EnabledLights.Two;
                            break;
                        case 3:
                        case 4:
                            effect.EnabledLights = EnabledLights.Four;
                            break;
                        case 5:
                        case 6:
                            effect.EnabledLights = EnabledLights.Six;
                            break;
                        case 7:
                        case 8:
                            effect.EnabledLights = EnabledLights.Eight;
                            break;
                    }
                }
            }
        }

        public void SetBoneMatrices(Matrix[] matrices)
        {
            effect.Bones = matrices;
        }

        /// <summary>
        /// This shader does not support special camera effect.
        /// </summary>
        /// <param name="camera"></param>
        public virtual void SetParameters(Camera camera)
        {
        }

        /// <summary>
        /// This shader does not support environmental effect.
        /// </summary>
        /// <param name="environment"></param>
        public virtual void SetParameters(GoblinXNA.Graphics.Environment environment)
        {
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
            effect.View = State.ViewMatrix;
            effect.Projection = State.ProjectionMatrix;
            effect.World = worldMatrix;

            try
            {
                effect.Begin();
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    if (renderDelegate != null)
                        renderDelegate();
                    pass.End();
                }
            }
            catch (Exception exp)
            {
                Log.Write("AnimatedModelShader exception: " + exp.Message);
            }
            finally
            {
                effect.End();
            }
        }

        #endregion
    }
}
