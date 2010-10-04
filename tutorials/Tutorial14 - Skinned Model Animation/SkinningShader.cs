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
    public class SkinnedModelShader : Shader
    {
        #region Member Fields

        /// <summary>
        /// Defines some of the commonly used effect parameters in shader files.
        /// </summary>
        protected EffectParameter 
            bones,
            view,
            texture,
            ambientLightColor,
            lights,
            numLights;

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
            : base(shaderName)
        {
            lightSources = new List<LightSource>();
        }

        #endregion

        #region Overriden Properties

        public override int MaxLights
        {
            get { return 3; }
        }

        #endregion

        #region Overriden Methods

        /// <summary>
        /// Loads the effect parameters from the loaded shader file.
        /// </summary>
        protected override void GetParameters()
        {
            // Geometry
            world = effect.Parameters["World"];
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
        /// This shader does not support material effect.
        /// </summary>
        /// <param name="material"></param>
        public override void SetParameters(Material material)
        {
            if (material.InternalEffect != null)
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
        }

        /// <summary>
        /// This shader does not support lighting effect.
        /// </summary>
        /// <param name="lightSources"></param>
        /// <param name="ambientLightColor"></param>
        public override void SetParameters(List<LightNode> globalLights, List<LightNode> localLights)
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
                    // skip the light source if not enabled or not a directional light
                    if (!lNode.LightSource.Enabled || (lNode.LightSource.Type != LightType.Directional))
                        continue;

                    LightSource source = new LightSource();
                    source.Diffuse = lNode.LightSource.Diffuse;

                    tmpVec1 = lNode.LightSource.Direction;
                    Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                    tmpMat2 = lNode.WorldTransformation;
                    MatrixHelper.GetRotationMatrix(ref tmpMat2, out tmpMat2);
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

                    source.Direction = tmpMat3.Translation;
                    source.Specular = lNode.LightSource.Specular;

                    lightSources.Add(source);

                    // If there are already 3 lights, then skip other lights
                    if (lightSources.Count >= MaxLights)
                        break;
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
                    // skip the light source if not enabled or not a directional light
                    if (!lNode.LightSource.Enabled || (lNode.LightSource.Type != LightType.Directional))
                        continue;

                    LightSource source = new LightSource();
                    source.Diffuse = lNode.LightSource.Diffuse;

                    tmpVec1 = lNode.LightSource.Direction;
                    Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                    tmpMat2 = lNode.WorldTransformation;
                    MatrixHelper.GetRotationMatrix(ref tmpMat2, out tmpMat2);
                    Matrix.Multiply(ref tmpMat1, ref tmpMat2, out tmpMat3);

                    source.Direction = tmpMat3.Translation;
                    source.Specular = lNode.LightSource.Specular;

                    lightSources.Add(source);

                    // If there are already 3 lights, then skip other lights
                    if (lightSources.Count >= MaxLights)
                        break;
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

        public override void Render(Matrix worldMatrix, String techniqueName,
            RenderHandler renderDelegate)
        {
            if (techniqueName == null)
                throw new GoblinException("techniqueName is null");
            if (renderDelegate == null)
                throw new GoblinException("renderDelegate is null");

            world.SetValue(worldMatrix);
            view.SetValue(State.ViewMatrix);
            projection.SetValue(State.ProjectionMatrix);

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

        #endregion

        #region Public Methods

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

        #endregion
    }
}
