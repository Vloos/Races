# Races! 1.0.1 (No Totalmente Arreglado)

Races! Es un mod para Kerbal Space Program que consiste en la construcción e intercambio de circuitos de carreras aéreas, terrestres y acuáticas en las que competir para completarlas en el menor tiempo posible.

## Cargar un circuito:
Races! Busca archivos .krt (kerbal race track) en la carpeta RaceTracks, situada dentro de GameData. Cada circuito encontrado se lista en forma de botón en la pantalla principal de la interfaz gráfica de Races! Los circuitos situados en otros planetas aparecen en forma de texto (tal vez se pueda hacer más bonito eso, pero no es importante ahora mismo) Al pulsar el botón, el circuito se carga y se muestra más información:
* Nombre del circuito y autor
* Tipo de circuito, una etapa o vueltas, junto con la longitud total del circuito.
* Mejor tiempo registrado
* Posición del punto de inicio del circuito, en longitud, latitud y altitud, y la distancia que lo separa de la nave controlada.
También aparen dos o tres botones con funciones bastante obvias: Start Race, sólo si el circuito tiene dos o más puntos de control, para empezar la carrera. Edit Race Track para editar el circuito y Clear Race Track para que desaparezca el circuito de la faz del planeta (El circuito desaparece al abandonar la esfera de influencia).

## Empezar a correr:
Cuando el circuito tiene dos o más puntos de control, puede empezar una competición. Pulsa el botón Star Race desde la pantalla de carga de circuitos o desde la pantalla de edición para empezar una carrera.
El tiempo empieza a correr cuando se atraviesa el primer punto de control, de color blanco. El color blanco indica el punto de control que se debe atravesar, el color amarillo indica el punto de control siguiente. Una vez atravesado el punto de control correcto este se volverá gris transparente.
En los circuitos de una etapa el último punto de control aparece de color rojo. En los circuitos de varias vueltas el último punto de control aparece de color rojo sólo en la última vuelta.
Tocar los bordes del punto de control blanco (o rojo, si es el que toca atravesar) penaliza con 10 segundos.
Si el tiempo total del circuito es menor que el record registrado, se guarda automáticamente. Si recorrido o el número de vueltas del circuito cambia, los tiempos se reinician. Se pueden guardar los tiempos de un mismo circuito para cada cantidad de vueltas independientemente, con tan solo cambiar el número de vueltas en la pantalla de edición.
La carrera puede interrumpirse pulsando Abort Race. La carrera queda interrumpida al salir de la escena de vuelo.
Cuando se completa la carrera aparecen las opciones:
* Restart Race: Vuelve a comenzar la carrera. El cronómetro vuelve a 0 y los puntos de control se colorean de nuevo para mostrar el punto de control inicial y el siguiente.
* Edit Race Track: Pasa al modo de edición de circuitos, en el que se puede modificar sus características.
* Load Track: Pantalla de carga de circuitos.

##Editar o Crear un circuito:
Desde la pantalla de carga de circuitos pulsar New Race Track, o Edit Race Track para entrar en el modo de edición de circuitos.
En la ventana se propone introducir alguna información acerca del circuito:
* Name: El nombre del circuito. El archivo krt se guarda con el mismo nombre sin caracteres especiales.
* Author: El autor del circuito, para ser mundialmente reconocido, o repudiado.
* Laps: Configurar el número de vueltas necesarias para completar el circuito, o circuito de una etapa.
Para crear un punto de control, al pulsar en New Checkpoint here se crea un punto de control en la posición y orientación del vehículo controlado. Al pulsar la tecla Control Derecho y Botón Izquierdo del Ratón se crea un punto de control ecuatorialmente alineado en la posición del ratón (conviene mencionar que a veces el ratón no está donde parece)
Al crear, mover o borrar un punto de control, o al cambiar el número de vueltas del circuito, la distancia total se actualiza.
Se pueden crear obstáculos del mismo modo. Para crear un obstáculo en la posición del ratón pulsar Mayúsculas Derecha y Boton Izquierdo del Ratón. Los obstáculos son creados como cubos no sólidos de 50m de lado (para evitar posibles explosiones).
Para seleccionar puntos de control u obstáculos puede hacerse por medio del ratón, pulsando Botón Izquierdo del Ratón sobre un punto de control u obstáculo para seleccionarlo, o mediante la interfaz gráfica, pulsando en Edit Checkpoints (disponible cuando el circuito tiene puntos de control) o Edit Obstacles (disponible cuando el circuito tiene obstáculos), y recorriendo la lista de puntos de control u obstáculos mediante los botones:
* |< : primero de la lista
* < : anterior
* > : siguiente
* >| : último de la lista

Tanto los puntos de control como los obstáculos pueden moverse y rotarse mediante los chirimbolos. Los mismos utilizados en el VAB o SPH. Pulsa Control Izquierdo para evitar el ajuste a la rejilla o al ángulo. Si acaban a demasiada altura o bajo tierra, utiliza Send to floor para ajustar su altitud a la altitud del suelo.
Los puntos de control pueden adoptar 5 tamaños diferentes pulsando los botones + y - .
Los obstáculos pueden redimensionarse desde 1m hasta 100m por cada lado:
* |- : 1m de lado
* -- : 0.2m menos por cada fotograma que se mantiene pulsado
* - : 0.1m menos por cada pulsación
* + : 0.1m más por cada pulsación
* ++ : 0.2m más por cada fotograma que se mantiene pulsado
* +| : 100m de lado

El punto de control u obstáculo seleccionado puede eliminarse pulsando Remove Checkpoint o Remove Obstacle, respectivamente.
Cuando el circuito tiene al menos un punto de control puede guardarse pulsando el botón Save Track. Al hacerlo, se genera o sobrescribe un archivo de extensión krt en la carpeta RaceTracks nombrado igual que el nombre del circuito exceptuando los caracteres especiales.

## Bugs conocidos:
* Ir muy rápido al atravesar los puntos de control puede causar que en un fotograma la nave esté de un lado del punto de control y en el siguiente fotograma ya haya quedado atrás. Trata de frenar un poco la próxima vez. No hay tanta prisa.
* Si la nave utilizada para competir es más grande que los puntos de control, puede suceder que, si la parte raíz está demasiado lejos y alguna parte de la nave toca los bordes de un punto de control, se acumule gran cantidad de tiempo de penalización (sin probar, pero por cómo está programado me hace sospechar que ocurrirá…)
* Los puntos de control se descuajaringan visualmente al moverse o rotarse, pero todo parece indicar que funcionan más o menos como se espera.

## Bugs desconocidos:
* Por supuesto.


## Historial de cambios

* 1.0.1
 * Compilado para Kerbal Space Program 1.2.1
 * He desterrado esos malditos "foreach".
 * Los chirimbolos de translación y rotación no funcionan (por alguna extraña razón), así que he puesto unos botones, que funcionan fatal, para mover y rotar los puntos de control y los obstáculos

* 1.0.0
 * Ahora que todo funciona decentemente (desde mi punto de vista), estoy en condiciones de hacer un lanzamiento completo.
 * Debido a los cambios en los elementos con los que se construyen los circuitos (https://github.com/Vloos/Races/commit/619e13d2f5fa73b10cf9214336ac817f22c72340), los circuitos guardados en las versiones alpha no son compatibles. Supongo que siendo versión alpha, era de esperar (y en parte me escudo en “Eh, es una versión alpha, las cosas se pueden romper” para no admitir que no tengo ni idea de cómo hacer compatibles los archivos de circuitos antiguos).
 * Rediseño parcial de la interfaz gráfica de usuario utilizada para editar circuitos.

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
