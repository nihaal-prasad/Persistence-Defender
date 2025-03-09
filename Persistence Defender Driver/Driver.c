#include <ntifs.h>
#include <ntddk.h>
#include <wdf.h>
#include <ntstrsafe.h>
#include <ntimage.h>

/*
 * bcdedit /set testsigning on
 * sc create PersistenceDefenderDriverService binPath= C:\Users\Administrator\Desktop\x64\Debug\PersistenceDefenderDriver.sys type= kernel
 * sc start PersistenceDefenderDriverService
 */

DRIVER_INITIALIZE DriverEntry;
EVT_WDF_DRIVER_UNLOAD DriverUnload;

#define PROCESS_TERMINATE 0x0001

// Function to get process name
BOOLEAN GetProcessName(PEPROCESS Process, PCHAR ProcessName, SIZE_T Size) {
    NTSTATUS status;
    PUNICODE_STRING imageName;
    ANSI_STRING ansiName;

    // Retrieve the process image name
    status = SeLocateProcessImageName(Process, &imageName);
    if (NT_SUCCESS(status)) {
        // Ensure the image name is valid before copying
        if (imageName != NULL && imageName->Buffer != NULL) {
            // Initialize the ANSI_STRING structure
            RtlZeroMemory(&ansiName, sizeof(ANSI_STRING));

            // Convert the UNICODE_STRING (imageName) to an ANSI_STRING
            status = RtlUnicodeStringToAnsiString(&ansiName, imageName, TRUE);
            if (NT_SUCCESS(status)) {
                // Copy the process name into the provided buffer
                RtlStringCbCopyNA(ProcessName, Size, ansiName.Buffer, Size - 1);
                ProcessName[Size - 1] = '\0';  // Null-terminate the string safely

                // Free the ANSI_STRING buffer after use
                RtlFreeAnsiString(&ansiName);

                // Free the allocated memory for imageName
                ExFreePoolWithTag(imageName, 'ipnm');
                return TRUE;
            }
            else {
                // If the conversion failed, return FALSE
                DbgPrint("RtlUnicodeStringToAnsiString failed\n");
            }
        }

        // Free the allocated memory for imageName
        ExFreePoolWithTag(imageName, 'ipnm');
    }

    return FALSE;
}

// Custom function to check if a string contains a substring
BOOLEAN ContainsSubstring(PCHAR String, PCHAR Substring) {
    size_t stringLen = strlen(String);
    size_t subStrLen = strlen(Substring);

    // Ensure the substring is smaller than the string to avoid invalid memory access
    if (subStrLen > stringLen) {
        return FALSE;
    }

    for (size_t i = 0; i <= stringLen - subStrLen; i++) {
        if (strncmp(&String[i], Substring, subStrLen) == 0) {
            return TRUE; // Found the substring
        }
    }
    return FALSE; // Substring not found
}

// Function to filter process access
OB_PREOP_CALLBACK_STATUS ProcessPreOperationCallback(
    PVOID RegistrationContext,
    POB_PRE_OPERATION_INFORMATION OperationInformation
) {
    UNREFERENCED_PARAMETER(RegistrationContext);

    if (OperationInformation->ObjectType == *PsProcessType) {
        PEPROCESS TargetProcess = (PEPROCESS)OperationInformation->Object;
        CHAR ProcessName[256] = { 0 };

        // Ensure the target process is valid before accessing
        if (TargetProcess == NULL) {
            return OB_PREOP_SUCCESS;
        }

        // Get the process name safely
        if (GetProcessName(TargetProcess, ProcessName, sizeof(ProcessName))) {
            // Check if "Persistence Defender.exe" is contained in the process name
            if (ContainsSubstring(ProcessName, "Persistence Defender.exe")) {
                if (OperationInformation->Operation == OB_OPERATION_HANDLE_CREATE ||
                    OperationInformation->Operation == OB_OPERATION_HANDLE_DUPLICATE) {

                    // Ensure DesiredAccess modification is safe
                    if (OperationInformation->Parameters->CreateHandleInformation.DesiredAccess & PROCESS_TERMINATE) {
                        OperationInformation->Parameters->CreateHandleInformation.DesiredAccess &= ~PROCESS_TERMINATE;
                    }
                }
            }
        }
    }

    return OB_PREOP_SUCCESS;
}

PVOID g_RegistrationHandle = NULL;

NTSTATUS RegisterProcessFilter() {
    OB_OPERATION_REGISTRATION OperationRegistration = { 0 };
    OB_CALLBACK_REGISTRATION CallbackRegistration = { 0 };

    // Register for process handle creation only
    OperationRegistration.ObjectType = PsProcessType;
    OperationRegistration.Operations = OB_OPERATION_HANDLE_CREATE;
    OperationRegistration.PreOperation = ProcessPreOperationCallback;

    UNICODE_STRING Altitude = RTL_CONSTANT_STRING(L"320000");
    CallbackRegistration.Version = OB_FLT_REGISTRATION_VERSION;
    CallbackRegistration.OperationRegistrationCount = 1;
    CallbackRegistration.Altitude = Altitude;
    CallbackRegistration.OperationRegistration = &OperationRegistration;

    NTSTATUS Status = ObRegisterCallbacks(&CallbackRegistration, &g_RegistrationHandle);
    if (!NT_SUCCESS(Status)) {
        DbgPrint("ObRegisterCallbacks failed: %X\n", Status);
        return Status;
    }

    return STATUS_SUCCESS;
}

VOID UnregisterProcessFilter() {
    if (g_RegistrationHandle) {
        ObUnRegisterCallbacks(g_RegistrationHandle);
        g_RegistrationHandle = NULL;
        DbgPrint("Unregistered callback\n");
    }
}

NTSTATUS DriverEntry(
    _In_ PDRIVER_OBJECT DriverObject,
    _In_ PUNICODE_STRING RegistryPath
) {
    UNREFERENCED_PARAMETER(RegistryPath);
    WDF_DRIVER_CONFIG Config;
    NTSTATUS Status;
    WDFDRIVER Driver;

    DbgPrint("Entry\n");

    WDF_DRIVER_CONFIG_INIT(&Config, WDF_NO_EVENT_CALLBACK);
    Config.EvtDriverUnload = DriverUnload;
    Status = WdfDriverCreate(DriverObject, RegistryPath, WDF_NO_OBJECT_ATTRIBUTES, &Config, &Driver);
    if (!NT_SUCCESS(Status)) {
        return Status;
    }

    DbgPrint("started\n");

    RegisterProcessFilter();

    return STATUS_SUCCESS;
}

VOID DriverUnload(_In_ WDFDRIVER Driver) {
    DbgPrint("unload\n");
    UNREFERENCED_PARAMETER(Driver);
    UnregisterProcessFilter();
}
