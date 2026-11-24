---
sidebar_position: 7
---

import useBaseUrl from '@docusaurus/useBaseUrl';

# Configuración

La parte de configuración del programa es muy variada y, a medida que se amplíen las funciones, se irán actualizando más opciones de personalización.

## Configuración general del programa

La configuración se divide principalmente en cuatro partes: rutas de archivos, internacionalización y personalización, avisos de mensajes, y otros. A continuación se explican brevemente los ajustes principales:

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/setting.webp')}
    alt="Configuración general"
    style={{width: '100%'}}
  />
</div>

### 🌍 Idioma y apariencia

-   **Idioma**: en la actualidad admite chino simplificado, chino tradicional, inglés, alemán, español, japonés, coreano, ruso, francés y portugués.
-   **Escala de la UI**: distintos ajustes de escala para pantallas de distinta resolución; en general, el valor predeterminado es suficiente.

:::info

En cuanto a la compatibilidad lingüística, si desea ayudar con la localización de su país, le invitamos encarecidamente a unirse a nuestro trabajo de traducción. La mayor parte de la traducción actual está asistida por IA, lo que significa que algunas partes pueden ser inexactas. Su ayuda es muy necesaria y se agradece mucho.

:::

### 🗨️ Avisos de mensajes

-   **Duración del cierre automático**: duración tras la cual se cierra solo el aviso emergente de mensajes mientras el programa está en ejecución. Los mensajes de error y de advertencia deben cerrarse manualmente.

### ⚙️ Otros

-   **Inicio con el sistema**: el programa se iniciará automáticamente al arrancar el sistema.
-   **Comprobar actualizaciones automáticamente**: el programa comprobará si hay actualizaciones al iniciarse.
-   **Modo de salida**: el programa puede salir de dos maneras: salir directamente o minimizarse a la bandeja del sistema. Si elige minimizar a la bandeja, puede hacer clic con el botón derecho en el icono de la bandeja y elegir Salir para cerrar el programa.

## Configuración del módulo de diagramas

La configuración del módulo de diagramas se refiere específicamente a las funciones de ese módulo, incluidas, entre otras:

*   configuración de la ruta de programas de terceros
*   programa de terceros predeterminado
*   idioma de carga predeterminado de los diagramas
*   actualización automática de las plantillas de diagramas y de la estructura de categorías

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/diagram_settings.webp')}
    alt="Configuración de diagramas"
    style={{width: '100%'}}
  />
</div>

## Acerca de

Todas las personas que han contribuido a este programa quedan registradas y reciben un agradecimiento en esta página. Las formas de contribución incluyen, entre otras, sugerencias de funciones del programa, comunicación de problemas, revisión de la interfaz y de la traducción, y cualquier otro apoyo y ayuda que contribuya a mejorar y desarrollar el programa.

<div className="text--center">
  <img
    src={useBaseUrl('/img/v0.9.0/en-US/about.webp')}
    alt="Configuración de Acerca de"
    style={{width: '100%'}}
  />
</div>
