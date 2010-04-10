/************************************************************************************ 
 * Copyright (c) 2008-2010, Columbia University
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
 *         Steve Henderson (henderso@cs.columbia.edu)
 *          
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaTexture = Microsoft.Xna.Framework.Graphics.Texture;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.Graphics.ParticleEffects;
using GoblinXNA.SceneGraph;
using GoblinXNA.Shaders;
using GoblinXNA.Helpers;

namespace Tutorial14___Skinned_Model_Animation
{
    /// <summary>
    /// An implementation of the IShader interface that works with the SkinnedModel shader (SkinnedModel.fx)
    /// </summary>
    public class SkinnedModelShader : IShader
    {
        #region Member Fields

        protected String shaderName;
        protected Effect effect;
        /// <summary>
        /// Defines some of the commonly used effect parameters in shader files.
        /// </summary>
        protected EffectParameter world,
            bones,
            view,
            worldViewProj,
            viewProj,
            projection,
            texture,
            viewInverse,
            ambientLightColor,
            lights,
            numLights;

        protected Matrix lastUsedWorldViewProjMatrix;
        protected Matrix lastUsedViewProjMatrix;
        protected Matrix lastUsedProjMatrix;
        protected Matrix lastUsedViewInverseMatrix;
        protected Matrix lastUsedViewMatrix;
        protected Matrix lastUsedWorldMatrix;
        protected XnaTexture lastUsedTexture;

        protected List<LightSource> lightSources;

        #region Temporary Variables

        private Matrix tmpMat1;
        private Matrix tmpMat2;
        private Matrix tmpMat3;
        private Vector3 tmpVec1;

        #endregion

        #endregion

        #region Constructors

        public SkinnedModelShader(String shaderName)
        {
            if (State.Device == null)
                throw new GoblinException(
                    "GoblinXNA device is not initialized, can't create Shader.");

            lightSources = new List<LightSource>();

            Reload(shaderName);
        }

        #endregion

        #region Properties
        public virtual int MaxLights
        {
            get { return 3; }
        }

        /// <summary>
        /// Gets whether this shader is valid to render.
        /// </summary>
        public bool Valid
        {
            get { return effect != null; }
        }

        /// <summary>
        /// Gets the currently used effect that is loaded from a shader file.
        /// </summary>
        public Effect CurrentEffect
        {
            get { return effect; }
        }

        /// <summary>
        /// Gets the effect technique from the technique name defined in the loaded shader file.
        /// </summary>
        /// <param name="techniqueName"></param>
        public EffectTechnique GetTechnique(String techniqueName)
        {
            return effect.Techniques[techniqueName];
        }

        /// <summary>
        /// Gets the number of techniques implemented in the loaded shader file.
        /// </summary>
        public int NumberOfTechniques
        {
            get { return effect.Techniques.Count; }
        }

        /// <summary>
        /// Sets an effect parameter for Matrix type.
        /// </summary>
        /// <param name="param">An effect parameter that contains Matrix type</param>
        /// <param name="lastUsedMatrix">Old Matrix value</param>
        /// <param name="newMatrix">New Matrix value</param>
        protected void SetValue(EffectParameter param,
            ref Matrix lastUsedMatrix, Matrix newMatrix)
        {
            lastUsedMatrix = newMatrix;
            param.SetValue(newMatrix);
        }

        /// <summary>
        /// Sets an effect parameter for Vector3 type.
        /// </summary>
        /// <param name="param">An effect parameter that contains Matrix type</param>
        /// <param name="lastUsedVector">Last used vector</param>
        /// <param name="newVector">New vector</param>
        protected void SetValue(EffectParameter param,
            ref Vector3 lastUsedVector, Vector3 newVector)
        {
            if (param != null &&
                lastUsedVector != newVector)
            {
                lastUsedVector = newVector;
                param.SetValue(newVector);
            }
        }

        /// <summary>
        /// Sets an effect parameter for Color type.
        /// </summary>
        /// <param name="param">An effect parameter that contains Color type</param>
        /// <param name="lastUsedColor">Last used color</param>
        /// <param name="newColor">New color</param>
        protected void SetValue(EffectParameter param,
            ref Color lastUsedColor, Color newColor)
        {
            // Note: This check eats few % of the performance, but the color
            // often stays the change (around 50%).
            if (param != null &&
                //slower: lastUsedColor != newColor)
                lastUsedColor.PackedValue != newColor.PackedValue)
            {
                lastUsedColor = newColor;
                param.SetValue(newColor.ToVector4());
            }
        }

        /// <summary>
        /// Sets an effect parameter for float type.
        /// </summary>
        /// <param name="param">An effect parameter that contains float type</param>
        /// <param name="lastUsedValue">Last used value</param>
        /// <param name="newValue">New value</param>
        protected void SetValue(EffectParameter param,
            ref float lastUsedValue, float newValue)
        {
            if (param != null &&
                lastUsedValue != newValue)
            {
                lastUsedValue = newValue;
                param.SetValue(newValue);
            }
        }

        /// <summary>
        /// Sets an effect parameter for Xna.Framework.Graphics.Texture type.
        /// </summary>
        /// <param name="param">An effect parameter that contains 
        /// Xna.Framework.Graphics.Texture type</param>
        /// <param name="lastUsedValue">Last used value</param>
        /// <param name="newValue">New value</param>
        protected void SetValue(EffectParameter param,
            ref XnaTexture lastUsedValue, XnaTexture newValue)
        {
            if (param != null &&
                lastUsedValue != newValue)
            {
                lastUsedValue = newValue;
                param.SetValue(newValue);
            }
        }

        /// <summary>
        /// Gets or sets world projection matrix.
        /// </summary>
        protected Matrix WorldViewProjMatrix
        {
            get { return lastUsedWorldViewProjMatrix; }
            set { SetValue(worldViewProj, ref lastUsedWorldViewProjMatrix, value); }
        }

        /// <summary>
        /// Gets or sets view projection matrix.
        /// </summary>
        protected Matrix ViewProjMatrix
        {
            get { return lastUsedViewProjMatrix; }
            set { SetValue(viewProj, ref lastUsedViewProjMatrix, value); }
        }

        /// <summary>
        /// Gets or sets inverse view projection matrix.
        /// </summary>
        protected Matrix ViewInverseMatrix
        {
            get { return lastUsedViewInverseMatrix; }
            set { SetValue(viewInverse, ref lastUsedViewInverseMatrix, value); }
        }

        /// <summary>
        /// Gets or sets projection matrix.
        /// </summary>
        protected Matrix ProjectionMatrix
        {
            get { return lastUsedProjMatrix; }
            set { SetValue(projection, ref lastUsedProjMatrix, value); }
        }

        #endregion

        /// <summary>
        /// Reloads the shader file with the specified path.
        /// </summary>
        /// <param name="shaderName">The name of the shader/effect file</param>
        public void Reload(String shaderName)
        {
            this.shaderName = Path.GetFileNameWithoutExtension(shaderName);
            // Load shader

            effect = State.Content.Load<Effect>(
                Path.Combine(State.GetSettingVariable("ShaderDirectory"),
                this.shaderName)).Clone(State.Device);

            // Reset and get all avialable parameters.
            // This is especially important for derived classes.
            ResetParameters();
            GetParameters();
        }

        /// <summary>
        /// Loads the effect parameters from the loaded shader file.
        /// </summary>
        protected virtual void GetParameters()
        {

            //Binding the effect parameters in to Effect File;

            /////////////////////////////////////////////////
            //  THIS IS ESSENTIAL.  EVEN THOUGH ITS A GETTER!
            /////////////////////////////////////////////////
            
            // Geometry
            world = effect.Parameters["World"];
            viewProj = effect.Parameters["viewProjection"];
            view = effect.Parameters["View"];
            projection = effect.Parameters["Projection"];

            //Bones
            bones = effect.Parameters["Bones"];

            //Texture
            texture = effect.Parameters["Texture"];

            lights = effect.Parameters["lights"];
            ambientLightColor = effect.Parameters["ambientLightColor"];
            numLights = effect.Parameters["numLights"];
        }

        /// <summary>
        /// Resets the values of effect parameters.
        /// </summary>
        protected virtual void ResetParameters()
        {
            lastUsedViewInverseMatrix = Matrix.Identity;
            lastUsedProjMatrix = Matrix.Identity;
            lastUsedViewProjMatrix = Matrix.Identity;
            lastUsedWorldViewProjMatrix = Matrix.Identity;
            lastUsedWorldMatrix = Matrix.Identity;
            lastUsedViewMatrix = Matrix.Identity;
        }

        /// <summary>
        /// This shader does not support material effect.
        /// </summary>
        /// <param name="material"></param>
        public virtual void SetParameters(Material material)
        {
            effect = material.InternalEffect;

            GetParameters();

            numLights.SetValue(lightSources.Count);

            if (lightSources.Count > 0)
            {
                for (int i = 0; i < lightSources.Count; i++)
                {
                    lights.Elements[i].StructureMembers["direction"].SetValue(lightSources[i].Direction);
                    lights.Elements[i].StructureMembers["color"].SetValue(lightSources[i].Diffuse);
                }
            }
        }

        /// <summary>
        /// This shader does not support lighting effect.
        /// </summary>
        /// <param name="lightSources"></param>
        /// <param name="ambientLightColor"></param>
        public virtual void SetParameters(List<LightNode> globalLights, List<LightNode> localLights)
        {
            bool ambientSet = false;
            lightSources.Clear();
            LightNode lNode = null;
            Vector4 ambient = new Vector4(0, 0, 0, 1);

            // traverse the local lights in reverse order in order to get closest light sources
            // in the scene graph
            for (int i = localLights.Count - 1; i >= 0; i--)
            {
                lNode = localLights[i];
                // only set the ambient light color if it's not set yet and not the default color (0, 0, 0, 1)
                if (!ambientSet && (!lNode.AmbientLightColor.Equals(ambient)))
                {
                    ambient = lNode.AmbientLightColor;
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

                        tmpVec1 = light.Direction;
                        Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                        tmpMat2 = lNode.WorldTransformation;
                        MatrixHelper.GetRotationMatrix(ref tmpMat2, out tmpMat2);
                        Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

                        source.Direction = tmpMat3.Translation;
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
                    ambient = lNode.AmbientLightColor;
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

                        tmpVec1 = light.Direction;
                        Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                        tmpMat2 = lNode.WorldTransformation;
                        MatrixHelper.GetRotationMatrix(ref tmpMat2, out tmpMat2);
                        Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

                        source.Direction = tmpMat3.Translation;
                        source.Specular = light.Specular;

                        lightSources.Add(source);

                        // If there are already 3 lights, then skip other lights
                        if (lightSources.Count >= MaxLights)
                            break;
                    }
                }
            }

            ambientLightColor.SetValue(ambient);
            numLights.SetValue(lightSources.Count);

            if (lightSources.Count > 0)
            {
                for (int i = 0; i < lightSources.Count; i++)
                {
                    lights.Elements[i].StructureMembers["direction"].SetValue(lightSources[i].Direction);
                    lights.Elements[i].StructureMembers["color"].SetValue(lightSources[i].Diffuse);
                }
            }
        }

        /// <summary>
        /// This shader does not support particle effect.
        /// </summary>
        /// <param name="particleEffect"></param>
        public virtual void SetParameters(ParticleEffect particleEffect)
        {
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


        public void UpdateBones(Matrix[] updatedBones)
        {
            int k = updatedBones.Length;
            Matrix[] temp = new Matrix[k];
            for (int i = 0; i < k; i++)
            {
                temp[i] = updatedBones[i];
            }
            bones.SetValue(temp);
        }


        #region IShader Members

        public virtual void Render(Matrix worldMatrix, String techniqueName,
            RenderHandler renderDelegate)
        {
            if (techniqueName == null)
                throw new GoblinException("techniqueName is null");
            if (renderDelegate == null)
                throw new GoblinException("renderDelegate is null");

            this.SetValue(world, ref lastUsedWorldMatrix, worldMatrix);
            this.SetValue(view, ref lastUsedViewMatrix, State.ViewMatrix);
            this.SetValue(projection, ref lastUsedProjMatrix, State.ProjectionMatrix);
            
            // Start shader
            effect.CurrentTechnique = effect.Techniques[techniqueName];
            try
            {
                BlendFunction origBlendFunc = State.Device.RenderState.BlendFunction;
                Blend origDestBlend = State.Device.RenderState.DestinationBlend;
                Blend origSrcBlend = State.Device.RenderState.SourceBlend;
                CompareFunction origDepthFunc = State.Device.RenderState.DepthBufferFunction;
                bool origAlphaEnable = State.Device.RenderState.AlphaBlendEnable;
                bool origDepthWriteEnable = State.Device.RenderState.DepthBufferWriteEnable;

                State.Device.RenderState.BlendFunction = BlendFunction.Add;
                State.Device.RenderState.DestinationBlend = Blend.One;
                State.Device.RenderState.SourceBlend = Blend.One;
                State.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
                State.Device.RenderState.DepthBufferEnable = true;

                effect.Begin(SaveStateMode.None);


                State.Device.RenderState.DepthBufferWriteEnable = true;
                State.Device.RenderState.AlphaBlendEnable = false;

                // Render all passes (usually just one)
                //foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                for (int num = 0; num < effect.CurrentTechnique.Passes.Count; num++)
                {
                    EffectPass pass = effect.CurrentTechnique.Passes[num];

                    pass.Begin();
                    renderDelegate();
                    pass.End();
                    
                }
                State.Device.RenderState.AlphaBlendEnable = true;
                State.Device.RenderState.DepthBufferWriteEnable = false;


                State.Device.RenderState.BlendFunction = origBlendFunc;
                State.Device.RenderState.DestinationBlend = origDestBlend;
                State.Device.RenderState.SourceBlend = origSrcBlend;
                State.Device.RenderState.DepthBufferFunction = origDepthFunc;
                State.Device.RenderState.AlphaBlendEnable = origAlphaEnable;
                State.Device.RenderState.DepthBufferWriteEnable = origDepthWriteEnable;
            }
            catch (Exception exp)
            {
                Log.Write(exp.Message);
            }
            finally
            {
                // End shader
                effect.End();
            }
        }

        public virtual void Dispose()
        {
            if (effect != null)
                effect.Dispose();
        }

        #endregion
    }
}
