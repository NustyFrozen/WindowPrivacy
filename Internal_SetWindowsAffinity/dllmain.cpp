#include <Windows.h>
#include <iostream>


BOOL CALLBACK SetAffinityCallback(HWND handle, LPARAM lParam)
{
    DWORD data = *(DWORD*)lParam;
    unsigned long process_id = 0;
    GetWindowThreadProcessId(handle, &process_id);
    //making sure its the current process
    if (data == process_id){
        SetWindowDisplayAffinity(handle,WDA_MONITOR);
    //printf("set monitor");//debugging
}
    return TRUE;
}
DWORD WINAPI MainThread(HMODULE hModule) {
    /*AllocConsole(); //debugging
    FILE* f;
    freopen_s(&f, "CONOUT$", "w", stdout);
    printf("Allocated");
    printf("proc id %d", ProcID);*/
    DWORD ProcID = GetCurrentProcessId();
    EnumWindows(SetAffinityCallback, (LPARAM)&ProcID);

	FreeLibraryAndExitThread(hModule,0);
	return 0;
}
BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        CloseHandle(CreateThread(nullptr, 0, (LPTHREAD_START_ROUTINE)MainThread, hModule, 0, nullptr));
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }

    return TRUE;
}