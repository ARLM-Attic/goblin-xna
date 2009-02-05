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
using Microsoft.Xna.Framework.Graphics;

using GoblinXNA.Graphics;
using GoblinXNA.Graphics.ParticleEffects;

namespace GoblinXNA.Shaders
{
    /// <summary>
    /// An implementation of a simple particle effect shader. The implementation is based on the
    /// Particle 3D tutorial from the XNA Creators Club website (http://creators.xna.com).
    /// </summary>
    internal class ParticleShader : Shader
    {
        #region Member Fields

        protected EffectParameter view,
            viewportHeight,
            time,
            duration,
            durationRandomness,
            gravity,
            endVelocity,
            minColor,
            maxColor,
            rotateSpeed,
            startSize,
            endSize,
            texture;

        protected String prevTextureName;
        protected Texture2D texture2d;
        protected bool parametersSet;

        #endregion

        #region Constructors
        /// <summary>
        /// Creates a particle effect shader.
        /// </summary>
        public ParticleShader()
            : base("ParticleEffect")
        {
            prevTextureName = "";
            texture2d = null;
            parametersSet = false;
        }

        #endregion

        public override int MaxLights
        {
            get { return 0; }
        }

        protected override void GetParameters()
        {
            view = effect.Parameters["View"];
            projection = effect.Parameters["Projection"];
            viewportHeight = effect.Parameters["ViewportHeight"];
            time = effect.Parameters["CurrentTime"];
            duration = effect.Parameters["Duration"];
            durationRandomness = effect.Parameters["DurationRandomness"];
            gravity = effect.Parameters["Gravity"];
            endVelocity = effect.Parameters["EndVelocity"];
            minColor = effect.Parameters["MinColor"];
            maxColor = effect.Parameters["MaxColor"];
            rotateSpeed = effect.Parameters["RotateSpeed"];
            startSize = effect.Parameters["StartSize"];
            endSize = effect.Parameters["EndSize"];
            texture = effect.Parameters["Texture"];
        }

        public override void SetParameters(ParticleEffect particleEffect)
        {
            time.SetValue(particleEffect.CurrentTime);

            if (!parametersSet)
            {
                duration.SetValue((float)particleEffect.Duration.TotalSeconds);
                durationRandomness.SetValue(particleEffect.DurationRandomness);
                gravity.SetValue(particleEffect.Gravity);
                endVelocity.SetValue(particleEffect.EndVelocity);
                minColor.SetValue(particleEffect.MinColor.ToVector4());
                maxColor.SetValue(particleEffect.MaxColor.ToVector4());
                rotateSpeed.SetValue(
                    new Vector2(particleEffect.MinRotateSpeed, particleEffect.MaxRotateSpeed));
                startSize.SetValue(
                    new Vector2(particleEffect.MinStartSize, particleEffect.MaxStartSize));
                endSize.SetValue(
                    new Vector2(particleEffect.MinEndSize, particleEffect.MaxEndSize));

                // reloads a new texture if the texture name is different from the 
                // previous one
                if (!prevTextureName.Equals(particleEffect.TextureName))
                {
                    prevTextureName = particleEffect.TextureName;
                    texture2d = State.Content.Load<Texture2D>(
                        Path.Combine(State.GetSettingVariable("TextureDirectory"),
                        prevTextureName));
                }

                if (texture2d != null)
                    texture.SetValue(texture2d);

                parametersSet = true;
            }
        }

        public override void Render(Matrix worldMatrix, string techniqueName, RenderHandler renderDelegate)
        {
            if (techniqueName == null)
                throw new GoblinException("techniqueName is null");
            if (renderDelegate == null)
                throw new GoblinException("renderDelegate is null");

            view.SetValue(State.ViewMatrix);
            projection.SetValue(State.ProjectionMatrix);
            viewportHeight.SetValue(State.Height);
            // Start shader
            effect.CurrentTechnique = effect.Techniques[techniqueName];
            try
            {
                effect.Begin(SaveStateMode.None);

                // Render all passes (usually just one)
                //foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    renderDelegate();
                    pass.End();
                }
            }
            finally
            {
                // End shader
                effect.End();
            }
        }
    }
}
