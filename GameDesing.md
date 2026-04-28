# 📑 GDD: PROYECTO DALTON (Atomic Simulator)

**Desarrollador:** Yeiko (Anwulf Games)

**Plataforma:** PC / Unity (URP)

**Estética:** Minimalista / High-Tech / Sci-Fi

---

## 1. Visión General

Un simulador interactivo de alta fidelidad visual diseñado para explorar la Teoría Atómica de Dalton. El objetivo es transformar conceptos abstractos de 1804 en una experiencia sensorial moderna, donde la materia se sienta tangible, pesada y tecnológica.

## 2. Pilares de Diseño

- **Fidelidad Visual:** Uso intensivo de Post-Processing (Bloom, ACES Tonemapping) para lograr un look cinematográfico.
    
- **Minimalismo Funcional:** Menos es más. La interfaz no debe ensuciar la observación de la materia.
    
- **Interacción Orgánica:** El movimiento basado en física e inercia debe hacer que el usuario sienta el "peso" del átomo.
    

---

## 3. Mecánicas de Juego (Core Gameplay)

### A. Manipulación Espacial (Scroll Navigation)

- **Acción:** El usuario utiliza la rueda del ratón.
    
- **Efecto:** El átomo se desplaza en el eje Z (profundidad) mediante coordenadas reales.
    
- **Física:** Aplicación de inercia para que el objeto no se detenga en seco, simulando un entorno de gravedad cero o vacío absoluto.
    

### B. Sistema de Información Diegética (Call-outs)

- **Visual:** Una línea blanca fina conecta el átomo con una etiqueta de texto flotante.
    
- **Billboard:** El texto y la línea rotan dinámicamente para mirar siempre al usuario.
    
- **Contenido:** Nombre del elemento, símbolo y datos relevantes según el postulado de Dalton.
    

### C. Evento de Fusión (Feedback Sensorial)

- **Trigger:** Colisión entre dos cuerpos atómicos.
    
- **Cambio de Estado:** El entorno (Sky-sphere) transiciona de Azul Místico a Rojo Reactor.
    
- **Propósito:** Indicar una reacción o cambio de estado energético, rompiendo la monotonía visual.
    

---

## 4. Especificaciones Artísticas

|**Elemento**|**Estética**|**Configuración Técnica**|
|---|---|---|
|**Fondo**|Espacio Profundo|Azul marino con nebulosas (Sky-sphere personalizada).|
|**Referencia**|Grilla Técnica|Grid blanco/cian con opacidad al 20% para profundidad.|
|**Átomo**|Esfera de Energía|Shader con Emission dorado y superficie metálica suave.|
|**Interfaz**|HUD Minimalista|Tipografía Sans-Serif blanca y líneas de 0.05 de grosor.|

---

## 5. Relación con la Teoría de Dalton (Valor Educativo)

El simulador sirve como prueba visual de los siguientes puntos:

1. **Átomos como esferas macizas:** Representados por Rigidbodies esféricos indeformables.
    
2. **Diferenciación de Elementos:** Cada tipo de átomo tendrá su propio tamaño y color metálico, respetando que "átomos de un mismo elemento son iguales entre sí".
    
3. **Combinación Química:** Los átomos pueden "engancharse" para formar estructuras mayores (compuestos).
    

---

## 6. Roadmap de Desarrollo (Pendientes)

- [x] Motor de renderizado y Post-processing configurado.
    
- [x] Movimiento con inercia y control por rueda del ratón.
    
- [x] Estética final de la grilla y el fondo azul.
    
- [x] Billboard y Call-out line funcional.
    
- [ ] Implementar sistema de colisión y cambio de color a rojo.
    
- [ ] Crear variantes para otros átomos (Hidrógeno, Oxígeno) como Prefabs.
    
- [ ] Pulido final de UI y exportación a `.exe`.
    

---

**"La materia no se crea ni se destruye, se simula con estilo."**