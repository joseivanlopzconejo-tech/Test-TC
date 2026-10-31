// Filtros rápidos y conteos del resumen. Una sola definición para que la tarjeta "Pendientes 4"
// y la tabla filtrada por "Pendientes" siempre coincidan.

export const FILTROS = {
    todos: () => true,
    // Pendiente = todavía le falta la hora real de inicio o la de llegada.
    pendientes: f => !f.hir || !f.htr,
    // Ya salió y no ha llegado (En ruta / En salida).
    camino: f => Boolean(f.hir) && !f.htr,
    // Ya llegó (En planta / Resguardo).
    finalizados: f => Boolean(f.htr),
    incidencia: f => f.tieneIncidencia
};
