﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Interop;
using System.Runtime.InteropServices;

namespace NotificationKit {
    /// <summary>
    /// Interaktionslogik für NKNotificationWindow.xaml
    /// </summary>
    public partial class NKNotificationWindow: Window {
        
        #region Window styles

        [Flags]
        public enum ExtendedWindowStyles {
            WS_EX_TOOLWINDOW = 0x00000080,
        }

        public enum GetWindowLongFields {
            GWL_EXSTYLE = (-20),
        }
        
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            //Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if(IntPtr.Size == 4) {
                //use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            } else {
                //use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if((result == IntPtr.Zero) && (error != 0)) {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        private static int IntPtrToInt32(IntPtr intPtr) {
            return unchecked((int)intPtr.ToInt64());
        }

        #endregion

        //private Dictionary<string, string> data;
        private NKNotification notification;
        private bool faded;
        //private bool notificationManagerClosedFlag;
        private NKNotificationManager manager;

        public Dictionary<string, string> Data {
            //get { return this.data; }
            set {
                if(value.ContainsKey("msg")){
                    this.notificationTextBlock.Text = this.modifyNotificationText(value["msg"]);
                }

                if(value.ContainsKey("level")) {
                    Int32 ilevel = Int32.Parse(value["level"]);
                    if(ilevel == 2) {
                        this.changeBackground(NKNotificationLevel.Critical);
                    }
                    if(ilevel == 1) {
                        this.changeBackground(NKNotificationLevel.Warning);
                    }
                }
            }
        }

        public NKNotification Notification { 
            //get;
            set {
                this.notificationTextBlock.Text = this.modifyNotificationText(value.Text);
                this.changeBackground(value.Level);
            }
        }

        public NKNotificationManager NotificationManager {
            get { return this.manager; }
            set { this.manager = value; }
        }

        /*
        public bool NotificationManagerClosedFlag {
            get { return this.notificationManagerClosedFlag; }
            set { this.notificationManagerClosedFlag = value; }
        }*/
        
        public NKNotificationWindow() {
            InitializeComponent();
            this.ShowActivated = false;
            this.ShowInTaskbar = false;
        }
        //deprecated
        public NKNotificationWindow(Dictionary<string,string> dataDict) : this() {
            InitializeComponent();
            //this.data = dataDict;
            if(dataDict.ContainsKey("msg")) {
                this.notificationTextBlock.Text = this.modifyNotificationText(dataDict["msg"]);
            }

            if(dataDict.ContainsKey("level")) {
                Int32 ilevel = Int32.Parse(dataDict["level"]);
                if(ilevel == 2) {
                    this.changeBackground(NKNotificationLevel.Critical);
                }
                if(ilevel == 1) {
                    this.changeBackground(NKNotificationLevel.Warning);
                }
            }
        }
        //deprecated
        public NKNotificationWindow(Dictionary<string, string> dataDict, NKNotificationManager manager) : this() {
            InitializeComponent();
            //this.data = dataDict;
            if(dataDict.ContainsKey("msg")) {
                this.notificationTextBlock.Text = this.modifyNotificationText(dataDict["msg"]);
            }

            if(dataDict.ContainsKey("level")) {
                Int32 ilevel = Int32.Parse(dataDict["level"]);
                if(ilevel == 2) {
                    this.changeBackground(NKNotificationLevel.Critical);
                }
                if(ilevel == 1) {
                    this.changeBackground(NKNotificationLevel.Warning);
                }
            }

            this.manager = manager;
        }

        public NKNotificationWindow(NKNotification notification) : this() {
            InitializeComponent();
            
            this.notificationTextBlock.Text = this.modifyNotificationText(notification.Text);
            this.changeBackground(notification.Level);

            this.notification = notification;
        }

        public NKNotificationWindow(NKNotification notification, NKNotificationManager manager) : this() {
            InitializeComponent();
            
            this.notificationTextBlock.Text = this.modifyNotificationText(notification.Text);
            this.changeBackground(notification.Level);
            
            this.notification = notification;
            this.manager = manager;
        }

        //event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Rect screen = System.Windows.SystemParameters.WorkArea;
            this.Left = screen.Right - this.Width - 10;
            this.Top = screen.Top + 10;
            this.faded = false;

            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            int extStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            extStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)extStyle);
        }

        private void Window_Closing(object sender, MouseButtonEventArgs e) {
            if(this.manager != null) {
                this.manager.ClosedFlag = true;
            }
            
            if(!this.faded) {
                this.faded = true;
                //e.Cancel = true;
                
                //DoubleAnimation animation = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(1));
                DoubleAnimation animation = new DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(200));
                animation.Completed += new EventHandler(this.animCompleted);
                
                this.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e) {
            if(this.manager != null) {
                //MessageBox.Show("notificationManagerClosedFlag = " + this.notificationManager.ClosedFlag.ToString());
                this.manager.ClosedFlag = true;
                //MessageBox.Show("notificationManagerClosedFlag = " + this.notificationManager.ClosedFlag.ToString());
            }

            this.Close();
        }

        private void animCompleted(object sender, EventArgs e) {
            this.Close();
        }

        //public methods

        public void FadedClose(double ms) {
            if(this.manager != null) {
                this.manager.ClosedFlag = true;
            }
            
            if(!this.faded) {
                this.faded = true;
                //e.Cancel = true;
                
                DoubleAnimation animation = new DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(ms));
                animation.Completed += new EventHandler(this.animCompleted);
                
                this.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        //private methods

        private string modifyNotificationText(string msg) {
            string newMsg = "";
            int modificationCount = 0;

            msg = msg.Trim();
            if(msg.Length > 210) {
                msg = msg.Substring(0, 210);
            }
            //MessageBox.Show(msg);

            while(msg.Length > 48 && modificationCount < 8) {
                if(msg.Substring(48, 1) == " ") {
                    newMsg = newMsg + msg.Substring(0, 48) + System.Environment.NewLine;
                    msg = msg.Substring(48).TrimStart(' ');
                } else if(msg.Substring(0, 48).Contains(" ") && msg.Substring(0, 48).LastIndexOf(" ") > 20) {
                    int lastWhitespace = msg.Substring(0, 48).LastIndexOf(" ");
                    newMsg = newMsg + msg.Substring(0, lastWhitespace) + System.Environment.NewLine;
                    msg = msg.Substring(lastWhitespace + 1);
                } else if(msg.Substring(0, 48).Contains(":") && msg.Substring(0, 48).LastIndexOf(":") > 20) {
                    int lastColon = msg.Substring(0, 48).LastIndexOf(":");
                    newMsg = newMsg + msg.Substring(0, lastColon) + System.Environment.NewLine;
                    msg = msg.Substring(lastColon).Trim();
                } else if(msg.Substring(0, 48).Contains("-") && msg.Substring(0, 48).LastIndexOf("-") > 20) {
                    int lastHypen = msg.Substring(0, 48).LastIndexOf("-");
                    newMsg = newMsg + msg.Substring(0, lastHypen) + System.Environment.NewLine;
                    msg = msg.Substring(lastHypen);
                } else if(msg.Substring(0, 48).Contains(".") && msg.Substring(0, 48).LastIndexOf(".") > 20) {
                    int lastPoint = msg.Substring(0, 48).LastIndexOf(".");
                    newMsg = newMsg + msg.Substring(0, lastPoint) + System.Environment.NewLine;
                    msg = msg.Substring(lastPoint);
                } else {
                    newMsg = newMsg + msg.Substring(0, 48) + System.Environment.NewLine;
                    msg = msg.Substring(48);
                }

                modificationCount++;
                //MessageBox.Show("newMsg: " + newMsg + System.Environment.NewLine + "msg: " + msg);
            }
            newMsg = newMsg + msg;
            //use of recursive function
            //MessageBox.Show(newMsg);
            return newMsg;
        }

        private bool changeBackground(NKNotificationLevel level) {
            if(level == NKNotificationLevel.Critical) {
                //this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,51,51));
                System.Windows.Media.Color lightRed = System.Windows.Media.Color.FromRgb(255, 61, 61);
                System.Windows.Media.Color red = System.Windows.Media.Color.FromRgb(255, 41, 41);
                //GradientStopCollection colors = new GradientStopCollection();
                this.Background = new System.Windows.Media.LinearGradientBrush(lightRed, red, 90);
                //255,26,26
                //this.Background = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF1A1A"));
                //System.Drawing.Color test = System.Drawing.ColorTranslator.FromHtml("FF1A1A");
            }

            if(level == NKNotificationLevel.Warning) {
                //this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 195, 77));
                System.Windows.Media.Color lightYellow = System.Windows.Media.Color.FromRgb(255, 205, 77);
                System.Windows.Media.Color yellow = System.Windows.Media.Color.FromRgb(255, 185, 57);
                //GradientStopCollection colors = new GradientStopCollection();
                this.Background = new System.Windows.Media.LinearGradientBrush(lightYellow, yellow, 90);
            }

            return true;
        }
        
    }
}
