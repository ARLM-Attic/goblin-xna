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

using GoblinXNA.Helpers;

namespace GoblinXNA
{
    /// <summary>
    /// A helper class for checking performance of certain code blocks.
    /// 
    /// A time performance measure will be either printed on the console or to a file for the
    /// code block enclosed between Start() and Stop(string) method.
    /// </summary>
    public sealed class PerformanceChecker
    {
        private static double startTime;
        /// <summary>
        /// Indicates whether to print on the console window or write to a log file
        /// </summary>
        public static bool Print;

        static PerformanceChecker()
        {
            startTime = 0;
            Print = true;
        }

        /// <summary>
        /// Starts the performance measurement.
        /// </summary>
        public static void Start()
        {
            startTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
        }

        /// <summary>
        /// Stops the performance measurement.
        /// </summary>
        /// <param name="identifier">A prefix id string added at the beginning of the printed message</param>
        public static void Stop(String identifier)
        {
            double stopTime = DateTime.Now.TimeOfDay.TotalMilliseconds;
            if (Print)
                Console.WriteLine(identifier + " - Timespan: " + (stopTime - startTime));
            else
                Log.Write(identifier + " - Timespan: " + (stopTime - startTime), Log.LogLevel.Log);
        }

        public static double Stop()
        {
            return DateTime.Now.TimeOfDay.TotalMilliseconds - startTime;
        }
    }
}
