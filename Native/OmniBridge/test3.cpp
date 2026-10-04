// test3.cpp -- comprueba que los encoders responden al movimiento real.
#include "OmniBridge.h"
#include <windows.h>
#include <cstdio>

int main(void)
{
    if (Omni_Init() != 0) { printf("ERROR: %s\n", Omni_GetLastError()); return 1; }
    printf("%s\n", Omni_GetDeviceModel());

    // Estado de calibracion reportado por hd.dll
    HMODULE hd = GetModuleHandleA("hd.dll");
    if (hd) {
        typedef unsigned int (__cdecl *CheckCal_t)(void);
        CheckCal_t chk = (CheckCal_t)GetProcAddress(hd, "hdCheckCalibration");
        if (chk) printf("hdCheckCalibration() = 0x%X\n", chk());
    }

    OmniParams p = {};
    Omni_GetParams(&p);
    p.mode = OMNI_MODE_FREE;          // sin fuerzas: movimiento totalmente libre
    Omni_SetParams(&p);

    printf("\n>>> SACA EL STYLUS DEL SOPORTE Y MUEVELO EN TODAS DIRECCIONES <<<\n");
    for (int i = 5; i > 0; --i) { printf("  empieza en %d...\n", i); Sleep(1000); }

    double mn[3] = {1e9, 1e9, 1e9}, mx[3] = {-1e9, -1e9, -1e9};
    for (int i = 0; i < 20; ++i) {
        for (int j = 0; j < 50; ++j) {          // muestrea a 50 Hz
            Sleep(20);
            OmniState s = {};
            Omni_GetState(&s);
            for (int k = 0; k < 3; ++k) {
                if (s.position[k] < mn[k]) mn[k] = s.position[k];
                if (s.position[k] > mx[k]) mx[k] = s.position[k];
            }
        }
        OmniState s = {};
        Omni_GetState(&s);
        printf("  t=%4.1f s  P=(%7.2f %7.2f %7.2f)  btn=%d ink=%d\n",
               s.timeSec, s.position[0], s.position[1], s.position[2], s.buttons, s.inkwell);
    }

    printf("\n--- Rango recorrido (mm) ---\n");
    const char *eje = "XYZ";
    for (int k = 0; k < 3; ++k)
        printf("  %c: min %8.2f   max %8.2f   rango %8.2f\n", eje[k], mn[k], mx[k], mx[k] - mn[k]);

    Omni_Shutdown();
    return 0;
}
