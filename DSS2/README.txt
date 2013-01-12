avss (DSS2) modded by fcp, version 2.0.0.10; LAVFilters settings interface version 0.55.1.


DSS2(string filename, float "fps", int "cache", int "seekthr", int "preroll", int "subsm", string "lavs", string "lavd",
     string "lavf_path", string "dvs_path", bool "flipv", bool "fliph", int "timeout")


1. CP_OEMCP replaced with CP_ACP, to fix codepage issue.
2. Added advanced error messages, HRESULT codes translated to "human-friendly" text (like in DSS).
3. Haali Video Sink - is now part of DSS2, so Haali Media Splitter is not required anymore (but you still may want to use it as a splitter).
   Also DSS2 will not hide Haali tray icon anymore - you can always disable it in the splitter settings.
4. Some changes to prevent hangs on the last frames or on incomplete\broken streams, both in VideoSink and in DSS2. In worst case, you will get
   a pause about 30sec long (see "timeout"), but no more hangs (I hope so)!
5. Store last decoded frame data and output it when decoding of next frame is not possible; or output a grey frame (like in DSS),
   if decoding isn't possible at all.
6. Exposed some settings (they was hardcoded in DSS2, but in some cases you may want to tweak them):
   6.1 "cache" (int, default = 10, if < 1 rounded to 1) - cache size in frames. Mostly useful for backward (almost)linear reading.
   6.2 "seekthr" (int, default = 100, if < 1 rounded to 1) - forward seeking threshold. If "requested_frame - current_frame > seekthr",
       we will seek the Graph to this position, otherwise we will read frame-by-frame all the frames, untill we get frame No. that we want.
7. Added "preroll" setting (int, default = 0, if < 0 rounded to 0). Since DirectShow seeking isn't frame-accurate (or time-accurate,
   as there is no frames, only time), when we perform a seeking to calculated time position, in some cases we can "overseek" or "overjump"
   requested position. Also with some decoders or files you may see alot of artifacts after seeking (has something to do with GOP and keyframes).
   "preroll" - it's a number of frames that will be minused from the requested frame No. when calculating desired time position for seeking.
   All "extra-frames" will be read frame-by-frame till we get what we want. Or in other words: we want frame No. 100 and preroll=10;
   seeking will be performed, as if we need frame No. 90; frames from 90 (or where we will be after seeking, since it isn't accurate most of the time)
   to 100 will be safely readed frame-by-frame. If after seeking you can see artifacts or frozen frames for some time - you may want to increase preroll
   value to 10, 20,... or probably 100 and even more (for ts\m2ts files). Or repack your file to something more seekable. Or use DGDecNV.
8. Added "subsm" setting (int, default = 0, if < 0 rounded to 0):
   0 = do not load subs (or "old method of building the Graph").
   1 = try to Render the first pin with MEDIATYPE_Subtitle or MEDIATYPE_Text, if GrapthBuilder can't render this pin - you will get an error.
   2+ = 1 + force loading of DirectVobSub to the Graph. If not installed (failed to CoCreateInstance) - it will be loaded directly from VSFilter.dll.
        If Haali Media Splitter is used with "Autoload VSFilter = Yes" - DSS2 will not load DirectVobSub, assuming that it will be loaded by Haali.
9. Added "lavs" and "lavd" options (string, default = ""). When not an empty string (not a ""), LAVSplitter and\or LAVVideo will be used.
   You can also pass some settings to LAVFilters via this keys.
   9.1 Available options for LAVSplitter and default values are "l3 vc2 sm2 sl[] sa[] ti0", where:
       l3 (l - Load, loading mode, from 0 to 3):
          0 = load LAVSplitter from system (must be installed\registered), do not apply custom settings (all other settings below will be ignored).
          1 = load LAVSplitter directly from LAVSplitter.ax, do not apply custom settings. If not installed, LAVSplitter will use default settings,
              otherwise it will use settings from the installed version. All other settings below will be ignored.
          2 = 0 + apply custom settings. In this mode LAVSplitter will ignore all the settings from the installed version.
          3 = 1 + apply custom settings. In this mode LAVSplitter will ignore all the settings from the installed version.
       vc2 (vc-1, VC1Fix, from 0 to 2):
          0 = "No Timestamp Correction"
          1 = "Always Timestamp Correction"
          2 = "Auto (Correction for Decoders that need it)"
       sm2 (sm - Subs Mode, from 0 to 3):
          0 = "NoSubs"
          1 = "ForcedOnly"
          2 = "Default"
          3 = "Advanced"
       sl[] (Subs Languages, string that contain subtitle preferred languages as ISO 639-2 language codes, empty by default, sl[write_something_here])
       sa[] (Subs Advanced, string with values for Advanced mode, empty by default, sa[write_something_here])
       ti0 (ti - Tray Icon, from 0 to x):
          0 = off
          1+ = on
   9.2 Available options for LAVVideo and default values are "l3 t0 r0 d1 dm0 fo0 sd0 vc1 hm0 hc7 hr3 hd0 hq0 ti0", where:
       l3 (l - Load, loading mode, from 0 to 3):
          0 = load LAVVideo from system (must be installed\registered), do not apply custom settings (all other settings below will be ignored).
          1 = load LAVVideo directly from LAVVideo.ax, do not apply custom settings. If not installed, LAVVideo will use default settings,
              otherwise it will use settings from the installed version. All other settings below will be ignored.
          2 = 0 + apply custom settings. In this mode LAVVideo will ignore all the settings from the installed version.
          3 = 1 + apply custom settings. In this mode LAVVideo will ignore all the settings from the installed version.
       t0 (t - Threads, from 0 to xx):
          0 = Auto
          1 = Disable MT
          x = x threads, if supported by decoder
       r0 (r - output Range for YUV->RGB conversion, from 0 to 2):
          0 = Auto (same as input)
          1 = Limited (16-235)
          2 = Full (0-255)
       d1 (d - Dither, from 0 to 1):
          0 = Ordered pattern
          1 = Random pattern
       dm0 (d - Deinterlace Mode, from 0 to 3):
          0 = Disabled
          1 = Auto
          2 = Auto (aggressive)
          3 = Always on
       fo0 (fo - Field Order for deinterlacing, from 0 to 2):
          0 = Auto
          1 = TFF
          2 = BFF
       sd0 (sd - Software Deinterlacer, from 0 to 2):
          0 = None
          1 = Yadif
          2 = Yadif x2 fps (but it seems not working, fps remains the same)
       vc1 (vc - VC-1 decoding, from 0 to x):
          0 = do not use MS WMV9 DMO Decoder for decoding VC-1\WMV3
          1+ = use MS WMV9 DMO Decoder for decoding VC-1\WMV3
       hm0 (hm - Hardware acceleration Mode, from 0 to 2):
          0 = None
          1 = CUDA
          2 = QuickSink
       hc7 (hc - Hardware Codecs, from 0 to 15):
          1 = H264
          2 = VC-1
          4 = MPEG2
          8 = MPEG4
          up to 15 - a bitwise-encoded combination (a summ of required values), for example: 7 (1+2+4) = H264, VC-1 and MPEG2 will be enabled, but MPEG4 will not
       hr3 (hc - Hardware Resolutions, from 0 to 7):
          1 = SD ( <= 1024x576)
          2 = HD ( <= 1980x1200)
          4 = UHD ( > 1980x1200)
          up to 7 - a bitwise-encoded combination (a summ of required values), for example: 3 (1+2) = SD and HD will be enabled, but UHD will not
       hd0 (hd - Hardware Deinterlacer, from 0 to 2):
          0 = Weave (none)
          1 = Adaptive
          2 = Adaptive x2 fps
       hq0 (hq - HQ hardware deinterlace (Vista+), from 0 to x):
          0 = off
          1+ = on
       ti0 (ti - Tray Icon, from 0 to x):
          0 = off
          1+ = on

    About options parser. "from 0 to 3" - means that if value > 3 it will be rounded down to 3. However, negative values aren't supported.
    Case of the letters ignored (t0 = T0). The string also can looks like this: "l3t0r0d1dm0fo0sd0vc1hm0hc7hr3hd0hq0ti0" to save some space :) .
    And of course it is not necessary to add all this keys if you don't need to change the defaults. So if you only want to change N of threads,
    use lavd="t2", or lavd="dm1 hm1 hd1" to enable decoding of H264, VC-1 or MPEG2 with hardware deinterlacer in Auto mode.

    For more details about Subs Languages, Subs Advanced and some other settings - please refer to LAVFilters documentation.

    With subsm=1 and LAVVideo enabled, you will not get any subtitles: mode 1 works like mode 0 in this case. Also, when LAVVideo is enabled,
    to prevent us from DS-garbage (some filters that auto-insert themself into the Graph) a ConnectDirect method will be used, except for splitter,
    if it's not LAVSplitter. So if LAVSplitter+LAVVideo is enabled, you will get 100% guarantee that there is no other filters in the Graph that will
    perform deinterlacing, colorspace conversion or any kind of other stupid things, that wasn't requested by you!

    Enabling tray icons actually will not give you the ability to change any settings. If LAVFilters isn't installed, you won't be able to open
    PropertyPage at all - nothing will happen when you click on the icon. But when installed, PropertyPage from the installed version will be loaded!

10. Added "lavf_path" and "dvs_path" (string, default for the first one is "LAVFilters", and "" for the second one) - you can specify a path
    (relative or absolute) to LAVFilters .ax files and to VSFilter.dll accordingly. By default, LAVFilters will be loaded from \LAVFilters subfolder
    in the same folder where avss.dll is placed (and was loaded from); VSFilter.dll by default will be loaded from the same folder where avss.dll
    is placed. If .ax or .dll should be loaded, but wasn't found, you will get an error message. If you loading several instances of DSS2 in one
    script (or, more correctly, in one process) - you can't set different values for this settings. Only one value of several will be used - the one,
    that was loaded the first, all other will be ignored.

11. Added "flipv" and "fliph" settings (bool, false by default) - when set to true, DSS2 will internally Invoke FlipVertical() or\and FlipHorizontal()
    AviSynth functions. Also it seems like RGB processing in DSS2 was flipped vertically, at least comparing with DSS. Besides RGB32, RGB24 is now
    also allowed, not sure why it wasn't..

12. Added "timeout" setting (int, default = 30, if < 0 rounded to 0) - maximum amount of time when waiting for the decoded frame, in seconds.
    0 = INFINITE, but in this case DSS2 in some situations may hangs again on broken streams.