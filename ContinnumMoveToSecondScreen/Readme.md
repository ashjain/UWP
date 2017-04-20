# Continuum Move to Second Screen Sample UWP

This sample demonstrates how to move the UWP app from the mobile screen to the Second screen when the Windows mobile device is in continuum mode.

A UWP app running on Windows Mobile needs to be closed first and then re-launched from the second screen so that the app then runs on the second screen.   This may be problem for some of the apps, especially if the app is a productivity app that has a state which the app needs to preserve while moving to second screen.   This sample demonstrates how you can seamlessly transition the UWP experience from the mobile screen to the second screen in Continuum mode without closing the app on the mobile or relaunching it from the second screen, preserving the state of the running app as running.


Build/Deploy and Run the sample
-------------------------------

 - Build the UWP app and deploy on Windows Mobile
 - Run the app on Windows Mobile
 - Connect the device to Continuum dock (or use Connect app on Windows 10 (inbox app) and Connect/Miracast to the app)
 - Now click the "Move" button in the app
 - The mobile screen shows "I have moved to second screen" and the original app UI moves to the second screen