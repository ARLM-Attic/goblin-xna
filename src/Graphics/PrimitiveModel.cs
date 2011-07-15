/************************************************************************************ 
 * Copyright (c) 2008-2011, Columbia University
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
    /// An implementation of IModel interface for models created with CustomMesh.
    /// </summary>
    public class PrimitiveModel : IModel, IPhysicsMeshProvider 
    {
        #region Fields

        protected bool enabled;
        protected bool useLighting;
        protected bool castShadow;
        protected bool receiveShadow;
        protected bool useVertexColor;

        protected CustomMesh customMesh;
        protected BoundingBox boundingBox;
        protected BoundingSphere boundingSphere;
        protected bool showBoundingBox;
        protected Matrix offsetTransform;
        protected bool offsetToOrigin;
        protected int triangleCount;

        protected IShader shader;
        protected String technique;
        protected List<IShader> afterEffectShaders;

        protected List<Vector3> vertices;
        protected List<int> indices;

        protected String shaderName;
        protected String customShapeParameters;

        protected List<LineManager3D.Line> lines;
        protected List<LineManager3D.Line> renderLines;
        protected Matrix prevRenderMatrix;

        #region Temporary Variables

        protected Matrix tmpMat1;
        protected Matrix tmpMat2;
        protected Vector3 tmpVec1;

        #endregion

        #endregion

        #region Constructors

        public PrimitiveModel() : this(null) { }

        /// <summary>
        /// Creates a model with VertexBuffer and IndexBuffer.
        /// </summary>
        /// <param name="customMesh">A mesh defined with VertexBuffer and IndexBuffer</param>
        public PrimitiveModel(CustomMesh customMesh)
        {
            offsetTransform = Matrix.Identity;
            offsetToOrigin = false;

            vertices = new List<Vector3>();
            indices = new List<int>();

            this.customMesh = customMesh;
            shader = new SimpleEffectShader();
            afterEffectShaders = new List<IShader>();

            customShapeParameters = "";
            shaderName = TypeDescriptor.GetClassName(shader);

            enabled = true;
            useLighting = true;
            castShadow = false;
            receiveShadow = false;
            showBoundingBox = false;
            useVertexColor = false;
            technique = "";

            if (customMesh != null)
            {
                CalculateMinimumBoundingBox();
                triangleCount = customMesh.NumberOfPrimitives;
            }
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
        /// Gets or sets whether to use the vertex color instead of material information to
        /// render this model.
        /// </summary>
        public bool UseVertexColor
        {
            get { return useVertexColor; }
            set 
            { 
                useVertexColor = value;
                if (shader is SimpleEffectShader)
                    ((SimpleEffectShader)shader).UseVertexColor = value;
            }
        }

        public IShader Shader
        {
            get { return shader; }
            set { shader = value; }
        }

        public String ShaderTechnique
        {
            get { return technique; }
            set { technique = value; }
        }

        public List<IShader> AfterEffectShaders
        {
            get { return afterEffectShaders; }
            set { afterEffectShaders = value; }
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
            get{ return customMesh.PrimitiveType; }
        }

        /// <summary>
        /// Gets the mesh defined with VertexBuffer and IndexBuffer .
        /// </summary>
        public CustomMesh CustomMesh
        {
            get { return customMesh; }
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
                showBoundingBox = value; 

                if (value && lines == null)
                    GenerateBoundingBox(boundingBox.Min, boundingBox.Max);
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
        /// Gets or sets the name of the shader used to illuminate this model. The default value 
        /// is SimpleEffectShader.
        /// </summary>
        /// <remarks>
        /// This information is necessary for saving and loading scene graph from an XML file.
        /// </remarks>
        public String ShaderName
        {
            get { return shaderName; }
            set { shaderName = value; }
        }

        /// <summary>
        /// Gets or sets the parameters needed to be passed to a class that contructs a primitive
        /// shape. 
        /// </summary>
        /// <remarks>
        /// This information is necessary for saving and loading scene graph from an XML file.
        /// </remarks>
        public String CustomShapeParameters
        {
            get { return customShapeParameters; }
            set { customShapeParameters = value; }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the minimum bounding box that fits this model.
        /// </summary>
        protected virtual void CalculateMinimumBoundingBox()
        {
            int stride = customMesh.VertexDeclaration.GetVertexStrideSize(0);
            int numberv = customMesh.NumberOfVertices;
            byte[] data = new byte[stride * numberv];

            customMesh.VertexBuffer.GetData<byte>(data);

            for (int ndx = 0; ndx < data.Length; ndx += stride)
            {
                tmpVec1.X = BitConverter.ToSingle(data, ndx);
                tmpVec1.Y = BitConverter.ToSingle(data, ndx + 4);
                tmpVec1.Z = BitConverter.ToSingle(data, ndx + 8);
                vertices.Add(tmpVec1);
            }

            if (customMesh.IndexBuffer.BufferUsage == BufferUsage.None)
            {
                short[] tmpIndices = new short[customMesh.IndexBuffer.SizeInBytes / sizeof(short)];
                customMesh.IndexBuffer.GetData<short>(tmpIndices);
                int[] tmpIntIndices = new int[tmpIndices.Length];
                Array.Copy(tmpIndices, tmpIntIndices, tmpIndices.Length);
                indices.AddRange(tmpIntIndices);
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
                    offsetTransform.Translation = -(boundingBox.Min + boundingBox.Max) / 2;
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
        /// Copies only the geometry (Mesh, customMesh, AnimatedMesh, 
        /// MinimumBoundingBox, MinimumBoundingSphere, TriangleCount and Transforms)
        /// </summary>
        /// <param name="model">A source model from which to copy</param>
        public virtual void CopyGeometry(IModel model)
        {
            if (!(model is PrimitiveModel))
                return;

            PrimitiveModel srcModel = (PrimitiveModel)model;
            vertices.AddRange(((IPhysicsMeshProvider)model).Vertices);
            indices.AddRange(((IPhysicsMeshProvider)model).Indices);
            customMesh = srcModel.customMesh;

            triangleCount = srcModel.TriangleCount;
            boundingBox = srcModel.MinimumBoundingBox;
            boundingSphere = srcModel.MinimumBoundingSphere;
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
            if ((shader.CurrentMaterial != material) || material.HasChanged)
            {
                shader.SetParameters(material);

                foreach (IShader afterEffect in afterEffectShaders)
                    afterEffect.SetParameters(material);

                material.HasChanged = false;
            }

            shader.Render(
                renderMatrix,
                technique,
                delegate
                {
                    State.Device.Vertices[0].SetSource(
                        customMesh.VertexBuffer, 0, customMesh.SizeInBytes);
                    State.Device.Indices = customMesh.IndexBuffer;
                    State.Device.VertexDeclaration = customMesh.VertexDeclaration;
                    State.Device.DrawIndexedPrimitives(customMesh.PrimitiveType,
                        0, 0, customMesh.NumberOfVertices, 0, customMesh.NumberOfPrimitives);
                });

            foreach (IShader afterEffect in afterEffectShaders)
            {
                afterEffect.Render(
                    renderMatrix,
                    technique,
                    delegate
                    {
                        State.Device.DrawIndexedPrimitives(customMesh.PrimitiveType,
                            0, 0, customMesh.NumberOfVertices, 0, customMesh.NumberOfPrimitives);
                    });
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

            if (shader != null)
                shader.Dispose();
            if (customMesh != null)
                customMesh.Dispose();
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

            State.ShadowShader.UpdateCalcShadowWorldMatrix(renderMatrix);
            State.Device.Vertices[0].SetSource(
                customMesh.VertexBuffer, 0, customMesh.SizeInBytes);
            State.Device.Indices = customMesh.IndexBuffer;
            State.Device.VertexDeclaration = customMesh.VertexDeclaration;
            State.Device.DrawIndexedPrimitives(customMesh.PrimitiveType,
                0, 0, customMesh.NumberOfVertices, 0, customMesh.NumberOfPrimitives);
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

            State.ShadowShader.UpdateGenerateShadowWorldMatrix(renderMatrix);
            State.Device.Vertices[0].SetSource(
                customMesh.VertexBuffer, 0, customMesh.SizeInBytes);
            State.Device.Indices = customMesh.IndexBuffer;
            State.Device.VertexDeclaration = customMesh.VertexDeclaration;
            State.Device.DrawIndexedPrimitives(customMesh.PrimitiveType,
                0, 0, customMesh.NumberOfVertices, 0, customMesh.NumberOfPrimitives);
        }

        public virtual XmlElement SaveModelCreationInfo(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = xmlDoc.CreateElement("ModelCreationInfo");

            if (customShapeParameters.Length == 0)
                throw new GoblinException("CustomShapeParameters must be specified");

            xmlNode.SetAttribute("CustomShapeParameters", customShapeParameters);

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
            if (xmlNode.HasAttribute("OffsetToOrigin"))
                offsetToOrigin = bool.Parse(xmlNode.GetAttribute("OffsetToOrigin"));
        }

        #endregion
    }
}
