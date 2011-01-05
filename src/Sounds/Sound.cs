/************************************************************************************ 
 *
 * Microsoft XNA Community Game Platform
 * Copyright (C) Microsoft Corporation. All rights reserved.
 * 
 * ===================================================================================
 * Modified by: Ohan Oda (ohan@cs.columbia.edu)
 * 
 *************************************************************************************/ 

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

using GoblinXNA.Helpers;

namespace GoblinXNA.Sounds
{
    /// <summary>
    /// A wrapper class for the XNA audio library. This class provides an easy interface to play both
    /// 2D and 3D sounds.
    /// </summary>
    public sealed class Sound
    {
        #region Member Fields
        private static AudioEngine audioEngine;
        private static WaveBank waveBank;
        private static SoundBank soundBank;

        private static bool initialized = false;

        private static List<Cue3D> activeCues;
        private static Stack<Cue3D> cuePool;

        // For 3D sound effect
        private static AudioListener listener;
        private static AudioEmitter emitter;

        private static Vector3 prevListenerPos;
        #endregion

        #region Constructors
        private Sound()
        {
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the audio engine.
        /// </summary>
        public static AudioEngine AudioEngine
        {
            get { return audioEngine; }
        }

        /// <summary>
        /// Gets the wave bank.
        /// </summary>
        public static WaveBank WaveBank
        {
            get { return waveBank; }
        }

        /// <summary>
        /// Gets the sound bank.
        /// </summary>
        public static SoundBank SoundBank
        {
            get { return soundBank; }
        }

        #endregion

        #region Public Static Methods
        /// <summary>
        /// Initializes the audio system with the given XACT project file (.xap) compiled using
        /// Microsoft Cross-Platform Audio Creation Tool that comes with XNA Game Studio 2.0
        /// </summary>
        /// <param name="xapAssetName">The asset name of the XACT project file file</param>
        public static void Initialize(String xapAssetName)
        {
            try
            {
                String name = Path.GetFileNameWithoutExtension(xapAssetName);
                audioEngine = new AudioEngine(Path.Combine(State.Content.RootDirectory + "/" +
                    State.GetSettingVariable("AudioDirectory"), name + ".xgs"));
                waveBank = new WaveBank(audioEngine, Path.Combine(State.Content.RootDirectory + 
                    "/" + State.GetSettingVariable("AudioDirectory"), "Wave Bank.xwb"));

                if (waveBank != null)
                {
                    soundBank = new SoundBank(audioEngine,
                        Path.Combine(State.Content.RootDirectory + "/" +
                        State.GetSettingVariable("AudioDirectory"), "Sound Bank.xsb"));
                }

                activeCues = new List<Cue3D>();
                cuePool = new Stack<Cue3D>();

                listener = new AudioListener();
                emitter = new AudioEmitter();

                initialized = true;
            }
            catch (NoAudioHardwareException nahe)
            {
                Log.Write(nahe.Message);
            }
        }

        /// <summary>
        /// Triggers a new sound.
        /// </summary>
        /// <remarks>
        /// In order to free up unnecessary memory usage, the played cue is automatically destroyed
        /// when it stops playing. 
        /// </remarks>
        /// <exception cref="GoblinException">Throws exception if this is called before Initialize(..)</exception>
        /// <param name="cueName">The name of the cue of a sound</param>
        /// <returns></returns>
        public static Cue Play(String cueName)
        {
            if (!initialized)
                throw new GoblinException("Sound engine is not initialized. Call Sound.Initialize(..) first");

            Cue3D cue3D;

            if (cuePool.Count > 0)
            {
                // If possible, reuse an existing Cue instance.
                cue3D = cuePool.Pop();
            }
            else
            {
                // Otherwise we have to allocate a new one.
                cue3D = new Cue3D();
            }

            // Fill in the cue and emitter fields.
            cue3D.Cue = soundBank.GetCue(cueName);
            cue3D.Emitter = null;

            cue3D.Cue.Play();

            // Remember that this cue is now active.
            activeCues.Add(cue3D);

            return cue3D.Cue;
        }

        /// <summary>
        /// Triggers a new 3D sound
        /// </summary>
        /// <remarks>
        /// In order to free up unnecessary memory usage, the played cue is automatically destroyed
        /// when it stops playing. 
        /// </remarks>
        /// <exception cref="GoblinException">Throws exception if this is called before Initialize(..)</exception>
        /// <param name="cueName">The name of the cue of a sound</param>
        /// <param name="emitter">An IAudioEmitter object that defines the properties of the sound
        /// including position, and velocity.</param>
        /// <returns></returns>
        public static Cue Play3D(String cueName, IAudioEmitter emitter)
        {
            if (!initialized)
                throw new GoblinException("Sound engine is not initialized. Call Sound.Initialize(..) first");

            Cue3D cue3D;

            if (cuePool.Count > 0)
            {
                // If possible, reuse an existing Cue3D instance.
                cue3D = cuePool.Pop();
            }
            else
            {
                // Otherwise we have to allocate a new one.
                cue3D = new Cue3D();
            }

            // Fill in the cue and emitter fields.
            cue3D.Cue = soundBank.GetCue(cueName);
            cue3D.Emitter = emitter;

            // Set the 3D position of this cue, and then play it.
            Apply3D(cue3D);

            cue3D.Cue.Play();

            // Remember that this cue is now active.
            activeCues.Add(cue3D);

            return cue3D.Cue;
        }

        /// <summary>
        /// Sets the volume of sounds in certain category
        /// </summary>
        /// <param name="categoryName">The name of the category</param>
        /// <param name="volume">The volume in dB</param>
        public static void SetVolume(String categoryName, float volume)
        {
            audioEngine.GetCategory(categoryName).SetVolume(volume);
        }

        /// <summary>
        /// Updates the XNA audio engine
        /// </summary>
        /// <param name="gameTime"></param>
        internal static void Update(GameTime gameTime)
        {
            if (!initialized)
                return;

            // Loop over all the currently playing 3D sounds.
            int index = 0;

            while (index < activeCues.Count)
            {
                Cue3D cue3D = activeCues[index];

                if (cue3D.Cue.IsStopped)
                {
                    // If the cue has stopped playing, dispose it.
                    cue3D.Cue.Dispose();

                    // Store the Cue3D instance for future reuse.
                    cuePool.Push(cue3D);

                    // Remove it from the active list.
                    activeCues.RemoveAt(index);
                }
                else
                {
                    // If the cue is still playing and it's 3D, update its 3D settings.
                    if(cue3D.Emitter != null)
                        Apply3D(cue3D);

                    index++;
                }
            }

            // Update the XACT engine.
            audioEngine.Update();
        }

        /// <summary>
        /// Updates the position and orientation of the listener for 3D audio effect
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="position">The position of the listener</param>
        /// <param name="forward">The forward vector of the listener</param>
        /// <param name="up">The up vector of the lister</param>
        internal static void UpdateListener(GameTime gameTime, Vector3 position, Vector3 forward,
            Vector3 up)
        {
            if (!initialized)
                return;

            listener.Position = position;
            listener.Up = up;
            listener.Forward = forward;
            listener.Velocity = (position - prevListenerPos) /
                (float)gameTime.ElapsedGameTime.TotalSeconds;
            prevListenerPos = position;
        }

        public static void Dispose()
        {
            if (audioEngine != null)
            {
                soundBank.Dispose();
                waveBank.Dispose();
                audioEngine.Dispose();
            }
        }
        #endregion

        #region Private Static Methods
        /// <summary>
        /// Updates the position and velocity settings of a 3D cue.
        /// </summary>
        private static void Apply3D(Cue3D cue3D)
        {
            emitter.Position = cue3D.Emitter.Position;
            emitter.Forward = cue3D.Emitter.Forward;
            emitter.Up = cue3D.Emitter.Up;
            emitter.Velocity = cue3D.Emitter.Velocity;

            cue3D.Cue.Apply3D(listener, emitter);
        }
        #endregion

        #region Private Classes
        /// <summary>
        /// Internal helper class for keeping track of an active 3D cue,
        /// and remembering which emitter object it is attached to.
        /// </summary>
        private class Cue3D
        {
            public Cue Cue;
            public IAudioEmitter Emitter;
        }
        #endregion
    }
}
