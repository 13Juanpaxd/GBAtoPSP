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

1. Descarga este repositorio como ZIP y extraelo, o clonalo con Git.
2. Ejecuta [Instalar dependencias.bat](Instalar%20dependencias.bat) una vez con conexion a Internet.
3. Cuando termine, abre [Abrir GBA a PSP.cmd](Abrir%20GBA%20a%20PSP.cmd).
4. Selecciona tu ROM .gba, tu BIOS de GBA de 16 KiB y pulsa **Crear EBOOT.PBP**.
5. Copia la carpeta creada dentro de out/PSP/GAME/ a la misma ruta de tu Memory Stick.

El instalador descarga las herramientas de compilacion y el codigo de gpSP Kai en carpetas ignoradas por Git. No debes descargar ni configurar nada adicional manualmente.

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

## Privacidad y contenido personal

Este repositorio no incluye BIOS, ROMs, EBOOTs, compilados ni herramientas descargadas. Esos archivos estan excluidos por [.gitignore](.gitignore).

## Creditos

- [gpSP Kai](https://github.com/PSP-Archive/unofficial-gpSP-kai), emulador de GBA para PSP.
- [PSPDev](https://pspdev.github.io/), SDK para PSP.
- [pspdev-win](https://github.com/dmang-dev/pspdev-win), toolchain de PSP para Windows usado por el instalador.

Hecho por Juanpa.

