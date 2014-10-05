//(XviD4PSP5) modded version, 2012-2013
/*
 * Copyright (c) 2004-2008 Mike Matsnev.  All Rights Reserved.
 * 
 * $Id: utils.cpp,v 1.64 2009/12/21 20:22:43 mike Exp $
 * 
 */

#include <windows.h>
#include <tchar.h>
#include <strsafe.h>

#define _ATL_FREE_THREADED
#include <atlstr.h>
#include <atlcom.h>

#include <streams.h>
#include <dshow.h>
#include <dvdmedia.h>
#include <ks.h>
#include <ksmedia.h>
#include <comdef.h>

#include "utils.h"
#include "guids.h"
#include "VideoSink.h"
#include ".\LAVFilters\LAVSplitterSettings.h"
#include ".\LAVFilters\LAVVideoSettings.h"

//Локеры для защиты кода с LoadLibrary от нескольких потоков
static CCritSec csLockForSplitter, csLockForFilter;

CComPtr<IPin> GetPin(IBaseFilter *pF, bool include_connected, PIN_DIRECTION dir, const GUID *pMT)
{
	if (pF == NULL)
		return CComPtr<IPin>();

	ENUM_PINS(pF, pP)
	{
		PIN_DIRECTION pd;
		if (FAILED(pP->QueryDirection(&pd)))
			continue;

		if (pd == dir) //Direction
		{
			if (!include_connected)
			{
				CComPtr<IPin> pQ;
				if (SUCCEEDED(pP->ConnectedTo(&pQ)))
					continue;
			}

			if (pMT == NULL)
				return pP;

			ENUM_MT(pP, MT) //MediaType
			{
				if (MT->majortype == *pMT)
					return pP;
			}

			if (include_connected)
			{
				AM_MEDIA_TYPE MT;
				if (SUCCEEDED(pP->ConnectionMediaType(&MT)))
				{
					BOOL found = (MT.majortype == *pMT);
					MTPtr::FreeMediaType(&MT);
					if (found) return pP;
				}
			}
		}
	}

	return CComPtr<IPin>();
}

HRESULT LoadSplitterFromFile(IFileSourceFilter **pFSource, volatile HMODULE *hModule, const char *subDir, const char *fileName, const GUID CLSID_Filter, char *err, rsize_t err_len)
{
	typedef HRESULT (__stdcall *DllGetClassObjectFunc)(REFCLSID, REFIID, void**);
	DllGetClassObjectFunc pDllGetClassObject;
	HRESULT hr;

	if (!*hModule)
	{
		//Блокировка для других потоков
		CAutoLock cObjectLock(&csLockForSplitter);

		if (!*hModule)
		{
			char OurPath[MAX_PATH], LibPath[MAX_PATH];
			GetModuleFileNameA(_AtlBaseModule.GetModuleInstance(), OurPath, MAX_PATH);  //Полный путь к DSS2
			PathRemoveFileSpecA(OurPath);                                               //Отсекаем имя файла
			PathCombineA(LibPath, OurPath, subDir);                                     //Добавляем подпапку
			PathAppendA(LibPath, fileName);                                             //Добавляем в путь имя нужной нам dll
			*hModule = LoadLibraryExA(LibPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);    //И грузим её (с указанием полного пути, иначе может подгрузиться хз что и хз откуда!)
			if (!*hModule)
			{
				DWORD res = GetLastError();
				_com_error error(HRESULT_FROM_WIN32(res));
				_snprintf_s(err, err_len, _TRUNCATE, "LoadLibrary: (%lu) %s\n", res, error.ErrorMessage());
				return E_FAIL;
			}
		}
	}

	pDllGetClassObject = (DllGetClassObjectFunc)GetProcAddress(*hModule, "DllGetClassObject");
	if (!pDllGetClassObject) { strncpy_s(err, err_len, "DllGetClassObject: ", _TRUNCATE); return E_FAIL; }

	CComPtr<IClassFactory> pCF;
	if (FAILED(hr = pDllGetClassObject(CLSID_Filter, IID_IClassFactory, (void**)&pCF))) {
		strncpy_s(err, err_len, "Get IClassFactory: ", _TRUNCATE); return hr; }

	if (FAILED(hr= pCF->CreateInstance(NULL, IID_IFileSourceFilter, (void**)pFSource))) {
		strncpy_s(err, err_len, "Get IUnknown: ", _TRUNCATE); return hr; }

	return S_OK;
}

HRESULT LoadFilterFromFile(IBaseFilter **pBFilter, volatile HMODULE *hModule, const char *subDir, const char *fileName, const GUID CLSID_Filter, char *err, rsize_t err_len)
{
	typedef HRESULT (__stdcall *DllGetClassObjectFunc)(REFCLSID, REFIID, void**);
	DllGetClassObjectFunc pDllGetClassObject;
	HRESULT hr;

	if (!*hModule)
	{
		//Блокировка для других потоков
		CAutoLock cObjectLock(&csLockForFilter);

		if (!*hModule)
		{
			char OurPath[MAX_PATH], LibPath[MAX_PATH];
			GetModuleFileNameA(_AtlBaseModule.GetModuleInstance(), OurPath, MAX_PATH);  //Полный путь к DSS2
			PathRemoveFileSpecA(OurPath);                                               //Отсекаем имя файла
			PathCombineA(LibPath, OurPath, subDir);                                     //Добавляем подпапку
			PathAppendA(LibPath, fileName);                                             //Добавляем в путь имя нужной нам dll
			*hModule = LoadLibraryExA(LibPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);    //И грузим её (с указанием полного пути, иначе может подгрузиться хз что и хз откуда!)
			if (!*hModule)
			{
				DWORD res = GetLastError();
				_com_error error(HRESULT_FROM_WIN32(res));
				_snprintf_s(err, err_len, _TRUNCATE, "LoadLibrary: (%lu) %s\n", res, error.ErrorMessage());
				return E_FAIL;
			}
		}
	}

	pDllGetClassObject = (DllGetClassObjectFunc)GetProcAddress(*hModule, "DllGetClassObject");
	if (!pDllGetClassObject) { strncpy_s(err, err_len, "DllGetClassObject: ", _TRUNCATE); return E_FAIL; }

	CComPtr<IClassFactory> pCF;
	if (FAILED(hr = pDllGetClassObject(CLSID_Filter, IID_IClassFactory, (void**)&pCF))) {
		strncpy_s(err, err_len, "Get IClassFactory: ", _TRUNCATE); return hr; }

	CComPtr<IUnknown> object;
	if (FAILED(hr = pCF->CreateInstance(NULL, IID_IUnknown, (void**)&object))) {
		strncpy_s(err, err_len, "Get IUnknown: ", _TRUNCATE); return hr; }

	if (FAILED(hr = object->QueryInterface(IID_IBaseFilter, (void**)pBFilter))) {
		strncpy_s(err, err_len, "Get IBaseFilter: ", _TRUNCATE); return hr; }

	return S_OK;
}

void ParseLAVSplitterSettings(LAVSplitterSettings *lss, const char *s)
{
	//Дефолты
	lss->Loading = 3;          //Загружать фильтр: 0 = системный (не задавать настройки), 1 = из подпапки (не задавать настройки), 2 = системный (задать настройки), 3 = из подпапки (задать настройки)
	lss->VC1Fix = 2;           //Исправлять таймкоды для VC-1: 0 = никогда, 1 = всегда, 2 = Auto (only for Decoders that need it)
	lss->SMode = 2;            //Режим субтитров: 0 = NoSubs, 1 = ForcedOnly, 2 = Default, 3 = Advanced
	lss->SLanguage[0] = '\0';  //Строка с кодами языков для автовыбора субтитров
	lss->SAdvanced[0] = '\0';  //Строка с Advanced-настройками для автовыбора субтитров
	lss->ExtSegments = false;  //Загружать linked segments из соседних файлов (для MKV)
	lss->TrayIcon = false;     //Иконка в трее

	const int l_len = sizeof(lss->SLanguage)/sizeof(lss->SLanguage[0]);
	char language[l_len] = {0};
	bool l_args = false;
	int l_index = 0;

	const int a_len = sizeof(lss->SAdvanced)/sizeof(lss->SAdvanced[0]);
	char advanced[a_len] = {0};
	bool a_args = false;
	int a_index = 0;

	for (unsigned int i = 0; i < strlen(s); i++)
	{
		if (!l_args && !a_args)
		{
			char next_ch = (i + 1 < strlen(s)) ? s[i + 1] : 0;
			char nnext_ch = (i + 2 < strlen(s)) ? s[i + 2] : 0;

			//48 = 0, 49 = 1, .. 57 = 9
			int next_i = (next_ch >= 48 && next_ch <= 57) ? next_ch - 48 : -1;
			int nnext_i = (nnext_ch >= 48 && nnext_ch <= 57) ? nnext_ch - 48 : -1;
			
			if ((s[i] == 'l' || s[i] == 'L') && next_i >= 0) //l - Loading (от 0 до 3)
			{
				lss->Loading = min(next_i, 3);
				i += 1;
			}
			else if ((s[i] == 'v' || s[i] == 'V') && (next_ch == 'c' || next_ch == 'C') && nnext_i >= 0) //vc - VC1Fix (от 0 до 2)
			{
				lss->VC1Fix = min(nnext_i, 2);
				i += 2;
			}
			else if ((s[i] == 'e' || s[i] == 'E') && (next_ch == 's' || next_ch == 'S') && nnext_i >= 0) //es - ExtSegments (0 = false, 1+ = true)
			{
				lss->ExtSegments = (nnext_i > 0);
				i += 2;
			}
			else if ((s[i] == 't' || s[i] == 'T') && (next_ch == 'i' || next_ch == 'I') && nnext_i >= 0) //ti - TrayIcon (0 = false, 1+ = true)
			{
				lss->TrayIcon = (nnext_i > 0);
				i += 2;
			}
			else if (s[i] == 's' || s[i] == 'S')
			{
				if ((next_ch == 'm' || next_ch == 'M') && nnext_i >= 0) //sm - SMode (от 0 до 3)
				{
					lss->SMode = min(nnext_i, 3);
					i += 2;
				}
				else if ((next_ch == 'l' || next_ch == 'L') && nnext_ch == '[') //sl - SLanguage (sl[...])
				{
					//Скобка открывается - всё идущее после неё будем сохранять в language
					l_args = true;
					l_index = 0;
					i += 2;
				}
				else if ((next_ch == 'a' || next_ch == 'A') && nnext_ch == '[') //sa - SAdvanced (sa[...])
				{
					//Скобка открывается - всё идущее после неё будем сохранять в advanced
					a_args = true;
					a_index = 0;
					i += 2;
				}
			}
		}
		else
		{
			if (s[i] == ']')
			{
				//Скобка закрывается - переключаемся обратно на парсинг
				l_args = a_args = false;
			}
			else if (l_args && s[i] != '[')
			{
				if (l_index < l_len - 1)
				{
					//Language args
					language[l_index] = s[i];
					l_index += 1;
				}
			}
			else if (a_args && s[i] != '[')
			{
				if (a_index < a_len - 1)
				{
					//Advanced args
					advanced[a_index] = s[i];
					a_index += 1;
				}
			}
		}
	}

	if (l_index > 0 && !l_args)
	{
		language[l_index] = '\0';
		MultiByteToWideChar(CP_ACP, 0, language, -1, lss->SLanguage, l_len);
	}
	if (a_index > 0 && !a_args)
	{
		advanced[a_index] = '\0';
		MultiByteToWideChar(CP_ACP, 0, advanced, -1, lss->SAdvanced, a_len);
	}
}

void ParseLAVVideoSettings(LAVVideoSettings *lvs, const char *s)
{
	//Дефолты
	lvs->Loading = 3;        //Загружать фильтр: 0 = системный (не задавать настройки), 1 = из подпапки (не задавать настройки), 2 = системный (задать настройки), 3 = из подпапки (задать настройки)
	lvs->Threads = 0;        //Кол-во потоков декодирования: 0 = Auto (based on number of CPU cores), 1 = 1 (без MT), x = x
	lvs->Range = 0;          //YUV->RGB уровни: 0 = Auto (same as input), 1 = Limited (16-235), 2 = Full (0-255)
	lvs->Dither = 1;         //Dithering mode при преобразованиях форматов: 0 = Ordered(постоянный), 1 = Random(случайный) паттерны
	lvs->DeintMode = 0;      //Режим деинтерлейса: 0 = Disabled, 1 = Auto, 2 = Auto (aggressive), 3 = Always on
	lvs->FieldOrder = 0;     //Порядок полей при деинтерлейсе: 0 = Auto, 1 = TFF, 2 = BFF
	lvs->SWDeint = 0;        //Софтварный деинтерлейс: 0 = None, 1 = Yadif, 2 = Yadif (x2)
	lvs->WMVDMO = true;      //Использовать MS WMV9 DMO Decoder для декодирования VC-1\WMV3
	lvs->HWMode = 0;         //Хардварное декодирование: 0 = выкл., 1 = CUDA, 2 = QuickSink, 3 = DXVA2 (copy-back)
	lvs->HWCodecs = 7;       //Кодеки, для которых хардварное декодирование будет использовано (если включено)
	lvs->HWRes = 3;          //Разрешения, для которых можно использовать хардварное декодирование
	lvs->HWDeint = 0;        //Хардварный деинтерлейс: 0 = Weave, 1 = Adaptive, 2 = Adaptive (x2)
	lvs->HWDeintHQ = false;  //Хардварный деинтерлейс: HQ-режим (Vista+)
	lvs->TrayIcon = false;   //Иконка в трее

	for (unsigned int i = 0; i < strlen(s); i++)
	{
		char next_ch = (i + 1 < strlen(s)) ? s[i + 1] : 0;
		char nnext_ch = (i + 2 < strlen(s)) ? s[i + 2] : 0;

		//48 = 0, 49 = 1, .. 57 = 9
		int next_i = (next_ch >= 48 && next_ch <= 57) ? next_ch - 48 : -1;
		int nnext_i = (nnext_ch >= 48 && nnext_ch <= 57) ? nnext_ch - 48 : -1;

		if ((s[i] == 'l' || s[i] == 'L') && next_i >= 0) //l - Loading (от 0 до 3)
		{
			lvs->Loading = min(next_i, 3);
			i += 1;
		}
		else if (s[i] == 't' || s[i] == 'T')
		{
			if (next_i >= 0) //t - Threads (от 0 до xx)
			{
				lvs->Threads = next_i;
				if (nnext_i >= 0)
				{
					lvs->Threads *= 10;
					lvs->Threads += nnext_i;
					i += 1;
				}
				i += 1;
			}
			else if ((next_ch == 'i' || next_ch == 'I') && nnext_i >= 0) //ti - TrayIcon (0 = false, 1+ = true)
			{
				lvs->TrayIcon = (nnext_i > 0);
				i += 2;
			}
		}
		else if ((s[i] == 'r' || s[i] == 'R') && next_i >= 0) //r - Range (от 0 до 2)
		{
			lvs->Range = min(next_i, 2);
			i += 1;
		}
		else if ((s[i] == 'd' || s[i] == 'D') && next_i >= 0) //d - Dither (от 0 до 1)
		{
			lvs->Dither = min(next_i, 1);
			i += 1;
		}
		else if ((s[i] == 'v' || s[i] == 'V') && (next_ch == 'c' || next_ch == 'C') && nnext_i >= 0) //vc - WMV9 DMO (0 = false, 1+ = true)
		{
			lvs->WMVDMO = (nnext_i > 0);
			i += 2;
		}
		else if ((s[i] == 'd' || s[i] == 'D') && (next_ch == 'm' || next_ch == 'M') && nnext_i >= 0) //dm - DeintMode (от 0 до 3)
		{
			lvs->DeintMode = min(nnext_i, 3);
			i += 2;
		}
		else if ((s[i] == 'f' || s[i] == 'F') && (next_ch == 'o' || next_ch == 'O') && nnext_i >= 0) //fo - FieldOrder (от 0 до 2)
		{
			lvs->FieldOrder = min(nnext_i, 2);
			i += 2;
		}
		else if ((s[i] == 's' || s[i] == 'S') && (next_ch == 'd' || next_ch == 'D') && nnext_i >= 0) //sd - SWDeint (от 0 до 2)
		{
			lvs->SWDeint = min(nnext_i, 2);
			i += 2;
		}
		else if ((s[i] == 'h' || s[i] == 'H') && nnext_i >=0)
		{
			if (next_ch == 'm' || next_ch == 'M') //hm - HW Mode (от 0 до 3)
			{
				lvs->HWMode = min(nnext_i, 3);
				i += 2;
			}
			else if (next_ch == 'c' || next_ch == 'C') //hc - HW Codecs (от 0 до 31)
			{
				lvs->HWCodecs = nnext_i;
				if (i + 3 < strlen(s) && s[i + 3] >= 48 && s[i + 3] <= 57)
				{
					lvs->HWCodecs *= 10;
					lvs->HWCodecs += s[i + 3] - 48;
					if (lvs->HWCodecs > 31) lvs->HWCodecs = 31;
					i += 1;
				}
				i += 2;
			}
			else if (next_ch == 'r' || next_ch == 'R') //hr - HW Resolutions (от 0 до 7)
			{
				lvs->HWRes = min(nnext_i, 7);
				i += 2;
			}
			else if (next_ch == 'd' || next_ch == 'D') //hd - HW Deint (от 0 до 2)
			{
				lvs->HWDeint = min(nnext_i, 2);
				i += 2;
			}
			else if (next_ch == 'q' || next_ch == 'Q') //hq - HW Deint HQ (0 = false, 1+ = true)
			{
				lvs->HWDeintHQ = (nnext_i > 0);
				i += 2;
			}
		}
	}
}

bool ApplyLAVSplitterSettings(IFileSourceFilter *pLAVS, LAVSplitterSettings lss)
{
	CComPtr<ILAVFSettings> pLAVSs;
	if (FAILED(pLAVS->QueryInterface(IID_ILAVFSettings, (void**)&pLAVSs)))
		return false;

	//Runtime Config mode вкл!
	pLAVSs->SetRuntimeConfig(true);
	pLAVSs->SetVC1TimestampMode(lss.VC1Fix);
	pLAVSs->SetSubtitleMode((LAVSubtitleMode)lss.SMode);
	pLAVSs->SetPreferredSubtitleLanguages(lss.SLanguage);
	pLAVSs->SetAdvancedSubtitleConfig(lss.SAdvanced);
	pLAVSs->SetLoadMatroskaExternalSegments(lss.ExtSegments);
	pLAVSs->SetTrayIcon(lss.TrayIcon);

	return true;
}

bool ApplyLAVVideoSettings(IBaseFilter *pLAVV, LAVVideoSettings lvs, unsigned int pixel_types)
{
	CComPtr<ILAVVideoSettings> pLAVVs;
	if (FAILED(pLAVV->QueryInterface(IID_ILAVVideoSettings, (void**)&pLAVVs)))
		return false;

	//Runtime Config mode вкл!
	pLAVVs->SetRuntimeConfig(true);
	pLAVVs->SetNumThreads(lvs.Threads);
	pLAVVs->SetRGBOutputRange(lvs.Range);
	pLAVVs->SetDitherMode((LAVDitherMode)lvs.Dither);
	pLAVVs->SetUseMSWMV9Decoder(lvs.WMVDMO);

	//Отключаем все форматы..
	for (int i = 0; i < LAVOutPixFmt_NB; i++)
		pLAVVs->SetPixelFormat((LAVOutPixFmts)i, false);

	//.. и включаем обратно только нужные
	pLAVVs->SetPixelFormat(LAVOutPixFmt_YV12, (pixel_types & IVS_YV12));
	pLAVVs->SetPixelFormat(LAVOutPixFmt_YUY2, (pixel_types & IVS_YUY2));
	pLAVVs->SetPixelFormat(LAVOutPixFmt_RGB32, (pixel_types & IVS_RGB32));
	pLAVVs->SetPixelFormat(LAVOutPixFmt_RGB24, (pixel_types & IVS_RGB24));

	if (lvs.DeintMode == 0) //Disabled
	{
		//Полностью отключаем деинтерлейс
		pLAVVs->SetDeinterlacingMode(DeintMode_Disable);
		pLAVVs->SetSWDeintMode(SWDeintMode_None);
	}
	else
	{
		if (lvs.DeintMode == 1) pLAVVs->SetDeinterlacingMode(DeintMode_Auto);            //Auto
		else if (lvs.DeintMode == 2) pLAVVs->SetDeinterlacingMode(DeintMode_Aggressive); //Auto (aggressive)
		else if (lvs.DeintMode == 3) pLAVVs->SetDeinterlacingMode(DeintMode_Force);      //Always on
		pLAVVs->SetDeintFieldOrder((LAVDeintFieldOrder)lvs.FieldOrder);

		if (lvs.SWDeint == 0) //None
		{
			pLAVVs->SetSWDeintMode(SWDeintMode_None);
		}
		else if (lvs.SWDeint == 1) //Yadif
		{
			pLAVVs->SetSWDeintMode(SWDeintMode_YADIF);
			pLAVVs->SetSWDeintOutput(DeintOutput_FramePer2Field);
		}
		else if (lvs.SWDeint == 2) //Yadif (x2)
		{
			pLAVVs->SetSWDeintMode(SWDeintMode_YADIF);
			pLAVVs->SetSWDeintOutput(DeintOutput_FramePerField);
		}
	}

	//Хардварное
	pLAVVs->SetHWAccel((LAVHWAccel)lvs.HWMode);
	if (lvs.HWMode > 0)
	{
		pLAVVs->SetHWAccelCodec(HWCodec_H264, (lvs.HWCodecs & H264));
		pLAVVs->SetHWAccelCodec(HWCodec_VC1, (lvs.HWCodecs & VC1));
		pLAVVs->SetHWAccelCodec(HWCodec_MPEG2, (lvs.HWCodecs & MPEG2));
		pLAVVs->SetHWAccelCodec(HWCodec_MPEG4, (lvs.HWCodecs & MPEG4));
		pLAVVs->SetHWAccelCodec(HWCodec_HEVC, (lvs.HWCodecs & HEVC));
		pLAVVs->SetHWAccelResolutionFlags(lvs.HWRes);

		if (lvs.DeintMode == 0 || lvs.HWDeint == 0) //Weave
		{
			pLAVVs->SetHWAccelDeintMode(HWDeintMode_Weave);
			pLAVVs->SetHWAccelDeintOutput(DeintOutput_FramePer2Field);
		}
		else if (lvs.HWDeint == 1) //Adaptive
		{
			pLAVVs->SetHWAccelDeintMode(HWDeintMode_Hardware);
			pLAVVs->SetHWAccelDeintOutput(DeintOutput_FramePer2Field);
		}
		else if (lvs.HWDeint == 2) //Adaptive (x2)
		{
			pLAVVs->SetHWAccelDeintMode(HWDeintMode_Hardware);
			pLAVVs->SetHWAccelDeintOutput(DeintOutput_FramePerField);
		}

		pLAVVs->SetHWAccelDeintHQ(lvs.HWDeintHQ);
	}

	pLAVVs->SetDVDVideoSupport(false);
	pLAVVs->SetHWAccelCodec(HWCodec_MPEG2DVD, false);
	pLAVVs->SetTrayIcon(lvs.TrayIcon);

	return true;
}