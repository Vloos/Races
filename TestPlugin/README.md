# Races
Pequeño mod para Kerbal Space Program en el que se pueden crear circuitos, colocando puntos de control, para competir en carreras contrarreloj.

## Historial de cambios

* 0.0.9-alpha
 * Los circuitos pueden tener como mínimo 1 vuelta, de forma que la carrera empieza y termina en el mismo punto de control, o pueden ser de recorrido, empezando y terminando en puntos de control diferentes.
 * Un problema que surgía al intentar guardar el circuito varias veces en un corto intervalo de tiempo. Intentaba abrir y escribir en un archivo en el que ya estaba escribiendo. Parece ser que eso hace enloquecer al sistema de archivos y todo dejaba de funcionar. Ahora aparece un aviso "Busy" y da la oportunidad de seguir esperando, o no. De eso se trata.
 * Puntos de control y obstáculos no guardaban correctamente su orientación cuando se guardaban con la orientación reiniciada. Al cargar un circuito estos elementos aparecían con la orientación del vehículo en el momento de crearlos. O con otra orientación. No lo sé. El caso es que ahora funciona correctamente.

* 0.0.8-alpha
 * Los puntos de control y los obstáculos se pueden mover y rotar con chirimbolos (también conocidos como "guizmos").
 * De pronto ha aparecido un botón en las ventanas de edición de puntos de control y obstáculos que devuelve el punto de control u obstáculo al suelo.
 * Arreglado un problema (eso espero) que hacía que tanto los puntos de control como los obstáculos perdieran su orientación con respecto al suelo cuando se revertía el vuelo.
 * Odio los cuaternios
 * He añadido el MiniAVC
 * Los circuitos guardados antes de la versión 0.0.7-alpha ya no son compatibles (utiliza la versión 0.0.7-alpha para convertirlos)

* 0.0.7-alpha
 * Cambio algunos deslizadores por botones, para precisión suprema.
 * Se podrán colocar puntos de control y obstáculos con el ratón pulsando Control Derecho y Botón Izquierdo del Ratón. Si la cámara está a demasiada altura puede que aparezcan en el aire.
 * Los archivos krt (Kerbal Race Track) se guardan de forma diferente. Carga un circuito, edítalo y guárdalo (no hace falta modificarlo) para guardarlo correctamente (quiero eliminar esa capacidad de convertir los circuito de un formato a otro en el próximo lanzamiento)
 * Le he toqueteado alguna que otra cosa a la interfaz gráfica de usuario, en especial al dialogo de confirmación de guardado de un circuito existente. Puede que funcione un poco menos mal que antes.

* 0.0.6-alpha
 * Se pueden poner obstáculos simples y estáticos, formados por aburridos cubos grises reescalables.

* 0.0.5-alpha
 * Puntos de control un poco menos feos
 * Penalización de 10 segundos al tocar los bordes del punto de control activo (blanco) durante la carrera

* 0.0.4-alpha
 * El punto de control inmediato se pone blanco; el siguiente, amarillo.
 * Unos cambios en los momentos en los que se generan las claves de los circuitos.
 * Ahora el editor asume que te estás confundiendo cuando quieres guardar el circuito y te pide una confirmación si encuentra un circuito con el mismo nombre.
 * Se ha diseñado cuidadosamente un icono muy feo para la barra de aplicaciones
 * La forma en la que se considera que un punto de control es superado ha cambiado bastante (más información https://github.com/Vloos/Races/commit/f5824b74dd8188da72c4b6910037a5307459a449)

* 0.0.3-alpha
 * Los mejores tiempos se guardan (más información: https://github.com/Vloos/Races/commit/db380160c6ea7c6ef5e6d7312b9e9ba590e6a2bf)
 * La interfaz gráfica de usuario tiene mejor pinta
  * Por medio de la ciencia, ahora la ventana se ajusta mejor a lo que tiene dentro.
  * La ristra de botones de los circuitos disponibles se puede desplazar verticalmente.
  * Más información en los botones que se encargan de cargar los circuitos.
  * Se puede ver la longitud total del circuito en los botones y en la ventana de edición.
  * También hay algo más de información sobre los puntos de control en la ventana de edición.
  * El botón para empezar la carrera no aparece cuando solo hay un punto de control, cosa que nunca debería haber sucedido.
 * Al poner puntos de control en un circuito los puntos de control nuevos reciben nombre. Antes no tenían nombre y al entrar en contacto con uno se activaban todos.
 * El tiempo en el cronometro se muestra en formato HH:MM:SS.CC
 * Los puntos de control no deberían poder estar por debajo del suelo.
 * Los puntos de control se activan cuando la parte raíz los cruza

* 0.0.2-alpha
 * Se pueden hacer circuitos de varias vueltas
 * Los puntos de control pueden ser de distintos tamaños

* 0.0.1-alpha
 * Cambios sin documentar entre los que destaca el cambio de aspecto de los puntos de control. De extremadamente horribles a tan solo feos.
