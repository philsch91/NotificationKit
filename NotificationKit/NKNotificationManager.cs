﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Threading;

namespace NotificationKit {
    public class NKNotificationManager {

        private List<NKNotificationWindow> notifications = new List<NKNotificationWindow>();
        //private List<Dictionary<string,string>> notifications = new List<Dictionary<string,string>>();
        private List<NKNotificationWindow> activeNotifications = new List<NKNotificationWindow>();
        private Thread thread;
        private DispatcherTimer timer = null;
        private bool stopflag = false;
        private bool autoClose = true;
        private int maxNotificationCount = 30;
        private int displayCount = 0;
        private int timerPixelCnt = 0;
        private int activeNotificationsIndex = 0;
        private int index = 0;
        //private bool closedflag = false;
        //private bool closedflag;

        public NKNotificationManager() {
        }

        public NKNotificationManager(int maxNotificationCount) {
            this.maxNotificationCount = maxNotificationCount;
        }

        public bool Stopped {
            get { return this.stopflag; }
        }

        public int NotificationCount {
            get { return this.notifications.Count; }
        }

        public bool AutoClose {
            get { return this.autoClose; }
            set { this.autoClose = value; }
        }

        public bool ClosedFlag {
            get;
            //{ return this.closedflag; }
            set;
            //{ this.closedflag = value; }
        }

        public int DisplayCount {
            get {
                return this.displayCount;
            }
            set {
                this.displayCount = value;
            }
        }

        public bool AddNotification(NKNotificationWindow window){
        //public bool AddNotification(Dictionary<string,string> notif){
            //Consider nw.data["priority"]
            notifications.Add(window);
            return true;
        }

        public bool Start() {
            this.stopflag = false;
            if(this.AutoClose) {
                thread = new Thread(new ThreadStart(this.autoCloseThreadWork));
            } else {
                thread = new Thread(new ThreadStart(this.threadWork));
            }

            if(this.displayCount > 1) {
                timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 30);
                timer.Tick += new EventHandler(timer_Tick);
            }

            thread.Start();
            if(thread.ThreadState != ThreadState.Running) {
                return false;
            }
            return true;
        }

        public bool Stop() {
            //thread.Abort();
            this.stopflag = true;
            /*int i = 0;
            while(thread.ThreadState == ThreadState.Running) {
                i++;
            }*/
            return true;
        }

        private void timer_Tick(object sender, EventArgs e) {
            //MessageBox.Show("activeNotificationsIndex: " + this.activeNotificationsIndex.ToString() + Environment.NewLine
                //+"activeNotifications.Count: " + this.activeNotifications.Count);
            
            //NKNotificationWindow window = this.activeNotifications[index];
            NKNotificationWindow window = this.activeNotifications[this.activeNotificationsIndex];

            int pixelcnt = (int)window.Height + 4;
            int easeInOutPixels = 4 * 4;    //4px*4
            if(this.timerPixelCnt < easeInOutPixels || this.timerPixelCnt > (pixelcnt - easeInOutPixels) || (this.timerPixelCnt + 32) > (pixelcnt - 16)) {
                window.Top = window.Top + 4;
                this.timerPixelCnt = this.timerPixelCnt + 4;
            } else {
                window.Top = window.Top + 32;
                this.timerPixelCnt = this.timerPixelCnt + 32;
            }

            if(this.timerPixelCnt > pixelcnt) {
                //index = activeNotificationsIndex % this.activeNotifications.Count;
                //this.activeNotificationsIndex++;
                index++;
                if(index == this.activeNotifications.Count - (1+activeNotificationsIndex)) {
                    this.activeNotificationsIndex++;
                    index = 0;
                }
                this.timerPixelCnt = 0;
            }

            //if(this.activeNotificationsIndex == this.activeNotifications.Count) {
            if(this.activeNotificationsIndex == this.activeNotifications.Count-1) {
                this.activeNotificationsIndex = 0;
                this.index = 0;
                this.timer.Stop();
            }
        }

        private void autoCloseThreadWork(){
            while(!stopflag){
                if(this.notifications.Count==0) {
                    Thread.Sleep(10000);
                    continue;
                }

                //get notification
                NKNotificationWindow currentNotification = notifications[0];
                //Dictionary<string, string> nextNotificationData = notifications[0];
                notifications.RemoveAt(0);

                //display notification
                //Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                Application.Current.Dispatcher.Invoke(new Action(() => {
                    currentNotification.ShowActivated = false;
                    currentNotification.Show();
                    //currentNotification.Owner = Application.Current.MainWindow;
                }));

                if((this.notifications.Count>0 || this.activeNotifications.Count>0) && this.timer!=null && this.displayCount > 1) {
                    //MessageBox.Show("notifications.Count: " + this.notifications.Count + " activeNotifications.Count: " + this.activeNotifications.Count.ToString());
                    this.activeNotifications.Add(currentNotification);
                    int cnt=0;
                    //int cnt<=displayCount
                    //cnt<=1
                    while(this.notifications.Count>0 && cnt<=1) {
                        //System.Windows.MessageBox.Show("dispatcherTimer");
                        currentNotification = this.notifications[0];
                        //display notification
                        //Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            currentNotification.ShowActivated = false;
                            currentNotification.Show();
                            //currentNotification.Owner = Application.Current.MainWindow;
                        }));
                        this.activeNotifications.Add(currentNotification);
                        this.notifications.RemoveAt(0);
                        cnt++;
                    }
                    if(this.timer.IsEnabled == false) {
                        //Main Thread
                        this.timer.Start();
                    }
                }
                
                //autoCloseThread
                Thread.Sleep(10000);
                if(Application.Current == null) {
                    continue;
                }

                //close notification
                int ms = 1000;
                if(notifications.Count > 0) {
                    ms = 500;
                }   
                
                if(this.activeNotifications.Count > 0 && this.timer != null && this.displayCount > 1) {
                    int closeIndex=0;
                    //MessageBox.Show("activeNotifications.Count: " + this.activeNotifications.Count.ToString());
                    while(closeIndex < this.activeNotifications.Count){
                        //Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        //sync Dispatcher.Invoke because of Clear() after loop
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            //currentNotification.FadedClose(500);
                            //currentNotification.Close();
                            //MessageBox.Show("index: "+closeIndex.ToString());
                            if(this.activeNotifications[closeIndex] != null) {
                                this.activeNotifications[closeIndex].FadedClose(ms);
                            }
                        }));
                        closeIndex++;
                    }
                    this.activeNotifications.Clear();
                 } else {
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                    //Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        //currentNotification.Close();
                        if(currentNotification != null) {
                            currentNotification.FadedClose(ms);
                        }
                    }));
                    //sleep because of async Dispatcher.BeginInvoke
                    //Thread.Sleep(ms);
                }
                
                /*
                }else{
                    //if(this.activeNotifications.Count
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        currentNotification.FadedClose(1000);
                    }));
                }*/

                if(notifications.Count > maxNotificationCount) {
                    notifications.Clear();
                    
                    //Dictionary<string, string> data = new Dictionary<string, string>(2);
                    //data["level"] = "3";
                    //data["msg"] = "More than " + maxNotificationCount.ToString() + " notifications in memory - memory cleared";

                    NKNotification notification = new NKNotification();
                    notification.Text = DateTime.Now.ToString() + " More than " + maxNotificationCount.ToString() + " notifications in memory - memory cleared";
                    notification.Level = NKNotificationLevel.Warning;

                    //NKNotificationWindow nw = new NKNotificationWindow(data, this);
                    //this.notifications.Add(nw);
                    
                    //Application.Current.Dispatcher.BeginInvoke(new Action(() =>{
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        //NKNotificationWindow nw = new NKNotificationWindow("More than 30 Notifications in memory");
                        //NKNotificationWindow nw = new NKNotificationWindow(data, this.ClosedFlag);
                        //NKNotificationWindow nw = new NKNotificationWindow(data,this);
                        NKNotificationWindow nw = new NKNotificationWindow(notification, this);
                        this.notifications.Add(nw);
                        
                        /*nw.ShowActivated = false;
                        nw.Show();*/
                        //nw.FadedClose(1000);
                    }));
                }

                //Thread.Sleep(10000);
                //End of infinite loop
            }
            //End of autoCloseThreadWork()
        }

        private void threadWork() {
            //this.closedflag = true;
            this.ClosedFlag = true;
            while(!this.stopflag) {
                if(this.notifications.Count > maxNotificationCount) {
                    notifications.Clear();
                    
                    //Dictionary<string, string> data = new Dictionary<string, string>(2);
                    //data["level"] = "3";
                    //data["msg"] = DateTime.Now.ToString() + " More than " + maxNotificationCount.ToString() + " notifications in memory - memory cleared";

                    NKNotification notification = new NKNotification();
                    notification.Text = DateTime.Now.ToString() + " More than " + maxNotificationCount.ToString() + " notifications in memory - memory cleared";
                    notification.Level = NKNotificationLevel.Warning;
                    
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        //NKNotificationWindow nw = new NKNotificationWindow("More than 30 Notifications in memory");
                        //NKNotificationWindow nw = new NKNotificationWindow(data, this);
                        NKNotificationWindow nw = new NKNotificationWindow(notification, this);
                        nw.ShowActivated = false;
                        nw.Show();
                        //nw.FadedClose(1000);
                    }));

                    this.ClosedFlag = false;
                    continue;
                }

                if(this.NotificationCount == 0 || this.ClosedFlag == false) {
                    Thread.Sleep(10000);
                    continue;
                }
                
                NKNotificationWindow currentNotification = notifications[0];
                notifications.RemoveAt(0);
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    currentNotification.ShowActivated = false;
                    currentNotification.Show();
                }));
                this.ClosedFlag = false;
            }
            //End of threadWork()
        }
    }
}
