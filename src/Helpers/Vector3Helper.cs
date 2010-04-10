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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Graphics;

namespace GoblinXNA.Helpers
{
    /// <summary>
    /// A helper class that implements various useful static functions that the Vector3 class
    /// does not support. 
    /// </summary>
    public class Vector3Helper
    {
        /// <summary>
        /// Gets the x, y, and z dimensions of a bounding box.
        /// </summary>
        /// <param name="box"></param>
        /// <returns>The x, y, and z dimension of a bounding box stored in Vector3 class</returns>
        public static Vector3 GetDimensions(BoundingBox box)
        {
            if (box.Equals(new BoundingBox()))
                return new Vector3();

            Vector3[] corners = box.GetCorners();

            Vector3 origin = corners[0];
            float x = 0, y = 0, z = 0;

            for (int i = 1; i < corners.Length; i++)
            {
                if ((corners[i].X == origin.X) && (corners[i].Y == origin.Y))
                    z = Math.Abs(origin.Z - corners[i].Z);
                else if ((corners[i].Z == origin.Z) && (corners[i].Y == origin.Y))
                    x = Math.Abs(origin.X - corners[i].X);
                else if ((corners[i].X == origin.X) && (corners[i].Z == origin.Z))
                    y = Math.Abs(origin.Y - corners[i].Y);
            }

            return Vector3Helper.Get(x, y, z);
        }

        /// <summary>
        /// Converts from Vector4 type to Vector3 type by dropping the w component.
        /// </summary>
        /// <param name="v4">A Vector4 object</param>
        /// <returns>A Vector3 object without the w component</returns>
        public static Vector3 GetVector3(Vector4 v4)
        {
            Vector3 vector3 = new Vector3();
            vector3.X = v4.X;
            vector3.Y = v4.Y;
            vector3.Z = v4.Z;
            return vector3;
        }

        /// <summary>
        /// Calculate the normal perpendicular to two vectors v0->v1 and v0->v2 using right hand rule.
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        public static Vector3 GetNormal(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            Vector3 v0_1 = v1 - v0;
            Vector3 v0_2 = v2 - v0;

            Vector3 normal = Vector3.Cross(v0_2, v0_1);
            normal.Normalize();

            return normal;
        }

        /// <summary>
        /// Adds two Vector3 object.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3 Add(ref Vector3 a, ref Vector3 b)
        {
            Vector3 result = new Vector3();
            result.X = a.X + b.X;
            result.Y = a.Y + b.Y;
            result.Z = a.Z + b.Z;
            return result;
        }

        public static Vector3 Multiply(ref Vector3 a, ref Vector3 b)
        {
            Vector3 result = new Vector3();
            result.X = a.X * b.X;
            result.Y = a.Y * b.Y;
            result.Z = a.Z * b.Z;
            return result;
        }

        public static Vector3 Multiply(ref Vector3 a, float scale)
        {
            Vector3 result = new Vector3();
            result.X = a.X * scale;
            result.Y = a.Y * scale;
            result.Z = a.Z * scale;
            return result;
        }

        public static Vector3 Divide(ref Vector3 a, float scale)
        {
            Vector3 result = new Vector3();
            result.X = a.X / scale;
            result.Y = a.Y / scale;
            result.Z = a.Z / scale;
            return result;
        }

        public static Vector3 Get(float x, float y, float z)
        {
            Vector3 result = new Vector3();
            result.X = x;
            result.Y = y;
            result.Z = z;
            return result;
        }

        /// <summary>
        /// Converts a Vector3 object to an array of three floats in the order of x, y, and z.
        /// </summary>
        /// <param name="v3"></param>
        /// <returns></returns>
        public static float[] ToFloats(Vector3 v3)
        {
            float[] floats = { v3.X, v3.Y, v3.Z };
            return floats;
        }

        public static Vector3 FromString(String strVal)
        {
            Vector3 vec3 = Vector3.Zero;

            String[] vals = strVal.Split(':', ' ', '}');
            vec3.X = float.Parse(vals[1]);
            vec3.Y = float.Parse(vals[3]);
            vec3.Z = float.Parse(vals[5]);

            return vec3;
        }

        /// <summary>
        /// http://www.codeguru.com/forum/archive/index.php/t-329530.html
        /// 
        /// For a homogeneous geometrical transformation matrix, you can get the roll, pitch and yaw angles, 
        /// following the TRPY convention, using the following formulas:
        ///
        ///  roll (rotation around z) : atan2(xy, xx)
        ///  pitch (rotation around y) : -arcsin(xz)
        ///  yaw (rotation around x) : atan2(yz,zz)
        ///
        ///  where the matrix is defined in the form:
        ///
        ///  [
        ///   xx, yx, zx, px;
        ///   xy, yy, zy, py;
        ///   xz, yz, zz, pz;
        ///   0, 0, 0, 1
        ///  ]
        /// </summary>
        /// <param name="mat"></param>
        /// <returns>x=pitch, y=yaw, z=roll</returns>
        public static Vector3 ExtractAngles(Matrix mat)
        {
            float roll = (float)Math.Atan2(mat.M12, mat.M11);
            float pitch = (float)Math.Atan2(mat.M23, mat.M33);
            float yaw = -(float)Math.Asin(mat.M13);

            return new Vector3(pitch, yaw, roll);
        }
    }
}
