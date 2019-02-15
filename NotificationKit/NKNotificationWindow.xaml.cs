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
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if(IntPtr.Size == 4) {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            } else {
                // use SetWindowLongPtr
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

        private Dictionary<string, string> data;
        private NKNotification notification;
        private bool faded;
        //private bool notificationManagerClosedFlag;
        private NKNotificationManager notificationManager;

        public Dictionary<string, string> Data {
            get { return this.data; }
            //set;
        }

        /*public NotificationManager NotificationManager {
            get { return this.notificationManager; }
            set { this.notificationManager = value; }
        }*/

        /*
        public bool NotificationManagerClosedFlag {
            get { return this.notificationManagerClosedFlag; }
            set { this.notificationManagerClosedFlag = value; }
        }*/
        
        /*
        public NKNotificationWindow() {
            InitializeComponent();
        }*/
        
        public NKNotificationWindow(Dictionary<string,string> ndata) {
            InitializeComponent();
            this.data = ndata;
            this.notificationTextBlock.Text = this.modifyNotificationText(this.data["msg"]);
            this.changeBackground(ndata["level"]);
        }

        public NKNotificationWindow(NKNotification notification) {
            InitializeComponent();
            this.notification = notification;
            this.notificationTextBlock.Text = this.modifyNotificationText(notification.Text);
            this.changeBackground(notification.Level.ToString());
        }

        public NKNotificationWindow(Dictionary<string, string> ndata, NKNotificationManager manager) {
            InitializeComponent();
            this.data = ndata;
            this.notificationTextBlock.Text = this.modifyNotificationText(this.data["msg"]);
            this.changeBackground(ndata["level"]);
            this.notificationManager = manager;
        }

        public NKNotificationWindow(NKNotification notification, NKNotificationManager manager) {
            InitializeComponent();
            this.notification = notification;
            this.notificationTextBlock.Text = this.modifyNotificationText(notification.Text);
            this.changeBackground(notification.Level.ToString());
            this.notificationManager = manager;
        }

        /*
        public NotificationWindow(Dictionary<string, string> _data, bool _notificationManagerClosedFlag) {
            InitializeComponent();
            this.data = _data;
            this.NotificationManagerClosedFlag = _notificationManagerClosedFlag;
            this.notificationTextBlock.Text = modifyNotificationText(this.data["msg"]);
            changeBackground(_data["level"]);
        }*/

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Rect screen = System.Windows.SystemParameters.WorkArea;
            this.Left = screen.Right - this.Width - 10;
            this.Top = screen.Top + 10;
            faded = false;

            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            int extStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            extStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)extStyle);
        }

        private void Window_Closing(object sender, MouseButtonEventArgs e) {
            this.notificationManager.ClosedFlag = true;
            if(!faded) {
                faded = true;
                //e.Cancel = true;
                //DoubleAnimation animation = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(1));
                DoubleAnimation animation = new DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(200));
                animation.Completed += new EventHandler(animCompleted);
                this.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

        public void FadedClose(double ms) {
            if(!faded) {
                faded = true;
                //e.Cancel = true;
                DoubleAnimation animation = new DoubleAnimation(0, (Duration)TimeSpan.FromMilliseconds(ms));

                animation.Completed += new EventHandler(animCompleted);
                this.BeginAnimation(UIElement.OpacityProperty, animation);
            }
        }

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

        private bool changeBackground(string level) {
            if(level == "3") {
                //this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255,51,51));
                System.Windows.Media.Color lightRed = System.Windows.Media.Color.FromRgb(255, 61, 61);
                System.Windows.Media.Color red = System.Windows.Media.Color.FromRgb(255, 41, 41);
                //GradientStopCollection colors = new GradientStopCollection();
                this.Background = new System.Windows.Media.LinearGradientBrush(lightRed, red, 90);
                //255,26,26
                //this.Background = new System.Windows.Media.SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF1A1A"));
                //System.Drawing.Color test = System.Drawing.ColorTranslator.FromHtml("FF1A1A");
            }
            if(level == "2") {
                //this.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 195, 77));
                System.Windows.Media.Color lightYellow = System.Windows.Media.Color.FromRgb(255, 205, 77);
                System.Windows.Media.Color yellow = System.Windows.Media.Color.FromRgb(255, 185, 57);
                //GradientStopCollection colors = new GradientStopCollection();
                this.Background = new System.Windows.Media.LinearGradientBrush(lightYellow, yellow, 90);
            }

            return true;
        }

        private void okButton_Click(object sender, RoutedEventArgs e) {
            //MessageBox.Show("notificationManagerClosedFlag = " + this.notificationManager.ClosedFlag.ToString());
            //this.NotificationManagerClosedFlag = true;
            this.notificationManager.ClosedFlag = true;
            //MessageBox.Show("notificationManagerClosedFlag = " + this.notificationManager.ClosedFlag.ToString());
            this.Close();
        }

        private void animCompleted(object sender, EventArgs e) {
            this.Close();
        }
    }
}