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

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// A scene graph node that defines a transformation.
    /// </summary>
    public class TransformNode : BranchNode
    {
        #region Member Fields

        protected Vector3 translation;
        protected Quaternion rotation;
        protected Vector3 scaling;
        protected Matrix worldTransformation;
        protected Matrix composedTransform;
        protected bool isWorldTransformationDirty;
        protected bool isReadOnly;
        protected bool useUserDefinedTransform;
        protected bool userDefinedTransformChanged;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a scene graph node that defines the transformation of its children.
        /// </summary>
        /// <param name="name">The name of this transform node</param>
        /// <param name="translation">Translation component of this transform</param>
        /// <param name="rotation">Rotation component of this transform in quaternion</param>
        /// <param name="scaling">Scaling component of this transform</param>
        public TransformNode(String name, Vector3 translation, Quaternion rotation, Vector3 scaling)
            : base(name)
        {
            this.translation = translation;
            this.rotation = rotation;
            this.scaling = scaling;
            worldTransformation = Matrix.Identity;
            isWorldTransformationDirty = true;
            useUserDefinedTransform = false;
            userDefinedTransformChanged = false;
            isReadOnly = false;
        }

        /// <summary>
        /// Creates a scene graph node that defines the transform of its children with scaling of 1
        /// in each dimension.
        /// </summary>
        /// <param name="name">The name of this transform node</param>
        /// <param name="translation">Translation component of this transform</param>
        /// <param name="rotation">Rotation component of this transform in quaternion</param>
        public TransformNode(String name, Vector3 translation, Quaternion rotation)
            :
            this(name, translation, rotation, Vector3.One) { }

        /// <summary>
        /// Creates a scene graph node that defines the transform of its children with scaling of 1
        /// in each dimension.
        /// </summary>
        /// <param name="translation">Translation component of this transform</param>
        /// <param name="rotation">Rotation component of this transform in quaternion</param>
        public TransformNode(Vector3 translation, Quaternion rotation) : this("", translation, rotation) { }

        /// <summary>
        /// Creates a scene graph node that defines the transform of its children with scaling of 1
        /// in each dimension, no rotation, and empty name.
        /// </summary>
        /// <param name="translation">Translation component of this transform</param>
        public TransformNode(Vector3 translation) : this(translation, Quaternion.Identity) { }

        /// <summary>
        /// Creates a scene graph node that defines the transform of its children with scaling of 1
        /// in each dimension, no rotation, and no translation.
        /// </summary>
        /// <param name="name">The name of this transform node</param>
        public TransformNode(String name) : this(name, Vector3.Zero, Quaternion.Identity) { }

        /// <summary>
        /// Creates a scene graph node that defines the transform of its children with scaling of 1
        /// in each dimension, no rotation, no translation, and no empty name.
        /// </summary>
        public TransformNode() : this("") { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the translation component.
        /// 
        /// If WorldTransformation matrix is set directly after setting this property, then
        /// the value set for this property will not affect the transformation of this node.
        /// </summary>
        /// <remarks>
        /// If ReadOnly is set to true, then you can't set this property.
        /// </remarks>
        /// <seealso cref="ReadOnly"/>
        /// <seealso cref="WorldTransformation"/>
        /// <exception cref="GoblinException"></exception>
        public Vector3 Translation
        {
            get
            {
                return translation;
            }
            set
            {
                if (isReadOnly)
                    throw new GoblinException("This TransformNode is read only, " + 
                        "setting Translation is not allowed");

                if (!translation.Equals(value))
                    isWorldTransformationDirty = true;
                translation = value;
                useUserDefinedTransform = false;
            }
        }

        /// <summary>
        /// Gets or sets the rotation component.
        /// 
        /// If WorldTransformation matrix is set directly after setting this property, then
        /// the value set for this property will not affect the transformation of this node.
        /// </summary>
        /// <remarks>
        /// If ReadOnly is set to true, then you can't set this property.
        /// </remarks>
        /// <seealso cref="ReadOnly"/>
        /// <seealso cref="WorldTransformation"/>
        /// <exception cref="GoblinException"></exception>
        public Quaternion Rotation
        {
            get { return rotation; }
            set
            {
                if (isReadOnly)
                    throw new GoblinException("This TransformNode is read only, " +
                        "setting Rotation is not allowed");

                if (!rotation.Equals(value))
                    isWorldTransformationDirty = true;
                rotation = value;
                useUserDefinedTransform = false;
            }
        }

        /// <summary>
        /// Gets or sets the scale component.
        /// 
        /// If WorldTransformation matrix is set directly after setting this property, then
        /// the value set for this property will not affect the transformation of this node.
        /// </summary>
        /// <remarks>
        /// If ReadOnly is set to true, then you can't set this property.
        /// </remarks>
        /// <seealso cref="ReadOnly"/>
        /// <seealso cref="WorldTransformation"/>
        /// <exception cref="GoblinException"></exception>
        public Vector3 Scale
        {
            get { return scaling; }
            set
            {
                if (isReadOnly)
                    throw new GoblinException("This TransformNode is read only, " +
                        "setting Scale is not allowed");

                if (!scaling.Equals(value))
                    isWorldTransformationDirty = true;
                scaling = value;
                useUserDefinedTransform = false;
            }
        }

        /// <summary>
        /// Gets or sets the transformation matrix. If you set this matrix directly, then whatever
        /// you set on Translation, Rotation, and Scale properties previoulsy will be ignored, and instead, this
        /// matrix value will be used to define the transformation of this node.
        /// 
        /// However, if you set any of the Translation, Rotation, and Scale properties after setting
        /// this matrix, then the composed matrix value from these three properties will be used.
        /// </summary>
        /// <remarks>
        /// If ReadOnly is set to true, then you can't set this property.
        /// </remarks>
        /// <seealso cref="ReadOnly"/>
        /// <seealso cref="Scale"/>
        /// <seealso cref="Translation"/>
        /// <seealso cref="Rotation"/>
        /// <exception cref="GoblinException"></exception>
        public Matrix WorldTransformation
        {
            get 
            {
                if (useUserDefinedTransform)
                    return worldTransformation;
                else
                    return composedTransform;
            }
            set 
            {
                if (isReadOnly)
                    throw new GoblinException("This TransformNode is read only, " +
                        "setting Scale is not allowed");

                useUserDefinedTransform = true;
                worldTransformation = value;
                userDefinedTransformChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets whether the user directly modified the transformation matrix instead using the
        /// composed one from Translation, Rotation, and Scale properties.
        /// </summary>
        internal bool UseUserDefinedTransform
        {
            get { return useUserDefinedTransform; }
            set { useUserDefinedTransform = value; }
        }

        /// <summary>
        /// Gets or sets whether the transformation matrix set directly by the user has been
        /// modified from the last one.
        /// </summary>
        internal bool UserDefinedTransformChanged
        {
            get { return userDefinedTransformChanged; }
            set { userDefinedTransformChanged = value; }
        }

        /// <summary>
        /// Gets the matrix composed from each individual transformation properties
        /// (Translation, Rotation, and Scale).
        /// </summary>
        internal Matrix ComposedTransform
        {
            get { return composedTransform; }
            set { composedTransform = value; }
        }

        /// <summary>
        /// Gets or sets whether the transformation has been modified from the previous one.
        /// </summary>
        internal bool IsWorldTransformationDirty
        {
            get { return isWorldTransformationDirty; }
            set { isWorldTransformationDirty = value; }
        }

        /// <summary>
        /// Gets or sets whether this transform node is read only. If true, then you can not
        /// set any of the Translation, Rotation, Scale, and WorldTransformation properties.
        /// </summary>
        public bool IsReadOnly
        {
            get { return isReadOnly; }
            set { isReadOnly = value; }
        }

        #endregion

        #region Override Methods

        public override Node CloneNode()
        {
            TransformNode clone = (TransformNode)base.CloneNode();
            clone.Translation = translation;
            clone.Rotation = rotation;
            clone.Scale = scaling;
            clone.WorldTransformation = WorldTransformation;
            clone.UseUserDefinedTransform = useUserDefinedTransform;
            clone.ComposedTransform = composedTransform;
            clone.IsWorldTransformationDirty = isWorldTransformationDirty;
            clone.IsReadOnly = isReadOnly;

            return clone;
        }

        #endregion
    }
}
