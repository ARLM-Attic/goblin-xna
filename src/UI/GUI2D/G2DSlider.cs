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
using Microsoft.Xna.Framework.Input;

using GoblinXNA.Graphics;
using GoblinXNA.Device.Generic;
using GoblinXNA.UI.Events;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// A slider lets the user graphically select a value by sliding a knob within a bounded interval. 
    /// The slider can show both major tick marks and minor tick marks between them, as well as labels.
    /// 
    /// In order to display the value labels, G2DSlider.TextFont must be set. 
    /// Otherwise, the labels will not show up.
    /// </summary>
    public class G2DSlider : G2DComponent
    {
        #region Member Fields
        protected GoblinEnums.Orientation orientation;
        protected int value;
        protected int maxValue;
        protected int minValue;
        protected int majorTickSpacing;
        protected int minorTickSpacing;
        protected bool snapToTicks;
        protected bool paintTicks;
        protected bool paintLabels;
        protected bool paintTrack;
        /// <summary>
        /// Indicator of whether the current changes to the value property are part of 
        /// a series of changes
        /// </summary>
        protected bool valueIsAdjusting;

        protected bool knobWithin;

        protected Color trackColor;
        protected Color knobColor;
        protected Color knobBorderColor;
        protected Color trackBorderColor;

        protected List<ChangeListener> changeListeners;

        #region Used Only For Drawing Purpose
        protected Rectangle knobBound;
        protected Rectangle trackBound;
        protected Rectangle tickBound;
        protected Rectangle labelBound;

        protected float knobIncrement;

        protected int minorTickCount;
        protected float minorTickDelta;
        protected int majorTickCount;
        protected float majorTickDelta;

        protected List<String> tickLabels;
        protected List<int> labelShifts;
        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a slider with specified orientation, and minimum, maximum, and initial values.
        /// </summary>
        /// <param name="orientation">The orientation of the slider</param>
        /// <param name="min">The minimum value the slider can take</param>
        /// <param name="max">The maximum value the slider can take</param>
        /// <param name="value">An initial value between the range of min and max values</param>
        public G2DSlider(GoblinEnums.Orientation orientation, int min, int max, int value)
            : base()
        {

            this.orientation = orientation;
            this.minValue = min;
            this.maxValue = max;
            this.value = value;

            drawBackground = false;
            drawBorder = true;
            name = "G2DSlider";

            majorTickSpacing = 0;
            minorTickSpacing = 0;
            snapToTicks = false;
            paintTicks = false;
            paintLabels = false;
            paintTrack = true;
            valueIsAdjusting = false;
            knobWithin = false;

            knobColor = new Color((byte)204, (byte)204, (byte)204, (byte)255);
            knobBorderColor = Color.Black;
            trackBorderColor = Color.DarkGray;
            trackColor = Color.White;

            changeListeners = new List<ChangeListener>();

            knobBound = new Rectangle();
            trackBound = new Rectangle();
            tickBound = new Rectangle();
            labelBound = new Rectangle();

            minorTickCount = 0;
            majorTickCount = 0;
            minorTickDelta = 0;
            majorTickDelta = 0;

            tickLabels = new List<string>();
            labelShifts = new List<int>();

            AdjustKnobIncrement();
        }

        /// <summary>
        /// Creates a horizontal slider with the specified minimum, maximum, and initial values.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="value"></param>
        public G2DSlider(int min, int max, int value)
            : this(GoblinEnums.Orientation.Horizontal, min, max, value) { }

        /// <summary>
        /// Creates a horizontal slider with the specified minimum and maximum values. The initial
        /// value is set to the average value of min and max.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public G2DSlider(int min, int max) 
            : this(min, max, (min + max) / 2) { }

        /// <summary>
        /// Creates a slider in the range between 0 and 10 with the specified orientation and initial value
        /// of 5.
        /// </summary>
        /// <param name="orientation"></param>
        public G2DSlider(GoblinEnums.Orientation orientation) 
            : this(orientation, 0, 10, 5) { }

        /// <summary>
        /// Craetes a horizontal slider in the range between 0 and 10 with initial value of 5.
        /// </summary>
        public G2DSlider() 
            : this(GoblinEnums.Orientation.Horizontal) { }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the slider's current value
        /// </summary>
        public virtual int Value
        {
            get { return value; }
            set
            {
                if (value < minValue || value > maxValue)
                    return;

                this.value = value;

                ChangeEvent evt = new ChangeEvent(this);
                foreach (ChangeListener listener in changeListeners)
                    listener.StateChanged(evt);

                AdjustKnobBounds();
            }
        }

        /// <summary>
        /// Gets or sets the slider's maximum value
        /// </summary>
        public virtual int Maximum
        {
            get { return maxValue; }
            set
            {
                if (value < minValue)
                    return;

                maxValue = value;
                if (Value > maxValue)
                    Value = maxValue;
                AdjustKnobIncrement();
                AdjustTickCount();
                AdjustTickLabels();
            }
        }

        /// <summary>
        /// Gets or sets the slider's minimum value
        /// </summary>
        public virtual int Minimum
        {
            get { return minValue; }
            set
            {
                if (value > maxValue)
                    return;

                minValue = value;
                if (Value < minValue)
                    Value = minValue;
                AdjustKnobIncrement();
                AdjustTickCount();
                AdjustTickLabels();
            }
        }

        /// <summary>
        /// Gets or sets the slider's orientation
        /// </summary>
        public virtual GoblinEnums.Orientation Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;

                AdjustDrawingBounds();
                AdjustKnobIncrement();
                AdjustTickCount();
                AdjustTickLabels();
            }
        }

        /// <summary>
        /// Gets the range of values "covered" by the knob
        /// </summary>
        public virtual int Extent
        {
            get
            {
                if (minorTickSpacing == 0)
                    return 0;
                else
                    return (maxValue - minValue) / minorTickSpacing;
            }
        }

        /// <summary>
        /// Gets or sets the slider's major tick spacing. 
        /// 
        /// For example, for a max value of 10 and min value of 0, major tick spacing of 5 
        /// will draw major ticks at 0, 5, and 10.
        /// </summary>
        public virtual int MajorTickSpacing
        {
            get { return majorTickSpacing; }
            set
            {
                if ((minorTickSpacing != 0) && (value < minorTickSpacing))
                    return;

                majorTickSpacing = value;
                AdjustTickCount();
                AdjustTickLabels();
            }
        }

        /// <summary>
        /// Gets or sets the slider's minor tick spacing. 
        /// 
        /// For example, for a max value of 10 and min value of 0, minor tick spacing of 2 
        /// will draw minor ticks at 0, 2, 4, 6, 8, and 10. 
        /// NOTE: If there is an overlap between major ticks and minor ticks, only major ticks 
        /// will be shown
        /// </summary>
        public virtual int MinorTickSpacing
        {
            get { return minorTickSpacing; }
            set
            {
                minorTickSpacing = value;
                AdjustTickCount();
            }
        }

        /// <summary>
        /// Gets or sets whether to make the knob resolve to the closest tick mark next to where the 
        /// user positioned the knob
        /// </summary>
        public virtual bool SnapToTicks
        {
            get { return snapToTicks; }
            set { snapToTicks = value; }
        }

        /// <summary>
        /// Gets or sets whether to paint the ticks including both major and minor ticks. 
        /// </summary>
        /// <remarks>Major tick spacing has to be defined before the ticks can be painted</remarks>
        public virtual bool PaintTicks
        {
            get { return paintTicks; }
            set
            {
                paintTicks = value;
                AdjustDrawingBounds();
            }
        }

        /// <summary>
        /// Gets or sets whether to paint the labels. 
        /// </summary>
        /// <remarks>
        /// Labels are only shown below major ticks. Also, major tick spacing has to
        /// be defined before the labels can be painted
        /// </remarks>
        public virtual bool PaintLabels
        {
            get { return paintLabels; }
            set
            {
                paintLabels = value;
                AdjustDrawingBounds();
            }
        }

        /// <summary>
        /// Gets or sets whether to paint the track behind the knob
        /// </summary>
        public virtual bool PaintTrack
        {
            get { return paintTrack; }
            set
            {
                paintTrack = value;
                AdjustDrawingBounds();
            }
        }

        /// <summary>
        /// Gets whether the current changes to the value property are part 
        /// of a series of changes
        /// </summary>
        public virtual bool ValueIsAdjusting
        {
            get { return valueIsAdjusting; }
        }

        /// <summary>
        /// Gets a list of added change listeners
        /// </summary>
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
                AdjustDrawingBounds();
                AdjustKnobIncrement();
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

                knobColor = new Color(knobColor.R, knobColor.G, knobColor.B, alpha);
                knobBorderColor = new Color(knobBorderColor.R, knobBorderColor.G, knobBorderColor.B, alpha);
                trackBorderColor = new Color(trackBorderColor.R, trackBorderColor.G, trackBorderColor.B, alpha);
                trackColor = new Color(trackColor.R, trackColor.G, trackColor.B, alpha);
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

                AdjustDrawingBounds();
            }
        }

        public override SpriteFont TextFont
        {
            get
            {
                return base.TextFont;
            }
            set
            {
                base.TextFont = value;
                if(textFont != null)
                    AdjustTickLabels();
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

        #region Inner Class Methods
        protected void AdjustDrawingBounds()
        {
            AdjustKnobBounds();
            AdjustTrackBounds();
            AdjustTickBounds();
            AdjustLabelBounds();
        }

        protected virtual void AdjustKnobBounds()
        {
            if (orientation == GoblinEnums.Orientation.Horizontal)
            {
                knobBound.X = (int)(paintBounds.X + 4 + (value - minValue) * knobIncrement - 5);
                knobBound.Width = 10;
                knobBound.Y = paintBounds.Y + (bounds.Height - 20) / 2;
                knobBound.Height = 20;

                if (paintTicks)
                    knobBound.Y -= 6;

                if (paintLabels)
                    knobBound.Y -= 6;
            }
            else
            {
                knobBound.Y = (int)(paintBounds.Y + 4 + (maxValue - value) * knobIncrement - 5);
                knobBound.Height = 10;
                knobBound.X = paintBounds.X + (bounds.Width - 20) / 2;
                knobBound.Width = 20;

                if (paintTicks)
                    knobBound.X -= 6;

                if (paintLabels)
                    knobBound.X -= 6;
            }
        }

        protected virtual void AdjustTrackBounds()
        {
            if (orientation == GoblinEnums.Orientation.Horizontal)
            {
                trackBound.X = paintBounds.X + 4;
                trackBound.Width = bounds.Width - 8;
                trackBound.Y = paintBounds.Y + (bounds.Height - 4) / 2;
                trackBound.Height = 4;

                if (paintTicks)
                    trackBound.Y -= 6;

                if (paintLabels)
                    trackBound.Y -= 6;
            }
            else
            {
                trackBound.Y = paintBounds.Y + 4;
                trackBound.Height = bounds.Height - 8;
                trackBound.X = paintBounds.X + (bounds.Width - 4) / 2;
                trackBound.Width = 4;

                if (paintTicks)
                    trackBound.X -= 6;

                if (paintLabels)
                    trackBound.X -= 6;
            }
        }

        protected virtual void AdjustTickBounds()
        {
            if (!paintTicks)
                return;

            if (orientation == GoblinEnums.Orientation.Horizontal)
            {
                tickBound.X = trackBound.X;
                tickBound.Width = trackBound.Width;

                tickBound.Y = trackBound.Y + trackBound.Height + 8;
                tickBound.Height = 10;
            }
            else
            {
                tickBound.Y = trackBound.Y;
                tickBound.Height = trackBound.Height;

                tickBound.X = trackBound.X + trackBound.Width + 8;
                tickBound.Width = 10;
            }

            AdjustTickCount();
        }

        protected virtual void AdjustLabelBounds()
        {
            if (!paintLabels)
                return;

            if (orientation == GoblinEnums.Orientation.Horizontal)
            {
                labelBound.X = trackBound.X;
                labelBound.Width = trackBound.Width;
                labelBound.Height = 10;

                if (paintTicks)
                    labelBound.Y = tickBound.Y + tickBound.Height + 2;
                else
                    labelBound.Y = trackBound.Y + trackBound.Height + 8;
            }
            else
            {
                labelBound.Y = trackBound.Y;
                labelBound.Height = trackBound.Height;
                labelBound.Width = 10;

                if (paintTicks)
                    labelBound.X = tickBound.X + tickBound.Width + 2;
                else
                    labelBound.X = trackBound.X + trackBound.Width + 8;
            }

            if (textFont != null)
                AdjustTickLabels();
        }

        protected virtual void AdjustKnobIncrement()
        {
            int pixels = (orientation == GoblinEnums.Orientation.Horizontal) ? 
                (bounds.Width - 8) : (bounds.Height - 8);

            knobIncrement = pixels / (float)(maxValue - minValue);
        }

        protected virtual void AdjustTickCount()
        {
            if (minorTickSpacing != 0)
            {
                minorTickCount = (maxValue - minValue) / minorTickSpacing;
                if (orientation == GoblinEnums.Orientation.Horizontal)
                    minorTickDelta = tickBound.Width / (float)minorTickCount;
                else
                    minorTickDelta = tickBound.Height / (float)minorTickCount;
            }
            else
                minorTickCount = 0;

            if (majorTickSpacing != 0)
            {
                majorTickCount = (maxValue - minValue) / majorTickSpacing;
                if (orientation == GoblinEnums.Orientation.Horizontal)
                    majorTickDelta = tickBound.Width / (float)majorTickCount;
                else
                    majorTickDelta = tickBound.Height / (float)majorTickCount;
            }
            else
                majorTickCount = 0;
        }

        protected virtual void AdjustTickLabels()
        {
            if (textFont != null && majorTickSpacing != 0)
            {
                tickLabels.Clear();

                for (int i = 0; i <= majorTickCount; i++)
                {
                    tickLabels.Add("" + (minValue + majorTickSpacing * i));
                    labelShifts.Add((int)(textFont.MeasureString(tickLabels[i]).X / 2));
                }
            }
        }

        protected virtual bool TestKnobWithin(Point location)
        {
            return UI2DHelper.IsWithin(location, knobBound);
        }
        #endregion

        #region Override Methods

        protected override bool TestWithin(Point location)
        {
            return UI2DHelper.IsWithin(location, new Rectangle(paintBounds.X, paintBounds.Y - 8,
                paintBounds.Width, paintBounds.Height + 13));
        }

        protected override void HandleMousePress(int button, Point mouseLocation)
        {
            base.HandleMousePress(button, mouseLocation);

            if (within && !knobWithin)
            {
                int gap = 1;
                if (snapToTicks)
                {
                    if (minorTickSpacing != 0)
                        gap = minorTickSpacing;
                    else if (majorTickSpacing != 0)
                        gap = majorTickSpacing;
                }

                int adjust = value % gap;

                if (orientation == GoblinEnums.Orientation.Horizontal)
                {
                    if (mouseLocation.X < knobBound.X)
                        Value = value - ((adjust > 0) ? adjust : gap);
                    else if (mouseLocation.X > (knobBound.X + knobBound.Width))
                        Value = value + (gap - adjust);
                }
                else
                {
                    if (mouseLocation.Y > (knobBound.Y + knobBound.Height))
                        Value = value - gap;
                    else if (mouseLocation.Y < knobBound.Y)
                        Value = value + gap;
                }
            }
        }

        protected override void HandleMouseRelease(int button, Point mouseLocation)
        {
            if (!enabled)
                return;

            foreach (MouseListener listener in mouseListeners)
                listener.MouseReleased(button, mouseLocation);

            mouseDown = false;

            valueIsAdjusting = false;
        }

        protected override void HandleMouseMove(Point mouseLocation)
        {
            base.HandleMouseMove(mouseLocation);

            if (!within || !enabled || !visible)
                return;

            knobWithin = TestKnobWithin(mouseLocation);

            if (knobWithin && mouseDown)
                valueIsAdjusting = true;

            if (valueIsAdjusting)
            {
                if (orientation == GoblinEnums.Orientation.Horizontal)
                {
                    if (mouseLocation.X < paintBounds.X + 4)
                        Value = minValue;
                    else if (mouseLocation.X > paintBounds.X + paintBounds.Width - 4)
                        Value = maxValue;
                    else
                    {
                        int val = (int)((mouseLocation.X - (paintBounds.X + 4)) / knobIncrement + minValue);
                        Value = val;
                    }
                }
                else
                {
                    if (mouseLocation.Y < paintBounds.Y + 4)
                        Value = maxValue;
                    else if (mouseLocation.Y > paintBounds.Y + paintBounds.Height - 4)
                        Value = minValue;
                    else
                    {
                        int val = (int)(maxValue - ((mouseLocation.Y - (paintBounds.Y + 4)) / knobIncrement));
                        Value = val;
                    }
                }
            }
        }

        /// <summary>
        /// Paints dashed border when focused.
        /// </summary>
        protected override void PaintBorder()
        {
            if (!enabled || !focused)
                return;

            UI2DRenderer.DrawRectangle(paintBounds, Color.Yellow, 1);
        }

        protected override void PaintComponent()
        {
            // Render the track first since it appears on the background of everything
            if (paintTrack)
            {
                UI2DRenderer.DrawRectangle(trackBound, trackBorderColor, 1);

                UI2DRenderer.FillRectangle(trackBound, null, trackColor);
            }

            // Render the knob
            if (paintTicks) 
            { // If ticks are painted, then the lower part of the knob will change to a pointing shape
                if (orientation == GoblinEnums.Orientation.Horizontal)
                {
                    UI2DRenderer.DrawLine(knobBound.X, knobBound.Y, knobBound.X + knobBound.Width,
                        knobBound.Y, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X, knobBound.Y, knobBound.X,
                        knobBound.Y + knobBound.Height - 5, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X + knobBound.Width, knobBound.Y,
                        knobBound.X + knobBound.Width, knobBound.Y + knobBound.Height - 5, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X, knobBound.Y + knobBound.Height - 5,
                        knobBound.X + 5, knobBound.Y + knobBound.Height, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X + 5, knobBound.Y + knobBound.Height, 
                        knobBound.X + knobBound.Width, knobBound.Y + knobBound.Height - 5, knobBorderColor, 1);
                }
                else
                {
                    UI2DRenderer.DrawLine(knobBound.X, knobBound.Y, knobBound.X,
                        knobBound.Y + knobBound.Height, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X, knobBound.Y, knobBound.X + knobBound.Width - 5,
                        knobBound.Y, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X, knobBound.Y + knobBound.Height,
                        knobBound.X + knobBound.Width - 5, knobBound.Y + knobBound.Height, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X + knobBound.Width - 5, knobBound.Y,
                        knobBound.X + knobBound.Width, knobBound.Y + 5, knobBorderColor, 1);

                    UI2DRenderer.DrawLine(knobBound.X + knobBound.Width - 5, knobBound.Y + knobBound.Height,
                        knobBound.X + knobBound.Width, knobBound.Y + knobBound.Height - 5, knobBorderColor, 1);
                }
            }
            else
            { // Otherwise, it's a simple rectangle
                Color c = (enabled) ? knobColor : disabledColor;
                // Render inside first
                UI2DRenderer.FillRectangle(knobBound, null, c);

                UI2DRenderer.DrawRectangle(knobBound, knobBorderColor, 1);
            }

            // Render the tickes
            if (paintTicks)
            {
                float nextLoc = 0;
                if (orientation == GoblinEnums.Orientation.Horizontal)
                {
                    nextLoc = tickBound.X + minorTickDelta;
                    // Render the minor ticks if minorTickSpacing is greater than 0
                    for (int i = 0; i < minorTickCount; i++)
                    {
                        UI2DRenderer.DrawLine((int)nextLoc, tickBound.Y, (int)nextLoc, tickBound.Y + 4,
                            knobBorderColor, 1);
                        nextLoc += minorTickDelta;
                    }

                    nextLoc = tickBound.X;
                    // Render the major ticks if majorTickSpacing is greater than 0
                    for (int i = 0; i <= majorTickCount; i++)
                    {
                        UI2DRenderer.DrawLine((int)nextLoc, tickBound.Y, (int)nextLoc, tickBound.Y + 8,
                            knobBorderColor, 1);
                        nextLoc += majorTickDelta;
                    }
                }
                else
                {
                    nextLoc = tickBound.Y + minorTickDelta;
                    // Render the minor ticks if minorTickSpacing is greater than 0
                    for (int i = 0; i < minorTickCount; i++)
                    {
                        UI2DRenderer.DrawLine(tickBound.X, (int)nextLoc, tickBound.X + 4, (int)nextLoc,
                            knobBorderColor, 1);
                        nextLoc += minorTickDelta;
                    }

                    nextLoc = tickBound.Y;
                    // Render the major ticks if majorTickSpacing is greater than 0
                    for (int i = 0; i <= majorTickCount; i++)
                    {
                        UI2DRenderer.DrawLine(tickBound.X, (int)nextLoc, tickBound.X + 8, (int)nextLoc,
                            knobBorderColor, 1);
                        nextLoc += majorTickDelta;
                    }
                }
            }

            // Render the labels
            if (paintLabels && textFont != null)
            {
                float nextLoc = 0;
                if (orientation == GoblinEnums.Orientation.Horizontal)
                {
                    nextLoc = tickBound.X;
                    // Render labels below major ticks only
                    for (int i = 0; i < tickLabels.Count; i++)
                    {
                        UI2DRenderer.WriteText(new Vector2((int)nextLoc - labelShifts[i], labelBound.Y + 1),
                            tickLabels[i], knobBorderColor, textFont);
                        nextLoc += majorTickDelta;
                    }
                }
                else
                {
                    nextLoc = tickBound.Y;
                    // Render labels next to major ticks only
                    for (int i = 0; i < tickLabels.Count; i++)
                    {
                        UI2DRenderer.WriteText(new Vector2(labelBound.X + 1, (int)nextLoc - 5),
                            tickLabels[i], knobBorderColor, textFont);
                        nextLoc += majorTickDelta;
                    }
                }
            }
        }
        #endregion
    }
}
