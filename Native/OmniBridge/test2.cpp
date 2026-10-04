// test2.cpp -- verifica que los parametros llegan al servo loop.
#include "OmniBridge.h"
#include <windows.h>
#include <cstdio>

int main(void)
{
    if (Omni_Init() != 0) { printf("ERROR: %s\n", Omni_GetLastError()); return 1; }
    printf("%s\n", Omni_GetDeviceModel());

    OmniParams p = {};
    Omni_GetParams(&p);
    printf("defaults: mode=%d scale=%.2f maxF=%.2f damping=%.4f anchorK=%.3f\n",
           p.mode, p.forceScale, p.maxForce, p.damping, p.anchorK);

    p.mode = OMNI_MODE_ATTRACT;
    p.anchor[0] = p.anchor[1] = p.anchor[2] = 0.0;
    p.anchorK = 0.06;
    p.anchorRadius = 0.0;
    Omni_SetParams(&p);

    OmniParams q = {};
    Omni_GetParams(&q);
    printf("leidos del DLL: mode=%d anchorK=%.3f scale=%.2f\n", q.mode, q.anchorK, q.forceScale);

    for (int i = 0; i < 10; ++i) {
        Sleep(300);
        OmniState s = {};
        Omni_GetState(&s);
        printf("P=(%7.2f %7.2f %7.2f)  F=(%6.3f %6.3f %6.3f)  fEnab=%d ticks=%d err=%d run=%d\n",
               s.position[0], s.position[1], s.position[2],
               s.force[0], s.force[1], s.force[2],
               s.forceEnabled, s.servoTicks, s.errorCode, s.running);
    }
    Omni_Shutdown();
    return 0;
}
