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

using GoblinXNA.Graphics;
using GoblinXNA.Helpers;

namespace GoblinXNA.Physics
{
    /// <summary>
    /// A default implementation of the IPhysicsObject interface.
    /// </summary>
    public class PhysicsObject : IPhysicsObject
    {
        #region Member Fields

        protected Object container;
        protected int collisionGroupID;
        protected String materialName;
        protected float mass;
        protected ShapeType shape;
        protected List<float> shapeData;
        protected Vector3 momentOfInertia;
        protected Vector3 centerOfMass;
        protected bool pickable;
        protected bool collidable;
        protected bool interactable;
        protected bool applyGravity;
        protected bool manipulatable;
        protected bool neverDeactivate;
        protected bool modified;
        protected bool shapeModified;
        protected Matrix physicsWorldTransform;
        protected Matrix initialWorldTransform;
        protected Matrix compoundInitialWorldTransform;
        protected Vector3 initialLinearVelocity;
        protected Vector3 initialAngularVelocity;
        protected float linearDamping;
        protected Vector3 angularDamping;
        protected IPhysicsMeshProvider meshProvider;
        protected IModel model;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a physics object with a container that uses the physical properties specified
        /// in this class. The 'container' is usually an instance of GeometryNode.
        /// </summary>
        /// <param name="container">The container of this physics object.</param>
        public PhysicsObject(Object container)
        {
            this.container = container;
            collisionGroupID = 0;
            materialName = "";
            mass = 1.0f;
            shape = ShapeType.Box;
            shapeData = new List<float>();
            momentOfInertia = new Vector3();
            centerOfMass = new Vector3();
            pickable = false;
            collidable = false;
            interactable = false;
            manipulatable = false;
            applyGravity = true;
            neverDeactivate = false;
            modified = false;
            shapeModified = false;

            physicsWorldTransform = Matrix.Identity;
            initialWorldTransform = Matrix.Identity;
            compoundInitialWorldTransform = Matrix.Identity;

            initialLinearVelocity = new Vector3();
            initialAngularVelocity = new Vector3();

            linearDamping = 0.0f;
            angularDamping = -Vector3.One;

            model = null;
            meshProvider = null;
        }

        public PhysicsObject() : this(null) { }

        #endregion

        #region Properties

        public virtual IModel Model
        {
            get { return model; }
            set
            {
                model = value;
                modified = true;
                shapeModified = true;
            }
        }

        public virtual IPhysicsMeshProvider MeshProvider
        {
            get { return meshProvider; }
            set 
            { 
                meshProvider = value;
                modified = true;
                shapeModified = true;
            }
        }

        public virtual Object Container
        {
            get { return container; }
            set { container = value; }
        }

        public virtual int CollisionGroupID
        {
            get { return collisionGroupID; }
            set { collisionGroupID = value; }
        }

        public virtual String MaterialName
        {
            get { return materialName; }
            set { materialName = value; }
        }

        public virtual float Mass
        {
            get { return mass; }
            set
            {
                if (mass != value)
                {
                    mass = value;
                    modified = true;
                }
            }
        }

        public virtual Vector3 CenterOfMass
        {
            get { return centerOfMass; }
            set
            {
                centerOfMass = value;
                modified = true;
            }
        }

        public virtual ShapeType Shape
        {
            get { return shape; }
            set 
            {
                if (shape != value)
                {
                    shape = value;
                    modified = true;
                    shapeModified = true;
                }
            }
        }

        public virtual List<float> ShapeData
        {
            get { return shapeData; }
            set 
            { 
                shapeData = value;
                modified = true;
                shapeModified = true;
            }
        }

        public virtual Vector3 MomentOfInertia
        {
            get { return momentOfInertia; }
            set
            {
                momentOfInertia = value;
                modified = true;
            }
        }

        public virtual bool Pickable
        {
            get { return pickable; }
            set
            {
                pickable = value;
                modified = true;
            }
        }

        public virtual bool Collidable
        {
            get { return collidable; }
            set
            {
                collidable = value;
                modified = true;
            }
        }

        public virtual bool Interactable
        {
            get { return interactable; }
            set
            {
                interactable = value;
                modified = true;
            }
        }

        public virtual bool Manipulatable
        {
            get { return manipulatable; }
            set { manipulatable = value; }
        }

        public virtual bool ApplyGravity
        {
            get { return applyGravity; }
            set { applyGravity = value; }
        }

        public virtual bool NeverDeactivate
        {
            get { return neverDeactivate; }
            set
            {
                neverDeactivate = value;
                modified = true;
            }
        }

        public bool Modified
        {
            get { return modified; }
            set { modified = value; }
        }

        public bool ShapeModified
        {
            get { return shapeModified; }
            set { shapeModified = value; }
        }

        public Matrix PhysicsWorldTransform
        {
            get { return physicsWorldTransform; }
            set { physicsWorldTransform = value; }
        }

        public Matrix CompoundInitialWorldTransform
        {
            get { return compoundInitialWorldTransform; }
            set { compoundInitialWorldTransform = value; }
        }

        public Matrix InitialWorldTransform
        {
            set
            {
                initialWorldTransform = MatrixHelper.CopyMatrix(value);
                modified = true;
            }
            get { return initialWorldTransform; }
        }

        public Vector3 InitialLinearVelocity
        {
            get { return initialLinearVelocity; }
            set
            {
                initialLinearVelocity = value;
                modified = true;
            }
        }

        public Vector3 InitialAngularVelocity
        {
            get { return initialAngularVelocity; }
            set
            {
                initialAngularVelocity = value;
                modified = true;
            }
        }

        public float LinearDamping
        {
            get { return linearDamping; }
            set
            {
                linearDamping = value;
                modified = true;
            }
        }

        public Vector3 AngularDamping
        {
            get { return angularDamping; }
            set
            {
                angularDamping = value;
                modified = true;
            }
        }

        #endregion

        #region Public Methods

        public virtual XmlElement Save(XmlDocument xmlDoc)
        {
            XmlElement xmlNode = xmlDoc.CreateElement(TypeDescriptor.GetClassName(this));

            xmlNode.SetAttribute("pickable", pickable.ToString());
            xmlNode.SetAttribute("collidable", collidable.ToString());
            xmlNode.SetAttribute("interactable", interactable.ToString());
            xmlNode.SetAttribute("applyGravity", applyGravity.ToString());
            xmlNode.SetAttribute("manipulatable", manipulatable.ToString());
            xmlNode.SetAttribute("neverDeactivate", neverDeactivate.ToString());

            if(collisionGroupID != 0)
                xmlNode.SetAttribute("collisionGroupID", collisionGroupID.ToString());
            if (materialName.Length > 0)
                xmlNode.SetAttribute("materialName", materialName);
            xmlNode.SetAttribute("mass", mass.ToString());
            xmlNode.SetAttribute("shapeType", shape.ToString());
            if (shapeData.Count > 0)
            {
                String shapeDataStr = "" + shapeData[0];
                for (int i = 1; i < shapeData.Count; ++i)
                    shapeDataStr += ", " + shapeData[i];
                xmlNode.SetAttribute("shapeData", shapeDataStr);
            }

            if (!momentOfInertia.Equals(Vector3.Zero))
                xmlNode.SetAttribute("momentOfInertia", momentOfInertia.ToString());
            if (!centerOfMass.Equals(Vector3.Zero))
                xmlNode.SetAttribute("centerOfMass", centerOfMass.ToString());
            if (!initialLinearVelocity.Equals(Vector3.Zero))
                xmlNode.SetAttribute("initialLinearVelocity", initialLinearVelocity.ToString());
            if (!initialAngularVelocity.Equals(Vector3.Zero))
                xmlNode.SetAttribute("initialAngularVelocity", initialAngularVelocity.ToString());
            if (linearDamping != 0)
                xmlNode.SetAttribute("linearDamping", linearDamping.ToString());
            if (!angularDamping.Equals(-Vector3.One))
                xmlNode.SetAttribute("angularDamping", angularDamping.ToString());

            return xmlNode;
        }

        public virtual void Load(XmlElement xmlNode)
        {
            if (xmlNode.HasAttribute("pickable"))
                pickable = bool.Parse(xmlNode.GetAttribute("pickable"));
            if (xmlNode.HasAttribute("collidable"))
                collidable = bool.Parse(xmlNode.GetAttribute("collidable"));
            if (xmlNode.HasAttribute("interactable"))
                interactable = bool.Parse(xmlNode.GetAttribute("interactable"));
            if (xmlNode.HasAttribute("applyGravity"))
                applyGravity = bool.Parse(xmlNode.GetAttribute("applyGravity"));
            if (xmlNode.HasAttribute("manipulatable"))
                manipulatable = bool.Parse(xmlNode.GetAttribute("manipulatable"));
            if (xmlNode.HasAttribute("neverDeactivate"))
                neverDeactivate = bool.Parse(xmlNode.GetAttribute("neverDeactivate"));

            if (xmlNode.HasAttribute("collisionGroupID"))
                collisionGroupID = int.Parse(xmlNode.GetAttribute("collisionGroupID"));
            if (xmlNode.HasAttribute("materialName"))
                materialName = xmlNode.GetAttribute("materialName");
            if (xmlNode.HasAttribute("mass"))
                mass = float.Parse(xmlNode.GetAttribute("mass"));
            if(xmlNode.HasAttribute("shapeType"))
                shape = (ShapeType)Enum.Parse(typeof(ShapeType), xmlNode.GetAttribute("shapeType"));
            if (xmlNode.HasAttribute("shapeData"))
            {
                String[] shapeDataStrs = xmlNode.GetAttribute("shapeData").Split(',');
                foreach (String data in shapeDataStrs)
                    shapeData.Add(float.Parse(data));
            }

            if (xmlNode.HasAttribute("momentOfInertia"))
                momentOfInertia = Vector3Helper.FromString(xmlNode.GetAttribute("momentOfInertia"));
            if (xmlNode.HasAttribute("centerOfMass"))
                centerOfMass = Vector3Helper.FromString(xmlNode.GetAttribute("centerOfMass"));
            if (xmlNode.HasAttribute("initialLinearVelocity"))
                initialLinearVelocity = Vector3Helper.FromString(xmlNode.GetAttribute("initialLinearVelocity"));
            if (xmlNode.HasAttribute("initialAngularVelocity"))
                initialAngularVelocity = Vector3Helper.FromString(xmlNode.GetAttribute("initialAngularVelocity"));
            if(xmlNode.HasAttribute("linearDamping"))
                linearDamping = float.Parse(xmlNode.GetAttribute("linearDamping"));
            if (xmlNode.HasAttribute("angularDamping"))
                angularDamping = Vector3Helper.FromString(xmlNode.GetAttribute("angularDamping"));
        }

        #endregion
    }
}
