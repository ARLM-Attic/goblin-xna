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

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// A text field displays a text string based on the user's keyboard input. This component
    /// needs to be focused in order to receive the keyboard input.
    /// 
    /// In order to display the text and the caret, G2DTextField.TextFont must be set. 
    /// Otherwise, the text and caret will not show up.
    /// </summary>
    public class G2DTextField : TextComponent
    {
        #region Member Fields
        /// <summary>
        /// Creates a text field with specified initial text string and the width of the text field.
        /// </summary>
        /// <param name="text">An initial text string</param>
        /// <param name="columns">The width of the text field that is used to determine
        /// how many characters this text field can display</param>
        public G2DTextField(String text, int columns)
            : base(text, columns)
        {
            name = "G2DTextField";
            backgroundColor = Color.White;
        }

        /// <summary>
        /// Creates a text field with specified initial text and column width of 30.
        /// </summary>
        /// <param name="text">An initial text string</param>
        public G2DTextField(String text) : this(text, 30) { }

        /// <summary>
        /// Creates a text field with specified column width.
        /// </summary>
        /// <param name="columns"></param>
        public G2DTextField(int columns) : this("", columns) { }

        /// <summary>
        /// Creates a text field with column width of 30.
        /// </summary>
        public G2DTextField() : this("", 30) { }
        #endregion

        #region Override Methods
        protected override void HandleKeyPress(Keys key, KeyModifier modifier)
        {
            bool handle = true;
            if (columns != 0)
            {
                if (label.Length >= columns)
                    handle = false;
            }
            else
                if (caretPosition.X + 20 > bounds.Width)
                    handle = false;

            if(handle)
                base.HandleKeyPress(key, modifier);
        }

        protected override void UpdateCaret(Keys key, KeyModifier modifier)
        {
            switch (key)
            {
                case Keys.Back:
                case Keys.Delete:
                    if (label.Length > 0)
                        caretPosition.X = (int)(textWidth + 4);
                    break;
                case Keys.Enter:
                    break;
                default:
                    caretPosition.X = (int)(textWidth + 4);
                    break;
            }
        }

        protected override void UpdateText(Keys key, KeyModifier modifier)
        {
            switch (key)
            {
                case Keys.Back:
                case Keys.Delete:
                    if (label.Length > 0)
                        Text = label.Substring(0, label.Length - 1);
                    break;
                case Keys.Enter:
                    break;
                default:
                    Text += "" + KeyboardInput.Instance.KeyToChar(key, modifier.ShiftKeyPressed);
                    break;
            }
        }

        protected override void PaintComponent()
        {
            base.PaintComponent();

            if (label.Length > 0)
            {
                int x = paintBounds.X, y = paintBounds.Y;
                switch (horizontalAlignment)
                {
                    case GoblinEnums.HorizontalAlignment.Left:
                        x += 4;
                        break;
                    case GoblinEnums.HorizontalAlignment.Center:
                        x += (int)(paintBounds.Width - textWidth) / 2;
                        break;
                    case GoblinEnums.HorizontalAlignment.Right:
                        x += (int)(paintBounds.Width - textWidth);
                        break;
                }

                switch (verticalAlignment)
                {
                    case GoblinEnums.VerticalAlignment.Top:
                        y += (int)(bounds.Height - textHeight) / 2;
                        break;
                    case GoblinEnums.VerticalAlignment.Center:
                        y += (int)(paintBounds.Height - textHeight) / 2;
                        break;
                    case GoblinEnums.VerticalAlignment.Bottom:
                        y += (int)(paintBounds.Height - textHeight) - 1;
                        break;
                }

                if(textFont != null)
                    UI2DRenderer.WriteText(new Vector2(x, y), label, textColor, textFont);
            }
        }
        #endregion
    }
}
