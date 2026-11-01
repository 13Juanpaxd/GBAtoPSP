# GBA to PSP

> Crea un EBOOT.PBP autocontenido para ejecutar una copia local de un juego de Game Boy Advance en PSP.

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white)
![PSP](https://img.shields.io/badge/PSP-Homebrew-1B1B1B)
![Estado](https://img.shields.io/badge/estado-v1-blue)

## Que hace

GBA to PSP crea una carpeta PSP/GAME/<JUEGO>/ con un EBOOT.PBP que ya incluye gpSP Kai y la ROM elegida. No hace falta instalar un emulador aparte en la consola.

Incluye:

- Interfaz grafica de Windows.
- Portadas, fondo, imagen de informacion, video PMF y audio AT3 opcionales.
- Ajustes de pantalla, filtro, frameskip, frecuencia y avance rapido.
- Save states, guardado automatico de la ROM y mapeado de controles.
- Pantalla de carga GBA to PSP durante el primer inicio.
- Opciones para HOME y salida TV en PSP-2000.

## Inicio rapido

1. Descarga o clona esta carpeta completa.
2. Abre **Abrir GBA a PSP.cmd** con doble clic.
3. Selecciona tu ROM `.gba`, tu BIOS GBA propio de 16 KiB y un titulo.
4. Opcionalmente selecciona icono, informacion, fondo, video PMF y musica.
5. Pulsa **Crear EBOOT.PBP**.
6. Copia la carpeta creada dentro de `out/PSP/GAME/` a `PSP/GAME/` de la consola.

No copies solo `EBOOT.PBP`: el paquete usa archivos junto a el para evitar que
la ROM quede duplicada y para iniciar mas rapido.



## En PSP

- El juego abre en 16:9 por defecto. Cambialo a 4:3 desde `SELECT + START`.
- `SELECT + L` guarda un save state y `SELECT + R` lo carga.
- Las partidas normales se guardan en `GBA/SAV/game.sav`.
- La carpeta incluye la pantalla de carga GBA a PSP y sus creditos.


## Requisitos

- Windows 10 u 11 de 64 bits.
- Conexion a Internet durante la instalacion inicial.
- Una PSP capaz de ejecutar homebrew.
- Una copia local de la ROM y un BIOS de GBA obtenido de tu propio hardware.

## Controles dentro del juego

| Funcion | Control |
| --- | --- |
| Abrir menu | SELECT + START |
| Cambiar pestaña | L / R |
| Guardar save state | SELECT + L |
| Cargar save state | START + R |

El menu tiene tres pestañas: **Ajustes**, **Estados** y **Controles**. La cruceta y el analogico siempre funcionan como direcciones. Los cambios de controles se guardan por juego.

## Archivos generados

Todos los paquetes se crean en una sola raiz:

    out/
      PSP/
        GAME/
          NOMBREDELJUEGO/
            EBOOT.PBP

Al iniciar por primera vez, el EBOOT extrae los recursos de ejecucion dentro de su propia carpeta. Los guardados de la ROM se almacenan en GBA/SAV/game.sav y los save states usan las ranuras 0 a 9.

## Imagenes y vista previa

La aplicacion muestra una vista previa de los PNG con el mismo ajuste que usa
la PSP:

- Icono: 144 x 80 px.
- Informacion: 480 x 272 px.
- Fondo: 480 x 272 px.

Las imagenes de otros tamanos se centran sin deformarse.

## Musica del menu

Puedes elegir un MP3 directamente. La aplicacion incluye `pspbuild.exe` y lo
convierte automaticamente al SND0.AT3 que requiere el menu de PSP: ATRAC3 LP4,
66 kbps, 44.1 kHz, estereo y hasta 55 segundos. Tambien puedes seleccionar un
archivo `.at3` ya preparado.


## No incluido

Por razones legales, este proyecto no contiene ROMs ni BIOS. Debes proporcionar
tus propias copias locales. Los resultados generados se excluyen mediante
`.gitignore`.

## Creditos

- gpSP Kai: https://github.com/PSP-Archive/unofficial-gpSP-kai
- PSPDev: https://pspdev.github.io/
- pspbuild: https://github.com/ItsNoHax/pspbuild
- Interfaz y empaquetador: Juanpa / GBA a PSP