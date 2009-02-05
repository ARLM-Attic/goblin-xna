using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using GoblinXNA.Network;
using GoblinXNA.Helpers;

namespace ARDominos
{
    public class GameState : INetworkObject
    {
        #region Mode Enums
        /// <summary>
        /// The game mode
        /// </summary>
        public enum GameMode
        {
            /// <summary>
            /// Edit mode allows the player to modify the position and orientation of 
            /// existing dominos
            /// </summary>
            Edit,
            /// <summary>
            /// Add mode allows the player to add new dominos on the game board
            /// </summary>
            Add,
            /// <summary>
            /// Play mode allows the player to play the domino smash game in which the
            /// player throws balls at the dominos and tries to get all of them off the
            /// board
            /// </summary>
            Play
        }

        /// <summary>
        /// The sub-modes in the 'Edit' mode
        /// </summary>
        public enum EditMode
        {
            /// <summary>
            /// Select and edit single domino at a time
            /// </summary>
            Single,
            /// <summary>
            /// Select and edit multiple dominos at a time
            /// </summary>
            Multiple
        }

        /// <summary>
        /// The sub-modes in the 'Add' mode
        /// </summary>
        public enum AdditionMode
        {
            /// <summary>
            /// Add single domino at a time
            /// </summary>
            Single,
            /// <summary>
            /// Add multiple dominos on the line drawn by the player
            /// </summary>
            LineDrawing
        }
        #endregion

        #region Member Fields

        private bool readyToSend;
        private bool hold;
        private int sendFrequencyInFrameCount;
        private int sendFrequencyInHertz;

        // The current game mode (Add, Edit, or Play)
        private GameMode gameMode;

        // The current edit mode (Single, or Multiple)
        private EditMode editMode;

        // The curent add mode (Single, or LineDrawing)
        private AdditionMode addMode;

        // The elapsed time since the game play is started in GameMode.Play mode
        private double elapsedSecond;
        private double elapsedMinute;

        private bool gameOver;
        private int winner;

        #endregion

        #region Constructor

        public GameState()
        {
            gameMode = GameMode.Add;
            editMode = EditMode.Single;
            addMode = AdditionMode.Single;

            elapsedMinute = 0;
            elapsedSecond = 0;

            gameOver = false;
            winner = -1;
        }

        #endregion

        #region Properties
        public String Identifier
        {
            get { return "ARDomino_GameState"; }
        }

        public bool ReadyToSend
        {
            get { return readyToSend; }
            set { readyToSend = value; }
        }

        public bool Hold
        {
            get { return hold; }
            set { hold = value; }
        }

        public int SendFrequencyInFrameCount
        {
            get { return sendFrequencyInFrameCount; }
            set { sendFrequencyInFrameCount = value; }
        }

        public int SendFrequencyInHertz
        {
            get { return sendFrequencyInHertz; }
            set { sendFrequencyInHertz = value; }
        }

        public GameMode CurrentGameMode
        {
            get { return gameMode; }
            set { gameMode = value; }
        }

        public EditMode CurrentEditMode
        {
            get { return editMode; }
            set { editMode = value; }
        }

        public AdditionMode CurrentAdditionMode
        {
            get { return addMode; }
            set { addMode = value; }
        }

        public double ElapsedSecond
        {
            get { return elapsedSecond; }
            set { elapsedSecond = value; }
        }

        public double ElapsedMinute
        {
            get { return elapsedMinute; }
            set { elapsedMinute = value; }
        }

        public bool GameOver
        {
            get { return gameOver; }
            set { gameOver = value; }
        }

        public int Winner
        {
            get { return winner; }
            set { winner = value; }
        }
        #endregion

        #region Public Methods
        public void ResetState()
        {
            elapsedMinute = 0;
            elapsedSecond = 0;
            gameOver = false;
        }

        public byte[] GetMessage()
        {
            return null;
        }

        public void InterpretMessage(byte[] msg)
        {
            
        }

        #endregion
    }
}
