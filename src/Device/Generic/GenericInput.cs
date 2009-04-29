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

        private String identifier;
        private bool isAvailable;
        private Vector3 translation;
        private Quaternion rotation;

        private Keys forwardKey;
        private Keys backwardKey;
        private Keys leftKey;
        private Keys rightKey;
        private Keys upKey;
        private Keys downKey;

        private bool forwardPressed;
        private bool backwardPressed;
        private bool leftPressed;
        private bool rightPressed;
        private bool upPressed;
        private bool downPressed;
        private bool moveSmoothly;

        private float sngWalk;
        private float sngStrafe;

        private float moveSpeed;
        private float pitchSpeed;
        private float yawSpeed;
        private int deltaX;
        private int deltaY;

        private bool useGenericInput;

        private Point prevMouseLocation;
        private static GenericInput input;

        #endregion

        #region Static Constructors

        /// <summary>
        /// A private constructor.
        /// </summary>
        /// <remarks>
        /// Don't instantiate this constructor.
        /// </remarks>
        private GenericInput()
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

            MouseInput.Instance.MouseDragEvent += 
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

            MouseInput.Instance.MousePressEvent +=
                delegate(int button, Point mouseLocation)
                {
                    if (button == MouseInput.RightButton)
                    {
                        prevMouseLocation.X = mouseLocation.X;
                        prevMouseLocation.Y = mouseLocation.Y;
                    }
                };

            KeyboardInput.Instance.KeyPressEvent += 
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

            KeyboardInput.Instance.KeyReleaseEvent += 
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

        public Vector3 Translation
        {
            get 
            {
                useGenericInput = true;
                return translation; 
            }
        }

        public Quaternion Rotation
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
                KeyboardInput.Instance.InitialRepetitionWait = 100;
                KeyboardInput.Instance.RepetitionWait = 100;
                useGenericInput = true;
                return Matrix.Transform(Matrix.CreateTranslation(translation),
                    rotation);
            }
        }

        /// <summary>
        /// Gets or sets the key used to move forward.
        /// </summary>
        public Keys ForwardKey
        {
            get { return forwardKey; }
            set { forwardKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move backward.
        /// </summary>
        public Keys BackwardKey
        {
            get { return backwardKey; }
            set { backwardKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move left.
        /// </summary>
        public Keys LeftKey
        {
            get { return leftKey; }
            set { leftKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move right.
        /// </summary>
        public Keys RightKey
        {
            get { return rightKey; }
            set { rightKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move upward.
        /// </summary>
        public Keys UpKey
        {
            get { return upKey; }
            set { upKey = value; }
        }

        /// <summary>
        /// Gets or sets the key used to move downward.
        /// </summary>
        public Keys DownKey
        {
            get { return downKey; }
            set { downKey = value; }
        }

        /// <summary>
        /// Gets or sets whether to move smoothly by introducing slight slidings.
        /// </summary>
        public bool MoveSmoothly
        {
            get { return moveSmoothly; }
            set { moveSmoothly = value; }
        }

        /// <summary>
        /// Gets or sets the move speed (how far it moves for each key type).
        /// </summary>
        public float MoveSpeed
        {
            get { return moveSpeed; }
            set { moveSpeed = value; }
        }

        /// <summary>
        /// Gets or sets how fast it pitches.
        /// </summary>
        public float PitchSpeed
        {
            get { return pitchSpeed; }
            set { pitchSpeed = value; }
        }

        /// <summary>
        /// Gets or sets how fast it yaws.
        /// </summary>
        public float YawSpeed
        {
            get { return yawSpeed; }
            set { yawSpeed = value; }
        }

        /// <summary>
        /// Gets the instantiation of GenericInput class.
        /// </summary>
        public static GenericInput Instance
        {
            get
            {
                if (input == null)
                {
                    input = new GenericInput();
                }

                return input;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets the translation and rotation.
        /// </summary>
        public void Reset()
        {
            translation = Vector3.Zero;
            rotation = Quaternion.Identity;
        }

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

        public void Dispose()
        {
        }

        #endregion
    }
}
