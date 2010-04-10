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
using System.Runtime.InteropServices;

namespace GoblinXNA.Helpers
{
    /// <summary>
    /// A helper class that implements various useful functions to convert between array of bytes
    /// and other types.
    /// </summary>
    public class ByteHelper
    {
        /// <summary>
        /// Converts a string to an array of bytes.
        /// </summary>
        /// <param name="s">A string to be converted</param>
        /// <returns></returns>
        public static byte[] ConvertToByte(String s)
        {
            return System.Text.Encoding.ASCII.GetBytes(s);
        }

        /// <summary>
        /// Converts an array of bytes to a string.
        /// </summary>
        /// <param name="b">An array of bytes</param>
        /// <returns></returns>
        public static String ConvertToString(byte[] b)
        {
            return System.Text.Encoding.ASCII.GetString(b);
        }

        /// <summary>
        /// Converts 4 bytes to a single-precision floating number.
        /// </summary>
        /// <param name="b">An array of bytes with length of at least 4</param>
        /// <param name="startIndex">4 bytes are taken from this start index in the byte array</param>
        /// <returns></returns>
        public static float ConvertToFloat(byte[] b, int startIndex)
        {
            return BitConverter.ToSingle(b, startIndex);
        }

        /// <summary>
        /// Converts 4 bytes to a 32-bit integer value.
        /// </summary>
        /// <param name="b">An array of bytes with length of at least 4</param>
        /// <param name="startIndex">4 bytes are taken from this start index in the byte array</param>
        /// <returns></returns>
        public static int ConvertToInt(byte[] b, int startIndex)
        {
            return BitConverter.ToInt32(b, startIndex);
        }

        /// <summary>
        /// Converts 2 bytes to a 16-bit integer value.
        /// </summary>
        /// <param name="b">An array of bytes with length of at least 2</param>
        /// <param name="startIndex">2 bytes are taken from this start index in the byte array</param>
        /// <returns></returns>
        public static short ConvertToShort(byte[] b, int startIndex)
        {
            return BitConverter.ToInt16(b, startIndex);
        }

        /// <summary>
        /// Converts a list of single-precision floating numbers to an array of bytes.
        /// </summary>
        /// <param name="floats">A list of single-precision floating numbers</param>
        /// <returns>An array of bytes with size of 4 * (number of floats)</returns>
        public static byte[] ConvertFloatArray(List<float> floats)
        {
            byte[] b = new byte[floats.Count * 4];
            for (int i = 0; i < floats.Count; i++)
                FillByteArray(ref b, i * 4, BitConverter.GetBytes(floats[i]));

            return b;
        }

        /// <summary>
        /// Converts a list of 32-bit integer numbers to an array of bytes.
        /// </summary>
        /// <param name="ints">A list of 32-bit integer numbers</param>
        /// <returns>An array of bytes with size of 4 * (number of ints)</returns>
        public static byte[] ConvertIntArray(List<int> ints)
        {
            byte[] b = new byte[ints.Count * sizeof(int)];
            for (int i = 0; i < ints.Count; i++)
                FillByteArray(ref b, i * sizeof(int), BitConverter.GetBytes(ints[i]));

            return b;
        }

        /// <summary>
        /// Converts a list of 16-bit integer numbers to an array of bytes.
        /// </summary>
        /// <param name="shorts">A list of 16-bit integer numbers</param>
        /// <returns>An array of bytes with size of 2 * (number of shorts)</returns>
        public static byte[] ConvertShortArray(List<short> shorts)
        {
            byte[] b = new byte[shorts.Count * sizeof(short)];
            for (int i = 0; i < shorts.Count; i++)
                FillByteArray(ref b, i * sizeof(short), BitConverter.GetBytes(shorts[i]));

            return b;
        }

        /// <summary>
        /// Fills the given dest byte array from the destStartIndex with the entire src byte array
        /// </summary>
        /// <remarks>
        /// If the source contains more than (dest.Length - destStartIndex) bytes, then the overflowed 
        /// bytes are not copied into the destination array..
        /// </remarks>
        /// <param name="dest">The destination where the source array will be copied</param>
        /// <param name="destStartIndex">The index of the destination array where the copy starts</param>
        /// <param name="src">The source array where to copy from</param>
        public static void FillByteArray(ref byte[] dest, int destStartIndex, byte[] src)
        {
            int length = (src.Length > (dest.Length - destStartIndex)) ?
                (dest.Length - destStartIndex) : src.Length;
            Array.Copy(src, 0, dest, destStartIndex, length);
        }

        /// <summary>
        /// Concatenates two byte arrays.
        /// </summary>
        /// <param name="b1">The first byte array to be concatenated</param>
        /// <param name="b2">The second byte array to be concatenated</param>
        /// <returns>The concatenated byte array with size of b1 + b2</returns>
        public static byte[] ConcatenateBytes(byte[] b1, byte[] b2)
        {
            byte[] b = new byte[b1.Length + b2.Length];

            Array.Copy(b1, 0, b, 0, b1.Length);
            Array.Copy(b2, 0, b, b1.Length, b2.Length);

            return b;
        }

        /// <summary>
        /// Truncates an array of bytes from the startIndex for the specified length.
        /// </summary>
        /// <param name="value">A byte array to be truncated</param>
        /// <param name="startIndex">The index in the 'value' array where truncation starts</param>
        /// <param name="length">The length of the new truncated array</param>
        /// <returns>The truncated byte array with size of 'length'</returns>
        public static byte[] Truncate(byte[] value, int startIndex, int length)
        {
            byte[] b = new byte[length];
            Array.Copy(value, startIndex, b, 0, length);

            return b;
        }
    }
}
