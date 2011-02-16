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
 * Authors: Ohan Oda (ohan@cs.columbia.edu) 
 * 
 *************************************************************************************/

#include <stdlib.h>
#include <vector>
#include <map>
#include "MarkerDetector.h"
#include "MultiMarker.h"
#include "MultiMarkerEx.h"

using namespace std;
using namespace alvar;

Camera cam;
int cam_width;
int cam_height;
MarkerDetector<MarkerData> markerDetector;
MarkerDetector<MarkerData> markerDetector2;
vector<MultiMarker> multiMarkers;
IplImage image;
IplImage *hide_texture;
unsigned int hide_texture_size;
unsigned int channels;
double margin;
map<int, int> idTable;
map<int, int>::const_iterator foundPtr;
vector<int> foundMarkers;
bool detect_additional;
double curMaxTrackError;
int detector_id;

// Used for camera calibration
ProjPoints pp;
bool calibration_started;

// Used for automatic configuration generation (marker bundle)
MultiMarkerInitializer *multi_marker_init;
MultiMarkerBundle *multi_marker_bundle;
Pose bundle_pose;
bool bundle_initialized;

extern "C"
{
	__declspec(dllexport) int alvar_init_camera(char* calibFile, int width, int height)
	{
		int ret = -1;
		if((calibFile != NULL) && cam.SetCalib(calibFile, width, height))
			ret = 0;
		else
			cam.SetRes(width, height);

		cam_width = width;
		cam_height = height;
		detector_id = 0;

		detect_additional = false;
		bundle_initialized = false;
		calibration_started = false;

		return ret;
	}

	__declspec(dllexport) void alvar_get_camera_params(double* projMat, double* fovX, double* fovY)
	{
		cam.GetOpenglProjectionMatrix(projMat, cam_width, cam_height);

		*fovX = cam.GetFovX();
		*fovY = cam.GetFovY();
	}

	__declspec(dllexport) void alvar_init_marker_detector(double markerSize, int markerRes = 5, double margin = 2)
	{
		markerDetector.SetMarkerSize(markerSize, markerRes, margin);
		markerDetector2.SetMarkerSize(markerSize, markerRes, margin);
	}

	__declspec(dllexport) void alvar_set_detect_additional(bool enable)
	{
		detect_additional = enable;
	}

	__declspec(dllexport) void alvar_set_marker_size(int id, double markerSize)
	{
		markerDetector.SetMarkerSizeForId(id, markerSize);
	}

	__declspec(dllexport) void alvar_set_hide_texture_configuration(unsigned int size, unsigned int depth, 
		unsigned int _channels, double _margin)
	{
		hide_texture = cvCreateImage(cvSize(size, size), depth, _channels);
		hide_texture_size = size * size * _channels;
		channels = _channels;
		margin = _margin;
	}

	__declspec(dllexport) void alvar_select_detector(int _detector_id)
	{
		detector_id = _detector_id;
	}

	__declspec(dllexport) void alvar_add_multi_marker(int num_ids, int* ids, char* filename)
	{
		vector<int> vector_id;
		for(int i = 0; i < num_ids; i++)
			vector_id.push_back(ids[i]);

		MultiMarker marker(vector_id);
		if(strstr(filename, ".xml") != NULL)
			marker.Load(filename, FILE_FORMAT_XML);
		else
			marker.Load(filename);
		multiMarkers.push_back(marker);
	}

	__declspec(dllexport) void alvar_detect_marker(int nChannels, char* colorModel, char* channelSeq,
		char* imageData, int* interestedMarkerIDs, int* numFoundMarkers, int* numInterestedMarkers,
		double maxMarkerError = 0.08, double maxTrackError = 0.2)
	{
		image.nSize = sizeof(IplImage);
		image.ID = 0;
		image.nChannels = nChannels;
		image.alphaChannel = 0;
		image.depth = IPL_DEPTH_8U;

		memcpy(&image.colorModel, colorModel, sizeof(char) * 4);
		memcpy(&image.channelSeq, channelSeq, sizeof(char) * 4);
		image.dataOrder = 0;

		image.origin = 0;
		image.align = 4;
		image.width = cam_width;
		image.height = cam_height;

		image.roi = NULL;
		image.maskROI = NULL;
		image.imageId = NULL;
		image.tileInfo = NULL;
		image.widthStep = cam_width * nChannels;
		image.imageSize = cam_height * image.widthStep;

		image.imageData = imageData;
		image.imageDataOrigin = NULL;

		if(detector_id == 0)
			markerDetector.Detect(&image, &cam, true, false, maxMarkerError, maxTrackError);
		else
			markerDetector2.Detect(&image, &cam, true, false, maxMarkerError, maxTrackError);
		curMaxTrackError = maxTrackError;
		if(detector_id == 0)
			*numFoundMarkers = markerDetector.markers->size();
		else
			*numFoundMarkers = markerDetector2.markers->size();

		int interestedMarkerNum = *numInterestedMarkers;
		int markerCount = 0;
		int tmpID = 0;
		foundMarkers.clear();
		int size = (detector_id == 0) ? markerDetector.markers->size() : markerDetector2.markers->size();
		if(size > 0 && interestedMarkerNum > 0)
		{
			idTable.clear();
			for(int i = 0; i < size; ++i)
			{
				if(detector_id == 0)
					tmpID = (*(markerDetector.markers))[i].GetId();
				else
					tmpID = (*(markerDetector2.markers))[i].GetId();
				idTable[tmpID] = i;
			}

			for(int i = 0; i < interestedMarkerNum; ++i)
			{
				foundPtr = idTable.find(interestedMarkerIDs[i]);
				if(foundPtr != idTable.end())
				{
					foundMarkers.push_back(foundPtr->second);
					markerCount++;
				}
			}
		}

		*numInterestedMarkers = markerCount;
	}

	__declspec(dllexport) void alvar_get_poses(int* ids, double* poseMats, bool returnHideTextures,
		unsigned char* hideTextures)
	{
		int size = foundMarkers.size();
		if(size == 0)
			return;

		double mat[16];
		int textureIndex = 0;
		for(size_t i = 0; i < foundMarkers.size(); ++i)
		{
			Pose p;
			if(detector_id == 0)
			{
				ids[i] = (*(markerDetector.markers))[foundMarkers[i]].GetId();
				p = (*(markerDetector.markers))[foundMarkers[i]].pose;
			}
			else
			{
				ids[i] = (*(markerDetector2.markers))[foundMarkers[i]].GetId();
				p = (*(markerDetector2.markers))[foundMarkers[i]].pose;
			}
			p.GetMatrixGL(mat);
			memcpy(poseMats + i * 16, &mat, sizeof(double) * 16);

			if(returnHideTextures)
			{
				BuildHideTexture(&image, hide_texture, &cam, mat, PointDouble(-margin, -margin),
					PointDouble(margin, margin));
				for(int j = 0; j < hide_texture_size; j += channels, textureIndex += channels)
				{
					hideTextures[textureIndex] = hide_texture->imageData[j];
					hideTextures[textureIndex + 1] = hide_texture->imageData[j + 1];
					hideTextures[textureIndex + 2] = hide_texture->imageData[j + 2];

					if(channels == 4)
						hideTextures[textureIndex + 3] = hide_texture->imageData[j + 3];
				}
			}
		}
	}

	__declspec(dllexport) void alvar_get_multi_marker_poses(int* ids, double* poseMats, double* errors, 
		bool returnHideTextures, unsigned char* hideTextures)
	{
		int size = (detector_id == 0) ? markerDetector.markers->size() : markerDetector2.markers->size();
		if(size == 0)
			return;

		double mat[16];
		int textureIndex = 0;
		for(int i = 0; i < multiMarkers.size(); ++i)
		{
			ids[i] = i;
			Pose pose;

			if(detect_additional)
			{
				errors[i] = multiMarkers.at(i).Update(markerDetector.markers, &cam, pose);
				multiMarkers.at(i).SetTrackMarkers(markerDetector, &cam, pose);
				markerDetector.DetectAdditional(&image, &cam, false, curMaxTrackError);
			}

			if(detector_id == 0)	
				errors[i] = multiMarkers.at(i).Update(markerDetector.markers, &cam, pose);
			else
				errors[i] = multiMarkers.at(i).Update(markerDetector2.markers, &cam, pose);
			pose.GetMatrixGL(mat);
			memcpy(poseMats + i * 16, &mat, sizeof(double) * 16);

			if(returnHideTextures)
			{
				BuildHideTexture(&image, hide_texture, &cam, mat, PointDouble(-margin, -margin),
					PointDouble(margin, margin));
				for(int j = 0; j < hide_texture_size; j += channels, textureIndex += channels)
				{
					hideTextures[textureIndex] = hide_texture->imageData[j + 2];
					hideTextures[textureIndex + 1] = hide_texture->imageData[j + 1];
					hideTextures[textureIndex + 2] = hide_texture->imageData[j];

					if(channels == 4)
						hideTextures[textureIndex + 3] = hide_texture->imageData[j + 3];
				}
			}
		}
	}

	__declspec(dllexport) bool alvar_calibrate_camera(int nChannels, char* colorModel, char* channelSeq,
		char* imageData, double etalon_square_size, int etalon_rows, int etalon_columns)
	{
		image.nSize = sizeof(IplImage);
		image.ID = 0;
		image.nChannels = nChannels;
		image.alphaChannel = 0;
		image.depth = IPL_DEPTH_8U;

		memcpy(&image.colorModel, colorModel, sizeof(char) * 4);
		memcpy(&image.channelSeq, channelSeq, sizeof(char) * 4);
		image.dataOrder = 0;

		image.origin = 0;
		image.align = 4;
		image.width = cam_width;
		image.height = cam_height;

		image.roi = NULL;
		image.maskROI = NULL;
		image.imageId = NULL;
		image.tileInfo = NULL;
		image.widthStep = cam_width * nChannels;
		image.imageSize = cam_height * image.widthStep;

		image.imageData = imageData;
		image.imageDataOrigin = NULL;

		bool ret = pp.AddPointsUsingChessboard(&image, etalon_square_size, etalon_rows, etalon_columns, false);
		if(ret)
			calibration_started = true;
		return ret;
	}

	__declspec(dllexport) bool alvar_finalize_calibration(char* calibrationFilename)
	{
		if(!calibration_started)
			return false;

		cam.Calibrate(pp);
		pp.Reset();
	
		bool ret = cam.SaveCalib(calibrationFilename);
		if(ret)
			calibration_started = false;
		return ret;
	}

	/*__declspec(dllexport) void alvar_bundle_init(int num_ids, int* ids, double marker_size,
		int min_num_images, int max_num_images)
	{
		if(bundle_initialized)
			return;

		vector<int> vector_id;
		for(int i = 0; i < num_ids; i++)
			vector_id.push_back(ids[i]);

		multi_marker_init = new MultiMarkerInitializer(vector_id, min_num_images, max_num_images);
		bundle_pose.Reset();
		multi_marker_init->PointCloudAdd(vector_id[0], marker_size, bundle_pose);
		multi_marker_bundle = new MultiMarkerBundle(vector_id);

		bundle_initialized = true;
	}

	__declspec(dllexport) void alvar_bundle_add_measurement()
	{
	}*/
}