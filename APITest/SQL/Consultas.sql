
CREATE TABLE ccRIACat_Areas (
    IDArea INT PRIMARY KEY,
    AreaName VARCHAR(100),
    StatusArea INT,
    CreateDate DATETIME
);


CREATE TABLE ccUsers (
    User_id INT PRIMARY KEY,
    Login VARCHAR(50) UNIQUE,
    Nombres VARCHAR(100),
    ApellidoPaterno VARCHAR(100),
    ApellidoMaterno VARCHAR(100),
    Password VARCHAR(255),
    TipoUser_id INT,
    Status INT,
    fCreate DATETIME,
    IDArea INT,
    LastLoginAttempt DATETIME,
    CONSTRAINT FK_User_Area FOREIGN KEY (IDArea) 
        REFERENCES ccRIACat_Areas(IDArea)
);

CREATE TABLE ccloglogin (
    Log_id INT IDENTITY(1,1) PRIMARY KEY, 
    User_id INT,
    Extension NVARCHAR(20),
    TipoMov INT,
    fecha DATETIME,
    CONSTRAINT FK_ccloglogin_ccUsers FOREIGN KEY (User_id) 
        REFERENCES ccUsers(User_id)
);

DELETE FROM ccloglogin;
DELETE FROM ccUsers;
DELETE FROM ccRIACat_Areas;

INSERT INTO ccUsers (User_id, Login, Nombres, ApellidoPaterno, ApellidoMaterno, Password, TipoUser_id, Status, fCreate, IDArea, LastLoginAttempt)
VALUES (70, 'adriAgent', 'adriAgent', 'adriAgent', 'adriAgent', '0CC175B9C0F1B6A831C399E269772661', 1, 1, '2024-06-04 12:55:00', 1, '2024-06-04 12:54:56');


INSERT INTO ccRIACat_Areas (IDArea, AreaName, StatusArea, CreateDate)
VALUES 
(1, 'Default', 1, '2021-09-03 17:32:30'),
(2, 'BBVA', 1, '2022-10-03 17:32:30'),
(3, 'Banamex', 1, '2024-09-30 17:32:30')

INSERT INTO ccloglogin (User_id, Extension, TipoMov, fecha)
VALUES 
(70, '-8', 1, '2023-01-05 18:02:44'),
(70, '9', 0, '2023-01-06 16:32:30'),
(70, '-7', 1, '2023-01-07 21:13:27'),
(70, '-2', 0, '2023-01-09 19:02:25');

select * from ccUsers;
select * from ccRIACat_Areas;
select * from ccloglogin;

									
DELETE FROM ccloglogin WHERE User_id = 1;

------------------Optimización-----------------

CREATE INDEX IX_ccloglogin_User_Fecha_TipoMov
ON ccloglogin (User_id, fecha, TipoMov);


-------------- Consulta del usuario que más tiempo ha estado logueado ---------------

WITH L AS (
  SELECT User_id, fecha, ROW_NUMBER() OVER (PARTITION BY User_id ORDER BY fecha) AS rn
  FROM ccloglogin
  WHERE TipoMov = 1
),
R AS (
  SELECT User_id, fecha, ROW_NUMBER() OVER (PARTITION BY User_id ORDER BY fecha) AS rn
  FROM ccloglogin
  WHERE TipoMov = 0
),
Pairs AS (
  SELECT
    L.User_id,
    L.fecha  AS login_at,
    R.fecha  AS logout_at,
    DATEDIFF(SECOND, L.fecha, R.fecha) AS seconds
  FROM L
  JOIN R
    ON L.User_id = R.User_id
   AND L.rn      = R.rn
  WHERE R.fecha > L.fecha
)
SELECT TOP 1
  User_id,
  SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END) AS total_seconds,
  CONCAT(
    SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END)/86400, ' días, ',
    (SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END)%86400)/3600, ' horas, ',
    (SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END)%3600)/60, ' minutos, ',
    (SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END))%60, ' segundos'
  ) AS TiempoTotalFmt
FROM Pairs
GROUP BY User_id
ORDER BY SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END) DESC;


---------------Consulta del usuario que menos tiempo ha estado logueado---------------

WITH L AS (
  SELECT User_id, fecha, ROW_NUMBER() OVER (PARTITION BY User_id ORDER BY fecha) AS rn
  FROM ccloglogin
  WHERE TipoMov = 1
),
R AS (
  SELECT User_id, fecha, ROW_NUMBER() OVER (PARTITION BY User_id ORDER BY fecha) AS rn
  FROM ccloglogin
  WHERE TipoMov = 0
),
Pairs AS (
  SELECT
    L.User_id,
    L.fecha AS login_at,
    R.fecha AS logout_at,
    DATEDIFF(SECOND, L.fecha, R.fecha) AS seconds
  FROM L
  JOIN R
    ON L.User_id = R.User_id
   AND L.rn      = R.rn
  WHERE R.fecha > L.fecha
)
SELECT TOP 1
  User_id,
  SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END) AS total_seconds,
  CONCAT(
    SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END)/86400, ' días, ',
    (SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END)%86400)/3600, ' horas, ',
    (SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END)%3600)/60, ' minutos, ',
    (SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END))%60, ' segundos'
  ) AS TiempoTotalFmt
FROM Pairs
GROUP BY User_id
HAVING SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END) > 0
ORDER BY SUM(CASE WHEN seconds > 0 THEN seconds ELSE 0 END) ASC;



-------Promedio de logueo por mes--------

WITH L AS (
    SELECT User_id, fecha,
           ROW_NUMBER() OVER (PARTITION BY User_id ORDER BY fecha) AS rn
    FROM ccloglogin
    WHERE TipoMov = 1
),
R AS (
    SELECT User_id, fecha,
           ROW_NUMBER() OVER (PARTITION BY User_id ORDER BY fecha) AS rn
    FROM ccloglogin
    WHERE TipoMov = 0
),
Pairs AS (
    SELECT
        L.User_id,
        L.fecha AS login_at,
        R.fecha AS logout_at,
        DATEDIFF(SECOND, L.fecha, R.fecha) AS seconds
    FROM L
    JOIN R
      ON L.User_id = R.User_id
     AND L.rn      = R.rn
    WHERE R.fecha > L.fecha
),
PairsPos AS (
    SELECT
        User_id,
        login_at,
        logout_at,
        CASE WHEN seconds > 0 THEN seconds ELSE 0 END AS seconds
    FROM Pairs
),
Agg AS (
    SELECT
        User_id,
        DATEFROMPARTS(YEAR(login_at), MONTH(login_at), 1) AS Mes,
        CAST(AVG(CAST(seconds AS float)) AS BIGINT) AS avg_seconds
    FROM PairsPos
    GROUP BY User_id, DATEFROMPARTS(YEAR(login_at), MONTH(login_at), 1)
)
SELECT
    User_id,
    Mes,
    avg_seconds,
    CONCAT(
        avg_seconds / 86400, ' días, ',
        (avg_seconds % 86400) / 3600, ' horas, ',
        (avg_seconds % 3600) / 60, ' minutos, ',
        avg_seconds % 60, ' segundos'
    ) AS PromedioFmt
FROM Agg
ORDER BY User_id, Mes;
