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

using GoblinXNA.UI.Events;

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// NOT COMPLETED YET.
    /// </summary>
    internal class G2DComboBox : G2DComponent
    {
        #region Member Fields
        protected List<Object> items;
        protected int selectedIndex;
        protected int maxRowCount;
        protected ListCellRenderer cellRenderer;

        protected List<ActionListener> actionListeners;
        protected List<ItemListener> itemListeners;
        #endregion

        #region Constructors
        public G2DComboBox(List<Object> items) 
            : base()
        {
            this.items = items;
            selectedIndex = -1;
            maxRowCount = 10;
            cellRenderer = new DefaultListCellRenderer();

            actionListeners = new List<ActionListener>();
            itemListeners = new List<ItemListener>();

            name = "G2DComboBox";
        }

        public G2DComboBox() : this(new List<Object>()) { }
        #endregion

        #region Properties
        public virtual int MaximumRowCount
        {
            get { return maxRowCount; }
            set { maxRowCount = value; }
        }

        public virtual ListCellRenderer CellRenderer
        {
            get { return cellRenderer; }
            set { cellRenderer = value; }
        }

        public virtual int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; }
        }

        public virtual Object SelectedItem
        {
            get
            {
                if (selectedIndex == -1)
                    return null;
                else
                    return items[selectedIndex];
            }
            set
            {
                if (value != null && items.Contains(value))
                    selectedIndex = items.IndexOf(value);
            }
        }
        #endregion

        #region Action Methods
        public virtual void AddItem(Object item)
        {
            if (item != null)
                items.Add(item);
        }

        public virtual bool RemoveItem(Object item)
        {
            return items.Remove(item);
        }

        public virtual bool RemoveItemAt(int index)
        {
            if (index < 0 || index >= items.Count)
                return false;
            else
            {
                items.RemoveAt(index);
                return true;
            }
        }

        public virtual void RemoveAllItem()
        {
            items.Clear();
        }
        #endregion


        public virtual List<Object> GetAllItems()
        {
            return items;
        }

        public virtual Object GetItemAt(int index)
        {
            if (index < 0 || index >= items.Count)
                return null;
            else
                return items[index];
        }

        public virtual void AddActionListener(ActionListener listener)
        {
            if(listener != null && !actionListeners.Contains(listener))
                actionListeners.Add(listener);
        }

        public virtual void AddItemListener(ItemListener listener)
        {
            if (listener != null && !itemListeners.Contains(listener))
                itemListeners.Add(listener);
        }

        public virtual void RemoveActionListener(ActionListener listener)
        {
            actionListeners.Remove(listener);
        }

        public virtual void RemoveItemListener(ItemListener listener)
        {
            itemListeners.Remove(listener);
        }

        public virtual List<ActionListener> GetActionListeners()
        {
            return actionListeners;
        }

        public virtual List<ItemListener> GetItemListeners()
        {
            return itemListeners;
        }

        public virtual void ShowPopup()
        {

        }
    }
}
