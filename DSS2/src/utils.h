/*
 * Copyright (c) 2004-2008 Mike Matsnev.  All Rights Reserved.
 * 
 * $Id: utils.h,v 1.77 2011/02/27 11:45:16 mike Exp $
 * 
 */

#ifndef UTILS_H
#define UTILS_H

#define LANG_BUFFER_SIZE 256
struct LAVSplitterSettings              //x = digit
{
	unsigned int Loading;               //lx
	unsigned int VC1Fix;                //vcx
	unsigned int SMode;                 //smx
	WCHAR SLanguage[LANG_BUFFER_SIZE];  //sl[...]
	WCHAR SAdvanced[LANG_BUFFER_SIZE];  //sa[...]
	bool TrayIcon;                      //tix
};

struct LAVVideoSettings       //x = digit
{
	unsigned int Loading;     //lx
	unsigned int Threads;     //tx or txx
	unsigned int Range;       //rx
	unsigned int Dither;      //dx
	unsigned int DeintMode;   //dmx
	unsigned int FieldOrder;  //fox
	unsigned int SWDeint;     //sdx
	bool WMVDMO;              //vcx

	unsigned int HWMode;      //hmx
	unsigned int HWCodecs;    //hcx or hcxx
	unsigned int HWRes;       //hrx
	unsigned int HWDeint;     //hdx
	bool HWDeintHQ;           //hqx
	bool TrayIcon;            //tix
};

enum LAVLoading
{
	LFSystem,   //LoadFromSystem
	LFFile,     //LoadFromFile
	LFSystemS,  //+ apply Settings
	LFFileS     //+ apply Settings
};

enum HWCodecs
{
	H264 = 1,
	VC1 = 2,
	MPEG2 = 4,
	MPEG4 = 8
};

CComPtr<IPin> GetPin(IBaseFilter *pF, bool include_connected, PIN_DIRECTION dir, const GUID *pMT = NULL);
HRESULT LoadSplitterFromFile(IFileSourceFilter **pFSource, volatile HMODULE *hModule, const char *subDir, const char *fileName, const GUID CLSID_Filter, char *err, rsize_t err_len);
HRESULT LoadFilterFromFile(IBaseFilter **pBFilter, volatile HMODULE *hModule, const char *subDir, const char *fileName, const GUID CLSID_Filter, char *err, rsize_t err_len);
void ParseLAVSplitterSettings(LAVSplitterSettings *lss, const char *s);
void ParseLAVVideoSettings(LAVVideoSettings *lvs, const char *s);
bool ApplyLAVSplitterSettings(IFileSourceFilter *pLAVS, LAVSplitterSettings lss);
bool ApplyLAVVideoSettings(IBaseFilter *pLAVV, LAVVideoSettings lvs, unsigned int pixel_types);

#define ENUM_FILTERS(graph, var) for (CComPtr<IEnumFilters> __pEF__; !__pEF__ && SUCCEEDED(graph->EnumFilters(&__pEF__)); ) for (CComPtr<IBaseFilter> var; __pEF__->Next(1, &var, NULL) == S_OK; var.Release())
#define ENUM_PINS(filter, var) for (CComPtr<IEnumPins> __pEP__; !__pEP__ && SUCCEEDED(filter->EnumPins(&__pEP__)); ) for (CComPtr<IPin> var; __pEP__->Next(1, &var, NULL) == S_OK; var.Release())
#define ENUM_MT(pin, var) for (CComPtr<IEnumMediaTypes> __pEMT__; !__pEMT__ && SUCCEEDED(pin->EnumMediaTypes(&__pEMT__)); ) for (MTPtr var; __pEMT__->Next(1, &var, NULL) == S_OK; )

class MTPtr {
  AM_MEDIA_TYPE *pMT;

  MTPtr(const MTPtr&);
  MTPtr& operator=(const MTPtr&);
public:
  MTPtr() : pMT(NULL) { }
  ~MTPtr() { DeleteMediaType(pMT); }

  AM_MEDIA_TYPE *operator->() { return pMT; }
  const AM_MEDIA_TYPE *operator->() const { return pMT; }
  operator AM_MEDIA_TYPE *() { return pMT; }
  AM_MEDIA_TYPE **operator&() { DeleteMediaType(pMT); pMT = NULL; return &pMT; }

  void  Set(MTPtr& other) {
    DeleteMediaType(pMT);
    pMT = other.pMT;
    other.pMT = NULL;
  }

  void  Allocate() {
    DeleteMediaType(pMT);
    pMT = (AM_MEDIA_TYPE *)CoTaskMemAlloc(sizeof(AM_MEDIA_TYPE));
    memset(pMT, 0, sizeof(*pMT));
  }

  void  ReallocFmt(ULONG size) {
    pMT->pbFormat = (BYTE *)CoTaskMemRealloc(pMT->pbFormat, size);
    pMT->cbFormat = pMT->pbFormat == NULL ? 0 : size;
  }

  static void FreeMediaType(AM_MEDIA_TYPE *pMT) {
    if (pMT == NULL)
      return;
    if (pMT->cbFormat > 0) {
      CoTaskMemFree(pMT->pbFormat);
      pMT->pbFormat = NULL;
      pMT->cbFormat = 0;
    }
    if (pMT->pUnk) {
      pMT->pUnk->Release();
      pMT->pUnk = NULL;
    }
  }

  static void  DeleteMediaType(AM_MEDIA_TYPE *pMT) {
    if (pMT == NULL)
      return;
    if (pMT->cbFormat > 0)
      CoTaskMemFree(pMT->pbFormat);
    if (pMT->pUnk)
      pMT->pUnk->Release();
    CoTaskMemFree(pMT);
  }
};

#endif