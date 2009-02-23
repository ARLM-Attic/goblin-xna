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
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.UI.Events;


namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// NOT FINISHED YET.
    /// </summary>
    internal class G2DSpinner : G2DComponent
    {
        #region Member Fields
        protected int value;
        protected List<ChangeListener> changeListeners;

        #region Internal handling
        /// <summary>
        /// When the mouse is held down on either the up or down arrow, it should increment or
        /// decrement the value. But before taking the action, wait for some press intervals
        /// for avoiding unintended multiple increments or decrements
        /// </summary>
        protected int initialPressInterval;
        protected int pressInterval;
        protected int pressCount;
        protected bool initialPress;
        protected bool upPressed;
        protected bool downPressed;
        protected bool heldDown;
        #endregion

        #region Only for drawing purpose
        protected G2DTextField textField;
        protected Rectangle upArrowBound;
        protected Rectangle downArrowBound;
        protected Color highlightColor;
        protected Color arrowColor;
        protected Color buttonColor;
        #endregion
        #endregion

        #region Constructors
        public G2DSpinner()
            : base()
        {
            value = 0;
            changeListeners = new List<ChangeListener>();
            initialPressInterval = 20;
            pressInterval = 5;
            pressCount = 0;
            initialPress = true;
            textField = new G2DTextField(""+value);
            textField.HorizontalAlignment = GoblinEnums.HorizontalAlignment.Right;
            textField.Editable = false;

            upArrowBound = new Rectangle();
            downArrowBound = new Rectangle();

            upPressed = downPressed = false;
            heldDown = false;

            buttonColor = Color.Turquoise;
            highlightColor = new Color((byte)0x99, (byte)255, (byte)255, (byte)255);
            arrowColor = Color.Navy;

            name = "G2DSpinner";
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the value of the spinner
        /// </summary>
        public virtual int Value
        {
            get{return value;}
            set
            {
                this.value = value;
                textField.Text = "" + value;

                ChangeEvent evt = new ChangeEvent(this);
                foreach (ChangeListener listener in changeListeners)
                    listener.StateChanged(evt);
            }
        }

        /// <summary>
        /// Gets the next value of the spinner
        /// </summary>
        public virtual int NextValue
        {
            get { return value + 1; }
        }

        /// <summary>
        /// Gets the previous value of the spinner
        /// </summary>
        public virtual int PreviousValue
        {
            get { return value - 1; }
        }

        public virtual List<ChangeListener> ChangeListeners
        {
            get { return changeListeners; }
        }
        #endregion

        #region Override Properties
        public override Rectangle Bounds
        {
            get
            {
                return base.Bounds;
            }
            set
            {
                base.Bounds = value;

                textField.Bounds = new Rectangle(paintBounds.X, paintBounds.Y, 
                    paintBounds.Width - 14, paintBounds.Height);
                upArrowBound = new Rectangle(paintBounds.X + paintBounds.Width - 14, paintBounds.Y,
                    14, paintBounds.Height / 2);
                downArrowBound = new Rectangle(paintBounds.X + paintBounds.Width - 14,
                    paintBounds.Y + paintBounds.Height / 2, 14, paintBounds.Height / 2);
            }
        }

        public override Component Parent
        {
            get
            {
                return base.Parent;
            }
            set
            {
                base.Parent = value;

                textField.Bounds = new Rectangle(paintBounds.X, paintBounds.Y, 
                    paintBounds.Width - 14, paintBounds.Height);
                upArrowBound = new Rectangle(paintBounds.X + paintBounds.Width - 14, paintBounds.Y,
                    14, paintBounds.Height / 2);
                downArrowBound = new Rectangle(paintBounds.X + paintBounds.Width - 14,
                    paintBounds.Y + paintBounds.Height / 2, 14, paintBounds.Height / 2);
            }
        }

        public override bool Enabled
        {
            get
            {
                return base.Enabled;
            }
            set
            {
                base.Enabled = value;

                textField.Enabled = value;
            }
        }

        public override bool Focused
        {
            get
            {
                return base.Focused;
            }
            internal set
            {
                base.Focused = value;
                textField.Focused = value;
            }
        }

        public override GoblinEnums.HorizontalAlignment HorizontalAlignment
        {
            get
            {
                return base.HorizontalAlignment;
            }
            set
            {
                base.HorizontalAlignment = value;

                textField.HorizontalAlignment = value;
            }
        }

        public override GoblinEnums.VerticalAlignment VerticalAlignment
        {
            get
            {
                return base.VerticalAlignment;
            }
            set
            {
                base.VerticalAlignment = value;

                textField.VerticalAlignment = value;
            }
        }

        public override float Transparency
        {
            get
            {
                return base.Transparency;
            }
            set
            {
                base.Transparency = value;

                textField.Transparency = alpha;

                highlightColor = new Color(highlightColor.R, highlightColor.G, highlightColor.B, alpha);
                arrowColor = new Color(arrowColor.R, arrowColor.G, arrowColor.B, alpha);
                buttonColor = new Color(buttonColor.R, buttonColor.G, buttonColor.B, alpha);
            }
        }
        #endregion

        #region Event Listener Methods
        public virtual void AddChangeListener(ChangeListener listener)
        {
            if (listener != null && !changeListeners.Contains(listener))
                changeListeners.Add(listener);
        }

        public virtual void RemoveChangeListener(ChangeListener listener)
        {
            changeListeners.Remove(listener);
        }
        #endregion

        #region Action Methods
        /// <summary>
        /// Programmatically perform 'up arrow' click
        /// </summary>
        public virtual void DoNextClick()
        {
            Value = NextValue;
        }
        /// <summary>
        /// Programmatically perform 'down arrow' click
        /// </summary>
        public virtual void DoPreviousClick()
        {
            Value = PreviousValue;
        }
        #endregion

        #region Inner Class Methods
        protected virtual void CheckMouseHeldDown()
        {
            if (heldDown)
            {
                if (initialPress)
                {
                    if (pressCount > initialPressInterval)
                    {
                        pressCount = 0;
                        if (upPressed)
                            DoNextClick();
                        else if (downPressed)
                            DoPreviousClick();
                        initialPress = false;
                    }
                }
                else
                {
                    if (pressCount > pressInterval)
                    {
                        pressCount = 0;
                        if (upPressed)
                            DoNextClick();
                        else if (downPressed)
                            DoPreviousClick();
                    }
                }

                pressCount++;
            }
        }

        protected virtual void CheckArrowPress(Point location)
        {
            if (UI2DHelper.IsWithin(location, upArrowBound)){
                DoNextClick();
                upPressed = true;
                heldDown = true;
            }
            else if (UI2DHelper.IsWithin(location, downArrowBound)){
                DoPreviousClick();
                downPressed = true;
                heldDown = true;
            }
        }
        #endregion

        #region Override Methods
        protected override void HandleMousePress(int button, Point mouseLocation)
        {
            base.HandleMousePress(button, mouseLocation);

            if (!within || !enabled || !visible)
                CheckArrowPress(mouseLocation);
        }

        protected override void HandleMouseRelease(int button, Point mouseLocatione)
        {
            base.HandleMouseRelease(button, mouseLocatione);

            pressCount = 0;
            initialPress = true;
            upPressed = downPressed = false;
            heldDown = false;
        }

        protected override void HandleMouseWheel(int delta, int value)
        {
            base.HandleMouseWheel(delta, value);

            bool up = (delta > 0);
            if (up)
                for (int i = 0; i < delta; i++)
                    DoNextClick();
            else
                for (int i = 0; i < -(delta); i++)
                    DoPreviousClick();
        }

        protected override void PaintComponent()
        {
            CheckMouseHeldDown();

            textField.RenderWidget();

            /*// Render the border of the up and down button
            Util.DrawThickBox(Scr2D, upArrowBound, borderColor, 1);
            Util.DrawThickBox(Scr2D, downArrowBound, borderColor, 1);

            // Render the up and down buttons
            int c = (enabled) ? buttonColor : disabledColor;
            Scr2D.DRAW_FilledBox(upArrowBound.X + 1, upArrowBound.Y + 1, 
                upArrowBound.X + upArrowBound.Width - 0.5f,
                upArrowBound.Y + upArrowBound.Height - 0.5f, c, c, c, c);
            Scr2D.DRAW_FilledBox(downArrowBound.X + 1, downArrowBound.Y + 1,
                downArrowBound.X + downArrowBound.Width - 0.5f,
                downArrowBound.Y + downArrowBound.Height - 0.5f, c, c, c, c);

            // Render the up and down arrows
            // First render the up arrow
            Util.DrawThickLine(Scr2D, upArrowBound.X + 3, upArrowBound.Y + (upArrowBound.Height / 2) + 3,
                upArrowBound.X + (upArrowBound.Width / 2), upArrowBound.Y + (upArrowBound.Height / 2) - 3,
                arrowColor, 2);
            Util.DrawThickLine(Scr2D, upArrowBound.X + upArrowBound.Width - 3, upArrowBound.Y + (upArrowBound.Height / 2) + 3,
                upArrowBound.X + (upArrowBound.Width / 2), upArrowBound.Y + (upArrowBound.Height / 2) - 3,
                arrowColor, 2);
            // Then render the down arrow
            Util.DrawThickLine(Scr2D, downArrowBound.X + 3, downArrowBound.Y + (downArrowBound.Height / 2) - 3,
                downArrowBound.X + (downArrowBound.Width / 2), 
                downArrowBound.Y + (downArrowBound.Height / 2) - 3, arrowColor, 2);
            Util.DrawThickLine(Scr2D, downArrowBound.X + downArrowBound.Width + 3, 
                downArrowBound.Y + (downArrowBound.Height / 2) - 3,
                downArrowBound.X + (downArrowBound.Width / 2), 
                downArrowBound.Y + (downArrowBound.Height / 2) + 3, arrowColor, 2);*/
        }
        #endregion
    }
}
