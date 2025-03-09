/*++

Module Name:

    driver.h

Abstract:

    This file contains the driver definitions.

Environment:

    Kernel-mode Driver Framework

--*/

#ifndef DRIVER_H
#define DRIVER_H

#include <ntddk.h>
#include <wdf.h>
#include <initguid.h>

#include "device.h"
#include "queue.h"
#include "trace.h"

EXTERN_C_START

//
// WDFDRIVER Events
//

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_DEVICE_ADD PersistenceDefenderDriverEvtDeviceAdd;
EVT_WDF_OBJECT_CONTEXT_CLEANUP PersistenceDefenderDriverEvtDriverContextCleanup;

EXTERN_C_END

#endif