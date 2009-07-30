﻿#pragma checksum "..\..\..\windows\VideoEncoding.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "67EE17673A9D5FF6FC327439BC24C6B3"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.3053
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Integration;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace XviD4PSP {
    
    
    /// <summary>
    /// VideoEncoding
    /// </summary>
    public partial class VideoEncoding : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 6 "..\..\..\windows\VideoEncoding.xaml"
        internal XviD4PSP.VideoEncoding Window;
        
        #line default
        #line hidden
        
        
        #line 10 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Grid LayoutRoot;
        
        #line default
        #line hidden
        
        
        #line 18 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Grid grid_panel;
        
        #line default
        #line hidden
        
        
        #line 29 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.ComboBox combo_codec;
        
        #line default
        #line hidden
        
        
        #line 30 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_size;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_outsize_value;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_insize_value;
        
        #line default
        #line hidden
        
        
        #line 33 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_codec;
        
        #line default
        #line hidden
        
        
        #line 34 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_incodec_value;
        
        #line default
        #line hidden
        
        
        #line 35 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_quality;
        
        #line default
        #line hidden
        
        
        #line 36 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_outquality_value;
        
        #line default
        #line hidden
        
        
        #line 37 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_inquality_value;
        
        #line default
        #line hidden
        
        
        #line 39 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Grid grid_codec;
        
        #line default
        #line hidden
        
        
        #line 40 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Grid grid_profiles;
        
        #line default
        #line hidden
        
        
        #line 47 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.ComboBox combo_profile;
        
        #line default
        #line hidden
        
        
        #line 48 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Label text_profile;
        
        #line default
        #line hidden
        
        
        #line 49 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Button button_add;
        
        #line default
        #line hidden
        
        
        #line 50 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Button button_remove;
        
        #line default
        #line hidden
        
        
        #line 52 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Grid grid_main_buttons;
        
        #line default
        #line hidden
        
        
        #line 56 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Button button_cancel;
        
        #line default
        #line hidden
        
        
        #line 57 "..\..\..\windows\VideoEncoding.xaml"
        internal System.Windows.Controls.Button button_ok;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/XviD4PSP;component/windows/videoencoding.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\windows\VideoEncoding.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.Window = ((XviD4PSP.VideoEncoding)(target));
            return;
            case 2:
            this.LayoutRoot = ((System.Windows.Controls.Grid)(target));
            return;
            case 3:
            this.grid_panel = ((System.Windows.Controls.Grid)(target));
            return;
            case 4:
            this.combo_codec = ((System.Windows.Controls.ComboBox)(target));
            
            #line 29 "..\..\..\windows\VideoEncoding.xaml"
            this.combo_codec.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.combo_codec_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 5:
            this.text_size = ((System.Windows.Controls.Label)(target));
            return;
            case 6:
            this.text_outsize_value = ((System.Windows.Controls.Label)(target));
            return;
            case 7:
            this.text_insize_value = ((System.Windows.Controls.Label)(target));
            return;
            case 8:
            this.text_codec = ((System.Windows.Controls.Label)(target));
            return;
            case 9:
            this.text_incodec_value = ((System.Windows.Controls.Label)(target));
            return;
            case 10:
            this.text_quality = ((System.Windows.Controls.Label)(target));
            return;
            case 11:
            this.text_outquality_value = ((System.Windows.Controls.Label)(target));
            return;
            case 12:
            this.text_inquality_value = ((System.Windows.Controls.Label)(target));
            return;
            case 13:
            this.grid_codec = ((System.Windows.Controls.Grid)(target));
            return;
            case 14:
            this.grid_profiles = ((System.Windows.Controls.Grid)(target));
            return;
            case 15:
            this.combo_profile = ((System.Windows.Controls.ComboBox)(target));
            
            #line 47 "..\..\..\windows\VideoEncoding.xaml"
            this.combo_profile.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(this.combo_profile_SelectionChanged);
            
            #line default
            #line hidden
            return;
            case 16:
            this.text_profile = ((System.Windows.Controls.Label)(target));
            return;
            case 17:
            this.button_add = ((System.Windows.Controls.Button)(target));
            
            #line 49 "..\..\..\windows\VideoEncoding.xaml"
            this.button_add.Click += new System.Windows.RoutedEventHandler(this.button_add_Click);
            
            #line default
            #line hidden
            return;
            case 18:
            this.button_remove = ((System.Windows.Controls.Button)(target));
            
            #line 50 "..\..\..\windows\VideoEncoding.xaml"
            this.button_remove.Click += new System.Windows.RoutedEventHandler(this.button_remove_Click);
            
            #line default
            #line hidden
            return;
            case 19:
            this.grid_main_buttons = ((System.Windows.Controls.Grid)(target));
            return;
            case 20:
            this.button_cancel = ((System.Windows.Controls.Button)(target));
            
            #line 56 "..\..\..\windows\VideoEncoding.xaml"
            this.button_cancel.Click += new System.Windows.RoutedEventHandler(this.button_cancel_Click);
            
            #line default
            #line hidden
            return;
            case 21:
            this.button_ok = ((System.Windows.Controls.Button)(target));
            
            #line 57 "..\..\..\windows\VideoEncoding.xaml"
            this.button_ok.Click += new System.Windows.RoutedEventHandler(this.button_ok_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}
