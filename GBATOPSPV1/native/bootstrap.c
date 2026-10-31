#include <pspiofilemgr.h>
#include <pspdebug.h>
#include <stdio.h>
#include <string.h>

/* Kept in the ELF so the Windows packager can reject an unmodified gpSP. */
volatile const char g2ps_bootstrap_marker[] = "G2PS_BOOTSTRAP_V1";

static unsigned short read16(FILE *file)
{
    unsigned char b[2];
    if (fread(b, 1, 2, file) != 2) return 0;
    return (unsigned short)(b[0] | (b[1] << 8));
}

static unsigned int read32(FILE *file)
{
    unsigned char b[4];
    if (fread(b, 1, 4, file) != 4) return 0;
    return (unsigned int)b[0] | ((unsigned int)b[1] << 8) |
           ((unsigned int)b[2] << 16) | ((unsigned int)b[3] << 24);
}

static int valid_path(const char *path)
{
    const unsigned char *p = (const unsigned char *)path;
    if (!*p || *p == '/' || strstr(path, "..")) return 0;
    while (*p)
    {
        if (!( (*p >= 'a' && *p <= 'z') || (*p >= 'A' && *p <= 'Z') ||
               (*p >= '0' && *p <= '9') || *p == '_' || *p == '-' ||
               *p == '.' || *p == '/' )) return 0;
        p++;
    }
    return 1;
}

static void make_directories(char *path)
{
    char *p;
    for (p = path; *p; p++)
    {
        if (*p == '/')
        {
            *p = 0;
            sceIoMkdir(path, 0777);
            *p = '/';
        }
    }
}

static void draw_loading(unsigned int done, unsigned int total, const char *name)
{
    unsigned int i;
    unsigned int filled = total ? (done * 24) / total : 0;
    pspDebugScreenClear();
    pspDebugScreenSetXY(13, 5);
    pspDebugScreenPrintf("G B A   t o   P S P");
    pspDebugScreenSetXY(11, 7);
    pspDebugScreenPrintf("gpSP Kai + PSPDev");
    pspDebugScreenSetXY(8, 10);
    pspDebugScreenPrintf("Preparando tu juego...");
    pspDebugScreenSetXY(8, 13);
    pspDebugScreenPrintf("[");
    for(i = 0; i < 24; i++) pspDebugScreenPrintf(i < filled ? "#" : ".");
    pspDebugScreenPrintf("]");
    pspDebugScreenSetXY(13, 15);
    pspDebugScreenPrintf("%u de %u archivos", done, total);
    pspDebugScreenSetXY(5, 18);
    pspDebugScreenPrintf("Extrayendo: %.42s", name);
    pspDebugScreenSetXY(14, 24);
    pspDebugScreenPrintf("Hecho por Juanpa");
}

int g2ps_extract(void)
{
    FILE *source;
    FILE *marker;
    unsigned char id[16];
    unsigned char saved[16];
    unsigned int archive_offset;
    unsigned int pbp_end;
    unsigned int count;
    unsigned int i;
    long source_length;
    unsigned char buffer[8192];
    char path[256];

    if (g2ps_bootstrap_marker[0] != 'G') return -1;
    source = fopen("EBOOT.PBP", "rb");
    if (!source) return -2;
    if (fseek(source, 0, SEEK_END) != 0 || (source_length = ftell(source)) < 40) goto fail;
    if (fseek(source, 0, SEEK_SET) != 0 ||
        fread(buffer, 1, 4, source) != 4 ||
        memcmp(buffer, "\0PBP", 4) != 0) goto fail;
    if (fseek(source, 36, SEEK_SET) != 0) goto fail;
    pbp_end = read32(source);
    if (pbp_end < 48 || pbp_end > (unsigned int)source_length) goto fail;
    if (fseek(source, (long)pbp_end - 8, SEEK_SET) != 0) goto fail;
    if (fread(buffer, 1, 4, source) != 4 || memcmp(buffer, "G2OF", 4) != 0) goto fail;
    archive_offset = read32(source);
    if (archive_offset < 40 || archive_offset >= pbp_end - 8 ||
        fseek(source, archive_offset, SEEK_SET) != 0) goto fail;
    if (fread(buffer, 1, 4, source) != 4 || memcmp(buffer, "G2PS", 4) != 0) goto fail;
    count = read32(source);
    if (count == 0 || count > 300 || fread(id, 1, 16, source) != 16) goto fail;

    marker = fopen(".g2ps-id", "rb");
    if (marker)
    {
        int same = fread(saved, 1, 16, marker) == 16 && memcmp(id, saved, 16) == 0;
        fclose(marker);
        if (same)
        {
            FILE *rom = fopen("GBA/ROM/game.gba", "rb");
            FILE *bios = fopen("gba_bios.bin", "rb");
            FILE *cfg = fopen("cfg/dir.cfg", "rb");
            if (rom && bios && cfg)
            {
                fclose(rom);
                fclose(bios);
                fclose(cfg);
                fclose(source);
                return 0;
            }
            if (rom) fclose(rom);
            if (bios) fclose(bios);
            if (cfg) fclose(cfg);
        }
    }

    sceIoMkdir("GBA", 0777);
    sceIoMkdir("GBA/ROM", 0777);
    sceIoMkdir("GBA/SAV", 0777);
    sceIoMkdir("GBA/CFG", 0777);
    sceIoMkdir("GBA/SS", 0777);
    sceIoMkdir("GBA/CHT", 0777);
    draw_loading(0, count, "recursos");

    for (i = 0; i < count; i++)
    {
        unsigned short name_len = read16(source);
        unsigned int remaining = read32(source);
        FILE *dest;
        if (name_len == 0 || name_len >= sizeof(path) || remaining > 32u * 1024u * 1024u)
            goto fail;
        if (fread(path, 1, name_len, source) != name_len) goto fail;
        path[name_len] = 0;
        if (!valid_path(path)) goto fail;
        make_directories(path);
        dest = fopen(path, "wb");
        if (!dest) goto fail;
        while (remaining)
        {
            unsigned int chunk = remaining < sizeof(buffer) ? remaining : sizeof(buffer);
            if (fread(buffer, 1, chunk, source) != chunk ||
                fwrite(buffer, 1, chunk, dest) != chunk)
            {
                fclose(dest);
                goto fail;
            }
            remaining -= chunk;
        }
        if (fclose(dest) != 0) goto fail;
        draw_loading(i + 1, count, path);
    }
    fclose(source);
    marker = fopen(".g2ps-id", "wb");
    if (!marker) return -3;
    if (fwrite(id, 1, 16, marker) != 16) { fclose(marker); return -3; }
    if (fclose(marker) != 0) return -3;
    return 0;

fail:
    fclose(source);
    return -4;
}


