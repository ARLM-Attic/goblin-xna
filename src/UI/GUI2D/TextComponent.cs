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

using GoblinXNA.Device.Generic;
using GoblinXNA.UI.Events;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// An abstract class for 2D components that can accept textual input through the keyboard.
    /// </summary>
    abstract public class TextComponent : G2DComponent
    {
        #region Member Fields
        protected bool editable;
        protected int columns;
        protected int selectStart;
        protected int selectEnd;
        protected int caretTimer;
        protected int caretBlinkInterval;
        protected Point caretPosition;
        protected bool drawCaret;

        protected List<CaretListener> caretListeners;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a text component with an initial text and number of columns (width).
        /// </summary>
        /// <param name="text">The initial text</param>
        /// <param name="columns">The number of characters a row can display</param>
        public TextComponent(String text, int columns)
            : base()
        {
            this.label = text;
            this.columns = columns;

            caretListeners = new List<CaretListener>();
            selectStart = selectEnd = 0;
            caretTimer = 0;
            caretBlinkInterval = 10;
            caretPosition = new Point(4, 0);

            drawCaret = true;
            editable = true;
        }

        public TextComponent(String text) : this(text, 50) { }

        public TextComponent(int columns) : this("", columns) { }

        public TextComponent() : this("", 50) { }
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets whether this text component is editable.
        /// </summary>
        public virtual bool Editable
        {
            get { return editable; }
            set { editable = value; }
        }

        /// <summary>
        /// Gets or sets the number of columns.
        /// </summary>
        public virtual int Columns
        {
            get { return columns; }
            set { columns = value; }
        }

        /// <summary>
        /// Gets or sets the blink interval of the caret.
        /// </summary>
        /// <remarks>
        /// The default value is 10.
        /// </remarks>
        public virtual int CaretBlinkInterval
        {
            get { return caretBlinkInterval; }
            set { caretBlinkInterval = value; }
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

                caretPosition.X = paintBounds.X + 4;
                if(textFont != null)
                    caretPosition.Y = paintBounds.Y + 
                        (int)(bounds.Height - textFont.MeasureString("a").Y) / 2;
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

                caretPosition.X = paintBounds.X + 4;
                if (textFont != null)
                    caretPosition.Y = paintBounds.Y +
                        (int)(bounds.Height - textFont.MeasureString("a").Y) / 2;
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

                if (textFont != null)
                    caretPosition.Y = paintBounds.Y +
                        (int)(bounds.Height - textFont.MeasureString("a").Y) / 2;
            }
        }

        public virtual List<CaretListener> CaretListeners
        {
            get { return caretListeners; }
        }
        #endregion

        #region Override Methods
        protected override void HandleMouseClick(int button, Point mouseLocation)
        {
            base.HandleMouseClick(button, mouseLocation);

            if (within && !focused)
            {
                Focused = true;

                if (UIRenderer.GlobalFocus2DComp != null)
                    UIRenderer.GlobalFocus2DComp.Focused = false;

                UIRenderer.GlobalFocus2DComp = this;
            }
        }
        #endregion

        #region Event Listener Methods
        public virtual void AddCaretListener(CaretListener listener)
        {
            if (listener != null && !caretListeners.Contains(listener))
                caretListeners.Add(listener);
        }

        public virtual void RemoveCaretListener(CaretListener listener)
        {
            caretListeners.Remove(listener);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Selects the text from a start index to an end index.
        /// </summary>
        /// <param name="selectStart"></param>
        /// <param name="selectEnd"></param>
        public virtual void Select(int selectStart, int selectEnd)
        {
            this.selectStart = selectStart;
            this.selectEnd = selectEnd;
        }

        /// <summary>
        /// Selects all of the text.
        /// </summary>
        public virtual void SelectAll()
        {
            selectStart = 0;
            selectEnd = label.Length - 1;
        }

        /// <summary>
        /// Cuts the selected text.
        /// </summary>
        public virtual void Cut()
        {
        }

        /// <summary>
        /// Copies the selected text into the clipboard.
        /// </summary>
        public virtual void Copy()
        {
        }

        /// <summary>
        /// Pastes a text from another source to this component.
        /// </summary>
        public virtual void Paste()
        {
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Renders the highlight color on the background of selected text.
        /// </summary>
        protected virtual void RenderHighlight()
        {
            if (selectStart != selectEnd)
            {

            }
        }

        /// <summary>
        /// Renders the caret.
        /// </summary>
        protected virtual void RenderCaret()
        {
            if (drawCaret && textFont != null)
            {
                UI2DRenderer.DrawLine(paintBounds.X + caretPosition.X, caretPosition.Y + 
                    (int)(textFont.MeasureString(label).Y),
                    paintBounds.X + caretPosition.X, caretPosition.Y, Color.Black, 1);
            }

            if (caretTimer >= caretBlinkInterval)
            {
                drawCaret = !drawCaret;
                caretTimer = 0;
            }
        }

        /// <summary>
        /// Updates the caret position when there is a keyboard input.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifier"></param>
        abstract protected void UpdateCaret(Keys key, KeyModifier modifier);

        /// <summary>
        /// Updates the text when there is a keyboard input.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="modifier"></param>
        abstract protected void UpdateText(Keys key, KeyModifier modifier);

        protected override void HandleKeyPress(Keys key, KeyModifier modifier)
        {
            if (focused && editable)
            {
                base.HandleKeyPress(key, modifier);
                UpdateText(key, modifier);
                UpdateCaret(key, modifier);
                CaretEvent evt = new CaretEvent(this, caretPosition);
                foreach (CaretListener listener in caretListeners)
                    listener.CaretUpdate(evt);
            }
        }

        public override void RenderWidget()
        {
            base.RenderWidget();

            if (visible && focused && editable)
            {
                RenderHighlight();
                RenderCaret();
                caretTimer++;
            }
        }
        #endregion
    }
}
