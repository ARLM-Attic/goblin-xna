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
 * Authors: Levi Lister
 *          Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace GoblinXNA.Device.Vision.Util
{
    /// <summary>
    /// A helper class for smoothing out the incoming matrix values based on the history of the
    /// previous matrices. This class is mainly used for smoothing out the pose matrix returned
    /// by the optical marker tracker to provide a smoother transition. A simplified DESP smoothing
    /// algorithm is used to perform the smoothing.
    /// </summary>
    public class Smoother
    {
        // Added by Levi for jitter compensation (4/19/07)
        protected List<Matrix> m_lstPoseHistory;
        protected Matrix m_Spt1, m_Spt2;
        protected float ALPHA = 0.3f;

        /// <summary>
        /// Creates a smoother with an alpha value that defines the weight of the incoming
        /// matrix and a history queue size of 10.
        /// </summary>
        /// <param name="alpha">A value in the range of [0.0f -- 1.0f] excluding 0 and 1. The larger the
        /// alpha value, the heavier the weight of the incoming matrix. If alpha is 0.3f,
        /// then the smoothed matrix will be incoming * 0.3f + history * 0.7f.</param>
        public Smoother(float alpha) : this(alpha, 10) { }

        /// <summary>
        /// Creates a smoother with an alpha value that defines the weight of the incoming
        /// matrix and a history queue size.
        /// </summary>
        /// <param name="alpha">A value in the range of [0.0f -- 1.0f] excluding 0 and 1. The larger the
        /// alpha value, the heavier the weight of the incoming matrix. If alpha is 0.3f,
        /// then the smoothed matrix will be incoming * 0.3f + history * 0.7f.</param>
        /// <param name="postHistorySize">The queue size of the history</param>
        public Smoother(float alpha, int postHistorySize)
        {
            if (alpha <= 0 || alpha >= 1)
                throw new ArgumentException("Alpha value has to be in the range [0, 1] excluding 0 and 1");

            this.ALPHA = alpha;
            this.m_lstPoseHistory = new List<Matrix>(postHistorySize);
            this.m_Spt1 = new Matrix();
            this.m_Spt2 = new Matrix();
        }

        /// <summary>
        /// Gets a filtered matrix based on the incoming matrix values and the history of
        /// previous matrices.
        /// </summary>
        /// <param name="mat">A new incoming matrix</param>
        /// <returns>A smoothed matrix</returns>
        public virtual Matrix FilterMatrix(Matrix mat)
        {
            // Is the current matrix different from the last (handles multiple calls per frame)
            if (m_lstPoseHistory.Count > 0)
            {
                if (!mat.Equals(m_lstPoseHistory[m_lstPoseHistory.Count - 1]))
                {
                    // remove from the front
                    m_lstPoseHistory.RemoveAt(0);

                    // add to the back
                    m_lstPoseHistory.Add(mat);
                }
                return GetDESPMatrix();
            }
            else
            {
                // Not enough data to smooth, just use latest unfiltered pose
                m_lstPoseHistory.Add(mat);
                return m_lstPoseHistory[m_lstPoseHistory.Count - 1];
            }
        }

        /// <summary>
        /// Performs the simplified DESP algorithm.
        /// </summary>
        /// <returns></returns>
        protected virtual Matrix GetDESPMatrix()
        {
            Matrix despResult = new Matrix();
            Matrix pt = m_lstPoseHistory[m_lstPoseHistory.Count - 1];
            Matrix b0 = new Matrix();
            Matrix b1 = new Matrix();

            m_Spt1.M11 = ALPHA * pt.M11 + (1 - ALPHA) * m_Spt1.M11;
            m_Spt2.M11 = ALPHA * m_Spt1.M11 + (1 - ALPHA) * m_Spt2.M11;

            b1.M11 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M11 - m_Spt2.M11);
            b0.M11 = 2 * m_Spt1.M11 - m_Spt2.M11 - b1.M11;

            despResult.M11 = b0.M11 + b1.M11;

            m_Spt1.M12 = ALPHA * pt.M12 + (1 - ALPHA) * m_Spt1.M12;
            m_Spt2.M12 = ALPHA * m_Spt1.M12 + (1 - ALPHA) * m_Spt2.M12;

            b1.M12 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M12 - m_Spt2.M12);
            b0.M12 = 2 * m_Spt1.M12 - m_Spt2.M12 - b1.M12;

            despResult.M12 = b0.M12 + b1.M12;

            m_Spt1.M13 = ALPHA * pt.M13 + (1 - ALPHA) * m_Spt1.M13;
            m_Spt2.M13 = ALPHA * m_Spt1.M13 + (1 - ALPHA) * m_Spt2.M13;

            b1.M13 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M13 - m_Spt2.M13);
            b0.M13 = 2 * m_Spt1.M13 - m_Spt2.M13 - b1.M13;

            despResult.M13 = b0.M13 + b1.M13;

            m_Spt1.M14 = ALPHA * pt.M14 + (1 - ALPHA) * m_Spt1.M14;
            m_Spt2.M14 = ALPHA * m_Spt1.M14 + (1 - ALPHA) * m_Spt2.M14;

            b1.M14 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M14 - m_Spt2.M14);
            b0.M14 = 2 * m_Spt1.M14 - m_Spt2.M14 - b1.M14;

            despResult.M14 = b0.M14 + b1.M14;

            m_Spt1.M21 = ALPHA * pt.M21 + (1 - ALPHA) * m_Spt1.M21;
            m_Spt2.M21 = ALPHA * m_Spt1.M21 + (1 - ALPHA) * m_Spt2.M21;

            b1.M21 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M21 - m_Spt2.M21);
            b0.M21 = 2 * m_Spt1.M21 - m_Spt2.M21 - b1.M21;

            despResult.M21 = b0.M21 + b1.M21;

            m_Spt1.M22 = ALPHA * pt.M22 + (1 - ALPHA) * m_Spt1.M22;
            m_Spt2.M22 = ALPHA * m_Spt1.M22 + (1 - ALPHA) * m_Spt2.M22;

            b1.M22 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M22 - m_Spt2.M22);
            b0.M22 = 2 * m_Spt1.M22 - m_Spt2.M22 - b1.M22;

            despResult.M22 = b0.M22 + b1.M22;

            m_Spt1.M23 = ALPHA * pt.M23 + (1 - ALPHA) * m_Spt1.M23;
            m_Spt2.M23 = ALPHA * m_Spt1.M23 + (1 - ALPHA) * m_Spt2.M23;

            b1.M23 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M23 - m_Spt2.M23);
            b0.M23 = 2 * m_Spt1.M23 - m_Spt2.M23 - b1.M23;

            despResult.M23 = b0.M23 + b1.M23;

            m_Spt1.M24 = ALPHA * pt.M24 + (1 - ALPHA) * m_Spt1.M24;
            m_Spt2.M24 = ALPHA * m_Spt1.M24 + (1 - ALPHA) * m_Spt2.M24;

            b1.M24 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M24 - m_Spt2.M24);
            b0.M24 = 2 * m_Spt1.M24 - m_Spt2.M24 - b1.M24;

            despResult.M24 = b0.M24 + b1.M24;

            m_Spt1.M31 = ALPHA * pt.M31 + (1 - ALPHA) * m_Spt1.M31;
            m_Spt2.M31 = ALPHA * m_Spt1.M31 + (1 - ALPHA) * m_Spt2.M31;

            b1.M31 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M31 - m_Spt2.M31);
            b0.M31 = 2 * m_Spt1.M31 - m_Spt2.M31 - b1.M31;

            despResult.M31 = b0.M31 + b1.M31;

            m_Spt1.M32 = ALPHA * pt.M32 + (1 - ALPHA) * m_Spt1.M32;
            m_Spt2.M32 = ALPHA * m_Spt1.M32 + (1 - ALPHA) * m_Spt2.M32;

            b1.M32 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M32 - m_Spt2.M32);
            b0.M32 = 2 * m_Spt1.M32 - m_Spt2.M32 - b1.M32;

            despResult.M32 = b0.M32 + b1.M32;

            m_Spt1.M33 = ALPHA * pt.M33 + (1 - ALPHA) * m_Spt1.M33;
            m_Spt2.M33 = ALPHA * m_Spt1.M33 + (1 - ALPHA) * m_Spt2.M33;

            b1.M33 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M33 - m_Spt2.M33);
            b0.M33 = 2 * m_Spt1.M33 - m_Spt2.M33 - b1.M33;

            despResult.M33 = b0.M33 + b1.M33;

            m_Spt1.M34 = ALPHA * pt.M34 + (1 - ALPHA) * m_Spt1.M34;
            m_Spt2.M34 = ALPHA * m_Spt1.M34 + (1 - ALPHA) * m_Spt2.M34;

            b1.M34 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M34 - m_Spt2.M34);
            b0.M34 = 2 * m_Spt1.M34 - m_Spt2.M34 - b1.M34;

            despResult.M34 = b0.M34 + b1.M34;

            m_Spt1.M41 = ALPHA * pt.M41 + (1 - ALPHA) * m_Spt1.M41;
            m_Spt2.M41 = ALPHA * m_Spt1.M41 + (1 - ALPHA) * m_Spt2.M41;

            b1.M41 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M41 - m_Spt2.M41);
            b0.M41 = 2 * m_Spt1.M41 - m_Spt2.M41 - b1.M41;

            despResult.M41 = b0.M41 + b1.M41;

            m_Spt1.M42 = ALPHA * pt.M42 + (1 - ALPHA) * m_Spt1.M42;
            m_Spt2.M42 = ALPHA * m_Spt1.M42 + (1 - ALPHA) * m_Spt2.M42;

            b1.M42 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M42 - m_Spt2.M42);
            b0.M42 = 2 * m_Spt1.M42 - m_Spt2.M42 - b1.M42;

            despResult.M42 = b0.M42 + b1.M42;

            m_Spt1.M43 = ALPHA * pt.M43 + (1 - ALPHA) * m_Spt1.M43;
            m_Spt2.M43 = ALPHA * m_Spt1.M43 + (1 - ALPHA) * m_Spt2.M43;

            b1.M43 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M43 - m_Spt2.M43);
            b0.M43 = 2 * m_Spt1.M43 - m_Spt2.M43 - b1.M43;

            despResult.M43 = b0.M43 + b1.M43;

            m_Spt1.M44 = ALPHA * pt.M44 + (1 - ALPHA) * m_Spt1.M44;
            m_Spt2.M44 = ALPHA * m_Spt1.M44 + (1 - ALPHA) * m_Spt2.M44;

            b1.M44 = (ALPHA / (1 - ALPHA)) * (m_Spt1.M44 - m_Spt2.M44);
            b0.M44 = 2 * m_Spt1.M44 - m_Spt2.M44 - b1.M44;

            despResult.M44 = b0.M44 + b1.M44;

            return despResult;
        }    
    }
}
