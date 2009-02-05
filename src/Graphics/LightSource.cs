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
using Microsoft.Xna.Framework.Graphics;

namespace GoblinXNA.Graphics
{
    public enum LightType
    {
        /// <summary>
        /// A point source is defined as a single point in space. The intensity of the light is
        /// attenuated with three attenuation coefficients. 
        /// </summary>
        Point,
        /// <summary>
        /// A directional source is described by the direction in which it is pointing, and 
        /// is useful for modeling a light source that is effectively infinitely far away from 
        /// the objects it illuminates.
        /// </summary>
        Directional,
        /// <summary>
        /// A spotlight is useful for creating dramatic localized lighting effects. It is defined 
        /// by its position, the direction in which it is pointing, and the width of the beam of 
        /// light it produces.
        /// </summary>
        SpotLight
    } 

    /// <summary>
    /// Light sources are used to illuminate the world.
    /// </summary>
    public class LightSource
    {
        #region Fields

        protected Vector3 position;
        protected Vector3 direction;
        protected LightType lightType;
        protected bool enabled;
        protected Vector4 diffuse;
        protected Vector4 specular;
        protected float attenuation0;
        protected float attenuation1;
        protected float attenuation2;
        protected float falloff;
        protected float innerConeAngle;
        protected float outerConeAngle;
        protected float range;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a light source with default configurations (see each field for the default values)
        /// </summary>
        public LightSource()
        {
            position = Vector3.Zero;
            direction = new Vector3(-1, -1, -1);
            lightType = LightType.Directional;
            enabled = true;
            diffuse = Color.Black.ToVector4();
            specular = Color.Black.ToVector4();
            attenuation0 = 1;
            attenuation1 = 0.1f;
            attenuation2 = 0.0f;
            falloff = 0.2f;
            innerConeAngle = (float)(0.2 * Math.PI);
            outerConeAngle = (float)(0.3 * Math.PI);
            range = 500;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="light"></param>
        public LightSource(LightSource light)
        {
            position = light.Position;
            direction = light.Direction;
            lightType = light.Type;
            enabled = light.Enabled;
            diffuse = light.Diffuse;
            specular = light.Specular;
            attenuation0 = light.Attenuation0;
            attenuation1 = light.Attenuation1;
            attenuation2 = light.Attenuation2;
            falloff = light.Falloff;
            innerConeAngle = light.InnerConeAngle;
            outerConeAngle = light.OuterConeAngle;
            range = light.Range;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the type of this light (Point, Directional, or SpotLight) 
        /// The default value is LightType.Directional.
        /// </summary>
        public LightType Type
        {
            get{ return lightType; }
            set{ lightType = value; }
        }

        /// <summary>
        /// Gets or sets whether this light source is enabled. The default value is true.
        /// </summary>
        public bool Enabled 
        {
            get{ return enabled; }
            set{ enabled = value; }
        }

        /// <summary>
        /// Gets or sets the diffuse component of this light. The default value is Color.Black.
        /// </summary>
        public Vector4 Diffuse
        {
            get{ return diffuse; }
            set{ diffuse = value; }
        }

        /// <summary>
        /// Gets or sets the specular component of this light. The default value is Color.Black.
        /// </summary>
        public Vector4 Specular
        {
            get { return specular; }
            set { specular = value; }
        }

        /// <summary>
        /// Gets or sets the position of this light source. This property is used only for Point
        /// and SpotLight types. The default value is Vector3.Zero.
        /// </summary>
        public Vector3 Position
        {
            get{ return position; }
            set{ position = value; }
        }

        /// <summary>
        /// Gets or sets the direction of this light source. This property is used
        /// only for Directional and SpotLight types. The default value is vector (-1, -1, -1).
        /// </summary>
        public Vector3 Direction
        {
            get{ return direction; }
            set{ direction = value; }
        }

        /// <summary>
        /// Gets or sets the zero-th degree attenuation coefficient for estimating light energy attenuation. 
        /// This property is used only for Point and SpotLight types. The default value is 0.01f.
        /// </summary>
        public float Attenuation0
        {
            get { return attenuation0; }
            set { attenuation0 = value; }
        }

        /// <summary>
        /// Gets or sets the first degree attenuation coefficient for estimating light energy attenuation. 
        /// This property is used only for Point and SpotLight types. The default value is 0.1f.
        /// </summary>
        public float Attenuation1
        {
            get { return attenuation1; }
            set { attenuation1 = value; }
        }

        /// <summary>
        /// Gets or sets the second degree attenuation coefficient for estimating light energy attenuation. 
        /// This property is used only for Point and SpotLight types. The default value is 0.0f.
        /// </summary>
        public float Attenuation2
        {
            get { return attenuation2; }
            set { attenuation2 = value; }
        }

        public float Falloff
        {
            get { return falloff; }
            set { falloff = value; }
        }

        /// <summary>
        /// Gets or sets the inner radius of the spotlight where the light begins to be attenuated. 
        /// This property is used only for SpotLight type. The default value is 0.2f * PI (36 degrees).
        /// </summary>
        public float InnerConeAngle
        {
            get { return innerConeAngle; }
            set { innerConeAngle = value; }
        }

        /// <summary>
        /// Gets or sets the outer radius of the spotlight where the light intensity (ambient) is zero. 
        /// The default value is 0.3f * PI (54 degrees).
        /// </summary>
        public float OuterConeAngle
        {
            get { return outerConeAngle; }
            set { outerConeAngle = value; }
        }

        /// <summary>
        /// Gets or sets the effective range of this light source. This property is used only for
        /// Point and SpotLight types. The default value is 500.
        /// </summary>
        public float Range
        {
            get { return range; }
            set { range = value; }
        }

        #endregion
    }
}
