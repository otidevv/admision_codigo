# SKILL: Diseño UNAMAD - Sistema de Admisión

Este archivo define reglas visuales para mantener la **alineación y consistencia** de las páginas de Inicio e Inscripción usando Tailwind.

## 1) Estructura base
- Fondo principal de aplicación: `bg-[#f4f3f7]`.
- Contenedor principal: `max-w-6xl mx-auto px-4 md:px-6`.
- Tarjetas: `bg-white border border-[#ecd9e2] rounded-2xl shadow-[0_6px_20px_rgba(245,68,119,0.08)]`.
- Espaciados verticales recomendados: `py-6`, `py-8`, `py-10`.

## 2) Encabezado (Topbar)
- Barra superior blanca con borde inferior: `bg-white border-b border-slate-200`.
- Altura visual compacta: `py-3`.
- Bloque de marca:
  - Icono/logo en caja suave: `w-12 h-12 rounded-md bg-emerald-100`.
  - Título principal en negrita.
  - Subtítulo en color primario: `text-primary uppercase tracking-wide text-xs`.

## 3) Tipografía
- Título de página: `text-3xl md:text-4xl font-bold text-slate-900`.
- Subtítulo descriptivo: `text-slate-500`.
- Títulos de sección: `text-xl font-semibold text-slate-900`.
- Labels de formulario: `text-sm font-medium text-slate-700`.

## 4) Formularios
- Inputs/select estándar:
  - `w-full h-12 rounded-xl border border-slate-300 bg-white px-4 text-slate-700`
  - Focus: `focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/40`
- Grupo segmentado (tabs simples):
  - Contenedor: `inline-flex bg-slate-100 rounded-xl p-1`
  - Activo: `bg-white text-primary shadow-sm`
  - Inactivo: `text-slate-500`

## 5) Componentes clave del flujo de inscripción
- Cabecera de paso activo:
  - `bg-primary text-white rounded-t-2xl px-6 py-5`
  - Badge numérico: `w-8 h-8 rounded-full bg-white text-primary font-semibold`
- Alerta importante:
  - `border-l-4 border-amber-500 bg-amber-50 text-amber-800 rounded-xl p-4`
- Zona de carga:
  - `border-2 border-dashed border-slate-300 rounded-xl bg-slate-50`
  - CTA archivo en primario (`text-primary`).

## 6) Botones
- Primario: `bg-primary hover:bg-primary-600 text-white h-12 px-6 rounded-xl font-medium`.
- Secundario claro: `bg-white border border-slate-300 hover:bg-slate-50 text-slate-700 h-12 px-6 rounded-xl font-medium`.

## 7) Colores funcionales
- Primario marca: `primary` (Tailwind config existente).
- Texto principal: `text-slate-900`.
- Texto secundario: `text-slate-500`.
- Bordes suaves: `border-slate-200` / `border-slate-300`.

## 8) Responsive
- En móvil: apilar formularios con `grid-cols-1`.
- En desktop: usar `md:grid-cols-2` para campos paralelos.
- Mantener acciones al final alineadas a la derecha con `justify-end`.

## 9) Reglas de consistencia
1. Reutilizar clases base de esta skill antes de crear nuevas variantes.
2. Mantener radio (`rounded-xl` / `rounded-2xl`) consistente en inputs y tarjetas.
3. Evitar mezclar Bootstrap en estas pantallas; usar solo Tailwind.
4. Conservar jerarquía visual: título > subtítulo > bloques > acciones.
