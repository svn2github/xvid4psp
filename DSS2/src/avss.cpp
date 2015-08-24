//(XviD4PSP5) modded version, 2012-2013
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
#include "VideoSink.h"
#include "utils.h"
#include "guids.h"
#include <math.h>

#if defined(AVS_PLUS)
#include "avisynth_plus.h"
#elif defined(AVS_26)
#include "avisynth_26.h"
#else
#include "avisynth.h"
#endif

#define ERRMSG_LEN 1024
#define SAFE_FREELIBRARY(x) { if (x) FreeLibrary(x); x = NULL; }
volatile static HMODULE hLAVSplitter = NULL;
volatile static HMODULE hLAVVideo = NULL;
volatile static HMODULE hVSFilter = NULL;

//Кол-во DSS2, открытых из-под одного процесса
volatile static long RefCount = 0;

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
		                    m_seek_thr,
		                    m_preroll,
		                    m_do_not_trespass;
	PVideoFrame             m_last_good_frame;

	CComPtr<IVideoSink>     m_pR;
	CComPtr<IMediaControl>  m_pGC;
	CComPtr<IMediaSeeking>  m_pGS;
	HANDLE                  m_hFrameReady;
	bool                    m_registered;
	DWORD                   m_rot_cookie,
		                    m_timeout;

	VideoInfo               m_vi;
	double                  m_avgframe;

	void RegROT()
	{
		if (!m_pGC || m_registered)
			return;

		CComPtr<IRunningObjectTable> rot;
		if (FAILED(GetRunningObjectTable(0, &rot)))
			return;

		CStringA name;
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

	HRESULT Open(const wchar_t *filename, char *err, double fps, unsigned int fps_den, int subs_mode, const char *lavs, const char *lavd, const char *lavf_path, const char *dvs_path, unsigned int pixel_types, int tc_offset)
	{
		InterlockedIncrement(&RefCount);

		bool lav_splitter = (strlen(lavs) > 0);
		bool lav_decoder = (strlen(lavd) > 0);

		HRESULT hr;

		CComPtr<IGraphBuilder> pGB; //CLSID_FilterGraphNoThread
		if (FAILED(hr = pGB.CoCreateInstance(CLSID_FilterGraph))) {
			SetError("Create FilterGraph: ", err); return hr; }

		CComPtr<IBaseFilter> pVS;
		if (FAILED(hr = CreateVideoSink(&pVS))) {
			SetError("Create VideoSink: ", err); return hr; }

		if (FAILED(hr = pGB->AddFilter(pVS, L"VideoSink"))) {
			SetError("Add VideoSink: ", err); return hr; }

		CComPtr<IPin> pVS_In(GetPin(pVS, false, PINDIR_INPUT));
		if (!pVS_In) { SetError("GetPin (VS_In): ", err); return E_FAIL; }

		CComQIPtr<IVideoSink> sink(pVS);
		if (!sink) { SetError("Get IVideoSink: ", err); return E_NOINTERFACE; }

		CComQIPtr<IVideoSink2> sink2(pVS);
		if (!sink2) { SetError("Get IVideoSink2: ", err); return E_NOINTERFACE; }

		sink->SetAllowedTypes(pixel_types);
		sink->SetTCOffset(tc_offset);
		ResetEvent(m_hFrameReady);
		sink2->NotifyFrame(m_hFrameReady);

		CComPtr<IBaseFilter> pSrc;
		if (!lav_splitter)
		{
			//Открываем через SourceFilter
			if (FAILED(hr = pGB->AddSourceFilter(filename, NULL, &pSrc))) {
				SetError("Add Source: ", err); return hr; }

			//Это отключает иконку Haali-сплиттера в трее (зачем?!)
			//CComQIPtr<IPropertyBag> pPB(pSrc);
			//if (pPB) pPB->Write(L"ui.interactive", &CComVariant(0u, VT_UI4));
		}
		else
		{
			//Открываем через LAVSplitterSource
			CComPtr<IFileSourceFilter> pLAVS;

			LAVSplitterSettings lss = {};
			ParseLAVSplitterSettings(&lss, lavs); //"l3 vc2 sm2 sl[] sa[] es0 ti0"

			if (lss.Loading == LFSystem || lss.Loading == LFSystemS)
			{
				if (FAILED(hr = pLAVS.CoCreateInstance(CLSID_LAVSplitterSource))) {
					SetError("Create LAVSplitter: ", err); return hr; }
			}
			else
			{
				if (FAILED(hr = LoadSplitterFromFile(&pLAVS, &hLAVSplitter, lavf_path, "LAVSplitter.ax", CLSID_LAVSplitterSource, err, ERRMSG_LEN))) {
					SetError("Load LAVSplitter: ", err); return hr; }
			}

			if (lss.Loading == LFSystemS || lss.Loading == LFFileS)
			{
				//Даже не будем пытаться искать субтитры, если в LAVSplitter`е они отключены
				if (lss.SMode == 0 && subs_mode > 0)
					subs_mode = 0;

				if (!ApplyLAVSplitterSettings(pLAVS, lss)) {
					SetError("Apply LAVSplitter settings: ", err); return E_NOINTERFACE; }
			}

			if (FAILED(hr = pLAVS->Load(filename, NULL))) {
				SetError("Add file to LAVSplitter: ", err); return hr; }

			if (FAILED(hr = pLAVS->QueryInterface(IID_IBaseFilter, (void**)&pSrc))) {
				SetError("Get IBaseFilter: ", err); return hr; }

			if (FAILED(hr = pGB->AddFilter(pSrc, L"LAV Splitter"))) {
				SetError("Add LAVSplitter: ", err); return hr; }
		}

		CComPtr<IPin> pSrc_Out(GetPin(pSrc, false, PINDIR_OUTPUT, &MEDIATYPE_Video));
		if (!pSrc_Out) pSrc_Out = GetPin(pSrc, false, PINDIR_OUTPUT, &MEDIATYPE_Stream);
		if (!pSrc_Out) { SetError("GetPin (Src_Out): ", err); return VFW_E_CANNOT_LOAD_SOURCE_FILTER; }

		CComPtr<IPin> pLAVV_Out;
		if (lav_decoder)
		{
			LAVVideoSettings lvs = {};
			ParseLAVVideoSettings(&lvs, lavd); //"l3 t0 r0 d1 dm0 fo0 sd0 vc1 hm0 hc7 hr3 hd0 hq0 ti0"

			CComPtr<IBaseFilter> pLAVV;
			if (lvs.Loading == LFSystem || lvs.Loading == LFSystemS)
			{
				if (FAILED(hr = pLAVV.CoCreateInstance(CLSID_LAVVideo))) {
					SetError("Create LAVVideo: ", err); return hr; }
			}
			else
			{
				if (FAILED(hr = LoadFilterFromFile(&pLAVV, &hLAVVideo, lavf_path, "LAVVideo.ax", CLSID_LAVVideo, err, ERRMSG_LEN))) {
					SetError("Load LAVVideo: ", err); return hr; }
			}

			if ((lvs.Loading == LFSystemS || lvs.Loading == LFFileS) && !ApplyLAVVideoSettings(pLAVV, lvs, pixel_types)) {
				SetError("Apply LAVVideo settings: ", err); return E_NOINTERFACE; }

			if(FAILED(hr = pGB->AddFilter(pLAVV, L"LAV Video Decoder"))) {
				SetError("Add LAVVideo: ", err); return hr; }

			CComPtr<IPin> pLAVV_In(GetPin(pLAVV, false, PINDIR_INPUT));
			if (!pLAVV_In) { SetError("GetPin (LAVV_In): ", err); return E_FAIL; }

			if (!lav_splitter)
			{
				//На выходе SourceFilter может быть просто Stream, в режиме Connect Граф сам вставит нужный сплиттер
				if(FAILED(hr = pGB->Connect(pSrc_Out, pLAVV_In))) {
					SetError("Connect (Src_Out + LAVV_In): ", err); return hr; }
			}
			else
			{
				//На выходе LAVSplitter всегда то, что нам нужно - можно подключаться напрямую
				if(FAILED(hr = pGB->ConnectDirect(pSrc_Out, pLAVV_In, NULL))) {
					SetError("Connect direct (Src_Out + LAVV_In): ", err); return hr; }
			}

			pLAVV_Out = GetPin(pLAVV, false, PINDIR_OUTPUT);
			if (!pLAVV_Out) { SetError("GetPin (LAVV_Out): ", err); return E_FAIL; }

			//LAVVideo не обрабатывает субтитры, а при ConnectDirect Граф сам никогда не вставит нужный обработчик.
			//В режиме 1 мы его тоже не вставляем - значит можно отключить все попытки искать субтитры (или выдать ошибку?).
			if (subs_mode == 1) subs_mode = 0;
		}

		if (subs_mode <= 0) //Без субтитров
		{
			if (!lav_decoder)
			{
				//Старый способ - промежуточные фильтры вставляются сами
				if (FAILED(hr = pGB->Connect(pSrc_Out, pVS_In))) {
					SetError("Connect filters (Src_Out + VS_In): ", err); return hr; }
			}
			else
			{
				//Прямое соединение фильтров - исключается подхват всякого DS-мусора
				if (FAILED(hr = pGB->ConnectDirect(pLAVV_Out, pVS_In, NULL))) {
					SetError("Connect direct (LAVV_Out + VS_In): ", err); return hr; }
			}
		}
		else //С возможностью грузить субтитры
		{
			CComPtr<IFilterGraph2> pFG2;
			if (FAILED(hr = pGB->QueryInterface(IID_IFilterGraph2, (void**)&pFG2))) {
				SetError("Get IFilterGraph2: ", err); return hr; }

			CComPtr<IPin> pSrc_SOut(GetPin(pSrc, false, PINDIR_OUTPUT, &MEDIATYPE_Subtitle));
			if (!pSrc_SOut) pSrc_SOut = (GetPin(pSrc, false, PINDIR_OUTPUT, &MEDIATYPE_Text));

			if (subs_mode == 1 || !pSrc_SOut)
			{
				if (!lav_decoder)
				{
					if (FAILED(hr = pFG2->RenderEx(pSrc_Out, AM_RENDEREX_RENDERTOEXISTINGRENDERERS, NULL))) {
						SetError("RenderEx: ", err); return hr; }
				}
				else
				{
					//IFilterMapper2::EnumMatchingFilters
					if (FAILED(hr = pGB->ConnectDirect(pLAVV_Out, pVS_In, NULL))) {
						SetError("Connect direct (LAVV_Out + VS_In): ", err); return hr; }
				}
			}

			if (pSrc_SOut)
			{
				if (subs_mode >= 2) //Принудительно грузим DirectVobSub
				{
					//Ищем DirectVobSub в Графе. Хотя скорее всего его там пока-что нет, т.к. мы его туда еще не добавляли.
					//А даже если он и добавляется туда Haali-сплиттером, еще чем или сам по себе - то только после команды Render(Ex).
					CComPtr<IBaseFilter> pDVS;
					ENUM_FILTERS(pGB, pBF)
					{
						GUID gID;
						pBF->GetClassID(&gID);
						if (gID == CLSID_DirectVobSubA)
						{
							//См. ниже..
							pGB->RemoveFilter(pBF);
							__pEF__->Reset();
						}
						else if (gID == CLSID_DirectVobSubM)
						{
							pDVS = pBF;
						}
					}

					if (!pDVS)
					{
						//A(uto)\M(anual) loading
						//Старый DVS_A не хочет ни с чем соединяться, если используется LAVSplitter (как при ConnectDirect, так и при Render).
						//Новый DVS_A (от xy) соединяется, но при ConnectDirect с LAVVideo Граф виснет при выгрузке. С DVS_M ничего такого не наблюдается.. 
						GUID gDVS = CLSID_DirectVobSubM; //(lav_decoder) ? CLSID_DirectVobSubM : CLSID_DirectVobSubA;

						int HaaliLoadDVS = -1;
						if (!lav_splitter && !lav_decoder)
						{
							CComQIPtr<IPropertyBag> pPB(pSrc);
							if (pPB)
							{
								CComVariant pVar(0u, VT_UI4); //uintVal = 0
								pPB->Read(L"vsfilter.autoload", &pVar, 0);
								if (pVar.uintVal > 0) //AutoLoad = true
								{
									CComPtr<IBaseFilter> pDummy;

									//Если мы не смогли - то и Haali Splitter тоже вряд-ли сможет
									HaaliLoadDVS = (FAILED(pDummy.CoCreateInstance(gDVS))) ? 0 : 1;
								}
							}
						}

						if (HaaliLoadDVS < 1)
						{
							if (HaaliLoadDVS == 0 || HaaliLoadDVS < 0 && FAILED(pDVS.CoCreateInstance(gDVS))) //Сначала пробуем системный..
							{
								if (FAILED(hr = LoadFilterFromFile(&pDVS, &hVSFilter, dvs_path, "VSFilter.dll", gDVS, err, ERRMSG_LEN))) { //..потом из dll
									SetError("Load VSFilter: ", err); return hr; }
							}

							if (FAILED(hr = pGB->AddFilter(pDVS, L"DirectVobSub"))) {
								SetError("Add DirectVobSub: ", err); return hr; }
						}
					}

					if (!lav_decoder)
					{
						//(тут Haali Splitter вставит в Граф DirectVobSub)
						if (FAILED(hr = pFG2->RenderEx(pSrc_Out, AM_RENDEREX_RENDERTOEXISTINGRENDERERS, NULL))) {
							SetError("RenderEx (video): ", err); return hr; }
					}
					else
					{
						//Соединяем (какой-то) первый входной пин DirectVobSub..
						CComPtr<IPin> pDVS_VIn(GetPin(pDVS, false, PINDIR_INPUT)); //DVS почему-то не выдаёт MediaType входного пина.. Или ENUM_MT криво работает?
						if (!pDVS_VIn) { SetError("GetPin (DVS_VIn): ", err); return E_FAIL; }

						//.. с видео выходом LAVVideo
						if (FAILED(hr = pGB->ConnectDirect(pLAVV_Out, pDVS_VIn, NULL))) {
							SetError("Connect direct (LAVV_Out + DVS_VIn): ", err); return hr; }

						//Соединяем (какой-то) второй входной пин DirectVobSub..
						CComPtr<IPin> pDVS_SIn(GetPin(pDVS, false, PINDIR_INPUT)); //Просто второй входной пин, хз как там оно устроено, но мы подключим к нему субтитры..
						if (!pDVS_SIn) { SetError("GetPin (DVS_SIn): ", err); return E_FAIL; }

						//.. с субтитровым выходом сплиттера
						if (FAILED(hr = pGB->ConnectDirect(pSrc_SOut, pDVS_SIn, NULL))) {
							SetError("Connect direct (Src_SOut + DVS_SIn): ", err); return hr; }

						//Соединяем выход DirectVobSub..
						CComPtr<IPin> pDVS_OutP(GetPin(pDVS, false, PINDIR_OUTPUT));
						if (!pDVS_OutP) { SetError("GetPin (DVS_Out): ", err); return E_FAIL; }

						//.. со входом VideoSink
						if (FAILED(hr = pGB->ConnectDirect(pDVS_OutP, pVS_In, NULL))) {
							SetError("Connect direct (DVS_Out + VS_In): ", err); return hr; }
					}
				}

				if (!lav_decoder)
				{
					if (FAILED(hr = pFG2->RenderEx(pSrc_SOut, AM_RENDEREX_RENDERTOEXISTINGRENDERERS, NULL))) {
						SetError("RenderEx (subs): ", err); return hr; }
				}
			}
		}

		CComQIPtr<IMediaControl> mc(pGB);
		if (!mc) { SetError("Get IMediaControl: ", err); return E_NOINTERFACE; }

		CComQIPtr<IMediaSeeking> ms(pGB);
		if (!ms) { SetError("Get IMediaSeeking: ", err); return E_NOINTERFACE; }

		if (FAILED(hr = mc->Run())) {
			SetError("Run FilterGraph: ", err); return hr; }

		OAFilterState fs;
		if (FAILED(hr = mc->GetState(2000, &fs))) {
			SetError("GetState: ", err); return hr; }

		// wait for the first frame to arrive
		if (WaitForSingleObject(m_hFrameReady, m_timeout) != WAIT_OBJECT_0) {
			SetError("Wait for FrameReady: ", err); return VFW_E_TIMEOUT; }

		__int64 defd;
		unsigned  type, width, height, arx, ary;
		if (FAILED(hr = sink2->GetFrameFormat(&type, &width, &height, &arx, &ary, &defd))) {
			SetError("GetFrameFormat: ", err); return hr; }

		REFERENCE_TIME duration, offset_tc, duration_tc;
		if (FAILED(hr = sink->GetTCOffset(&offset_tc, &duration_tc))) {
			SetError("GetTCOffset: ", err); return hr; }

		if (FAILED(hr = ms->GetDuration(&duration))) {
			SetError("GetDuration: ", err); return hr; }

		bool still_picture = (duration == 0 && defd == 0 && duration_tc > 0);
		if (defd == 0 && duration_tc > 1)  //AvgTimePerFrame   (VIDEOINFOHEADER)
			defd = duration_tc;            //TimeEnd-TimeStart (IMediaSample::GetTime)

		//Auto fps
		if (fps <= 0)
		{
			if (still_picture)
			{
				fps = 25;
				fps_den = 1;
			}
			else if (defd > 0)
			{
				__int64 _fps = (__int64)(10000000000.0 / defd + 0.5); //Немного округлим..
				switch (_fps)
				{
				case 23976: fps = 24000; fps_den = 1001; break;   //23.97602
				case 24000: fps = 24; fps_den = 1; break;         //24
				case 25000: fps = 25; fps_den = 1; break;         //25
				case 29970: fps = 30000; fps_den = 1001; break;   //29.97002
				case 30000: fps = 30; fps_den = 1; break;         //30
				case 48000: fps = 48; fps_den = 1; break;         //48
				case 50000: fps = 50; fps_den = 1; break;         //50
				case 59940: fps = 60000; fps_den = 1001; break;   //59.94005
				case 60000: fps = 60; fps_den = 1; break;         //60
				case 10000: fps = 100; fps_den = 1; break;        //100
				case 11988: fps = 120000; fps_den = 1001; break;  //119.8801
				case 12000: fps = 120000; fps_den = 1; break;     //120
				default: fps = 10000000; fps_den = (unsigned int)defd;
				}
			}
			else
			{
				SetError("Can't determine the frame rate!\n", err);
				return E_FAIL;
			}
		}

		if (fps_den == 0)
		{
			//fps=xx.xxx (фактически уже не используется, только
			//если вдруг не удалось получить fps_num/fps_den)
			m_avgframe = floor(10000000ll / fps + 0.5);
			m_vi.SetFPS(10000000, (unsigned int)m_avgframe);
		}
		else
		{
			//fps_num/fps_den
			m_avgframe = 10000000ll / ((floor(fps) / fps_den));
			m_vi.SetFPS((unsigned int)fps, fps_den);
		}

		if (duration == 0 && !still_picture)
		{
			SetError("Can't determine the duration!\n", err);
			return E_FAIL;
		}

		if (offset_tc > duration)
		{
			offset_tc = max(duration - duration_tc, 0);
			sink->SetTCOffset(offset_tc);
		}

		duration -= offset_tc;
		if (duration < m_avgframe)
			duration = (__int64)m_avgframe;

		m_vi.num_frames = (int)((duration / m_avgframe) + 0.5);
		if (!m_f.SetCount(m_vi.num_frames))
		{
			SetError("Can't handle so many frames!\n", err);
			return E_OUTOFMEMORY;
		}

		switch (type)
		{
		case IVS_RGB32: m_vi.pixel_type = VideoInfo::CS_BGR32; break;
		case IVS_RGB24: m_vi.pixel_type = VideoInfo::CS_BGR24; break;
		case IVS_YUY2: m_vi.pixel_type = VideoInfo::CS_YUY2; break;
		case IVS_YV12: m_vi.pixel_type = VideoInfo::CS_YV12; break;
		default: { SetError("Unsupported colorspace!\n", err); return E_FAIL; }
		}

		//Crop (1 pixel), если требуется mod2
		m_vi.width = width - ((m_vi.pixel_type == VideoInfo::CS_YUY2 || m_vi.pixel_type == VideoInfo::CS_YV12) ? (width % 2) : 0);
		m_vi.height = height - ((m_vi.pixel_type == VideoInfo::CS_YV12) ? (height % 2) : 0);

		m_pR = sink;
		m_pGC = mc;
		m_pGS = ms;

		SetEvent(m_hFrameReady);

		RegROT();

		return S_OK;
	}

	void Close()
	{
		CComQIPtr<IVideoSink2> pVS2(m_pR);
		if (pVS2) pVS2->NotifyFrame(NULL);

		m_f.RemoveAll();

		UnregROT();

		m_pR.Release();
		m_pGC.Release();
		m_pGS.Release();

		if (!InterlockedDecrement(&RefCount))
		{
			//К этому моменту какие-то потоки из наших длл-ок всё ещё
			//могут быть в работе и использовать ресурсы. Вобщем костыль..
			if (hLAVSplitter || hLAVVideo || hVSFilter)
				Sleep(100);

			SAFE_FREELIBRARY(hLAVSplitter);
			SAFE_FREELIBRARY(hLAVVideo);
			SAFE_FREELIBRARY(hVSFilter);
		}
	}

	struct RFArg
	{
		DF        *df;
		VideoInfo *vi;
	};

	static void ReadFrame(__int64 timestamp, unsigned format, unsigned bpp, const unsigned char *frame, unsigned width, unsigned height, int stride, unsigned arx, unsigned ary, void *arg)
	{
		RFArg     *rfa = (RFArg *)arg;
		DF        *df = rfa->df;
		VideoInfo *vi = rfa->vi;

		df->timestamp = timestamp;

		int w_cp = min((int)width, vi->width);
		int h_cp = min((int)height, vi->height);

		if ((format == IVS_RGB32 && vi->pixel_type == VideoInfo::CS_BGR32) ||
			(format == IVS_RGB24 && vi->pixel_type == VideoInfo::CS_BGR24) ||
			(format == IVS_YUY2 && vi->pixel_type == VideoInfo::CS_YUY2))
		{
			BYTE *dst = df->frame->GetWritePtr();
			int  dstride = df->frame->GetPitch();

			w_cp *= bpp; //=dstride?

			if (format != IVS_YUY2)
			{
				dst += (h_cp - 1) * dstride;
				for (int y = 0; y < h_cp; ++y)
				{
					memcpy(dst, frame, w_cp);
					frame += stride;
					dst -= dstride;
				}
			}
			else
			{
				for (int y = 0; y < h_cp; ++y)
				{
					memcpy(dst, frame, w_cp);
					frame += stride;
					dst += dstride;
				}
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
		while (true)
		{
			//Ждем не дольше установленного лимита времени
			if (WaitForSingleObject(m_hFrameReady, m_timeout) != WAIT_OBJECT_0)
			{
				//Запоминаем номер кадра, получая который мы зависли
				m_do_not_trespass = fn;

				m_pR->Reset();
				return false;
			}

			DF df(env->NewVideoFrame(m_vi));
			RFArg arg = { &df, &m_vi };

			HRESULT hr = m_pR->ReadFrame(ReadFrame, &arg);
			if (FAILED(hr))
				return false;

			if (hr == S_FALSE) // EOF
			{
				//Запоминаем номер кадра, получая который мы дошли до EOF
				m_do_not_trespass = fn;

				m_pR->Reset();
				return false;
			}

			//Картинка последнего успешно декодированного кадра
			m_last_good_frame = df.frame;

			if (df.timestamp >= 0)
			{
				int frameno = (int)(df.timestamp / m_avgframe + 0.5);
				if (frameno >= 0 && frameno < (int)m_f.GetCount())
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
		m_qfirst(0),                //Номер кадра, с которого начинается кэш.
		m_qnext(0),                 //Номер текущего кадра + 1 (т.е. следующий кадр).
		m_qmax(10),                 //Макс. кол-во кадров, хранимых в кэше, а так-же размер кэшируемого "прыжка назад" при последовательном чтении в обратную сторону.
		m_seek_thr(100),            //Порог для сикинга вперёд. Если требуемый кадр дальше от текущего на эту величину - будет сикинг, иначе будем ползти покадрово.
		m_preroll(0),               //На сколько кадров "недосикивать" (т.е. отступ при сикинге), оставшееся будет пройдено покадрово - помогает выдать нужный кадр на "плохосикающихся" файлах.
		m_timeout(30000),           //Лимит времени для WaitForSingleObject (ожидание готовности m_hFrameReady).
		m_do_not_trespass(INT_MAX)  //Номер кадра, дальше которого двигаться нельзя (конец файла\потока).
	{                               //P.S. Актуальные дефолты перенесены отсюда в Create_DSS2()
		m_hFrameReady = CreateEvent(NULL, FALSE, FALSE, NULL);
		memset(&m_vi, 0, sizeof(m_vi));
	}

	~DSS2()
	{
		Close();
		CloseHandle(m_hFrameReady);
	}

	HRESULT OpenFile(const wchar_t *filename, char *err, double fps, unsigned int fps_den, int cache, int seekthr, int preroll, int subsm, const char *lavs, const char *lavd, const char *lavf_path, const char *dvs_path, unsigned int pixel_types, DWORD timeout, int tc_offset)
	{
		m_qmax = cache;
		m_seek_thr = seekthr;
		m_preroll = preroll;
		m_timeout = timeout;

		return Open(filename, err, fps, fps_den, subsm, lavs, lavd, lavf_path, dvs_path, pixel_types, tc_offset);
	}

	// IClip
	PVideoFrame __stdcall GetFrame(int n, IScriptEnvironment *env)
	{
		if (n < 0 || n >= (int)m_f.GetCount())
			env->ThrowError("DSS2: GetFrame: Requested frame number is out of range!");

		if (m_f[n].frame)
			return m_f[n].frame;

		// we need to seek to get this frame
		if (n < m_qfirst || n > m_qnext + m_seek_thr)
		{
			// clear buffered frames
			for (int i = m_qfirst; i < m_qnext; ++i)
				m_f[i].frame = NULL;

			// reset read postion
			if (n >= m_qfirst - m_qmax && n < m_qfirst) // assume we are scanning backwards (не дальше, чем на 10(m_qmax) кадров назад от начала кэша)
			{
				m_qfirst = n - m_qmax + 1;       //Отступ на -9 (m_qmax-1) кадров от требуемого, "лишние" кадры (от m_qfirst до n) будут кэшированы.
				if (m_qfirst < 0) m_qfirst = 0;  //Т.е. чтение назад идет не покадрово, а "прыжками" по 9-ть кадров, по мере надобности кадры выдаются из кэша.
				m_qnext = m_qfirst;
			}
			else
			{
				// just some random seek
				m_qfirst = m_qnext = n;
			}

			// seek the graph
			ResetEvent(m_hFrameReady);

			//Отступ (без кэширования "лишних" кадров)
			int pos_frame = m_qfirst - m_preroll;
			if (pos_frame < 0) pos_frame = 0;

			REFERENCE_TIME pos_time = (REFERENCE_TIME)(m_avgframe * pos_frame - 10001); // -1ms, account for typical timestamps rounding
			if (pos_time < 0) pos_time = 0;

			if (FAILED(m_pGS->SetPositions(&pos_time, AM_SEEKING_AbsolutePositioning, NULL, AM_SEEKING_NoPositioning)))
				env->ThrowError("DSS2: DirectShow seeking failed!");

			//Сброс защиты от "последнего кадра"
			m_do_not_trespass = INT_MAX;

			//Сброс последнего полученного изображения
			m_last_good_frame = NULL;
		}

		// we performed a seek or are reading sequentially
		while (true)
		{
			DF df;
			int frameno = n;

			//Если дальше нельзя или просто некуда - выдаем последний декодированный кадр
			if (n >= m_do_not_trespass || !NextFrame(env, df, frameno))
			{
				//Обновляем m_qnext, иначе через m_seek_thr кадров будет попытка сделать сикинг.
				//Это нужно для совсем кривых файлов, у которых duration определяется намного больше, чем есть на самом деле.
				if (m_qnext < (int)m_f.GetCount() - 1)
					m_qnext += 1;

				if (!m_last_good_frame)
				{
					//Если выдать нечего - создаем "серый" кадр (из DSS)
					m_last_good_frame = env->NewVideoFrame(m_vi);
					memset(m_last_good_frame->GetWritePtr(), 128, m_last_good_frame->GetPitch() * m_last_good_frame->GetHeight() +
						m_last_good_frame->GetPitch(PLANAR_U) * m_last_good_frame->GetHeight(PLANAR_U) * 2);
				}
				return m_last_good_frame;
			}

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
	void __stdcall GetAudio(void *buf, __int64 start, __int64 count, IScriptEnvironment *env) { memset(buf, 0, (size_t)m_vi.BytesFromAudioSamples(count)); }
	bool __stdcall GetParity(int n) { return true; }
#if defined(AVS_26) || defined(AVS_PLUS)
	int __stdcall SetCacheHints(int cachehints, int frame_range) { return 0; }
#else
	void __stdcall SetCacheHints(int cachehints, int frame_range) { }
#endif

	void SetError(const char *text, char *err)
	{
		if (strlen(err) > 0)
		{
			//Swap & append
			char err_temp[ERRMSG_LEN] = {0};
			strncpy_s(err_temp, ERRMSG_LEN, err, _TRUNCATE);
			strncpy_s(err, ERRMSG_LEN, text, _TRUNCATE);
			strncat_s(err, ERRMSG_LEN, err_temp, _TRUNCATE);
		}
		else
		{
			//Copy
			strncpy_s(err, ERRMSG_LEN, text, _TRUNCATE);
		}
	}
};

static AVSValue __cdecl Create_DSS2(AVSValue args, void*, IScriptEnvironment* env)
{
	if (args[0].ArraySize() != 1)
		env->ThrowError("DSS2: Only 1 filename currently supported!");

	const char *filename = args[0][0].AsString();
	if (filename == NULL || strlen(filename) == 0)
		env->ThrowError("DSS2: Filename expected!");

	double fps = max(args[1].AsFloat(0), 0);                                                     //fps        (>=0, def=0,auto)
	unsigned int fps_den = max(args[2].AsInt(0), 0);                                             //fps_den    (>=0, def=0)
	int cache = max(args[3].AsInt(10), 1);                                                       //cache      (>=1, def=10)
	int seekthr = max(args[4].AsInt(100), 1);                                                    //seekthr    (>=1, def=100)
	int preroll = max(args[5].AsInt(0), 0);                                                      //preroll    (>=0, def=0)
	int subsm = max(args[6].AsInt(0), 0);                                                        //subsm      (>=0, def=0)
	const char *lavs = args[7].AsString("");                                                     //lavs       (def="")
	const char *lavd = args[8].AsString("");                                                     //lavd       (def="")
	const char *lavf_path = args[9].AsString("LAVFilters");                                      //lavf_path  (def="LAVFilters")
	const char *dvs_path = args[10].AsString("");                                                //dvs_path   (def="")
	bool flipv = args[11].AsBool(false);                                                         //flipv      (def=false)
	bool fliph = args[12].AsBool(false);                                                         //fliph      (def=false)
	const char *pixel_type = args[13].AsString("");                                              //pixel_type (def="")
	DWORD timeout = max(args[14].AsInt(30), 0);                                                  //timeout    (>=0, def=30)
	int tc_offset = max(args[15].AsInt(0), -1);                                                  //tc_offset  (>=-1, def=0)

	if (fps > 0 && fps_den == 0)
	{
		try
		{
			//Пересчитываем fps=xx.xxx в num\den через AssumeFPS()
			AVSValue temp_args[3] = { 0, 0, 0 }; //length, width, height
			PClip temp_clip = env->Invoke("BlankClip", AVSValue(temp_args, 3)).AsClip();
			AVSValue temp_args2[2] = { temp_clip, fps }; //clip, fps
			temp_clip = env->Invoke("AssumeFPS", AVSValue(temp_args2, 2)).AsClip();
			fps = temp_clip->GetVideoInfo().fps_numerator;
			fps_den = temp_clip->GetVideoInfo().fps_denominator;
		}
		catch (IScriptEnvironment::NotFound) { }
		catch (AvisynthError) { }
	}

	unsigned int pixel_types = (IVS_RGB24|IVS_RGB32|IVS_YUY2|IVS_YV12);
	if (strlen(pixel_type) > 0)
	{
		if (_strcmpi(pixel_type, "YV12") == 0) pixel_types = IVS_YV12;
		else if (_strcmpi(pixel_type, "YUY2") == 0) pixel_types = IVS_YUY2;
		else if (_strcmpi(pixel_type, "RGB24") == 0) pixel_types = IVS_RGB24;
		else if (_strcmpi(pixel_type, "RGB32") == 0) pixel_types = IVS_RGB32;
		else if (_strcmpi(pixel_type, "RGB") == 0) pixel_types = (IVS_RGB24|IVS_RGB32);
		else env->ThrowError("DSS2: Valid values for \"pixel_type\" are (choose only one): \"YV12\", \"YUY2\", \"RGB24\", \"RGB32\" or \"RGB\"!");
	}

	timeout = (timeout == 0) ? INFINITE : (timeout * 1000);

	DSS2 *dss2 = new DSS2();
	char err[ERRMSG_LEN] = {0};

	HRESULT hr = dss2->OpenFile(CA2WEX<128>(filename, CP_ACP), err, fps, fps_den, cache, seekthr, preroll, subsm, lavs, lavd, lavf_path, dvs_path, pixel_types, timeout, tc_offset);
	if (FAILED(hr))
	{
		delete dss2;

		//Расшифровка HRESULT и формирование сообщения об ошибке (из DSS)
		char hr_err[MAX_ERROR_TEXT_LEN] = {0}; //string[1024]
		if (AMGetErrorTextA(hr, hr_err, MAX_ERROR_TEXT_LEN) > 0) //1023
			env->ThrowError("DSS2: Can't open \"%s\"\n\n%s(%08x) %s\n", filename, err, hr, hr_err);
		env->ThrowError("DSS2: Can't open \"%s\"\n\n%s(%08x) Unknown error.\n", filename, err, hr);
	}

	if (flipv || fliph)
	{
		PClip flipped = dss2;
		if (flipv) flipped = env->Invoke("FlipVertical", flipped).AsClip();
		if (fliph) flipped = env->Invoke("FlipHorizontal", flipped).AsClip();
		return flipped;
	}

	return dss2;
}

#if defined(AVS_26) || defined(AVS_PLUS)
const AVS_Linkage *AVS_linkage = 0;
extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit3(IScriptEnvironment* env, const AVS_Linkage* const vectors)
{
	AVS_linkage = vectors;
#else
extern "C" __declspec(dllexport) const char* __stdcall AvisynthPluginInit2(IScriptEnvironment* env)
{
#endif
	env->AddFunction("DSS2", "s+[fps]f[fps_den]i[cache]i[seekthr]i[preroll]i[subsm]i[lavs]s[lavd]s[lavf_path]s[dvs_path]s[flipv]b[fliph]b[pixel_type]s[timeout]i[tc_offset]i", Create_DSS2, 0);
	return "DSS2";
}
