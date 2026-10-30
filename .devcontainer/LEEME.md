# Torre de Control en Codespaces

Este Codespace ya trae .NET 10, SQL Server 2022 y los datos de ejemplo. No hay que instalar nada.

## Abrir la app

1. Espera 1 o 2 minutos la primera vez (la app se compila y carga los datos de ejemplo).
2. Abre la pestaña **PORTS** (Puertos) de abajo y da clic en el ícono del globo 🌐 de
   **Torre de Control (5178)**. Si apareció un aviso "Open in Browser", también sirve.
3. Inicia sesión con un usuario demo. La contraseña está en la terminal (o escribe
   `cat ~/.torre-control/contrasena-demo`).

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
tail -f /tmp/torre-control.log   # ver lo que hace la app
bash .devcontainer/iniciar.sh    # volver a levantarla si se detuvo
dotnet test                      # correr todas las pruebas
```

## Seguridad

- El puerto es **privado**: solo tú (con tu cuenta de GitHub) puedes abrir la app. Si lo cambias
  a público, cualquiera con la dirección la vería.
- Las contraseñas (SQL Server y usuarios demo) se generan al azar **dentro** de este Codespace;
  no están en el repositorio.
- Detén el Codespace cuando no lo uses para no gastar tus horas gratis del mes.
