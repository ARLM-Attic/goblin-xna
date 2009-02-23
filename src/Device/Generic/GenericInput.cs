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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GoblinXNA.Device.Generic
{
    /// <summary>
    /// An implementation of 6DOF input device using a combination of mouse and keyboard inputs.
    /// Good for navigation of 3D space for debugging, but maybe not for an actual game or application.
    /// </summary>
    public class GenericInput : InputDevice_6DOF
    {
        #region Member Fields

        private static String identifier;
        private static bool isAvailable;
        private static Vector3 translation;
        private static Quaternion rotation;

        private static Keys forwardKey;
        private static Keys backwardKey;
        private static Keys leftKey;
        private static Keys rightKey;
        private static Keys upKey;
        private static Keys downKey;

        private static bool forwardPressed;
        private static bool backwardPressed;
        private static bool leftPressed;
        private static bool rightPressed;
        private static bool upPressed;
        private static bool downPressed;
        private static bool moveSmoothly;

        private static float sngWalk;
        private static float sngStrafe;

        private static float moveSpeed;
        private static float pitchSpeed;
        private static float yawSpeed;
        private static int deltaX;
        private static int deltaY;

        private static bool useGenericInput;

        private static Point prevMouseLocation;

        #endregion

        #region Static Constructors

        /// <summary>
        /// A static constructor.
        /// </summary>
        /// <remarks>
        /// Don't instantiate this constructor.
        /// </remarks>
        static GenericInput()
        {
            forwardKey = Keys.W;
            backwardKey = Keys.S;
            leftKey = Keys.A;
            rightKey = Keys.D;
            upKey = Keys.Z;
            downKey = Keys.X;

            forwardPressed = false;
            backwardPressed = false;
            leftPressed = false;
            rightPressed = false;
            upPressed = false;
            downPressed = false;

            sngWalk = 0;
            sngStrafe = 0;

            moveSmoothly = true;
            moveSpeed = 1;
            pitchSpeed = 1;
            yawSpeed = 1;

            translation = Vector3.Zero;
            rotation = Quaternion.Identity;

            prevMouseLocation = new Point(-1, -1);

            MouseInput.MouseDragEvent += 
                delegate(int button, Point startLocation, Point currentLocation)
                {
                    if (button == MouseInput.RightButton)
                    {
                        if (prevMouseLocation.X == -1)
                        {
                            prevMouseLocation.X = currentLocation.X;
                            prevMouseLocation.Y = currentLocation.Y;
                        }
                        else
                        {
                            deltaX = currentLocation.X - prevMouseLocation.X;
                            deltaY = currentLocation.Y - prevMouseLocation.Y;

                            prevMouseLocation.X = currentLocation.X;
                            prevMouseLocation.Y = currentLocation.Y;
                        }
                    }
                };

            MouseInput.MousePressEvent +=
                delegate(int button, Point mouseLocation)
                {
                    if (button == MouseInput.RightButton)
                    {
                        prevMouseLocation.X = mouseLocation.X;
                        prevMouseLocation.Y = mouseLocation.Y;
                    }
                };

            KeyboardInput.KeyPressEvent += 
                delegate(Keys key, KeyModifier modifier)
                {
                    if (key == forwardKey)
                        forwardPressed = true;
                    else if (key == backwardKey)
                        backwardPressed = true;
                    else if (key == leftKey)
                        leftPressed = true;
                    else if (key == rightKey)
                        rightPressed = true;
                    else if (key == upKey)
                        upPressed = true;
                    else if (key == downKey)
                        downPressed = true;
                };

            KeyboardInput.KeyReleaseEvent += 
                delegate(Keys key, KeyModifier modifier)
                {
                    if (key == forwardKey)
                        forwardPressed = false;
                    else if (key == backwardKey)
                        backwardPressed = false;
                    else if (key == leftKey)
                        leftPressed = false;
                    else if (key == rightKey)
                        rightPressed = false;
                    else if (key == upKey)
                        upPressed = false;
                    else if (key == downKey)
                        downPressed = false;
                };

            useGenericInput = false;
            isAvailable = true;
        }

        #endregion

        #region Public Properties

        public String Identifier
        {
            get { return identifier; }
            set { identifier = value; }
        }

        public bool IsAvailable
        {
            get { return isAvailable; }
        }

        public static Vector3 Translation
        {
            get 
            {
                useGenericInput = true;
                return translation; 
            }
        }

        public static Quaternion Rotation
        {
            get 
            {
                useGenericInput = true;
                return rotation; 
            }
        }

        public Matrix WorldTransformation
        {
            get
            {   
                KeyboardInput.InitialRepetitionWait = 100;
                KeyboardInput.RepetitionWait = 100;
                useGenericInput = true;
                return Matrix.Transform(Matrix.CreateTranslation(translation),
                    rotation);
            }
        }

        /// <summary>
        /// Gets or sets the key used to move forward.
        /// </summary>
        public static Keys ForwardKey
        {
            get { return forwardKey; }
            set { forwardKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move backward.
        /// </summary>
        public static Keys BackwardKey
        {
            get { return backwardKey; }
            set { backwardKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move left.
        /// </summary>
        public static Keys LeftKey
        {
            get { return leftKey; }
            set { leftKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move right.
        /// </summary>
        public static Keys RightKey
        {
            get { return rightKey; }
            set { rightKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move upward.
        /// </summary>
        public static Keys UpKey
        {
            get { return upKey; }
            set { upKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move downward.
        /// </summary>
        public static Keys DownKey
        {
            get { return downKey; }
            set { downKey = value; }
        }

        /// <summary>
        /// Gets or sets whether to move smoothly by introducing slight slidings.
        /// </summary>
        public static bool MoveSmoothly
        {
            get { return moveSmoothly; }
            set { moveSmoothly = value; }
        }

        /// <summary>
        /// Gets or sets the move speed (how far it moves for each key type).
        /// </summary>
        public static float MoveSpeed
        {
            get { return moveSpeed; }
            set { moveSpeed = value; }
        }

        /// <summary>
        /// Gets or sets how fast it pitches.
        /// </summary>
        public static float PitchSpeed
        {
            get { return pitchSpeed; }
            set { pitchSpeed = value; }
        }

        /// <summary>
        /// Gets or sets how fast it yaws.
        /// </summary>
        public static float YawSpeed
        {
            get { return yawSpeed; }
            set { yawSpeed = value; }
        }

        #endregion

        #region Update

        public void Update(GameTime gameTime, bool deviceActive)
        {
            if (!useGenericInput)
                return;

            if (!(deltaX == 0 && deltaY == 0))
            {
                Quaternion change = Quaternion.CreateFromYawPitchRoll
                    ((float)(deltaX * yawSpeed * Math.PI / 360),
                    (float)(deltaY * pitchSpeed * Math.PI / 360), 0);
                rotation = Quaternion.Multiply(rotation, change);
                deltaX = deltaY = 0;
            }

            if (forwardPressed)
                sngWalk = -moveSpeed;
            else if (backwardPressed)
                sngWalk = moveSpeed;

            if (leftPressed)
                sngStrafe = -moveSpeed;
            else if (rightPressed)
                sngStrafe = moveSpeed;

            if (upPressed)
                translation.Y += moveSpeed;
            else if (downPressed)
                translation.Y -= moveSpeed;

            if (moveSmoothly)
            {
                if (sngWalk > 0)
                {
                    sngWalk = sngWalk - 0.005f * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (sngWalk < 0)
                        sngWalk = 0;
                }
                else
                {
                    sngWalk = sngWalk + 0.005f * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (sngWalk > 0)
                        sngWalk = 0;
                }

                // Now, we update the left and right (strafe) movement.
                if (sngStrafe > 0)
                {
                    sngStrafe = sngStrafe - 0.005f * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (sngStrafe < 0)
                        sngStrafe = 0;
                }
                else
                {
                    sngStrafe = sngStrafe + 0.005f * (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                    if (sngStrafe > 0)
                        sngStrafe = 0;
                }

                translation.Z += sngWalk;
                translation.X += sngStrafe;
            }
            else
            {
                translation.Z += sngWalk;
                translation.X += sngStrafe;

                sngWalk = sngStrafe = 0;
            }
        }

        #endregion
    }
}
