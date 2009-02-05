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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using GoblinXNA.Graphics;
using GoblinXNA.Device.Generic;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// An implementation of a check box, which is a box that can be selected or deselected.
    /// 
    /// In order to display the text next to the check box, G2DButton.TextFont must be set. 
    /// Otherwise, the text will not show up. Note that the size of the radio button icon depends on the
    /// font size. The larger the font size, the larger the radio button icon.
    /// </summary>
    public class G2DCheckBox : ToggleButton
    {
        #region Member Fields
        /// <summary>
        /// Size of the check box
        /// </summary>
        protected int size;
        private Point boxP1;
        private Point boxP2;
        private Color boxColor;
        private Color selectedColor;
        private Color color;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates an initially unselected check box button with the specified label.
        /// </summary>
        /// <param name="label">The text displayed on the right of the check box icon</param>
        public G2DCheckBox(String label)
            : base(label)
        {
            size = 12;
            drawBorder = false;
            highlightColor = Color.Yellow;
            selectedColor = Color.Green;

            color = Color.Navy;
            boxColor = Color.White;

            boxP1 = new Point();
            boxP2 = new Point();

            name = "G2DCheckBox";
        }
        /// <summary>
        /// Creates an initially unselected check box button with no text.
        /// </summary>
        public G2DCheckBox() : this("") { }
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

                size = bounds.Height - 14;
                boxP1.X = paintBounds.X + 2;
                boxP1.Y = paintBounds.Y + (bounds.Height - size) / 2;
                boxP2.X = boxP1.X + size;
                boxP2.Y = boxP1.Y + size;
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

                boxP1.X = paintBounds.X + 1;
                boxP1.Y = paintBounds.Y + (bounds.Height - size) / 2;
                boxP2.X = boxP1.X + size;
                boxP2.Y = boxP1.Y + size;
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

                color = new Color(color.R, color.G, color.B, alpha);
                selectedColor = new Color(selectedColor.R, selectedColor.G, selectedColor.B, alpha);
                boxColor = new Color(boxColor.R, boxColor.G, boxColor.B, alpha);
            }
        }
        #endregion

        #region Override Methods

        protected override void HandleMouseClick(int button, Point mouseLocation)
        {
            base.HandleMouseClick(button, mouseLocation);

            if (within && enabled && visible)
                DoClick();
        }

        protected override void PaintComponent()
        {
            base.PaintComponent();

            // Draw the check box part
            if (mouseDown && enabled)
            {
                // Use disabled color to display pressed down action
                UI2DRenderer.FillRectangle(new Rectangle(boxP1.X, boxP1.Y,
                    boxP2.X - boxP1.X, boxP2.Y - boxP1.Y), null, disabledColor);
            }
            else if (selected)
            {
                Color c = (enabled) ? boxColor : disabledColor;
                UI2DRenderer.DrawRectangle(new Rectangle(boxP1.X + 1, boxP1.Y + 1,
                    boxP2.X - boxP1.X - 3, boxP2.Y - boxP1.Y - 3), c, 2);

                UI2DRenderer.FillRectangle(new Rectangle(boxP1.X + 3, boxP1.Y + 3, 
                    boxP2.X - boxP1.X - 7, boxP2.Y - boxP1.Y - 7), null, selectedColor);
            }
            else
            {
                Color c = (enabled) ? boxColor : disabledColor;
                UI2DRenderer.FillRectangle(new Rectangle(boxP1.X + 1, boxP1.Y + 1, 
                    boxP2.X - boxP1.X - 1, boxP2.Y - boxP1.Y - 1), null, c);
            }
            
            // Draw the border of the check box part
            UI2DRenderer.DrawRectangle(new Rectangle(boxP1.X, boxP1.Y,
                    boxP2.X - boxP1.X, boxP2.Y - boxP1.Y), color, 1);

            // Show the highlight when mouse is over
            if (enabled && within && !mouseDown)
            {
                UI2DRenderer.DrawRectangle(new Rectangle(boxP1.X + 1, boxP1.Y + 1,
                    boxP2.X - boxP1.X - 3, boxP2.Y - boxP1.Y - 3), highlightColor, 2);
            }

            if (label.Length > 0 && textFont != null)
            {
                // Draw the label part
                int x = (int)boxP2.X + 5;
                int y = (int)(bounds.Height - textHeight) / 2 + paintBounds.Y;
                UI2DRenderer.WriteText(new Vector2(x, y), label, textColor, textFont);
            }
        }
        #endregion
    }
}
