/************************************************************************************ 
 * Copyright (c) 2008, Columbia University
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

namespace GoblinXNA.Physics
{
    /// <summary>
    /// A default implementation of the IPhysicsMaterial interface. A physics material defines
    /// how two materials behave when they physically interact, including support for friction 
    /// and elasticity. 
    /// </summary>
    public class PhysicsMaterial : IPhysicsMaterial
    {
        #region Member Fields
        protected String materialName1;
        protected String materialName2;
        protected bool collidable;
        protected float staticFriction;
        protected float kineticFriction;
        protected float softness;
        protected float elasticity;

        protected ContactBegin contactBeginCallback;
        protected ContactProcess contactProcessCallback;
        protected ContactEnd contactEndCallback;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a physics material that defines two materials behave when they physically
        /// interact.
        /// </summary>
        /// <remarks>
        /// 'materialName1' and 'materialName2' can be the same if you want to define how
        /// the same material behave when they physically interact.
        /// </remarks>
        /// <param name="materialName1">The first material name</param>
        /// <param name="materialName2">The second material name</param>
        /// <param name="collidable">Whether these two materials are collidable</param>
        /// <param name="staticFriction">The static friction coefficient</param>
        /// <param name="kineticFriction">The kinetic friction coefficient</param>
        /// <param name="softness">The softness of the material. Softness defines how fiercely
        /// the two materials will repel from each other when the two materials penetrate
        /// against each other</param>
        /// <param name="elasticity">The elasticity coefficient</param>
        /// <param name="contactBeginCallback">The callback function when two materials
        /// begin to contact. Set to 'null' if you don't need this callback function.</param>
        /// <param name="contactProcessCallback">The callback function when two materials
        /// proceed contact. Set to 'null' if you don't need this callback function.</param>
        /// <param name="contactEndCallback">The callback function when two materials
        /// end contact. Set to 'null' if you don't need this callback function.</param>
        public PhysicsMaterial(String materialName1, String materialName2, bool collidable, 
            float staticFriction, float kineticFriction, float softness, float elasticity,
            ContactBegin contactBeginCallback, ContactProcess contactProcessCallback,
            ContactEnd contactEndCallback)
        {
            this.materialName1 = materialName1;
            this.materialName2 = materialName2;
            this.collidable = collidable;
            this.staticFriction = staticFriction;
            this.kineticFriction = kineticFriction;
            this.elasticity = elasticity;
            this.softness = softness;
            this.contactBeginCallback = contactBeginCallback;
            this.contactProcessCallback = contactProcessCallback;
            this.contactEndCallback = contactEndCallback;
        }

        /// <summary>
        /// Creates a physics material with empty material names and no callback functions.
        /// </summary>
        public PhysicsMaterial()
            : this("", "", true, -1, -1, -1, -1, null, null, null)
        {
        }
        #endregion

        #region Properties
        public String MaterialName1
        {
            get { return materialName1; }
            set { materialName1 = value; }
        }

        public String MaterialName2
        {
            get { return materialName2; }
            set { materialName2 = value; }
        }

        public bool Collidable
        {
            get { return collidable; }
            set { collidable = value; }
        }

        public float StaticFriction
        {
            get { return staticFriction; }
            set { staticFriction = value; }
        }

        public float KineticFriction
        {
            get { return kineticFriction; }
            set { kineticFriction = value; }
        }

        public float Softness
        {
            get { return softness; }
            set { softness = value; }
        }

        public float Elasticity
        {
            get { return elasticity; }
            set { elasticity = value; }
        }

        public ContactBegin ContactBeginCallback
        {
            get { return contactBeginCallback; }
            set { contactBeginCallback = value; }
        }

        public ContactProcess ContactProcessCallback
        {
            get { return contactProcessCallback; }
            set { contactProcessCallback = value; }
        }

        public ContactEnd ContactEndCallback
        {
            get { return contactEndCallback; }
            set { contactEndCallback = value; }
        }
        #endregion
    }
}
