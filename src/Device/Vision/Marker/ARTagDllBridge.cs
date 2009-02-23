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
 * Author: Mark Fiala
 * 
 *************************************************************************************/

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace GoblinXNA.Device.Vision.Marker
{
    /// <summary>
    /// A DLL bridge class that accesses the methods in ARTagWrapper.dll, which contains
    /// wrapped methods from the original ARTag marker tracker library.
    /// </summary>
    public class ARTagDllBridge
    {
        #region ARTag Function Import

        // Import the Win32 DLL sample function
        [DllImport("ARTagWrapper.dll", EntryPoint = "fnARTagWrapper")]
        public static extern int fnARTagWrapper();

        // ARTag Wrapper: ARTag Init
        [DllImport("ARTagWrapper.dll", EntryPoint = "init_artag_wrapped")]
        public static extern char init_artag_wrapped(int width, int height, int bpp);

        // ARTag Wrapper: Glut Init
        //[DllImport("ARTagWrapper.dll", EntryPoint = "init_glut_wrapped")]
        //public static extern void init_glut_wrapped(int pos_x, int pos_y);

        // ARTag Wrapper: ARTag Close
        [DllImport("ARTagWrapper.dll", EntryPoint = "close_artag_wrapped")]
        public static extern void close_artag_wrapped();

        // ARTag Wrapper: load_array_file
        // int load_array_file(char* filename);   //returns -1 if file not found
        [DllImport("ARTagWrapper.dll", EntryPoint = "load_array_file_wrapped")]
        public static extern int load_array_file_wrapped(string filename);

        // ARTag Wrapper: artag_associate_array
        //-associate an array with an object, this function will return an ID to use in future
        //calls.  "frame_name" is the same as in the array .cf file that must be loaded first.
        //-if the return value is -1, the object could not be initialized
        //example: base_artag_object_id=artag_associate_array("base0");
        //int artag_associate_array(char* frame_name);  //returns artag_object_id
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_associate_array_wrapped")]
        public static extern int artag_associate_array_wrapped(string frame_name);

        // ARTag Wrapper: artag_associate_marker
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_associate_marker_wrapped")]
        public static extern int artag_associate_marker_wrapped(int artag_id);

        // ARTag Wrapper: artag_find_objects
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_find_objects_wrapped")]
        public static extern int artag_find_objects_wrapped([In, Out] IntPtr rgb_cam_image,
            char rgb_greybar);

        // ARTag Wrapper: artag_is_object_found
        // artag_is_object_found(artag_object_num) returns 1 if object was found from most
        // recent artag_find_objects() call, returns 0 if object was not found 
        // char artag_is_object_found(int artag_object_num);
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_is_object_found_wrapped")]
        public static extern char artag_is_object_found_wrapped(int artag_object_num);

        // ARTag Wrapper: artag_set_object_opengl_matrix
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_set_object_opengl_matrix_wrapped")]
        public static extern void artag_set_object_opengl_matrix_wrapped(int object_num, char mirror_on);

        // ARTag Wrapper: artag_get_object_matrix
        //[DllImport("ARTagWrapper.dll", EntryPoint = "artag_get_object_matrix_wrapped")]
        //public static extern IntPtr artag_get_object_matrix_wrapped();

        // ARTag Wrapper: artag_set_camera_params_wrapped
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_set_camera_params_wrapped")]
        public static extern void artag_set_camera_params_wrapped(double _camera_fx, double _camera_fy,
            double _camera_cx, double _camera_cy);

        // ARTag Wrapper: artag_create_marker
        //artag_create_marker() will fill an unsigned char array with 100*scale*scale bytes
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_create_marker_wrapped")]
        public static extern int artag_create_marker_wrapped(int artag_id, int scale,
            [In][Out] ref IntPtr image);

        // ARTag Wrapper: 
        //void artag_set_output_image_mode(void);     //turn on output debug image writing 
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_set_output_image_mode_wrapped")]
        public static extern void artag_set_output_image_mode_wrapped();

        // ARTag Wrapper: 
        //void artag_clear_output_image_mode(void);   //turn off output debug image writing
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_clear_output_image_mode_wrapped")]
        public static extern void artag_clear_output_image_mode_wrapped();

        // ARTag Utils

        // ARTag Wrapper: write_pgm
        //void write_pgm(char *file_name, char *comment, unsigned char *image,int width,int height)
        [DllImport("ARTagWrapper.dll", EntryPoint = "write_pgm_wrapped")]
        public static extern void write_pgm_wrapped(string file_name, string comment, [In] IntPtr image,
            int width, int height);

        // ARTag Wrapper: write_ppm
        //void write_ppm_wrapped(char *file_name, char *comment, unsigned char *image,int width,int height)
        [DllImport("ARTagWrapper.dll", EntryPoint = "write_ppm_wrapped")]
        public static extern void write_ppm_wrapped(string file_name, string comment, [In] IntPtr image,
            int width, int height);

        // ARTag Wrapper: read_ppm_wrapped
        //unsigned char* read_ppm_wrapped(char *file_name, int *width, int *height)
        [DllImport("ARTagWrapper.dll", EntryPoint = "read_ppm_wrapped")]
        public static extern IntPtr read_ppm_wrapped(string file_name, ref int width, ref int height);

        //-find what an X,Y,Z in an object maps to in camera image coordinates (NOT! screen
        //coordinates if the screen is a different resolution as typically with OpenGL)
        //-only call this if artag_is_object_found(artag_object_num) returned 1 in this image frame
        //void artag_project_point(int object_num, float x, float y, float z, float* u, float* v);
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_project_point")]
        public static extern void artag_project_point(int object_num, float x, float y, float z,
            [In] IntPtr u, [In] IntPtr v);

        //-find what X,Y,Z in one object maps to in another, useful for interactivity and
        //detecting collisions between objects
        //-only call this if artag_is_object_found(artag_object_num) returned 1 in this image frame
        //void artag_project_between_objects(int object_num1, double source2dest_scale, int object_num2, double x1, double y1, double z1, double* x2, double* y2, double* z2);
        [DllImport("ARTagWrapper.dll", EntryPoint = "artag_project_between_objects")]
        public static extern void artag_project_between_objects(int object_num1, double source2dest_scale,
            int object_num2, double x1, double y1, double z1, [In, Out] Int64 x2, [In, Out] Int64 y2,
            [In, Out] Int64 z2);

        #endregion ARTag Function Import
    }
}
