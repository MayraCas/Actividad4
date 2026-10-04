// hd_dyn.h -- Carga dinamica de la API HD de OpenHaptics (hd.dll).
//
// No requiere instalar el SDK de OpenHaptics: los prototipos y constantes de
// HD/hd.h se declaran aqui y las funciones se resuelven en tiempo de ejecucion
// con GetProcAddress sobre hd.dll (que se distribuye con los drivers del
// Geomagic Touch / Touch Device Drivers).
#pragma once

#include <windows.h>

// ---------------------------------------------------------------- tipos HD --
typedef unsigned int  HDenum;
typedef unsigned int  HDuint;
typedef int           HDint;
typedef unsigned char HDboolean;
typedef double        HDdouble;
typedef float         HDfloat;
typedef long          HDlong;
typedef unsigned short HDushort;
typedef unsigned int  HHD;                 // handle de dispositivo
typedef unsigned long HDSchedulerHandle;

typedef struct {
    HDenum errorCode;
    int    internalErrorCode;
    HHD    hHD;
} HDErrorInfo;

typedef HDenum (__cdecl *HDSchedulerCallback)(void *pUserData);

// ----------------------------------------------------------- constantes HD --
#define HD_SUCCESS                 0x0000
#define HD_INVALID_HANDLE          0xFFFFFFFF
#define HD_DEFAULT_DEVICE          ((const char *)0)

// Estado del dispositivo (hdGetDoublev / hdGetIntegerv)
#define HD_CURRENT_BUTTONS         0x2000
#define HD_CURRENT_SAFETY_SWITCH   0x2001
#define HD_CURRENT_INKWELL_SWITCH  0x2002
#define HD_CURRENT_ENCODER_VALUES  0x2010
#define HD_CURRENT_POSITION        0x2050   // mm, 3 doubles
#define HD_CURRENT_VELOCITY        0x2051   // mm/s, 3 doubles
#define HD_CURRENT_TRANSFORM       0x2052   // matriz 4x4 column-major
#define HD_CURRENT_ANGULAR_VELOCITY 0x2053
#define HD_CURRENT_JOINT_ANGLES    0x2100   // rad, 3 doubles
#define HD_CURRENT_GIMBAL_ANGLES   0x2150   // rad, 3 doubles

// Salida de fuerza (hdSetDoublev)
#define HD_CURRENT_FORCE           0x2700   // N, 3 doubles
#define HD_CURRENT_TORQUE          0x2701

// Capacidades (hdEnable / hdDisable)
#define HD_FORCE_OUTPUT            0x4000
#define HD_MAX_FORCE_CLAMPING      0x4001
#define HD_FORCE_RAMPING           0x4002
#define HD_SOFTWARE_FORCE_IMPULSE_LIMIT 0x4003
#define HD_SOFTWARE_FORCE_LIMIT    0x4004
#define HD_SOFTWARE_VELOCITY_LIMIT 0x4005

// Codigos de retorno de un callback del scheduler
#define HD_CALLBACK_DONE           0
#define HD_CALLBACK_CONTINUE       1

#define HD_MAX_SCHEDULER_PRIORITY     ((HDushort)0xFFFF)
#define HD_MIN_SCHEDULER_PRIORITY     ((HDushort)0x0000)
#define HD_DEFAULT_SCHEDULER_PRIORITY ((HDushort)0x7FFF)

// ------------------------------------------------------- punteros a funcion --
struct HdApi {
    HMODULE mod;

    HHD         (__cdecl *hdInitDevice)(const char *name);
    void        (__cdecl *hdDisableDevice)(HHD hHD);
    void        (__cdecl *hdMakeCurrentDevice)(HHD hHD);
    HHD         (__cdecl *hdGetCurrentDevice)(void);

    void        (__cdecl *hdBeginFrame)(HHD hHD);
    void        (__cdecl *hdEndFrame)(HHD hHD);

    void        (__cdecl *hdEnable)(HDenum cap);
    void        (__cdecl *hdDisable)(HDenum cap);
    HDboolean   (__cdecl *hdIsEnabled)(HDenum cap);

    void        (__cdecl *hdGetDoublev)(HDenum pname, HDdouble *params);
    void        (__cdecl *hdGetIntegerv)(HDenum pname, HDint *params);
    void        (__cdecl *hdGetBooleanv)(HDenum pname, HDboolean *params);
    const char *(__cdecl *hdGetString)(HDenum pname);
    void        (__cdecl *hdSetDoublev)(HDenum pname, const HDdouble *params);

    HDErrorInfo (__cdecl *hdGetError)(void);
    const char *(__cdecl *hdGetErrorString)(HDenum errorCode);

    HDSchedulerHandle (__cdecl *hdScheduleAsynchronous)(HDSchedulerCallback cb, void *data, HDushort prio);
    void        (__cdecl *hdScheduleSynchronous)(HDSchedulerCallback cb, void *data, HDushort prio);
    void        (__cdecl *hdUnschedule)(HDSchedulerHandle h);
    void        (__cdecl *hdStartScheduler)(void);
    void        (__cdecl *hdStopScheduler)(void);
    void        (__cdecl *hdSetSchedulerRate)(HDdouble hz);
};

// Carga hd.dll (primero junto al modulo indicado, si se pasa uno) y resuelve
// los simbolos. Devuelve true si todo se resolvio correctamente.
bool hdDynLoad(HdApi &api, HMODULE selfModule, char *errBuf, int errBufSize);
void hdDynUnload(HdApi &api);
