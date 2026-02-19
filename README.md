# EVALUACIÓN TÉCNICA NUXIBA

Prueba: **DESARROLLADOR JR**

Deadline: **1 día**

Nombre: Gustavo Flores Cadena

## Instrucciones:Docker, SQL Server, API y CSV

1) Levantar SQL Server (Docker)
- Requiere Docker en ejecución.

```bash
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourStrong!Passw0rd' -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2019-latest
```

2) Conectar la base de datos
- Servidor: localhost,1433
- Usuario: sa
- Password: YourStrong!Passw0rd
- La API usa la BD CCenterRIA; conexión en [APITest/appsettings.json](APITest/appsettings.json):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=CCenterRIA;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"
  }
}
```



3) Ejecutar la API

```bash
cd APITest
dotnet run --urls http://localhost:5160
```

4) Endpoints principales
- GET http://localhost:5160/logins
- POST http://localhost:5160/logins
- PUT http://localhost:5160/logins/{id}
- DELETE http://localhost:5160/logins/{id}
- GET http://localhost:5160/report/csv
- GET http://localhost:5160/report/csv/{userId}

5) Ejemplos curl

```bash
# Listar logins
curl http://localhost:5160/logins

# Crear login
curl -X POST http://localhost:5160/logins ^
  -H "Content-Type: application/json" ^
  -d "{\"userId\":70,\"extension\":\"-8\",\"tipoMov\":1,\"fecha\":\"2023-01-05T18:02:44\"}"

# Cerrar sesión (PUT sobre un registro existente)
curl -X PUT http://localhost:5160/logins/1 ^
  -H "Content-Type: application/json" ^
  -d "{\"userId\":70,\"extension\":\"-8\",\"tipoMov\":0,\"fecha\":\"2023-01-05T19:02:44\"}"

# Eliminar registro
curl -X DELETE http://localhost:5160/logins/1

# Descargar CSV (todos)
curl -o reporte_horas.csv http://localhost:5160/report/csv

# Descargar CSV por usuario
curl -o reporte_horas_user_70.csv http://localhost:5160/report/csv/70
```

6) Notas
- La API calcula tiempos emparejando login (TipoMov=1) con logout (TipoMov=0).
- Si cambias la contraseña del contenedor, actualiza la cadena en appsettings.json.
- Pruebas unitarias: no incluidas en esta entrega.

7) Consultas SQL del Ejercicio 2
- Las consultas solicitadas (máximo, mínimo y promedio por mes), junto con la creación de tablas de apoyo y el índice de optimización, están en:
  - APITest/SQL/Consultas.sql
  - Ruta completa: testdevbackjr\APITest\SQL\Consultas.sql
