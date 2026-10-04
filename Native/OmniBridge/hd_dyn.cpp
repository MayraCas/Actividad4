#include "hd_dyn.h"
#include <cstdio>
#include <cstring>

#define LOAD_FN(name)                                                          \
    do {                                                                       \
        *(FARPROC *)&api.name = GetProcAddress(api.mod, #name);                \
        if (!api.name) {                                                       \
            _snprintf_s(errBuf, errBufSize, _TRUNCATE,                         \
                        "hd.dll no exporta '%s'", #name);                      \
            hdDynUnload(api);                                                  \
            return false;                                                      \
        }                                                                      \
    } while (0)

static const char *kDriverHdPath =
    "C:\\Program Files\\3D Systems\\Touch Device Drivers\\OpenHaptics_Demos\\"
    "HapticDemo\\HapticDemo_Data\\Plugins\\x86_64\\hd.dll";

bool hdDynLoad(HdApi &api, HMODULE selfModule, char *errBuf, int errBufSize)
{
    memset(&api, 0, sizeof(api));

    // 1) hd.dll en la misma carpeta que este modulo (Assets/Plugins/x86_64).
    if (selfModule) {
        char path[MAX_PATH];
        DWORD n = GetModuleFileNameA(selfModule, path, MAX_PATH);
        if (n > 0 && n < MAX_PATH) {
            char *slash = strrchr(path, '\\');
            if (slash) {
                *(slash + 1) = '\0';
                strncat_s(path, MAX_PATH, "hd.dll", _TRUNCATE);
                api.mod = LoadLibraryExA(path, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
            }
        }
    }
    // 2) Busqueda estandar (PATH / System32).
    if (!api.mod) api.mod = LoadLibraryA("hd.dll");
    // 3) Ruta donde los drivers del Touch instalan hd.dll.
    if (!api.mod) api.mod = LoadLibraryExA(kDriverHdPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);

    if (!api.mod) {
        _snprintf_s(errBuf, errBufSize, _TRUNCATE,
                    "No se pudo cargar hd.dll (error Win32 %lu). "
                    "Copia hd.dll junto a OmniBridge.dll.", GetLastError());
        return false;
    }

    LOAD_FN(hdInitDevice);
    LOAD_FN(hdDisableDevice);
    LOAD_FN(hdMakeCurrentDevice);
    LOAD_FN(hdGetCurrentDevice);
    LOAD_FN(hdBeginFrame);
    LOAD_FN(hdEndFrame);
    LOAD_FN(hdEnable);
    LOAD_FN(hdDisable);
    LOAD_FN(hdIsEnabled);
    LOAD_FN(hdGetDoublev);
    LOAD_FN(hdGetIntegerv);
    LOAD_FN(hdGetBooleanv);
    LOAD_FN(hdGetString);
    LOAD_FN(hdSetDoublev);
    LOAD_FN(hdGetError);
    LOAD_FN(hdGetErrorString);
    LOAD_FN(hdScheduleAsynchronous);
    LOAD_FN(hdScheduleSynchronous);
    LOAD_FN(hdUnschedule);
    LOAD_FN(hdStartScheduler);
    LOAD_FN(hdStopScheduler);
    LOAD_FN(hdSetSchedulerRate);

    errBuf[0] = '\0';
    return true;
}

void hdDynUnload(HdApi &api)
{
    if (api.mod) FreeLibrary(api.mod);
    memset(&api, 0, sizeof(api));
}
