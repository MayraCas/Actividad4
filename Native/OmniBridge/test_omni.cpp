// test_omni.cpp -- prueba del puente sin Unity: verifica lectura de posicion
// y los cuatro modos de fuerza directamente contra el dispositivo.
#include "OmniBridge.h"
#include <windows.h>
#include <cstdio>

static void banner(const char *txt)
{
    printf("\n=== %s ===\n", txt);
}

static void correr(const char *titulo, const OmniParams &p, int segundos)
{
    banner(titulo);
    Omni_SetParams(&p);
    for (int i = 0; i < segundos * 5; ++i) {
        Sleep(200);
        OmniState s = {};
        Omni_GetState(&s);
        printf("t=%6.2f  P=(%7.2f %7.2f %7.2f) mm   F=(%6.3f %6.3f %6.3f) N  "
               "btn=%d ink=%d  servo=%6.1f Hz  err=%d\n",
               s.timeSec, s.position[0], s.position[1], s.position[2],
               s.force[0], s.force[1], s.force[2],
               s.buttons, s.inkwell, s.servoRateHz, s.errorCode);
    }
}

int main(void)
{
    printf("Inicializando Geomagic Touch...\n");
    int rc = Omni_Init();
    if (rc != 0) {
        printf("ERROR %d: %s\n", rc, Omni_GetLastError());
        return 1;
    }
    printf("OK. Modelo reportado: %s\n", Omni_GetDeviceModel());

    Omni_StartRecording(60000);   // 60 s a 1 kHz

    OmniParams p = {};
    Omni_GetParams(&p);

    // 1) Movimiento libre -- mueve el stylus y observa X, Y, Z.
    p.mode = OMNI_MODE_FREE;
    correr("1) MOVIMIENTO LIBRE (mueve el stylus)", p, 6);

    // 2) Resistencia virtual -- se siente como mover dentro de miel.
    p.mode    = OMNI_MODE_VISCOUS;
    p.damping = 0.006;
    correr("2) RESISTENCIA VIRTUAL (viscosidad)", p, 6);

    // 3) Atraccion hacia el centro del espacio de trabajo.
    p.mode    = OMNI_MODE_ATTRACT;
    p.anchor[0] = 0; p.anchor[1] = 0; p.anchor[2] = 0;
    p.anchorK      = 0.06;
    p.anchorRadius = 0;
    correr("3) ATRACCION HACIA EL PUNTO (0,0,0)", p, 6);

    // 4) Colision: esfera solida de 30 mm en el centro.
    p.mode = OMNI_MODE_SPHERES;
    p.sphereActive[0] = 1;
    p.sphereCenter[0] = 0; p.sphereCenter[1] = 0; p.sphereCenter[2] = 0;
    p.sphereRadius[0] = 30.0;
    p.sphereK[0]      = 0.20;
    correr("4) COLISION CON ESFERA (R=30mm en el centro)", p, 8);

    p.mode = OMNI_MODE_FREE;
    Omni_SetParams(&p);

    int n = Omni_SaveRecordingCsv("omni_test_log.csv");
    printf("\nMuestras guardadas en omni_test_log.csv: %d\n", n);

    Omni_Shutdown();
    printf("Dispositivo liberado.\n");
    return 0;
}
