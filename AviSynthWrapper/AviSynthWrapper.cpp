//27.06.2012:  renamed: "dimzon_avs_getintvariable" to "dimzon_avs_getvariable_i"
//(XviD4PSP5)  added:   "dimzon_avs_getvariable_b", "dimzon_avs_getvariable_f",
//                      "dimzon_avs_getvariable_s", "dimzon_avs_invoke",
//                      "dimzon_avs_isfuncexists" (dimzone.. - only for consistency)
//             modded:  "dimzon_avs_init" (Pixel\SampleType handling, MTMode, ...)
//             removed: "dimzon_avs_init_2"
//
//-------
//
// modified by dimzon, renamed to AvisynthWrapper for futher independent development
// avisynth redirecter dll modified by Inc.
// Original by MobileHackerz http://www.nurs.or.jp/~calcium/

// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <io.h>
#include <fcntl.h>
#include "internal.h"
#include "avisynth.h"
#include <windows.h>
#include <float.h>

typedef __int64 int64_t;
#include "AviSynthWrapper.h"

using namespace std;

extern "C" {
	__declspec(dllexport) int __stdcall dimzon_avs_init(SafeStruct** ppstr, char *func, char *arg, AVSDLLVideoInfo *vi);
	__declspec(dllexport) int __stdcall dimzon_avs_invoke(SafeStruct* pstr, char *func, char **arg, int len, AVSDLLVideoInfo *vi, float* func_out);
	__declspec(dllexport) int __stdcall dimzon_avs_isfuncexists(SafeStruct* pstr, const char *func);
	__declspec(dllexport) int __stdcall dimzon_avs_getlasterror(SafeStruct* pstr, char *str, int len);
	__declspec(dllexport) int __stdcall dimzon_avs_getvframe(SafeStruct* pstr, void *buf, int stride, int frm);
	__declspec(dllexport) int __stdcall dimzon_avs_getaframe(SafeStruct* pstr, void *buf, __int64 start, __int64 count);
	__declspec(dllexport) int __stdcall dimzon_avs_getvariable_b(SafeStruct* pstr, const char* name, bool* result);
	__declspec(dllexport) int __stdcall dimzon_avs_getvariable_i(SafeStruct* pstr, const char* name, int* result);
	__declspec(dllexport) int __stdcall dimzon_avs_getvariable_f(SafeStruct* pstr, const char* name, float* result);
	__declspec(dllexport) int __stdcall dimzon_avs_getvariable_s(SafeStruct* pstr, const char* name, char* result, int len);
	__declspec(dllexport) int __stdcall dimzon_avs_destroy(SafeStruct** ppstr);
}

int __stdcall dimzon_avs_init(SafeStruct** ppstr, char *func, char *arg, AVSDLLVideoInfo *vi)
{
	SafeStruct* pstr = NULL;

	if(!*ppstr)
	{
		pstr = ((SafeStruct*)malloc(sizeof(SafeStruct)));
		*ppstr = pstr;
		memset(pstr, 0, sizeof(SafeStruct));

		pstr->dll = LoadLibrary("avisynth.dll");
		if(!pstr->dll)
		{
			strncpy_s(pstr->err, ERRMSG_LEN, "Cannot load avisynth.dll", _TRUNCATE);
			return 1;
		}

		IScriptEnvironment* (*CreateScriptEnvironment)(int version) = (IScriptEnvironment*(*)(int)) GetProcAddress(pstr->dll, "CreateScriptEnvironment");
		if(!CreateScriptEnvironment)
		{
			strncpy_s(pstr->err, ERRMSG_LEN, "Cannot load CreateScriptEnvironment", _TRUNCATE);
			return 2;
		}

		pstr->env = CreateScriptEnvironment(AVISYNTH_INTERFACE_VERSION);

		if (pstr->env == NULL)
		{
			strncpy_s(pstr->err, ERRMSG_LEN, "Required Avisynth 2.5", _TRUNCATE);
			return 3;
		}
	}
	else
	{
		pstr = *ppstr;
	}

	pstr->err[0] = 0;

	//Заходили только чтоб получить ppstr
	if (!func || strlen(func) == 0 || !arg)
		return 0;

	try
	{
		AVSValue arg(arg);
		AVSValue res;

		if (vi != NULL && vi->mt_import == MT_DISABLED)
		{
			//Если надо, отключаем MT - до импорта
			try { res = pstr->env->Invoke("SetMTMode", 0); }
			catch (IScriptEnvironment::NotFound) { /*AviSynth без MT*/ }
		}

		res = pstr->env->Invoke(func, AVSValue(&arg, 1));
		if(!*ppstr) return 1;

		if (!res.IsClip())
		{
			strncpy_s(pstr->err, ERRMSG_LEN, "The script's return was not a video clip.", _TRUNCATE);
			return 4;
		}

		if (vi != NULL && vi->mt_import == MT_ADDDISTR)
		{
			try
			{
				//Если надо, добавляем Distributor() - после импорта
				AVSValue mt_test = pstr->env->Invoke("GetMTMode", false);
				const int mt_mode = mt_test.IsInt() ? mt_test.AsInt() : 0;
				if (mt_mode > 0 && mt_mode < 5) res = pstr->env->Invoke("Distributor", res);
			}
			catch (IScriptEnvironment::NotFound) { /*AviSynth без MT*/ }

			if (!res.IsClip())
			{
				strncpy_s(pstr->err, ERRMSG_LEN, "After adding \"Distributor()\" the script's return was not a video clip.", _TRUNCATE);
				return 4;
			}
		}

		pstr->clp = res.AsClip();
		VideoInfo inf = pstr->clp->GetVideoInfo();

		if (inf.HasVideo())
		{
			string filter = "";
			string err_string = "";

			//Original и Requested PixelType
			if (vi != NULL) vi->pixel_type_orig = inf.pixel_type;
			int pixel_type_req = (vi != NULL) ? vi->pixel_type : 0;

			if (pixel_type_req == 0) { /*Выводим видео как оно есть, без проверок и преобразований*/ }
			else if (pixel_type_req == inf.CS_BGR32) { if (!inf.IsRGB32()) { filter = "ConvertToRGB32"; err_string = "AviSynthWrapper: Cannot convert video to RGB32!"; }}
			else if (pixel_type_req == inf.CS_BGR24) { if (!inf.IsRGB24()) { filter = "ConvertToRGB24"; err_string = "AviSynthWrapper: Cannot convert video to RGB24!"; }}
			else if (pixel_type_req == inf.CS_YUY2) { if (!inf.IsYUY2()) { filter = "ConvertToYUY2"; err_string = "AviSynthWrapper: Cannot convert video to YUY2!"; }}
			else if (pixel_type_req == inf.CS_YV12) { if (!inf.IsYV12()) { filter = "ConvertToYV12"; err_string = "AviSynthWrapper: Cannot convert video to YV12!"; }}
			else if (pixel_type_req == inf.CS_I420) { if (!inf.IsYV12()) { filter = "ConvertToYV12"; err_string = "AviSynthWrapper: Cannot convert video to YV12!"; }}
			else
			{
				//"2.5 Baked API will see all new planar as YV12"
				//YV411, YV24, YV16 и Y8 в IsYV12() определяются как YV12
				strncpy_s(pstr->err, ERRMSG_LEN, "AviSynthWrapper: Requested PixelType isn't valid or such conversion is not yet implemented!", _TRUNCATE);
				return 5;
			}

			if (filter.length() > 0)
			{
				res = pstr->env->Invoke(filter.c_str(), AVSValue(&res, 1));

				pstr->clp = res.AsClip();
				VideoInfo infh = pstr->clp->GetVideoInfo();

				if (pixel_type_req == inf.CS_BGR32 && !infh.IsRGB32() ||
					pixel_type_req == inf.CS_BGR24 && !infh.IsRGB24() ||
					pixel_type_req == inf.CS_YUY2 && !infh.IsYUY2() ||
					pixel_type_req == inf.CS_YV12 && !infh.IsYV12() ||
					pixel_type_req == inf.CS_I420 && !infh.IsYV12())
				{
					strncpy_s(pstr->err, ERRMSG_LEN, err_string.c_str(), _TRUNCATE);
					return 5;
				}
			}
		}

		if (inf.HasAudio())
		{
			string filter = "";
			string err_string = "";

			//Original и Requested SampleType
			if (vi != NULL) vi->sample_type_orig = inf.sample_type;
			int sample_type_req = (vi != NULL) ? vi->sample_type : 0;

			if (sample_type_req == 0) { /*Выводим звук как он есть, без проверок и преобразований*/ }
			else if (sample_type_req == SAMPLE_FLOAT) { if (inf.sample_type != SAMPLE_FLOAT) { filter = "ConvertAudioToFloat"; err_string = "AviSynthWrapper: Cannot convert audio to FLOAT!"; }}
			else if (sample_type_req == SAMPLE_INT32) { if (inf.sample_type != SAMPLE_INT32) { filter = "ConvertAudioTo32bit"; err_string = "AviSynthWrapper: Cannot convert audio to INT32!"; }}
			else if (sample_type_req == SAMPLE_INT24) { if (inf.sample_type != SAMPLE_INT24) { filter = "ConvertAudioTo24bit"; err_string = "AviSynthWrapper: Cannot convert audio to INT24!"; }}
			else if (sample_type_req == SAMPLE_INT16) { if (inf.sample_type != SAMPLE_INT16) { filter = "ConvertAudioTo16bit"; err_string = "AviSynthWrapper: Cannot convert audio to INT16!"; }}
			else if (sample_type_req == SAMPLE_INT8) { if (inf.sample_type != SAMPLE_INT8) { filter = "ConvertAudioTo8bit"; err_string = "AviSynthWrapper: Cannot convert audio to INT8!"; }}
			else
			{
				strncpy_s(pstr->err, ERRMSG_LEN, "AviSynthWrapper: Requested SampleType isn't valid or such conversion is not yet implemented!", _TRUNCATE);
				return 6;
			}

			if (filter.length() > 0)
			{
				res = pstr->env->Invoke(filter.c_str(), res);

				pstr->clp = res.AsClip();
				VideoInfo infh = pstr->clp->GetVideoInfo();

				if (sample_type_req == SAMPLE_FLOAT && infh.sample_type != SAMPLE_FLOAT ||
					sample_type_req == SAMPLE_INT32 && infh.sample_type != SAMPLE_INT32 ||
					sample_type_req == SAMPLE_INT24 && infh.sample_type != SAMPLE_INT24 ||
					sample_type_req == SAMPLE_INT16 && infh.sample_type != SAMPLE_INT16 ||
					sample_type_req == SAMPLE_INT8 && infh.sample_type != SAMPLE_INT8)
				{
					strncpy_s(pstr->err, ERRMSG_LEN, err_string.c_str(), _TRUNCATE);
					return 6;
				}
			}
		}

		inf = pstr->clp->GetVideoInfo();
		if (vi != NULL) {
			vi->width   = inf.width;
			vi->height  = inf.height;
			vi->raten   = inf.fps_numerator;
			vi->rated   = inf.fps_denominator;
			vi->field_based = (inf.IsFieldBased()) ? 1 : 0;
			vi->first_field = (inf.IsTFF()) ? 1 : (inf.IsBFF()) ? 2 : 0;
			vi->num_frames = inf.num_frames;

			//if (vi->pixel_type == 0) vi->pixel_type = inf.pixel_type;
			//if (vi->sample_type == 0) vi->sample_type = inf.sample_type;
			vi->pixel_type = inf.pixel_type;
			vi->sample_type = inf.sample_type;

			vi->audio_samples_per_second = inf.audio_samples_per_second;
			vi->num_audio_samples        = inf.num_audio_samples;
			vi->nchannels                = inf.nchannels;
		}

		//Нужен ли нам вообще этот res?!
		if(pstr->res) delete pstr->res;
		pstr->res = new AVSValue(res);

		pstr->err[0] = 0;
		return 0;
	}
	catch(AvisynthError err)
	{
		strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
		return AVS_GERROR;
	}
}

int __stdcall dimzon_avs_invoke(SafeStruct* pstr, char *func, char **arg, int len, AVSDLLVideoInfo *vi, float* func_out)
{
	try
	{
		*func_out = -FLT_MAX;
		pstr->err[0] = 0;

		const int N = 10;
		int actual_len = 0;

		AVSValue args[N] = { };
		if (len == 0) args[0] = 0;
		else if (len > N) len = N;

		for(int i = 0; i < len; i++)
		{
			if (strlen(arg[i]) > 0)
			{
				string lower = arg[i];
				bool was_letters = false;
				bool was_digits = false;
				bool was_spaces = false;

				//Слишком длинные значения - точно текст
				for (unsigned int n = 0; n < lower.size() && lower.size() <= 10; n++)
				{
					lower[n] = tolower(lower[n]);
					if (!was_letters && isalpha(lower[n])) was_letters = true;
					if (!was_digits && isdigit(lower[n])) was_digits = true;
					if (!was_spaces && isspace(lower[n])) was_spaces = true;
				}

				if (i == 0 && was_letters && !was_digits && !was_spaces && lower.compare("last") == 0)
				{
					//Clip (last)
					if(!pstr->clp) throw AvisynthError("AviSynthWrapper: The \"last\" clip was requested, but it doesn't exist!");
					args[actual_len] = pstr->clp; //pstr->res->AsClip();
					actual_len += 1;

					//pstr->clp; pstr->res->AsClip(); //С обработкой после прошлых вызовов Invoke
					//pstr->env->GetVar("last").AsClip(); //"Чистый" выход скрипта
				}
				else if (was_letters && !was_digits && !was_spaces && lower.compare("true") == 0)
				{
					//Bool (true)
					args[actual_len] = true;
					actual_len += 1;
				}
				else if (was_letters && !was_digits && !was_spaces && lower.compare("false") == 0)
				{
					//Bool (false)
					args[actual_len] = false;
					actual_len += 1;
				}
				else if (!was_letters && was_digits && !was_spaces && lower.find(".") != string::npos)
				{
					//Float (double..)
					args[actual_len] = atof(arg[i]);
					actual_len += 1;
				}
				else if (!was_letters && was_digits && !was_spaces)
				{
					//Integer
					args[actual_len] = atoi(arg[i]);
					actual_len += 1;
				}
				else
				{
					//String
					args[actual_len] = arg[i];
					actual_len += 1;
				}
			}
		}

		AVSValue res = pstr->env->Invoke(func, AVSValue(args, actual_len));

		if (!res.IsClip())
		{
			//Вывод результата
			if (res.IsBool())
			{ 
				if(!res.AsBool()) *func_out = 0;
				else *func_out = FLT_MAX;
			}
			else if (res.IsInt()) *func_out = (float)res.AsInt();
			else if (res.IsFloat()) *func_out = (float)res.AsFloat();
			else if (res.IsString()) { *func_out = FLT_MAX; strncpy_s(pstr->err, ERRMSG_LEN, res.AsString(), _TRUNCATE); }
		}
		else
		{
			pstr->clp = res.AsClip();
			VideoInfo inf = pstr->clp->GetVideoInfo();

			if (vi != NULL)
			{
				vi->width   = inf.width;
				vi->height  = inf.height;
				vi->raten   = inf.fps_numerator;
				vi->rated   = inf.fps_denominator;
				vi->field_based = (inf.IsFieldBased()) ? 1 : 0;
				vi->first_field = (inf.IsTFF()) ? 1 : (inf.IsBFF()) ? 2 : 0;
				vi->num_frames = inf.num_frames;

				//Или не меняем?
				if (vi->pixel_type_orig == 0) vi->pixel_type_orig = inf.pixel_type;
				if (vi->sample_type_orig == 0) vi->sample_type_orig = inf.sample_type;

				vi->pixel_type = inf.pixel_type;
				vi->sample_type = inf.sample_type;

				vi->num_audio_samples        = inf.num_audio_samples;
				vi->audio_samples_per_second = inf.audio_samples_per_second;
				vi->nchannels                = inf.nchannels;
			}

			//Нужен ли нам вообще этот res?!
			if(pstr->res) delete pstr->res;
			pstr->res = new AVSValue(res);

			pstr->err[0] = 0;
		}

		return 0;
	}
	catch(AvisynthError err)
	{
		strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
		return AVS_GERROR;
	}
	catch(IScriptEnvironment::NotFound)
	{
		strncpy_s(pstr->err, ERRMSG_LEN, "AviSynthWrapper: Wrong function name or invalid parameters was passed to Invoke!", _TRUNCATE);
		return AVS_VARNFOUND
	}
}

int __stdcall dimzon_avs_isfuncexists(SafeStruct* pstr, const char *func)
{
	pstr->err[0] = 0;
	try
	{
		AVSValue var = pstr->env->FunctionExists(func);
		if (var.IsBool())
		{
			return (var.AsBool()) ? 0 : AVS_VARNFOUND;
		}
		return AVS_VARWRNGTYPE;
	}
	catch(AvisynthError err)
	{
		strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
		return AVS_GERROR;
	}
}

int __stdcall dimzon_avs_getlasterror(SafeStruct* pstr, char *str, int len)
{
	strncpy_s(str, len, pstr->err, len - 1);
	return (int)strlen(str);
}

int __stdcall dimzon_avs_getvframe(SafeStruct* pstr, void *buf, int stride, int frm)
{
	try
	{
		PVideoFrame f = pstr->clp->GetFrame(frm, pstr->env);
		if(buf && stride)
		{
			pstr->env->BitBlt((BYTE*)buf, stride, f->GetReadPtr(), f->GetPitch(), f->GetRowSize(), f->GetHeight());
		}
		pstr->err[0] = 0;
		return 0;
	}
	catch(AvisynthError err)
	{
		strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
		return AVS_GERROR;
	}
}

int __stdcall dimzon_avs_getaframe(SafeStruct* pstr, void *buf, __int64 start, __int64 count)
{
	try
	{
		pstr->clp->GetAudio(buf, start, count, pstr->env);
		pstr->err[0] = 0;
		return 0;
	}
	catch(AvisynthError err)
	{
		strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
		return AVS_GERROR;
	}
}

int __stdcall dimzon_avs_getvariable_b(SafeStruct* pstr, const char* name, bool* result)
{
	try
	{
		pstr->err[0] = 0;
		try
		{
			AVSValue var = pstr->env->GetVar(name);
			if(var.Defined())
			{
				if(!var.IsBool())
				{
					strncpy_s(pstr->err, ERRMSG_LEN, "AviSynthWrapper: Requested variable is not Boolean!", _TRUNCATE);
					return AVS_VARWRNGTYPE;
				}
				*result = var.AsBool();
				return 0;
			}
			return AVS_VARNDEFINED;
		}
		catch(AvisynthError err)
		{
			strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
			return AVS_GERROR;
		}
	}
	catch(IScriptEnvironment::NotFound)
	{
		return AVS_VARNFOUND;
	}
}

int __stdcall dimzon_avs_getvariable_i(SafeStruct* pstr, const char* name, int* result)
{
	try
	{
		pstr->err[0] = 0;
		try
		{
			AVSValue var = pstr->env->GetVar(name);
			if(var.Defined())
			{
				if(!var.IsInt())
				{
					strncpy_s(pstr->err, ERRMSG_LEN, "AviSynthWrapper: Requested variable is not Integer!", _TRUNCATE);
					return AVS_VARWRNGTYPE;
				}
				*result = var.AsInt();
				return 0;
			}
			return AVS_VARNDEFINED;
		}
		catch(AvisynthError err)
		{
			strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
			return AVS_GERROR;
		}
	}
	catch(IScriptEnvironment::NotFound)
	{
		return AVS_VARNFOUND;
	}
}

int __stdcall dimzon_avs_getvariable_f(SafeStruct* pstr, const char* name, float* result)
{
	try
	{
		pstr->err[0] = 0;
		try
		{
			AVSValue var = pstr->env->GetVar(name);
			if(var.Defined())
			{
				if(!var.IsFloat())
				{
					strncpy_s(pstr->err, ERRMSG_LEN, "AviSynthWrapper: Requested variable is not Float!", _TRUNCATE);
					return AVS_VARWRNGTYPE;
				}
				*result = (float)var.AsFloat();
				return 0;
			}
			return AVS_VARNDEFINED;
		}
		catch(AvisynthError err)
		{
			strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
			return AVS_GERROR;
		}
	}
	catch(IScriptEnvironment::NotFound)
	{
		return AVS_VARNFOUND;
	}
}

int __stdcall dimzon_avs_getvariable_s(SafeStruct* pstr, const char* name, char* result, int len)
{
	try
	{
		pstr->err[0] = 0;
		try
		{
			AVSValue var = pstr->env->GetVar(name);
			if(var.Defined())
			{
				if(!var.IsString())
				{
					strncpy_s(pstr->err, ERRMSG_LEN, "AviSynthWrapper: Requested variable is not String!", _TRUNCATE);
					return AVS_VARWRNGTYPE;
				}
				strncpy_s(result, len, var.AsString(), len - 1);
				return 0;
			}
			return AVS_VARNDEFINED;
		}
		catch(AvisynthError err)
		{
			strncpy_s(pstr->err, ERRMSG_LEN, err.msg, _TRUNCATE);
			return AVS_GERROR;
		}
	}
	catch(IScriptEnvironment::NotFound)
	{
		return AVS_VARNFOUND;
	}
}

int __stdcall dimzon_avs_destroy(SafeStruct** ppstr)
{
	if(!ppstr)
	{
		return 0;
	}

	SafeStruct* pstr = *ppstr;
	if(!pstr)
	{
		return 0;
	}

	if(pstr->clp)
	{
		pstr->clp = NULL;
	}

	if(pstr->res)
	{
		delete pstr->res;
		pstr->res = NULL;
	}

	if(pstr->env)
	{
		delete pstr->env;
		pstr->env = NULL;
	}

	if(pstr->dll)
	{
		FreeLibrary(pstr->dll);
	}

	free(pstr);
	*ppstr = NULL;
	return 0;
}
