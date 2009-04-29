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
 * Authors: Mark Eaddy
 *          Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Diagnostics;

using Microsoft.Xna.Framework;

using System.Xml.Serialization;

namespace GoblinXNA.Device.InterSense
{
	/// <summary>
	/// Summary description for InterSenseStation.
	/// </summary>
	public class InterSenseStation
    {
        #region Member Fields
		private long nStationIndex;

		//the head transform is: (0, -0.1397, -0.1524), as x,y,z in intersense format.
		// We make it DX format
		private Matrix matTRANSFORM_TRACKER2EYE;
		//NOTE: removed readonly above, so it could be serialized
		//also removed static, so each tracker can have its own transform (seems possible)

		private Matrix mat;
		private ISDllBridge.ISD_STATION_STATE_TYPE state;
        #endregion

        #region Constructors

        public InterSenseStation(long _nStationIndex, Matrix tracker_to_eye_transform)
		{
			nStationIndex = _nStationIndex;
			matTRANSFORM_TRACKER2EYE = tracker_to_eye_transform;

            matTRANSFORM_TRACKER2EYE = Matrix.Identity;
            mat = Matrix.Identity;
        }

        #endregion

        #region Properties

        public Matrix WorldTransformation
        {
            get { return mat; }
        }

        public Matrix TransformTrackerToEye
        {
            get { return matTRANSFORM_TRACKER2EYE; }
            set { matTRANSFORM_TRACKER2EYE = value; }
        }

        #endregion

        #region Public Methods

        public void SetData(ISDllBridge.ISD_TRACKER_DATA_TYPE dataISense)
		{
			Debug.Assert(nStationIndex != -1);
			state = dataISense.Station[nStationIndex];
			CreateDXTransformationMatrix();
        }

        #endregion

        #region Private Methods

        // Position[0],[1], and [3] correspond to x, y, and z,
        // so you might expect Orientation[0], [1], and [2] to
        // correspond to rotation around the x, y, and z axes.  However,
        // InterSense appears to prefer the order implied by the traditional
        // order of "yaw,pitch,roll".  Orientation[2] is actually "yaw"
        // (rotation around z-axis), Orientation[1] is "pitch" (rotation around
        // y-axis) and Orientation[0] is actually "roll" (rotation around x-axis).
        private void CreateDXTransformationMatrix()
		{
			//Trace.WriteLine(string.Format("ISENSE xrot:{0,4:F}, yrot:{1,4:F}, zrot:{2,4:F}",
			//	aryOrientation[2], aryOrientation[1], aryOrientation[0]));

			// Convert InterSense position and orientation into a transformation
			// matrix appropriate for DirectX.  InterSense is a right-hand coordinate
			// system, DirectX is a left-hand coordinate system.
			//
			// InterSense:    DirectX:		Translation:  Rotation:
			//        ^         y^  ^ z     dx = -ix      
			//   X   / y         | /        dy = -iz
			//  <---/            |---->     dz =  iy
			//      |               x
			//    z v

			mat = 
				Matrix.CreateRotationX(MathHelper.ToRadians(-state.Orientation[2])) *
                Matrix.CreateRotationX(MathHelper.ToRadians(-state.Orientation[1])) *
                Matrix.CreateRotationY(MathHelper.ToRadians(-90 + state.Orientation[0])) *
				Matrix.CreateTranslation(-state.Position[0], -state.Position[2], state.Position[1]) *
				matTRANSFORM_TRACKER2EYE;

			//Trace.Write(SceneGraph.Matrix2String(mat, "mat"));
        }

        #endregion
    }
}
