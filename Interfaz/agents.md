# Guía para Agentes - Sistema de Interfaz (Dalton Visualizer)

Este documento sirve como guía para cualquier agente de IA que trabaje en el sistema de interfaz de este proyecto. Mantener la coherencia visual y técnica es **CRÍTICO** para preservar la estética "Premium" y el bajo acoplamiento del simulador.

## 1. Tecnologías y Estándares
- **Sistema**: Unity UI Toolkit (UXML y USS).
- **Fuentes**: Montserrat (Brand) y Roboto (Base).
- **Renderizado**: Siempre preferir assets **SDF (Font Assets)** para evitar bordes dentados ("serruchados").
- **Localización**: `Assets/Interfaz` contiene todos los estilos y definiciones visuales.

## 2. Sistema de Diseño (USS Variables)
Todos los colores y espaciados deben definirse en el selector `:root` de `Styles.uss`. **NUNCA** uses colores "hardcoded" en clases individuales si representan una identidad de marca.

### Colores Principales:
- `--color-accent-blue`: Azul principal tecnológico.
- `--color-accent-secondary`: Rojo Borravino (#7D1C34). Usado para acciones destructivas (Cerrar, Salir, Borrar).
- `--color-accent-tertiary`: Turquesa/Teal (#398084). Usado para diagnósticos, logs importantes y acentos técnicos.
- `--color-border-light`: Azul translúcido usado para fondos de hover o bordes sutiles.

## 3. Reglas de Estilo (USS)
- **NO USAR `text-transform`**: No es soportado por UI Toolkit. Si necesitas mayúsculas, hazlo en el UXML o vía C# con `.ToUpper()`.
- **NO USAR `pointer-events`**: No es una propiedad válida de USS. Usa `picking-mode: Ignore` en el UXML si necesitas que un elemento sea traspasable.
- **Transiciones**: Usa `transition-property`, `transition-duration` y `transition-timing-function` para que la interfaz se sienta "viva".
- **Estado Oculto**: Usa la clase `.panel--hidden` (u variantes como `--hidden-right`) que maneja `opacity` y `translate` para animaciones fluidas.

## 4. Tipografía y Jerarquía
- **Montserrat (Brand)**: Usar para títulos, información de marca, créditos y consola.
- **Bold**: Se recomienda usar `-unity-font-style: bold;` en Montserrat para mejorar la legibilidad y nitidez SDF.
- **Escalas Sugeridas**:
  - Títulos Principales: `18px`.
  - Etiquetas de Info: `11px` - `13px`.
  - Consola: `14px`.
  - Créditos: `12px`.

## 5. Arquitectura de Código (C#)
- **Bajo Acoplamiento**: Los componentes de UI (como `ConsolaUI`, `DetalleAtomoUI`) deben comunicarse preferentemente mediante **Eventos de C#** (`Action`, `Func`) en lugar de referencias directas, para evitar dependencias circulares.
- **LogN**: Usa la clase estática `LogN` para enviar mensajes a la consola del usuario.

## 6. Mantenimiento
Antes de agregar un nuevo color o estilo, verifica si ya existe una variable en el `:root`. Si necesitas una variación de un color existente (más claro o más oscuro), agrégala como `--color-accent-[nombre]-alt`.

---
*Recuerda: La estética es lo que hace que este simulador destaque. Si no se ve increíble, no está terminado.*
