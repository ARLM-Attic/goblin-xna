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
 * Authors: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace GoblinXNA.Device.Util
{
    /// <summary>
    /// A helper class for smoothing out the incoming matrix values based on double 
    /// exponential smoothing (DES) algorithm (The equations can be found at
    /// http://www.itl.nist.gov/div898/handbook/pmc/section4/pmc433.htm). We use
    /// 'b1 = y2 - y1' equation to set the initial value of b1.
    /// </summary>
    public class DESSmoother : ISmoother
    {
        #region Member Fields

        protected const int RESTART_THRESHOLD = 5;
        protected Vector3 transSt, transPrevSt;
        protected Vector3 transBt, transPrevBt;
        protected Vector3 tmpTrans;
        protected Quaternion rotSt, rotPrevSt;
        protected Quaternion rotBt, rotPrevBt;
        protected Quaternion tmpRot;

        protected Vector3 prevRawP;
        protected Quaternion prevRawQ;

        protected Matrix prevComputedMat;

        protected float transAlpha;
        protected float transGamma;

        protected float rotAlpha;
        protected float rotGamma;

        protected float transThreshold;
        protected float rotThreshold;

        protected bool initialized;
        protected int restartCount;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a DES smoother with alpha and gamma values ([0.0f - 1.0f] excluding 0) 
        /// without translation or rotational threhold. Both translation and rotation alpha 
        /// and gamma values will be set to 'alpha' and 'gamma' respectively.
        /// </summary>
        /// <param name="alpha">The alpha value used in the first equation (see the website
        /// mentioned in the summary of this class). The larger the alpha value, the heavier 
        /// the weight of the incoming matrix. If alpha is 0.3f, then the smoothed matrix will 
        /// be roughly incoming * 0.3f + previous * 0.7f.</param>
        /// <param name="gamma">The gamma value used in the second equation.</param>
        /// <exception cref="ArgumentException">If alpha or gamma values are outside of the range</exception>
        public DESSmoother(float alpha, float gamma) 
            : this(alpha, gamma, alpha, gamma, -1, -1) { }

        /// <summary>
        /// Creates a DES smoother with alpha and gamma values ([0.0f - 1.0f] excluding 0)  
        /// separately for translational and rotational smoothing without translation or rotational 
        /// threhold.
        /// </summary>
        /// <param name="transAlpha">An alpha value for translational smoothing</param>
        /// <param name="transGamma">A gamma value for translational smoothing</param>
        /// <param name="rotAlpha">An alpha value for rotational smoothing</param>
        /// <param name="rotGamma">A gamma value for rotational smoothing</param>
        /// <exception cref="ArgumentException">If alpha or gamma values are outside of the range</exception>
        public DESSmoother(float transAlpha, float transGamma, float rotAlpha, float rotGamma)
            : this(transAlpha, transGamma, rotAlpha, rotGamma, -1, -1) { }

        /// <summary>
        /// Creates a smoother with alpha and gamma values ([0.0f - 1.0f] excluding 0) separately for 
        /// translational and rotational smoothing with translation or rotational threhold.
        /// </summary>
        /// <param name="transAlpha">An alpha value for translational smoothing</param>
        /// <param name="transGamma">A gamma value for translational smoothing</param>
        /// <param name="rotAlpha">An alpha value for rotational smoothing</param>
        /// <param name="rotGamma">A gamma value for rotational smoothing</param>
        /// <param name="transThreshold">A translational threshold. If set to larger than 0, then
        /// if the distance between the previous transformation and the current transformation is larger
        /// than this threshold, then the current transformation will be dropped from the smoothing
        /// calculation.</param>
        /// <param name="rotThreshold">A rotational threshold. If set to largen than 0, then
        /// if the angle (in radians) between the previous transformation and the current transformation
        /// is larger than this threshold, then the current transformation will be dropped from the
        /// smoothing calculation.</param>
        /// <exception cref="ArgumentException">If alpha or gamma values are outside of the range</exception>
        public DESSmoother(float transAlpha, float transGamma, float rotAlpha, float rotGamma, 
            float transThreshold, float rotThreshold)
        {
            if (transAlpha <= 0 || transAlpha > 1 || rotAlpha <= 0 || rotAlpha > 1)
                throw new ArgumentException("alpha value has to be between 0.0f and 1.0f excluding 0");
            if (transGamma < 0 || transGamma > 1 || rotGamma < 0 || rotGamma > 1)
                throw new ArgumentException("gamma value has to be between 0.0f and 1.0f");

            this.transAlpha = transAlpha;
            this.transGamma = transGamma;
            this.rotAlpha = rotAlpha;
            this.rotGamma = rotGamma;

            this.transThreshold = transThreshold;
            this.rotThreshold = rotThreshold;

            transSt = Vector3.Zero;
            transPrevSt = Vector3.Zero;
            transBt = Vector3.Zero;
            transPrevBt = Vector3.Zero;
            tmpTrans = Vector3.Zero;

            rotSt = Quaternion.Identity;
            rotPrevSt = Quaternion.Identity;
            rotBt = Quaternion.Identity;
            rotPrevBt = Quaternion.Identity;
            tmpRot = Quaternion.Identity;

            prevRawP = Vector3.Zero;
            prevRawQ = Quaternion.Identity;

            prevComputedMat = Matrix.Identity;

            initialized = false;
            restartCount = 0;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a filtered matrix using double exponential smoothing algorithm.
        /// </summary>
        /// <param name="p">The original position</param>
        /// <param name="q">The original rotation</param>
        /// <returns>A smoothed matrix</returns>
        public Matrix FilterMatrix(Vector3 p, Quaternion q)
        {
            if (initialized)
            {
                bool compute = true;

                // If translation threshold is set to larger than 0, then check whether
                // the current transformation and previous transformation has larger translation
                // difference than the threhold. If it is, then we ignore the currently passed
                // transformation, and return the previously smoothed transformation.
                if (transThreshold > 0)
                {
                    float dist = Vector3.Distance(p, prevRawP);
                    if (dist > transThreshold)
                        compute = false;
                }

                // If rotational threshold is set to larger than 0, then check whether
                // the current transformation and previous transformation has larger angle
                // difference than the threhold. If it is, then we ignore the currently passed
                // transformation, and return the previously smoothed transformation.
                if (rotThreshold > 0)
                {
                    q.Normalize();
                    prevRawQ.Normalize();
                    float dotProduct = Quaternion.Dot(q, prevRawQ);
                    if (dotProduct > 1)
                        dotProduct = 1;
                    else if (dotProduct < -1)
                        dotProduct = -1;
                    float angleDiff = (float)Math.Acos(dotProduct);
                    if (angleDiff > rotThreshold)
                        compute = false;
                }

                if (compute)
                {
                    prevComputedMat = ComputeDESMatrix(p, q);

                    prevRawP = p;
                    prevRawQ = q;

                    restartCount = 0;
                }
                else
                {
                    restartCount++;
                    if (restartCount > RESTART_THRESHOLD)
                    {
                        prevRawP = p;
                        prevRawQ = q;

                        restartCount = 0;
                    }
                }

                return prevComputedMat;
            }
            else
            {
                if (tmpTrans.Equals(Vector3.Zero))
                {
                    tmpTrans = p;
                    tmpRot = q;
                }
                else
                {
                    transPrevSt = p;
                    transPrevBt = p - tmpTrans;

                    rotPrevSt = q;
                    rotPrevBt = q;

                    prevRawP = p;
                    prevRawQ = q;

                    initialized = true;
                }

                // return unfiltered matrix since can not smooth yet
                return Matrix.CreateFromQuaternion(q) * Matrix.CreateTranslation(p);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Performs the DES algorithm.
        /// </summary>
        /// <returns></returns>
        private Matrix ComputeDESMatrix(Vector3 p, Quaternion q)
        {
            Matrix desResult = Matrix.Identity;

            // If translational alpha is 1, then no need to perform smoothing
            if (transAlpha == 1)
                transSt = p;
            else
            {
                // Compute the translational smoothing
                transSt = transAlpha * p + (1 - transAlpha) * (transPrevSt + transPrevBt);
                transBt = transGamma * (transSt - transPrevSt) + (1 - transGamma) * transPrevBt;

                transPrevSt = transSt;
                transPrevBt = transBt;
            }

            // If rotational alpha is 1, then no need to perform smoothing
            if (rotAlpha == 1)
                rotSt = q;
            else
            {
                // Compute the rotational smoothing (for rotation, the difference between
                // the two rotation is q1(inverse) * q2
                rotSt = Quaternion.Multiply(q, rotAlpha) +
                    Quaternion.Multiply(rotPrevSt + rotPrevBt, 1 - rotAlpha);
                rotBt = Quaternion.Multiply(rotSt - rotPrevSt, rotGamma) +
                    Quaternion.Multiply(rotPrevBt, 1 - rotGamma);

                rotSt.Normalize();
                rotBt.Normalize();

                rotPrevSt = rotSt;
                rotPrevBt = rotBt;
            }

            desResult = Matrix.CreateFromQuaternion(rotSt) * Matrix.CreateTranslation(transSt);

            return desResult;
        }

        #endregion
    }
}
