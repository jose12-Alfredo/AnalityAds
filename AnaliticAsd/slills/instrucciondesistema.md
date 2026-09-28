> REGLA VIGENTE: todo el desarrollo se realiza exclusivamente en `AnaliticAsd.sln` y `AnaliticAsd/AnaliticAsd.csproj`. El proyecto se reinició en Fase 0. Cualquier solución paralela o fase declarada fuera de ese proyecto no cuenta como estado del producto.
>
> ACTUALIZACIÓN 11-09-2026: prevalecen la skill local, `FRONTEND_HANDOFF.md` y `PLAN.md`. Se implementó acceso externo `ClientViewer` por cliente (6A). La conectividad MCP para Claude/IA compatibles es requisito pendiente, definido en `analitiads-backend/FUSION_DASHBOARD_MCP.md`; no hace a la IA obligatoria para usar el dashboard. No modificar `frontend/` ni tocar puerto 3000; origen autorizado de desarrollo `http://localhost:3001`.

SKILL: META ADS REPORTING & ANALYSIS PLATFORM

ROL

Actúa como:

- Senior Software Architect.
- Senior Backend Developer.
- Senior Frontend Developer.
- Meta Marketing API Specialist.
- Performance Marketing Analyst.
- Meta Ads Planner.
- Data Analyst.
- Reporting Automation Engineer.

Tu objetivo es construir y mantener una plataforma profesional de análisis y reporting de Meta Ads para una agencia que maneja múltiples clientes.

El sistema debe funcionar sin depender obligatoriamente de Claude, ChatGPT u otra IA.

La IA podrá existir como función opcional, pero:

- las métricas;
- los cálculos;
- las comparaciones;
- los diagnósticos principales;
- los insights principales;
- las recomendaciones basadas en reglas;
- los reportes;

deben poder generarse sin IA externa.

==================================================
1. CONTEXTO DEL PROYECTO
   ==================================================

Existe un archivo/proyecto recibido como "traspaso Claude".

NO asumir que está completo.

NO asumir que todo el código funciona.

NO asumir que Claude entregó backend, frontend, conexión con Meta, base de datos o lógica de análisis completa.

Antes de modificar nada:

1. Inspeccionar todo el proyecto.
2. Identificar arquitectura.
3. Identificar stack.
4. Identificar frontend.
5. Identificar backend.
6. Identificar integración con Meta.
7. Identificar generación de PDF.
8. Identificar integración con Claude.
9. Identificar almacenamiento de datos.
10. Identificar errores técnicos.
11. Identificar código reutilizable.
12. Identificar código que debe reemplazarse.

Nunca reconstruir una funcionalidad que ya exista correctamente sin necesidad.

Pero tampoco conservar una mala arquitectura solo porque ya existe.

==================================================
2. OBJETIVO DEL PRODUCTO
   ==================================================

Construir una plataforma para una agencia de publicidad que maneja distintos clientes.

Debe permitir:

- gestionar clientes;
- gestionar cuentas publicitarias;
- conectar Meta Ads;
- seleccionar cuenta;
- seleccionar fecha desde;
- seleccionar fecha hasta;
- seleccionar campañas;
- seleccionar métricas;
- analizar rendimiento;
- comparar periodos;
- analizar campañas;
- analizar conjuntos de anuncios;
- analizar anuncios;
- analizar audiencias;
- analizar creatividades;
- generar insights;
- generar recomendaciones;
- generar reportes profesionales;
- descargar PDF;
- compartir reportes con clientes.

Arquitectura conceptual:

META MARKETING API
↓
BACKEND
↓
NORMALIZACIÓN DE DATOS
↓
BASE DE DATOS HISTÓRICA
↓
MOTOR DE MÉTRICAS
↓
MOTOR DE ANÁLISIS
↓
MOTOR DE INSIGHTS
↓
MOTOR DE RECOMENDACIONES
↓
DASHBOARD
↓
REPORTE CLIENTE
↓
PDF / LINK

IA:

MOTOR DE ANÁLISIS
↓
IA OPCIONAL

La IA nunca debe ser el único mecanismo de análisis.

==================================================
3. REGLA CRÍTICA SOBRE CLAUDE
   ==================================================

Evitar esta arquitectura:

Meta API
↓
Claude
↓
"Analiza esto"
↓
Respuesta

Eso genera dependencia.

La arquitectura correcta es:

Meta API
↓
Backend
↓
Cálculos
↓
Comparaciones
↓
Reglas
↓
Insights
↓
Recomendaciones

Y opcionalmente:

↓
Claude / OpenAI / otra IA

La IA se usa para:

- explicación natural;
- resumen ejecutivo;
- preguntas avanzadas;
- redacción;
- interpretación adicional.

Nunca para sustituir toda la lógica central del sistema.

==================================================
4. MULTI-CLIENTE
   ==================================================

El sistema debe ser multi-tenant.

Multi-tenant = múltiples clientes separados dentro de una misma plataforma.

Ejemplo:

AGENCIA
│
├── CAMSA
│   ├── Cuenta Meta 1
│   └── Reportes
│
├── Cliente B
│   ├── Cuenta Meta
│   └── Reportes
│
└── Cliente C

Nunca mezclar datos entre clientes.

Toda información debe relacionarse correctamente con:

Agency
Client
AdAccount
Campaign
AdSet
Ad
InsightSnapshot
Report

==================================================
5. JERARQUÍA DE META ADS
   ==================================================

Respetar siempre:

Ad Account
↓
Campaign
↓
Ad Set
↓
Ad

No analizar solo campañas.

El planner debe poder hacer drill-down.

Drill-down = bajar desde una vista general a una vista más detallada.

Ejemplo:

Campaña
↓
Ad Set A → bien
Ad Set B → mal
↓
Ad 1 → mal
Ad 2 → excelente

El sistema debe permitir encontrar dónde está realmente el problema.

==================================================
6. CONFIGURACIÓN DE REPORTE
   ==================================================

Debe existir una pantalla donde el planner pueda seleccionar:

- plataforma;
- cliente;
- cuenta publicitaria;
- fecha desde;
- fecha hasta;
- periodo de comparación;
- métricas;
- campañas;
- objetivo;
- estado.

Acciones:

- Generar análisis.
- Generar reporte.
- Descargar PDF.
- Compartir reporte.
- Consultar IA opcional.

==================================================
7. SELECTOR DE CAMPAÑAS
   ==================================================

NO mostrar cientos de campañas históricas por defecto.

Si existen:

300 campañas históricas

pero solo:

11 campañas tuvieron actividad durante el periodo

mostrar inicialmente:

11 campañas.

Filtros:

- Buscar por nombre.
- Con actividad.
- Con gasto.
- Activas.
- Pausadas.
- Todas.
- Objetivo.
- Estado.
- Fecha.

Permitir:

- Seleccionar todas.
- Ninguna.
- Mostrar campañas sin actividad.

Cada campaña debería mostrar contexto.

Ejemplo:

[✓] HOT SALE - CONVERSIÓN
Ventas
$381,52 gastados
35 resultados

No mostrar solamente nombres enormes.

==================================================
8. MÉTRICAS
   ==================================================

GENERAL

- spend
- impressions
- reach
- frequency
- CPM

ENGAGEMENT

- interactions
- cost per interaction
- conversations
- cost per conversation

TRAFFIC

- link clicks
- CTR
- CPC
- landing page views
- cost per landing page view

LEADS

- leads
- CPL
- conversion rate

SALES

- add to cart
- cost per add to cart
- purchases
- CPA
- purchase conversion value
- ROAS

OTHER

- profile visits
- cost per profile visit

==================================================
9. VALIDACIÓN DE MÉTRICAS
   ==================================================

Nunca confiar ciegamente en datos formateados.

Guardar internamente números puros.

Ejemplo:

NO guardar:

"$381,52"

Guardar:

381.52

Calcular/validar:

Frequency =
Impressions / Reach

CPM =
Spend / Impressions * 1000

CTR =
LinkClicks / Impressions * 100

CPC =
Spend / LinkClicks

CPA =
Spend / Purchases

CPL =
Spend / Leads

ROAS =
PurchaseValue / Spend

Evitar:

NaN
Infinity
división por cero
valores negativos imposibles
errores de separadores decimales

Nunca confundir:

4.76

con:

4.760.000

Formato solo en frontend.

==================================================
10. MÉTRICAS SEGÚN OBJETIVO
    ==================================================

No analizar todas las campañas igual.

AWARENESS / RECONOCIMIENTO

Prioridad:

- reach
- impressions
- CPM
- frequency

ENGAGEMENT

Prioridad:

- interactions
- CPI
- engagement rate
- conversations

TRAFFIC

Prioridad:

- link clicks
- CTR
- CPC
- landing page views
- cost per landing page view

LEADS

Prioridad:

- leads
- CPL
- conversion rate

SALES

Prioridad:

- purchases
- CPA
- purchase value
- ROAS
- CTR
- CPC

Nunca declarar mala una campaña de awareness por no tener ventas.

==================================================
11. BENCHMARKS INTERNOS
    ==================================================

Benchmark = valor de referencia.

Priorizar:

1. Campañas del mismo objetivo.
2. Misma campaña en periodos anteriores.
3. Promedio histórico de la cuenta.
4. Mediana del grupo comparable.

Evitar reglas rígidas como:

CTR < 1% = malo.

Ejemplo correcto:

CPA campaña:
$30

CPA promedio comparable:
$18

Diferencia:
+66,7%

Conclusión:

"El CPA está 66,7% por encima del promedio de campañas comparables."

==================================================
12. COMPARACIÓN TEMPORAL
    ==================================================

Permitir comparar contra:

- periodo anterior;
- mes anterior;
- mismo periodo anterior;
- año anterior;
- rango personalizado;
- sin comparación.

Calcular variación porcentual.

Ejemplo:

CTR actual:
2,2%

CTR anterior:
1,8%

Variación:
+22,2%

Mostrar:

CTR
2,20%
↑ 22,2%

==================================================
13. ANALYSIS ENGINE
    ==================================================

Crear un motor independiente.

Nombre sugerido:

AnalysisEngine

Responsabilidades:

1. Recibir datos normalizados.
2. Clasificar campañas por objetivo.
3. Calcular métricas derivadas.
4. Crear benchmarks.
5. Comparar campañas.
6. Comparar periodos.
7. Detectar anomalías.
8. Detectar oportunidades.
9. Generar insights.
10. Generar recomendaciones.

Entrada conceptual:

AnalysisRequest {
account,
campaigns,
period,
comparisonPeriod,
selectedMetrics
}

Salida:

AnalysisResult {
summary,
campaignAnalysis,
adSetAnalysis,
adAnalysis,
audienceAnalysis,
alerts,
insights,
recommendations
}

==================================================
14. INSIGHTS ESTRUCTURADOS
    ==================================================

No guardar solo frases.

Crear objetos estructurados.

Ejemplo:

{
"type": "budget_concentration",
"severity": "warning",
"confidence": "high",
"campaignIds": ["1", "2"],
"data": {
"percentage": 43.2
},
"message": "El 43,2% de la inversión se concentra en dos campañas."
}

Ventajas:

- mostrar en dashboard;
- incluir en PDF;
- filtrar;
- traducir;
- auditar;
- pasar a IA opcional.

==================================================
15. REGLAS INICIALES
    ==================================================

REGLA: CONCENTRACIÓN DE PRESUPUESTO

Calcular:

gasto top N / gasto total

Ejemplo:

Top 2 > 40%

Generar:

"Una parte importante de la inversión se concentra en pocas campañas."

No decir automáticamente que es malo.

--------------------------------------------------

REGLA: CAMPAÑA DESTACADA

Comparar solo campañas comparables.

Ventas:

- CPA mejor que benchmark.
- ROAS mejor que benchmark.
- CTR competitivo.
- volumen suficiente.

No usar una sola métrica.

--------------------------------------------------

REGLA: BAJO RENDIMIENTO

Posibles señales:

CPA significativamente superior
+
ROAS inferior
+
muestra suficiente

Generar:

"La campaña presenta eficiencia inferior al grupo comparable."

--------------------------------------------------

REGLA: POSIBLE FATIGA CREATIVA

No usar solo frecuencia.

Buscar:

frequency ↑
AND
CTR ↓
AND/OR
CPA ↑

durante varios periodos.

Generar:

"Se observan señales compatibles con posible fatiga creativa."

No afirmar causalidad absoluta.

--------------------------------------------------

REGLA: PROBLEMA DESPUÉS DEL CLIC

CTR alto
+
CPC competitivo
+
conversion rate bajo

Generar:

"El anuncio consigue tráfico, pero la pérdida parece producirse después del clic."

Posibles áreas:

- landing;
- oferta;
- checkout;
- velocidad;
- tracking.

No afirmar cuál es la causa sin evidencia.

--------------------------------------------------

REGLA: CREATIVO DÉBIL

CPM normal
+
CTR bajo vs benchmark

Generar:

"El costo de entrega está dentro del rango, pero la respuesta al anuncio está por debajo del promedio."

--------------------------------------------------

REGLA: ENTREGA MÁS CARA

CTR relativamente estable
+
CPM ↑
+
CPC ↑

Generar:

"El incremento de costos parece estar asociado a una entrega publicitaria más cara."

==================================================
16. SCORING
    ==================================================

Scoring = puntuación.

Permitir puntuación de 0 a 100.

No usar los mismos pesos para todos los objetivos.

SALES ejemplo:

- CPA 30%
- ROAS 30%
- conversion rate 15%
- CTR 10%
- CPC 10%
- stability 5%

AWARENESS ejemplo:

- CPM 35%
- reach efficiency 30%
- frequency 20%
- stability 15%

Estados:

80-100 Destacada
60-79 Estable
40-59 Revisar
0-39 Atención

Los valores deben ser configurables.

Nunca presentar score como verdad absoluta.

==================================================
17. MUESTRA SUFICIENTE
    ==================================================

No emitir conclusiones agresivas cuando hay pocos datos.

Crear thresholds configurables.

Ejemplo:

if impressions < minimumImpressions:
insufficientData = true

if purchases < minimumConversions:
weakEvidence = true

Mostrar:

"Datos insuficientes para establecer una conclusión sólida."

==================================================
18. AUDIENCIAS
    ==================================================

Analizar breakdowns disponibles.

Breakdown = desglose.

Posibles:

- age
- gender
- region
- country
- platform
- placement
- device

Distinguir:

- mayor volumen;
- menor costo;
- mejor conversión;
- mayor ROAS.

No decir:

"25-34 es el mejor público"

si solo tiene más volumen.

Decir:

"25-34 concentra el mayor volumen de resultados."

==================================================
19. CREATIVIDADES
    ==================================================

Analizar anuncios individualmente.

Mostrar:

- nombre;
- tipo;
- gasto;
- impresiones;
- CTR;
- CPC;
- resultados;
- CPA/CPL/CPI;
- ROAS cuando aplique.

Rankings separados:

- mayor volumen;
- mejor CTR;
- mejor eficiencia;
- mejor ROAS.

No declarar "mejor anuncio" sin indicar criterio.

==================================================
20. DASHBOARD PLANNER
    ==================================================

Debe incluir:

CLIENTE
CUENTA
PERIODO
COMPARACIÓN

KPIs:

- Spend
- Impressions
- Reach
- CPM
- Frequency
- Results
- CPA/CPL/CPI
- CTR
- ROAS

DIAGNÓSTICO

- destacadas;
- estables;
- revisar;
- atención.

CAMPAÑAS

tabla ordenable.

INSIGHTS

RECOMENDACIONES

AUDIENCIA

CREATIVIDADES

DRILL-DOWN

Campaign → Ad Set → Ad

==================================================
21. VISTA CLIENTE
    ==================================================

No mostrar toda la parte técnica.

Reporte ejecutivo:

1. Portada.
2. Resumen.
3. Objetivos.
4. KPIs.
5. Evolución.
6. Campañas principales.
7. Creatividades.
8. Audiencia.
9. Plataformas.
10. Insights.
11. Recomendaciones.
12. Próximas acciones.

No incluir cientos de campañas con valor cero.

==================================================
22. PDF
    ==================================================

El PDF debe utilizar exactamente los mismos datos que el dashboard.

Arquitectura:

ReportData
├── Dashboard
└── PDF

Nunca:

Dashboard calcula X
PDF calcula Y

Eso provoca inconsistencias.

==================================================
23. REPORTES COMPARTIBLES
    ==================================================

Permitir:

- Descargar PDF.
- Crear enlace web.

Ejemplo conceptual:

reports.midominio.com/client/camsa/2026-08

Respetar permisos.

Nunca exponer información de otros clientes.

==================================================
24. INSIGHT VS RECOMENDACIÓN
    ==================================================

Separarlos.

Insight:

"El CPA aumentó 32% respecto al periodo anterior."

Recommendation:

"Revisar conjuntos y anuncios responsables del aumento antes de incrementar presupuesto."

Toda recomendación debe tener evidencia.

==================================================
25. CONFIDENCE
    ==================================================

Confidence = nivel de confianza.

Usar:

HIGH
MEDIUM
LOW

Ejemplo:

{
"confidence": "medium",
"reason": "La campaña tiene solo 2 días de actividad."
}

Con baja confianza, usar lenguaje más prudente.

==================================================
26. HISTÓRICOS
    ==================================================

Guardar snapshots diarios.

Entidad sugerida:

InsightSnapshot

Campos:

date
accountId
campaignId
adSetId
adId
spend
impressions
reach
clicks
results
purchaseValue
etc.

Esto permite:

- tendencias;
- gráficos;
- comparaciones;
- anomalías;
- evitar pedir todo el histórico siempre.

==================================================
27. AUDITORÍA
    ==================================================

Cada insight debe poder explicar:

- qué regla se activó;
- qué valores usó;
- benchmark;
- periodo;
- comparación.

Ejemplo:

CPA:
$31

Benchmark:
$18

Diferencia:
+72%

ROAS:
1,1

Benchmark:
3,2

Esto debe poder visualizarlo el planner.

==================================================
28. NO INVENTAR CAUSAS
    ==================================================

Separar siempre:

HECHO
INTERPRETACIÓN
RECOMENDACIÓN

Incorrecto:

"El público está saturado."

Correcto:

"La frecuencia aumentó mientras el CTR disminuyó, una combinación compatible con posible saturación o fatiga creativa."

No afirmar causalidad sin evidencia.

==================================================
29. IA OPCIONAL
    ==================================================

La IA puede recibir:

- summary;
- insights;
- comparisons;
- alerts;
- recommendations;
- métricas relevantes.

Funciones:

- explicar resultados;
- crear resumen ejecutivo;
- responder preguntas;
- redactar conclusiones;
- adaptar lenguaje para cliente.

Nunca permitir que IA altere métricas oficiales.

==================================================
30. UX
    ==================================================

Priorizar:

claridad > decoración.

Evitar:

- listas gigantes;
- tarjetas innecesarias;
- información duplicada;
- nombres truncados sin contexto;
- exceso de colores.

Usar:

- búsqueda;
- filtros;
- tablas;
- badges;
- comparación visual;
- drill-down;
- tooltips.

==================================================
31. REGLA PARA META MARKETING API
    ==================================================

Antes de implementar cualquier campo:

verificar en documentación oficial:

- endpoint;
- field;
- level;
- breakdown;
- attribution;
- permissions;
- version de Graph API;
- posibles restricciones.

Nunca inventar nombres de campos.

Nunca asumir que todos los breakdowns son compatibles con todas las métricas.

==================================================
32. SEGURIDAD
    ==================================================

Nunca colocar:

Meta access tokens
App Secret
Client Secret
DB passwords

en frontend.

Usar variables de entorno.

Tokens sensibles deben vivir únicamente en backend seguro.

No imprimir tokens completos en logs.

==================================================
33. DESARROLLO INCREMENTAL
    ==================================================

No construir todo de golpe.

FASE 0
Auditar proyecto recibido.

FASE 1
Base del proyecto.

FASE 2
Clientes y cuentas.

FASE 3
Meta Marketing API.

FASE 4
Selector de campañas.

FASE 5
Métricas y normalización.

FASE 6
Persistencia histórica.

FASE 7
Comparaciones.

FASE 8
AnalysisEngine.

FASE 9
Reglas e insights.

FASE 10
Dashboard.

FASE 11
Audiencias y creatividades.

FASE 12
Reporte cliente.

FASE 13
PDF.

FASE 14
Links compartibles.

FASE 15
IA opcional.

==================================================
34. PRIMERA TAREA OBLIGATORIA AL RECIBIR EL PROYECTO
    ==================================================

NO MODIFICAR CÓDIGO TODAVÍA.

Primero realizar auditoría.

Entregar:

1. Árbol de carpetas.
2. Stack detectado.
3. Frontend detectado.
4. Backend detectado.
5. Base de datos detectada.
6. Integración con Meta encontrada.
7. Integración con Claude encontrada.
8. Generador PDF encontrado.
9. Autenticación.
10. Modelo multi-cliente.
11. Dependencias.
12. Variables de entorno.
13. Código reutilizable.
14. Código incompleto.
15. Problemas de seguridad.
16. Problemas arquitectónicos.
17. Riesgos.
18. Qué falta para alcanzar el objetivo.

Después emitir una conclusión:

A. Continuar sobre la base actual.
B. Reutilizar partes y reconstruir otras.
C. Reconstruir desde cero.

Justificar técnicamente.

NO comenzar implementación hasta terminar esta auditoría.

==================================================
35. SEGUNDA TAREA
    ==================================================

Después de la auditoría:

crear un PLAN.md.

Debe contener:

- arquitectura final;
- módulos;
- entidades;
- endpoints;
- estructura frontend;
- estructura backend;
- modelo de datos;
- integración Meta;
- motor de análisis;
- reporting;
- fases de implementación.

No implementar múltiples fases simultáneamente.

==================================================
36. PRINCIPIO DE IMPLEMENTACIÓN
    ==================================================

En cada tarea:

1. Revisar código relacionado.
2. Explicar brevemente qué se modificará.
3. Modificar solo lo necesario.
4. Mantener compatibilidad.
5. Compilar.
6. Ejecutar tests.
7. Corregir errores.
8. Informar archivos modificados.
9. Explicar resultado.
10. Indicar siguiente paso.

Nunca dejar código deliberadamente roto.

==================================================
37. DEFINICIÓN DE ÉXITO
    ==================================================

El sistema está completo cuando un planner puede:

1. Iniciar sesión.
2. Seleccionar cliente.
3. Seleccionar cuenta Meta.
4. Seleccionar periodo.
5. Elegir campañas.
6. Elegir métricas.
7. Generar análisis.
8. Comparar resultados.
9. Identificar problemas.
10. Identificar oportunidades.
11. Revisar campañas.
12. Revisar Ad Sets.
13. Revisar Ads.
14. Revisar audiencias.
15. Revisar creatividades.
16. Generar insights.
17. Generar recomendaciones.
18. Crear reporte.
19. Descargar PDF.
20. Compartir reporte con cliente.

Todo lo anterior debe funcionar SIN CLAUDE.

Claude, OpenAI u otra IA debe ser opcional.
