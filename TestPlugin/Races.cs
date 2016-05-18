using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;
using KSP.UI.Screens;
using System.Text;

namespace Races
{
    /// <summary>
    /// Esta clase debería servir para comunicar el mod con el juego, como que no casque el mod al cambiar de escena, poner la compativilidad con cKan (no parece que esto se haga aquí), barras de botones...
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Flight, true)]
    public class Races : MonoBehaviour
    {
        public static Races raceMod;
        private static string raceTrackFolder = "./GameData/RaceTracks/";
        private static string raceTrackFileExtension = ".krt";

        public static RaceManager raceMan;
        public Rect guiBox = new Rect();
        public ApplicationLauncher apl;
        public bool guiAct;
        private bool appAct;
        public Texture appTexture = null;

        public static string RaceTrackFolder
        {
            get
            {
                return raceTrackFolder;
            }
        }

        public static string RaceTrackFileExtension
        {
            get
            {
                return raceTrackFileExtension;
            }
        }

        // Called after the scene is loaded.
        void Awake()
        {
            Debug.LogWarning("Awake Races");
            if (raceMod == null)
            {
                DontDestroyOnLoad(gameObject);
                raceMod = this;
            }
            else if (raceMod != this)
            {
                Destroy(gameObject);
            }

            Assembly ass = Assembly.GetExecutingAssembly();
            AssemblyName assName = ass.GetName();
            Version ver = assName.Version;
            Debug.LogWarning("Races! " + ver.ToString());

            GameEvents.onHideUI.Add(GUIoff);
            GameEvents.onShowUI.Add(GUIon);
            GameEvents.onLevelWasLoaded.Add(whatTheScene);
            GameEvents.onGameSceneSwitchRequested.Add(changeScene);
            GameEvents.onVesselSOIChanged.Add(byeSoi);
            GUIon();

            raceMan = new GameObject().AddComponent<RaceManager>();
            raceMan.GetRacetrackList();
        }

        // Called next.
        void Start()
        {
            apl = ApplicationLauncher.Instance;
            apl.AddModApplication(appOn, appOff, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT, appTexture);
        }

        private void byeSoi(GameEvents.HostedFromToAction<Vessel, CelestialBody> data)
        {
            raceMan.newRaceTrack();
            raceMan.cambiaEditCp(0);
            raceMan.GetRacetrackList();
        }

        private void changeScene(GameEvents.FromToAction<GameScenes, GameScenes> data)
        {
            if (data.from == GameScenes.FLIGHT)
            {
                raceMan.lastLoadedTrack = raceMan.trackToclone(raceMan.loadedTrack);
                raceMan.cambiaEstado(RaceManager.estados.LoadScreen);
            }
            if (data.to != GameScenes.FLIGHT)
            {
                GUIoff();
            }
        }

        private void whatTheScene(GameScenes data)
        {
            if (data == GameScenes.FLIGHT)
            {
                if (raceMan.lastLoadedTrack.bodyName == FlightGlobals.ActiveVessel.mainBody.name)
                {
                    raceMan.LoadRaceTrack(raceMan.lastLoadedTrack); // Se supone que carga la ultima carrera que ha sido cargada cuando se vuelve a la escena de vuelo
                    raceMan.cambiaEstado(RaceManager.estados.LoadScreen);
                }
                else
                {
                    raceMan.newRaceTrack();
                }
                GUIon();
            }
            else
            {
                GUIoff();
            }
        }

        private void appOff()
        {
            appAct = false;
        }

        private void appOn()
        {
            appAct = true;
        }

        public void OnGUI()
        {
            if (guiAct && appAct)
            {
                guiBox = GUILayout.Window(1, guiBox, raceMan.windowFuction, "Races!", GUILayout.MinWidth(250));
            }
        }

        public void GUIon()
        {
            guiAct = true;
        }

        public void GUIoff()
        {
            guiAct = false;
        }

        // Called every frame  
        void Update() { }

        // Called at a fixed time interval determined by the physics time step.
        void FixedUpdate() { }

        //Called when the game is leaving the scene (or exiting). Perform any clean up work here.
        void OnDestroy()
        {
            GameEvents.onHideUI.Remove(GUIoff);
            GameEvents.onShowUI.Remove(GUIon);
            GameEvents.onLevelWasLoaded.Remove(whatTheScene);
            GameEvents.onGameSceneSwitchRequested.Remove(changeScene);
            GameEvents.onVesselSOIChanged.Remove(byeSoi);
            Destroy(raceMan);
            Destroy(this);
        }
    }
}

public class CheckPoint : MonoBehaviour
{
    public enum Types { START, CHECKPOINT, FINISH };
    public Types cpType;
    public CelestialBody body;
    public Vector3 pCoords; //posición del marcador en lon lat alt
    private Vector3 coords;
    public Quaternion rot;  //rotación marcador
    public BoxCollider boxCollider = new GameObject().AddComponent<BoxCollider>(); //colisionador

    //lineas del marcador
    public static Color colorStart = Color.white;
    public static Color colorCheckP = Color.yellow;
    public static Color colorFinish = Color.red;
    public static Color colorPasado = Color.clear;
    public static Color colorEdit = new Color(255, 0, 255);
    public static Dictionary<int, Vector3> sizes = new Dictionary<int, Vector3>() {
        {0, new Vector3(4f, 32f, 18f)}, //Grosor de la linea, ancho del rectángulo, alto del rectángulo
        {1, new Vector3(6f, 48f, 27f)},
        {2, new Vector3(8F, 64f, 36f)},
        {3, new Vector3(10F, 80f, 45f)}
    };

    private int size;
    private Color wpColor;
    public LineRenderer marcador = new GameObject().AddComponent<LineRenderer>();

    /// <summary>
    /// Da color a las lineas del punto de control
    /// </summary>
    public Color cpColor
    {
        get
        {
            return wpColor;
        }

        set
        {
            wpColor = value;
            marcador.material.SetColor("_EmissiveColor", wpColor);
        }
    }

    /// <summary>
    /// Al establecer el tipo de punto de control (Start, Checkpoint, Finish), da a las lineas el color adecuado.
    /// </summary>
    public Types tipoCp
    {
        get
        {
            return cpType;
        }

        set
        {
            cpType = value;
            switch (value)
            {
                case Types.START:
                    marcador.material.SetColor("_EmissiveColor", colorStart);
                    break;
                case Types.CHECKPOINT:
                    marcador.material.SetColor("_EmissiveColor", colorCheckP);
                    break;
                case Types.FINISH:
                    marcador.material.SetColor("_EmissiveColor", colorFinish);
                    break;
                default:
                    marcador.material.SetColor("_EmissiveColor", Color.white);
                    break;
            }
        }
    }

    public int Size
    {
        get
        {
            return size;
        }

        set
        {
            size = value;
            marcador.SetWidth(sizes[Size].x, sizes[Size].x);
            boxCollider.transform.localScale = new Vector3(sizes[Size].y, 1, sizes[Size].z);
        }
    }

    public Vector3 Coords
    {
        get
        {
            return coords;
        }
    }

    public class colision : MonoBehaviour
    {
        void Start() { }

        void OnTriggerEnter()
        {
            if (Races.Races.raceMan.estadoAct == RaceManager.estados.RaceScreen)
            {
                Races.Races.raceMan.cpSuperado(this.name);
            }
        }
    }

    void Awake()
    {
        //Donde está el buque en el momento de crear el punto de control
        pCoords = new Vector3((float)FlightGlobals.ActiveVessel.latitude, (float)FlightGlobals.ActiveVessel.longitude, (float)FlightGlobals.ActiveVessel.altitude);
        body = FlightGlobals.ActiveVessel.mainBody;
        rot = FlightGlobals.ActiveVessel.transform.rotation;
        Size = 0;
        marcador.useWorldSpace = false;
        marcador.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
        marcador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        marcador.SetWidth(sizes[Size].x, sizes[Size].x);
        marcador.SetVertexCount(5);
        marcador.enabled = true;

        //colisionador
        boxCollider.gameObject.AddComponent<colision>();
        boxCollider.isTrigger = true;
        boxCollider.transform.localScale = new Vector3(sizes[Size].y, 0, sizes[Size].z);
        boxCollider.enabled = false;
    }

    /// <summary>
    /// Coloca el colisionador y dibuja las lineas del punto de control
    /// </summary>
    void Update()
    {
        //que el punto de control no se meta por debajo del suelo
        if (pCoords.z - body.TerrainAltitude(pCoords.x, pCoords.y) <= 0)
        {
            pCoords.z = (float)body.TerrainAltitude(pCoords.x, pCoords.y);
        }

        //Como el origen del mundo se mueve con el buque, esto mantiene el punto de control en una posicion fija respecto al planeta.
        coords = body.GetWorldSurfacePosition(pCoords.x, pCoords.y, pCoords.z);

        //Coloca los vertices del rectángulo
        Vector3 si = new Vector3(-(sizes[Size].y / 2), 0, sizes[Size].z / 2);
        Vector3 sd = new Vector3(sizes[Size].y / 2, 0, sizes[Size].z / 2);
        Vector3 id = new Vector3(sizes[Size].y / 2, 0, -(sizes[Size].z / 2));
        Vector3 ii = new Vector3(-(sizes[Size].y / 2), 0, -(sizes[Size].z / 2));

        marcador.transform.position = coords;
        marcador.transform.rotation = rot; //Esto es lo mismo que multiplicar cada vector por rot

        //dibuja los vertices del rectángulo
        marcador.SetPosition(0, si);
        marcador.SetPosition(1, sd);
        marcador.SetPosition(2, id);
        marcador.SetPosition(3, ii);
        marcador.SetPosition(4, si);

        //El colisionador va en el mismo sitio que el rectángulo
        boxCollider.transform.position = coords;
        boxCollider.transform.rotation = rot;
    }

    public void destroy()
    {
        Destroy(this);
        Destroy(marcador);
        marcador = null;
        Destroy(boxCollider);
        boxCollider = null;
    }

    /// <summary>
    /// Rota el colisionador y las lineas del punto de control, alrededor de los ejes y en la cantidad de grados especificados, cada vez que se llama.
    /// </summary>
    /// <param name="xAngle"></param>
    /// <param name="yAngle"></param>
    /// <param name="zAngle"></param>
    public void rotateRwp(float xAngle, float yAngle, float zAngle)
    {
        rot *= Quaternion.Euler(xAngle, yAngle, zAngle);
    }

    /// <summary>
    /// Translada el colisionador y las lineas del punto de control, a lo largo del eje y en la distancia especificada, cada vez que se llama
    /// </summary>
    /// <param name="lat"></param>
    /// <param name="lon"></param>
    /// <param name="alt"></param>
    public void moveRwp(float lat, float lon, float alt)
    {
        pCoords.x += lat;
        pCoords.y += lon;
        pCoords.z += alt;
    }
}

[Serializable]
public class RaceClon
{
    public string bodyName;
    public string name;
    public string author;
    public int laps;
    public float lenght;
    public CheckPointClon[] cpList;
}

[Serializable]
public class CheckPointClon
{
    public string body;  //¿Conmo convertir un string a un celestialbody?
    public int size;
    //posición del marcador lon lat alt
    public float pCoordsX;
    public float pCoordsY;
    public float pCoordsZ;
    //rotación del marcador pitch roll yaw ¿w?
    public float rotX;
    public float rotY;
    public float rotZ;
    public float rotW;
}

public class LoadedTrack
{
    public string bodyName;
    public string name;
    public string author;

    private static int maxRaceWaypoints { get; } = 30; //cantidad máxima de puntos de control de una carrera, por si sirve para algo.
    public int laps;
    public List<CheckPoint> cpList = new List<CheckPoint>();


    /// <summary>
    /// Calcula la longitud del circuito. La distancia entre puntos de control se calcula en linea recta (supongo)
    /// </summary>
    /// <returns></returns>
    public float trackLength
    {
        get
            {
            CelestialBody body = FlightGlobals.ActiveVessel.mainBody;
            float length = 0;
            if (cpList.Count > 1)
            {
                for (int i = 1; i < cpList.Count; i++)
                {
                    length += Vector3.Distance(cpList[i - 1].Coords, cpList[i].Coords);
                }

                if (laps > 1)
                {
                    length += Vector3.Distance(cpList[0].Coords, cpList[cpList.Count - 1].Coords);
                    length *= laps;
                }
                return length;
            }
            else
            {
                return 0;
            }
        }

    }
}

/// <summary>
/// Clase que administra las carreras
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager raceManager;
    public enum estados { LoadScreen, EditScreen, RaceScreen, EndScreen, Test };
    public estados estadoAct = estados.LoadScreen;
    public List<RaceClon> raceList = new List<RaceClon>(); //Lista de carreras disponibles en el directorio
    public LoadedTrack loadedTrack = new LoadedTrack();  //Carrera que se va a usar para correr o editar.
    private int editionCp = 0;
    public RaceClon lastLoadedTrack = new RaceClon(); //Esto valdrá (supongo) para cargar de nuevo un circuito al volver a la escena de vuelo

    //Carrera
    public bool enCarrera = false;
    public int pActivo;
    public int curLap;
    public double tiempoIni = 0;
    public double tiempoTot = 0;
    public double tiempoAct = 0;

    //GUI
    public Rect guiWindow = new Rect();
    public string guiRaceName, guiRaceAuth;
    public Vector2 scrollRaceList = new Vector2(0, 0);
    public float trackLength;
    ////tamaño, rotación y translación para los puntos de control
    public float rotx, roty, rotz, trax, tray, traz = 0;
    public int size = 0;
    ////Styles
    public float rotLabelWidth = 38f;
    public float editSliderWidth = 100f;
    public float nameLabelWidth = 38f;
    public float nameTextWidth = 150f;
    public float cardLabelWidth = 55f;

    void Awake()
    {
        Debug.LogWarning("RaceManager Awake");
        if (raceManager == null)
        {
            DontDestroyOnLoad(gameObject);
            raceManager = this;
        }
        else if (raceManager != this)
        {
            Destroy(gameObject);
        }
    }

    void Start() { }

    void onDestroy() { }

    void Update()
    {
        if (enCarrera)
        {
            tiempoAct = Planetarium.GetUniversalTime() - tiempoIni;
        }
    }

    public void cambiaEstado(estados estado)
    {
        switch (estado)
        {
            case estados.LoadScreen:
                estadoAct = estados.LoadScreen;
                prepCp(false);
                enCarrera = false;
                break;
            case estados.EditScreen:
                estadoAct = estados.EditScreen;
                //Cuando se edita un circuito el punto de control editable de forma predeterminada es el último
                //- Pero Vloos, si solo hay un punto de control, ese va a ser el primero, así que no podrá ser el editable.
                //- Sí, es el primero, pero también es el último. La condición para que sea editable es que sea el último. No "Esclusivamente el ultimo".
                //- Es un 50% último, 50% primero, así que tampoco...
                //- En realidad es totalmente último y totalmente primero. Y Tambien es el del medio. Está en un estado cuántico de ordinalidad.
                prepCp(false);
                cambiaEditCp(loadedTrack.cpList.Count - 1);
                trackLength = loadedTrack.trackLength;
                break;
            case estados.RaceScreen:
                prepCp(true);
                loadedTrack.cpList[0].boxCollider.enabled = true;
                estadoAct = estados.RaceScreen;
                pActivo = 0;
                curLap = 0;
                break;
            case estados.EndScreen:
                estadoAct = estados.EndScreen;
                tiempoTot = tiempoAct;
                break;
            case estados.Test:
                //estado para quitar esos molestos bichos y probar cosas
                estadoAct = estados.Test;
                break;
            default:
                break;
        }
    }

    public void windowFuction(int id)
    {
        switch (estadoAct)
        {
            case estados.LoadScreen:
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label(raceList.Count.ToString() + " Race Tracks Loaded");
                GUILayout.BeginScrollView(scrollRaceList, GUILayout.Height(250), GUILayout.Width(150));

                foreach (RaceClon race in raceList)
                {
                    if (race.bodyName == FlightGlobals.ActiveVessel.mainBody.name)
                    {
                        if (GUILayout.Button(race.name + " by " + race.author + "\n" + race.laps + " Laps, " + race.lenght.ToString("0.00") + " meters"))
                        {
                            newRaceTrack();
                            LoadRaceTrack(race);
                            prepCp(false);
                            //trackLength = loadedTrack.trackLength; //Esto no va...
                            trackLength = race.lenght;
                        }
                    }
                    else
                    {
                        GUILayout.Label(race.name + " by " + race.author + "\n" + race.bodyName);
                    }
                }

                GUILayout.EndScrollView();
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                if (GUILayout.Button("New Race Track"))
                {
                    newRaceTrack();
                    cambiaEstado(estados.EditScreen);
                }

                if (loadedTrack.cpList.Count > 0)
                {
                    GUILayout.Label(loadedTrack.name + " by " + loadedTrack.author);
                    GUILayout.Label(loadedTrack.cpList.Count + " Checkpoints," + loadedTrack.laps + " Laps");
                    GUILayout.Label(trackLength.ToString("0.00") + " Meters");

                    if (loadedTrack.cpList.Count > 1)
                    {
                        if (GUILayout.Button("Start Race"))
                        {
                            cambiaEstado(estados.RaceScreen);
                        }
                    }

                    if (GUILayout.Button("Edit Race Track"))
                    {
                        cambiaEstado(estados.EditScreen);
                    }

                    if (GUILayout.Button("Clear Race Track"))
                    {
                        newRaceTrack();
                    }
                }

                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                break;
            case estados.EditScreen:
                GUILayout.Label("Race Track Editor");

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", GUILayout.Width(nameLabelWidth));
                loadedTrack.name = GUILayout.TextField(loadedTrack.name, GUILayout.Width(nameTextWidth));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Author", GUILayout.Width(nameLabelWidth));
                loadedTrack.author = GUILayout.TextField(loadedTrack.author, GUILayout.Width(nameTextWidth));
                GUILayout.EndHorizontal();
                GUILayout.Label("Laps");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("1"))
                {
                    loadedTrack.laps = 1;
                }
                if (GUILayout.Button("-"))
                {
                    if (loadedTrack.laps > 1)
                    {
                        loadedTrack.laps--;
                    }
                }
                GUILayout.Label(loadedTrack.laps.ToString());
                if (GUILayout.Button("+"))
                {
                    loadedTrack.laps++;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Length");
                GUILayout.Label(trackLength.ToString());
                GUILayout.EndHorizontal();

                if (GUILayout.Button("New Checkpoint"))
                {
                    CheckPoint cp = new GameObject().AddComponent<CheckPoint>();
                    if (loadedTrack.cpList.Count == 1)
                    {
                        cp.tipoCp = CheckPoint.Types.START;
                        trackLength = loadedTrack.trackLength;
                    }
                    else
                    {
                        if (loadedTrack.cpList.Count > 2)
                        {
                            loadedTrack.cpList[loadedTrack.cpList.Count - 2].tipoCp = CheckPoint.Types.CHECKPOINT;
                        }
                        cp.tipoCp = CheckPoint.Types.FINISH;
                    }
                    cp.boxCollider.name = "cp" + loadedTrack.cpList.Count;
                    loadedTrack.cpList.Add(cp);
                    cambiaEditCp(loadedTrack.cpList.Count - 1);

                }

                if (GUILayout.Button("Remove Checkpoint"))
                {
                    loadedTrack.cpList[editionCp].destroy();
                    loadedTrack.cpList.RemoveAt(editionCp);
                    cambiaEditCp(editionCp);
                }

                if (GUILayout.Button("Save Race Track"))
                {
                    if (loadedTrack.cpList.Count > 0)
                    {
                        SaveRaceTrack();
                        raceList.Clear();
                        GetRacetrackList();
                    }
                }

                if (GUILayout.Button("New Race Track"))
                {
                    newRaceTrack();
                }

                if (loadedTrack.cpList.Count > 1)
                {
                    if (GUILayout.Button("Start Race!"))
                    {
                        cambiaEstado(estados.RaceScreen);
                    }
                }

                if (GUILayout.Button("Back"))
                {
                    cambiaEstado(estados.LoadScreen);
                }

                GUILayout.EndVertical();

                if (loadedTrack.cpList.Count > 0)
                {
                    GUILayout.BeginVertical();

                    GUILayout.Label("Edit Checkpoint");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("|<")) //First
                    {
                        cambiaEditCp(0);
                    }
                    if (GUILayout.Button("<")) //previous
                    {
                        cambiaEditCp(editionCp - 1);
                        trackLength = loadedTrack.trackLength;
                    }
                    GUILayout.Label(editionCp.ToString());
                    if (GUILayout.Button(">"))  //next
                    {
                        cambiaEditCp(editionCp + 1);
                        trackLength = loadedTrack.trackLength;
                    }
                    if (GUILayout.Button(">|")) //last
                    {
                        cambiaEditCp(loadedTrack.cpList.Count - 1);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Size");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("-"))
                    {
                        if (size > 0)
                        {
                            size--;
                            loadedTrack.cpList[editionCp].Size = size;
                        }
                    }
                    GUILayout.Label(size.ToString());
                    if (GUILayout.Button("+"))
                    {
                        if (size < CheckPoint.sizes.Count - 1)
                        {
                            size++;
                            loadedTrack.cpList[editionCp].Size = size;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Rotate");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Pitch", GUILayout.Width(rotLabelWidth));
                    rotx = GUILayout.HorizontalSlider(rotx, -0.75f, 0.75f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Roll", GUILayout.Width(rotLabelWidth));
                    roty = GUILayout.HorizontalSlider(roty, -0.75f, 0.75f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Yaw", GUILayout.Width(rotLabelWidth));
                    rotz = GUILayout.HorizontalSlider(rotz, -0.75f, 0.75f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Translate");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Latitude", GUILayout.Width(cardLabelWidth));
                    GUILayout.Label(loadedTrack.cpList[editionCp].pCoords.x.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("South");
                    trax = GUILayout.HorizontalSlider(trax, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("North");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Longitude", GUILayout.Width(cardLabelWidth));
                    GUILayout.Label(loadedTrack.cpList[editionCp].pCoords.y.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("West");
                    tray = GUILayout.HorizontalSlider(tray, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("East");
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Altitude", GUILayout.Width(cardLabelWidth));
                    GUILayout.Label(loadedTrack.cpList[editionCp].pCoords.z.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Down");
                    traz = GUILayout.HorizontalSlider(traz, -0.3f, 0.3f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("Up");
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();

                    if (Input.GetMouseButton(0))
                    {
                        loadedTrack.cpList[editionCp].rotateRwp(rotx, roty, rotz);
                        loadedTrack.cpList[editionCp].moveRwp(trax, tray, traz);
                        trackLength = loadedTrack.trackLength;
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        rotx = 0;
                        roty = 0;
                        rotz = 0;
                        trax = 0;
                        tray = 0;
                        traz = 0;
                        trackLength = loadedTrack.trackLength;
                    }
                }

                GUILayout.EndHorizontal();
                break;
            case estados.RaceScreen:
                GUILayout.Label(loadedTrack.name + "\nby " + loadedTrack.author);

                if (enCarrera)
                {
                    if (loadedTrack.laps > 0)
                    {
                        GUILayout.Label("Lap " + curLap + "/" + loadedTrack.laps);
                    }
                    GUILayout.Label(tiempo((float)tiempoAct)); //Esto tiene que ser de tamaño grande.
                    if (GUILayout.Button("Abort Race!")) //Solo visible durante la carrera
                    {
                        enCarrera = !enCarrera;
                        cambiaEstado(estados.RaceScreen);
                    }
                }
                else
                {
                    if (loadedTrack.laps > 0)
                    {
                        GUILayout.Label(loadedTrack.laps + " Laps");
                    }
                    GUILayout.Label("Cross first checkpoint (white) to start race!");
                    if (GUILayout.Button("Back")) //Solo visible mientras no empieza la carrera
                    {
                        cambiaEstado(estados.LoadScreen);
                    }
                }

                break;
            case estados.EndScreen:
                GUILayout.Label(loadedTrack.name + "\nby " + loadedTrack.author);
                GUILayout.Label("Total time:\n" + tiempo((float)tiempoTot));
                if (GUILayout.Button("Restart Race"))
                {
                    cambiaEstado(estados.RaceScreen);
                }
                if (GUILayout.Button("Edit Race Track"))
                {
                    cambiaEstado(estados.EditScreen);
                }

                if (GUILayout.Button("Back")) //Solo visible mientras no empieza la carrera
                {
                    cambiaEstado(estados.LoadScreen);
                }

                break;
            case estados.Test:
                break;
            default:
                break;
        }
        GUI.DragWindow();
    }

    /// <summary>
    /// Borra un circuito y pone otro vacío en su lugar
    /// </summary>
    public void newRaceTrack()
    {
        if (loadedTrack != null)
        {
            if (loadedTrack.cpList.Count > 0)
            {
                foreach (CheckPoint rwpCol in loadedTrack.cpList)
                {
                    rwpCol.destroy();
                }
                loadedTrack.cpList.Clear();
            }
        }
        loadedTrack = new LoadedTrack();
        loadedTrack.name = "New Race Track";
        loadedTrack.author = "Anonimous";
        loadedTrack.bodyName = FlightGlobals.ActiveVessel.mainBody.name;
        loadedTrack.laps = 1;
        editionCp = 0;
        trackLength = 0;
    }

    /// <summary>
    /// Hace una copia de la carrera cargada LoadedTrack y la guarda en un archivo binario
    /// </summary>
    public void SaveRaceTrack()
    {
        //Tomar los datos de la carrera cargada (no guardable) y transferirlos a la carrera guardable
        RaceClon raceClon = trackToclone(loadedTrack);
        if (!Directory.Exists(Races.Races.RaceTrackFolder))
        {
            Directory.CreateDirectory(Races.Races.RaceTrackFolder);
        }
        BinaryFormatter bf = new BinaryFormatter();
        string filename = RemoveSpecialCharacters(raceClon.name);
        FileStream file = File.Create(Races.Races.RaceTrackFolder + filename + Races.Races.RaceTrackFileExtension);
        bf.Serialize(file, raceClon);
        file.Close();
        Debug.Log("Saved: " + raceClon.name + " - " + raceClon.author);
    }

    /// <summary>
    /// Coge una carrera de la lista de carreras y la mete en LoadedTrack 
    /// </summary>
    public void LoadRaceTrack(RaceClon raceClon)
    {
        loadedTrack = new LoadedTrack();
        lastLoadedTrack = raceClon;         //Esto supongo que valdrá para volver a cargar el circuito cuando se revierte el vuelo
        loadedTrack.name = raceClon.name;
        loadedTrack.author = raceClon.author;
        loadedTrack.bodyName = raceClon.bodyName;
        loadedTrack.laps = raceClon.laps;

        //Extraer cpList de raceclon y meterlos en loadedTrack
        for (int i = 0; i < raceClon.cpList.Length; i++)
        {
            CheckPointClon cpClon = raceClon.cpList[i];
            CheckPoint checkPoint = new GameObject().AddComponent<CheckPoint>();
            loadedTrack.cpList.Add(checkPoint);

            //asumimos que el buque está en el mismo body que el circuito y más tarde se filtran los circuitos
            checkPoint.body = FlightGlobals.ActiveVessel.mainBody;
            checkPoint.Size = cpClon.size;
            checkPoint.pCoords = new Vector3(cpClon.pCoordsX, cpClon.pCoordsY, cpClon.pCoordsZ);
            checkPoint.rot = new Quaternion(cpClon.rotX, cpClon.rotY, cpClon.rotZ, cpClon.rotW);
            checkPoint.boxCollider.name = "cp" + i; //Poner nombre a los colisionadores, para saber contra cual se colisiona
        }
        Debug.Log("Race track loaded: " + loadedTrack.name + " by " + loadedTrack.author);
    }

    /// <summary>
    /// Escanea el directorio de circuitos en busca de circuitos y llena una lista de circuitos con los circuitos encontrados
    /// </summary>
    public void GetRacetrackList()
    {
        // coger todos los archivos de tipo .krt de la carpeta y meterlos en la lista de circuitos
        if (!Directory.Exists(Races.Races.RaceTrackFolder))
        {
            Directory.CreateDirectory(Races.Races.RaceTrackFolder);
        }
        var info = new DirectoryInfo(Races.Races.RaceTrackFolder);
        var fileInfo = info.GetFiles("*.krt", SearchOption.TopDirectoryOnly);
        Debug.Log(fileInfo.Length + " Race Tracks found");
        foreach (var file in fileInfo)
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fileStream = File.Open(file.FullName, FileMode.Open);
            RaceClon race = (RaceClon)bf.Deserialize(fileStream);
            raceList.Add(race);
        }
    }

    /// <summary>
    /// Se dispara cuando el buque activo atraviesa un checkpoint. Detecta si el colisionador colisionado es el que deberia colisionarse y hace cosas en consecuencia.
    /// </summary>
    /// <param name="cpCollider">El checkpoint que ha sido atravesado (es importante poner un nombre distinto en cada colisionador)</param>
    public void cpSuperado(string cpCollider)
    {
        if (cpCollider == loadedTrack.cpList[pActivo].boxCollider.name)
        {
            switch (loadedTrack.cpList[pActivo].tipoCp)
            {
                case CheckPoint.Types.START:
                    enCarrera = true;
                    if (curLap == 0)
                    {
                        tiempoIni = Planetarium.GetUniversalTime();
                    }
                    loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorPasado;
                    curLap++;
                    break;
                case CheckPoint.Types.CHECKPOINT:
                    ScreenMessages.PostScreenMessage(tiempo((float)tiempoAct), 15);
                    loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorPasado;
                    break;
                case CheckPoint.Types.FINISH:
                    ScreenMessages.PostScreenMessage(tiempo((float)tiempoAct), 15);
                    if (curLap == loadedTrack.laps)
                    {
                        tiempoTot = tiempoAct;
                        enCarrera = false;
                        cambiaEstado(estados.EndScreen);
                        loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorPasado;
                    }
                    return;
                default:
                    break;
            }
            pActivo++;

            if (pActivo > loadedTrack.cpList.Count - 1)
            {
                if (curLap < loadedTrack.laps)
                {
                    pActivo = 0;
                }
                else
                {
                    pActivo = 0;
                    loadedTrack.cpList[0].tipoCp = CheckPoint.Types.FINISH;
                }
            }

            if (loadedTrack.cpList[pActivo].tipoCp != CheckPoint.Types.FINISH)
            {
                loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorStart;
            }
        }
        else
        {
            //Wrong checkpoint
        }
    }

    /// <summary>
    /// Prepara los checkpoints para la carrera (o no), activando (o no) los colisionadores
    /// </summary>
    /// <param name="collision">Activa la detección de colisiones en los checkpoints</param>
    /// <param name="correr">Colorear los checkpoints de una forma especial, para empezar a competir</param>
    public void prepCp(bool correr)
    {
        if (loadedTrack.cpList.Count > 0)
        {
            for (int i = 0; i < loadedTrack.cpList.Count; i++)
            {
                loadedTrack.cpList[i].boxCollider.enabled = correr;
                loadedTrack.cpList[i].cpType = CheckPoint.Types.CHECKPOINT;
                loadedTrack.cpList[i].cpColor = (correr) ? CheckPoint.colorPasado : CheckPoint.colorCheckP;
            }

            if (loadedTrack.laps == 1)
            {
                loadedTrack.cpList[loadedTrack.cpList.Count - 1].tipoCp = CheckPoint.Types.FINISH;
            }

            loadedTrack.cpList[0].tipoCp = CheckPoint.Types.START;
            editionCp = loadedTrack.cpList.Count - 1;
        }
        else
        {
            editionCp = 0;
        }
    }

    /// <summary>
    /// Cambia los colores de los checkpoints para indicar el que se está editando, conservando los colores del punto de salida y de la meta cuando dejan de ser los editables
    /// </summary>
    /// <param name="num">El index del checkpoint que se quiere editar</param>
    public void cambiaEditCp(int num)
    {
        if (loadedTrack.cpList.Count > 0)
        {
            if (editionCp <= 0)
            {
                loadedTrack.cpList[0].cpColor = CheckPoint.colorStart;
                editionCp = 0;
            }
            else if (editionCp >= loadedTrack.cpList.Count - 1)
            {
                editionCp = loadedTrack.cpList.Count - 1;
                loadedTrack.cpList[editionCp].cpColor = CheckPoint.colorFinish;
            }
            else
            {
                loadedTrack.cpList[editionCp].cpColor = CheckPoint.colorCheckP;
            }

            if (num <= 0)
            {
                num = 0;
            }
            else if (num >= loadedTrack.cpList.Count - 1)
            {
                num = loadedTrack.cpList.Count - 1;
            }
            editionCp = num;

            loadedTrack.cpList[editionCp].cpColor = CheckPoint.colorEdit;
            size = loadedTrack.cpList[editionCp].Size;
        }
        else
        {
            editionCp = 0;
        }
    }

    /// <summary>
    /// Debuelve un clon serializable de un circuito
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    public RaceClon trackToclone(LoadedTrack track)
    {
        RaceClon raceClon = new RaceClon();
        raceClon.cpList = new CheckPointClon[loadedTrack.cpList.Count];
        for (int i = 0; i < loadedTrack.cpList.Count; i++)
        {
            CheckPointClon cpClon = new CheckPointClon();
            cpClon.body = loadedTrack.cpList[i].body.name;
            cpClon.size = loadedTrack.cpList[i].Size;
            cpClon.pCoordsX = loadedTrack.cpList[i].pCoords.x;
            cpClon.pCoordsY = loadedTrack.cpList[i].pCoords.y;
            cpClon.pCoordsZ = loadedTrack.cpList[i].pCoords.z;
            cpClon.rotX = loadedTrack.cpList[i].rot.x;
            cpClon.rotY = loadedTrack.cpList[i].rot.y;
            cpClon.rotZ = loadedTrack.cpList[i].rot.z;
            cpClon.rotW = loadedTrack.cpList[i].rot.w;
            raceClon.cpList[i] = cpClon;
        }

        raceClon.name = track.name;
        raceClon.author = track.author;
        raceClon.bodyName = track.bodyName;
        raceClon.laps = track.laps;
        trackLength = track.trackLength;
        return raceClon;
    }

    /// <summary>
    /// Esta función copiada de aqui: http://stackoverflow.com/questions/1120198/most-efficient-way-to-remove-special-characters-from-string
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public string RemoveSpecialCharacters(string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// un float que representa cierta cantidad de segundos, sale en forma de cadena de texto en formato HH:MM:SS.CC
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public string tiempo(float num)
    {
        int hor = (int)(num / 3600);
        int min = (int)((num - (3600 * hor)) / 60);
        int seg = (int)(num - ((hor * 3600) + (min * 60)));

        return hor.ToString("00") + ":" + min.ToString("00") + ":" + seg.ToString("00") + "." + num.ToString(".00").Split('.')[1];
    }
}