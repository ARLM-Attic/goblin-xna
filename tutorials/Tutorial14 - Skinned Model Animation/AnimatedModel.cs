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
 * Notes:  The shadow mapping part is not working correctly due to skin mesh transformation
 *         that is computed in the shader
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GoblinXNA;
using GoblinXNA.Graphics;
using GoblinXNA.Shaders;
using GoblinXNA.Helpers;
using GoblinXNA.Physics;

using SkinnedModelPipeline.SkinnedModel;

namespace Tutorial14___Skinned_Model_Animation
{
    public class AnimatedModel : IModel, IPhysicsMeshProvider
    {
        #region Fields

        /// <summary>
        /// The speed and direction of the animation.  +fwd, -reverse
        /// </summary>
        private float animationSpeedDirection = 1.0f;
        
        /// <summary>
        /// A holder for the last SpeedDir when animation is stopped.
        /// </summary>
        private float lastSpeedDir;

        private AnimationPlayer animationPlayer;
        private SkinningData skinningData;
        private Microsoft.Xna.Framework.Graphics.Model skinnedModel;
        
        protected bool enabled;
        protected bool useLighting;
        protected bool castShadow;
        protected bool receiveShadow;
        
        protected ModelMeshCollection mesh;
        protected BoundingBox boundingBox;
        protected BoundingSphere boundingSphere;
        protected bool showBoundingBox;
        protected Matrix offsetTransform;
        protected bool offsetToOrigin;
        protected int triangleCount;

        protected IShader shader;
        protected String technique;
        protected Matrix[] transforms;
        protected List<Vector3> vertices;
        protected List<int> indices;
        protected bool useInternalMaterials;

        protected String modelLoaderName;
        protected String resourceName;
        protected String shaderName;
        protected String primitiveShapeParameters;

        #region For Drawing Bounding Box

        protected bool boundingBoxInitialized;
        protected VertexPositionColor[] verts;
        protected short[] indecies;

        #endregion

        protected Matrix tmpMat1;
        protected Matrix tmpMat2;
        protected Vector3 tmpVec1;
        #endregion

        #region Constructors

        /// <summary>
        /// Creates a model with both information loaded from a model file and mesh defined
        /// using VertexBuffer and IndexBuffer
        /// </summary>
        /// <param name="transforms">Transforms applied to each model meshes</param>
        /// <param name="mesh">A collection of model meshes</param>
        /// <param name="model"></param>
        public AnimatedModel(Microsoft.Xna.Framework.Graphics.Model aModel)
        {  
            this.mesh = aModel.Meshes;
            this.skinnedModel = aModel;
            
            offsetTransform = Matrix.Identity;
            offsetToOrigin = false;

            vertices = new List<Vector3>();
            indices = new List<int>();

            skinnedModel = aModel;

            // Look up our custom skinning information.
            skinningData = aModel.Tag as SkinningData;

            if (skinningData == null)
                throw new GoblinException("This model does not contain a SkinningData tag.");

            // Create an animation player, and start decoding an animation clip.
            animationPlayer = new AnimationPlayer(skinningData);
            //int z =animationPlayer.GetBoneTransforms().Length;
            

            Matrix[] newBones = animationPlayer.GetBoneTransforms();
            int k = animationPlayer.GetBoneTransforms().Length;

            this.transforms = new Matrix[k];

            for (int i = 0; i < k; i++)
                this.transforms[i] = newBones[i];

            CalculateMinimumBoundingBox();
            
            //The text you pass in here needs to match the .fx shader file 
            shader = new SkinnedModelShader("SkinnedModel");

            enabled = true;
            useInternalMaterials = false;
            useLighting = true;
            castShadow = false;
            receiveShadow = false;
            showBoundingBox = false;
            boundingBoxInitialized = false;

            resourceName = "";
            primitiveShapeParameters = "";
            shaderName = "SkinnedModel";
            modelLoaderName = "AnimatedModelLoader";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Flag indicating whether model is enabled and should be rendered. The default value is true.
        /// </summary>
        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        /// <summary>
        /// Flag reflecting whether lighting should be used when rendering this model. 
        /// The default value is true.
        /// </summary>
        public bool UseLighting
        {
            get { return useLighting; }
            set { useLighting = value; }
        }

        /// <summary>
        /// Gets or sets whether to use the material setting set inside the model file.
        /// The default value is false.
        /// </summary>
        public bool UseInternalMaterials
        {
            get { return useInternalMaterials; }
            set { useInternalMaterials = value; }
        }

        /// <summary>
        /// Gets or sets if this model can cast shadows on other objects that can receive shadows.
        /// The default value is false.
        /// </summary>
        public bool CastShadows
        {
            get { return castShadow; }
            set { castShadow = value; }
        }

        /// <summary>
        /// Gets or sets if this model can receive shadows cast by objects that can cast shadows.
        /// The default value is false.
        /// </summary>
        public bool ReceiveShadows
        {
            get { return receiveShadow; }
            set { receiveShadow = value; }
        }

        /// <summary>
        /// Gets or sets the shader to use for rendering this model.
        /// </summary>
        public IShader Shader
        {
            get { return shader; }
            set { shader = value; }
        }

        /// <summary>
        /// Gets or sets the shader technique to use.
        /// </summary>
        public String ShaderTechnique
        {
            get { return technique; }
            set { technique = value; }
        }

        /// <summary>
        /// Gets the skinned model.
        /// </summary>
        public Microsoft.Xna.Framework.Graphics.Model SkinnedModel
        {
            get { return skinnedModel; }
        }

        /// <summary>
        /// Gets the minimum bounding box used for display and by the physics engine.
        /// </summary>
        public BoundingBox MinimumBoundingBox
        {
            get { return boundingBox; }
        }

        /// <summary>
        /// Gets the minimum bounding sphere that bounds this model mesh
        /// </summary>
        public BoundingSphere MinimumBoundingSphere
        {
            get { return boundingSphere; }
        }

        public List<Vector3> Vertices
        {
            get { return vertices; }
        }

        public List<int> Indices
        {
            get { return indices; }
        }

        public PrimitiveType PrimitiveType
        {
            get { return PrimitiveType.TriangleList; }
        }

        /// <summary>
        /// Gets the offset transformation from the origin of the world coordinate.
        /// </summary>
        /// <remarks>
        /// If not provided, this transform will be calculated based on the following equation:
        /// OffsetTranslation.Translation = (MinimumBoundingBox.Min + MinimumBoundingBox.Max) / 2.
        /// In this case, no rotation offset will be calculated.
        /// </remarks>
        public Matrix OffsetTransform
        {
            get { return offsetTransform; }
            internal set { offsetTransform = value; }
        }

        /// <summary>
        /// Gets or sets whether to relocate the model to the origin. Each model has its 
        /// position stored in the model file, but if you want to relocate the model to the 
        /// origin instead of locating it based on the position stored in the file, you should
        /// set this to true. The default value is false.
        /// </summary>
        public bool OffsetToOrigin
        {
            get { return offsetToOrigin; }
            set { offsetToOrigin = value; }
        }

        /// <summary>
        /// Gets or sets whether to draw the minimum bounding box around the model.
        /// The default value is false.
        /// </summary>
        public bool ShowBoundingBox
        {
            get { return showBoundingBox; }
            set
            {
                if (showBoundingBox == value)
                    return;

                if (value && !boundingBox.Equals(new BoundingBox()) &&
                    !boundingBoxInitialized)
                {
                    GenerateBoundingBox(boundingBox.Min, boundingBox.Max);
                }

                showBoundingBox = value;
            }
        }

        /// <summary>
        /// Gets the triangle count of this model
        /// </summary>
        public int TriangleCount
        {
            get { return triangleCount; }
        }

        public AnimationPlayer AnimationPlayer 
        {
            get { return animationPlayer; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the minimum bounding box that fits this model.
        /// </summary>
        protected virtual void CalculateMinimumBoundingBox()
        {
            triangleCount = 0;

            bool needTransform = false;
            if (mesh != null)
            {                
                foreach (ModelMesh modelMesh in mesh)
                {
                    needTransform = !transforms[modelMesh.ParentBone.Index].Equals(Matrix.Identity);

                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        Vector3[] data = new Vector3[part.NumVertices];

                        modelMesh.VertexBuffer.GetData<Vector3>(part.StreamOffset + part.BaseVertex * part.VertexStride,
                            data, 0, part.NumVertices, part.VertexStride);

                        for (int ndx = 0; ndx < data.Length; ndx++)
                        {
                            if (needTransform)
                                Vector3.Transform(ref data[ndx], ref transforms[modelMesh.ParentBone.Index],
                                    out data[ndx]);
                        }

                        triangleCount += part.PrimitiveCount;

                        vertices.AddRange(data);

                        int[] tmpIndices = new int[part.PrimitiveCount * 3];

                        if (modelMesh.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
                        {
                            short[] tmp = new short[part.PrimitiveCount * 3];
                            modelMesh.IndexBuffer.GetData<short>(part.StartIndex * 2, tmp, 0,
                                tmp.Length);
                            Array.Copy(tmp, 0, tmpIndices, 0, tmpIndices.Length);
                        }
                        else
                            modelMesh.IndexBuffer.GetData<int>(part.StartIndex * 2, tmpIndices, 0,
                                tmpIndices.Length);

                        if (part.BaseVertex != 0)
                            for (int i = 0; i < tmpIndices.Length; i++)
                                tmpIndices[i] += part.BaseVertex;

                        indices.AddRange(tmpIndices);
                    }
                }
            }

            if (vertices.Count == 0)
            {
                boundingBox = new BoundingBox();
                boundingSphere = new BoundingSphere();
            }
            else
            {
                boundingBox = BoundingBox.CreateFromPoints(vertices);
                boundingSphere = BoundingSphere.CreateFromPoints(vertices);
                if (offsetTransform.Equals(Matrix.Identity))
                    offsetTransform.Translation = Vector3.Negate((boundingBox.Min + boundingBox.Max) / 2);
            }
        }

        /// <summary>
        /// Generates the necessary mesh for drawing a minimum bounding box around this model.
        /// </summary>
        /// <param name="min">The minimum point of the minimum bounding box</param>
        /// <param name="max">The maximum point of the minimum bounding box</param>
        protected virtual void GenerateBoundingBox(Vector3 min, Vector3 max)
        {
            Vector3 minMaxZ = new Vector3(min.X, min.Y, max.Z);
            Vector3 minMaxX = new Vector3(max.X, min.Y, min.Z);
            Vector3 minMaxY = new Vector3(min.X, max.Y, min.Z);
            Vector3 maxMinX = new Vector3(min.X, max.Y, max.Z);
            Vector3 maxMinY = new Vector3(max.X, min.Y, max.Z);
            Vector3 maxMinZ = new Vector3(max.X, max.Y, min.Z);

            verts = new VertexPositionColor[8];
            verts[0] = new VertexPositionColor(min, ColorHelper.Empty);
            verts[1] = new VertexPositionColor(max, ColorHelper.Empty);
            verts[2] = new VertexPositionColor(minMaxX, ColorHelper.Empty);
            verts[3] = new VertexPositionColor(minMaxY, ColorHelper.Empty);
            verts[4] = new VertexPositionColor(minMaxZ, ColorHelper.Empty);
            verts[5] = new VertexPositionColor(maxMinX, ColorHelper.Empty);
            verts[6] = new VertexPositionColor(maxMinY, ColorHelper.Empty);
            verts[7] = new VertexPositionColor(maxMinZ, ColorHelper.Empty);

            indecies = new short[24];

            indecies[0] = 0; indecies[1] = 2;
            indecies[2] = 0; indecies[3] = 3;
            indecies[4] = 0; indecies[5] = 4;
            indecies[6] = 1; indecies[7] = 5;
            indecies[8] = 1; indecies[9] = 6;
            indecies[10] = 1; indecies[11] = 7;
            indecies[12] = 3; indecies[13] = 5;
            indecies[14] = 3; indecies[15] = 7;
            indecies[16] = 4; indecies[17] = 5;
            indecies[18] = 4; indecies[19] = 6;
            indecies[20] = 2; indecies[21] = 6;
            indecies[22] = 2; indecies[23] = 7;

            boundingBoxInitialized = true;
        }

        /// <summary>
        /// Copies only the geometry (Mesh, PrimitiveMesh, AnimatedMesh, 
        /// MinimumBoundingBox, MinimumBoundingSphere, TriangleCount and Transforms)
        /// </summary>
        /// <param name="model">A source model from which to copy</param>
        public virtual void CopyGeometry(IModel model)
        {
            if (!(model is AnimatedModel))
                return;

            AnimatedModel srcModel = (AnimatedModel)model;
            skinnedModel = srcModel.SkinnedModel;

            // Look up our custom skinning information.
            skinningData = srcModel.skinnedModel.Tag as SkinningData;

            if (skinningData == null)
                throw new GoblinException("This model does not contain a SkinningData tag.");

            // Create an animation player, and start decoding an animation clip.
            animationPlayer = new AnimationPlayer(skinningData);

            triangleCount = srcModel.TriangleCount;
            boundingBox = srcModel.MinimumBoundingBox;
            boundingSphere = srcModel.MinimumBoundingSphere;
            UseInternalMaterials = srcModel.UseInternalMaterials;
        }

        /// <summary>
        /// Renders the model itself as well as the minimum bounding box if showBoundingBox
        /// is true. By default, SimpleEffectShader is used to render the model.
        /// </summary>
        /// <remarks>
        /// This function is called automatically to render the model, so do not call this method
        /// </remarks>
        /// <param name="material">Material properties of this model</param>
        /// <param name="renderMatrix">Transform of this model</param>
        public virtual void Render(Matrix renderMatrix, Material material)
        {
            if (!UseInternalMaterials)
            {
                material.InternalEffect = null;
                shader.SetParameters(material);
            }
                        
            //Update the bones
            ((SkinnedModelShader)shader).UpdateBones(transforms);

            // Render the actual model
            if (mesh != null)
            {
                foreach (ModelMesh modelMesh in this.mesh)
                {
                    Matrix.Multiply(ref transforms[modelMesh.ParentBone.Index], ref renderMatrix, out tmpMat1);

                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        if (UseInternalMaterials)
                        {
                            material.InternalEffect = part.Effect;                            
                            shader.SetParameters(material);
                            ((SkinnedModelShader)shader).UpdateBones(this.transforms);
                        }

                        shader.Render(
                            tmpMat1,
                            technique,
                            delegate
                            {
                                State.Device.Vertices[0].SetSource(
                                    modelMesh.VertexBuffer, part.StreamOffset, part.VertexStride);
                                State.Device.Indices = modelMesh.IndexBuffer;
                                State.Device.VertexDeclaration = part.VertexDeclaration;
                                State.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                    part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);

                            });                        
                    }
                }
            }

            if (showBoundingBox)
            {
                State.BoundingBoxShader.Render(
                    renderMatrix,
                    "",
                    delegate
                    {
                        State.Device.DrawUserIndexedPrimitives<VertexPositionColor>(
                            PrimitiveType.LineList, verts, 0, 8, indecies, 0, 12);
                    });
            }
        }

        /// <summary>
        /// Disposes of model contents.
        /// </summary>
        public virtual void Dispose()
        {
            vertices.Clear();
            mesh = null;
            shader = null;
        }

        /// <summary>
        /// Renders all objects that should receive shadows here. Called from the 
        /// ShadowMappingShader.UseShadow method.
        /// </summary>
        /// <remarks>
        /// This function is called automatically to receive shadows, so do not call this method
        /// </remarks>
        /// <param name="renderMatrix">Transform of this model</param>
        public virtual void UseShadows(Matrix renderMatrix)
        {
            // The model only receives shadow if receiveShadow is set to true
            if (!receiveShadow)
                return;

            Matrix worldMatrix;

            if (mesh != null)
            {
                foreach (ModelMesh modelMesh in this.mesh)
                {
                    worldMatrix = transforms[modelMesh.ParentBone.Index] * renderMatrix;
                    State.ShadowShader.UpdateCalcShadowWorldMatrix(worldMatrix);

                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        State.Device.Vertices[0].SetSource(
                            modelMesh.VertexBuffer, part.StreamOffset, part.VertexStride);
                        State.Device.Indices = modelMesh.IndexBuffer;
                        State.Device.VertexDeclaration = part.VertexDeclaration;
                        State.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                            part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
        }

        /// <summary>
        /// Generate shadows for this model in the generate shadows pass
        /// of our shadow mapping shader. All objects rendered here will
        /// cast shadows in our scene.
        /// </summary>
        /// <remarks>
        /// This function is called automatically to cast shadows, so do not call this method
        /// </remarks>
        /// <param name="renderMatrix">Transform of this model</param>
        public virtual void GenerateShadows(Matrix renderMatrix)
        {
            if (!castShadow)
                return;

            Matrix worldMatrix;

            if (mesh != null)
            {
                foreach (ModelMesh modelMesh in this.mesh)
                {
                    worldMatrix = transforms[modelMesh.ParentBone.Index] * renderMatrix;
                    State.ShadowShader.UpdateGenerateShadowWorldMatrix(worldMatrix);

                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        State.Device.Vertices[0].SetSource(
                            modelMesh.VertexBuffer, part.StreamOffset, part.VertexStride);
                        State.Device.Indices = modelMesh.IndexBuffer;
                        State.Device.VertexDeclaration = part.VertexDeclaration;
                        State.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                            part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the animation speed and direction.  +fwd, -rev
        /// </summary>
        public float AnimationSpeedDirection
        {
            get { return animationSpeedDirection; }
            set
            {
                //Are we stopped?
                if (animationSpeedDirection == 0)
                {
                    lastSpeedDir = value;
                }
                else
                {
                    animationSpeedDirection = value;
                    lastSpeedDir = value;
                }
            }
        }

        /// <summary>
        /// Stop (pause) the animation.
        /// </summary>
        public void Stop()
        {
            if (animationSpeedDirection != 0)
            {
                lastSpeedDir = animationSpeedDirection;
                animationSpeedDirection = 0;
            }
        }

        /// <summary>
        /// Start (play) the animation
        /// </summary>
        public void Start()
        {
            animationSpeedDirection = lastSpeedDir;
        }

        /// <summary>
        /// Rewind the anmation
        /// </summary>
        public void Rewind()
        {
            animationPlayer.RewindCurrentClip();
        }

        /// <summary>
        /// Helper class to get all the animation clip names in the model.
        /// </summary>
        /// <returns></returns>
        public string DumpClipNames()
        {
            string result = "";

            foreach (KeyValuePair<string, AnimationClip> kvp in skinningData.AnimationClips)
            {
                result = result + kvp.Key + ":";
            }
            return result;
        }

        /// <summary>
        /// Load the specified clip and play it.  Started by default
        /// to facilitate debugging.
        /// 
        /// If you don't want to play it after it's loaded,
        /// call Stop() immedietly.
        /// </summary>
        /// <param name="clipName">The name of the clip (e.g. "Take 001" in dude.fbx)</param>
        public void LoadAnimationClip(string clipName)
        {
            if (skinningData.AnimationClips.ContainsKey(clipName))
            {
                AnimationClip clip = skinningData.AnimationClips[clipName];
                animationPlayer.StartClip(clip);
                animationSpeedDirection = 1.0f;
                this.lastSpeedDir = animationSpeedDirection;
            }
            else
            {
                throw new GoblinException("Clip name does not exist in model.  Use DumpClipNames for a list..");
            }
        }

        /// <summary>
        /// Update the animation
        /// </summary>
        /// <param name="gt"></param>
        public void Update(GameTime gt)
        {
            long etTicks = gt.ElapsedGameTime.Ticks;
            long scaledTicks = (long)(etTicks * animationSpeedDirection);
            TimeSpan adjustedElapsedTime = new TimeSpan(scaledTicks);
            animationPlayer.Update(adjustedElapsedTime, true, Matrix.Identity);

            //Copy the bones
            Matrix[] newBones = animationPlayer.GetSkinTransforms();
            for (int i = 0; i < newBones.Length; i++)
                this.transforms[i] = newBones[i];
        }

        public virtual XmlElement SaveModelCreationInfo(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = xmlDoc.CreateElement("ModelCreationInfo");

            if (resourceName.Length == 0)
                throw new GoblinException("ResourceName must be specified in order to " +
                    "save this model information to an XML file");

            xmlNode.SetAttribute("resourceName", resourceName);
            if (Path.GetExtension(resourceName).Length > 0)
            {
                if (modelLoaderName.Length == 0)
                    throw new GoblinException("ModelLoaderName must be specified if ResourceName " +
                        "contains file extension");

                xmlNode.SetAttribute("modelLoaderName", modelLoaderName);
            }
            else
            {
                if (primitiveShapeParameters.Length == 0)
                    throw new GoblinException("PrimitiveShapeParameters must be specified if " +
                        "ResourceName does not contain a file extension");

                xmlNode.SetAttribute("primitiveShapeParameters", primitiveShapeParameters);
            }

            xmlNode.SetAttribute("shaderName", shaderName);
            if (technique.Length > 0)
                xmlNode.SetAttribute("shaderTechniqueName", technique);

            return xmlNode;
        }

        public virtual XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = xmlDoc.CreateElement("Model");

            BuildXML(xmlNode);

            return xmlNode;
        }

        public virtual void Load(XmlElement xmlNode)
        {
        }

        #endregion

        #region Protected Methods

        protected virtual void BuildXML(XmlElement xmlNode)
        {
            xmlNode.SetAttribute("enabled", enabled.ToString());
            xmlNode.SetAttribute("useLighting", useLighting.ToString());
            xmlNode.SetAttribute("castShadow", castShadow.ToString());
            xmlNode.SetAttribute("receiveShadow", receiveShadow.ToString());
            xmlNode.SetAttribute("showBoundingBox", showBoundingBox.ToString());
            xmlNode.SetAttribute("useInternalMaterials", useInternalMaterials.ToString());
            xmlNode.SetAttribute("offsetToOrigin", offsetToOrigin.ToString());
        }

        #endregion
    }
}
