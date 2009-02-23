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
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Helpers;
using GoblinXNA.Device.Generic;
using Model = GoblinXNA.Graphics.Model;

using NewtonDynamics;

namespace GoblinXNA.Physics
{
    /// <summary>
    /// An implementation of the IPhysics interface using the Newton physics library developed by
    /// Newton Game Dynamics (http://www.newtondynamics.com).
    /// </summary>
    public class NewtonPhysics : IPhysics
    {
        #region Structs
        /// <summary>
        /// A structure class that defines the properties of two collided physics objects
        /// including the collision points, collision normals, and so on.
        /// </summary>
        public class CollisionPair
        {
            private IPhysicsObject colObj1;
            private IPhysicsObject colObj2;
            private int maxSize;
            private int contactPoints;
            private List<Vector3> normals;
            private List<Vector3> contacts;
            private List<float> penetration;

            /// <summary>
            /// Creates a pair of objects for detecting the collision event between these two objects
            /// </summary>
            /// <param name="colObj1"></param>
            /// <param name="colObj2"></param>
            public CollisionPair(IPhysicsObject colObj1, IPhysicsObject colObj2)
            {
                this.colObj1 = colObj1;
                this.colObj2 = colObj2;
                maxSize = 1;
                normals = new List<Vector3>();
                contacts = new List<Vector3>();
                penetration = new List<float>();
                contactPoints = 0;
            }

            /// <summary>
            /// Gets the first collision object of the pair
            /// </summary>
            public IPhysicsObject CollisionObject1
            {
                get { return colObj1; }
            }

            /// <summary>
            /// Gets the second collision object of the pair
            /// </summary>
            public IPhysicsObject CollisionObject2
            {
                get { return colObj2; }
            }

            /// <summary>
            /// Gets or sets the maximum number of elements in contacts, normals, and penetration.
            /// </summary>
            public int MaxSize
            {
                get { return maxSize; }
                set { maxSize = value; }
            }

            /// <summary>
            /// Gets the number of contact points.
            /// </summary>
            public int ContactPoints
            {
                get { return contactPoints; }
                internal set { contactPoints = value; }
            }

            /// <summary>
            /// Gets a list of at least MaxSize vectors to contain the collision contact normals. 
            /// </summary>
            public List<Vector3> Normals
            {
                get { return normals; }
                internal set { normals = value; }
            }

            /// <summary>
            /// Gets a list of at least MaxSize vectors to contain the collision contact points. 
            /// </summary>
            public List<Vector3> Contacts
            {
                get { return contacts; }
                internal set { contacts = value; }
            }

            /// <summary>
            /// Gets a list of at least MaxSize floats to contain the collision penetration at each contact.
            /// </summary>
            public List<float> Penetration
            {
                get { return penetration; }
                internal set { penetration = value; }
            }
        }

        private struct Joint
        {
            public IPhysicsObject Child;
            public IPhysicsObject Parent;
            public JointInfo Info;
        }
        #endregion

        #region Delegates
        /// <summary>
        /// A delegate/callback function that defines what to do when two physics object collide.
        /// </summary>
        /// <param name="pair">The pair that collided, and it contains the detailed information of
        /// the collision including the collision points and normals</param>
        public delegate void CollisionCallback(CollisionPair pair);
        #endregion

        #region Member Fields
        protected float gravity;
        protected Vector3 gravityDir;
        protected BoundingBox worldSize;
        protected bool useBoundingBox;

        /// <summary>
        /// Used to keep track of the object ids that are added to the physics simulation
        /// e.g., objectIDs.Put("PalmTree", 3), this way, when we want to get "PalmTree" matrix
        /// we can do by simply calling GetMatrix("PalmTree") where GetMatrix does 
        /// RigidBody body = RigidBody.Upcast(world.CollisionObjects[objectIDs["PalmTree"]]);
        /// return body.Transform
        /// </summary>
        private IDictionary<IPhysicsObject, IntPtr> objectIDs;
        private IDictionary<IntPtr, Vector3> scaleTable;
        private IDictionary<IntPtr, IPhysicsObject> reverseIDs;

        private IDictionary<String, int> materialIDs;
        private List<String> materialPairs;
        private IDictionary<IntPtr, IPhysicsMaterial> materials;

        private IDictionary<CollisionPair, CollisionCallback> collisionCallbacks;

        private List<PickedObject> pickedObjects;

        private IDictionary<IntPtr, Stack<Vector3>> forces;
        private IDictionary<IntPtr, Stack<Vector3>> torques;

        private List<List<Vector3>> collisionMesh;

        #region Newton Physics
        private IntPtr nWorld;
        private Newton.NewtonSetTransform transformCallback;
        private Newton.NewtonApplyForceAndTorque applyForceAndTorqueCallback;
        private Newton.NewtonWorldRayFilter rayCastFilterCallback;

        private Newton.NewtonContactBegin materialContactBeginCallback;
        private Newton.NewtonContactProcess materialContactProcessCallback;
        private Newton.NewtonContactEnd materialContactEndCallback;

        private Newton.NewtonCollisionIterator collisionIterator;
        private Newton.NewtonTreeCollision treeCollisionCallback;

        private Dictionary<IPhysicsObject, Newton.NewtonSetTransform> setTransformMap;
        private Dictionary<IPhysicsObject, Newton.NewtonApplyForceAndTorque> applyForceMap;
        private Dictionary<IPhysicsObject, Newton.NewtonTreeCollision> treeCollisionMap;

        private List<Joint> jointsToBeAdded;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an instance of the Newton physics simulation engine.
        /// </summary>
        public NewtonPhysics()
        {
            gravity = 9.8f;
            gravityDir = new Vector3(0, -1, 0);
            useBoundingBox = false;
            worldSize = new BoundingBox(Vector3.One * -100, Vector3.One * 100);

            objectIDs = new Dictionary<IPhysicsObject, IntPtr>();
            scaleTable = new Dictionary<IntPtr, Vector3>();
            reverseIDs = new Dictionary<IntPtr, IPhysicsObject>();
            collisionCallbacks = new Dictionary<CollisionPair, CollisionCallback>();

            materialIDs = new Dictionary<String, int>();
            materialPairs = new List<string>();
            materials = new Dictionary<IntPtr, IPhysicsMaterial>();

            pickedObjects = new List<PickedObject>();

            forces = new Dictionary<IntPtr, Stack<Vector3>>();
            torques = new Dictionary<IntPtr, Stack<Vector3>>();

            setTransformMap = new Dictionary<IPhysicsObject,Newton.NewtonSetTransform>();
            applyForceMap = new Dictionary<IPhysicsObject,Newton.NewtonApplyForceAndTorque>();
            treeCollisionMap = new Dictionary<IPhysicsObject,Newton.NewtonTreeCollision>();

            collisionMesh = new List<List<Vector3>>();

            jointsToBeAdded = new List<Joint>();

            transformCallback = delegate(IntPtr body, float[] pMatrix)
            {
                if (reverseIDs[body].Manipulatable)
                    return;

                ShapeType shape = reverseIDs[body].Shape;
                if ((shape == ShapeType.Box) || (shape == ShapeType.Sphere) || (shape == ShapeType.ConvexHull) 
                    || (shape == ShapeType.TriangleMesh))
                {
                    reverseIDs[body].PhysicsWorldTransform = Matrix.CreateScale(scaleTable[body]) *
                        MatrixHelper.FloatsToMatrix(pMatrix);
                }
                else
                {
                    reverseIDs[body].PhysicsWorldTransform = Matrix.CreateScale(scaleTable[body]) *
                        Matrix.CreateRotationZ(-MathHelper.PiOver2) *
                        MatrixHelper.FloatsToMatrix(pMatrix);
                }
            };

            applyForceAndTorqueCallback = delegate(IntPtr pNewtonBody)
            {
                float[] force = new float[3];
                force[0] = force[1] = force[2] = 0;
                if (reverseIDs[pNewtonBody].ApplyGravity)
                {
                    float Ixx = 0, Iyy = 0, Izz = 0, mass = 0;

                    Newton.NewtonBodyGetMassMatrix(pNewtonBody, ref mass, ref Ixx, ref Iyy, ref Izz);
                    force = Vector3Helper.ToFloats(gravityDir * gravity * mass);
                }

                Vector3 tmp;
                if (forces.ContainsKey(pNewtonBody) && forces[pNewtonBody].Count > 0)
                {
                    Stack<Vector3> _forces = forces[pNewtonBody];
                    while (_forces.Count > 0)
                    {
                        tmp = _forces.Pop();
                        force[0] += tmp.X;
                        force[1] += tmp.Y;
                        force[2] += tmp.Z;
                    }
                }
                Newton.NewtonBodyAddForce(pNewtonBody, force);

                if (torques.ContainsKey(pNewtonBody) && torques[pNewtonBody].Count > 0)
                {
                    float[] torque = new float[3];
                    torque[0] = torque[1] = torque[2] = 0;
                    Stack<Vector3> _torques = torques[pNewtonBody];
                    while (_torques.Count > 0)
                    {
                        tmp = _torques.Pop();
                        torque[0] += tmp.X;
                        torque[1] += tmp.Y;
                        torque[2] += tmp.Z;
                    }
                    Newton.NewtonBodyAddTorque(pNewtonBody, torque);
                }
                
            };

            rayCastFilterCallback = delegate(IntPtr pNewtonBody, float[] pHitNormal, int pCollisionID,
                IntPtr pUserData, float pIntersetParam)
            {
                IPhysicsObject physObj = reverseIDs[pNewtonBody];
                if (physObj.Pickable)
                {
                    PickedObject pickedObject = new PickedObject(physObj, pIntersetParam);
                    pickedObjects.Add(pickedObject);
                }

                return pIntersetParam;
            };

            materialContactBeginCallback = delegate(IntPtr pMaterial, IntPtr pNewtonBody0, IntPtr pNewtonBody1)
            {
                IPhysicsMaterial physMat = materials[Newton.NewtonMaterialGetMaterialPairUserData(pMaterial)];

                if (physMat.ContactBeginCallback == null)
                    return 1;

                if (reverseIDs.ContainsKey(pNewtonBody0) && reverseIDs.ContainsKey(pNewtonBody1))
                    physMat.ContactBeginCallback(reverseIDs[pNewtonBody0], reverseIDs[pNewtonBody1]);

                return 1;
            };

            materialContactProcessCallback = delegate(IntPtr pMaterial, IntPtr pContact)
            {
                IPhysicsMaterial physMat = materials[Newton.NewtonMaterialGetMaterialPairUserData(pMaterial)];

                if (physMat.ContactProcessCallback == null)
                    return 1;

                float contactNormalSpeed = Newton.NewtonMaterialGetContactNormalSpeed(pMaterial, pContact);

                float[] contactPos = new float[3];
                float[] contactNormal = new float[3];
                Newton.NewtonMaterialGetContactPositionAndNormal(pMaterial, contactPos, contactNormal);

                float colObj1ContactTangentSpeed = Newton.NewtonMaterialGetContactTangentSpeed(pMaterial, pContact, 0);
                float colObj2ContactTangentSpeed = Newton.NewtonMaterialGetContactTangentSpeed(pMaterial, pContact, 1);

                float[] colObj1ContactTangentDir = new float[3];
                float[] colObj2ContactTangentDir = new float[3];
                Newton.NewtonMaterialGetContactTangentDirections(pMaterial, colObj1ContactTangentDir,
                    colObj2ContactTangentDir);

                physMat.ContactProcessCallback(new Vector3(contactPos[0], contactPos[1], contactPos[2]),
                    new Vector3(contactNormal[0], contactNormal[1], contactNormal[2]),
                    contactNormalSpeed, colObj1ContactTangentSpeed, colObj2ContactTangentSpeed,
                    new Vector3(colObj1ContactTangentDir[0], colObj1ContactTangentDir[1], colObj1ContactTangentDir[2]),
                    new Vector3(colObj2ContactTangentDir[0], colObj2ContactTangentDir[1], colObj2ContactTangentDir[2]));

                return 1;
            };

            materialContactEndCallback = delegate(IntPtr pMaterial)
            {
                IPhysicsMaterial physMat = materials[Newton.NewtonMaterialGetMaterialPairUserData(pMaterial)];

                if (physMat.ContactEndCallback == null)
                    return;

                physMat.ContactEndCallback();
            };

            collisionIterator = delegate(IntPtr pNewtonBody, int vertexCount, float[] faceArray, int faceID)
            {
                List<Vector3> verts = new List<Vector3>();
                int max = faceArray.Length / 3;
                if (vertexCount > max)
                    Log.Write("More faceArray needed for drawing the collision mesh: " + vertexCount);
                for (int i = 0; i < vertexCount && i < max; i++)
                    verts.Add(new Vector3(faceArray[i * 3], faceArray[i * 3 + 1], faceArray[i * 3 + 2]));

                collisionMesh.Add(verts);
            };
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the gravity applied to all of the objects in the world.
        /// </summary>
        public float Gravity
        {
            get { return gravity; }
            set
            {
                gravity = value;
            }
        }

        /// <summary>
        /// Gets or sets the bound of the physics world where simulation is performed
        /// </summary>
        /// <remarks>
        /// Newton's original physics world bound is (-100, -100, -100) to (100, 100, 100)
        /// </remarks>
        public BoundingBox WorldSize
        {
            get { return worldSize; }
            set
            {
                worldSize = value;
                if (nWorld != null)
                {
                    Newton.NewtonSetWorldSize(nWorld, Vector3Helper.ToFloats(worldSize.Min),
                        Vector3Helper.ToFloats(worldSize.Max));
                }
            }
        }

        /// <summary>
        /// Gets the pointer to the NewtonWorld type defined in the Newton physics library.
        /// </summary>
        /// <remarks>
        /// This can be used to interface with the Newton physics engine directly combined 
        /// with the GetBody method which returns the NewtonBody type. Since this class does
        /// not support all of the features implemented in the Newton library, this provides a way
        /// for you to perform advanced physics simulation.
        /// </remarks>
        /// <see cref="GetBody(IPhysicsObject)"/>
        public IntPtr NewtonWorld
        {
            get { return nWorld; }
        }

        /// <summary>
        /// Gets or sets the direction in which the gravity is applied.
        /// </summary>
        public Vector3 GravityDirection
        {
            get { return gravityDir; }
            set
            {
                gravityDir = value;
                gravityDir.Normalize();
            }
        }

        /// <summary>
        /// True if we want to speed up the physics simulation by using bounding box
        /// collision detection instead of triangle mesh collision detection
        /// </summary>
        public bool UseBoundingBox
        {
            get { return useBoundingBox; }
            set { useBoundingBox = value; }
        }
        #endregion

        #region Public Methods
        public void InitializePhysics()
        {
            nWorld = Newton.NewtonCreate(null, null);

            WorldSize = worldSize;
        }

        public void RestartsSimulation()
        {
            Dispose();

            nWorld = Newton.NewtonCreate(null, null);

            WorldSize = worldSize;

            List<IPhysicsObject> physObjs = new List<IPhysicsObject>(objectIDs.Keys);
            List<IPhysicsMaterial> physMats = new List<IPhysicsMaterial>(materials.Values);

            objectIDs.Clear();
            reverseIDs.Clear();
            scaleTable.Clear();

            materials.Clear();
            materialPairs.Clear();
            materialIDs.Clear();

            foreach (IPhysicsMaterial physMat in physMats)
                AddPhysicsMaterial(physMat);

            foreach (IPhysicsObject physObj in physObjs)
                AddPhysicsObject(physObj);
        }

        public void AddPhysicsObject(IPhysicsObject physObj)
        {
            if (objectIDs.ContainsKey(physObj))
                return;

            // rigidbody is dynamic if and only if mass is non zero, otherwise static
            bool isDynamic = (physObj.Mass != 0.0f && physObj.Interactable) || 
                (physObj.Shape == ShapeType.TriangleMesh);

            Quaternion rot;
            Vector3 trans;
            Vector3 scale;
            physObj.CompoundInitialWorldTransform.Decompose(out scale, out rot, out trans);

            IntPtr collision = GetNewtonCollision(physObj, scale);

            //create the rigid body
            IntPtr body = Newton.NewtonCreateBody(nWorld, collision);

            Matrix startTransform = Matrix.CreateFromQuaternion(rot);
            if (physObj.Shape == ShapeType.Capsule ||
                physObj.Shape == ShapeType.Cone ||
                physObj.Shape == ShapeType.Cylinder ||
                physObj.Shape == ShapeType.ChamferCylinder)
                startTransform *= Matrix.CreateRotationZ(MathHelper.PiOver2);
            startTransform.Translation = trans;

            physObj.PhysicsWorldTransform = physObj.CompoundInitialWorldTransform;

            // set the matrix for the rigid body
            Newton.NewtonBodySetMatrix(body, MatrixHelper.ToFloats(startTransform));

            if (isDynamic)
            {
                if (!(physObj is NewtonVehicle))
                {
                    // set the transform call back function
                    // if the SetTransformCallback is specified by the programmer for this specific
                    // physics object, then use the specified one; otherwise, use the default
                    // SetTransformCallback function
                    if (setTransformMap.ContainsKey(physObj))
                        Newton.NewtonBodySetTransformCallback(body, setTransformMap[physObj]);
                    else
                        Newton.NewtonBodySetTransformCallback(body, transformCallback);

                    // set the force and torque call back function
                    // if the ApplyForceAndTorqueCallback is specified by the programmer for this specific
                    // physics object, then use the specified one; otherwise, use the default
                    // ApplyForceAndTorqueCallback function
                    if (applyForceMap.ContainsKey(physObj))
                        Newton.NewtonBodySetForceAndTorqueCallback(body, applyForceMap[physObj]);
                    else
                        Newton.NewtonBodySetForceAndTorqueCallback(body, applyForceAndTorqueCallback);
                }

                // set the mass matrix and moment of inertia
                Vector3 momentOfInertia = physObj.MomentOfInertia;
                if (momentOfInertia.Equals(Vector3.Zero))
                    momentOfInertia = GetMomentOfInertia(physObj, scale, collision);
                // Swap the axis values since Newton's capsule, cone, and cylinder primitives are oriented 
                // along X axis while our primitives are oriented along Y axis
                else if (physObj.Shape == ShapeType.Capsule ||
                    physObj.Shape == ShapeType.Cone ||
                    physObj.Shape == ShapeType.Cylinder ||
                    physObj.Shape == ShapeType.ChamferCylinder)
                {
                    float tmp = momentOfInertia.Y;
                    momentOfInertia.Y = -momentOfInertia.X;
                    momentOfInertia.X = tmp;
                }
                    
                Newton.NewtonBodySetMassMatrix(body, physObj.Mass, momentOfInertia.X,
                    momentOfInertia.Y, momentOfInertia.Z);

                // set the relative position of center of mass if not Vector3.Zero
                if (!physObj.CenterOfMass.Equals(Vector3.Zero))
                {
                    float[] centerOfMass = {physObj.CenterOfMass.X, physObj.CenterOfMass.Y,
                                               physObj.CenterOfMass.Z};
                    // Swap the axis values since Newton's capsule, cone, and cylinder primitives are oriented 
                    // along X axis while our primitives are oriented along Y axis
                    if (physObj.Shape == ShapeType.Capsule ||
                        physObj.Shape == ShapeType.Cone ||
                        physObj.Shape == ShapeType.Cylinder ||
                        physObj.Shape == ShapeType.ChamferCylinder)
                    {
                        centerOfMass[0] = physObj.CenterOfMass.Y;
                        centerOfMass[1] = -physObj.CenterOfMass.X;
                    }
                    Newton.NewtonBodySetCentreOfMass(body, centerOfMass);
                }

                // set the initial force and torque
                Newton.NewtonBodySetVelocity(body, Vector3Helper.ToFloats(physObj.InitialLinearVelocity));
                Newton.NewtonBodySetOmega(body, Vector3Helper.ToFloats(physObj.InitialAngularVelocity));

                // set the damping values
                if (physObj.LinearDamping >= 0)
                    Newton.NewtonBodySetLinearDamping(body, physObj.LinearDamping);

                if (physObj.AngularDamping != -Vector3.One)
                {
                    float[] angularDamping = {physObj.AngularDamping.X, physObj.AngularDamping.Y,
                        physObj.AngularDamping.Z};
                    Newton.NewtonBodySetAngularDamping(body, angularDamping);
                }

                if (physObj.NeverDeactivate)
                {
                    Newton.NewtonBodySetAutoFreeze(body, 0);
                }
            }

            if (!physObj.Collidable)
                Newton.NewtonWorldFreezeBody(nWorld, body);

            if (materialIDs.ContainsKey(physObj.MaterialName))
                Newton.NewtonBodySetMaterialGroupID(body, materialIDs[physObj.MaterialName]);

            // Apply extra settings for vehicle joint
            if (physObj is NewtonVehicle)
            {
                NewtonVehicle vehicle = (NewtonVehicle) physObj;
                float[] upDir = Vector3Helper.ToFloats(startTransform.Up);
                vehicle.Joint = Newton.NewtonConstraintCreateVehicle(nWorld, upDir, body);
                if(vehicle.TransformCallback == null)
                    throw new GoblinException("You need to set the TransformCallback for NewtonVehicle");
                if (vehicle.ForceCallback == null)
                    throw new GoblinException("You need to set the ForceCallback for NewtonVehicle");
                if (vehicle.TireUpdateCallback == null)
                    throw new GoblinException("You need to set the TireUpdateCallback for NewtonVehicle");

                Newton.NewtonBodySetTransformCallback(body, vehicle.TransformCallback);
                Newton.NewtonBodySetForceAndTorqueCallback(body, vehicle.ForceCallback);
                Newton.NewtonVehicleSetTireCallback(vehicle.Joint, vehicle.TireUpdateCallback);

                for (int i = 0; i < 4; i++)
                {
                    NewtonTire tire = vehicle.Tires[i];
                    if (tire == null)
                        throw new GoblinException("All of the four tires need to be set: " + ((TireID)i)
                            + " tire is null");

                    float[] localMatrix = MatrixHelper.ToFloats(tire.TireOffsetMatrix);
                    float[] pin = Vector3Helper.ToFloats(tire.Pin);
                    IntPtr newtonTireID = Newton.NewtonVehicleAddTire(vehicle.Joint, localMatrix, pin, 
                        tire.Mass, tire.Width, tire.Radius, tire.SuspensionShock, tire.SuspensionSpring, 
                        tire.SuspensionLength, IntPtr.Zero, tire.CollisionID);

                    vehicle.AddToTireTable(newtonTireID, i);
                }
            }

            objectIDs.Add(physObj, body);
            scaleTable.Add(body, scale);
            reverseIDs.Add(body, physObj);

            AddJoint();

            Newton.NewtonReleaseCollision(nWorld, collision);
        }

        public void ModifyPhysicsObject(IPhysicsObject physObj, Matrix newTransform)
        {
            if (!objectIDs.ContainsKey(physObj))
                return;

            IntPtr body = objectIDs[physObj];

            Quaternion rot;
            Vector3 trans;
            Vector3 scale;
            newTransform.Decompose(out scale, out rot, out trans);

            if (!scaleTable[body].Equals(scale))
            {
                IntPtr collision = GetNewtonCollision(physObj, scale);

                Newton.NewtonBodySetCollision(body, collision);
                Newton.NewtonReleaseCollision(nWorld, collision);

                scaleTable.Remove(body);
                scaleTable.Add(body, scale);
            }

            // rigidbody is dynamic if and only if mass is non zero, otherwise static
            bool isDynamic = (physObj.Mass != 0.0f && physObj.Interactable);

            Matrix startTransform = Matrix.CreateFromQuaternion(rot);
            if (physObj.Shape == ShapeType.Capsule ||
                physObj.Shape == ShapeType.Cone ||
                physObj.Shape == ShapeType.Cylinder ||
                physObj.Shape == ShapeType.ChamferCylinder)
                startTransform *= Matrix.CreateRotationZ(MathHelper.PiOver2);
            startTransform.Translation = trans;

            physObj.PhysicsWorldTransform = newTransform;

            // set the matrix for the rigid body
            Newton.NewtonBodySetMatrix(body, MatrixHelper.ToFloats(startTransform));

            if (isDynamic)
            {
                // set the transform call back function
                // if the SetTransformCallback is specified by the programmer for this specific
                // physics object, then use the specified one; otherwise, use the default
                // SetTransformCallback function
                if(setTransformMap.ContainsKey(physObj))
                    Newton.NewtonBodySetTransformCallback(body, setTransformMap[physObj]);
                else
                    Newton.NewtonBodySetTransformCallback(body, transformCallback);

                // set the force and torque call back function
                // if the ApplyForceAndTorqueCallback is specified by the programmer for this specific
                // physics object, then use the specified one; otherwise, use the default
                // ApplyForceAndTorqueCallback function
                if(applyForceMap.ContainsKey(physObj))
                    Newton.NewtonBodySetForceAndTorqueCallback(body, applyForceMap[physObj]);
                else
                    Newton.NewtonBodySetForceAndTorqueCallback(body, applyForceAndTorqueCallback);

                // set the mass matrix and moment of inertia
                Vector3 momentOfInertia = physObj.MomentOfInertia;
                if (momentOfInertia.Equals(Vector3.Zero))
                {
                    IntPtr collision = Newton.NewtonBodyGetCollision(body);
                    momentOfInertia = GetMomentOfInertia(physObj, scale, collision);
                }
                // Swap the axis values since Newton's capsule, cone, and cylinder primitives are oriented 
                // along X axis while our primitives are oriented along Y axis
                else if (physObj.Shape == ShapeType.Capsule ||
                    physObj.Shape == ShapeType.Cone ||
                    physObj.Shape == ShapeType.Cylinder ||
                    physObj.Shape == ShapeType.ChamferCylinder)
                {
                    float tmp = momentOfInertia.Y;
                    momentOfInertia.Y = -momentOfInertia.X;
                    momentOfInertia.X = tmp;
                }
                Newton.NewtonBodySetMassMatrix(body, physObj.Mass, momentOfInertia.X,
                    momentOfInertia.Y, momentOfInertia.Z);

                // set the center of mass if not Vector3.Zero
                if (!physObj.CenterOfMass.Equals(Vector3.Zero))
                {
                    float[] centerOfMass = {physObj.CenterOfMass.X, physObj.CenterOfMass.Y,
                                               physObj.CenterOfMass.Z};
                    // Swap the axis values since Newton's capsule, cone, and cylinder primitives are oriented 
                    // along X axis while our primitives are oriented along Y axis
                    if (physObj.Shape == ShapeType.Capsule ||
                        physObj.Shape == ShapeType.Cone ||
                        physObj.Shape == ShapeType.Cylinder ||
                        physObj.Shape == ShapeType.ChamferCylinder)
                    {
                        centerOfMass[0] = physObj.CenterOfMass.Y;
                        centerOfMass[1] = -physObj.CenterOfMass.X;
                    }
                    Newton.NewtonBodySetCentreOfMass(body, centerOfMass);
                }

                // set the initial force and torque
                Newton.NewtonBodySetVelocity(body, Vector3Helper.ToFloats(physObj.InitialLinearVelocity));
                Newton.NewtonBodySetOmega(body, Vector3Helper.ToFloats(physObj.InitialAngularVelocity));

                // set the damping values
                if (physObj.LinearDamping >= 0)
                    Newton.NewtonBodySetLinearDamping(body, physObj.LinearDamping);

                if (physObj.AngularDamping != -Vector3.One)
                {
                    float[] angularDamping = {physObj.AngularDamping.X, physObj.AngularDamping.Y,
                        physObj.AngularDamping.Z};
                    Newton.NewtonBodySetAngularDamping(body, angularDamping);
                }

                if (physObj.NeverDeactivate)
                    Newton.NewtonBodySetAutoFreeze(body, 0);
                else
                    Newton.NewtonBodySetAutoFreeze(body, 1);
            }

            // Apply extra settings for vehicle joint
            if (physObj is NewtonVehicle)
            {
                NewtonVehicle vehicle = (NewtonVehicle)physObj;
                float[] upDir = Vector3Helper.ToFloats(startTransform.Up);
                vehicle.Joint = Newton.NewtonConstraintCreateVehicle(nWorld, upDir, body);
                if (vehicle.TransformCallback == null)
                    throw new GoblinException("You need to set the TransformCallback for NewtonVehicle");
                if (vehicle.ForceCallback == null)
                    throw new GoblinException("You need to set the ForceCallback for NewtonVehicle");
                if (vehicle.TireUpdateCallback == null)
                    throw new GoblinException("You need to set the TireUpdateCallback for NewtonVehicle");

                Newton.NewtonBodySetTransformCallback(body, vehicle.TransformCallback);
                Newton.NewtonBodySetForceAndTorqueCallback(body, vehicle.ForceCallback);
                Newton.NewtonVehicleSetTireCallback(vehicle.Joint, vehicle.TireUpdateCallback);

                for (int i = 0; i < 4; i++)
                {
                    NewtonTire tire = vehicle.Tires[i];
                    if (tire == null)
                        throw new GoblinException("All of the four tires need to be set: " + ((TireID)i)
                            + " tire is null");

                    float[] localMatrix = MatrixHelper.ToFloats(tire.TireOffsetMatrix);
                    float[] pin = Vector3Helper.ToFloats(tire.Pin);
                    IntPtr newtonTireID = Newton.NewtonVehicleAddTire(vehicle.Joint, localMatrix, pin,
                        tire.Mass, tire.Width, tire.Radius, tire.SuspensionShock, tire.SuspensionSpring,
                        tire.SuspensionLength, IntPtr.Zero, tire.CollisionID);

                    vehicle.AddToTireTable(newtonTireID, i);
                }
            }

            if (!physObj.Collidable)
                Newton.NewtonWorldFreezeBody(nWorld, body);
            else
                Newton.NewtonWorldUnfreezeBody(nWorld, body);
        }

        public void RemovePhysicsObject(IPhysicsObject physObj)
        {
            if (objectIDs.ContainsKey(physObj))
            {
                Newton.NewtonDestroyBody(nWorld, objectIDs[physObj]);

                reverseIDs.Remove(objectIDs[physObj]);
                scaleTable.Remove(objectIDs[physObj]);
                objectIDs.Remove(physObj);
            }
        }

        /// <summary>
        /// Overwrites the physics transform, which basically makes the object 'teleport' from
        /// its current physics transform to a new transform.
        /// </summary>
        /// <param name="physObj">An existing physics object</param>
        /// <param name="transform">The new transform</param>
        public void SetTransform(IPhysicsObject physObj, Matrix transform)
        {
            if (objectIDs.ContainsKey(physObj))
            {
                Newton.NewtonBodySetMatrix(objectIDs[physObj], MatrixHelper.ToFloats(transform));
                physObj.PhysicsWorldTransform = transform;
                Newton.NewtonWorldUnfreezeBody(nWorld, objectIDs[physObj]);
            }
        }

        /// <summary>
        /// Adds a physics material to this physics engine for material simulation.
        /// </summary>
        /// <param name="physMat">A physics material that defines the physical 
        /// material properties to be added</param>
        public void AddPhysicsMaterial(IPhysicsMaterial physMat)
        {
            String pairName = physMat.MaterialName1 + "_" + physMat.MaterialName2;
            if (!materialPairs.Contains(pairName))
            {
                int mat1ID = 0, mat2ID = 0;
                if (materialIDs.ContainsKey(physMat.MaterialName1))
                    mat1ID = materialIDs[physMat.MaterialName1];
                else
                {
                    mat1ID = Newton.NewtonMaterialCreateGroupID(nWorld);
                    materialIDs.Add(physMat.MaterialName1, mat1ID);
                }

                if (materialIDs.ContainsKey(physMat.MaterialName2))
                    mat2ID = materialIDs[physMat.MaterialName2];
                else
                {
                    mat2ID = Newton.NewtonMaterialCreateGroupID(nWorld);
                    materialIDs.Add(physMat.MaterialName2, mat2ID);
                }

                Newton.NewtonMaterialSetDefaultCollidable(nWorld, mat1ID, mat2ID, ((physMat.Collidable) ? 1 : 0));

                if ((physMat.StaticFriction >= 0) && (physMat.KineticFriction >= 0) &&
                    (physMat.KineticFriction <= physMat.StaticFriction))
                    Newton.NewtonMaterialSetDefaultFriction(nWorld, mat1ID, mat2ID,
                        physMat.StaticFriction, physMat.KineticFriction);

                if (physMat.Elasticity >= 0)
                    Newton.NewtonMaterialSetDefaultElasticity(nWorld, mat1ID, mat2ID, physMat.Elasticity);

                if (physMat.Softness >= 0)
                    Newton.NewtonMaterialSetDefaultSoftness(nWorld, mat1ID, mat2ID, physMat.Softness);

                IntPtr material = Marshal.AllocHGlobal(1);
                Newton.NewtonMaterialSetCollisionCallback(nWorld, mat1ID, mat2ID, material,
                    (physMat.ContactBeginCallback != null) ? materialContactBeginCallback : null, 
                    (physMat.ContactProcessCallback != null) ? materialContactProcessCallback : null, 
                    (physMat.ContactEndCallback != null) ? materialContactEndCallback : null);

                if (!materials.ContainsKey(material))
                    materials.Add(material, physMat);

                materialPairs.Add(pairName);
            }
        }

        /// <summary>
        /// Removes an existing physics material.
        /// </summary>
        /// <param name="physMat">A physics material that defines the physical 
        /// material properties to be removed</param>
        public void RemovePhysicsMaterial(IPhysicsMaterial physMat)
        {
            materialPairs.Remove(physMat.MaterialName1 + "_" + physMat.MaterialName2);
        }

        public void CreateJoint(IPhysicsObject child, IPhysicsObject parent, JointInfo info)
        {
            Joint joint = new Joint();
            joint.Child = child;
            joint.Parent = parent;
            joint.Info = info;

            jointsToBeAdded.Add(joint);
        }

        /// <summary>
        /// Updates the physical simulation.
        /// </summary>
        /// <param name="elapsedTime">The amount of time to advance the simulation in
        /// seconds</param>
        public void Update(float elapsedTime)
        {
            int updateTime = Math.Max((int)(elapsedTime / 0.016f * 2), 2);
            if (updateTime > 5)
                updateTime = 5;
            for(int i = 0; i < updateTime; i++)
                Newton.NewtonUpdate(nWorld, updateTime);

            // Checks for collisions
            foreach (CollisionPair pair in collisionCallbacks.Keys)
            {
                try
                {
                    IntPtr collision1 = Newton.NewtonBodyGetCollision(objectIDs[pair.CollisionObject1]);
                    IntPtr collision2 = Newton.NewtonBodyGetCollision(objectIDs[pair.CollisionObject2]);
                    Vector3 invScale1 = scaleTable[objectIDs[pair.CollisionObject1]];
                    invScale1 = new Vector3(1 / invScale1.X, 1 / invScale1.Y, 1 / invScale1.Z);
                    Vector3 invScale2 = scaleTable[objectIDs[pair.CollisionObject2]];
                    invScale2 = new Vector3(1 / invScale2.X, 1 / invScale2.Y, 1 / invScale2.Z);

                    float[] contacts = new float[3 * pair.MaxSize];
                    float[] normals = new float[3 * pair.MaxSize];
                    float[] penetrations = new float[pair.MaxSize];
                    int contactPts = Newton.NewtonCollisionCollide(nWorld, pair.MaxSize, collision1,
                        MatrixHelper.ToFloats(Matrix.CreateScale(invScale1) *
                        pair.CollisionObject1.PhysicsWorldTransform),
                        collision2, MatrixHelper.ToFloats(Matrix.CreateScale(invScale2) *
                        pair.CollisionObject2.PhysicsWorldTransform),
                        contacts, normals, penetrations);

                    if (contactPts > 0)
                    {
                        pair.ContactPoints = contactPts;
                        pair.Contacts = new List<Vector3>();
                        pair.Normals = new List<Vector3>();
                        pair.Penetration = new List<float>();
                        for (int i = 0; i < pair.MaxSize * 3; i += 3)
                        {
                            pair.Normals.Add(new Vector3(normals[i], normals[i + 1], normals[i + 2]));
                            pair.Contacts.Add(new Vector3(contacts[i], contacts[i + 1], contacts[i + 2]));
                            pair.Penetration.Add(penetrations[i / 3]);
                        }
                        collisionCallbacks[pair](pair);
                    }
                }
                catch (Exception exp) { }
            }

            
        }

        public void Dispose()
        {
            Newton.NewtonDestroyAllBodies(nWorld);
            Newton.NewtonMaterialDestroyAllGroupID(nWorld);
            Newton.NewtonDestroy(nWorld);
        }

        /// <summary>
        /// Applies linear velocity to an existing physics object.
        /// </summary>
        /// <param name="physObj">An existing physics object to apply linear velocity</param>
        /// <param name="linearVelocity">The linear velocity</param>
        public void ApplyLinearVelocity(IPhysicsObject physObj, Vector3 linearVelocity)
        {
            if (objectIDs.ContainsKey(physObj))
            {
                Newton.NewtonWorldUnfreezeBody(nWorld, objectIDs[physObj]);
                Newton.NewtonBodySetVelocity(objectIDs[physObj], Vector3Helper.ToFloats(linearVelocity));
            }
        }

        /// <summary>
        /// Applies angular velocity to an existing physics object.
        /// </summary>
        /// <param name="physObj">An existing physics object to apply angular velocity</param>
        /// <param name="angularVelocity">The angular velocity</param>
        public void ApplyAngularVelocity(IPhysicsObject physObj, Vector3 angularVelocity)
        {
            if (objectIDs.ContainsKey(physObj))
            {
                Newton.NewtonWorldUnfreezeBody(nWorld, objectIDs[physObj]);
                Newton.NewtonBodySetOmega(objectIDs[physObj], Vector3Helper.ToFloats(angularVelocity));
            }
        }

        public BoundingBox GetAxisAlignedBoundingBox(IPhysicsObject physObj)
        {
            if (!objectIDs.ContainsKey(physObj))
                return new BoundingBox();

            float[] min = new float[3];
            float[] max = new float[3];

            Newton.NewtonBodyGetAABB(objectIDs[physObj], min, max);

            return new BoundingBox(new Vector3(min[0], min[1], min[2]), 
                new Vector3(max[0], max[1], max[2]));
        }

        public List<List<Vector3>> GetCollisionMesh(IPhysicsObject physObj)
        {
            collisionMesh.Clear();

            Newton.NewtonBodyForEachPolygonDo(objectIDs[physObj], collisionIterator);

            return collisionMesh;
        }

        /// <summary>
        /// Adds a collision callback function. You need to set the CollisionPair.PhysicsObject1
        /// and CollisionPair.PhysicsObject2, but you shouldn't set other properties of CollisionPair.
        /// Other properties of CollisionPair are set automatically when it's returned from the
        /// CollisionCallback delegate/callback function.
        /// </summary>
        /// <remarks>
        /// You can't add more than one collision callback function for the same collision pair.
        /// </remarks>
        /// <param name="pair">A pair of IPhysicsObject to detect collisions</param>
        /// <param name="handler">The callback function to be called when the pair collides</param>
        public void AddCollisionCallback(CollisionPair pair, CollisionCallback handler)
        {
            if (!collisionCallbacks.ContainsKey(pair))
            {
                collisionCallbacks.Add(pair, handler);
            }
        }

        /// <summary>
        /// Removes an existing callback function. You need to set the CollisionPair.PhysicsObject1
        /// and CollisionPair.PhysicsObject2 so that it knows which collision callback to remove.
        /// </summary>
        /// <param name="pair">A pair of IPhysicsObject to detect collisions</param>
        public void RemoveCollisionCallback(CollisionPair pair)
        {
            collisionCallbacks.Remove(pair);
        }

        /// <summary>
        /// Removes all of the collision callbacks.
        /// </summary>
        public void RemoveAllCollisionCallbacks()
        {
            collisionCallbacks.Clear();
        }

        /// <summary>
        /// Enables the simulation of an existing physics object.
        /// </summary>
        /// <param name="physObj">A physics object to enable simulation</param>
        public void EnableSimulation(IPhysicsObject physObj)
        {
            if(objectIDs.ContainsKey(physObj))
                Newton.NewtonWorldUnfreezeBody(nWorld, objectIDs[physObj]);
        }

        /// <summary>
        /// Disables the simulation of an existing physics object.
        /// </summary>
        /// <param name="physObj">A physics object to disable simulation</param>
        public void DisableSimulation(IPhysicsObject physObj)
        {
            if (objectIDs.ContainsKey(physObj))
                Newton.NewtonWorldFreezeBody(nWorld, objectIDs[physObj]);
        }

        /// <summary>
        /// Performs raycast picking with the given near and far points.
        /// </summary>
        /// <param name="nearPoint">The near point of the pick ray</param>
        /// <param name="farPoint">The far point of the pick ray</param>
        /// <returns>A list of picked objects</returns>
        public List<PickedObject> PickRayCast(Vector3 nearPoint, Vector3 farPoint)
        {
            pickedObjects.Clear();
            Newton.NewtonWorldRayCast(nWorld, Vector3Helper.ToFloats(nearPoint),
                Vector3Helper.ToFloats(farPoint), rayCastFilterCallback, IntPtr.Zero,
                null);

            return pickedObjects;
        }

        /// <summary>
        /// Adds extra force to an existing physics object.
        /// </summary>
        /// <param name="physObj">An existing physics object</param>
        /// <param name="force">The force to be added</param>
        public void AddForce(IPhysicsObject physObj, Vector3 force)
        {
            if (!objectIDs.ContainsKey(physObj))
                throw new GoblinException("physObj is not added in the physics engine");

            IntPtr body = objectIDs[physObj];
            if (forces.ContainsKey(body))
                forces[body].Push(force);
            else
            {
                Stack<Vector3> _forces = new Stack<Vector3>();
                _forces.Push(force);
                forces.Add(body, _forces);
            }

            Newton.NewtonWorldUnfreezeBody(nWorld, body);
        }

        /// <summary>
        /// Adds extra torque to an existing physics object.
        /// </summary>
        /// <param name="physObj">An existing physics object</param>
        /// <param name="torque">The torque to be added</param>
        public void AddTorque(IPhysicsObject physObj, Vector3 torque)
        {
            if (!objectIDs.ContainsKey(physObj))
                throw new GoblinException("physObj is not added in the physics engine");

            IntPtr body = objectIDs[physObj];
            if (torques.ContainsKey(body))
                torques[body].Push(torque);
            else
            {
                Stack<Vector3> _torques = new Stack<Vector3>();
                _torques.Push(torque);
                torques.Add(body, _torques);
            }

            Newton.NewtonWorldUnfreezeBody(nWorld, body);
        }

        /// <summary>
        /// Gets the pointer to the NewtonBody type defined in the original Newton physics library.
        /// If not found, IntPtr.Zero is returned.
        /// </summary>
        /// <remarks>
        /// This can be used to interface with the Newton physics engine directly combined 
        /// with the NewtonWorld property which returns the NewtonWorld type. Since this class does
        /// not support all of the features implemented in the Newton library, this provides a way
        /// for you to perform advanced physics simulation.
        /// </remarks>
        /// <param name="physObj">The physics object that contains the Newton body</param>
        /// <see cref="NewtonWorld"/>
        /// <returns>A pointer to NewtonBody type if found, otherwise, IntPtr.Zero</returns>
        public IntPtr GetBody(IPhysicsObject physObj)
        {
            if (objectIDs.ContainsKey(physObj))
                return objectIDs[physObj];
            else
                return IntPtr.Zero;
        }

        /// <summary>
        /// Gets the IPhysicsObject used by Goblin XNA framework from the pointer to a NewtonBody
        /// used in the Newton physics library. If not found, null is returned.
        /// </summary>
        /// <param name="pNewtonBody">A pointer to a NewtonBody used in the Newton library</param>
        /// <returns>The physics object used in Goblin XNA framework if found, otherwise, null</returns>
        public IPhysicsObject GetPhysicsObject(IntPtr pNewtonBody)
        {
            if (reverseIDs.ContainsKey(pNewtonBody))
                return reverseIDs[pNewtonBody];
            else
                return null;
        }

        /// <summary>
        /// Gets the velocity of an IPhysicsObject.
        /// </summary>
        /// <param name="physObj">The physics object to get velocity</param>
        /// <returns>A velocity vector</returns>
        public Vector3 GetVelocity(IPhysicsObject physObj)
        {
            float[] speed = new float[3];
            Newton.NewtonBodyGetVelocity(objectIDs[physObj], speed);

            return new Vector3(speed[0], speed[1], speed[2]);
        }

        /// <summary>
        /// Set a user defined NewtonSetTransform callback function to be used for this physics object.
        /// If not specified, a default callback function will be used.
        /// </summary>
        /// <remarks>
        /// This is an advanced functionality. We do not recommend using this method unless you know
        /// what exactly to do with the returned transform from Newton physics.
        /// </remarks>
        /// <param name="physObj"></param>
        /// <param name="callback"></param>
        /// <see cref="GetPhysicsObject"/>
        public void SetTransformCallback(IPhysicsObject physObj, Newton.NewtonSetTransform callback)
        {
            if (setTransformMap.ContainsKey(physObj))
                setTransformMap.Remove(physObj);

            setTransformMap.Add(physObj, callback);
        }

        /// <summary>
        /// Set a user defined NewtonApplyForceAndTorque callback function to be used for this 
        /// physics object. If not specified, a default callback function will be used.
        /// </summary>
        /// <remarks>
        /// This is an advanced functionality. We do not recommend using this method unless you know
        /// how exactly you want to manipulate the force and torque calculation.
        /// </remarks>
        /// <param name="physObj"></param>
        /// <param name="callback"></param>
        /// <see cref="GetPhysicsObject"/>
        public void SetApplyForceAndTorqueCallback(IPhysicsObject physObj,
            Newton.NewtonApplyForceAndTorque callback)
        {
            if (applyForceMap.ContainsKey(physObj))
                applyForceMap.Remove(physObj);

            applyForceMap.Add(physObj, callback);
        }

        /// <summary>
        /// Set a user defined NewtonTreeCollision callback function to be used for this 
        /// physics object. If not specified, null will be passed.
        /// </summary>
        /// <param name="?"></param>
        /// <param name="callback"></param>
        public void SetTreeCollisionCallback(IPhysicsObject physObj, Newton.NewtonTreeCollision callback)
        {
            if (treeCollisionMap.ContainsKey(physObj))
                treeCollisionMap.Remove(physObj);

            treeCollisionMap.Add(physObj, callback);
        }
        #endregion

        #region Helper Methods
        private void AddJoint()
        {
            List<Joint> removeList = new List<Joint>();
            foreach (Joint joint in jointsToBeAdded)
            {
                if (!objectIDs.ContainsKey(joint.Child) ||
                    (joint.Parent != null && !objectIDs.ContainsKey(joint.Parent)))
                    continue;

                IntPtr newtonJoint = IntPtr.Zero;
                if (joint.Info is BallAndSocketJoint)
                {
                    BallAndSocketJoint ballJoint = (BallAndSocketJoint)joint.Info;
                    float[] pivot = new float[3];
                    pivot[0] = ballJoint.Pivot.X;
                    pivot[1] = ballJoint.Pivot.Y;
                    pivot[2] = ballJoint.Pivot.Z;

                    newtonJoint = Newton.NewtonConstraintCreateBall(nWorld, pivot,
                        objectIDs[joint.Child], (joint.Parent != null) ?
                        objectIDs[joint.Parent] : IntPtr.Zero);

                    if (ballJoint.Pin != Vector3.Zero)
                    {
                        float[] pin = new float[3];
                        pin[0] = ballJoint.Pin.X;
                        pin[1] = ballJoint.Pin.Y;
                        pin[2] = ballJoint.Pin.Z;

                        Newton.NewtonBallSetConeLimits(newtonJoint, pin, ballJoint.MaxConeAngle,
                            ballJoint.MaxTwistAngle);
                    }

                    if (ballJoint.NewtonBallCallback != null)
                        Newton.NewtonBallSetUserCallback(newtonJoint, ballJoint.NewtonBallCallback);
                }
                else if (joint.Info is HingeJoint)
                {
                    HingeJoint hingeJoint = (HingeJoint)joint.Info;
                    float[] pivotPoint = new float[3];
                    pivotPoint[0] = hingeJoint.Pivot.X;
                    pivotPoint[1] = hingeJoint.Pivot.Y;
                    pivotPoint[2] = hingeJoint.Pivot.Z;

                    float[] pinDir = new float[3];
                    pinDir[0] = hingeJoint.Pin.X;
                    pinDir[1] = hingeJoint.Pin.Y;
                    pinDir[2] = hingeJoint.Pin.Z;

                    newtonJoint = Newton.NewtonConstraintCreateHinge(nWorld, pivotPoint, pinDir,
                        objectIDs[joint.Child], (joint.Parent != null) ?
                        objectIDs[joint.Parent] : IntPtr.Zero);

                    if (hingeJoint.NewtonHingeCallback != null)
                        Newton.NewtonHingeSetUserCallback(newtonJoint, hingeJoint.NewtonHingeCallback);
                }
                else if (joint.Info is SliderJoint)
                {
                    SliderJoint sliderJoint = (SliderJoint)joint.Info;
                    float[] pivotPoint = new float[3];
                    pivotPoint[0] = sliderJoint.Pivot.X;
                    pivotPoint[1] = sliderJoint.Pivot.Y;
                    pivotPoint[2] = sliderJoint.Pivot.Z;

                    float[] pinDir = new float[3];
                    pinDir[0] = sliderJoint.Pin.X;
                    pinDir[1] = sliderJoint.Pin.Y;
                    pinDir[2] = sliderJoint.Pin.Z;

                    newtonJoint = Newton.NewtonConstraintCreateSlider(nWorld, pivotPoint, pinDir,
                        objectIDs[joint.Child], (joint.Parent != null) ?
                        objectIDs[joint.Parent] : IntPtr.Zero);

                    if (sliderJoint.NewtonSliderCallback != null)
                        Newton.NewtonSliderSetUserCallback(newtonJoint, sliderJoint.NewtonSliderCallback);
                }
                else if (joint.Info is CorkscrewJoint)
                {
                    CorkscrewJoint corkscrewJoint = (CorkscrewJoint)joint.Info;
                    float[] pivotPoint = new float[3];
                    pivotPoint[0] = corkscrewJoint.Pivot.X;
                    pivotPoint[1] = corkscrewJoint.Pivot.Y;
                    pivotPoint[2] = corkscrewJoint.Pivot.Z;

                    float[] pinDir = new float[3];
                    pinDir[0] = corkscrewJoint.Pin.X;
                    pinDir[1] = corkscrewJoint.Pin.Y;
                    pinDir[2] = corkscrewJoint.Pin.Z;

                    newtonJoint = Newton.NewtonConstraintCreateCorkscrew(nWorld, pivotPoint, pinDir,
                        objectIDs[joint.Child], (joint.Parent != null) ?
                        objectIDs[joint.Parent] : IntPtr.Zero);

                    if (corkscrewJoint.NewtonCorkscrewCallback != null)
                        Newton.NewtonCorkscrewSetUserCallback(newtonJoint, 
                            corkscrewJoint.NewtonCorkscrewCallback);
                }
                else if (joint.Info is UniversalJoint)
                {
                    UniversalJoint universalJoint = (UniversalJoint)joint.Info;
                    float[] pivotPoint = new float[3];
                    pivotPoint[0] = universalJoint.Pivot.X;
                    pivotPoint[1] = universalJoint.Pivot.Y;
                    pivotPoint[2] = universalJoint.Pivot.Z;

                    float[] pinDir0 = new float[3];
                    pinDir0[0] = universalJoint.Pin0.X;
                    pinDir0[1] = universalJoint.Pin0.Y;
                    pinDir0[2] = universalJoint.Pin0.Z;

                    float[] pinDir1 = new float[3];
                    pinDir1[0] = universalJoint.Pin1.X;
                    pinDir1[1] = universalJoint.Pin1.Y;
                    pinDir1[2] = universalJoint.Pin1.Z;

                    newtonJoint = Newton.NewtonConstraintCreateUniversal(nWorld, pivotPoint, pinDir0,
                        pinDir1, objectIDs[joint.Child], (joint.Parent != null) ?
                        objectIDs[joint.Parent] : IntPtr.Zero);

                    if (universalJoint.NewtonUniversalCallback != null)
                        Newton.NewtonUniversalSetUserCallback(newtonJoint, 
                            universalJoint.NewtonUniversalCallback);
                }
                else if (joint.Info is UpVectorJoint)
                {
                    UpVectorJoint upVectorJoint = (UpVectorJoint)joint.Info;

                    float[] pinDir = new float[3];
                    pinDir[0] = upVectorJoint.Pin.X;
                    pinDir[1] = upVectorJoint.Pin.Y;
                    pinDir[2] = upVectorJoint.Pin.Z;

                    newtonJoint = Newton.NewtonConstraintCreateUpVector(nWorld, pinDir,
                        objectIDs[joint.Child]);
                }

                if (joint.Info.EnableCollision)
                    Newton.NewtonJointSetCollisionState(newtonJoint, 1);

                Newton.NewtonJointSetStiffness(newtonJoint, joint.Info.Stiffness);

                removeList.Add(joint);
            }

            foreach (Joint joint in removeList)
                jointsToBeAdded.Remove(joint);

        }

        /// <summary>
        /// Gets the newton collision type based on the physics object properties (mostly depends on
        /// the Shape, ShapeData, and Model properties)
        /// </summary>
        /// <param name="physObj"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        protected IntPtr GetNewtonCollision(IPhysicsObject physObj, Vector3 scale)
        {
            IntPtr collision = new IntPtr();
            Vector3 boundingBox = Vector3Helper.GetDimensions(physObj.Model.MinimumBoundingBox);
            float[] offsetMatrix = null;
            if (physObj.Model.OffsetToOrigin)
            {
                offsetMatrix = MatrixHelper.ToFloats(physObj.Model.OffsetTransform);

                offsetMatrix[12] *= scale.X;
                offsetMatrix[13] *= scale.Y;
                offsetMatrix[14] *= scale.Z;
            }

            // create the collision type
            if (useBoundingBox)
            {
                if (physObj.ShapeData.Count == 3)
                    collision = Newton.NewtonCreateBox(nWorld, physObj.ShapeData[0],
                        physObj.ShapeData[1], physObj.ShapeData[2], offsetMatrix);
                else
                    collision = Newton.NewtonCreateBox(nWorld,
                        boundingBox.X * scale.X,
                        boundingBox.Y * scale.Y,
                        boundingBox.Z * scale.Z, offsetMatrix);
            }
            else
            {
                switch (physObj.Shape)
                {
                    case ShapeType.Box:
                        if (physObj.ShapeData.Count == 3)
                            collision = Newton.NewtonCreateBox(nWorld, physObj.ShapeData[0],
                                physObj.ShapeData[1], physObj.ShapeData[2], offsetMatrix);
                        else
                            collision = Newton.NewtonCreateBox(nWorld,
                                boundingBox.X * scale.X,
                                boundingBox.Y * scale.Y,
                                boundingBox.Z * scale.Z, offsetMatrix);
                        break;
                    case ShapeType.Sphere:
                        if (physObj.ShapeData.Count == 3)
                            collision = Newton.NewtonCreateSphere(nWorld, physObj.ShapeData[0],
                                physObj.ShapeData[1], physObj.ShapeData[2], offsetMatrix);
                        else if (physObj.ShapeData.Count == 1)
                            collision = Newton.NewtonCreateSphere(nWorld, physObj.ShapeData[0],
                                physObj.ShapeData[1], physObj.ShapeData[2], offsetMatrix);
                        else
                            collision = Newton.NewtonCreateSphere(nWorld,
                                boundingBox.X * scale.X / 2,
                                boundingBox.Y * scale.Y / 2,
                                boundingBox.Z * scale.Z / 2, offsetMatrix);
                        break;
                    case ShapeType.Cone:
                        if (physObj.ShapeData.Count == 2)
                            collision = Newton.NewtonCreateCone(nWorld, physObj.ShapeData[0],
                                physObj.ShapeData[1], offsetMatrix);
                        else
                            collision = Newton.NewtonCreateCone(nWorld,
                                boundingBox.X * scale.X / 2,
                                boundingBox.Y * scale.Y, offsetMatrix);
                        break;
                    case ShapeType.Capsule:
                        if (physObj.ShapeData.Count == 2)
                            collision = Newton.NewtonCreateCapsule(nWorld, physObj.ShapeData[0],
                                physObj.ShapeData[1], offsetMatrix);
                        else
                            collision = Newton.NewtonCreateCapsule(nWorld,
                                boundingBox.X * scale.X / 2,
                                boundingBox.Y * scale.Y, offsetMatrix);
                        break;
                    case ShapeType.Cylinder:
                        if (physObj.ShapeData.Count == 2)
                            collision = Newton.NewtonCreateCylinder(nWorld, physObj.ShapeData[0],
                                physObj.ShapeData[1], offsetMatrix);
                        else
                            collision = Newton.NewtonCreateCylinder(nWorld,
                                boundingBox.X * scale.X / 2,
                                boundingBox.Y * scale.Y, offsetMatrix);
                        break;
                    case ShapeType.ChamferCylinder:
                        if (physObj.ShapeData.Count == 2)
                            collision = Newton.NewtonCreateChamferCylinder(nWorld, physObj.ShapeData[0],
                                physObj.ShapeData[1], offsetMatrix);
                        else
                            collision = Newton.NewtonCreateChamferCylinder(nWorld,
                                boundingBox.X * scale.X / 2,
                                boundingBox.Y * scale.Y, offsetMatrix);
                        break;
                    case ShapeType.Compound:
                        if (physObj.ShapeData.Count > 0)
                        {
                            List<IntPtr> collisions = new List<IntPtr>();
                            try
                            {
                                int i = 0;
                                while (i < physObj.ShapeData.Count)
                                {
                                    int numVertices = (int)physObj.ShapeData[i++];
                                    float[] verts = new float[numVertices * 3];
                                    for (int j = 0; j < verts.Length; j++)
                                        verts[j] = physObj.ShapeData[j + i];
                                    i += verts.Length;
                                    collisions.Add(Newton.NewtonCreateConvexHull(nWorld, numVertices,
                                        verts, sizeof(float) * 3, offsetMatrix));
                                }
                            }
                            catch (Exception exp)
                            {
                                throw new GoblinException("Wrong shape data format. See the " +
                                    "documentation for the correct format");
                            }

                            collision = Newton.NewtonCreateCompoundCollision(nWorld, collisions.Count,
                                collisions.ToArray());
                        }
                        else
                        {
                            throw new GoblinException("You have to provide the shape data in correct format");
                        }
                        break;
                    case ShapeType.ConvexHull:
                        List<Vector3> vertices = physObj.Vertices;
                        float[] vertexCloud = new float[vertices.Count * 3];
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            vertexCloud[i * 3] = vertices[i].X * scale.X;
                            vertexCloud[i * 3 + 1] = vertices[i].Y * scale.Y;
                            vertexCloud[i * 3 + 2] = vertices[i].Z * scale.Z;
                        }

                        collision = Newton.NewtonCreateConvexHull(nWorld, vertices.Count,
                            vertexCloud, sizeof(float) * 3, offsetMatrix);
                        break;
                    case ShapeType.TriangleMesh:
                        collision = Newton.NewtonCreateTreeCollision(nWorld,
                            (treeCollisionMap.ContainsKey(physObj)) ? treeCollisionMap[physObj] : null);
                        Newton.NewtonTreeCollisionBeginBuild(collision);

                        List<float> vertPtr = new List<float>();
                        int ndx = 0;
                        Vector3 vert = new Vector3();
                        if (physObj.Model.Mesh != null)
                        {
                            foreach (ModelMesh modelMesh in physObj.Model.Mesh)
                            {
                                bool needTransform = !physObj.Model.Transforms[modelMesh.ParentBone.Index].
                                     Equals(Matrix.Identity);
                                foreach (ModelMeshPart part in modelMesh.MeshParts)
                                {
                                    int stride = part.VertexStride;
                                    int numberv = part.NumVertices;
                                    byte[] data = new byte[stride * numberv];

                                    modelMesh.VertexBuffer.GetData<byte>(data);

                                    int[] indices = new int[part.PrimitiveCount * 3];

                                    if (modelMesh.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits)
                                    {
                                        short[] tmp = new short[part.PrimitiveCount * 3];
                                        modelMesh.IndexBuffer.GetData<short>(tmp, 0,
                                            tmp.Length);
                                        Array.Copy(tmp, 0, indices, 0, indices.Length);
                                    }
                                    else
                                        modelMesh.IndexBuffer.GetData<int>(indices, 0,
                                            indices.Length);

                                    for (int i = 0; i < indices.Length; i += 3)
                                    {
                                        vertPtr.Clear();
                                        bool fail = false;
                                        for (int j = 0; j < 3; j++)
                                        {
                                            if (indices[i + j] >= numberv)
                                            {
                                                fail = true;
                                                break;
                                            }

                                            ndx = indices[i + j] * stride;

                                            float x = BitConverter.ToSingle(data, ndx);
                                            float y = BitConverter.ToSingle(data, ndx + 4);
                                            float z = BitConverter.ToSingle(data, ndx + 8);
                                            if (needTransform)
                                            {
                                                vert = Matrix.Multiply(Matrix.CreateTranslation(new Vector3(x, y, z)),
                                                    physObj.Model.Transforms[modelMesh.ParentBone.Index]).Translation;
                                                x = vert.X;
                                                y = vert.Y;
                                                z = vert.Z;
                                            }

                                            vertPtr.Add(x * scale.X);
                                            vertPtr.Add(y * scale.Y);
                                            vertPtr.Add(z * scale.Z);
                                        }

                                        if (!fail)
                                            Newton.NewtonTreeCollisionAddFace(collision, 3, vertPtr.ToArray(),
                                                sizeof(float) * 3, 1);
                                    }

                                }
                            }
                        }
                        else if ((physObj.Model is Model) && ((Model)physObj.Model).PrimitiveMesh != null)
                        {
                            int stride = ((Model)physObj.Model).PrimitiveMesh.VertexDeclaration.GetVertexStrideSize(0);
                            int numberv = ((Model)physObj.Model).PrimitiveMesh.NumberOfVertices;
                            byte[] data = new byte[stride * numberv];

                            ((Model)physObj.Model).PrimitiveMesh.VertexBuffer.GetData<byte>(data);

                            short[] indices = new short[
                                ((Model)physObj.Model).PrimitiveMesh.IndexBuffer.SizeInBytes / sizeof(short)];
                            ((Model)physObj.Model).PrimitiveMesh.IndexBuffer.GetData<short>(indices);

                            if (((Model)physObj.Model).PrimitiveMesh.PrimitiveType == PrimitiveType.TriangleList)
                            {
                                for (int i = 0; i < indices.Length; i += 3)
                                {
                                    vertPtr.Clear();
                                    for (int j = 0; j < 3; j++)
                                    {
                                        ndx = indices[i + j] * stride;
                                        vertPtr.Add(BitConverter.ToSingle(data, ndx) * scale.X);
                                        vertPtr.Add(BitConverter.ToSingle(data, ndx + 4) * scale.Y);
                                        vertPtr.Add(BitConverter.ToSingle(data, ndx + 8) * scale.Z);
                                    }

                                    Newton.NewtonTreeCollisionAddFace(collision, 3, vertPtr.ToArray(),
                                        sizeof(float) * 3, 1);
                                }
                            }
                            else if (((Model)physObj.Model).PrimitiveMesh.PrimitiveType == PrimitiveType.TriangleFan)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    ndx = indices[1] * stride;
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx) * scale.X);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 4) * scale.Y);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 8) * scale.Z);
                                }

                                for (int i = 2; i < indices.Length; i++)
                                {
                                    ndx = indices[i] * stride;
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx) * scale.X);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 4) * scale.Y);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 8) * scale.Z);

                                    Newton.NewtonTreeCollisionAddFace(collision, 3, vertPtr.ToArray(),
                                        sizeof(float) * 3, 1);

                                    vertPtr.RemoveRange(6, 3);
                                }
                            }
                            else if (((Model)physObj.Model).PrimitiveMesh.PrimitiveType == PrimitiveType.TriangleStrip)
                            {
                                for (int i = 0; i < 2; i++)
                                {
                                    ndx = indices[1] * stride;
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx) * scale.X);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 4) * scale.Y);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 8) * scale.Z);
                                }

                                for (int i = 2; i < indices.Length; i++)
                                {
                                    ndx = indices[i] * stride;
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx) * scale.X);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 4) * scale.Y);
                                    vertPtr.Add(BitConverter.ToSingle(data, ndx + 8) * scale.Z);

                                    Newton.NewtonTreeCollisionAddFace(collision, 3, vertPtr.ToArray(),
                                        sizeof(float) * 3, 1);

                                    vertPtr.RemoveRange(0, 3);
                                }
                            }
                            else
                            {
                                Log.Write("PrimitiveType: " + ((Model)physObj.Model).PrimitiveMesh.PrimitiveType.ToString() +
                                    " is not supported for ShapeType.TriangleMesh collision");
                            }
                        }

                        Newton.NewtonTreeCollisionEndBuild(collision, 1);
                        break;
                }
            }

            return collision;
        }

        /// <summary>
        /// Gets the moment of inertia for certain shapes.
        /// </summary>
        /// <param name="physObj"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        protected Vector3 GetMomentOfInertia(IPhysicsObject physObj, Vector3 scale, IntPtr collision)
        {
            Vector3 momentOfInertia = Vector3.Zero;
            Vector3 boundingBoxDimension = Vector3Helper.GetDimensions(physObj.Model.MinimumBoundingBox);

            if (useBoundingBox)
            {
                Vector3 dimension = Vector3.Zero;
                if (physObj.ShapeData.Count == 3)
                {
                    dimension.X = physObj.ShapeData[0];
                    dimension.Y = physObj.ShapeData[1];
                    dimension.Z = physObj.ShapeData[2];
                }
                else
                {
                    dimension.X = boundingBoxDimension.X * scale.X;
                    dimension.Y = boundingBoxDimension.Y * scale.Y;
                    dimension.Z = boundingBoxDimension.Z * scale.Z;
                }

                momentOfInertia.X = physObj.Mass * (dimension.Y * dimension.Y +
                    dimension.Z * dimension.Z) / 12;
                momentOfInertia.Y = physObj.Mass * (dimension.X * dimension.X +
                    dimension.Z * dimension.Z) / 12;
                momentOfInertia.Z = physObj.Mass * (dimension.Y * dimension.Y +
                    dimension.X * dimension.X) / 12;
            }
            else
            {
                float[] inert = new float[3];
                float[] origin = new float[3];
                Newton.NewtonConvexCollisionCalculateInertialMatrix(collision,
                    inert, origin);
                momentOfInertia.X = physObj.Mass * inert[0];
                momentOfInertia.Y = physObj.Mass * inert[1];
                momentOfInertia.Z = physObj.Mass * inert[2];
            }

            return momentOfInertia;
        }
        #endregion
    }
}
