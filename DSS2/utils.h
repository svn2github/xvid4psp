/*
 * Copyright (c) 2004-2008 Mike Matsnev.  All Rights Reserved.
 * 
 * $Id: utils.h,v 1.77 2011/02/27 11:45:16 mike Exp $
 * 
 */

#ifndef UTILS_H
#define UTILS_H

CComPtr<IPin> GetPin(IBaseFilter *pF, bool include_connected, PIN_DIRECTION dir, const GUID *pMT = NULL);

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