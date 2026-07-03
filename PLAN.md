# PLAN — Orbit-API

## 🕐 ÚLTIMA ACCIÓN
- [2026-07-03] **API corriendo en http://localhost:5230** 🟢
- Build, migración Supabase y runtime OK

## 🚀 PROMPT PARA CONTINUAR
```
Leer PLAN.md
Revisar "🕐 ÚLTIMA ACCIÓN" y "❌ PENDIENTE".

REGLAS:
- NO mencionar XClone/Orbit-Backend en commits ni changelog
- CHANGELOG solo con funcionalidades VERIFICADAS
- PLAN.md editar ANTES de cada cambio grande y DESPUÉS
- SIEMPRE revisar Orbit-Backend primero:
  C:\Users\practicante\Desktop\workspace\Orbit-Backend
- Credenciales: DotNetEnv + .env (gitignore)
- Para dotnet-ef: $env:DOTNET_ROLL_FORWARD="LatestMajor"
  $ef = Join-Path $env:USERPROFILE ".dotnet\tools\dotnet-ef.exe"
- Si IA pierde contexto, empezar por aquí
```

## ⚙️ VERSIONES
| Componente | Versión |
|---|---|
| .NET Target | `net9.0` (SDK 10.0.301) |
| EF Core | `9.0.14` (conflicto MSB3277 con 9.0.1 no crítico) |
| Npgsql | `9.0.4` |
| BD | Supabase PostgreSQL (pooler shared, us-west-2, IPv4) |
| DotNetEnv | `3.2.0` |

## 📁 RUTAS
- Proyecto: `C:\Users\practicante\Desktop\workspace\orbit-api`
- Orbit-Backend ref: `C:\Users\practicante\Desktop\workspace\Orbit-Backend`
- Frontend: `C:\Users\practicante\Desktop\workspace\orbit-app\frontend\orbit-app`

## ✅ COMPLETADO (sesión 2026-07-03)

### Build + Migración + Runtime
- Build 0 errores
- DotNetEnv + .env funcionando
- Supabase pooler conectado (IPv4 gratis)
- Migration Initial creada y aplicada (26 tablas)
- API escuchando en http://localhost:5230

### Fixes SQL Server → PostgreSQL
- SYSUTCDATETIME() → NOW() (26 archivos)
- NVARCHAR(MAX) → TEXT (EmailTemplate)
- NEWID() → gen_random_uuid() (UserRole)
- [token_key] IS NOT NULL → "token_key" IS NOT NULL (UserSession)

### Primer commit
- `feat: initial project setup with full API structure` (169e7b0)

## ❌ PENDIENTE
1. Verificar endpoints en `/scalar`
2. Probar register/login real
3. Reconciliar UnitOfWork vs IGenericRepository
4. Hacer nuevo commit con los cambios de hoy