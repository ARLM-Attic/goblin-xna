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
 * 
 *************************************************************************************/ 

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Shaders;
using GoblinXNA.Graphics;
using GoblinXNA.Helpers;
using GoblinXNA.Physics;

namespace GoblinXNA.Graphics
{
    /// <summary>
    /// This implementation is suitable for DirectX models and primitive shapes.
    /// </summary>
    public class Model : IModel, IPhysicsMeshProvider 
    {
        #region Fields

        protected bool enabled;
        protected bool useLighting;
        protected bool castShadow;
        protected bool receiveShadow;

        protected ModelMeshCollection mesh;
        protected List<ModelMesh> animatedMesh;

        protected BoundingBox boundingBox;
        protected BoundingSphere boundingSphere;
        protected bool showBoundingBox;
        protected Matrix offsetTransform;
        protected bool offsetToOrigin;
        protected int triangleCount;

        protected IShader shader;
        protected String technique;
        protected List<IShader> afterEffectShaders;
        protected Matrix[] transforms;
        protected Matrix[] animationTransforms;
        protected List<Vector3> vertices;
        protected List<int> indices;
        protected bool useInternalMaterials;

        protected String modelLoaderName;
        protected String resourceName;
        protected String shaderName;

        protected List<LineManager3D.Line> lines;
        protected List<LineManager3D.Line> renderLines;
        protected bool boundingBoxCalculated;
        protected Matrix prevRenderMatrix;

        #region Temporary Variables

        protected Matrix tmpMat1;
        protected Matrix tmpMat2;
        protected Vector3 tmpVec1;

        #endregion

        #endregion

        #region Constructors

        public Model() : this(null, null, null) { }

        /// <summary>
        /// Creates a model with information loaded from a model file
        /// </summary>
        /// <param name="transforms">Transforms applied to each model meshes</param>
        /// <param name="mesh">A collection of model meshes</param>
        /// <param name="animatedMesh">
        /// A list of animated model meshes that can be transformed by the user
        /// </param>
        public Model(Matrix[] transforms, ModelMeshCollection mesh, List<ModelMesh> animatedMesh)
        {
            this.transforms = transforms;
            this.mesh = mesh;
            this.animatedMesh = animatedMesh;
            if (animatedMesh != null)
            {
                animationTransforms = new Matrix[animatedMesh.Count];
                for (int i = 0; i < animationTransforms.Length; i++)
                    animationTransforms[i] = Matrix.Identity;
            }

            offsetTransform = Matrix.Identity;
            offsetToOrigin = false;

            vertices = new List<Vector3>();
            indices = new List<int>();

            shader = new SimpleEffectShader();
            technique = "";
            afterEffectShaders = new List<IShader>();

            resourceName = "";
            shaderName = TypeDescriptor.GetClassName(shader);
            modelLoaderName = "";

            enabled = true;
            useInternalMaterials = false;
            useLighting = true;
            castShadow = false;
            receiveShadow = false;
            showBoundingBox = false;
            boundingBoxCalculated = false;

            CalculateTriangleCount();
            CalculateMinimumBoundingSphere();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Flag indicating whether model is enabled and should be rendered. The default value is true.
        /// </summary>
        public virtual bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        /// <summary>
        /// Flag reflecting whether lighting should be used when rendering this model. 
        /// The default value is true.
        /// </summary>
        public virtual bool UseLighting
        {
            get { return useLighting; }
            set { useLighting = value; }
        }

        /// <summary>
        /// Gets or sets whether to use the material setting set inside the model file.
        /// The default value is false.
        /// </summary>
        public virtual bool UseInternalMaterials
        {
            get { return useInternalMaterials; }
            set { useInternalMaterials = value; }
        }

        /// <summary>
        /// Gets or sets if this model can cast shadows on other objects that can receive shadows.
        /// The default value is false.
        /// </summary>
        public virtual bool CastShadows
        {
            get { return castShadow; }
            set { castShadow = value; }
        }

        /// <summary>
        /// Gets or sets if this model can receive shadows cast by objects that can cast shadows.
        /// The default value is false.
        /// </summary>
        public virtual bool ReceiveShadows
        {
            get { return receiveShadow; }
            set { receiveShadow = value; }
        }

        public virtual IShader Shader
        {
            get { return shader; }
            set { shader = value; }
        }

        public virtual String ShaderTechnique
        {
            get { return technique; }
            set { technique = value; }
        }

        public virtual List<IShader> AfterEffectShaders
        {
            get { return afterEffectShaders; }
            set { afterEffectShaders = value; }
        }

        public virtual ModelMeshCollection Mesh
        {
            get { return mesh; }
        }

        public virtual List<Vector3> Vertices
        {
            get { return vertices; }
        }

        public virtual List<int> Indices
        {
            get { return indices; }
        }

        public virtual PrimitiveType PrimitiveType
        {
            get { return PrimitiveType.TriangleList; }
        }

        /// <summary>
        /// Gets a list of model meshes that can be transformed by the user.
        /// </summary>
        public virtual List<ModelMesh> AnimatedMesh
        {
            get { return animatedMesh; }
        }

        /// <summary>
        /// Gets the minimum bounding box used for display and by the physics engine.
        /// </summary>
        public virtual BoundingBox MinimumBoundingBox
        {
            get 
            {
                if (!boundingBoxCalculated)
                    CalculateMinimumBoundingBox();

                return boundingBox; 
            }
        }

        /// <summary>
        /// Gets the minimum bounding sphere that bounds this model mesh
        /// </summary>
        public virtual BoundingSphere MinimumBoundingSphere
        {
            get { return boundingSphere; }
        }

        /// <summary>
        /// Gets the offset transformation from the origin of the world coordinate.
        /// </summary>
        /// <remarks>
        /// If not provided, this transform will be calculated based on the following equation:
        /// OffsetTranslation.Translation = (MinimumBoundingBox.Min + MinimumBoundingBox.Max) / 2.
        /// In this case, no rotation offset will be calculated.
        /// </remarks>
        public virtual Matrix OffsetTransform
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
        public virtual bool OffsetToOrigin
        {
            get { return offsetToOrigin; }
            set { offsetToOrigin = value; }
        }

        /// <summary>
        /// Gets or sets whether to draw the minimum bounding box around the model.
        /// The default value is false.
        /// </summary>
        public virtual bool ShowBoundingBox
        {
            get { return showBoundingBox; }
            set 
            {
                showBoundingBox = value; 

                if (!boundingBoxCalculated)
                    CalculateMinimumBoundingBox();

                if (value && lines == null)
                    GenerateBoundingBox(boundingBox.Min, boundingBox.Max);
            }
        }

        /// <summary>
        /// Gets the triangle count of this model
        /// </summary>
        public virtual int TriangleCount
        {
            get { return triangleCount; }
        }

        /// <summary>
        /// Gets or sets the name of the resource (asset name) used to create this model. 
        /// This name should not contain any extensions.
        /// </summary>
        /// <remarks>
        /// This information is necessary for saving and loading scene graph from an XML file.
        /// </remarks>
        /// <see cref="ModelLoaderName"/>
        public virtual String ResourceName
        {
            get { return resourceName; }
            set { resourceName = value; }
        }

        /// <summary>
        /// Gets or sets the name of the model loader if this model was loaded using a specific
        /// model loader.
        /// </summary>
        /// <remarks>
        /// This information is necessary for saving and loading scene graph from an XML file if
        /// the ResourceName is specified.
        /// </remarks>
        public virtual String ModelLoaderName
        {
            get { return modelLoaderName; }
            set { modelLoaderName = value; }
        }

        /// <summary>
        /// Gets or sets the name of the shader used to illuminate this model. The default value 
        /// is SimpleEffectShader.
        /// </summary>
        /// <remarks>
        /// This information is necessary for saving and loading scene graph from an XML file.
        /// </remarks>
        public virtual String ShaderName
        {
            get { return shaderName; }
            set { shaderName = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the minimum bounding sphere used for visibility testing against view frustum.
        /// </summary>
        protected virtual void CalculateMinimumBoundingSphere()
        {
            bool needTransform = false;

            foreach (ModelMesh modelMesh in mesh)
            {
                if(transforms != null)
                    needTransform = !transforms[modelMesh.ParentBone.Index].Equals(Matrix.Identity);

                if (needTransform)
                {
                    tmpMat1 = Matrix.CreateTranslation(modelMesh.BoundingSphere.Center);
                    Matrix.Multiply(ref tmpMat1, ref transforms[modelMesh.ParentBone.Index],
                        out tmpMat2);
                    BoundingSphere bSphere = new BoundingSphere(tmpMat2.Translation,
                        modelMesh.BoundingSphere.Radius);

                    if (boundingSphere.Radius == 0)
                        boundingSphere = bSphere;
                    else
                        BoundingSphere.CreateMerged(ref boundingSphere, ref bSphere,
                            out boundingSphere);
                }
                else
                {
                    if (boundingSphere.Radius == 0)
                        boundingSphere = modelMesh.BoundingSphere;
                    else
                        boundingSphere = BoundingSphere.CreateMerged(boundingSphere, 
                            modelMesh.BoundingSphere);
                }
            }
        }

        /// <summary>
        /// Calcuates the triangle count of this model.
        /// </summary>
        protected virtual void CalculateTriangleCount()
        {
            triangleCount = 0;

            foreach (ModelMesh modelMesh in mesh)
            {
                foreach (ModelMeshPart part in modelMesh.MeshParts)
                {
                    triangleCount += part.PrimitiveCount;
                }
            }
        }

        /// <summary>
        /// Calculates the minimum bounding box that fits this model.
        /// </summary>
        protected virtual void CalculateMinimumBoundingBox()
        {
            bool needTransform = false;

            foreach (ModelMesh modelMesh in mesh)
            {
                if(transforms != null)
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

            if (vertices.Count == 0)
            {
                throw new GoblinException("Corrupted model vertices. Failed to calculate MBB.");
            }
            else
            {
                boundingBox = BoundingBox.CreateFromPoints(vertices);
                boundingSphere = BoundingSphere.CreateFromPoints(vertices);
                if(offsetTransform.Equals(Matrix.Identity))
                    offsetTransform.Translation = (boundingBox.Min + boundingBox.Max) / 2;
                boundingBoxCalculated = true;
            }
        }

        /// <summary>
        /// Generates the necessary mesh for drawing a minimum bounding box around this model.
        /// </summary>
        /// <param name="min">The minimum point of the minimum bounding box</param>
        /// <param name="max">The maximum point of the minimum bounding box</param>
        protected virtual void GenerateBoundingBox(Vector3 min, Vector3 max)
        {
            Vector3 minMaxZ = Vector3Helper.Get(min.X, min.Y, max.Z);
            Vector3 minMaxX = Vector3Helper.Get(max.X, min.Y, min.Z);
            Vector3 minMaxY = Vector3Helper.Get(min.X, max.Y, min.Z);
            Vector3 maxMinX = Vector3Helper.Get(min.X, max.Y, max.Z);
            Vector3 maxMinY = Vector3Helper.Get(max.X, min.Y, max.Z);
            Vector3 maxMinZ = Vector3Helper.Get(max.X, max.Y, min.Z);

            lines = new List<LineManager3D.Line>();

            lines.Add(new LineManager3D.Line(min, minMaxX, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(min, minMaxY, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(min, minMaxZ, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(max, maxMinX, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(max, maxMinY, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(max, maxMinZ, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(minMaxY, maxMinX, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(minMaxY, maxMinZ, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(minMaxZ, maxMinX, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(minMaxZ, maxMinY, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(minMaxX, maxMinY, State.BoundingBoxColor));
            lines.Add(new LineManager3D.Line(minMaxX, maxMinZ, State.BoundingBoxColor));

            renderLines = new List<LineManager3D.Line>();
            renderLines.AddRange(lines);
        }

        /// <summary>
        /// Copies only the geometry (Mesh, AnimatedMesh, 
        /// MinimumBoundingBox, MinimumBoundingSphere, TriangleCount and Transforms)
        /// </summary>
        /// <param name="model">A source model from which to copy</param>
        public virtual void CopyGeometry(IModel model)
        {
            if (!(model is Model))
                return;

            Model srcModel = (Model)model;
            vertices.AddRange(((IPhysicsMeshProvider)model).Vertices);
            indices.AddRange(((IPhysicsMeshProvider)model).Indices);
            animatedMesh = srcModel.AnimatedMesh;
            if (animatedMesh != null)
            {
                animationTransforms = new Matrix[animatedMesh.Count];
                for (int i = 0; i < animationTransforms.Length; i++)
                    animationTransforms[i] = Matrix.Identity;
            }

            triangleCount = srcModel.TriangleCount;
            boundingBox = srcModel.MinimumBoundingBox;
            boundingSphere = srcModel.MinimumBoundingSphere;
            UseInternalMaterials = srcModel.UseInternalMaterials;
        }

        /// <summary>
        /// Updates the transforms of any animated meshes.
        /// </summary>
        /// <param name="animationTransforms">
        /// An array of transforms applied to each AnimatedMesh model mesh
        /// </param>
        public virtual void UpdateAnimationTransforms(Matrix[] animationTransforms)
        {
            if (animationTransforms.Length != this.animationTransforms.Length)
                throw new GoblinException("Number of animation transforms need to match number " +
                    "of animdatedMesh: animatedMesh.Count = " + animatedMesh.Count);

            for (int i = 0; i < animationTransforms.Length; i++)
                this.animationTransforms[i] = animationTransforms[i];
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
                if ((shader.CurrentMaterial != material) || material.HasChanged)
                {
                    shader.SetParameters(material);

                    foreach (IShader afterEffect in afterEffectShaders)
                        afterEffect.SetParameters(material);

                    material.HasChanged = false;
                }
            }

            // Render the actual model
            foreach (ModelMesh modelMesh in this.mesh)
            {
                // Skip animated mesh
                if (animatedMesh != null && animatedMesh.Contains(modelMesh))
                    continue;

                Matrix.Multiply(ref transforms[modelMesh.ParentBone.Index], ref renderMatrix, out tmpMat1);

                if (UseInternalMaterials && (shader is IAlphaBlendable))
                    ((IAlphaBlendable)shader).SetOriginalAlphas(modelMesh.Effects);

                foreach (ModelMeshPart part in modelMesh.MeshParts)
                {
                    if (UseInternalMaterials)
                    {
                        material.InternalEffect = part.Effect;
                        shader.SetParameters(material);
                    }

                    shader.Render(
                        tmpMat1,
                        (UseInternalMaterials) ? part.Effect.CurrentTechnique.Name : technique,
                        delegate
                        {
                            State.Device.Vertices[0].SetSource(
                                modelMesh.VertexBuffer, part.StreamOffset, part.VertexStride);
                            State.Device.Indices = modelMesh.IndexBuffer;
                            State.Device.VertexDeclaration = part.VertexDeclaration;
                            State.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);

                        });

                    foreach (IShader afterEffect in afterEffectShaders)
                    {
                        if (UseInternalMaterials)
                            afterEffect.SetParameters(material);

                        afterEffect.Render(
                            tmpMat1,
                            "",
                            delegate
                            {
                                State.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                    part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);

                            });
                    }
                }

            }

            if (animatedMesh != null)
            {
                Matrix worldMatrix;

                int counter = 0;
                foreach (ModelMesh modelMesh in animatedMesh)
                {
                    worldMatrix = animationTransforms[counter++] * transforms[modelMesh.ParentBone.Index] *
                        renderMatrix;

                    if (UseInternalMaterials && (shader is IAlphaBlendable))
                        ((IAlphaBlendable)shader).SetOriginalAlphas(modelMesh.Effects);

                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        if (UseInternalMaterials)
                        {
                            material.InternalEffect = part.Effect;
                            shader.SetParameters(material);
                        }

                        shader.Render(
                            worldMatrix,
                            (UseInternalMaterials) ? part.Effect.CurrentTechnique.Name : technique,
                            delegate
                            {
                                State.Device.Vertices[0].SetSource(
                                    modelMesh.VertexBuffer, part.StreamOffset, part.VertexStride);
                                State.Device.Indices = modelMesh.IndexBuffer;
                                State.Device.VertexDeclaration = part.VertexDeclaration;
                                State.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                    part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);

                            });

                        foreach (IShader afterEffect in afterEffectShaders)
                        {
                            if (UseInternalMaterials)
                                afterEffect.SetParameters(material);

                            afterEffect.Render(
                                tmpMat1,
                                "",
                                delegate
                                {
                                    State.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                                        part.BaseVertex, 0, part.NumVertices, part.StartIndex, part.PrimitiveCount);

                                });
                        }
                    }
                }
            }

            if (showBoundingBox)
                RenderBoundingBox(renderMatrix);
        }

        protected virtual void RenderBoundingBox(Matrix renderMatrix)
        {
            if (!renderMatrix.Equals(prevRenderMatrix))
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    tmpMat1 = Matrix.CreateTranslation(lines[i].startPoint);
                    Matrix.Multiply(ref tmpMat1, ref renderMatrix, out tmpMat1);

                    tmpMat2 = Matrix.CreateTranslation(lines[i].endPoint);
                    Matrix.Multiply(ref tmpMat2, ref renderMatrix, out tmpMat2);

                    renderLines[i] = new LineManager3D.Line(tmpMat1.Translation,
                        tmpMat2.Translation, lines[i].endColor);
                }

                prevRenderMatrix = renderMatrix;
            }

            foreach (LineManager3D.Line line in renderLines)
                State.LineManager.AddLine(line);
        }

        /// <summary>
        /// Disposes of model contents.
        /// </summary>
        public virtual void Dispose()
        {
            vertices.Clear();
            mesh = null;
            animatedMesh = null;

            if (shader != null)
                shader.Dispose();
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

            foreach (ModelMesh modelMesh in this.mesh)
            {
                // Skip animated mesh
                if (animatedMesh != null && animatedMesh.Contains(modelMesh))
                    continue;

                Matrix.Multiply(ref transforms[modelMesh.ParentBone.Index], ref renderMatrix, out tmpMat1);

                State.ShadowShader.UpdateCalcShadowWorldMatrix(tmpMat1);

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

            if (animatedMesh != null)
            {
                int counter = 0;
                foreach (ModelMesh modelMesh in animatedMesh)
                {
                    Matrix.Multiply(ref animationTransforms[counter++], ref transforms[modelMesh.ParentBone.Index], out tmpMat1);
                    Matrix.Multiply(ref tmpMat1, ref renderMatrix, out tmpMat2);

                    State.ShadowShader.UpdateCalcShadowWorldMatrix(tmpMat2);

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

            foreach (ModelMesh modelMesh in this.mesh)
            {
                // Skip animated mesh
                if (animatedMesh != null && animatedMesh.Contains(modelMesh))
                    continue;

                Matrix.Multiply(ref transforms[modelMesh.ParentBone.Index], ref renderMatrix, out tmpMat1);
                State.ShadowShader.UpdateGenerateShadowWorldMatrix(tmpMat1);

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

            if (animatedMesh != null)
            {
                int counter = 0;
                foreach (ModelMesh modelMesh in animatedMesh)
                {
                    Matrix.Multiply(ref animationTransforms[counter++], ref transforms[modelMesh.ParentBone.Index], out tmpMat1);
                    Matrix.Multiply(ref tmpMat1, ref renderMatrix, out tmpMat2);

                    State.ShadowShader.UpdateGenerateShadowWorldMatrix(tmpMat2);

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

        public virtual XmlElement SaveModelCreationInfo(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = xmlDoc.CreateElement("ModelCreationInfo");

            if (resourceName.Length == 0)
                throw new GoblinException("ResourceName must be specified in order to " +
                    "save this model information to an XML file");

            xmlNode.SetAttribute("ResourceName", resourceName);
            if (modelLoaderName.Length == 0)
                throw new GoblinException("ModelLoaderName must be specified");

            xmlNode.SetAttribute("ModelLoaderName", modelLoaderName);

            xmlNode.SetAttribute("ShaderName", shaderName);
            if (technique.Length > 0)
                xmlNode.SetAttribute("ShaderTechniqueName", technique);

            return xmlNode;
        }

        public virtual XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = xmlDoc.CreateElement(TypeDescriptor.GetClassName(this));

            xmlNode.SetAttribute("Enabled", enabled.ToString());
            xmlNode.SetAttribute("UseLighting", useLighting.ToString());
            xmlNode.SetAttribute("CastShadow", castShadow.ToString());
            xmlNode.SetAttribute("ReceiveShadow", receiveShadow.ToString());
            xmlNode.SetAttribute("ShowBoundingBox", showBoundingBox.ToString());
            xmlNode.SetAttribute("UseInternalMaterials", useInternalMaterials.ToString());
            xmlNode.SetAttribute("OffsetToOrigin", offsetToOrigin.ToString());

            return xmlNode;
        }

        public virtual void Load(XmlElement xmlNode)
        {
            if (xmlNode.HasAttribute("Enabled"))
                enabled = bool.Parse(xmlNode.GetAttribute("Enabled"));
            if (xmlNode.HasAttribute("UseLighting"))
                useLighting = bool.Parse(xmlNode.GetAttribute("UseLighting"));
            if (xmlNode.HasAttribute("CastShadow"))
                castShadow = bool.Parse(xmlNode.GetAttribute("CastShadow"));
            if (xmlNode.HasAttribute("ReceiveShadow"))
                receiveShadow = bool.Parse(xmlNode.GetAttribute("ReceiveShadow"));
            if (xmlNode.HasAttribute("ShowBoundingBox"))
                showBoundingBox = bool.Parse(xmlNode.GetAttribute("ShowBoundingBox"));
            if (xmlNode.HasAttribute("UseInternalMaterials"))
                useInternalMaterials = bool.Parse(xmlNode.GetAttribute("UseInternalMaterials"));
            if (xmlNode.HasAttribute("OffsetToOrigin"))
                offsetToOrigin = bool.Parse(xmlNode.GetAttribute("OffsetToOrigin"));
        }

        #endregion
    }
}
