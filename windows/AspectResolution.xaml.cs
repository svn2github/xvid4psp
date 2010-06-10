using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;

namespace XviD4PSP
{
	public partial class AspectResolution
	{
        public Massive m;
        private Massive oldm;
        private MainWindow p;

        public enum AspectFixes { Disabled = 1, SAR, Crop, Black }
        public enum GetResolutionModes { Maximum = 1, Optimal }

        public AspectResolution(Massive mass, MainWindow parent)
		{
			this.InitializeComponent();

            m = mass.Clone();
            oldm = mass.Clone();
            p = parent;
            Owner = p;

            //переводим
            button_cancel.Content = Languages.Translate("Cancel");
            button_ok.Content = Languages.Translate("OK");
            button_refresh.Content = button_manual_apply.Content = text_manual_apply.Content = Languages.Translate("Apply");
            button_refresh.ToolTip = Languages.Translate("Refresh preview");

            text_resolution.Content = m.inresw + "x" + m.inresh;
            text_source_res.Content = Languages.Translate("Input resolution:");
            text_final_res.Content = text_manual_res.Content = Languages.Translate("Output resolution:");
            text_resizer.Content = Languages.Translate("Resize filter:");
            text_aspectfix.Content = Languages.Translate("Aspect adjusting method:");
            text_inaspect.Content = Languages.Translate("Input aspect:");
            //text_outaspect.Content = Languages.Translate("Output aspect:");
            text_manual_outasp.Content = (text_outaspect.Content = Languages.Translate("Output aspect:")) + " | ";
            text_crop_tb.Content = text_manual_crop_tb.Content = Languages.Translate("Crop top, bottom:");
            text_crop_lr.Content = text_manual_crop_lr.Content = Languages.Translate("Crop left, right:");
            text_black.Content = text_manual_black.Content = Languages.Translate("Black width, height:");
            combo_crop_b.ToolTip = manual_crop_b.ToolTip = Languages.Translate("Bottom");
            combo_crop_t.ToolTip = manual_crop_t.ToolTip = Languages.Translate("Top");
            combo_crop_l.ToolTip = manual_crop_l.ToolTip = Languages.Translate("Left");
            combo_crop_r.ToolTip = manual_crop_r.ToolTip = Languages.Translate("Right");
            combo_black_h.ToolTip = manual_black_h.ToolTip = Languages.Translate("Height");
            combo_black_w.ToolTip = manual_black_w.ToolTip = Languages.Translate("Width");
            text_autocropsens.Content = Languages.Translate("Autocrop sensivity:");
            text_autocropframes.Content = Languages.Translate("Frames to analyze:");
            text_aspecterror.Content = Languages.Translate("Aspect error:");
            text_recalculate.Content = Languages.Translate("Recalculate aspect when crop is using:");
            button_analyse.ToolTip = Languages.Translate("Autocrop black borders");
            button_vcrop.ToolTip = Languages.Translate("Crop black borders manually");
            button_analyse.Content = group_autocrop.Header = Languages.Translate("Auto crop");
            button_vcrop.Content = group_visualcrop.Header = Languages.Translate("Manual crop");
            combo_modw.ToolTip = Languages.Translate("Width") + "\r\n" + Languages.Translate("Default: 16") + "\r\n" + Languages.Translate("Values XX are strongly NOT recommended!").Replace("XX", "4, 8");
            combo_modh.ToolTip = Languages.Translate("Height") + "\r\n" + Languages.Translate("Default: 8") + "\r\n" + Languages.Translate("Values XX are strongly NOT recommended!").Replace("XX", "2, 4");
            tab_main.Header = Languages.Translate("Main");
            tab_settings.Header = Languages.Translate("Settings");
            Title = Languages.Translate("Resolution/Aspect");
            textbox_error.Text = "";
            text_ffmpeg_ar.Content = Languages.Translate("Use FFmpeg AR info:");
            text_ffmpeg_ar.ToolTip = check_use_ffmpeg_ar.ToolTip = Languages.Translate("MediaInfo provides rounded values, so for better precision it`s recommended to use AR info from the FFmpeg");
            text_visualcrop_opacity.Content = Languages.Translate("Background opacity:");
            text_visualcrop_brightness.Content = Languages.Translate("Brightness of the mask:");
            text_visualcrop_frame.Content = Languages.Translate("Startup frame:");
            text_mod.Content = Languages.Translate("Allow resolutions divisible by:");
            manual_outaspect.ToolTip = Languages.Translate("In case of non-anamorphic encoding: Aspect = Width/Height.") +
                "\r\n" + Languages.Translate("In case of anamorphic encoding: Aspect = (Width/Height)*SAR.");
            manual_outsar.ToolTip = Languages.Translate("Leave it empty for non-anamorphic encoding.") +
                "\r\n" + Languages.Translate("For anamorphic encoding you must specify SAR.");
            button_calc_sar.ToolTip = Languages.Translate("Calculate SAR for specified output resolution and aspect.") +
                "\r\n" + Languages.Translate("It must be used for anamorphic encoding only!"); ;
            
            //ресайзеры
            foreach (string resizer in Enum.GetNames(typeof(AviSynthScripting.Resizers)))
                combo_resizer.Items.Add(resizer);
            combo_resizer.SelectedItem = m.resizefilter.ToString();

            for (int n = 0; n < 101; n++)
                combo_autocropsens.Items.Add(n);
            combo_autocropsens.SelectedItem = Settings.AutocropSensivity;

            for (int n = 5; n < 31; n++)
                combo_autocropframes.Items.Add(n);
            combo_autocropframes.SelectedItem = Settings.AutocropFrames;

            for (int n = 0; n < 10; n ++)
                combo_visualcrop_opacity.Items.Add(n);
            combo_visualcrop_opacity.SelectedItem = Settings.VCropOpacity;

            for (int n = 0; n < 26; n++)
                combo_visualcrop_brightness.Items.Add(n);
            combo_visualcrop_brightness.SelectedItem = Settings.VCropBrightness;

            combo_visualcrop_frame.Items.Add("THM-frame");
            combo_visualcrop_frame.Items.Add("1-st frame");
            combo_visualcrop_frame.SelectedItem = Settings.VCropFrame;

            //ModW-ограничение
            for (int n = 4; n <= 16; n *= 2) combo_modw.Items.Add(n);
            combo_modw.SelectedItem = Settings.LimitModW;
            
            //ModH-ограничение
            for (int n = 2; n <= 16; n *= 2) combo_modh.Items.Add(n);
            combo_modh.SelectedItem = Settings.LimitModH;

            if (m.format == Format.ExportFormats.Avi || m.format == Format.ExportFormats.Mkv || m.format == Format.ExportFormats.Mov
                || m.format == Format.ExportFormats.Mp4)
            {
                combo_modw.IsEnabled = combo_modh.IsEnabled = true;
            }

            check_recalculate_aspect.IsChecked = Settings.RecalculateAspect;
            check_use_ffmpeg_ar.IsChecked = Settings.UseFFmpegAR;

            //аспект фиксы
            foreach (string afix in Enum.GetNames(typeof(AspectFixes)))
                combo_aspectfix.Items.Add(afix);
            combo_aspectfix.SelectedItem = m.aspectfix.ToString();

            //Разрешения
            LoadResolutions();

            //входной аспект
            LoadInAspect();

            //обрезка
            LoadCrop();

            //поля
            LoadBlack();

            //загружаем выходной аспект
            LoadOutAspect();

            CalculateMod();

            FillManualBox();

            ShowDialog();
		}

        private void button_ok_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (p.m.inaspect != m.inaspect ||
                p.m.outaspect != m.outaspect ||
                p.m.outresw != m.outresw ||
                p.m.outresh != m.outresh ||
                p.m.cropl != m.cropl ||
                p.m.cropr != m.cropr ||
                p.m.cropb != m.cropb ||
                p.m.cropt != m.cropt ||
                p.m.blackw != m.blackw ||
                p.m.blackh != m.blackh ||
                p.m.aspectfix != m.aspectfix)
                m = AviSynthScripting.CreateAutoAviSynthScript(m);
            Close();
        }

        private void button_cancel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m = oldm.Clone();
            Close();
        }

        private void button_refresh_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            m = AviSynthScripting.CreateAutoAviSynthScript(m);
            p.m = m.Clone();
            p.Refresh(m.script);
        }

        private void combo_resizer_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_resizer.IsDropDownOpen || combo_resizer.IsSelectionBoxHighlighted)
            {
                Settings.ResizeFilter = (AviSynthScripting.Resizers)Enum.Parse(typeof(AviSynthScripting.Resizers), combo_resizer.SelectedItem.ToString());
                m.resizefilter = Settings.ResizeFilter;
                Refresh();
            }
        }

        private void combo_aspectfix_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_aspectfix.IsDropDownOpen || combo_aspectfix.IsSelectionBoxHighlighted)
            {
                m.aspectfix = (AspectFixes)Enum.Parse(typeof(AspectFixes), combo_aspectfix.SelectedItem.ToString());

                m = FixAspectDifference(m); 

                LoadCrop();
                LoadBlack();

                LoadInAspect();
                textbox_error.Text = Calculate.ConvertDoubleToPointString(100 - ((m.outaspect * 100) / m.inaspect), 2) + "%";

                Refresh();
            }
        }

        private void combo_outaspect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_outaspect.IsDropDownOpen || combo_outaspect.IsSelectionBoxHighlighted)
            {
                string outasp = combo_outaspect.SelectedItem.ToString();

                if (m.aspectfix == AspectFixes.SAR)
                {
                    m.outaspect = Calculate.ConvertStringToDouble(outasp);
                    m = Calculate.CalculateSAR(m);
                }
                //else if (m.aspectfix == AspectFixes.Crop ||
                //    m.aspectfix == AspectFixes.Black)
                //{

                //    m.outaspect = Calculate.ConvertStringToDouble(outasp);
                //}
                else
                {
                    double oldinaspect = m.inaspect;
                    m.inaspect = Calculate.ConvertStringToDouble(outasp);
                    //m = Format.GetValidResolution(m);

                    //запоминаем разрешение для сравнения
                    int outresw = m.outresw;

                    m = Format.GetValidResolution(m, m.outresw);
                    m.inaspect = oldinaspect;

                    if (outresw != m.outresw)
                        m.outresw = outresw;
                    
                    m.outaspect = Calculate.ConvertStringToDouble(outasp);

                    combo_width.SelectedItem = m.outresw;
                    combo_height.SelectedItem = m.outresh;
                }
               
                m = FixAspectDifference(m);

                LoadBlack();
                LoadCrop();
                combo_aspectfix.SelectedItem = m.aspectfix.ToString();

                //пересчет ошибки аспекта
                textbox_error.Text = Calculate.ConvertDoubleToPointString(100 - ((m.outaspect * 100) / m.inaspect), 2) + "%";
                
                Refresh();
            }
        }

        private void combo_inaspect_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_inaspect.IsDropDownOpen || combo_inaspect.IsSelectionBoxHighlighted)
            {
                m.inaspect = Calculate.ConvertStringToDouble(combo_inaspect.SelectedItem.ToString());

                m = Format.GetValidResolution(m);
                m = Format.GetValidOutAspect(m);        
                m = FixAspectDifference(m);

                combo_aspectfix.SelectedItem = m.aspectfix.ToString();
                LoadOutAspect();
                LoadResolutions();
                LoadBlack();
                LoadCrop();

                Refresh();
            }
        }

        private void combo_width_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_width.IsDropDownOpen || combo_width.IsSelectionBoxHighlighted)
            {
                m.outresw = Convert.ToInt32(combo_width.SelectedItem);

                //запоминаем разрешение для сравнения
                int outresw = m.outresw;
                
                m = Format.GetValidResolution(m, m.outresw); //пересчет высоты
                
                if (outresw != m.outresw)
                {
                    m.outresw = outresw;
                    m = Format.GetValidOutAspect(m);
                }
                else //тут
                {
                    m = Format.GetValidOutAspect(m);
                }

                m = FixAspectDifference(m);
         
                LoadOutAspect();
                LoadBlack();
                LoadCrop();

                combo_aspectfix.SelectedItem = m.aspectfix.ToString();//
                combo_height.SelectedItem = m.outresh;
                
                Refresh();
            }
        }

        private void combo_height_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_height.IsDropDownOpen || combo_height.IsSelectionBoxHighlighted)
            {
                m.outresh = Convert.ToInt32(combo_height.SelectedItem);

                if (m.aspectfix != AspectFixes.SAR)
                    m = Format.GetValidOutAspect(m);

                m = FixAspectDifference(m);

                if (m.aspectfix != AspectFixes.SAR)
                    LoadOutAspect();

                combo_aspectfix.SelectedItem = m.aspectfix.ToString();//
             
                Refresh();
            }
        }

        private void LoadInAspect()
        {
            string inaspect = Calculate.ConvertDoubleToPointString(m.inaspect, 4);

            combo_inaspect.Items.Clear();

            //подбираем наиболее подходящий аспект из стандартных аспектов
            string[] aspects = Calculate.InsertAspect(new string[] { "1.3333 (4:3)", "1.7778 (16:9)", "1.8500", "2.3529" }, inaspect);
            foreach (string _asp in aspects)
                combo_inaspect.Items.Add(_asp);
            combo_inaspect.SelectedItem = Calculate.GetClosePointDouble(inaspect, aspects);
        }

        private void LoadOutAspect()
        {
            try
            {
                string outaspect = Calculate.ConvertDoubleToPointString(m.outaspect, 4);
                string inaspect = Calculate.ConvertDoubleToPointString(m.inaspect, 4);

                combo_outaspect.Items.Clear();

                //подбираем наиболее подходящий аспект из стандартных аспектов
                string[] aspects = Calculate.InsertAspect(Format.GetValidOutAspects(m), outaspect);

                //только для форматов со свободным аспектом
                if (!Format.IsLockedOutAspect(m))
                    aspects = Calculate.InsertAspect(aspects, inaspect);
                else
                    aspects = Format.GetValidOutAspects(m);
                
                foreach (string _asp in aspects)
                    combo_outaspect.Items.Add(_asp);

                string closeaspect = Calculate.GetClosePointDouble(outaspect, aspects);
                //if (m.IsAnamorphic && closeaspect == "1.333 (4:3)")
                //    combo_outaspect.SelectedItem = "Anamorphic (4:3)";
                //else if (m.IsAnamorphic && closeaspect == "1.778 (16:9)")
                //    combo_outaspect.SelectedItem = "Anamorphic (16:9)";
                //else if (m.IsAnamorphic && closeaspect == "2.353")
                //    combo_outaspect.SelectedItem = "Anamorphic (2.353)";
                //else
                    combo_outaspect.SelectedItem = closeaspect;
                
                //ошибка в выходном аспекте
                textbox_error.Text = Calculate.ConvertDoubleToPointString(100 - ((m.outaspect * 100) / m.inaspect), 2) + "%";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadResolutions()
        {
            //ширина
            combo_width.Items.Clear();
            ArrayList reswlist = Format.GetResWList(m);
            foreach (int r in reswlist)
                combo_width.Items.Add(r);
            if (!combo_width.Items.Contains(m.outresw)) combo_width.Items.Add(m.outresw);
            combo_width.SelectedItem = m.outresw;

            //высота
            combo_height.Items.Clear();
            ArrayList reshlist = Format.GetResHList(m);
            foreach (int r in reshlist)
                combo_height.Items.Add(r);
            if (!combo_height.Items.Contains(m.outresh)) combo_height.Items.Add(m.outresh);
            combo_height.SelectedItem = m.outresh;
        }

        private void combo_crop_t_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_crop_t.IsDropDownOpen || combo_crop_t.IsSelectionBoxHighlighted)
            {
                m.cropt = Convert.ToInt32(combo_crop_t.SelectedItem);
                m.cropt_copy = m.cropt;

                if (Settings.RecalculateAspect == true)
                {
                    m = FixInputAspect(m);//
                    m = Format.GetValidOutAspect(m);
                    LoadInAspect();//
                    LoadOutAspect();//
                }

                Refresh();
            }
        }

        private void combo_crop_b_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_crop_b.IsDropDownOpen || combo_crop_b.IsSelectionBoxHighlighted)
            {
                m.cropb = Convert.ToInt32(combo_crop_b.SelectedItem);
                m.cropb_copy = m.cropb;

                if (Settings.RecalculateAspect == true)
                {
                    m = FixInputAspect(m);//
                    m = Format.GetValidOutAspect(m);
                    LoadInAspect();//
                    LoadOutAspect();//
                }

                combo_aspectfix.SelectedItem = m.aspectfix.ToString();//
                Refresh();
            }
        }

        private void combo_crop_l_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_crop_l.IsDropDownOpen || combo_crop_l.IsSelectionBoxHighlighted)
            {
                m.cropl = Convert.ToInt32(combo_crop_l.SelectedItem);
                m.cropl_copy = m.cropl;

                if (Settings.RecalculateAspect == true)
                {
                    m = FixInputAspect(m);//
                    m = Format.GetValidOutAspect(m);
                    LoadInAspect();//
                    LoadOutAspect();//
                }             
               
                combo_aspectfix.SelectedItem = m.aspectfix.ToString();//
                Refresh();
            }
        }

        private void combo_crop_r_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_crop_r.IsDropDownOpen || combo_crop_r.IsSelectionBoxHighlighted)
            {
                m.cropr = Convert.ToInt32(combo_crop_r.SelectedItem);
                m.cropr_copy = m.cropr;

                if (Settings.RecalculateAspect == true)
                {
                    m = FixInputAspect(m);//
                    m = Format.GetValidOutAspect(m);
                    LoadInAspect();//
                    LoadOutAspect();//
                }             
               
                combo_aspectfix.SelectedItem = m.aspectfix.ToString();//
                Refresh();
            }
        }

        private void combo_black_w_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_black_w.IsDropDownOpen || combo_black_w.IsSelectionBoxHighlighted)
            {
                m.blackw = Convert.ToInt32(combo_black_w.SelectedItem);
                Refresh();
            }
        }

        private void combo_black_h_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (combo_black_h.IsDropDownOpen || combo_black_h.IsSelectionBoxHighlighted)
            {
                m.blackh = Convert.ToInt32(combo_black_h.SelectedItem);
                Refresh();
            }
        }

        private void button_fullscreen_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            p.SwitchToFullScreen(); 
        }

        public static Massive FixInputAspect(Massive mass)
        {
            //правим входной аспект с учётом кропа

            //подправляем аспект
            //if (mass.cropb_copy == 0 &&
            //    mass.cropl_copy == 0 &&
            //    mass.cropr_copy == 0 &&
            //    mass.cropt_copy == 0)
            //{
            //  
            //}
            //else
            {
                mass.inaspect = ((double)(mass.inresw - mass.cropl_copy - mass.cropr_copy) /
                    (double)(mass.inresh - mass.cropt_copy - mass.cropb_copy)) * mass.pixelaspect;
            }
            return mass;
        }

        private void Refresh()
        {
            m = AviSynthScripting.CreateAutoAviSynthScript(m);
            p.m = m.Clone();
            p.Refresh(m.script);

            CalculateMod();
            FillManualBox();
        }

        public static Massive FixAspectDifference(Massive mass)
        {
            mass.blackh = 0;
            mass.blackw = 0;
            mass.cropb = mass.cropb_copy;
            mass.cropl = mass.cropl_copy;
            mass.cropr = mass.cropr_copy;
            mass.cropt = mass.cropt_copy;

            //правим по чёрному
            if (mass.aspectfix == AspectFixes.Black)
            {
                //mass.sar = null;
                
                if (mass.inaspect < mass.outaspect)
                {
                    double diff = ((double)mass.outresw - ((double)mass.outresw / (double)mass.outaspect) * (double)mass.inaspect);
                    mass.blackw = Calculate.GetValid((int)(diff / 2), 2);
                    mass.blackh = 0;
                }
                else if (mass.inaspect > mass.outaspect)
                {
                    double diff = (((double)mass.outresw / (double)mass.outaspect) - ((double)mass.outresw / (double)mass.inaspect));
                    mass.blackw = 0;
                    mass.blackh = Calculate.GetValid((int)(diff / 2), 2);
                }
            }
            else if (mass.aspectfix == AspectFixes.Crop)
            {
                //mass.sar = null;
                
                //Считаем чистый исходный аспект (без учета откропленного) ..или все-же с учетом..
                double inputasp = ((double)(mass.inresw - mass.cropl - mass.cropr) / (double)(mass.inresh - mass.cropt - mass.cropb)) * mass.pixelaspect;
                //((double)mass.inresw / (double)mass.inresh) * mass.pixelaspect; 
                
                if (mass.inaspect < mass.outaspect)
                {
                    //Считаем сколько нужно откропить, с учетом аспекта и уже откропленного
                    double diff = (mass.inresh - mass.cropt - mass.cropb) - (int)(((double)(mass.inresh - mass.cropt - mass.cropb) * inputasp) / mass.outaspect);
                    mass.cropt = Calculate.GetValid((int)(diff / 2) + mass.cropt_copy, 2);
                    mass.cropb = Calculate.GetValid((int)(diff / 2) + mass.cropb_copy, 2);
                }
                else if (mass.inaspect > mass.outaspect)
                {
                    //Считаем сколько нужно откропить, с учетом аспекта и уже откропленного
                    double diff = (mass.inresw - mass.cropl - mass.cropr) - (int)((double)(mass.inresw - mass.cropl - mass.cropr) / inputasp) * mass.outaspect;
                    mass.cropl = Calculate.GetValid((int)(diff / 2) + mass.cropl_copy, 2);
                    mass.cropr = Calculate.GetValid((int)(diff / 2) + mass.cropr_copy, 2);
                }
                
                //Входной аспект с учетом откропленного
                if (Settings.RecalculateAspect)
                    mass.inaspect = ((double)(mass.inresw - mass.cropl - mass.cropr) / (double)(mass.inresh - mass.cropt - mass.cropb)) * mass.pixelaspect;
            }
            else if (mass.aspectfix == AspectFixes.SAR)
            {
                mass = Calculate.CalculateSAR(mass);
            }
            else if (mass.aspectfix == AspectFixes.Disabled)
            {
                //mass.sar = null;
            }

            return mass;
        }

        private void LoadCrop()
        {
            combo_crop_l.Items.Clear();
            combo_crop_r.Items.Clear();
            combo_crop_t.Items.Clear();
            combo_crop_b.Items.Clear();
            
            for (int n = 0; n < (m.inresw / 2); n += 2)
            {
                combo_crop_l.Items.Add(n);
                combo_crop_r.Items.Add(n);
            }
            for (int n = 0; n < (m.inresh / 2); n += 2)
            {
                combo_crop_t.Items.Add(n);
                combo_crop_b.Items.Add(n);
            }

            if (!combo_crop_l.Items.Contains(m.cropl)) combo_crop_l.Items.Add(m.cropl);
            if (!combo_crop_r.Items.Contains(m.cropr)) combo_crop_r.Items.Add(m.cropr);
            if (!combo_crop_t.Items.Contains(m.cropt)) combo_crop_t.Items.Add(m.cropt);
            if (!combo_crop_b.Items.Contains(m.cropb)) combo_crop_b.Items.Add(m.cropb);

            combo_crop_l.SelectedItem = m.cropl;
            combo_crop_r.SelectedItem = m.cropr;
            combo_crop_t.SelectedItem = m.cropt;
            combo_crop_b.SelectedItem = m.cropb;
        }

        private void LoadBlack()
        {
            combo_black_w.Items.Clear();
            combo_black_h.Items.Clear();
            
            for (int n = 0; n < (m.outresw / 2); n += 2)
            {
                combo_black_w.Items.Add(n);
            }
            for (int n = 0; n < (m.outresh / 2); n += 2)
            {
                combo_black_h.Items.Add(n);
            }

            if (!combo_black_w.Items.Contains(m.blackw)) combo_black_w.Items.Add(m.blackw);
            if (!combo_black_h.Items.Contains(m.blackh)) combo_black_h.Items.Add(m.blackh);

            combo_black_w.SelectedItem = m.blackw;
            combo_black_h.SelectedItem = m.blackh;
        }

        private void combo_autocropsens_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_autocropsens.IsDropDownOpen || combo_autocropsens.IsSelectionBoxHighlighted)
            {
                Settings.AutocropSensivity = (int)combo_autocropsens.SelectedItem;
            }
        }

        private void combo_autocropframes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_autocropframes.IsDropDownOpen || combo_autocropframes.IsSelectionBoxHighlighted)
            {
                Settings.AutocropFrames = (int)combo_autocropframes.SelectedItem;
            }
        }

        private void check_recalculate_aspect_Click(object sender, RoutedEventArgs e)
        {
            Settings.RecalculateAspect = check_recalculate_aspect.IsChecked.Value;
        }

        private void button_analyse_Click(object sender, RoutedEventArgs e)
        {
            Autocrop acrop = new Autocrop(m, this);
            m = acrop.m.Clone();
            ApplyCrop();
        }

        private void button_vcrop_Click(object sender, RoutedEventArgs e)
        {
            VisualCrop vcrop = new VisualCrop(m, this);
            if (m.cropl != vcrop.m.cropl || m.cropr != vcrop.m.cropr || m.cropt != vcrop.m.cropt || m.cropb != vcrop.m.cropb)
            {
                m = vcrop.m.Clone();
                ApplyCrop();
            }
        }

        private void ApplyCrop()
        {
            combo_crop_b.SelectedItem = m.cropb;
            combo_crop_l.SelectedItem = m.cropl;
            combo_crop_r.SelectedItem = m.cropr;
            combo_crop_t.SelectedItem = m.cropt;

            m = FixInputAspect(m);
            m = Format.GetValidResolution(m);
            m = Format.GetValidOutAspect(m);
            m = FixAspectDifference(m);

            LoadResolutions();
            LoadInAspect();
            LoadOutAspect();

            Refresh();
        }

        private void check_use_ffmpeg_ar_Click(object sender, RoutedEventArgs e)
        {
            Settings.UseFFmpegAR = check_use_ffmpeg_ar.IsChecked.Value;
        }

        private void combo_visualcrop_opacity_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_visualcrop_opacity.IsDropDownOpen || combo_visualcrop_opacity.IsSelectionBoxHighlighted)
            {
                Settings.VCropOpacity = (int)combo_visualcrop_opacity.SelectedItem;
            }
        }

        private void combo_visualcrop_brightness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_visualcrop_brightness.IsDropDownOpen || combo_visualcrop_brightness.IsSelectionBoxHighlighted)
            {
                Settings.VCropBrightness = (int)combo_visualcrop_brightness.SelectedItem;
            }
        }

        private void combo_visualcrop_frame_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_visualcrop_frame.IsDropDownOpen || combo_visualcrop_frame.IsSelectionBoxHighlighted)
            {
                if (combo_visualcrop_frame.IsDropDownOpen || combo_visualcrop_frame.IsSelectionBoxHighlighted)
                {
                    Settings.VCropFrame = combo_visualcrop_frame.SelectedItem.ToString();
                }
            }
        }

        private void combo_modw_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_modw.IsDropDownOpen || combo_modw.IsSelectionBoxHighlighted)
            {
                Settings.LimitModW = Convert.ToInt32(combo_modw.SelectedItem);
                ApplyCrop();
            }
        }
        
        private void combo_modh_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (combo_modh.IsDropDownOpen || combo_modh.IsSelectionBoxHighlighted)
            {
                Settings.LimitModH = Convert.ToInt32(combo_modh.SelectedItem);
                ApplyCrop();
            }
        }

        private void CalculateMod()
        {
            if (m.outresw % 16 == 0) combo_width.ToolTip = ":16 ";
            else if (m.outresw % 8 == 0) combo_width.ToolTip = ":8 ";
            else if (m.outresw % 4 == 0) combo_width.ToolTip = ":4 ";
            else if (m.outresw % 2 == 0) combo_width.ToolTip = ":2 ";
            else combo_width.ToolTip = ":1 ";
            combo_width.ToolTip += Languages.Translate("Width");
            manual_w.ToolTip = combo_width.ToolTip;

            if (m.outresh % 16 == 0) combo_height.ToolTip = ":16 ";
            else if (m.outresh % 8 == 0) combo_height.ToolTip = ":8 ";
            else if (m.outresh % 4 == 0) combo_height.ToolTip = ":4 ";
            else if (m.outresh % 2 == 0) combo_height.ToolTip = ":2 ";
            else combo_height.ToolTip = ":1 ";
            combo_height.ToolTip += Languages.Translate("Height");
            manual_h.ToolTip = combo_height.ToolTip;
        }

        private void FillManualBox()
        {
            manual_w.Text = m.outresw.ToString();
            manual_h.Text = m.outresh.ToString();
            manual_crop_l.Text = m.cropl.ToString();
            manual_crop_r.Text = m.cropr.ToString();
            manual_crop_t.Text = m.cropt.ToString();
            manual_crop_b.Text = m.cropb.ToString();
            manual_black_w.Text = m.blackw.ToString();
            manual_black_h.Text = m.blackh.ToString();
            manual_outsar.Text = m.sar;
            manual_outaspect.Text = Calculate.ConvertDoubleToPointString(m.outaspect, 4);
        }

        private void button_manual_apply_Click(object sender, RoutedEventArgs e)
        {
            int var;
            if (int.TryParse(manual_w.Text, out var)) m.outresw = var;
            if (int.TryParse(manual_h.Text, out var)) m.outresh = var;
            if (int.TryParse(manual_crop_l.Text, out var)) m.cropl = m.cropl_copy = var;
            if (int.TryParse(manual_crop_r.Text, out var)) m.cropr = m.cropr_copy = var;
            if (int.TryParse(manual_crop_t.Text, out var)) m.cropt = m.cropt_copy = var;
            if (int.TryParse(manual_crop_b.Text, out var)) m.cropb = m.cropb_copy = var;
            if (int.TryParse(manual_black_w.Text, out var)) m.blackw = var;
            if (int.TryParse(manual_black_h.Text, out var)) m.blackh = var;

            //Все-же немного автоматики.. смотрим, что вписано в SAR, и пересчитываем аспект под этот SAR.
            //Если в SAR пусто или недопустимое значение, то аспект = ширина/высота.
            if (manual_outsar.Text.Length > 2 && manual_outsar.Text.Contains(":"))
            {
                int n, d;
                string[] sar = manual_outsar.Text.Split(new string[] { ":" }, StringSplitOptions.None);
                if (sar.Length == 2 && int.TryParse(sar[0], out n) && int.TryParse(sar[1], out d))
                {
                    m.sar = n.ToString() + ":" + d.ToString();
                    manual_outaspect.Text = Calculate.ConvertDoubleToPointString((m.outaspect = ((double)m.outresw / (double)m.outresh) * ((double)n / (double)d)), 4);
                }
                else
                {
                    m.sar = "";
                    manual_outaspect.Text = Calculate.ConvertDoubleToPointString((m.outaspect = (double)m.outresw / (double)m.outresh), 4);
                }
            }
            else
            {
                m.sar = "";
                manual_outaspect.Text = Calculate.ConvertDoubleToPointString((m.outaspect = (double)m.outresw / (double)m.outresh), 4);
            }

            if (m.sar != "" && m.sar != "1:1") m.aspectfix = AspectFixes.SAR;
            else if (m.aspectfix == AspectFixes.SAR) m.aspectfix = AspectFixes.Disabled;
            combo_aspectfix.SelectedItem = m.aspectfix.ToString();

            if (Settings.RecalculateAspect)
                m.inaspect = ((double)(m.inresw - m.cropl - m.cropr) / (double)(m.inresh - m.cropt - m.cropb)) * m.pixelaspect;

            LoadResolutions();
            LoadInAspect();
            LoadOutAspect();
            LoadCrop();
            LoadBlack();

            Refresh();
        }

        private void button_calc_sar_Click(object sender, RoutedEventArgs e)
        {
            int w, h;
            double asp = 0;

            //Определяем требуемый аспект
            if (manual_outaspect.Text.Length > 2 && manual_outaspect.Text.Contains("/"))
            {
                int n, d;
                string[] a = manual_outaspect.Text.Split(new string[] { "/" }, StringSplitOptions.None);
                if (a.Length == 2 && int.TryParse(a[0], out n) && int.TryParse(a[1], out d))
                    asp = (double)n / (double)d;
                else asp = Calculate.ConvertStringToDouble(manual_outaspect.Text);
            }
            else asp = Calculate.ConvertStringToDouble(manual_outaspect.Text);

            if (int.TryParse(manual_w.Text, out w) && int.TryParse(manual_h.Text, out h) && asp > 0)
            {
                manual_outsar.Text = Calculate.CalculateSAR(w, h, asp);
            }
        }
	}
}