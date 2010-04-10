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
 * Author: Fan Lin (linfan68@gmail.com)
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.SceneGraph;
using Model = GoblinXNA.Graphics.Model;
using GoblinXNA.Graphics.Geometry;
using GoblinXNA.Helpers;
using Texture = Microsoft.Xna.Framework.Graphics.Texture2D;
using GoblinXNA.Shaders;

namespace Tutorial11___Shader
{
    public class GeneralShader : Shader
    {
        private EffectParameter
            worldForNormal,
            cameraPosition,

            //Material Paramters
            emissiveColor,
            diffuseColor,
            specularColor,
            specularPower,
            diffuseTexture,
            diffuseTexEnabled,

            //Light paramters
            lights,
            light,
            numberOfLights,

            ambientLightColor;

        private List<LightSource> lightSources;
        private List<LightSource> dirLightSources;
        private List<LightSource> pointLightSources;
        private List<LightSource> spotLightSources;

        private int maxNumLightsPerPass;
        private Material material;
        private bool is_3_0;
        private bool forcePS20;

        public GeneralShader()
            : base("DirectXShader")
        {
            lightSources = new List<LightSource>();
            dirLightSources = new List<LightSource>();
            pointLightSources = new List<LightSource>();
            spotLightSources = new List<LightSource>();

            maxNumLightsPerPass = 12;
            is_3_0 = false;
            forcePS20 = true;

            if ((State.Device.GraphicsDeviceCapabilities.PixelShaderVersion.Major >= 3) && ! forcePS20)
            {
                is_3_0 = true;
            }
            else
            {
                is_3_0 = false;
            }
        }

        /// <summary>
        /// Gets or sets whether to force this shader to use Pixel Shader 2.0 profile even if
        /// the graphics card support Pixel Shader 3.0. By default, this is set to true.
        /// </summary>
        /// <remarks>
        /// When there are less than fifty lights, it's faster to use Pixel Shader 2.0 profile.
        /// However, if you have more than fifty lights, Pixel Shader 3.0 will perform better.
        /// </remarks>
        public bool UsePS20
        {
            get { return forcePS20; }
            set { forcePS20 = value; }
        }

        public override int MaxLights
        {
            get { return 1000; }
        }

        protected override void GetParameters()
        {
            //Binding the effect parameters in to Effect File;

            // Geometry
            world = effect.Parameters["world"];
            viewProj = effect.Parameters["viewProjection"];
            worldForNormal = effect.Parameters["worldForNormal"];
            cameraPosition = effect.Parameters["cameraPosition"];

            // Material
            emissiveColor = effect.Parameters["emissiveColor"];
            diffuseColor = effect.Parameters["diffuseColor"];
            specularColor = effect.Parameters["specularColor"];
            specularPower = effect.Parameters["specularPower"];
            diffuseTexture = effect.Parameters["diffuseTexture"];
            diffuseTexEnabled = effect.Parameters["diffuseTexEnabled"];

            // Lights
            lights = effect.Parameters["lights"];
            light = effect.Parameters["light"];
            ambientLightColor = effect.Parameters["ambientLightColor"];
            numberOfLights = effect.Parameters["numberOfLights"];
        }

        public override void SetParameters(Material material)
        {
            this.material = material;
            emissiveColor.SetValue(material.Emissive);
            diffuseColor.SetValue(material.Diffuse);
            specularColor.SetValue(material.Specular);
            specularPower.SetValue(material.SpecularPower);
            if (material.HasTexture)
            {
                diffuseTexEnabled.SetValue(true);
                diffuseTexture.SetValue(material.Texture);
            }
            else
            {
                diffuseTexEnabled.SetValue(false);
            }
        }

        public override void SetParameters(List<LightNode> globalLights, List<LightNode> localLights)
        {
            bool ambientSet = false;
            this.lightSources.Clear();
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

                foreach (LightSource light in lNode.LightSources)
                {
                    // skip the light source if not enabled
                    if (!light.Enabled)
                        continue;

                    LightSource source = new LightSource(light);
                    if (light.Type != LightType.Directional)
                        source.Position = ((Matrix)(lNode.WorldTransformation *
                            Matrix.CreateTranslation(light.Position))).Translation;
                    if (light.Type != LightType.Point)
                        source.Direction = ((Matrix)(Matrix.CreateTranslation(light.Direction) *
                            MatrixHelper.GetRotationMatrix(lNode.WorldTransformation))).Translation;

                    lightSources.Add(source);
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

                foreach (LightSource light in lNode.LightSources)
                {
                    // skip the light source if not enabled
                    if (!light.Enabled)
                        continue;

                    LightSource source = new LightSource(light);
                    if (light.Type != LightType.Directional)
                        source.Position = ((Matrix)(lNode.WorldTransformation *
                            Matrix.CreateTranslation(light.Position))).Translation;
                    if (light.Type != LightType.Point)
                        source.Direction = ((Matrix)(Matrix.CreateTranslation(light.Direction) *
                            MatrixHelper.GetRotationMatrix(lNode.WorldTransformation))).Translation;

                    lightSources.Add(source);
                }
            }

            dirLightSources.Clear();
            pointLightSources.Clear();
            spotLightSources.Clear();
            foreach (LightSource l in lightSources)
            {
                switch (l.Type)
                {
                    case LightType.Directional:
                        dirLightSources.Add(l);
                        break;
                    case LightType.Point:
                        pointLightSources.Add(l);
                        break;
                    case LightType.SpotLight:
                        spotLightSources.Add(l);
                        break;
                }
            }
            this.ambientLightColor.SetValue(ambientLightColor);
        }

        public override void Render(Matrix worldMatrix, string techniqueName, RenderHandler renderDelegate)
        {
            if (renderDelegate == null)
                throw new GoblinException("renderDelegate is null");

            world.SetValue(worldMatrix);
            cameraPosition.SetValue(Vector3.Transform(Vector3.Zero, Matrix.Invert(State.ViewMatrix)));
            viewProj.SetValue(State.ViewMatrix * State.ProjectionMatrix);
            worldForNormal.SetValue(Matrix.Transpose(Matrix.Invert(worldMatrix)));

            // Start shader
            effect.CurrentTechnique = effect.Techniques["GeneralLighting"];

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

                effect.Begin();

                State.Device.RenderState.DepthBufferWriteEnable = true;
                State.Device.RenderState.AlphaBlendEnable = false;
                effect.CurrentTechnique.Passes["Ambient"].Begin();
                renderDelegate();
                effect.CurrentTechnique.Passes["Ambient"].End();

                State.Device.RenderState.AlphaBlendEnable = true;
                State.Device.RenderState.DepthBufferWriteEnable = false;
               // System.Diagnostics.Debug.Assert(false);

                if (is_3_0)
                {
                    DoRendering30(renderDelegate, dirLightSources);
                    DoRendering30(renderDelegate, pointLightSources);
                    DoRendering30(renderDelegate, spotLightSources);
                }
                else
                {
                    DoRendering20(renderDelegate, dirLightSources);
                    DoRendering20(renderDelegate, pointLightSources);
                    DoRendering20(renderDelegate, spotLightSources);
                }
                effect.End();

                State.Device.RenderState.BlendFunction = origBlendFunc;
                State.Device.RenderState.DestinationBlend = origDestBlend;
                State.Device.RenderState.SourceBlend = origSrcBlend;
                State.Device.RenderState.DepthBufferFunction = origDepthFunc;
                State.Device.RenderState.AlphaBlendEnable = origAlphaEnable;
                State.Device.RenderState.DepthBufferWriteEnable = origDepthWriteEnable;
            }
        }

        private void DoRendering30(RenderHandler renderDelegate, List<LightSource> lightSources)
        {
            string passName;
            if (lightSources.Count == 0)
            {
                return;
            }
            else
            {
                passName = "Multiple" + GetPassName(lightSources[0].Type);
            }
            for (int passCount = 0; passCount < (((lightSources.Count - 1) / maxNumLightsPerPass) + 1); passCount++)
            {

                int count = 0;
                for (int l = 0; l < maxNumLightsPerPass; l++)
                {
                    int lightIndex = (passCount * maxNumLightsPerPass) + l;
                    if (lightIndex >= lightSources.Count)
                    {
                        break;
                    }

                    SetUpLightSource(lightSources[lightIndex], l);
                    count++;
                }
               
                numberOfLights.SetValue(count);
                
                effect.CurrentTechnique.Passes[passName].Begin();
                renderDelegate();
                effect.CurrentTechnique.Passes[passName].End();
            }
        }

        private void DoRendering20(RenderHandler renderDelegate, List<LightSource> lightSources)
        {
            string passName;
            if (lightSources.Count == 0)
            {
                return;
            }
            else
            {
                passName = "Single" + GetPassName(lightSources[0].Type);
            }
            for (int passCount = 0; passCount < lightSources.Count; passCount++)
            {
                SetUpSingleLightSource(lightSources[passCount]);
                effect.CurrentTechnique.Passes[passName].Begin();
                renderDelegate();
                effect.CurrentTechnique.Passes[passName].End();
            }
        }
        private void SetUpLightSource(LightSource lightSource, int index)
        {
            lights.Elements[index].StructureMembers["direction"].SetValue(lightSource.Direction);
            lights.Elements[index].StructureMembers["position"].SetValue(lightSource.Position);
            lights.Elements[index].StructureMembers["falloff"].SetValue(lightSource.Falloff);
            lights.Elements[index].StructureMembers["range"].SetValue(lightSource.Range);
            lights.Elements[index].StructureMembers["color"].SetValue(lightSource.Diffuse);
            lights.Elements[index].StructureMembers["attenuation0"].SetValue(lightSource.Attenuation0);
            lights.Elements[index].StructureMembers["attenuation1"].SetValue(lightSource.Attenuation1);
            lights.Elements[index].StructureMembers["attenuation2"].SetValue(lightSource.Attenuation2);
            lights.Elements[index].StructureMembers["innerConeAngle"].SetValue(lightSource.InnerConeAngle);
            lights.Elements[index].StructureMembers["outerConeAngle"].SetValue(lightSource.OuterConeAngle);
        }
        private void SetUpSingleLightSource(LightSource lightSource)
        {
            light.StructureMembers["direction"].SetValue(lightSource.Direction);
            light.StructureMembers["position"].SetValue(lightSource.Position);
            light.StructureMembers["falloff"].SetValue(lightSource.Falloff);
            light.StructureMembers["range"].SetValue(lightSource.Range);
            light.StructureMembers["color"].SetValue(lightSource.Diffuse);
            light.StructureMembers["attenuation0"].SetValue(lightSource.Attenuation0);
            light.StructureMembers["attenuation1"].SetValue(lightSource.Attenuation1);
            light.StructureMembers["attenuation2"].SetValue(lightSource.Attenuation2);
            light.StructureMembers["innerConeAngle"].SetValue(lightSource.InnerConeAngle);
            light.StructureMembers["outerConeAngle"].SetValue(lightSource.OuterConeAngle);
        }

        static private int compareLightSource(LightSource l1, LightSource l2)
        {
            if ((int)l1.Type > (int)l2.Type)
            {
                return 1;
            }
            else if (l1.Type == l2.Type)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        private string GetPassName(LightType type)
        {
            switch (type)
            {
                case LightType.Directional:
                    return "DirectionalLight";
                case LightType.Point:
                    return "PointLight";
                case LightType.SpotLight:
                    return "SpotLight";
            }
            return null;
        }
    }
}
