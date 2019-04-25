﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NotificationKit {
    public class NKNotification {
        public string Text { get; set; }
        //public int Level { get; set; }
        public NKNotificationLevel Level { get; set; }

        public NKNotification(string text, NKNotificationLevel level) {
            this.Text = text;
            this.Level = level;
        }
    }
}
