// diag_omni.cpp -- diagnostico: prueba cada consulta de la API HD por separado
// y reporta el error exacto que devuelve el dispositivo.
#include "hd_dyn.h"
#include <cstdio>
#include <cstring>

static HdApi api;

struct Query { const char *nombre; HDenum pname; int tipo; int n; };
// tipo: 0 = double, 1 = int, 2 = boolean

static Query queries[] = {
    {"HD_CURRENT_BUTTONS        (0x2000) int",  0x2000, 1, 1},
    {"HD_CURRENT_SAFETY_SWITCH  (0x2001) int",  0x2001, 1, 1},
    {"HD_CURRENT_INKWELL_SWITCH (0x2002) int",  0x2002, 1, 1},
    {"HD_CURRENT_INKWELL_SWITCH (0x2002) bool", 0x2002, 2, 1},
    {"HD_CURRENT_ENCODER_VALUES (0x2010) int",  0x2010, 1, 6},
    {"HD_CURRENT_POSITION       (0x2050) dbl",  0x2050, 0, 3},
    {"HD_CURRENT_VELOCITY       (0x2051) dbl",  0x2051, 0, 3},
    {"HD_CURRENT_TRANSFORM      (0x2052) dbl",  0x2052, 0, 16},
    {"HD_CURRENT_ANG_VELOCITY   (0x2053) dbl",  0x2053, 0, 3},
    {"HD_CURRENT_JOINT_ANGLES   (0x2100) dbl",  0x2100, 0, 3},
    {"HD_CURRENT_GIMBAL_ANGLES  (0x2150) dbl",  0x2150, 0, 3},
};

static const int NQ = sizeof(queries) / sizeof(queries[0]);

static int    errCode[NQ];
static double valD[NQ][16];
static int    valI[NQ][6];

static HDenum __cdecl diagCb(void *)
{
    HHD h = api.hdGetCurrentDevice();
    api.hdBeginFrame(h);
    api.hdGetError();                       // limpiar cola

    for (int i = 0; i < NQ; ++i) {
        memset(valD[i], 0, sizeof(valD[i]));
        memset(valI[i], 0, sizeof(valI[i]));
        switch (queries[i].tipo) {
            case 0: api.hdGetDoublev(queries[i].pname, valD[i]); break;
            case 1: api.hdGetIntegerv(queries[i].pname, valI[i]); break;
            case 2: api.hdGetBooleanv(queries[i].pname, (HDboolean *)valI[i]); break;
        }
        errCode[i] = (int)api.hdGetError().errorCode;
    }

    api.hdEndFrame(h);
    return HD_CALLBACK_DONE;
}

int main(void)
{
    char err[512];
    if (!hdDynLoad(api, NULL, err, sizeof(err))) { printf("%s\n", err); return 1; }

    HHD dev = api.hdInitDevice(HD_DEFAULT_DEVICE);
    HDErrorInfo e = api.hdGetError();
    if (e.errorCode != HD_SUCCESS) {
        printf("hdInitDevice fallo: 0x%X (%s)\n", e.errorCode,
               api.hdGetErrorString(e.errorCode));
        return 1;
    }
    printf("Dispositivo inicializado (handle=%u)\n", dev);

    // Cadenas del dispositivo: probar posibles pnames de tipo string.
    for (HDenum pn = 0x2000; pn <= 0x2600; ++pn) {
        const char *s = api.hdGetString(pn);
        HDenum ec = api.hdGetError().errorCode;
        if (ec == HD_SUCCESS && s && s[0]) printf("  hdGetString(0x%04X) = \"%s\"\n", pn, s);
    }
    api.hdGetError();

    api.hdMakeCurrentDevice(dev);
    api.hdEnable(HD_FORCE_OUTPUT);
    printf("hdEnable(0x4000) -> err 0x%X\n", api.hdGetError().errorCode);
    api.hdEnable(HD_MAX_FORCE_CLAMPING);
    printf("hdEnable(0x4001) -> err 0x%X\n", api.hdGetError().errorCode);

    api.hdStartScheduler();
    e = api.hdGetError();
    if (e.errorCode != HD_SUCCESS) { printf("hdStartScheduler fallo 0x%X\n", e.errorCode); return 1; }

    printf("\n--- Consultas (repetido 3 veces, mueve el stylus entre ellas) ---\n");
    for (int rep = 0; rep < 3; ++rep) {
        api.hdScheduleSynchronous(diagCb, NULL, HD_DEFAULT_SCHEDULER_PRIORITY);
        printf("\n[pasada %d]\n", rep + 1);
        for (int i = 0; i < NQ; ++i) {
            printf("  %-42s err=0x%03X  ", queries[i].nombre, errCode[i]);
            if (queries[i].tipo == 0)
                for (int k = 0; k < queries[i].n && k < 4; ++k) printf("%9.3f ", valD[i][k]);
            else
                for (int k = 0; k < queries[i].n; ++k) printf("%d ", valI[i][k]);
            if (errCode[i] != 0) printf("  <- %s", api.hdGetErrorString((HDenum)errCode[i]));
            printf("\n");
        }
        Sleep(400);
    }

    api.hdStopScheduler();
    api.hdDisableDevice(dev);
    printf("\nFin.\n");
    return 0;
}
