#!/usr/bin/env bash
# Se ejecuta cada vez que arranca el Codespace: levanta la app en segundo plano.
# Para verla trabajar:  tail -f /tmp/torre-control.log
set -uo pipefail

PUERTO=5178

# ¿Ya hay algo escuchando en el puerto? Se revisa el PUERTO y no el nombre del proceso, porque
# la extensión de C# del editor también tiene procesos con el nombre del proyecto.
if (exec 3<>"/dev/tcp/127.0.0.1/$PUERTO") 2>/dev/null; then
  echo "La app ya está corriendo en el puerto $PUERTO."
  exit 0
fi

# SQL Server tarda unos segundos en aceptar conexiones; si la app arranca antes, falla al
# cargar los datos de ejemplo.
echo "Esperando a SQL Server..."
for _ in $(seq 1 60); do
  (exec 3<>/dev/tcp/db/1433) 2>/dev/null && break
  sleep 2
done

# setsid + nohup: la app queda separada de esta terminal y sigue viva cuando el script termina.
setsid nohup dotnet run --project src/TorreControl.Web --no-launch-profile --urls "http://0.0.0.0:$PUERTO" \
  > /tmp/torre-control.log 2>&1 < /dev/null &

echo "La app está arrancando (tarda ~1 minuto la primera vez). Bitácora: /tmp/torre-control.log"
echo "Usuarios demo: 900001 a 900006. Contraseña: $(cat "$HOME/.torre-control/contrasena-demo" 2>/dev/null)"
