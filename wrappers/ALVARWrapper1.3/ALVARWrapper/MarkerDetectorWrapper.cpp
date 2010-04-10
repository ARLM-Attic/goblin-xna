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

using namespace std;
using namespace alvar;

Camera cam;
int cam_width;
int cam_height;
MarkerDetector<MarkerData> markerDetector;
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

		detect_additional = false;

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

	__declspec(dllexport) void alvar_add_multi_marker_bundle(int num_ids, int* ids)
	{
		vector<int> vector_id;
		for(int i = 0; i < num_ids; i++)
			vector_id.push_back(ids[i]);
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

		markerDetector.Detect(&image, &cam, true, false, maxMarkerError, maxTrackError);
		curMaxTrackError = maxTrackError;
		*numFoundMarkers = markerDetector.markers->size();

		int interestedMarkerNum = *numInterestedMarkers;
		int markerCount = 0;
		int tmpID = 0;
		foundMarkers.clear();
		if(markerDetector.markers->size() > 0 && interestedMarkerNum > 0)
		{
			idTable.clear();
			for(int i = 0; i < markerDetector.markers->size(); ++i)
			{
				tmpID = (*(markerDetector.markers))[i].GetId();
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
			ids[i] = (*(markerDetector.markers))[foundMarkers[i]].GetId();
			Pose p = (*(markerDetector.markers))[foundMarkers[i]].pose;
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
		int size = markerDetector.markers->size();
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

			errors[i] = multiMarkers.at(i).Update(markerDetector.markers, &cam, pose);
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
}