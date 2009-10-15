/************************************************************************************ 
 * Copyright (c) 2008-2009, Columbia University
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

using GoblinXNA.Shaders;
using GoblinXNA.Graphics;
using GoblinXNA.Helpers;

namespace GoblinXNA.Graphics
{
    /// <summary>
    /// This implementation is suitable for DirectX models and primitive shapes.
    /// </summary>
    public class Model : IModel
    {
        #region Fields

        protected bool enabled;
        protected bool useLighting;
        protected bool castShadow;
        protected bool receiveShadow;

        protected ModelMeshCollection mesh;
        protected List<ModelMesh> animatedMesh;
        protected PrimitiveMesh primitiveMesh;
        protected BoundingBox boundingBox;
        protected BoundingSphere boundingSphere;
        protected bool showBoundingBox;
        protected Matrix offsetTransform;
        protected bool offsetToOrigin;
        protected int triangleCount;

        protected IShader shader;
        protected String technique;
        protected Matrix[] transforms;
        protected Matrix[] animationTransforms;
        protected List<Vector3> vertices;
        protected bool useInternalMaterials;

        #region For Drawing Bounding Box

        protected bool boundingBoxInitialized;
        protected VertexPositionColor[] verts;
        protected short[] indecies;

        #endregion

        #region Temporary Variables

        protected Matrix tmpMat1;
        protected Matrix tmpMat2;
        protected Vector3 tmpVec1;

        #endregion

        #endregion

        #region Constructors

        public Model() : this(null) { }
        /// <summary>
        /// Creates a model from a model file.
        /// </summary>
        /// <param name="transforms">Transforms applied to each model meshes</param>
        /// <param name="mesh">A collection of model meshes</param>
        /// <param name="animatedMesh">
        /// A list of animated model meshes that can be transformed by the user
        /// </param>
        public Model(Matrix[] transforms, ModelMeshCollection mesh, List<ModelMesh> animatedMesh)
            : this(transforms, mesh, animatedMesh, null)
        {
        }
        /// <summary>
        /// Creates a model with VertexBuffer and IndexBuffer.
        /// </summary>
        /// <param name="primitiveMesh">A mesh defined with VertexBuffer and IndexBuffer</param>
        public Model(PrimitiveMesh primitiveMesh)
            : this(null, null, null, primitiveMesh)
        {
        }
        /// <summary>
        /// Creates a model with both information loaded from a model file and mesh defined
        /// using VertexBuffer and IndexBuffer
        /// </summary>
        /// <param name="transforms">Transforms applied to each model meshes</param>
        /// <param name="mesh">A collection of model meshes</param>
        /// <param name="animatedMesh">
        /// A list of animated model meshes that can be transformed by the user
        /// </param>
        /// <param name="primitiveMesh">A mesh defined with VertexBuffer and IndexBuffer</param>
        public Model(Matrix[] transforms, ModelMeshCollection mesh, List<ModelMesh> animatedMesh, 
            PrimitiveMesh primitiveMesh)
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

            this.primitiveMesh = primitiveMesh;
            CalculateMinimumBoundingBox();
            shader = new SimpleEffectShader();

            enabled = true;
            useInternalMaterials = false;
            useLighting = true;
            castShadow = false;
            receiveShadow = false;
            showBoundingBox = false;
            boundingBoxInitialized = false;
            technique = "";
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
        /// Gets the geometry data of this model.
        /// </summary>
        /// <remarks>
        /// If PrimitiveMesh is non-null, then this is probably null
        /// </remarks>
        public ModelMeshCollection Mesh
        {
            get { return mesh; }
        }

        /// <summary>
        /// Gets the mesh defined with VertexBuffer and IndexBuffer .
        /// </summary>
        /// <remarks>
        /// If Mesh is non-null, then this is probably null
        /// </remarks>
        public PrimitiveMesh PrimitiveMesh
        {
            get { return primitiveMesh; }
        }

        /// <summary>
        /// Gets a list of model meshes that can be transformed by the user.
        /// </summary>
        public List<ModelMesh> AnimatedMesh
        {
            get { return animatedMesh; }
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
                if(showBoundingBox == value)
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

        /// <summary>
        /// Gets the transform matrices of each mesh part of this model
        /// </summary>
        public Matrix[] Transforms
        {
            get { return transforms; }
        }

        public List<Vector3> Vertices
        {
            get { return vertices; }
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
                        int stride = part.VertexStride;
                        int numberv = part.NumVertices;
                        byte[] data = new byte[stride * numberv];

                        modelMesh.VertexBuffer.GetData<byte>(data);

                        for (int ndx = 0; ndx < data.Length; ndx += stride)
                        {
                            float x = BitConverter.ToSingle(data, ndx);
                            float y = BitConverter.ToSingle(data, ndx + 4);
                            float z = BitConverter.ToSingle(data, ndx + 8);

                            tmpVec1.X = x; tmpVec1.Y = y; tmpVec1.Z = z;
                            if (needTransform)
                            {
                                Matrix.CreateTranslation(ref tmpVec1, out tmpMat1);
                                Matrix.Multiply(ref tmpMat1, ref transforms[modelMesh.ParentBone.Index], out tmpMat2);
                                vertices.Add(tmpMat2.Translation);
                            }
                            else
                                vertices.Add(tmpVec1);
                        }

                        triangleCount += part.PrimitiveCount;
                    }
                }
            }

            if (primitiveMesh != null)
            {
                int stride = primitiveMesh.VertexDeclaration.GetVertexStrideSize(0);
                int numberv = primitiveMesh.NumberOfVertices;
                byte[] data = new byte[stride * numberv];

                primitiveMesh.VertexBuffer.GetData<byte>(data);

                for (int ndx = 0; ndx < data.Length; ndx += stride)
                {
                    tmpVec1.X = BitConverter.ToSingle(data, ndx);
                    tmpVec1.Y = BitConverter.ToSingle(data, ndx + 4);
                    tmpVec1.Z = BitConverter.ToSingle(data, ndx + 8);
                    vertices.Add(tmpVec1);
                }

                triangleCount += primitiveMesh.NumberOfPrimitives;
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
                if(offsetTransform.Equals(Matrix.Identity))
                    offsetTransform.Translation = (boundingBox.Min + boundingBox.Max) / 2;
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
            if (!(model is Model))
                return;

            Model srcModel = (Model)model;
            mesh = srcModel.Mesh;
            primitiveMesh = srcModel.PrimitiveMesh;
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
            transforms = srcModel.Transforms;
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
                shader.SetParameters(material);
            }

            // Render the actual model
            if (mesh != null)
            {
                foreach (ModelMesh modelMesh in this.mesh)
                {
                    // Skip animated mesh
                    if (animatedMesh != null && animatedMesh.Contains(modelMesh))
                        continue;

                    Matrix.Multiply(ref transforms[modelMesh.ParentBone.Index], ref renderMatrix, out tmpMat1);

                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        if (UseInternalMaterials)
                        {
                            material.InternalEffect = part.Effect;
                            shader.SetParameters(material);
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

            if (animatedMesh != null)
            {
                Matrix worldMatrix;

                int counter = 0;
                foreach (ModelMesh modelMesh in animatedMesh)
                {
                    worldMatrix = animationTransforms[counter++] * transforms[modelMesh.ParentBone.Index] *
                        renderMatrix;

                    foreach (ModelMeshPart part in modelMesh.MeshParts)
                    {
                        if (UseInternalMaterials)
                        {
                            material.InternalEffect = part.Effect;
                            shader.SetParameters(material);
                        }

                        shader.Render(
                            worldMatrix,
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
            
            if (primitiveMesh != null)
            {
                shader.Render(
                    renderMatrix,
                    technique,
                    delegate
                    {
                        State.Device.Vertices[0].SetSource(
                            primitiveMesh.VertexBuffer, 0, primitiveMesh.SizeInBytes);
                        State.Device.Indices = primitiveMesh.IndexBuffer;
                        State.Device.VertexDeclaration = primitiveMesh.VertexDeclaration;
                        State.Device.DrawIndexedPrimitives(primitiveMesh.PrimitiveType,
                            0, 0, primitiveMesh.NumberOfVertices, 0, primitiveMesh.NumberOfPrimitives);
                    });
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
            animatedMesh = null;
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

            if (mesh != null)
            {
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
            }
            else if (primitiveMesh != null)
            {
                State.ShadowShader.UpdateCalcShadowWorldMatrix(renderMatrix);
                State.Device.Vertices[0].SetSource(
                    primitiveMesh.VertexBuffer, 0, primitiveMesh.SizeInBytes);
                State.Device.Indices = primitiveMesh.IndexBuffer;
                State.Device.VertexDeclaration = primitiveMesh.VertexDeclaration;
                State.Device.DrawIndexedPrimitives(primitiveMesh.PrimitiveType,
                    0, 0, primitiveMesh.NumberOfVertices, 0, primitiveMesh.NumberOfPrimitives);
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

            if (mesh != null)
            {
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
            }
            else if (primitiveMesh != null)
            {
                State.ShadowShader.UpdateGenerateShadowWorldMatrix(renderMatrix);
                State.Device.Vertices[0].SetSource(
                    primitiveMesh.VertexBuffer, 0, primitiveMesh.SizeInBytes);
                State.Device.Indices = primitiveMesh.IndexBuffer;
                State.Device.VertexDeclaration = primitiveMesh.VertexDeclaration;
                State.Device.DrawIndexedPrimitives(primitiveMesh.PrimitiveType,
                    0, 0, primitiveMesh.NumberOfVertices, 0, primitiveMesh.NumberOfPrimitives);
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

        #endregion

    }
}
