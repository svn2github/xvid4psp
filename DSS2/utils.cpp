//(XviD4PSP5) modded version
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

#include <dshow.h>
#include <dvdmedia.h>
#include <ks.h>
#include <ksmedia.h>

#include "utils.h"

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
					//"warning C4800: 'int' : forcing value to bool 'true' or 'false'"
					bool found = ((MT.majortype == *pMT) ? true : false);
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
		TCHAR VarPath[MAX_PATH], LibPath[MAX_PATH];
		GetModuleFileName(_AtlBaseModule.GetModuleInstance(), VarPath, MAX_PATH);  //Полный путь к DSS2
		_tcsrchr(VarPath, _T('\\'))[1] = 0;                                        //Отсекаем имя файла
		_stprintf_s(LibPath, MAX_PATH, "%s%s", VarPath, subDir);                   //Добавляем подпапку
		GetCurrentDirectory(MAX_PATH, VarPath);                                    //Запоминаем текущую директорию
		SetCurrentDirectory(LibPath);                                              //Меняем её на директорию с загружаемой длл (иначе не подгрузятся связанные с ней dll)
		_tcsncat_s(LibPath, MAX_PATH, fileName, _TRUNCATE);                        //Добавляем в путь имя нужной нам dll
		*hModule = LoadLibrary(LibPath);                                           //И грузим её (с указанием полного пути, иначе может подгрузиться хз что и хз откуда!)
		SetCurrentDirectory(VarPath);                                              //Восстанавливаем текущую директорию
		if (!*hModule) { strncpy_s(err, err_len, "LoadLibrary: ", _TRUNCATE); return E_FAIL; }
	}

	pDllGetClassObject = (DllGetClassObjectFunc)GetProcAddress(*hModule, "DllGetClassObject");
	if (!pDllGetClassObject) { strncpy_s(err, err_len, "DllGetClassObject: ", _TRUNCATE); return E_FAIL; }

	CComPtr<IClassFactory> pCF;
	if (FAILED(hr = pDllGetClassObject(CLSID_Filter, IID_IClassFactory, (void**)&pCF))) {
		strncpy_s(err, err_len, "Get IClassFactory: ", _TRUNCATE); return hr; }

	if (FAILED(hr= pCF->CreateInstance(NULL, IID_IFileSourceFilter, (void **)pFSource))) {
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
		TCHAR VarPath[MAX_PATH], LibPath[MAX_PATH];
		GetModuleFileName(_AtlBaseModule.GetModuleInstance(), VarPath, MAX_PATH);  //Полный путь к DSS2
		_tcsrchr(VarPath, _T('\\'))[1] = 0;                                        //Отсекаем имя файла
		_stprintf_s(LibPath, MAX_PATH, "%s%s", VarPath, subDir);                   //Добавляем подпапку
		GetCurrentDirectory(MAX_PATH, VarPath);                                    //Запоминаем текущую директорию
		SetCurrentDirectory(LibPath);                                              //Меняем её на директорию с загружаемой длл (иначе не подгрузятся связанные с ней dll)
		_tcsncat_s(LibPath, MAX_PATH, fileName, _TRUNCATE);                        //Добавляем в путь имя нужной нам dll
		*hModule = LoadLibrary(LibPath);                                           //И грузим её (с указанием полного пути, иначе может подгрузиться хз что и хз откуда!)
		SetCurrentDirectory(VarPath);                                              //Восстанавливаем текущую директорию
		if (!*hModule) { strncpy_s(err, err_len, "LoadLibrary: ", _TRUNCATE); return E_FAIL; }
	}

	pDllGetClassObject = (DllGetClassObjectFunc)GetProcAddress(*hModule, "DllGetClassObject");
	if (!pDllGetClassObject) { strncpy_s(err, err_len, "DllGetClassObject: ", _TRUNCATE); return E_FAIL; }

	CComPtr<IClassFactory> pCF;
	if (FAILED(hr = pDllGetClassObject(CLSID_Filter, IID_IClassFactory, (void**)&pCF))) {
		strncpy_s(err, err_len, "Get IClassFactory: ", _TRUNCATE); return hr; }

	CComPtr<IUnknown> object;
	if (FAILED(hr = pCF->CreateInstance(NULL, IID_IUnknown, (void **)&object)))	{
		strncpy_s(err, err_len, "Get IUnknown: ", _TRUNCATE); return hr; }

	if (FAILED(hr = object->QueryInterface(IID_IBaseFilter, (void **)pBFilter))) {
		strncpy_s(err, err_len, "Get IBaseFilter: ", _TRUNCATE); return hr; }

	return S_OK;
}
