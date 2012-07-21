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
