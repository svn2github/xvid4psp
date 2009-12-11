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
            button_refresh.Content = Languages.Translate("Apply");
            button_refresh.ToolTip = Languages.Translate("Refresh preview");

            text_resolution.Content = m.inresw + "x" + m.inresh;
            text_source_res.Content = Languages.Translate("Input resolution:");
            text_final_res.Content = Languages.Translate("Output resolution:");
            text_resizer.Content = Languages.Translate("Resize filter:");
            text_aspectfix.Content = Languages.Translate("Aspect adjusting method:");
            text_inaspect.Content = Languages.Translate("Input aspect:");
            text_outaspect.Content = Languages.Translate("Output aspect:");
            text_crop_tb.Content = Languages.Translate("Crop top, bottom:");
            text_crop_lr.Content = Languages.Translate("Crop left, right:");
            combo_crop_b.ToolTip = Languages.Translate("Bottom");
            combo_crop_t.ToolTip = Languages.Translate("Top");
            combo_crop_l.ToolTip = Languages.Translate("Left");
            combo_crop_r.ToolTip = Languages.Translate("Right");
            text_black.Content = Languages.Translate("Black width, height:");
            combo_black_h.ToolTip = Languages.Translate("Height");
            combo_black_w.ToolTip = Languages.Translate("Width");
            text_autocropsens.Content = Languages.Translate("Autocrop sensivity:");
            text_autocropframes.Content = Languages.Translate("Frames to analyze:");
            text_aspecterror.Content = Languages.Translate("Aspect error:");
            text_recalculate.Content = Languages.Translate("Recalculate aspect when crop is using:");
            button_analyse.ToolTip = Languages.Translate("Autocrop black borders");
            button_vcrop.ToolTip = Languages.Translate("Crop black borders manually");
            button_analyse.Content = Languages.Translate("Auto crop");
            button_vcrop.Content = Languages.Translate("Manual crop");
            combo_height.ToolTip = Languages.Translate("Height");
            combo_width.ToolTip = Languages.Translate("Width");
            tab_main.Header = Languages.Translate("Main");
            tab_settings.Header = Languages.Translate("Settings");
            Title = Languages.Translate("Resolution/Aspect");
            textbox_error.Text = "";
            text_ffmpeg_ar.Content = Languages.Translate("Use FFmpeg AR info:");
            text_ffmpeg_ar.ToolTip = check_use_ffmpeg_ar.ToolTip = Languages.Translate("MediaInfo provides rounded values, so for better precision it`s recommended to use AR info from the FFmpeg");

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
            combo_width.SelectedItem = m.outresw;

            //высота
            combo_height.Items.Clear();
            ArrayList reshlist = Format.GetResHList(m);
            foreach (int r in reshlist)
                combo_height.Items.Add(r);
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
                
                //Считаем чистый исходный аспект (без учета откропленного)
                double inputasp = ((double)mass.inresw / (double)mass.inresh) * mass.pixelaspect; 
                if (mass.inaspect < mass.outaspect)
                {
                    //double diff = mass.inresh - (mass.inresw / mass.outaspect);
                    
                    //Считаем сколько нужно откропить, с учетом аспекта
                    double diff = mass.inresh - (int)(((double)mass.inresh * inputasp) / mass.outaspect);
                    mass.cropt = Calculate.GetValid((int)(diff / 2) + mass.cropt_copy, 2);
                    mass.cropb = Calculate.GetValid((int)(diff / 2) + mass.cropb_copy, 2);
                }
                else if (mass.inaspect > mass.outaspect)
                {
                    //double diff = mass.inresw - (mass.inresh * mass.outaspect);
                    
                    //Считаем сколько нужно откропить, с учетом аспекта
                    double diff = mass.inresw - (int)((double)mass.inresw / inputasp) * mass.outaspect;                    
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
            for (int n = 0; n < (m.inresw / 2); n += 2)
            {
                combo_crop_l.Items.Add(n);
                combo_crop_r.Items.Add(n);
            }
            combo_crop_l.SelectedItem = m.cropl;
            combo_crop_r.SelectedItem = m.cropr;
            for (int n = 0; n < (m.inresh / 2); n += 2)
            {
                combo_crop_t.Items.Add(n);
                combo_crop_b.Items.Add(n);
            }
            combo_crop_b.SelectedItem = m.cropb;
            combo_crop_t.SelectedItem = m.cropt;
        }

        private void LoadBlack()
        {
            for (int n = 0; n < (m.outresw / 2); n += 2)
            {
                combo_black_w.Items.Add(n);
            }
            combo_black_w.SelectedItem = m.blackw;
            for (int n = 0; n < (m.outresh / 2); n += 2)
            {
                combo_black_h.Items.Add(n);
            }
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
	}
}