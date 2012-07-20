/*
 * Copyright (c) 2004-2008 Mike Matsnev.  All Rights Reserved.
 * 
 * $Id: avss.cpp,v 1.7 2008/03/29 15:41:28 mike Exp $
 * 
 */

#define _ATL_FREE_THREADED
#define _ATL_CSTRING_EXPLICIT_CONSTRUCTORS	// some CString constructors will be explicit
#define _ATL_ALL_WARNINGS

#include <windows.h>
#include <tchar.h>
#include <dshow.h>
#include <atlbase.h>
#include <atlcom.h>
#include <atlstr.h>
#include <atlcoll.h>
#include <initguid.h>
#include "VideoSink.h"
#include "utils.h"

#if defined AVS_26
#include "avisynth_26.h"
#else
#include "avisynth.h"
#endif

// {F13D3732-96BD-4108-AFEB-E85F68FF64DC}
DEFINE_GUID(CLSID_VideoSink, 0xf13d3732, 0x96bd, 0x4108, 0xaf, 0xeb, 0xe8, 0x5f, 0x68, 0xff, 0x64, 0xdc);

class DSS2 : public IClip
{
	struct DF
	{
		REFERENCE_TIME  timestamp;  // DS timestamp that we used for this frame
		PVideoFrame     frame;      // cached AVS frame

		DF() : timestamp(-1) { }
		DF(PVideoFrame f) : timestamp(-1), frame(f) { }
		DF(const DF& f) { operator=(f); }
		DF& operator=(const DF& f) { timestamp = f.timestamp; frame = f.frame; return *this; }
	};

	CAtlArray<DF>           m_f;
	int                     m_qfirst,
		                    m_qnext,
		                    m_qmax,
		                    m_seek_thr;

	CComPtr<IVideoSink>     m_pR;
	CComPtr<IMediaControl>  m_pGC;
	CComPtr<IMediaSeeking>  m_pGS;
	HANDLE                  m_hFrameReady;
	bool                    m_registered;
	DWORD                   m_rot_cookie;

	VideoInfo               m_vi;
	long long               m_avgframe;

	void RegROT()
	{
		if (!m_pGC || m_registered)
			return;

		CComPtr<IRunningObjectTable> rot;
		if (FAILED(GetRunningObjectTable(0, &rot)))
			return;

		CStringA  name;
		name.Format("FilterGraph %08p pid %08x (avss)", m_pGC.p, GetCurrentProcessId());

		CComPtr<IMoniker> mk;
		if (FAILED(CreateItemMoniker(L"!", CA2W(name), &mk)))
			return;

		if (SUCCEEDED(rot->Register(ROTFLAGS_REGISTRATIONKEEPSALIVE, m_pGC, mk, &m_rot_cookie)))
			m_registered = true;
	}

	void UnregROT()
	{
		if (!m_registered)
			return;

		CComPtr<IRunningObjectTable> rot;
		if (FAILED(GetRunningObjectTable(0, &rot)))
			return;

		if (SUCCEEDED(rot->Revoke(m_rot_cookie)))
			m_registered = false;
	}

	HRESULT Open(const wchar_t *filename, long long avgf)
	{
		HRESULT hr;

		CComPtr<IGraphBuilder> pG;
		if (FAILED(hr = pG.CoCreateInstance(CLSID_FilterGraph)))
			return hr;

		CComPtr<IBaseFilter> pR;
		if (FAILED(hr = pR.CoCreateInstance(CLSID_VideoSink)))
			return hr;

		pG->AddFilter(pR, L"VideoSink");

		CComQIPtr<IVideoSink> sink(pR);
		if (!sink) return E_NOINTERFACE;

		CComQIPtr<IVideoSink2> sink2(pR);
		if (!sink2) return E_NOINTERFACE;

		sink->SetAllowedTypes(IVS_RGB32|IVS_YV12|IVS_YUY2);
		ResetEvent(m_hFrameReady);
		sink2->NotifyFrame(m_hFrameReady);

		CComPtr<IBaseFilter> pS;
		if (FAILED(hr = pG->AddSourceFilter(filename, NULL, &pS)))
			return hr;

		CComQIPtr<IPropertyBag> pPB(pS);
		if (pPB) pPB->Write(L"ui.interactive", &CComVariant(0u, VT_UI4));

		CComPtr<IPin> pO(GetPin(pS, false, PINDIR_OUTPUT, &MEDIATYPE_Video));
		if (!pO) pO = GetPin(pS, false, PINDIR_OUTPUT, &MEDIATYPE_Stream);

		CComPtr<IPin> pI(GetPin(pR, false, PINDIR_INPUT));

		if (!pO || !pI)
			return E_FAIL;

		if (FAILED(hr = pG->Connect(pO, pI)))
			return hr;

		CComQIPtr<IMediaControl> mc(pG);
		CComQIPtr<IMediaSeeking> ms(pG);

		if (!mc || !ms)
			return E_NOINTERFACE;

		if (FAILED(hr = mc->Run()))
			return hr;

		OAFilterState fs;
		if (FAILED(hr = mc->GetState(2000, &fs)))
			return hr;

		// wait for the first frame to arrive
		if (WaitForSingleObject(m_hFrameReady, 5000) != WAIT_OBJECT_0) // up to 5s
			return E_FAIL;

		long long defd;
		unsigned  type, width, height, arx, ary;
		if (FAILED(hr = sink2->GetFrameFormat(&type, &width, &height, &arx, &ary, &defd)))
			return hr;

		REFERENCE_TIME duration;
		if (FAILED(hr = ms->GetDuration(&duration)))
			return hr;

		if (defd == 0)
			defd = 400000;

		if (avgf > 0)
			defd = avgf;

		switch (type)
		{
			case IVS_RGB32: m_vi.pixel_type = VideoInfo::CS_BGR32; break;
			case IVS_YUY2: m_vi.pixel_type = VideoInfo::CS_YUY2; break;
			case IVS_YV12: m_vi.pixel_type = VideoInfo::CS_YV12; break;
			default: return E_FAIL;
		}

		m_vi.width = width;
		m_vi.height = height;

		m_vi.num_frames = duration / defd;
		m_vi.SetFPS(10000000, defd);

		m_avgframe = defd;

		m_pR = sink;
		m_pGC = mc;
		m_pGS = ms;

		SetEvent(m_hFrameReady);

		RegROT();

		m_f.SetCount(m_vi.num_frames);

		return S_OK;
	}

	void  Close()
	{
		CComQIPtr<IVideoSink2> pVS2(m_pR);
		if (pVS2) pVS2->NotifyFrame(NULL);

		m_f.RemoveAll();

		UnregROT();

		m_pR.Release();
		m_pGC.Release();
		m_pGS.Release();
	}

	struct RFArg
	{
		DF        *df;
		VideoInfo *vi;
	};

	static void ReadFrame(long long timestamp, unsigned format, unsigned bpp, const unsigned char *frame, unsigned width, unsigned height, int stride, unsigned arx, unsigned ary, void *arg)
	{
		RFArg     *rfa = (RFArg *)arg;
		DF        *df = rfa->df;
		VideoInfo *vi = rfa->vi;

		df->timestamp = timestamp;

		int w_cp = min((int)width, vi->width);
		int h_cp = min((int)height, vi->height);

		if ((format == IVS_RGB32 && vi->pixel_type == VideoInfo::CS_BGR32) ||
			(format == IVS_YUY2 && vi->pixel_type == VideoInfo::CS_YUY2))
		{
			BYTE *dst = df->frame->GetWritePtr();
			int  dstride = df->frame->GetPitch();

			w_cp *= bpp;

			for (int y = 0; y < h_cp; ++y)
			{
				memcpy(dst, frame, w_cp);
				frame += stride;
				dst += dstride;
			}
		}
		else if (format == IVS_YV12 && vi->pixel_type == VideoInfo::CS_YV12)
		{
			// plane Y
			BYTE                *dp = df->frame->GetWritePtr(PLANAR_Y);
			const unsigned char *sp = frame;
			int                 dstride = df->frame->GetPitch(PLANAR_Y);

			for (int y = 0; y < h_cp; ++y)
			{
				memcpy(dp, sp, w_cp);
				sp += stride;
				dp += dstride;
			}

			// UV
			dstride >>= 1;
			stride >>= 1;
			w_cp >>= 1;
			h_cp >>= 1;

			// plane V
			dp = df->frame->GetWritePtr(PLANAR_V);
			sp = frame + height * stride * 2;
			dstride = df->frame->GetPitch(PLANAR_V);

			for (int y = 0; y < h_cp; ++y)
			{
				memcpy(dp, sp, w_cp);
				sp += stride;
				dp += dstride;
			}

			// plane U
			dp = df->frame->GetWritePtr(PLANAR_U);
			sp = frame + height * stride * 2 + (height >> 1) * stride;
			dstride = df->frame->GetPitch(PLANAR_U);

			for (int y = 0; y < h_cp; ++y)
			{
				memcpy(dp, sp, w_cp);
				sp += stride;
				dp += dstride;
			}
		}
	}

	bool NextFrame(IScriptEnvironment *env, DF& rdf, int& fn)
	{
		for (;;)
		{
			if (WaitForSingleObject(m_hFrameReady, INFINITE) != WAIT_OBJECT_0)
				return false;

			DF df(env->NewVideoFrame(m_vi));
			RFArg arg = { &df, &m_vi };

			HRESULT hr = m_pR->ReadFrame(ReadFrame, &arg);
			if (FAILED(hr))
				return false;

			if (hr == S_FALSE) // EOF
				return false;

			if (df.timestamp >= 0)
			{
				int frameno = (int)((double)df.timestamp / m_avgframe + 0.5);

				if (frameno >= 0 && frameno < m_f.GetCount())
				{
					fn = frameno;
					rdf = df;
					return true;
				}
			}
		}
	}

public:
	DSS2() :
		m_registered(false),
		m_qfirst(0),
		m_qnext(0),
		m_qmax(10),
		m_seek_thr(100)
	{
		m_hFrameReady = CreateEvent(NULL, FALSE, FALSE, NULL);
		memset(&m_vi, 0, sizeof(m_vi));
	}

	~DSS2()
	{
		Close();
		CloseHandle(m_hFrameReady);
	}

	HRESULT OpenFile(const wchar_t *filename, long long avgframe)
	{
		return Open(filename, avgframe);
	}

	// IClip
	PVideoFrame __stdcall GetFrame(int n, IScriptEnvironment *env)
	{
		if (n < 0 || n >= m_f.GetCount())
			env->ThrowError("GetFrame: frame number out of range");

		if (m_f[n].frame)
			return m_f[n].frame;

		// we need to seek to get this frame
		if (n < m_qfirst || n > m_qnext + m_seek_thr)
		{
			// clear buffered frames
			for (int i = m_qfirst; i < m_qnext; ++i)
				m_f[i].frame = NULL;

			// reset read postion
			if (n >= m_qfirst - m_qmax && n < m_qfirst) // assume we are scanning backwards
			{
				m_qfirst = n - m_qmax + 1;
				if (m_qfirst < 0) m_qfirst = 0;
				m_qnext = m_qfirst;
			}
			else
			{
				// just some random seek
				m_qfirst = m_qnext = n;
			}

			// seek the graph
			ResetEvent(m_hFrameReady);

			REFERENCE_TIME cur = m_avgframe * m_qfirst - 10001; // -1ms, account for typical timestamps rounding
			if (cur < 0) cur = 0;

			if (FAILED(m_pGS->SetPositions(&cur, AM_SEEKING_AbsolutePositioning, NULL, AM_SEEKING_NoPositioning)))
				env->ThrowError("DS Seeking failed");
		}

		// we performed a seek or are reading sequentially
		for (;;)
		{
			DF df;
			int frameno;

			if (!NextFrame(env, df, frameno))
				return env->NewVideoFrame(m_vi);

			// let's see what we've got
			if (frameno < m_qnext)
			{
				// preroll frame
				continue;
			}
			else if (frameno == m_qnext)
			{ 
				// we want this frame, compare timestamps to account for decimation
				if (m_f[frameno].timestamp < 0) // we see this for the first time
					m_f[frameno].timestamp = df.timestamp;

				if (df.timestamp < m_f[frameno].timestamp) // early, ignore
					continue;

				// this is the frame we want
				m_f[frameno].frame = df.frame;
			}
			else //frameno > m_qnext
			{ 
				// somehow we got beyond the place we wanted, fill in intermediate frames        
				// don't check for timestamps, just use what we've got
				for (int i = m_qnext; i <= frameno; ++i)
				{
					if (m_f[i].timestamp < 0)
						m_f[i].timestamp = df.timestamp;
					m_f[i].frame = df.frame;
				}
			}

			// keep cached frames below max
			for (; m_qnext - m_qfirst > m_qmax && m_qfirst < m_qnext && m_qfirst < n; ++m_qfirst)
				m_f[m_qfirst].frame = NULL;

			m_qnext = frameno + 1;

			if (n >= m_qfirst && n < m_qnext)
				return m_f[n].frame;
		}
	}

	const VideoInfo& __stdcall GetVideoInfo() { return m_vi; }

	// TODO
	void __stdcall GetAudio(void *buf, long long start, long long count, IScriptEnvironment *env) { memset(buf, 0, m_vi.BytesFromAudioSamples(count)); }
	bool __stdcall GetParity(int n) { return true; }
#ifdef AVS_26
	int __stdcall SetCacheHints(int cachehints,int frame_range) { return 0; };
#else
	void __stdcall SetCacheHints(int cachehints, int frame_range) { }
#endif
};

static AVSValue __cdecl Create_DSS2(AVSValue args, void*, IScriptEnvironment* env)
{
	if (args[0].ArraySize() != 1)
		env->ThrowError("DSS2: Only 1 filename currently supported!");

	const char *filename = args[0][0].AsString();
	if (filename == NULL || strlen(filename) == 0)
		env->ThrowError("Filename expected");

	long long avgframe = args[1].Defined() ? (long long)(10000000ll / args[1].AsFloat() + 0.5) : 0;

	DSS2 *dss2 = new DSS2();

	HRESULT hr = dss2->OpenFile(CA2WEX<128>(filename, CP_OEMCP), avgframe);
	if (FAILED(hr))
	{
		delete dss2;
		env->ThrowError("Can't open %s: %08x", filename, hr);
	}

	return dss2;
}

#ifdef AVS_26
const AVS_Linkage *AVS_linkage = 0;
extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit3(IScriptEnvironment* env, const AVS_Linkage* const vectors)
{
	AVS_linkage = vectors;
	env->AddFunction("DSS2", "s+[fps]f", Create_DSS2, 0);
	return "DSS2";
}
#else
extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit2(IScriptEnvironment* env)
{
	env->AddFunction("DSS2", "s+[fps]f", Create_DSS2, 0);
	return "DSS2";
}
#endif
