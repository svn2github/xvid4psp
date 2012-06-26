/*
 * Avisynth DLL definitions
 * MobileHackerz http://www.nurs.or.jp/~calcium/
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */
#ifndef AVISYNTHWRAPPER_H
#define AVISYNTHWRAPPER_H

#define ERRMSG_LEN 1024

#define AVS_VARNFOUND 666;    //Var. not found (нет такой переменной)
#define AVS_VARNDEFINED 999;  //Var. not defined (есть, но без значения)
#define AVS_VARWRNGTYPE -999; //Var. wrong type (значение не того типа, что нам нужно)
#define AVS_GERROR -1;        //General (пойманная как AvisynthError)

typedef struct tagSafeStruct
{
	char err[ERRMSG_LEN];
	IScriptEnvironment* env;
	AVSValue* res;
	PClip clp;
	HMODULE dll;
} SafeStruct;

enum
{
	MT_UNDEFINED  = 0,  //Ничего не делать с MT
	MT_DISABLED = 1<<0, //Запретить, добавив SetMTMode(0) перед импортом скрипта
	MT_ADDDISTR = 1<<1  //При MT-режимах добавлять Distributor() после импорта скрипта
};

typedef struct AVSDLLVideoInfo {
	int mt_import;

	// Video
	int width;
	int height;
	int raten;
	int rated;
	int num_frames;
	int field_based;
	int first_field;
	int pixel_type_orig;
	int pixel_type;

	// Audio
	int audio_samples_per_second;
	int sample_type_orig;
	int sample_type;
	int nchannels;
	int64_t num_audio_samples;
} AVSDLLVideoInfo;

#endif /* AVISYNTHWRAPPER_H */
