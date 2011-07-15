using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;

namespace MarkerLayout
{
    /// <summary>
    /// This program demonstrates how to use MarkerLayout class to generate marker array
    /// images and configuration files automatically.
    /// </summary>
    public class SampleProgram
    {
        static void Main(string[] args)
        {
            GenerateALVARLayout();
            //GenerateFromXML();
        }

        /// <summary>
        /// Generates a marker layout image and configuration file to be used with ALVAR
        /// tracking library.
        /// </summary>
        public static void GenerateALVARLayout()
        {
            // Create a layout manager with size 400x400 pixels and 10 pixels inch (40x40 inches)
            LayoutManager layout = new LayoutManager(400, 400, 10);

            // Begin a coordinate frame (ALVAR does not need name or min_points)
            layout.BeginCoordframe("");

            // Create arrays of marker IDs we want to layout
            // NOTE: Please use the SampleMarkerCreator project that comes with the ALVAR
            // package to generate the raw marker images
            int[] array1 = { 0, 1 };
            int[] array2 = { 2, 3 };

            int[][] marker_arrays = new int[2][];
            marker_arrays[0] = array1;
            marker_arrays[1] = array2;

            // Layout the markers
            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < 2; i++)
                    layout.AddMarker(marker_arrays[j][i], new Point(60 + j * 172, 60 + i * 172),
                        "raw_markers/ALVAR/MarkerData_" + marker_arrays[j][i] + ".png");
            }

            // End "ground" coordinate frame
            layout.EndCoordframe();

            // Set the (0, 0) point in the configuration file to be at (60, 60) in the layout image
            // In this case, it is at the left-upper corner of marker ID 0. 
            layout.ConfigCenter = new Point(60, 60);

            // Compile the layout
            layout.Compile();

            // Output the layout image in gif format
            layout.OutputImage("ALVARArray.gif", ImageFormat.Gif);

            // Output the configuration file
            layout.OutputConfig("ALVARConfig.xml", LayoutManager.ConfigType.ALVAR);

            // Disposes the layout
            layout.Dispose();
        }

        /// <summary>
        /// Generates the same marker layout in GenerateALVARLayout using an XML file.
        /// </summary>
        public static void GenerateFromXML()
        {
            // Create a layout manager from an XML file
            LayoutManager layout = new LayoutManager("SampleLayout.xml");

            // Compile the layout
            layout.Compile();

            // Output the layout image in gif format
            layout.OutputImage("ALVARArrayFromXML.gif", ImageFormat.Gif);

            // Output the configuration file
            layout.OutputConfig("ALVARConfigFromXML.xml", LayoutManager.ConfigType.ALVAR);

            // Disposes the layout
            layout.Dispose();
        }
    }
}
