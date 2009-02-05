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

using GoblinXNA.UI.Events;
using GoblinXNA.Device.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// The top-level class for any Goblin XNA 2D GUI.
    /// </summary>
    /// <remarks>
    /// Any GoblinXNA 2D GUI class should extend this class.
    /// </remarks>
    public class G2DComponent : Component
    {
        #region Member Fields
        /// <summary>
        /// Original background bounds of this component
        /// </summary>
        protected Rectangle bounds;
        /// <summary>
        /// Drawing background bounds of this component. 
        /// NOTE: This is different from 'bounds' in case this component has a parent
        /// </summary>
        protected Rectangle paintBounds;

        protected float textWidth;

        protected float textHeight;

        protected SpriteFont textFont;

        #region Event Listeners Fields
        /// <summary>
        /// A list of key listeners
        /// </summary>
        protected List<KeyListener> keyListeners;
        /// <summary>
        /// A list of mouse listeners
        /// </summary>
        protected List<MouseListener> mouseListeners;
        /// <summary>
        /// A list of mouse motion listeners
        /// </summary>
        protected List<MouseMotionListener> mouseMotionListeners;
        /// <summary>
        /// A list of mouse wheel listeners
        /// </summary>
        protected List<MouseWheelListener> mouseWheelListeners;

        /// <summary>
        /// Indicator of whether a key input control is already associated with 
        /// this component
        /// </summary>
        protected bool keyInputRegistered;
        /// <summary>
        /// Indicator of whether a mouse input control is already associated with 
        /// this component
        /// </summary>
        protected bool mouseInputRegistered;

        private HandleKeyType keyType;
        private HandleKeyPress keyPress;
        private HandleKeyRelease keyRelease;

        private HandleMouseClick mouseClick;
        private HandleMousePress mousePress;
        private HandleMouseRelease mouseRelease;
        private HandleMouseMove mouseMove;
        private HandleMouseDrag mouseDrag;
        private HandleMouseWheelMove mouseWheelMove;
        #endregion
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a 2D GUI component with the specified rectangular bounds, background color,
        /// and transparency value.
        /// </summary>
        /// <param name="bounds">Rectangular background bounds of this component</param>
        /// <param name="bgColor">Background color of this component</param>
        /// <param name="alpha">Transparency value of this component [0.0f - 1.0f]. 1.0f
        /// meaning totally opague, and 0.0f meaning totally transparent</param>
        public G2DComponent(Rectangle bounds, Color bgColor, float alpha) :
            base(bgColor, alpha)
        {
            this.bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            paintBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            keyListeners = new List<KeyListener>();
            mouseListeners = new List<MouseListener>();
            mouseMotionListeners = new List<MouseMotionListener>();
            mouseWheelListeners = new List<MouseWheelListener>();

            keyInputRegistered = false;
            mouseInputRegistered = false;
        }

        /// <summary>
        /// Creates a 2D GUI component with the specified rectangular bounds and background color, and
        /// transparency of 1.0f.
        /// </summary>
        /// <param name="bounds">Rectangular background bounds of this component</param>
        /// <param name="bgColor">Background color of this component</param>
        public G2DComponent(Rectangle bounds, Color bgColor) :
            this(bounds, bgColor, DEFAULT_ALPHA) { }

        /// <summary>
        /// Creates a 2D GUI component with the specified rectangular bounds, light gray background
        /// color, and transparency of 1.0f.
        /// </summary>
        /// <param name="bounds">Rectangular background bounds of this component</param>
        public G2DComponent(Rectangle bounds) :
            this(bounds, DEFAULT_COLOR, DEFAULT_ALPHA){ }

        /// <summary>
        /// Creates a 2D GUI component with 1x1 bounds at position (0, 0), light gray background
        /// color, and transparency of 1.0f.
        /// </summary>
        public G2DComponent() :
            this(new Rectangle(0, 0, 1, 1), DEFAULT_COLOR, DEFAULT_ALPHA) { }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the sprite font used to display any text associated with this component.
        /// </summary>
        public virtual SpriteFont TextFont
        {
            get { return textFont; }
            set
            {
                textFont = value;
                Text = label;
            }
        }

        /// <summary>
        /// Indicates whether a key input is already associated with this component
        /// </summary>
        internal bool KeyInputRegistered
        {
            get { return keyInputRegistered; }
        }
        /// <summary>
        /// Indicates whether a mouse input is already associated with this component
        /// </summary>
        internal bool MouseInputRegistered
        {
            get { return mouseInputRegistered; }
        }
        /// <summary>
        /// Gets or sets the background bounds of this component.
        /// </summary>
        public virtual Rectangle Bounds
        {
            get { return bounds; }
            set
            {
                if (value != null)
                {
                    bounds = new Rectangle(value.X, value.Y, value.Width, value.Height);

                    if (parent != null)
                    {
                        paintBounds = new Rectangle(bounds.X + ((G2DComponent)parent).paintBounds.X,
                            bounds.Y + ((G2DComponent)parent).paintBounds.Y, bounds.Width,
                            bounds.Height);
                    }
                    else
                    {
                        paintBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    }
                }
            }
        }
        /// <summary>
        /// Gets a list of mouse listeners added to this component.
        /// </summary>
        public virtual List<MouseListener> MouseListeners
        {
            get { return mouseListeners; }
        }
        /// <summary>
        /// Gets a list of mouse motion listeners added to this component.
        /// </summary>
        public virtual List<MouseMotionListener> MouseMotionListener
        {
            get { return mouseMotionListeners; }
        }
        /// <summary>
        /// Gets a list of mouse wheel listeners added to this component
        /// </summary>
        public virtual List<MouseWheelListener> MouseWheelListener
        {
            get { return mouseWheelListeners; }
        }
        /// <summary>
        /// Gets a list of key listeners added to this component
        /// </summary>
        public virtual List<KeyListener> KeyListeners
        {
            get { return keyListeners; }
        }
        #endregion

        #region Override Properties
        /// <summary>
        /// Gets or sets the parent of this component
        /// </summary>
        /// <exception cref="GoblinException">
        /// Throws GoblinException if non-G2DComponent is assigned
        /// </exception>
        public override Component Parent
        {
            get { return parent; }
            set
            {
                G2DComponent g2dParent = null;

                try
                {
                    if (value != null)
                        g2dParent = (G2DComponent)value;
                }
                catch (Exception)
                {
                    throw new GoblinException("Can not assign non-G2DComponent as a parent of a G2DComponent");
                }

                base.Parent = value;

                if (g2dParent == null)
                    paintBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width,
                        bounds.Height);
                else
                    paintBounds = new Rectangle(bounds.X + g2dParent.paintBounds.X,
                        bounds.Y + g2dParent.paintBounds.Y, bounds.Width,
                        bounds.Height);
            }
        }

        public override string Text
        {
            get
            {
                return base.Text;
            }
            set
            {
                base.Text = value;
                if (textFont != null)
                {
                    textWidth = textFont.MeasureString(value).X;
                    textHeight = textFont.MeasureString(value).Y;
                }
            }
        }
        #endregion

        #region Event Listener Methods
        /// <summary>
        /// Adds a listener for mouse events (mouseClicked, mouseDoubleClicked,
        /// mousePressed, mouseReleased, mouseEntered, mouseExited)
        /// </summary>
        /// <param name="listener">A mouse listener</param>
        public virtual void AddMouseListener(MouseListener listener)
        {
            if (listener != null && !mouseListeners.Contains(listener))
                mouseListeners.Add(listener);
        }
        /// <summary>
        /// Adds a listener for mouse motion events (mouseMoved, mouseDragged)
        /// </summary>
        /// <param name="listener">A mouse motion listener</param>
        public virtual void AddMouseMotionListener(MouseMotionListener listener)
        {
            if (listener != null && !mouseMotionListeners.Contains(listener))
                mouseMotionListeners.Add(listener);
        }
        /// <summary>
        /// Adds a listener for mouse wheel events (mouseWheelMoved)
        /// </summary>
        /// <param name="listener">A mouse wheel listener</param>
        public virtual void AddMouseWheelListener(MouseWheelListener listener)
        {
            if (listener != null && !mouseWheelListeners.Contains(listener))
                mouseWheelListeners.Add(listener);
        }
        /// <summary>
        /// Adds a listener for key events (keyPressed, keyReleased, keyTyped)
        /// </summary>
        /// <param name="listener">A key listener</param>
        public virtual void AddKeyListener(KeyListener listener)
        {
            if (listener != null && !keyListeners.Contains(listener))
                keyListeners.Add(listener);
        }

        /// <summary>
        /// Removes a specific mouse listener if already added
        /// </summary>
        /// <param name="listener">A mouse listener</param>
        public virtual void RemoveMouseListener(MouseListener listener)
        {
            mouseListeners.Remove(listener);
        }
        /// <summary>
        /// Removes a specific mouse motion listener if already added
        /// </summary>
        /// <param name="listener">A mouse motion listener</param>
        public virtual void RemoveMouseMotionListener(MouseMotionListener listener)
        {
            mouseMotionListeners.Remove(listener);
        }
        /// <summary>
        /// Removes a specific mouse wheel listener if already added
        /// </summary>
        /// <param name="listener">A mouse wheel listener</param>
        public virtual void RemoveMouseWheelListener(MouseWheelListener listener)
        {
            mouseWheelListeners.Remove(listener);
        }
        /// <summary>
        /// Removes a specific key listener if already added
        /// </summary>
        /// <param name="listener">A key listener</param>
        public virtual void RemoveKeyListener(KeyListener listener)
        {
            keyListeners.Remove(listener);
        }
        #endregion

        #region Paint Methods
        /// <summary>
        /// Implements how the component should be painted. 
        /// </summary>
        /// <remarks>
        /// This base class method paints only the background
        /// </remarks>
        protected virtual void PaintComponent() {

            if (!drawBackground)
                return;

            // Draw normal background if no image is set
            if (backTexture == null)
            {
                Color color = (enabled) ? backgroundColor : disabledColor;
                // Draw the background
                UI2DRenderer.FillRectangle(paintBounds, State.BlankTexture, color);
            }
            else
            {
                // Draw the background
                UI2DRenderer.FillRectangle(paintBounds, backTexture, textureColor);
            }
        }
        /// <summary>
        /// Implements how the border of the component should be painted. 
        /// </summary>
        /// <remarks>
        /// This base class method paints only the outer-most border
        /// </remarks>
        protected virtual void PaintBorder() 
        {
            UI2DRenderer.DrawRectangle(paintBounds, borderColor, 1);
        }
        /// <summary>
        /// Implements how this component should be rendered
        /// </summary>
        public virtual void RenderWidget() 
        {
            if (!visible)
                return;

            PaintComponent();

            if(drawBorder)
                PaintBorder();
        }
        #endregion

        #region Register Key Input
        internal void RegisterKeyInput()
        {
            if (keyInputRegistered)
                return;

            keyType = new HandleKeyType(HandleKeyType);
            keyPress = new HandleKeyPress(HandleKeyPress);
            keyRelease = new HandleKeyRelease(HandleKeyRelease);

            KeyboardInput.KeyTypeEvent += keyType;
            KeyboardInput.KeyPressEvent += keyPress;
            KeyboardInput.KeyReleaseEvent += keyRelease;

            keyInputRegistered = true;
        }

        internal void RemoveKeyInput()
        {
            if (!keyInputRegistered)
                return;

            KeyboardInput.KeyTypeEvent -= keyType;
            KeyboardInput.KeyPressEvent -= keyPress;
            KeyboardInput.KeyReleaseEvent -= keyRelease;

            keyInputRegistered = false;
        }
        #endregion

        #region Key Input
        /// <summary>
        /// Implements how a key typed event should be handled. 
        /// </summary>
        /// <param name="key">The key typed</param>
        /// <param name="modifier">A struct that indicates whether any of the modifier keys 
        /// (e.g., Shift, Alt, or Ctrl) are pressed</param>
        protected virtual void HandleKeyType(Keys key, KeyModifier modifier)
        {
            if (!focused || !enabled || !visible)
                return;

            foreach (KeyListener listener in keyListeners)
                listener.KeyTyped(key, modifier);
        }
        /// <summary>
        /// Implements how a key press event should be handled. 
        /// </summary>
        /// <param name="key">The key pressed</param>
        /// <param name="modifier">A struct that indicates whether any of the modifier keys 
        /// (e.g., Shift, Alt, or Ctrl) are pressed</param>
        protected virtual void HandleKeyPress(Keys key, KeyModifier modifier)
        {
            if (!focused || !enabled || !visible)
                return;

            foreach (KeyListener listener in keyListeners)
                listener.KeyPressed(key, modifier);

            keyDown = true;
        }
        /// <summary>
        /// Implements how a key release event should be handled. 
        /// </summary>
        /// <param name="key">The key released</param>
        /// <param name="modifier">A struct that indicates whether any of the modifier keys 
        /// (e.g., Shift, Alt, or Ctrl) are pressed</param>
        protected virtual void HandleKeyRelease(Keys key, KeyModifier modifier)
        {
            if (!focused || !enabled || !visible)
                return;

            foreach (KeyListener listener in keyListeners)
                listener.KeyReleased(key, modifier);

            keyDown = false;
        }
        #endregion

        #region Mouse Input Registration
        /// <summary>
        /// Registers mouse input events from an existing Control. 
        /// NOTE: Does not allow registering mouse inputs from more than 2 sources
        /// </summary>
        internal void RegisterMouseInput()
        {
            if (mouseInputRegistered)
                return;

            mouseClick = new HandleMouseClick(HandleMouseClick);
            mousePress = new HandleMousePress(HandleMousePress);
            mouseRelease = new HandleMouseRelease(HandleMouseRelease);
            mouseMove = new HandleMouseMove(HandleMouseMove);
            mouseDrag = new HandleMouseDrag(HandleMouseDrag);
            mouseWheelMove = new HandleMouseWheelMove(HandleMouseWheel);

            MouseInput.MouseClickEvent += mouseClick;
            MouseInput.MousePressEvent += mousePress;
            MouseInput.MouseReleaseEvent += mouseRelease;
            MouseInput.MouseMoveEvent += mouseMove;
            MouseInput.MouseDragEvent += mouseDrag;
            MouseInput.MouseWheelMoveEvent += mouseWheelMove;

            mouseInputRegistered = true;
        }
        /// <summary>
        /// Removes the existing mouse input
        /// </summary>
        internal void RemoveMouseInput()
        {
            if (!mouseInputRegistered)
                return;

            MouseInput.MouseClickEvent -= mouseClick;
            MouseInput.MousePressEvent -= mousePress;
            MouseInput.MouseReleaseEvent -= mouseRelease;
            MouseInput.MouseMoveEvent -= mouseMove;
            MouseInput.MouseDragEvent -= mouseDrag;
            MouseInput.MouseWheelMoveEvent -= mouseWheelMove;

            mouseInputRegistered = false;
        }
        /// <summary>
        /// Implements how a mouse click event should be handled 
        /// </summary>
        /// <param name="button">MouseInput.LeftButton, MouseInput.MiddleButton, 
        /// or MouseInput.RightButton</param>
        /// <param name="mouseLocation">The location in screen coordinates where 
        /// the mouse is clicked</param>
        protected virtual void HandleMouseClick(int button, Point mouseLocation)
        {
            if (!within || !enabled || !visible)
                return;

            foreach (MouseListener listener in mouseListeners)
                listener.MouseClicked(button, mouseLocation);
        }

        /// <summary>
        /// Implements how a mouse press event should be handled. 
        /// </summary>
        /// <param name="button">MouseInput.LeftButton, MouseInput.MiddleButton, 
        /// or MouseInput.RightButton</param>
        /// <param name="mouseLocation">The location in screen coordinates where 
        /// the mouse is pressed</param>
        protected virtual void HandleMousePress(int button, Point mouseLocation)
        {
            if (!within || !enabled || !visible)
                return;

            foreach (MouseListener listener in mouseListeners)
                listener.MousePressed(button, mouseLocation);

            mouseDown = true;
        }
        /// <summary>
        /// Implements how a mouse release event should be handled. 
        /// </summary>
        /// <param name="button">MouseInput.LeftButton, MouseInput.MiddleButton, 
        /// or MouseInput.RightButton</param>
        /// <param name="mouseLocation">The location in screen coordinates where 
        /// the mouse is released</param>
        protected virtual void HandleMouseRelease(int button, Point mouseLocation)
        {
            if (!within || !enabled || !visible)
                return;

            foreach (MouseListener listener in mouseListeners)
                listener.MouseReleased(button, mouseLocation);

            mouseDown = false;
        }

        /// <summary>
        /// Implements how a mouse drag event should be handled.
        /// </summary>
        /// <param name="button">MouseInput.LeftButton, MouseInput.MiddleButton, 
        /// or MouseInput.RightButton</param>
        /// <param name="startLocation">The start location of the mouse drag in 
        /// screen coordinates</param>
        /// <param name="currentLocation">The current location of the mouse drag 
        /// in screen coordinates</param>
        protected virtual void HandleMouseDrag(int button, Point startLocation,
            Point currentLocation)
        {
            within = TestWithin(currentLocation);

            if (!within || !enabled || !visible)
                return;

            foreach (MouseMotionListener listener in mouseMotionListeners)
                listener.MouseDragged(button, startLocation, currentLocation);
        }

        /// <summary>
        /// Implements how a mouse move event should be handled.
        /// </summary>
        /// <param name="mouseLocation">The current location of the mouse in 
        /// screen coordinates</param>
        protected virtual void HandleMouseMove(Point mouseLocation)
        {
            within = TestWithin(mouseLocation);

            if (!within || !enabled || !visible)
            {
                mouseDown = false;
                return;
            }

            foreach (MouseMotionListener listener in mouseMotionListeners)
                listener.MouseMoved(mouseLocation);

            // FIXME: need fix for enter and exit event for boundary cases!!
            if (UI2DHelper.OnEdge(mouseLocation, paintBounds))
            {
                if (!entered) // Enter event
                {
                    foreach (MouseListener listener in mouseListeners)
                        listener.MouseEntered();
                    entered = true;
                }
                else // Exit event
                {
                    foreach (MouseListener listener in mouseListeners)
                        listener.MouseExited();
                    entered = false;
                }
            }
        }
        /// <summary>
        /// Implements how a mouse drag event should be handled. 
        /// </summary>
        /// <param name="delta">The difference of current mouse scroll wheel value from previous
        /// mouse scroll wheel value</param>
        /// <param name="value">The cumulative mouse scroll wheel value since the game/application
        /// was started</param>
        protected virtual void HandleMouseWheel(int delta, int value)
        {
            if (!within || !enabled || !visible)
                return;

            foreach (MouseWheelListener listener in mouseWheelListeners)
                listener.MouseWheelMoved(delta, value);
        }

        #endregion

        #region Inner Class Methods
        /// <summary>
        /// Tests whether the mouse is within the bounds
        /// </summary>
        /// <returns></returns>
        protected virtual bool TestWithin(Point location)
        {
            return UI2DHelper.IsWithin(location, paintBounds);
        }
        #endregion

        #region Override Methods
        /// <summary>
        /// Gets the name of this component
        /// </summary>
        /// <returns>Name of this component</returns>
        public override string ToString()
        {
            return name;
        }
        #endregion
    }
}
