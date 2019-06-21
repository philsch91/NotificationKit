# NotificationKit

Create and display notifications to the user

#### Classes
- NKNotification
- NKNotificationWindow
- NKNotificationManager

#### Enums
- NKNotificationLevel

~~~~
//create and start an instance of NKNotificationManager
//that automatically creates, displays and closes one or more NKNotificationWindows

NKNotificationManager manager = new NKNotificationManager();
manager.Start();

//create a notification with a message that gets displayed

NKNotification notification = new Notification("notification text", NKNotificationLevel.Warning);

//hand the NKNotification over to the NKNotificationManager

manager.AddNotification(notification);
~~~~





