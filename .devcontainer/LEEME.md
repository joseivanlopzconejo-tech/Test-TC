# Torre de Control en Codespaces

Este Codespace ya trae .NET 10, SQL Server 2022 y los datos de ejemplo. No hay que instalar nada.

## Abrir la app

1. Abre una terminal (menú ☰ → Terminal → Nueva terminal) y escribe:

   ```bash
   bash .devcontainer/probar.sh
   ```

   Arranca la app, espera a que responda y te dice la dirección y la contraseña. Si algo
   falla, muestra el error.
2. Abre esa dirección en una pestaña **normal** del navegador (no en el "Explorador simple"
   del editor). También sirve el globo 🌐 de la fila 5178 en la pestaña **Puertos**.

   Si sale **error 502** aunque el comando diga ✅, el Codespace probablemente se creó con una
   configuración anterior: reconstrúyelo (clic en "Codespaces" abajo a la izquierda →
   "Rebuild Container") y vuelve a correr el comando.
3. Inicia sesión con un usuario demo.

| Número | Rol | Ve |
|---|---|---|
| 900001 | Sistemas | Todas las plantas |
| 900002 | Jefe de Monitoreo | Todas las plantas |
| 900003 | Supervisor de Apoyo | Todas las plantas |
| 900004 | Supervisor de Operaciones | Todas las plantas |
| 900005 | Monitorista | Módulo 1 (Norvial Silao, Norvial Irapuato) |
| 900006 | Monitorista | Módulo 2 (Quimtek Salamanca, Quimtek León) |

## Comandos útiles (en la terminal)

```bash
bash .devcontainer/probar.sh     # arrancar la app (si no corre) y revisar que responda
bash .devcontainer/probar.sh --reiniciar-datos   # regenerar los datos de ejemplo desde cero
tail -f /tmp/torre-control.log   # ver lo que hace la app
dotnet test                      # correr todas las pruebas
```

## ¿Por qué la app comparte la red de la base de datos?

En un Codespace con Docker Compose, si la app vive en su propia red, el reenvío de puertos de
GitHub no llega hasta ella y responde **502** aunque la app funcione por dentro. Por eso la app
usa `network_mode: service:db` (igual que las plantillas oficiales de Dev Containers): ambos
contenedores comparten la red, la base se ve en `localhost:1433` y el puerto 5178 se reenvía bien.

## Seguridad

- El puerto es **privado**: solo tú (con tu cuenta de GitHub) puedes abrir la app. Si lo cambias
  a público, cualquiera con la dirección la vería.
- Las contraseñas (SQL Server y usuarios demo) se generan al azar **dentro** de este Codespace;
  no están en el repositorio.
- Detén el Codespace cuando no lo uses para no gastar tus horas gratis del mes.
