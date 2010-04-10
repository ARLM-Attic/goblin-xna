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
using System.Collections.Generic;
using System.Text;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Shaders;

namespace GoblinXNA.Graphics
{
    /// <summary>
    /// Model encapsulates all the model-related information needed for rendering.
    /// </summary>
    public interface IModel
    {
        #region Properties

        /// <summary>
        /// Flag indicating whether model is enabled and should be rendered.
        /// </summary>
        bool Enabled { get; set; }

        /// <summary>
        /// Flag reflecting whether lighting should be used when rendering this model.
        /// </summary>
        bool UseLighting { get; set; }

        /// <summary>
        /// Gets or sets if this model can cast shadows on other objects that can receive shadows
        /// </summary>
        bool CastShadows { get; set; }

        /// <summary>
        /// Gets or sets if this model can receive shadows cast by objects that can cast shadows
        /// </summary>
        bool ReceiveShadows { get; set; }

        /// <summary>
        /// Gets or sets the shader to use for rendering this model
        /// </summary>
        IShader Shader { get; set; }

        /// <summary>
        /// Gets or sets the shader technique to use
        /// </summary>
        String ShaderTechnique { get; set; }

        /// <summary>
        /// Gets the minimum bounding box used for display and by the physics engine
        /// </summary>
        BoundingBox MinimumBoundingBox { get; }

        /// <summary>
        /// Gets the minimum bounding sphere that bounds this model mesh
        /// </summary>
        BoundingSphere MinimumBoundingSphere { get; }

        /// <summary>
        /// Gets or sets whether to draw the minimum bounding box around the model
        /// </summary>
        bool ShowBoundingBox { get; set; }

        /// <summary>
        /// Gets the offset transformation from the origin of the world coordinate
        /// </summary>
        /// <remarks>
        /// If not provided, this transform will be calculated based on the following equation:
        /// OffsetTranslation.Translation = (MinimumBoundingBox.Min + MinimumBoundingBox.Max) / 2.
        /// In this case, no rotation offset will be calculated.
        /// </remarks>
        Matrix OffsetTransform { get; }

        /// <summary>
        /// Gets or sets whether to relocate the model to the origin. Each model has its 
        /// position stored in the model file, but if you want to relocate the model to the 
        /// origin instead of locating it based on the position stored in the file, you should
        /// set this to true.
        /// </summary>
        bool OffsetToOrigin { get; set; }
 
        /// <summary>
        /// Gets the triangle count of this model
        /// </summary>
        int TriangleCount { get; }

        /// <summary>
        /// Gets or sets whether to use the material setting set inside the model file
        /// </summary>
        bool UseInternalMaterials { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Disposes of model contents.
        /// </summary>
        void Dispose();

        /// <summary>
        /// Copies only the geometry (Mesh, PrimitiveMesh, AnimatedMesh, 
        /// MinimumBoundingBox, MinimumBoundingSphere, TriangleCount and Transforms)
        /// </summary>
        /// <param name="model">A source model from which to copy</param>
        void CopyGeometry(IModel model);

        /// <summary>
        /// Renders the model itself as well as the minimum bounding box if showBoundingBox
        /// is true. By default, SimpleEffectShader is used to render the model.
        /// </summary>
        /// <remarks>
        /// This function is called automatically to render the model, so do not call this method
        /// </remarks>
        /// <param name="material">Material properties of this model</param>
        /// <param name="renderMatrix">Transform of this model</param>
        void Render(Matrix renderMatrix, Material material);

        /// <summary>
        /// Generate shadows for this model in the generate shadows pass
        /// of our shadow mapping shader. All objects rendered here will
        /// cast shadows in our scene.
        /// </summary>
        /// <remarks>
        /// This function is called automatically to cast shadows, so do not call this method
        /// </remarks>
        /// <param name="renderMatrix">Transform of this model</param>
        void GenerateShadows(Matrix renderMatrix);

        /// <summary>
        /// Renders all objects that should receive shadows here. Called from the 
        /// ShadowMappingShader.UseShadow method.
        /// </summary>
        /// <remarks>
        /// This function is called automatically to receive shadows, so do not call this method
        /// </remarks>
        /// <param name="renderMatrix">Transform of this model</param>
        void UseShadows(Matrix renderMatrix);

        /// <summary>
        /// Saves the information necessary to create this model.
        /// </summary>
        /// <param name="xmlDoc"></param>
        /// <returns></returns>
        XmlElement SaveModelCreationInfo(XmlDocument xmlDoc);

        /// <summary>
        /// Saves the information of this model to an XML element.
        /// </summary>
        /// <param name="xmlDoc">The XML document to be saved.</param>
        /// <returns></returns>
        XmlElement Save(XmlDocument xmlDoc);

        /// <summary>
        /// Loads the information of this model from an XML element.
        /// </summary>
        /// <param name="xmlNode"></param>
        void Load(XmlElement xmlNode);

        #endregion
    }
}
