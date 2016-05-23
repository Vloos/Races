# Races
Pequeño mod para Kerbal Space Program en el que se pueden crear circuitos, colocando puntos de control, para competir en carreras contrarreloj.

## Historial de cambios

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
  * El boton para empezar la carrera no aparece cuando solo hay un punto de control, cosa que nunca debería haber sucedido.
 * Al poner puntos de control en un circuito los puntos de control nuevos reciven nombre. Antes no tenian nombre y al entrar en contacto con uno se activaban todos.
 * El tiempo en el cronometro se muestra en formato HH:MM:SS.CC
 * Los puntos de control no deberían poder estar por debajo del suelo.
 * Los puntos de control se activan cuando la parte raiz los cruza

* 0.0.2-alpha
 * Se pueden hacer circuitos de varias vueltas
 * Los puntos de control pueden ser de distintos tamaños

* 0.0.1-alpha
 * Cambios sin documentar entre los que destaca el cambio de aspecto de los puntos de control. De extremadamente horribles a tan solo feos.