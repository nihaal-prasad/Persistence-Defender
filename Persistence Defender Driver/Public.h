/*++

Module Name:

    public.h

Abstract:

    This module contains the common declarations shared by driver
    and user applications.

Environment:

    user and kernel

--*/

//
// Define an Interface Guid so that apps can find the device and talk to it.
//

DEFINE_GUID (GUID_DEVINTERFACE_PersistenceDefenderDriver,
    0x85357641,0x0765,0x46e0,0x96,0x3f,0xac,0x04,0x90,0xe8,0xf7,0xf3);
// {85357641-0765-46e0-963f-ac0490e8f7f3}
