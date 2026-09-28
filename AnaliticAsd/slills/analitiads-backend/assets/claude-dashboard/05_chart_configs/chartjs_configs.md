# Configuración completa de gráficos Chart.js — Dashboard C&P

> Todas las funciones fueron copiadas literalmente del código fuente (`cp-dashboard-meta.html`). Chart.js versión usada: **4.5.0**.
> Config global aplicada una vez al cargar: `Chart.defaults.color="#cbd3da"`, `Chart.defaults.borderColor="rgba(255,255,255,.08)"`, `Chart.defaults.font.family="'Roboto','Segoe UI',Arial,sans-serif"`.

## 1. `volumeCostChart(cid, labels, vol, cost, volLabel, costLabel, costFmt)`
Usado en: tarjetas KPI de Resultados (barra de volumen + línea de costo, doble eje Y).
```js
function volumeCostChart(cid,labels,vol,cost,volLabel,costLabel,costFmt){
  destroy(cid);
  CHARTS[cid]=new Chart(document.getElementById(cid),{
    data:{labels,datasets:[
      {type:"bar",label:volLabel,data:vol,backgroundColor:hexA(LILA,.5),borderColor:LILA,yAxisID:"y",order:2},
      {type:"line",label:costLabel,data:cost,borderColor:"#C9CD30",backgroundColor:"transparent",tension:.35,pointRadius:0,borderWidth:2,yAxisID:"y2",order:1}
    ]},
    options:{
      responsive:true,maintainAspectRatio:false,
      interaction:{mode:"index",intersect:false},
      plugins:{
        legend:{position:"bottom",labels:{font:{size:9},boxWidth:10}},
        tooltip:{callbacks:{label:c=>c.dataset.label===volLabel
          ? (volLabel+": "+fmtInt(c.parsed.y))
          : (costLabel+": "+(costFmt==="dec2"?fmtDec(c.parsed.y,2):costFmt==="money4"?("$"+fmtDec(c.parsed.y,4)):fmtMoney(c.parsed.y)))}}
      },
      scales:{
        x:{grid:{display:false},ticks:{maxTicksLimit:8,font:{size:9}}},
        y:{position:"left",ticks:{font:{size:9},callback:v=>compact(v)}},
        y2:{position:"right",grid:{display:false},ticks:{font:{size:9},callback:v=>costFmt==="dec2"?fmtDec(v,2):costFmt==="money4"?("$"+fmtDec(v,3)):("$"+fmtDec(v,2))}}
      }
    }
  });
}
```

## 2. `lineChart(cid, labels, data, label, color, money)`
Usado en: gráfico simple de una serie (ej. ingresos por día en la vista Global).
```js
function lineChart(cid,labels,data,label,color,money){
  destroy(cid);
  CHARTS[cid]=new Chart(document.getElementById(cid),{
    type:"line",
    data:{labels,datasets:[{
      label,data,borderColor:color,backgroundColor:hexA(color,.14),
      fill:true,tension:.35,pointRadius:0,pointHoverRadius:5,pointHitRadius:18,
      pointHoverBackgroundColor:color,pointHoverBorderColor:"#fff",borderWidth:2
    }]},
    options:{
      responsive:true,maintainAspectRatio:false,
      interaction:{mode:"index",intersect:false},
      plugins:{
        legend:{display:false},
        tooltip:{mode:"index",intersect:false,callbacks:{label:c=>label+": "+(money?fmtMoney(c.parsed.y):fmtInt(c.parsed.y))}}
      },
      scales:{
        x:{grid:{display:false},ticks:{maxTicksLimit:8,font:{size:9}}},
        y:{ticks:{font:{size:9},callback:v=>compact(v)}}
      }
    }
  });
}
```

## 3. `imprCpmChart(cid, labels, impr, cpm)`
Usado en: histórico de marca / comparativas Impresiones vs CPM.
```js
function imprCpmChart(cid,labels,impr,cpm){
  destroy(cid);
  CHARTS[cid]=new Chart(document.getElementById(cid),{
    data:{labels,datasets:[
      {type:"bar",label:"Impresiones",data:impr,backgroundColor:hexA(LILA,.5),borderColor:LILA,yAxisID:"y",order:2},
      {type:"line",label:"CPM",data:cpm,borderColor:"#C9CD30",backgroundColor:"transparent",tension:.35,pointRadius:0,borderWidth:2,yAxisID:"y2",order:1}
    ]},
    options:{
      responsive:true,maintainAspectRatio:false,
      interaction:{mode:"index",intersect:false},
      plugins:{
        legend:{position:"bottom",labels:{font:{size:9},boxWidth:10}},
        tooltip:{callbacks:{label:c=>c.dataset.label==="CPM" ? ("CPM: "+fmtMoney(c.parsed.y)) : ("Impresiones: "+fmtInt(c.parsed.y))}}
      },
      scales:{
        x:{grid:{display:false},ticks:{maxTicksLimit:8,font:{size:9}}},
        y:{position:"left",ticks:{font:{size:9},callback:v=>compact(v)}},
        y2:{position:"right",grid:{display:false},ticks:{font:{size:9},callback:v=>"$"+fmtDec(v,2)}}
      }
    }
  });
}
```

## 4. `barChart(cid, labels, female, male, unknown)`
Usado en: distribución por género en Comunidad (barras agrupadas Mujeres/Hombres/N-D).
```js
function barChart(cid,labels,female,male,unknown){
  destroy(cid);
  const ds=[{label:"Mujeres",data:female,backgroundColor:LILA},{label:"Hombres",data:male,backgroundColor:"#C9A8F5"}];
  if(unknown && unknown.some(x=>x>0)) ds.push({label:"N/D",data:unknown,backgroundColor:GRIS});
  CHARTS[cid]=new Chart(document.getElementById(cid),{
    type:"bar",data:{labels,datasets:ds},
    options:{
      responsive:true,maintainAspectRatio:false,
      plugins:{
        legend:{position:"bottom",labels:{font:{size:10},boxWidth:12}},
        tooltip:{callbacks:{label:c=>c.dataset.label+": "+fmtInt(c.parsed.y)}}
      },
      scales:{
        x:{grid:{display:false},ticks:{font:{size:10}}},
        y:{ticks:{font:{size:9},callback:v=>compact(v)}}
      }
    }
  });
}
```

## 5. `pieChart(cid, labels, data, colors)`
Usado en: distribución por ciudad/región (donut) en Comunidad.
```js
function pieChart(cid,labels,data,colors){
  destroy(cid);
  CHARTS[cid]=new Chart(document.getElementById(cid),{
    type:"doughnut",
    data:{labels,datasets:[{data,backgroundColor:colors||PALETTE,borderColor:"#1B232A",borderWidth:2}]},
    options:{
      responsive:true,maintainAspectRatio:false,cutout:"55%",
      plugins:{
        legend:{position:"bottom",labels:{font:{size:10},boxWidth:12}},
        tooltip:{callbacks:{label:c=>{
          const tot=c.dataset.data.reduce((a,b)=>a+b,0);
          return c.label+": "+fmtInt(c.parsed)+" ("+fmtDec(c.parsed/tot*100,1)+"%)";
        }}}
      }
    }
  });
}
```

## 6. `platOpts(stack)` — opciones compartidas por los 3 gráficos de "Ubicación por plataforma"
```js
function platOpts(stack){
  return {
    responsive:true,maintainAspectRatio:false,
    plugins:{
      legend:{position:"bottom",labels:{font:{size:10},boxWidth:12}},
      tooltip:{callbacks:{label:function(c){return c.dataset.label+": "+fmtInt(c.parsed.y);}}}
    },
    scales:{
      x:{stacked:!!stack,grid:{display:false},ticks:{font:{size:10}}},
      y:{stacked:!!stack,ticks:{font:{size:9},callback:function(v){return compact(v);}}}
    }
  };
}
// Uso real (3 instancias, todas type:"bar"):
CHARTS["chPlatReach"] = new Chart(ctx, {type:"bar", data:{labels, datasets:[
  {label:"Alcance", data: rows.map(r=>r.reach), backgroundColor:"#9D5BF0"},
  {label:"Impresiones", data: rows.map(r=>r.impr), backgroundColor:"#C9A8F5"}
]}, options: platOpts()});

CHARTS["chPlatEng"] = new Chart(ctx, {type:"bar", data:{labels, datasets:[
  {label:"Interacciones", data: rows.map(r=>r.eng), backgroundColor:"#9D5BF0"}
]}, options: platOpts()});

CHARTS["chPlatGen"] = new Chart(ctx, {type:"bar", data:{labels, datasets:[
  {label:"Mujeres", data: rows.map(r=>Math.round(r.reach*fr)), backgroundColor:"#9D5BF0"},
  {label:"Hombres", data: rows.map(r=>Math.round(r.reach*mr)), backgroundColor:"#C9A8F5"}
]}, options: platOpts(true)}); // este último usa el género ESTIMADO, ver catálogo de métricas
```

## 7. `multiLineChart(cid, labels, datasets)` — vista Global, "Compras por canal"/"Compras por campaña"
```js
var GROUP_COLORS=[LILA,"#3ed27e","#f0b34a","#ff6b6b","#4ea1ff","#e774d0","#7cd9c6","#c9a86a"];
function multiLineChart(cid,labels,datasets){
  destroy(cid);
  CHARTS[cid]=new Chart(document.getElementById(cid),{
    type:"line",
    data:{labels,datasets:datasets.map(function(d,i){
      var c=GROUP_COLORS[i%GROUP_COLORS.length];
      return {label:d.label,data:d.data,borderColor:c,backgroundColor:hexA(c,.10),fill:false,tension:.35,
        pointRadius:0,pointHoverRadius:5,pointHitRadius:18,pointHoverBackgroundColor:c,pointHoverBorderColor:"#fff",borderWidth:2};
    })},
    options:{
      responsive:true,maintainAspectRatio:false,
      interaction:{mode:"index",intersect:false},
      plugins:{
        legend:{display:true,position:"bottom",labels:{boxWidth:10,font:{size:10}}},
        tooltip:{mode:"index",intersect:false,callbacks:{label:c=>c.dataset.label+": "+fmtInt(c.parsed.y)}}
      },
      scales:{
        x:{grid:{display:false},ticks:{maxTicksLimit:8,font:{size:9}}},
        y:{ticks:{font:{size:9},callback:v=>compact(v)}}
      }
    }
  });
}
```

## 8. `chBrand` — histórico de marca (multi-serie con doble eje Y)
```js
CHARTS["chBrand"]=new Chart(document.getElementById("chBrand"),{
  data:{labels:months,datasets:[
    {type:"bar",label:"Alcance",data:data.map(r=>pNum(r.reach)),backgroundColor:hexA(LILA,.5),borderColor:LILA,yAxisID:"y",order:3},
    {type:"line",label:"Impresiones",data:data.map(r=>pNum(r.impressions)),borderColor:"#C9A8F5",backgroundColor:"transparent",tension:.35,yAxisID:"y",order:2},
    {type:"line",label:"Interacciones",data:data.map(r=>pNum(r["actions:page_engagement"])),borderColor:"#7d8893",backgroundColor:"transparent",tension:.35,yAxisID:"y",order:1},
    {type:"line",label:"Tasa interacción %",data:data.map(r=>{const im=pNum(r.impressions);return im?pNum(r["actions:page_engagement"])/im*100:0;}),borderColor:"#C9CD30",backgroundColor:"transparent",tension:.35,yAxisID:"y2",order:0}
  ]},
  options:{
    responsive:true,maintainAspectRatio:false,
    interaction:{mode:"index",intersect:false},
    plugins:{legend:{position:"bottom",labels:{font:{size:10},boxWidth:12}}},
    scales:{
      y:{position:"left",ticks:{callback:v=>compact(v),font:{size:9}}},
      y2:{position:"right",grid:{display:false},ticks:{callback:v=>fmtDec(v,1)+"%",font:{size:9}}}
    }
  }
});
```

## 9. Funciones auxiliares usadas por todos los gráficos
```js
function compact(v){v=+v;if(v>=1e6)return (v/1e6).toFixed(1)+"M";if(v>=1e3)return Math.round(v/1e3)+"k";return v;}
function hexA(hex,a){const n=parseInt(hex.slice(1),16);return `rgba(${(n>>16)&255},${(n>>8)&255},${n&255},${a})`;}
function destroy(cid){if(CHARTS[cid]){CHARTS[cid].destroy();delete CHARTS[cid];}}
```

## 10. Paleta y colores usados en gráficos
```js
const LILA="#9D5BF0", LILA_DEEP="#640AE6", NEGRO="#cbd3da", GRIS="#6b7681";
const PALETTE=["#9D5BF0","#C9A8F5","#7d8893","#640AE6","#C9CD30","#566069"];
var GROUP_COLORS=[LILA,"#3ed27e","#f0b34a","#ff6b6b","#4ea1ff","#e774d0","#7cd9c6","#c9a86a"];
```
Notas:
- El amarillo-verde `#C9CD30` se reserva consistentemente para líneas de "costo" (el eje secundario) en los gráficos de doble eje.
- El lila (`#9D5BF0`/`#640AE6`) es siempre la serie principal/volumen.
- Todos los charts usan `responsive:true, maintainAspectRatio:false` — dependen de que su contenedor (`.chart-box`, alturas fijas por CSS: `height:120px`, `.tall{height:240px}`, `.pie{height:230px}`) les dé una altura explícita.
