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
        protected bool isVehicle;
        protected bool neverDeactivate;
        protected bool modified;
        protected Matrix physicsWorldTransform;
        protected Matrix initialWorldTransform;
        protected Matrix compoundInitialWorldTransform;
        protected Vector3 initialLinearVelocity;
        protected Vector3 initialAngularVelocity;
        protected float linearDamping;
        protected Vector3 angularDamping;

        protected IModel model;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a physics object with a container that uses the physical properties specified
        /// in this class. The 'container' is usually an instance of GeometryNode.
        /// </summary>
        /// <param name="container"></param>
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

            physicsWorldTransform = Matrix.Identity;
            initialWorldTransform = Matrix.Identity;
            compoundInitialWorldTransform = Matrix.Identity;

            initialLinearVelocity = new Vector3();
            initialAngularVelocity = new Vector3();

            linearDamping = 0.0f;
            angularDamping = -Vector3.One;
        }
        #endregion

        #region Properties

        public IModel Model
        {
            get { return model; }
            set { model = value; }
        }

        public Object Container
        {
            get { return container; }
        }

        public int CollisionGroupID
        {
            get { return collisionGroupID; }
            set { collisionGroupID = value; }
        }

        public String MaterialName
        {
            get { return materialName; }
            set { materialName = value; }
        }

        public float Mass
        {
            get { return mass; }
            set
            {
                mass = value;
                modified = true;
            }
        }

        public Vector3 CenterOfMass
        {
            get { return centerOfMass; }
            set
            {
                centerOfMass = value;
                modified = true;
            }
        }

        public ShapeType Shape
        {
            get { return shape; }
            set { shape = value; }
        }

        public List<float> ShapeData
        {
            get { return shapeData; }
            set { shapeData = value; }
        }

        public Vector3 MomentOfInertia
        {
            get { return momentOfInertia; }
            set
            {
                momentOfInertia = value;
                modified = true;
            }
        }

        public List<Vector3> Vertices
        {
            get
            {
                if (model != null)
                    return model.Vertices;
                else
                    return new List<Vector3>();
            }
        }

        public bool Pickable
        {
            get { return pickable; }
            set
            {
                pickable = value;
                modified = true;
            }
        }   

        public bool Collidable
        {
            get { return collidable; }
            set
            {
                collidable = value;
                modified = true;
            }
        }

        public bool Interactable
        {
            get { return interactable; }
            set
            {
                interactable = value;
                modified = true;
            }
        }

        public bool Manipulatable
        {
            get { return manipulatable; }
            set { manipulatable = value; }
        }

        public bool ApplyGravity
        {
            get { return applyGravity; }
            set { applyGravity = value; }
        }

        public bool NeverDeactivate
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
    }
}
