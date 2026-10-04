// diag_limites.cpp -- Descubre los limites nominales que el propio dispositivo
// declara (rigidez y amortiguamiento maximos estables, fuerza maxima,
// dimensiones del espacio de trabajo). Barre los identificadores de la API HD
// y reporta los que responden sin error.
#include "hd_dyn.h"
#include <cstdio>
#include <cstring>

static HdApi api;

int main(void)
{
    char err[512];
    if (!hdDynLoad(api, NULL, err, sizeof(err))) { printf("%s\n", err); return 1; }

    HHD dev = api.hdInitDevice(HD_DEFAULT_DEVICE);
    HDErrorInfo e = api.hdGetError();
    if (e.errorCode != HD_SUCCESS) {
        printf("hdInitDevice fallo: 0x%X (%s)\n", e.errorCode, api.hdGetErrorString(e.errorCode));
        return 1;
    }
    api.hdMakeCurrentDevice(dev);
    api.hdGetError();

    printf("=== Propiedades de tipo double que acepta el dispositivo ===\n");
    for (HDenum pn = 0x2200; pn <= 0x2600; ++pn) {
        double v[16];
        for (int i = 0; i < 16; ++i) v[i] = -12345.6789;
        api.hdGetDoublev(pn, v);
        if (api.hdGetError().errorCode != HD_SUCCESS) continue;

        int n = 0;
        while (n < 16 && v[n] != -12345.6789) ++n;
        if (n == 0) continue;
        printf("  0x%04X : ", pn);
        for (int i = 0; i < n && i < 6; ++i) printf("%12.6f ", v[i]);
        if (n > 6) printf(" ... (%d valores)", n);
        printf("\n");
    }

    printf("\n=== Propiedades de tipo entero ===\n");
    for (HDenum pn = 0x2200; pn <= 0x2600; ++pn) {
        int v[8];
        for (int i = 0; i < 8; ++i) v[i] = -12345;
        api.hdGetIntegerv(pn, v);
        if (api.hdGetError().errorCode != HD_SUCCESS) continue;

        int n = 0;
        while (n < 8 && v[n] != -12345) ++n;
        if (n == 0) continue;
        printf("  0x%04X : ", pn);
        for (int i = 0; i < n && i < 6; ++i) printf("%d ", v[i]);
        printf("\n");
    }

    api.hdDisableDevice(dev);
    return 0;
}
