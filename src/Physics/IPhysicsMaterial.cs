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

using Microsoft.Xna.Framework;

namespace GoblinXNA.Physics
{
    /// <summary>
    /// A delegate/callback function that defines what to do when two physics objects 
    /// begin to contact.
    /// </summary>
    /// <param name="physObj1">One of the collided pair</param>
    /// <param name="physObj2">The other of the collided pair</param>
    public delegate void ContactBegin(IPhysicsObject physObj1, IPhysicsObject physObj2);

    /// <summary>
    /// A delegate/callback function that defines what to do when the contact proceeds 
    /// between the two physics objects.
    /// </summary>
    /// <param name="contactPosition">The position of the contact between the two objects</param>
    /// <param name="contactNormal">The normal of the contact</param>
    /// <param name="contactSpeed">The speed of the contact</param>
    /// <param name="colObj1ContactTangentSpeed">One of the collided pair's (physObj1 returned
    /// from ContactBegin callback function) contact tangent speed.</param>
    /// <param name="colObj2ContactTangentSpeed">The other of the collided pair's (physObj2 returned
    /// from ContactBegin callback function) contact tangent speed.</param>
    /// <param name="colObj1ContactTangentDirection">One of the collided pair's (physObj1 returned
    /// from ContactBegin callback function) contact tangent direction.</param>
    /// <param name="colObj2ContactTangentDirection">The other of the collided pair's (physObj1 returned
    /// from ContactBegin callback function) contact tangent direction.</param>
    public delegate void ContactProcess(Vector3 contactPosition, Vector3 contactNormal, float contactSpeed,
        float colObj1ContactTangentSpeed, float colObj2ContactTangentSpeed,
        Vector3 colObj1ContactTangentDirection, Vector3 colObj2ContactTangentDirection);

    /// <summary>
    /// A delegate/callback function that defines what to do when the contact process ends.
    /// </summary>
    public delegate void ContactEnd();

    /// <summary>
    /// This class defines the physical material properties between two materials.
    /// </summary>
    public interface IPhysicsMaterial
    {
        /// <summary>
        /// Gets or sets the first material name
        /// </summary>
        String MaterialName1 { get; set; }

        /// <summary>
        /// Gets or sets the second material name
        /// </summary>
        String MaterialName2 { get; set; }

        /// <summary>
        /// Gets or sets whether these two materials can collide.
        /// </summary>
        bool Collidable { get; set; }

        /// <summary>
        /// Gets or sets the static friction between the two materials.
        /// </summary>
        float StaticFriction { get; set; }

        /// <summary>
        /// Gets or sets the kinetic/dynamic friction between the two materials.
        /// </summary>
        float KineticFriction { get; set; }

        /// <summary>
        /// Gets or sets the elasticity between the two materials. 
        /// </summary>
        float Elasticity { get; set; }

        /// <summary>
        /// Gets or sets the softness between the two materials. This property is used 
        /// only when the two objects interpenetrate. The larger the value, the more restoring 
        /// force is applied to the interpenetrating objects. Restoring force is a force 
        /// applied to make both interpenetrating objects push away from each other so that 
        /// they no longer interpenetrate.
        /// </summary>
        float Softness { get; set; }

        /// <summary>
        /// Gets or sets the delegate/callback function called when contact begins 
        /// between two materials.
        /// </summary>
        ContactBegin ContactBeginCallback { get; set; }

        /// <summary>
        /// Gets or sets the delegate/callback function called when contact proceeds 
        /// between two materials.
        /// </summary>
        ContactProcess ContactProcessCallback { get; set; }

        /// <summary>
        /// Gets or sets the delegate/callback function called when contact ends 
        /// between two materials.
        /// </summary>
        ContactEnd ContactEndCallback { get; set; }
    }
}
