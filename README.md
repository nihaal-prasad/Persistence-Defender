# Persistence Defender Service

## Introduction

After obtaining access to a machine, threat actors generally seek to preserve their access to exploited machines in order to accomplish their goals over a period of time. This project aims to develop an open-source .NET application that will monitor locations in the Windows operating system that are used by threat actors as common persistence points and prevent them from being modified. This application will be run as a Windows service that prevents attackers from utilizing some of the most common avenues for storing persistence artifacts. In order to prevent disruptions to legitimate applications, programs that are already configured to be persistent on the system at the time of deployment will be considered to be whitelisted, and this service will only defend against programs that were configured to persist post-deployment. In doing so, this software enables system administrators to take a zero-trust approach with regard to application persistence.

## Details

This application will focus on monitoring the following common persistence points:
-	Scheduled Tasks
-	Startup Folder
-	Windows Services
-	PowerShell Profiles
-	Application Shims
-	BITS Jobs
-	Special Registry Keys

Whenever the application notices a modification in one of the above locations that indicates the use of persistence, the application will automatically prevent or undo the change unless it was whitelisted prior to deployment. In essence, this program will cause the above persistence points to become read-only. If a legitimate system administrator wishes to modify one of these locations after this service has been deployed, then a reboot will be necessary.

Because there are many legitimate applications that may use these persistence points, by default, any application that is installed prior to deployment will be considered trustworthy and will not be removed. This project will assume that the pre-deployment environment is trustworthy (meaning that pre-deployment persistence artifacts will be implicitly whitelisted by default) and will only block environment changes that occur post-deployment. Obviously, in a real-world production system, the pre-deployment environment may contain malicious code; however, a solution that protects the system prior to the deployment of this service will be considered out-of-scope for this project.

This service will generate an event in the Windows event log whenever an attempted change in one of the above persistence points is detected in order to aid threat hunters. If desired, a logging-only mode may be enabled by system administrators that will not block any persistence techniques and only create event logs.

Unfortunately, a threat actor that has gained access to a system using this application may be able to simply kill the user-mode process running this application. To prevent this, a custom low-lever kernel driver is also given as part of this project. This driver will prevent the user-mode service from simply being terminated by a malicious actor without rebooting the machine. System administrators will have the option to request the operating system to dynamically load the driver via the Service Control Manager.

## Installation

For installing the user-mode service:
```
sc create "Persistence Defender Service" binPath="C:\path\to\Persistence Defender.exe" start= auto
sc start "Persistence Defender Service"
```
To stop the service, set HKLM\SOFTWARE\PersistenceDefenderService\Running to 0 and reboot.

For installing the kernel-mode driver that defends the user-mode service:
```
bcdedit -set loadoptions DDISABLE_INTEGRITY_CHECKS 
bcdedit /set testsigning on
sc create PersistenceDefenderDriverService binPath= C:\path\to\PersistenceDefenderDriver.sys type= kernel
sc start PersistenceDefenderDriverService
```
(Note that at this point in time, driver signature enforcement must be disabled in order for this kernel driver to work.)

## Settings
The settings for this project are located in the registry under `HKLM\SOFTWARE\PersistenceDefenderService`. The following keys are available:
- Running
- SchTasksDefender
- StartupFoldersDefender
- AppShimsDefender
- ServicesDefender
- PSProfilesDefender
- BITSJobsDefender
- RegKeysDefender

For the "Running" setting:
- 0 indicates that the program immediately terminates.
- 1 indicates that the program executes properly.
Note that this setting is only checked when the service is initially started in order to prevent attackers from easily stopping the service without rebooting.

For all other registry settings:
- 0 indicates that the defender is disabled.
- 1 indicates that the defender is enabled.
- 2 indicates that only logging is enabled.
