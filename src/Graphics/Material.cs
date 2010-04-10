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

using GoblinXNA.Helpers;

namespace GoblinXNA.Graphics
{
    /// <summary>
    /// Material defines the surface properties of the model geometry such as its color, transparency,
    /// and shininess.
    /// </summary>
    public class Material
    {
        #region Member Fields
        protected float specularPower;

        protected Vector4 specularColor;
        protected Vector4 ambientColor;
        protected Vector4 emissiveColor;
        protected Vector4 diffuseColor;

        protected Texture2D texture;
        protected Effect internalEffect;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a default material. See each properties for default values.
        /// </summary>
        public Material()
        {
            specularPower = 10.0f;

            specularColor = Vector4.Zero;
            ambientColor = Vector4.Zero;
            emissiveColor = Vector4.Zero;
            diffuseColor = new Vector4(0, 0, 0, 1);

            texture = null;
            internalEffect = null;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the shininess of this material when highlighted with lights.
        /// The larger the specular power, the smaller the size of the specular highlight.
        /// The default value is 10.0f
        /// </summary>
        public float SpecularPower
        {
            get { return specularPower; }
            set { specularPower = value; }
        }

        /// <summary>
        /// Gets or sets the diffuse color of this material.
        /// The default value is Color.Black
        /// </summary>
        public Vector4 Diffuse
        {
            get { return diffuseColor; }
            set { diffuseColor = value; }
        }

        /// <summary>
        /// Gets or sets the ambient color of this material.
        /// The default value is Color.Black
        /// </summary>
        public Vector4 Ambient
        {
            get { return ambientColor; }
            set { ambientColor = value; }
        }

        /// <summary>
        /// Gets or sets the specular color of this material.
        /// The default value is Color.Black
        /// </summary>
        public Vector4 Specular
        {
            get { return specularColor; }
            set { specularColor = value; }
        }

        /// <summary>
        /// Gets or sets the color of the light this material emits. 
        /// The default value is Color.Black
        /// </summary>
        public Vector4 Emissive
        {
            get { return emissiveColor; }
            set { emissiveColor = value; }
        }

        /// <summary>
        /// Gets whether this material contains texture information.
        /// </summary>
        public bool HasTexture
        {
            get { return (texture != null); }
        }

        /// <summary>
        /// Gets or sets the texture applied to this material.
        /// </summary>
        public Texture2D Texture
        {
            get { return texture; }
            set { texture = value; }
        }

        /// <summary>
        /// Gets or sets the effect associated with model contents. Some model files include
        /// their own material information.
        /// </summary>
        /// <remarks>
        /// See XNA's reference manual for the details of an "Effect" class
        /// </remarks>
        public Effect InternalEffect
        {
            get { return internalEffect; }
            set { internalEffect = value; }
        }
        #endregion

        #region Public Methods

        public virtual void Dispose()
        {
            if(texture != null)
                texture.Dispose();
        }

        public virtual XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = xmlDoc.CreateElement(TypeDescriptor.GetClassName(this));

            xmlNode.SetAttribute("diffuseColor", diffuseColor.ToString());
            xmlNode.SetAttribute("ambientColor", ambientColor.ToString());
            xmlNode.SetAttribute("specularColor", specularColor.ToString());
            xmlNode.SetAttribute("specularPower", specularPower.ToString());
            xmlNode.SetAttribute("emissiveColor", emissiveColor.ToString());

            if (texture != null)
                xmlNode.SetAttribute("textureName", texture.Name);

            return xmlNode;
        }

        public virtual void Load(XmlElement xmlNode)
        {
            if (xmlNode.HasAttribute("diffuseColor"))
                diffuseColor = Vector4Helper.FromString(xmlNode.GetAttribute("diffuseColor"));
            if (xmlNode.HasAttribute("ambientColor"))
                ambientColor = Vector4Helper.FromString(xmlNode.GetAttribute("ambientColor"));
            if (xmlNode.HasAttribute("specularColor"))
                specularColor = Vector4Helper.FromString(xmlNode.GetAttribute("specularColor"));
            if (xmlNode.HasAttribute("specularPower"))
                specularPower = float.Parse(xmlNode.GetAttribute("specularPower"));
            if (xmlNode.HasAttribute("emissiveColor"))
                emissiveColor = Vector4Helper.FromString(xmlNode.GetAttribute("emissiveColor"));

            if (xmlNode.HasAttribute("textureName"))
            {
                String textureName = xmlNode.GetAttribute("textureName");
                texture = State.Content.Load<Texture2D>(textureName);
            }
        }

        #endregion
    }
}
