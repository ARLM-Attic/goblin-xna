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
using System.Collections.Generic;
using System.Text;
using System.Xml;

using Microsoft.Xna.Framework;

using GoblinXNA.Graphics;
using GoblinXNA.Helpers;

namespace GoblinXNA.SceneGraph
{
    /// <summary>
    /// A scene graph node that holds a light source.
    /// </summary>
    public class LightNode : Node
    {
        #region Member Fields

        protected LightSource lightSource;
        protected bool global;
        protected Vector4 ambientLightColor;
        protected Matrix worldTransform;

        protected bool hasChanged;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a node that can hold a light source.
        /// </summary>
        /// <param name="name">The name of this light node</param>
        public LightNode(String name)
            : base(name)
        {
            lightSource = new LightSource();
            global = true;
            worldTransform = Matrix.Identity;
            ambientLightColor = new Vector4(0, 0, 0, 1);
            hasChanged = true;
        }
        /// <summary>
        /// Creates a light node with an empty name
        /// </summary>
        public LightNode() : this("") { }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a light source associated with this node
        /// </summary>
        public LightSource LightSource
        {
            get { return lightSource; }
            set 
            { 
                lightSource = value;
                hasChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets whether this light is a global light source. If set to true, then no 
        /// matter where in the scene graph this light node exists, the light source associated
        /// with this node will affect all objects in the scene graph. If set to false, then this node's
        /// light source will affect only this node's siblings and siblings children. The default 
        /// value is true.
        /// </summary>
        public bool Global
        {
            get { return global; }
            set 
            {
                if (global != value)
                {
                    global = value;
                    hasChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the ambient light color. The default value is (0.0f, 0.0f, 0.0f, 1.0f).
        /// </summary>
        public Vector4 AmbientLightColor
        {
            get { return ambientLightColor; }
            set 
            {
                if (!ambientLightColor.Equals(value))
                {
                    ambientLightColor = value;
                    hasChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets the world transformation of this light node.
        /// </summary>
        public Matrix WorldTransformation
        {
            get { return worldTransform; }
            internal set
            {
                if (!worldTransform.Equals(value))
                {
                    worldTransform = value;
                    hasChanged = true;
                }
            }
        }

        /// <summary>
        /// Indicates whether the light source or light node itself has changes to be reflected.
        /// </summary>
        internal bool HasChanged
        {
            get { return (hasChanged | lightSource.HasChanged); }
            set
            {
                hasChanged = value;
                lightSource.HasChanged = value;
            }
        }

        #endregion

        #region Overriden Methods
        /// <summary>
        /// Clones this light node.
        /// </summary>
        /// <returns></returns>
        public override Node CloneNode()
        {
            LightNode clone = (LightNode)base.CloneNode();
            clone.Global = global;
            clone.AmbientLightColor = ambientLightColor;
            clone.LightSource = lightSource;

            return clone;
        }

        public override XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = base.Save(xmlDoc);

            xmlNode.SetAttribute("Global", global.ToString());
            xmlNode.SetAttribute("AmbientLightColor", ambientLightColor.ToString());

            xmlNode.AppendChild(lightSource.Save(xmlDoc));

            return xmlNode;
        }

        public override void Load(XmlElement xmlNode)
        {
            base.Load(xmlNode);

            if (xmlNode.HasAttribute("Global"))
                global = bool.Parse(xmlNode.GetAttribute("Global"));
            if (xmlNode.HasAttribute("AmbientLightColor"))
                ambientLightColor = Vector4Helper.FromString(xmlNode.GetAttribute("AmbientLightColor"));

            XmlElement lightSourceXml = (XmlElement)xmlNode.ChildNodes[0];
            lightSource = (LightSource)Activator.CreateInstance(
                Type.GetType(lightSourceXml.Name));
            lightSource.Load(lightSourceXml);
        }

        #endregion
    }
}
