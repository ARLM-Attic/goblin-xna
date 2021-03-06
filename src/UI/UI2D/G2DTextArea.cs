/************************************************************************************ 
 * Copyright (c) 2008-2012, Columbia University
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

using Microsoft.Xna.Framework.Input;

using GoblinXNA.Device.Generic;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// IMPLEMENTATION NOT FINISHED YET.
    /// </summary>
    internal class G2DTextArea : TextComponent
    {
        #region Member Fields
        protected int rows;
        #endregion

        #region Constructors
        public G2DTextArea(String text, int rows, int columns)
            : base(text, columns)
        {
            this.rows = rows;

            name = "G2DTextArea";
        }

        public G2DTextArea(String text) : this(text, 0, 0) { }

        public G2DTextArea(int rows, int columns) : this("", rows, columns) { }

        public G2DTextArea() : this("", 0, 0) { }
        #endregion

        #region Properties
        public virtual int Rows
        {
            get { return rows; }
            set { rows = value; }
        }
        #endregion

        #region Override Methods
        protected override void  UpdateCaret(Keys key, KeyModifier modifier)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        protected override void UpdateText(Keys key, KeyModifier modifier)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        protected override void PaintComponent()
        {
            base.PaintComponent();
        }
        #endregion
    }
}
