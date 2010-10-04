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

namespace GoblinXNA.Helpers
{
    /// <summary>
    /// A helper class that implements various useful static functions that the Matrix class
    /// does not support.
    /// </summary>
    public class MatrixHelper
    {
        private static Matrix emptyMatrix = new Matrix();

        /// <summary>
        /// Copies the contents from a 'src' matrix.
        /// </summary>
        /// <param name="src">The matrix to copy from</param>
        /// <returns>The matrix with copied contents</returns>
        public static Matrix CopyMatrix(Matrix src)
        {
            return new Matrix(src.M11, src.M12, src.M13, src.M14, src.M21, src.M22, src.M23, src.M24,
                src.M31, src.M32, src.M33, src.M34, src.M41, src.M42, src.M43, src.M44);
        }

        /// <summary>
        /// Converts an array of sixteen floats to Matrix.
        /// </summary>
        /// <param name="mat">An array of 16 floats</param>
        /// <returns></returns>
        public static Matrix FloatsToMatrix(float[] mat)
        {
            if (mat == null || mat.Length != 16)
                throw new ArgumentException("mat has to contain 16 floating numbers");

            return new Matrix(
                mat[0], mat[1], mat[2], mat[3],
                mat[4], mat[5], mat[6], mat[7],
                mat[8], mat[9], mat[10], mat[11],
                mat[12], mat[13], mat[14], mat[15]);
        }

        public static void FloatsToMatrix(float[] mat, out Matrix m)
        {
            m.M11 = mat[0]; m.M12 = mat[1]; m.M13 = mat[2]; m.M14 = mat[3];
            m.M21 = mat[4]; m.M22 = mat[5]; m.M23 = mat[6]; m.M24 = mat[7];
            m.M31 = mat[8]; m.M32 = mat[9]; m.M33 = mat[10]; m.M34 = mat[11];
            m.M41 = mat[12]; m.M42 = mat[13]; m.M43 = mat[14]; m.M44 = mat[15];
        }

        /// <summary>
        /// Copies only the rotation part of the matrix (the upper-left 3x3 matrix, so it
        /// may actually contain the scaling factor as well).
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Matrix GetRotationMatrix(Matrix src)
        {
            Matrix rotMat = CopyMatrix(src);
            rotMat.M41 = rotMat.M42 = rotMat.M43 = 0;
            return rotMat;
        }

        public static void GetRotationMatrix(ref Matrix src, out Matrix dest)
        {
            dest.M11 = src.M11; dest.M12 = src.M12; dest.M13 = src.M13; dest.M14 = src.M14;
            dest.M21 = src.M21; dest.M22 = src.M22; dest.M23 = src.M23; dest.M24 = src.M24;
            dest.M31 = src.M31; dest.M32 = src.M32; dest.M33 = src.M33; dest.M34 = src.M34;
            dest.M41 = 0; dest.M42 = 0; dest.M43 = 0; dest.M44 = 1;
        }

        /// <summary>
        /// Multiplies a matrix with a vector. The calculation is Matrix.CreateTranslation('v') *
        /// 'mat'.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Matrix Multiply(Vector3 v, Matrix mat)
        {
            Matrix vMat = Matrix.CreateTranslation(v);
            return vMat * mat;
        }

        /// <summary>
        /// Orthonormalizes a transformation matrix.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static Matrix OrthonormalizeMatrix(Matrix mat)
        {
            Matrix m = mat;

            Vector3 axisX = Vector3Helper.Get(m.M11, m.M12, m.M13);
            Vector3 axisY = Vector3Helper.Get(m.M21, m.M22, m.M23);
            Vector3 axisZ = Vector3Helper.Get(m.M31, m.M32, m.M33);

            axisX.Normalize();
            axisY.Normalize();
            axisZ.Normalize();

            axisZ = Vector3.Normalize(Vector3.Cross(axisX, axisY));
            axisY = Vector3.Normalize(Vector3.Cross(axisZ, axisX));
            axisX = Vector3.Normalize(Vector3.Cross(axisY, axisZ));

            m.M11 = axisX.X; m.M12 = axisX.Y; m.M13 = axisX.Z;
            m.M21 = axisY.X; m.M22 = axisY.Y; m.M23 = axisY.Z;
            m.M31 = axisZ.X; m.M32 = axisZ.Y; m.M33 = axisZ.Z;

            return m;
        }

        /// <summary>
        /// Checks whether a transformation has changed/moved significantly compared to the previous
        /// transformation with 0.01f translational threshold and 0.1f * Math.PI / 180 rotational
        /// threshold. This means that if either the transformation's translation component changed
        /// more than 0.1f in distance or rotation component changed more than 0.1f * Math.PI / 180
        /// radians in any of the three (x, y, z) directions, then it's judged as having moved significantly.
        /// </summary>
        /// <param name="matPrev">The previous transformation matrix</param>
        /// <param name="matCurr">The current transformation matrix</param>
        /// <returns></returns>
        public static bool HasMovedSignificantly(Matrix matPrev, Matrix matCurr)
        {
            return HasMovedSignificantly(matPrev, matCurr, 0.01f, 0.1f * (float)Math.PI / 180);
        }
        
        /// <summary>
        /// Checks whether a transformation has changed/moved significantly compared to the previous
        /// transformation with the specified translational threshold and rotational threshold. 
        /// </summary>
        /// <param name="matPrev">The previous transformation matrix</param>
        /// <param name="matCurr">The current transformation matrix</param>
        /// <param name="transThreshold">The translational threshold</param>
        /// <param name="rotThreshold">The rotational threshold</param>
        /// <returns></returns>
        public static bool HasMovedSignificantly(Matrix matPrev, Matrix matCurr,
            float transThreshold, float rotThreshold)
        {
            // 1st time through
            if (matPrev.Equals(Matrix.Identity))
                return true;

            // Test translation
            if (Vector3.Distance(matPrev.Translation, matCurr.Translation) > transThreshold)
                return true;

            // Test rotations
            float dRollPrev, dPitchPrev, dYawPrev, dRollCurr, dPitchCurr, dYawCurr;
            Vector3 dPrev = Vector3Helper.ExtractAngles(matPrev);
            Vector3 dCurr = Vector3Helper.ExtractAngles(matCurr);

            dPitchPrev = dPrev.X;
            dYawPrev = dPrev.Y;
            dRollPrev = dPrev.Z;
            dPitchCurr = dCurr.X;
            dYawCurr = dCurr.Y;
            dRollCurr = dCurr.Z;

            if (Math.Abs(dRollPrev - dRollCurr) > rotThreshold)
                return true;

            if (Math.Abs(dPitchPrev - dPitchCurr) > rotThreshold)
                return true;

            if (Math.Abs(dYawPrev - dYawCurr) > rotThreshold)
                return true;

            // Not enough movement
            return false;
        }

        /// <summary>
        /// Convert every elements of the matrix to bytes.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static byte[] ConvertToUnoptimizedBytes(Matrix mat)
        {
            List<float> data = new List<float>();
            data.Add(mat.M11); data.Add(mat.M12); data.Add(mat.M13); data.Add(mat.M14);
            data.Add(mat.M21); data.Add(mat.M22); data.Add(mat.M23); data.Add(mat.M24);
            data.Add(mat.M31); data.Add(mat.M32); data.Add(mat.M33); data.Add(mat.M34);
            data.Add(mat.M41); data.Add(mat.M42); data.Add(mat.M43); data.Add(mat.M44);

            return ByteHelper.ConvertFloatArray(data);
        }

        /// <summary>
        /// Decompose the matrix into rotation (Quaternion: 4 floats), scale (3 floats) if
        /// the scale is not Vector.One, and translation (3 floats), and pack these information
        /// into an array of bytes for efficiently transfering over the network.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static byte[] ConvertToOptimizedBytes(Matrix mat)
        {
            Quaternion rot;
            Vector3 scale;
            Vector3 trans;
            mat.Decompose(out scale, out rot, out trans);

            List<float> data = new List<float>();
            data.Add(rot.X);
            data.Add(rot.Y);
            data.Add(rot.Z);
            data.Add(rot.W);
            data.Add(trans.X);
            data.Add(trans.Y);
            data.Add(trans.Z);

            // Send scale information if and only if its not Vector.One
            if (Vector3.Distance(scale, Vector3.One) > 0.00001f)
            {
                data.Add(scale.X);
                data.Add(scale.Y);
                data.Add(scale.Z);
            }

            return ByteHelper.ConvertFloatArray(data);
        }

        /// <summary>
        /// Converts an array of bytes containing transformation (rotation, scale, and
        /// translation) into a matrix. Use this method to convert back the information
        /// packed by ConvertToBytes method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <see cref="ConvertToBytes"/>
        /// <returns></returns>
        public static Matrix ConvertFromOptimizedBytes(byte[] bytes)
        {
            Matrix mat = Matrix.Identity;
            ConvertFromOptimizedBytes(bytes, ref mat);
            return mat;
        }

        /// <summary>
        /// Converts an array of bytes containing transformation (rotation, scale, and
        /// translation) into a matrix. Use this method to convert back the information
        /// packed by ConvertToBytes method.
        /// </summary>
        /// <param name="bytes"></param>
        /// <see cref="ConvertToBytes"/>
        /// <returns></returns>
        public static void ConvertFromOptimizedBytes(byte[] bytes, ref Matrix mat)
        {
            Quaternion rot = new Quaternion(
                ByteHelper.ConvertToFloat(bytes, 0),
                ByteHelper.ConvertToFloat(bytes, 4),
                ByteHelper.ConvertToFloat(bytes, 8),
                ByteHelper.ConvertToFloat(bytes, 12));

            Vector3 trans = new Vector3(
                ByteHelper.ConvertToFloat(bytes, 16),
                ByteHelper.ConvertToFloat(bytes, 20),
                ByteHelper.ConvertToFloat(bytes, 24));

            Vector3 scale = Vector3.One;
            if (bytes.Length > 28)
            {
                scale = new Vector3(
                    ByteHelper.ConvertToFloat(bytes, 28),
                    ByteHelper.ConvertToFloat(bytes, 32),
                    ByteHelper.ConvertToFloat(bytes, 36));
            }

            mat = Matrix.CreateScale(scale) *
                Matrix.CreateFromQuaternion(rot) * Matrix.CreateTranslation(trans);
        }

        public static Matrix ConvertFromUnoptimizedBytes(byte[] bytes)
        {
            Matrix mat = Matrix.Identity;
            ConvertFromUnoptimizedBytes(bytes, ref mat);
            return mat;
        }

        public static void ConvertFromUnoptimizedBytes(byte[] bytes, ref Matrix mat)
        {
            mat.M11 = BitConverter.ToSingle(bytes, 0);
            mat.M12 = BitConverter.ToSingle(bytes, 4);
            mat.M13 = BitConverter.ToSingle(bytes, 8);
            mat.M14 = BitConverter.ToSingle(bytes, 12);

            mat.M21 = BitConverter.ToSingle(bytes, 16);
            mat.M22 = BitConverter.ToSingle(bytes, 20);
            mat.M23 = BitConverter.ToSingle(bytes, 24);
            mat.M24 = BitConverter.ToSingle(bytes, 28);

            mat.M31 = BitConverter.ToSingle(bytes, 32);
            mat.M32 = BitConverter.ToSingle(bytes, 36);
            mat.M33 = BitConverter.ToSingle(bytes, 40);
            mat.M34 = BitConverter.ToSingle(bytes, 44);

            mat.M41 = BitConverter.ToSingle(bytes, 48);
            mat.M42 = BitConverter.ToSingle(bytes, 52);
            mat.M43 = BitConverter.ToSingle(bytes, 56);
            mat.M44 = BitConverter.ToSingle(bytes, 60);
        }

        public static Matrix FromString(String matVals)
        {
            Matrix mat = Matrix.Identity;

            String[] strs = matVals.Split(':', ' ', '{', '}');
            mat.M11 = float.Parse(strs[4]);
            mat.M12 = float.Parse(strs[6]);
            mat.M13 = float.Parse(strs[8]);
            mat.M14 = float.Parse(strs[10]);

            mat.M21 = float.Parse(strs[14]);
            mat.M22 = float.Parse(strs[16]);
            mat.M23 = float.Parse(strs[18]);
            mat.M24 = float.Parse(strs[20]);

            mat.M31 = float.Parse(strs[24]);
            mat.M32 = float.Parse(strs[26]);
            mat.M33 = float.Parse(strs[28]);
            mat.M34 = float.Parse(strs[30]);

            mat.M41 = float.Parse(strs[34]);
            mat.M42 = float.Parse(strs[36]);
            mat.M43 = float.Parse(strs[38]);
            mat.M44 = float.Parse(strs[40]);

            return mat;
        }

        /// <summary>
        /// An empty (all zero) matrix.
        /// </summary>
        public static Matrix Empty
        {
            get { return emptyMatrix; }
        }

        /// <summary>
        /// Converts a matrix to an array of 16 floats.
        /// </summary>
        /// <param name="mat"></param>
        /// <returns></returns>
        public static float[] ToFloats(Matrix mat)
        {
            float[] floats = {mat.M11, mat.M12, mat.M13, mat.M14, mat.M21, mat.M22, mat.M23, mat.M24, 
                mat.M31, mat.M32, mat.M33, mat.M34, mat.M41, mat.M42, mat.M43, mat.M44};

            return floats;
        }

        public static void ToFloats(ref Matrix mat, float[] floats)
        {
            floats[0] = mat.M11; floats[1] = mat.M12; floats[2] = mat.M13; floats[3] = mat.M14;
            floats[4] = mat.M21; floats[5] = mat.M22; floats[6] = mat.M23; floats[7] = mat.M24;
            floats[8] = mat.M31; floats[9] = mat.M32; floats[10] = mat.M33; floats[11] = mat.M34;
            floats[12] = mat.M41; floats[13] = mat.M42; floats[14] = mat.M43; floats[15] = mat.M44;
        }

        /// <summary>
        /// Prints out a matrix to the console.
        /// </summary>
        /// <param name="mat"></param>
        public static void PrintMatrix(Matrix mat)
        {
            Console.WriteLine(mat.M11 + " " + mat.M21 + " " + mat.M31 + " " + mat.M41);
            Console.WriteLine(mat.M12 + " " + mat.M22 + " " + mat.M32 + " " + mat.M42);
            Console.WriteLine(mat.M13 + " " + mat.M23 + " " + mat.M33 + " " + mat.M43);
            Console.WriteLine(mat.M14 + " " + mat.M24 + " " + mat.M34 + " " + mat.M44);
            Console.WriteLine("");
        }

        /// <summary>
        /// Converts a rotation vector into a rotation matrix.
        /// </summary>
        /// <param name="Rotation"></param>
        /// <remarks>
        /// http://www.innovativegames.net/blog/blog/2009/03/18/matrices-quaternions-and-euler-angle-vectors/
        /// </remarks>
        /// <returns></returns>
        public static Matrix Vector3ToMatrix(Vector3 Rotation)
        {
            return Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
        }
    }
}
