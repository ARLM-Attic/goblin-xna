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

namespace GoblinXNA.UI.UI2D
{
    /// <summary>
    /// A helper class for performing 2D drawing, such as drawing lines, filling or drawing 
    /// rectangular or circle shapes.
    /// </summary>
    public class UI2DRenderer
    {
        #region Static Member Fields
        private static SpriteBatch spriteBatch = new SpriteBatch(State.Device);
        private static Dictionary<Circle, Texture2D> circleTextures = new Dictionary<Circle, Texture2D>();
        #endregion

        #region Public Static Drawing Methods
        /// <summary>
        /// Fills a rectangle portion of the screen with given texture and color. If just want to fill
        /// it with a color, then set 'texture' to null.
        /// </summary>
        /// <param name="rect">A rectangle region to fill</param>
        /// <param name="texture">A texture to use for filling</param>
        /// <param name="color">A color to use for filling</param>
        public static void FillRectangle(Rectangle rect, Texture2D texture, Color color)
        {
            if (spriteBatch == null)
                return;

            if (texture == null)
                texture = State.BlankTexture;

            // Start rendering with alpha blending mode, and render back to front
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);

            spriteBatch.Draw(texture, rect, color);

            // Finish rendering
            spriteBatch.End();
        }

        /// <summary>
        /// Draws a rectangle with the given color and pixel width.
        /// </summary>
        /// <param name="rect">A rectangle to draw</param>
        /// <param name="color">The color of the line</param>
        /// <param name="pixelWidth">The width of the line</param>
        public static void DrawRectangle(Rectangle rect, Color color, int pixelWidth)
        {
            if (spriteBatch == null)
                return;

            // Start rendering with alpha blending mode, and render back to front
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);

            spriteBatch.Draw(State.BlankTexture, new Rectangle(rect.X, rect.Y, rect.Width, 
                pixelWidth), color);
            spriteBatch.Draw(State.BlankTexture, new Rectangle(rect.X, rect.Y, pixelWidth, 
                rect.Height), color);
            spriteBatch.Draw(State.BlankTexture, new Rectangle(rect.X, rect.Y + rect.Height - pixelWidth, 
                rect.Width, pixelWidth), color);
            spriteBatch.Draw(State.BlankTexture, new Rectangle(rect.X + rect.Width - pixelWidth, rect.Y, 
                pixelWidth, rect.Height), color);

            // Finish rendering
            spriteBatch.End();
        }

        /// <summary>
        /// Draws a line between two given points with the given color and pixel width
        /// </summary>
        /// <param name="p1">The starting point of the line</param>
        /// <param name="p2">The endint point of the line</param>
        /// <param name="color">The color of the line</param>
        /// <param name="pixelWidth">The width of the line</param>
        public static void DrawLine(Point p1, Point p2, Color color, int pixelWidth)
        {
            DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, pixelWidth);
        }

        /// <summary>
        /// Draws a line between two given points with the given color and pixel width
        /// </summary>
        /// <param name="x1">The x-coordinate of the starting point of the line</param>
        /// <param name="y1">The y-coordinate of the starting point of the line</param>
        /// <param name="x2">The x-coordinate of the ending point of the line</param>
        /// <param name="y2">The y-coordinate of the ending point of the line</param>
        /// <param name="color">The color of the line</param>
        /// <param name="pixelWidth">The width of the line</param>
        public static void DrawLine(int x1, int y1, int x2, int y2, Color color, int pixelWidth)
        {
            if (spriteBatch == null)
                return;

            int xDiff = x2 - x1;
            int yDiff = y2 - y1;
            Rectangle rect = new Rectangle(x1, y1, (int)(Math.Sqrt(xDiff * xDiff +
                yDiff * yDiff)), pixelWidth);
            float angle = 0;
            if (xDiff != 0)
            {
                angle = (float)(Math.Atan(yDiff / xDiff));
                if (xDiff < 0)
                    angle += (float)Math.PI;
            }
            else
            {
                if (y2 > y1)
                    angle = MathHelper.PiOver2;
                else
                    angle = MathHelper.PiOver2 * 3;
            }

            // Start rendering with alpha blending mode, and render back to front
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);

            spriteBatch.Draw(State.BlankTexture, rect, null, color, angle, Vector2.Zero, SpriteEffects.None,
                0);

            // Finish rendering
            spriteBatch.End();
        }

        /// <summary>
        /// Draws a circle centered at (x, y) with specified radius and a color.
        /// </summary>
        /// <param name="x">The x-coordinate of the center</param>
        /// <param name="y">The y-coordinate of the center</param>
        /// <param name="radius">The radius of this circle</param>
        /// <param name="color">The color of the circle</param>
        public static void DrawCircle(int x, int y, int radius, Color color)
        {
            if (spriteBatch == null)
                return;

            Circle circle = new Circle(x, y, radius, false);
            Texture2D texture = null;
            Rectangle rect = new Rectangle(x - radius, y - radius, 2 * radius + 1, 2 * radius + 1);

            if (circleTextures.ContainsKey(circle))
                texture = circleTextures[circle];
            else
            {
                texture = new Texture2D(State.Device, rect.Width, rect.Height, 1, TextureUsage.None,
                    SurfaceFormat.Bgra5551);

                ushort[] data = new ushort[rect.Width * rect.Height];

                int i = 0;
                int j = radius;
                int d = 3 - 2 * radius;
                while (i < j)
                {
                    CirclePoints(data, i, j, rect.Width, radius);
                    if (d < 0)
                        d = d + 4 * i + 6;
                    else
                    {
                        d = d + 4 * (i - j) + 10;
                        j--;
                    }
                    i++;
                }
                if (i == j)
                    CirclePoints(data, i, j, rect.Width, radius);

                texture.SetData(data);
                circleTextures.Add(circle, texture);
            }

            // Start rendering with alpha blending mode, and render back to front
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);

            spriteBatch.Draw(texture, rect, color);

            // Finish rendering
            spriteBatch.End();
        }

        /// <summary>
        /// Draws a circle centered at the 'center' point with the specified 'radius' and a 'color'.
        /// </summary>
        /// <param name="center">The center point of the circle</param>
        /// <param name="radius">The radius of this circle</param>
        /// <param name="color">The color of the circle</param>
        public static void DrawCircle(Point center, int radius, Color color)
        {
            DrawCircle(center.X, center.Y, radius, color);
        }

        /// <summary>
        /// Fills a circlular region centered at (x, y) with the specified radius and a color.
        /// </summary>
        /// <param name="x">The x-coordinate of the center</param>
        /// <param name="y">The y-coordinate of the center</param>
        /// <param name="radius">The radius of this circle</param>
        /// <param name="color">The color of the circle</param>
        public static void FillCircle(int x, int y, int radius, Color color)
        {
            if (spriteBatch == null)
                return;

            Circle circle = new Circle(x, y, radius, true);
            Texture2D texture = null;
            Rectangle rect = new Rectangle(x - radius, y - radius, 2 * radius + 1, 2 * radius + 1);

            if (circleTextures.ContainsKey(circle))
                texture = circleTextures[circle];
            else
            {
                texture = new Texture2D(State.Device, rect.Width, rect.Height, 1, TextureUsage.None,
                    SurfaceFormat.Bgra5551);

                ushort[] data = new ushort[rect.Width * rect.Height];

                int i = 0;
                int j = radius;
                int d = 3 - 2 * radius;
                while (i < j)
                {
                    FillPoints(data, i, j, rect.Width, radius);
                    if (d < 0)
                        d = d + 4 * i + 6;
                    else
                    {
                        d = d + 4 * (i - j) + 10;
                        j--;
                    }
                    i++;
                }
                if (i == j)
                    FillPoints(data, i, j, rect.Width, radius);

                texture.SetData(data);
                circleTextures.Add(circle, texture);
            }

            // Start rendering with alpha blending mode, and render back to front
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.BackToFront, SaveStateMode.None);

            spriteBatch.Draw(texture, rect, color);

            // Finish rendering
            spriteBatch.End();
        }

        /// <summary>
        /// Fills a circlular region centered at the 'center' point with the specified 'radius' and a 'color'.
        /// </summary>
        /// <param name="center">The center point of the circle</param>
        /// <param name="radius">The radius of this circle</param>
        /// <param name="color">The color of the circle</param>
        public static void FillCircle(Point center, int radius, Color color)
        {
            FillCircle(center.X, center.Y, radius, color);
        }
        #endregion

        #region Public Static Text Writing Methods
        /// <summary>
        /// Draws a 2D text string on the screen
        /// </summary>
        /// <param name="pos">Position, in screen coordinates, of the upper left corner of 
        /// the first character</param>
        /// <param name="text">Text to be rendered on the screen</param>
        /// <param name="color">Color of the text</param>
        /// <param name="font">SpriteFont that defines the font family, style, size, etc</param>
        public static void WriteText(Vector2 pos, String text, Color color, SpriteFont font)
        {
            WriteText(pos, text, color, font, Vector2.One, SpriteEffects.None, Vector2.Zero,
                0, 0, GoblinEnums.HorizontalAlignment.None, GoblinEnums.VerticalAlignment.None);
        }

        /// <summary>
        /// Draws a 2D text string on the screen
        /// </summary>
        /// <param name="pos">Position, in screen coordinates, of the upper left corner of 
        /// the first character</param>
        /// <param name="text">Text to be rendered on the screen</param>
        /// <param name="color">Color of the text</param>
        /// <param name="font">SpriteFont that defines the font family, style, size, etc</param>
        /// <param name="scale">Scale to apply to the font</param>
        public static void WriteText(Vector2 pos, String text, Color color, SpriteFont font, Vector2 scale)
        {
            WriteText(pos, text, color, font, scale, SpriteEffects.None, Vector2.Zero,
                0, 0, GoblinEnums.HorizontalAlignment.None, GoblinEnums.VerticalAlignment.None);
        }

        /// <summary>
        /// Draws a 2D text string on the screen
        /// </summary>
        /// <param name="pos">Position, in screen coordinates, of the upper-left corner of 
        /// the first character</param>
        /// <param name="text">Text to be rendered on the screen</param>
        /// <param name="color">Color of the text</param>
        /// <param name="font">SpriteFont that defines the font family, style, size, etc</param>
        /// <param name="xAlign">Horizontal axis alignment of the text string (If set to other than None,
        /// then the X coordinate information of the 'pos' parameter will be ignored)</param>
        /// <param name="yAlign">Vertical axis alignment of the text string (If set to other than None,
        /// then the Y coordinate information of the 'pos' parameter will be ignored)</param>
        public static void WriteText(Vector2 pos, String text, Color color, SpriteFont font,
            GoblinEnums.HorizontalAlignment xAlign, GoblinEnums.VerticalAlignment yAlign)
        {
            WriteText(pos, text, color, font, Vector2.One, SpriteEffects.None, Vector2.Zero, 
                0, 0, xAlign, yAlign);
        }

        /// <summary>
        /// Draws a 2D text string on the screen
        /// </summary>
        /// <param name="pos">Position, in screen coordinates, of the upper-left corner of 
        /// the first character</param>
        /// <param name="text">Text to be rendered on the screen</param>
        /// <param name="color">Color of the text</param>
        /// <param name="font">SpriteFont that defines the font family, style, size, etc</param>
        /// <param name="scale">Scale to apply to the font</param>
        /// <param name="xAlign">Horizontal axis alignment of the text string (If set to other than None,
        /// then the X coordinate information of the 'pos' parameter will be ignored)</param>
        /// <param name="yAlign">Vertical axis alignment of the text string (If set to other than None,
        /// then the Y coordinate information of the 'pos' parameter will be ignored)</param>
        public static void WriteText(Vector2 pos, String text, Color color, SpriteFont font, Vector2 scale,
            GoblinEnums.HorizontalAlignment xAlign, GoblinEnums.VerticalAlignment yAlign)
        {
            WriteText(pos, text, color, font, scale, SpriteEffects.None, Vector2.Zero, 0, 0, xAlign, yAlign);
        }

        /// <summary>
        /// Draws a 2D text string on the screen
        /// </summary>
        /// <param name="pos">Position, in screen coordinates, of the left upper corner of 
        /// the first character</param>
        /// <param name="text">Text to be rendered on the screen</param>
        /// <param name="color">Color of the text</param>
        /// <param name="font">SpriteFont that defines the font family, style, size, etc</param>
        /// <param name="scale">Scale to apply to the font</param>
        /// <param name="effect">Rotations to apply prior to rendering</param>
        /// <param name="origin">Origin of the text. Specify (0, 0) for the upper-left corner</param>
        /// <param name="rotation">Angle, in radians, to rotate the text around origin</param>
        /// <param name="depth">Sorting depth of the sprite text, between 0(front) and 1(back)</param>
        /// <param name="xAlign">Horizontal axis alignment of the text string (If set to other than None,
        /// then the X coordinate information of the 'pos' parameter will be ignored)</param>
        /// <param name="yAlign">Vertical axis alignment of the text string (If set to other than None,
        /// then the Y coordinate information of the 'pos' parameter will be ignored)</param>
        public static void WriteText(Vector2 pos, String text, Color color, SpriteFont font,
            Vector2 scale, SpriteEffects effect, Vector2 origin, float rotation, float depth,
            GoblinEnums.HorizontalAlignment xAlign, GoblinEnums.VerticalAlignment yAlign)
        {
            Vector2 finalPos = new Vector2(pos.X, pos.Y);
            switch (xAlign)
            {
                case GoblinEnums.HorizontalAlignment.Left:
                    finalPos.X = origin.X;
                    break;
                case GoblinEnums.HorizontalAlignment.Center:
                    finalPos.X = (State.Graphics.PreferredBackBufferWidth -
                        font.MeasureString(text).X * scale.X) / 2 + origin.X;
                    break;
                case GoblinEnums.HorizontalAlignment.Right:
                    finalPos.X = State.Graphics.PreferredBackBufferWidth -
                        font.MeasureString(text).X * scale.X + origin.X;
                    break;
            }
            switch (yAlign)
            {
                case GoblinEnums.VerticalAlignment.Top:
                    finalPos.Y = origin.Y;
                    break;
                case GoblinEnums.VerticalAlignment.Center:
                    finalPos.Y = (State.Graphics.PreferredBackBufferHeight -
                        font.MeasureString(text).Y * scale.Y) / 2 + origin.Y;
                    break;
                case GoblinEnums.VerticalAlignment.Bottom:
                    finalPos.Y = State.Graphics.PreferredBackBufferHeight -
                        font.MeasureString(text).Y * scale.Y + origin.Y;
                    break;
            }

            spriteBatch.Begin();

            spriteBatch.DrawString(font, text, finalPos, color, rotation, origin, scale, effect, depth);

            spriteBatch.End();
        }
        #endregion

        #region Helper Methods
        private static void CirclePoints(ushort[] data, int x, int y, int width, int radius)
        {
            int negX = radius - x;
            int posX = x + radius;
            int negY = radius - y;
            int posY = y + radius;
            data[posY * width + posX] = ushort.MaxValue;
            data[posX * width + posY] = ushort.MaxValue;
            data[negX * width + posY] = ushort.MaxValue;
            data[negY * width + posX] = ushort.MaxValue;
            data[negY * width + negX] = ushort.MaxValue;
            data[negX * width + negY] = ushort.MaxValue;
            data[posX * width + negY] = ushort.MaxValue;
            data[posY * width + negX] = ushort.MaxValue;
        }

        private static void FillPoints(ushort[] data, int x, int y, int width, int radius)
        {
            int negX = radius - x;
            int posX = x + radius;
            int negY = radius - y;
            int posY = y + radius;
            data[posY * width + posX] = ushort.MaxValue;
            data[posX * width + posY] = ushort.MaxValue;
            data[negX * width + posY] = ushort.MaxValue;
            data[negY * width + posX] = ushort.MaxValue;
            data[negY * width + negX] = ushort.MaxValue;
            data[negX * width + negY] = ushort.MaxValue;
            data[posX * width + negY] = ushort.MaxValue;
            data[posY * width + negX] = ushort.MaxValue;

            int start = (negY + 1) * width;
            int end = (posY - 1) * width;
            for (int i = start; i <= end; i += width)
            {
                data[i + posX] = ushort.MaxValue;
                data[i + negX] = ushort.MaxValue;
            }

            start = (negX + 1) * width;
            end = (posX - 1) * width;
            for (int i = start; i <= end; i += width)
            {
                data[i + posY] = ushort.MaxValue;
                data[i + negY] = ushort.MaxValue;
            }
        }
        #endregion

        #region Dispose
        internal static void Dispose()
        {
            foreach (Texture2D texture in circleTextures.Values)
                texture.Dispose();

            circleTextures.Clear();
        }
        #endregion

        #region Private Classes
        private class Circle
        {
            public Point Center;
            public float Radius;
            public bool Fill;

            public Circle(Point center, float radius, bool fill)
            {
                Center = center;
                Radius = radius;
                Fill = fill;
            }

            public Circle(int x, int y, float radius, bool fill)
            {
                Center = new Point(x, y);
                Radius = radius;
                Fill = fill;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Circle))
                    return false;
                else
                {
                    Circle c = (Circle)obj;
                    return (Center.X == c.Center.X) && (Center.Y == c.Center.Y) && (Radius == c.Radius)
                        && (Fill == c.Fill);
                }
            }

            public override int GetHashCode()
            {
                return Center.GetHashCode() | Radius.GetHashCode() | Fill.GetHashCode();
            }
        }
        #endregion
    }
}
