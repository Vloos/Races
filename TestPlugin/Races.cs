using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Reflection;
using KSP.UI.Screens;
using System.Text;
using System.Security.Cryptography;


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
        public ApplicationLauncher apl;
        public Texture appTexture = GameDatabase.Instance.GetTexture("Races!/Textures/icon", false);

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

            raceMan = new GameObject().AddComponent<RaceManager>();

            GameEvents.onHideUI.Add(GUIoff);
            GameEvents.onShowUI.Add(GUIon);
            GameEvents.onLevelWasLoaded.Add(whatTheScene);
            GameEvents.onGameSceneSwitchRequested.Add(changeScene);
            GameEvents.onVesselSOIChanged.Add(byeSoi);
            GUIon();
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
            raceMan.raceList.Clear();
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
            raceMan.appAct = false;
        }

        private void appOn()
        {
            raceMan.appAct = true;
        }

        public void GUIon()
        {
            raceMan.guiAct = true;
        }

        public void GUIoff()
        {
            raceMan.guiAct = false;
        }

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

/// <summary>
/// Punto de control con primitivos
/// </summary>
public class CheckPoint : MonoBehaviour
{
    public enum Types { START, CHECKPOINT, FINISH };
    public Types cpType;
    public CelestialBody body;
    public Vector3 pCoords; //posición del marcador en lon lat alt
    private Vector3 coords;
    public Quaternion rot;  //rotación marcador
    public static int maxAlt = 50000;
    public GameObject cpBoxTrigger = GameObject.CreatePrimitive(PrimitiveType.Cube); //Esto detecta el buque pasar, para validar el punto de control
    private bool solid;
    private bool penalization;
    private int size;
    private Color wpColor;
    public Qub cuUp, cuDown, cuLe, cuRi;

    public static Color colorStart = Color.white;
    public static Color colorCheckP = Color.yellow;
    public static Color colorFinish = Color.red;
    public static Color colorPasado = Color.clear;
    public static Color colorEdit = new Color(255, 0, 255);
    public static Dictionary<int, Vector3> sizes = new Dictionary<int, Vector3>() {
        {0, new Vector3(2f, 32f, 18f)}, //Grosor de la linea, ancho del rectángulo, alto del rectángulo
        {1, new Vector3(3f, 48f, 27f)},
        {2, new Vector3(4F, 64f, 36f)},
        {3, new Vector3(5F, 80f, 45f)}
    };

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
            cuUp.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
            cuDown.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
            cuRi.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
            cuLe.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
        }
    }

    /// <summary>
    /// Al establecer el tipo de punto de control (Start, Checkpoint, Finish), da al marcador el color adecuado.
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
                    cuUp.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorStart);
                    cuDown.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorStart);
                    cuRi.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorStart);
                    cuLe.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorStart);
                    break;
                case Types.CHECKPOINT:
                    cuUp.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorCheckP);
                    cuDown.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorCheckP);
                    cuRi.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorCheckP);
                    cuLe.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorCheckP);
                    break;
                case Types.FINISH:
                    cuUp.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorFinish);
                    cuDown.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorFinish);
                    cuRi.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorFinish);
                    cuLe.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", colorFinish);
                    break;
                default:
                    cuUp.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", Color.white);
                    cuDown.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", Color.white);
                    cuRi.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", Color.white);
                    cuLe.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", Color.white);
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
            cpBoxTrigger.transform.localScale = new Vector3(sizes[Size].y, sizes[Size].x + 0.2f, sizes[Size].z);
            cuUp.transform.localPosition = new Vector3(0, 0, sizes[Size].z / 2);
            cuDown.transform.localPosition = new Vector3(0, 0, -sizes[Size].z / 2);
            cuLe.transform.localPosition = new Vector3(-sizes[Size].y / 2, 0, 0);
            cuRi.transform.localPosition = new Vector3(sizes[Size].y / 2, 0, 0);

            cuUp.transform.localScale = new Vector3(sizes[Size].y + sizes[Size].x, sizes[Size].x, sizes[Size].x);
            cuDown.transform.localScale = new Vector3(sizes[Size].y + sizes[Size].x, sizes[Size].x, sizes[Size].x);
            cuRi.transform.localScale = new Vector3(sizes[Size].x, sizes[Size].x, sizes[Size].z - sizes[Size].x);
            cuLe.transform.localScale = new Vector3(sizes[Size].x, sizes[Size].x, sizes[Size].z - sizes[Size].x);
        }
    }

    public Vector3 Coords
    {
        get
        {
            return coords;
        }
    }

    public bool Solid
    {
        get
        {
            return solid;
        }

        set
        {
            solid = value;
            cuUp.cubo.gameObject.GetComponent<BoxCollider>().isTrigger = !solid;
            cuDown.cubo.gameObject.GetComponent<BoxCollider>().isTrigger = !solid;
            cuLe.cubo.gameObject.GetComponent<BoxCollider>().isTrigger = !solid;
            cuRi.cubo.gameObject.GetComponent<BoxCollider>().isTrigger = !solid;
        }
    }

    public bool Penalization
    {
        get
        {
            return penalization;
        }

        set
        {
            penalization = value;
            cuUp.cubo.GetComponent<timePenalization>().enabled = penalization;
            cuDown.cubo.GetComponent<timePenalization>().enabled = penalization;
            cuLe.cubo.GetComponent<timePenalization>().enabled = penalization;
            cuRi.cubo.GetComponent<timePenalization>().enabled = penalization;
        }
    }

    public class colision : MonoBehaviour
    {
        public int count = 0;

        void OnTriggerEnter(Collider thing)
        {
            //Cuando el numero de partes +1 que han pasado por el punto de control son más de la mitad de las partes del buque, el punto de control se considera "pasado"
            if (Races.Races.raceMan.estadoAct == RaceManager.estados.RaceScreen && name == Races.Races.raceMan.loadedTrack.cpList[Races.Races.raceMan.pActivo].cpBoxTrigger.name)
            {
                count++;
                if (count + 1 >= FlightGlobals.ActiveVessel.parts.Count / 2)
                {
                    Races.Races.raceMan.cpSuperado(name);
                    count = 0;
                }
            }
        }
    }

    public class timePenalization : MonoBehaviour
    {
        void OnTriggerEnter()
        {
            if (enabled)
            {
                Races.Races.raceMan.penTime += 10;
                ScreenMessages.PostScreenMessage("+10 sec penalty", 5);
                Races.Races.raceMan.loadedTrack.cpList[Races.Races.raceMan.pActivo].Penalization = false;
            }
        }
    }

    public class Qub : MonoBehaviour
    {
        public GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);

        void Awake()
        {
            cubo.AddComponent<timePenalization>();
            cubo.gameObject.GetComponent<timePenalization>().enabled = false;
            cubo.transform.parent = transform;
            cubo.GetComponent<MeshRenderer>().material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
            cubo.GetComponent<BoxCollider>().isTrigger = true;
            cubo.GetComponent<BoxCollider>().enabled = true;
        }

        void OnDestroy()
        {
            Destroy(cubo);
            Destroy(this);
        }
    }

    void Awake()
    {
        //Donde está el buque en el momento de crear el punto de control
        pCoords = new Vector3((float)FlightGlobals.ActiveVessel.latitude, (float)FlightGlobals.ActiveVessel.longitude, (float)FlightGlobals.ActiveVessel.altitude);
        body = FlightGlobals.ActiveVessel.mainBody;
        rot = FlightGlobals.ActiveVessel.transform.rotation;
        size = 0;
        wpColor = colorCheckP;

        //colisionador
        cpBoxTrigger.GetComponent<BoxCollider>().gameObject.AddComponent<colision>();
        cpBoxTrigger.transform.parent = this.transform;
        cpBoxTrigger.GetComponent<BoxCollider>().isTrigger = true;
        cpBoxTrigger.transform.localScale = new Vector3(sizes[Size].y, sizes[Size].x + 0.2f, sizes[Size].z);
        cpBoxTrigger.GetComponent<BoxCollider>().enabled = false;
        cpBoxTrigger.GetComponent<Renderer>().enabled = false;

        //cubos
        cuUp = new GameObject().AddComponent<Qub>();
        cuDown = new GameObject().AddComponent<Qub>();
        cuLe = new GameObject().AddComponent<Qub>();
        cuRi = new GameObject().AddComponent<Qub>();

        cuUp.transform.parent = transform;
        cuDown.transform.parent = transform;
        cuLe.transform.parent = transform;
        cuRi.transform.parent = transform;

        cuUp.transform.localPosition = new Vector3(0, 0, sizes[Size].z / 2);
        cuDown.transform.localPosition = new Vector3(0, 0, -sizes[Size].z / 2);
        cuLe.transform.localPosition = new Vector3(-sizes[Size].y / 2, 0, 0);
        cuRi.transform.localPosition = new Vector3(sizes[Size].y / 2, 0, 0);

        cuUp.transform.localScale = new Vector3(sizes[Size].y + sizes[Size].x, sizes[Size].x, sizes[Size].x);
        cuDown.transform.localScale = new Vector3(sizes[Size].y + sizes[Size].x, sizes[Size].x, sizes[Size].x);
        cuRi.transform.localScale = new Vector3(sizes[Size].x, sizes[Size].x, sizes[Size].z - sizes[Size].x);
        cuLe.transform.localScale = new Vector3(sizes[Size].x, sizes[Size].x, sizes[Size].z - sizes[Size].x);

    }

    /// <summary>
    /// Coloca el punto de control. Todo el rato.
    /// </summary>
    void Update()
    {
        //que el punto de control no se meta por debajo del suelo
        if (pCoords.z - body.TerrainAltitude(pCoords.x, pCoords.y) <= 0)
        {
            pCoords.z = (float)body.TerrainAltitude(pCoords.x, pCoords.y);
        }

        if (pCoords.z > maxAlt)
        {
            pCoords.z = maxAlt;
        }

        //Como el origen del mundo se mueve con el buque, esto mantiene el punto de control en una posicion fija respecto al planeta.
        coords = body.GetWorldSurfacePosition(pCoords.x, pCoords.y, pCoords.z);

        this.transform.position = coords;
        this.transform.rotation = rot;
    }

    public void destroy()
    {
        Destroy(cuUp);
        Destroy(cuDown);
        Destroy(cuLe);
        Destroy(cuRi);
        Destroy(cpBoxTrigger);
        cpBoxTrigger = null;
        Destroy(this);
    }

    /// <summary>
    /// Rota el punto de cotrol alrededor de los ejes y en la cantidad de grados especificados, cada vez que se llama.
    /// </summary>
    /// <param name="xAngle"></param>
    /// <param name="yAngle"></param>
    /// <param name="zAngle"></param>
    public void rotateRwp(float xAngle, float yAngle, float zAngle)
    {
        rot *= Quaternion.Euler(xAngle, yAngle, zAngle);
    }

    /// <summary>
    /// Translada el punto de control, a lo largo del eje y en la distancia especificada, cada vez que se llama
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

public class LoadedTrack
{
    public string bodyName;
    public string name;
    public string author;
    public string trackKey;
    public float trackTime;

    private static int maxRaceWaypoints { get; } = 30; //cantidad máxima de puntos de control de una carrera, por si sirve para algo.
    public int laps;
    public List<CheckPoint> cpList = new List<CheckPoint>();
    public List<Obstacle> obList = new List<Obstacle>();

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

[Serializable]
public class RaceClon
{
    public string bodyName;
    public string name;
    public string author;
    public int laps;
    public float lenght;
    public string key;
    public CheckPointClon[] cpList;
    public Obstacle.ObsClon[] obList;
}

[Serializable]
public class Records
{
    public string[] key;
    public float[] value;
}

[Serializable]
public class keyGenData
{
    public CheckPointClon[] cpList;
    public int laps;
}

/// <summary>
/// Clase que administra las carreras
/// </summary>
public class RaceManager : MonoBehaviour
{
    public static RaceManager raceManager;
    public enum estados { LoadScreen, EditScreen, RaceScreen, EndScreen, ObsScreen, Test };
    public estados estadoAct = estados.LoadScreen;
    public List<RaceClon> raceList = new List<RaceClon>(); //Lista de carreras disponibles en el directorio
    public LoadedTrack loadedTrack = new LoadedTrack();  //Carrera que se va a usar para correr o editar.
    private int editionCp = 0;
    private int editionOb = 0;
    public RaceClon lastLoadedTrack = new RaceClon(); //Esto valdrá (supongo) para cargar de nuevo un circuito al volver a la escena de vuelo
    public Dictionary<string, float> records = new Dictionary<string, float>() { { "0", 0 } };

    //Carrera
    public bool enCarrera = false;
    public int pActivo;
    public int curLap;
    public double tiempoIni = 0d;
    public double tiempoTot = 0d;
    public double tiempoAct = 0d;
    public double penTime = 0d;

    //GUI
    public bool guiAct;
    public bool appAct;
    public Rect guiBox = new Rect();
    public string guiRaceName, guiRaceAuth;
    public Vector2 scrollRaceList = Vector2.zero;
    public float trackLength;
    bool trackExist, saving = false;
    ////tamaño, rotación y translación para los puntos de control, y para obstáculos
    public float rotx, roty, rotz, trax, tray, traz = 0;
    public int size = 0;
    public float obrotx, obroty, obrotz, obtrax, obtray, obtraz, obscalex, obscaley, obscalez;
    ////Styles
    public float rotLabelWidth = 38f;
    public float editSliderWidth = 100f;
    public float nameLabelWidth = 38f;
    public float nameTextWidth = 150f;
    public float cardLabelWidth = 35f;

    void Awake()
    {
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

    void Start()
    {
        GetRacetrackList();
    }

    void Update()
    {
        if (enCarrera)
        {
            tiempoAct = Planetarium.GetUniversalTime() - tiempoIni;
            if (Vector3.Distance(FlightGlobals.ActiveVessel.CoM, loadedTrack.cpList[pActivo].Coords) > CheckPoint.sizes[loadedTrack.cpList[pActivo].Size].y && !loadedTrack.cpList[pActivo].Penalization)
            {
                loadedTrack.cpList[pActivo].Penalization = true;
            }
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
                loadedTrack.trackKey = genTrackKey();
                loadedTrack.trackTime = (records.ContainsKey(loadedTrack.trackKey)) ? records[loadedTrack.trackKey] : 0;
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
                if (loadedTrack.obList.Count > 0)
                {
                    loadedTrack.obList[editionOb].ObColor = Obstacle.colorNormal;
                }
                editionOb = 0;
                break;
            case estados.RaceScreen:
                prepCp(true);
                loadedTrack.cpList[0].cpBoxTrigger.GetComponent<BoxCollider>().enabled = true;
                estadoAct = estados.RaceScreen;
                pActivo = 0;
                curLap = 0;
                penTime = 0;
                if (loadedTrack.cpList[1].tipoCp != CheckPoint.Types.FINISH)
                {
                    loadedTrack.cpList[1].cpColor = CheckPoint.colorCheckP;
                }
                break;
            case estados.EndScreen:
                estadoAct = estados.EndScreen;
                recordRecord();
                saveRecordFile();
                prepCp(false);
                break;
            case estados.ObsScreen:
                estadoAct = estados.Test;
                prepCp(false);
                if (loadedTrack.obList.Count > 0)
                {
                    cambiaEditOb(loadedTrack.obList.Count - 1);
                }
                break;
            case estados.Test:
                //estado para quitar esos molestos bichos y probar cosas
                break;
            default:
                break;
        }
    }

    public void OnGUI()
    {
        if (guiAct && appAct)
        {
            guiBox = GUILayout.Window(1, guiBox, windowFuction, "Races!", GUILayout.Width(0), GUILayout.Height(0));
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

                scrollRaceList = GUILayout.BeginScrollView(scrollRaceList, GUILayout.Height(250), GUILayout.Width(180));

                foreach (RaceClon race in raceList)
                {
                    if (race.bodyName == FlightGlobals.ActiveVessel.mainBody.name)
                    {
                        if (GUILayout.Button(race.name + " by " + race.author + "\n" + race.laps + " Laps, " + race.lenght.ToString("0.00") + " meters", GUILayout.MaxWidth(170)))
                        {
                            newRaceTrack();
                            LoadRaceTrack(race);
                            prepCp(false);
                            //trackLength = loadedTrack.trackLength; //Esto no va...
                            trackLength = race.lenght;
                            loadedTrack.trackTime = (records.ContainsKey(loadedTrack.trackKey)) ? records[loadedTrack.trackKey] : 0;
                        }
                    }
                    else
                    {
                        GUILayout.Label(race.name + " by " + race.author + "\n" + race.bodyName);
                    }
                }

                GUILayout.EndScrollView();
                if (GUILayout.Button("New Race Track"))
                {
                    newRaceTrack();
                    cambiaEstado(estados.EditScreen);
                }
                GUILayout.EndVertical();
                GUILayout.BeginVertical();
                if (loadedTrack.cpList.Count > 0)
                {
                    // Información del circuito cargado. No se asusten.
                    GUILayout.Label(loadedTrack.name + " by " + loadedTrack.author + "\n" + loadedTrack.cpList.Count + " Checkpoints\n" + loadedTrack.laps + " Laps\n" + trackLength.ToString("0.00") + " Meters\nBest time: " + tiempo(loadedTrack.trackTime));
                    GUILayout.Label("Starting point:\n" + "Latitude: " + loadedTrack.cpList[0].pCoords.x + "\nLongitude: " + loadedTrack.cpList[0].pCoords.y + "\nAltitude: " + loadedTrack.cpList[0].pCoords.z + "\nDistance: " + Vector3.Distance(loadedTrack.cpList[0].Coords, FlightGlobals.ActiveVessel.CoM).ToString("0.00"));
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
                    cp.cpBoxTrigger.GetComponent<BoxCollider>().name = "cp" + loadedTrack.cpList.Count;
                    loadedTrack.cpList.Add(cp);
                    cambiaEditCp(loadedTrack.cpList.Count - 1);
                }

                if (loadedTrack.cpList.Count > 0)
                {
                    if (GUILayout.Button("Remove Checkpoint"))
                    {
                        loadedTrack.cpList[editionCp].destroy();
                        loadedTrack.cpList.RemoveAt(editionCp);
                        cambiaEditCp(editionCp);
                    }
                }

                if (GUILayout.Button("Edit Obstacles"))
                {
                    cambiaEstado(estados.ObsScreen);
                }

                if (GUILayout.Button("Save Race Track"))
                {
                    if (loadedTrack.cpList.Count > 0)
                    {
                        trackExist = (raceList.FindAll(x => x.name == loadedTrack.name).Count != 0);

                        if (!trackExist)
                        {
                            SaveRaceTrack();
                            raceList.Clear();
                            GetRacetrackList();
                            trackExist = false;
                            saving = false;
                        }
                        else
                        {
                            saving = true;
                        }
                    }
                }
                if (saving)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Already exist");
                    if (GUILayout.Button("Overwrite"))
                    {
                        SaveRaceTrack();
                        raceList.Clear();
                        GetRacetrackList();
                        trackExist = false;
                        saving = false;
                    }
                    if (GUILayout.Button("Cancel"))
                    {
                        trackExist = false;
                        saving = false;
                    }
                    GUILayout.EndHorizontal();

                    /* este es el tema: quiero que cuando esté activa esta confirmación, si hay algun cambio en el circuito la confirmación desaparezca sin guardar
                     * lo que pasa es que si lo hago con esto aquí mismo:
                     * 
                     * if (GUI.changed)
                     * {
                     *  Debug.Log("GUI changed, y trackExist = false, saving = false");
                     * }
                     * 
                     * detecta el cambio en el mismo momento en que se pulsa en el botón de guardar, y tiene en cuenta esa pulsación para el cambio
                     */
                }

                if (GUILayout.Button("New Race Track"))
                {
                    newRaceTrack();
                }

                if (loadedTrack.cpList.Count > 1)
                {
                    if (GUILayout.Button("Start Race!"))
                    {
                        loadedTrack.trackKey = genTrackKey();
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

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Size");
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
                    GUILayout.Label("Latitude");
                    GUILayout.Label(loadedTrack.cpList[editionCp].pCoords.x.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("South", GUILayout.Width(cardLabelWidth));
                    trax = GUILayout.HorizontalSlider(trax, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("North", GUILayout.Width(cardLabelWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Longitude");
                    GUILayout.Label(loadedTrack.cpList[editionCp].pCoords.y.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("West", GUILayout.Width(cardLabelWidth));
                    tray = GUILayout.HorizontalSlider(tray, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("East", GUILayout.Width(cardLabelWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Altitude");
                    GUILayout.Label(loadedTrack.cpList[editionCp].pCoords.z.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Down", GUILayout.Width(cardLabelWidth));
                    traz = GUILayout.HorizontalSlider(traz, -0.3f, 0.3f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("Up", GUILayout.Width(cardLabelWidth));
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
                GUILayout.Label(loadedTrack.name + " by " + loadedTrack.author);

                if (enCarrera)
                {
                    if (loadedTrack.laps > 1)
                    {
                        GUILayout.Label("Lap " + curLap + "/" + loadedTrack.laps);
                    }
                    GUILayout.Label("Time: " + tiempo((float)tiempoAct)); //Esto tiene que ser de tamaño grande.
                    GUILayout.Label("Time penalty: " + tiempo((float)penTime));
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
                    if (GUILayout.Button("Edit Track"))
                    {
                        cambiaEstado(estados.EditScreen);
                    }
                    if (GUILayout.Button("Load Track"))
                    {
                        cambiaEstado(estados.LoadScreen);
                    }
                }

                break;
            case estados.EndScreen:
                GUILayout.Label(loadedTrack.name + " by " + loadedTrack.author);
                GUILayout.Label("Time: " + tiempo((float)tiempoAct));
                GUILayout.Label("Time penalty: " + tiempo((float)penTime));
                GUILayout.Label("Total time: " + tiempo((float)tiempoTot));
                GUILayout.Label("Best Time: " + tiempo(loadedTrack.trackTime));
                if (GUILayout.Button("Restart Race"))
                {
                    cambiaEstado(estados.RaceScreen);
                }
                if (GUILayout.Button("Edit Race Track"))
                {
                    cambiaEstado(estados.EditScreen);
                }

                if (GUILayout.Button("Load Track"))
                {
                    cambiaEstado(estados.LoadScreen);
                }
                break;
            case estados.Test:
                GUILayout.Label("Obstacle Editor");

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();
                if (GUILayout.Button("Spawn Obstacle"))
                {
                    Obstacle obs = new GameObject().AddComponent<Obstacle>();
                    loadedTrack.obList.Add(obs);
                    cambiaEditOb(loadedTrack.obList.Count - 1);
                }

                if (loadedTrack.obList.Count > 0)
                {
                    if (GUILayout.Button("Remove Obstacle"))
                    {
                        loadedTrack.obList[editionOb].destroy();
                        loadedTrack.obList.RemoveAt(editionOb);
                        cambiaEditOb(editionOb);
                    }
                }

                if (GUILayout.Button("Edit checkpoints"))
                {
                    cambiaEstado(estados.EditScreen);
                }

                GUILayout.EndVertical();

                if (loadedTrack.obList.Count > 0)
                {
                    GUILayout.BeginVertical();

                    GUILayout.Label("Edit Checkpoint");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("|<")) //First
                    {
                        cambiaEditOb(0);
                    }
                    if (GUILayout.Button("<")) //previous
                    {
                        cambiaEditOb(editionOb - 1);
                    }
                    GUILayout.Label(editionOb.ToString());
                    if (GUILayout.Button(">"))  //next
                    {
                        cambiaEditOb(editionOb + 1);
                    }
                    if (GUILayout.Button(">|")) //last
                    {
                        cambiaEditOb(loadedTrack.obList.Count - 1);
                    }
                    GUILayout.EndHorizontal();

                    loadedTrack.obList[editionOb].Solid = GUILayout.Toggle(loadedTrack.obList[editionOb].Solid, "Solid thing");

                    GUILayout.Label("Rotate");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Pitch", GUILayout.Width(rotLabelWidth));
                    obrotx = GUILayout.HorizontalSlider(obrotx, -0.75f, 0.75f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Roll", GUILayout.Width(rotLabelWidth));
                    obroty = GUILayout.HorizontalSlider(obroty, -0.75f, 0.75f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Yaw", GUILayout.Width(rotLabelWidth));
                    obrotz = GUILayout.HorizontalSlider(obrotz, -0.75f, 0.75f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Scale");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("X", GUILayout.Width(15));

                    if (GUILayout.Button("|-"))
                    {
                        obscalex = -Obstacle.maxScale;
                        obscaley = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    //if (GUILayout.RepeatButton("--") || GUILayout.Button("-")) //Esto hace cosas raras...
                    if (GUILayout.RepeatButton("--"))
                    {
                        obscalex = -0.1f;
                        obscaley = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.Button("-"))
                    {
                        obscalex = -0.1f;
                        obscaley = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    GUILayout.Label(loadedTrack.obList[editionOb].cube.transform.localScale.x.ToString());
                    if (GUILayout.Button("+") || GUILayout.RepeatButton("++")) //...Pero esto va bien
                    {
                        obscalex = 0.1f;
                        obscaley = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.Button("+|"))
                    {
                        obscalex = Obstacle.maxScale;
                        obscaley = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Y", GUILayout.Width(15));

                    if (GUILayout.Button("|-"))
                    {
                        obscaley = -Obstacle.maxScale;
                        obscalex = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.RepeatButton("--"))
                    {
                        obscaley = -0.1f;
                        obscalex = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.Button("-"))
                    {
                        obscaley = -0.1f;
                        obscalex = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    GUILayout.Label(loadedTrack.obList[editionOb].cube.transform.localScale.y.ToString());
                    if (GUILayout.Button("+") || GUILayout.RepeatButton("++"))
                    {
                        obscaley = 0.1f;
                        obscalex = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.Button("+|"))
                    {
                        obscaley = Obstacle.maxScale;
                        obscalex = 0;
                        obscalez = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Z", GUILayout.Width(15));

                    if (GUILayout.Button("|-"))
                    {
                        obscalez = -Obstacle.maxScale;
                        obscalex = 0;
                        obscaley = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.RepeatButton("--"))
                    {
                        obscalez = -0.1f;
                        obscalex = 0;
                        obscaley = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.Button("-"))
                    {
                        obscalez = -0.1f;
                        obscalex = 0;
                        obscaley = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    GUILayout.Label(loadedTrack.obList[editionOb].cube.transform.localScale.z.ToString());
                    if (GUILayout.Button("+") || GUILayout.RepeatButton("++"))
                    {
                        obscalez = 0.1f;
                        obscalex = 0;
                        obscaley = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    if (GUILayout.Button("+|"))
                    {
                        obscalez = Obstacle.maxScale;
                        obscalex = 0;
                        obscaley = 0;
                        loadedTrack.obList[editionOb].scaleOb(obscalex, obscaley, obscalez);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Label("Translate");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Latitude");
                    GUILayout.Label(loadedTrack.obList[editionOb].pCoords.x.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("South", GUILayout.Width(cardLabelWidth));
                    obtrax = GUILayout.HorizontalSlider(obtrax, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("North", GUILayout.Width(cardLabelWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Longitude");
                    GUILayout.Label(loadedTrack.obList[editionOb].pCoords.y.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("West", GUILayout.Width(cardLabelWidth));
                    obtray = GUILayout.HorizontalSlider(obtray, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("East", GUILayout.Width(cardLabelWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Altitude");
                    GUILayout.Label(loadedTrack.obList[editionOb].pCoords.z.ToString());
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Down", GUILayout.Width(cardLabelWidth));
                    obtraz = GUILayout.HorizontalSlider(obtraz, -0.3f, 0.3f, GUILayout.Width(editSliderWidth));
                    GUILayout.Label("Up", GUILayout.Width(cardLabelWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();

                    if (Input.GetMouseButton(0))
                    {
                        loadedTrack.obList[editionOb].rotateOb(obrotx, obroty, obrotz);
                        loadedTrack.obList[editionOb].moveOb(obtrax, obtray, obtraz);
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        obrotx = 0;
                        obroty = 0;
                        obrotz = 0;
                        obtrax = 0;
                        obtray = 0;
                        obtraz = 0;
                    }
                }
                GUILayout.EndHorizontal();
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

            if (loadedTrack.obList.Count > 0)
            {
                foreach (Obstacle obs in loadedTrack.obList)
                {
                    obs.destroy();
                }
                loadedTrack.obList.Clear();
            }
        }
        loadedTrack = new LoadedTrack();
        loadedTrack.name = "New Race Track";
        loadedTrack.author = "Anonimous";
        loadedTrack.bodyName = FlightGlobals.ActiveVessel.mainBody.name;
        loadedTrack.laps = 1;
        loadedTrack.trackTime = 0;
        loadedTrack.trackKey = "";
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
        loadedTrack.trackKey = raceClon.key;

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
            checkPoint.cpBoxTrigger.GetComponent<BoxCollider>().name = "cp" + i; //Poner nombre a los colisionadores, para saber contra cual se colisiona
        }

        if (raceClon.obList.Length > 0)
        {
            foreach (Obstacle.ObsClon obClon in raceClon.obList)
            {
                Obstacle obs = new GameObject().AddComponent<Obstacle>();
                obs.pCoords = new Vector3(obClon.pCoordsx, obClon.pCoordsy, obClon.pCoordsz);
                obs.rot = new Quaternion(obClon.rotx, obClon.roty, obClon.rotz, obClon.rotw);
                obs.cube.transform.localScale = new Vector3(obClon.scalex, obClon.scaley, obClon.scalez);
                obs.Solid = obClon.solid;
                loadedTrack.obList.Add(obs);
            }
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
        var fileInfo = info.GetFiles("*" + Races.Races.RaceTrackFileExtension, SearchOption.TopDirectoryOnly);
        Debug.Log(fileInfo.Length + " Race Tracks found");
        foreach (var file in fileInfo)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fileStream = File.Open(file.FullName, FileMode.Open);
                RaceClon race = (RaceClon)bf.Deserialize(fileStream);
                raceList.Add(race);
                fileStream.Close();
            }
            catch (Exception)
            {
                Debug.LogError("Wrong file format");
            }
        }
        //y tambien se carga el archivo de tiempos
        loadRecordFile();
    }

    /// <summary>
    /// Se dispara cuando el buque activo atraviesa un checkpoint. Detecta si el colisionador colisionado es el que deberia colisionarse y hace cosas en consecuencia.
    /// </summary>
    /// <param name="cpCollider">El checkpoint que ha sido atravesado (es importante poner un nombre distinto en cada colisionador)</param>
    public void cpSuperado(string cpCollider)
    {
        if (cpCollider == loadedTrack.cpList[pActivo].cpBoxTrigger.GetComponent<BoxCollider>().name)
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
                    if (loadedTrack.laps > 1 && curLap != 0)
                    {
                        ScreenMessages.PostScreenMessage("Lap " + curLap + " Checkpoint " + pActivo + "\n" + tiempo((float)tiempoAct), 15);
                    }
                    curLap++;
                    if (loadedTrack.laps > 1 && curLap == loadedTrack.laps)
                    {
                        loadedTrack.cpList[0].tipoCp = CheckPoint.Types.FINISH;
                    }
                    break;
                case CheckPoint.Types.CHECKPOINT:
                    ScreenMessages.PostScreenMessage(" Lap " + curLap + " Checkpoint " + pActivo + "\n" + tiempo((float)tiempoAct), 15);
                    loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorPasado;
                    break;
                case CheckPoint.Types.FINISH:
                    ScreenMessages.PostScreenMessage("Total time " + tiempo((float)tiempoAct), 15);
                    if (curLap == loadedTrack.laps)
                    {
                        tiempoTot = tiempoAct + penTime;
                        enCarrera = false;
                        cambiaEstado(estados.EndScreen);
                    }
                    return;
                default:
                    break;
            }
            loadedTrack.cpList[pActivo].Penalization = false; //Una vez superado el punto de control, ya no puede penalizar
            //indicar el punto de control inmediato
            pActivo++;
            if (pActivo > loadedTrack.cpList.Count - 1)
            {
                pActivo = 0;
            }
            loadedTrack.cpList[pActivo].Penalization = true; //Despues de cambiar el punto de control activo, se reinicia la deteccion de penalizacion de tiempo
            if (loadedTrack.cpList[pActivo].tipoCp != CheckPoint.Types.FINISH)
            {
                loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorStart;
            }
            //colorear el siguiente punto de control
            int next = pActivo + 1;
            if (next > loadedTrack.cpList.Count - 1)
            {
                next = 0;
            }
            if (loadedTrack.cpList[pActivo].tipoCp != CheckPoint.Types.FINISH && loadedTrack.cpList[next].tipoCp != CheckPoint.Types.FINISH)
            {
                loadedTrack.cpList[next].cpColor = CheckPoint.colorCheckP;
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
                loadedTrack.cpList[i].cpBoxTrigger.GetComponent<BoxCollider>().enabled = correr;
                loadedTrack.cpList[i].cpType = CheckPoint.Types.CHECKPOINT;
                loadedTrack.cpList[i].cpColor = (correr) ? CheckPoint.colorPasado : CheckPoint.colorCheckP;
                loadedTrack.cpList[i].cpBoxTrigger.GetComponent<BoxCollider>().GetComponent<CheckPoint.colision>().count = 0;
            }

            foreach (Obstacle obs in loadedTrack.obList)
            {
                obs.ObColor = Obstacle.colorNormal;
            }

            if (loadedTrack.laps == 1)
            {
                loadedTrack.cpList[loadedTrack.cpList.Count - 1].tipoCp = CheckPoint.Types.FINISH;
            }

            loadedTrack.cpList[0].tipoCp = CheckPoint.Types.START;
            loadedTrack.cpList[0].Penalization = correr;
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
    /// Cambia el color de los obstaculos para indicar el que se está editando
    /// </summary>
    /// <param name="num"></param>
    public void cambiaEditOb(int num)
    {
        if (loadedTrack.obList.Count > 0)
        {
            if (num < 0)
            {
                num = 0;
            }
            else if (num > loadedTrack.obList.Count - 1)
            {
                num = loadedTrack.obList.Count - 1;
            }
            if (editionOb <= loadedTrack.obList.Count-1)
            {
                loadedTrack.obList[editionOb].ObColor = Obstacle.colorNormal;
            }
            editionOb = num;
            loadedTrack.obList[editionOb].ObColor = Obstacle.colorEdit;
        }
        else
        {
            editionOb = 0;
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

        //Guarda los obstáculos
        raceClon.obList = new Obstacle.ObsClon[loadedTrack.obList.Count];
        for (int i = 0; i < loadedTrack.obList.Count; i++)
        {
            raceClon.obList[i] = loadedTrack.obList[i].toClon();
        }

        raceClon.name = track.name;
        raceClon.author = track.author;
        raceClon.bodyName = track.bodyName;
        raceClon.laps = track.laps;
        raceClon.lenght = track.trackLength;
        //Genera una clave unica partiendo de los puntos de control del circuito
        keyGenData data = new keyGenData();
        data.laps = raceClon.laps;
        data.cpList = raceClon.cpList;
        raceClon.key = MD5Hash(data);
        track.trackKey = raceClon.key;

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

    /// <summary>
    /// calcula md5 del circuito actual
    /// </summary>
    /// <returns></returns>
    public string MD5Hash(object obj)
    {
        byte[] hash = MD5.Create().ComputeHash(bArray.ObjectToByteArray(obj));
        return BitConverter.ToString(hash).Replace("-", "");
    }

    /// <summary>
    /// carga el archivo records.dat, si no lo hay, lo crea
    /// </summary>
    public void loadRecordFile()
    {
        if (!Directory.Exists(Races.Races.RaceTrackFolder))
        {
            Directory.CreateDirectory(Races.Races.RaceTrackFolder);
        }
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream fileStream = File.Open(Races.Races.RaceTrackFolder + "records.dat", FileMode.Open);
            Records cosa = (Records)bf.Deserialize(fileStream);
            fileStream.Close();
            records.Clear();
            for (int i = 0; i < cosa.key.Length; i++)
            {
                records.Add(cosa.key[i], cosa.value[i]);
            }
        }
        catch (Exception)
        {
            saveRecordFile();
        }
    }

    /// <summary>
    /// guarda en el archivo records.dat los records de tiempo de los circuitos
    /// </summary>
    public void saveRecordFile()
    {
        if (!Directory.Exists(Races.Races.RaceTrackFolder))
        {
            Directory.CreateDirectory(Races.Races.RaceTrackFolder);
        }

        //parece que no soy capaz de serializar un diccionario, asi que esto hace una copia que se puede serializar
        Records saveThis = new Records();
        saveThis.key = new string[records.Count];
        saveThis.value = new float[records.Count];
        List<string> keyList = new List<string>(this.records.Keys);
        List<float> valueList = new List<float>(this.records.Values);

        for (int i = 0; i < records.Count; i++)
        {
            saveThis.key[i] = keyList[i];
            saveThis.value[i] = valueList[i];
        }

        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Races.Races.RaceTrackFolder + "records.dat");
        bf.Serialize(file, saveThis);
        file.Close();
    }

    /// <summary>
    /// Registra un record personal en la lista de records
    /// </summary>
    public void recordRecord()
    {
        if (records.ContainsKey(loadedTrack.trackKey))
        {
            if (records[loadedTrack.trackKey] > (float)tiempoTot)
            {
                ScreenMessages.PostScreenMessage("New Record!" + tiempo((float)tiempoTot));
                records[loadedTrack.trackKey] = (float)tiempoTot;
                loadedTrack.trackTime = (float)tiempoTot;
            }
        }
        else
        {
            records.Add(loadedTrack.trackKey, (float)tiempoTot);
            loadedTrack.trackTime = (float)tiempoTot;
        }
    }

    /// <summary>
    /// genera una clave cara el circuito
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    public string genTrackKey()
    {
        keyGenData data = new keyGenData();
        data.cpList = new CheckPointClon[loadedTrack.cpList.Count];
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
            data.cpList[i] = cpClon;
        }
        data.laps = loadedTrack.laps;
        return MD5Hash(data);
    }

    //public ObsClon[] toObsClon(List<Obstacle> obCourse)
    //{
    //    ObsClon[] clones = new ObsClon[obCourse.Count];

    //    for (int i = 0; i < obCourse.Count; i++)
    //    {
    //        clones[i].body = obCourse[i].body.name;
    //        clones[i].pCoordsx = obCourse[i].pCoords.x;
    //        clones[i].pCoordsy = obCourse[i].pCoords.y;
    //        clones[i].pCoordsz = obCourse[i].pCoords.z;
    //        clones[i].rotx = obCourse[i].rot.x;
    //        clones[i].roty = obCourse[i].rot.y;
    //        clones[i].rotz = obCourse[i].rot.z;
    //        clones[i].rotw = obCourse[i].rot.w;
    //        clones[i].scalex = obCourse[i].transform.localScale.x;
    //        clones[i].scaley = obCourse[i].transform.localScale.y;
    //        clones[i].scalez = obCourse[i].transform.localScale.z;
    //        clones[i].solid = obCourse[i].Solid;
    //    }
    //    return clones;
    //}
}

/// <summary>
/// Convierte un objeto serializable en una matriz de bytes y al reves
/// http://stackoverflow.com/questions/4865104/convert-any-object-to-a-byte
/// </summary>
public class bArray
{
    // Convert an object to a byte array
    public static byte[] ObjectToByteArray(System.Object obj)
    {
        if (obj == null)
            return null;

        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        bf.Serialize(ms, obj);

        return ms.ToArray();
    }

    // Convert a byte array to an Object
    public static System.Object ByteArrayToObject(byte[] arrBytes)
    {
        MemoryStream memStream = new MemoryStream();
        BinaryFormatter binForm = new BinaryFormatter();
        memStream.Write(arrBytes, 0, arrBytes.Length);
        memStream.Seek(0, SeekOrigin.Begin);
        System.Object obj = (System.Object)binForm.Deserialize(memStream);

        return obj;
    }
}

public class Obstacle : MonoBehaviour
{
    public CelestialBody body;
    public Vector3 pCoords; //posición del marcador en lon lat alt
    private Vector3 coords;
    public Quaternion rot;  //rotación marcador
    public static int maxAlt = 50000;
    public static int maxScale = 100;
    public static int minScale = 1;
    public GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    public BoxCollider cubeCol;
    private bool solid;
    public static Color colorNormal = new Color(0.75f, 0.75f, 0.75f);
    public static Color colorEdit = new Color(1f, 0f, 1f);
    private Color obColor;

    public Vector3 Coords
    {
        get
        {
            return coords;
        }
    }

    public bool Solid
    {
        get
        {
            return solid;
        }

        set
        {
            solid = value;
            cubeCol.enabled = solid;
            ObColor = obColor;
        }
    }

    public Color ObColor
    {
        get
        {
            return obColor;
        }

        set
        {
            obColor = value;
            cube.GetComponent<Renderer>().material.color = new Color(obColor.r, obColor.g, obColor.b, (solid) ? 1 : 0.5f);
        }
    }

    void Awake()
    {
        //Donde está el buque en el momento de crear el punto de control
        cube.transform.parent = transform;
        cubeCol = cube.gameObject.GetComponent<BoxCollider>();
        ObColor = colorEdit;
        cubeCol.enabled = false;
        cube.GetComponent<Renderer>().material = new Material(Shader.Find("Transparent/Diffuse"));
        Solid = false;
        pCoords = new Vector3((float)FlightGlobals.ActiveVessel.latitude, (float)FlightGlobals.ActiveVessel.longitude, (float)FlightGlobals.ActiveVessel.altitude);
        body = FlightGlobals.ActiveVessel.mainBody;
        rot = FlightGlobals.ActiveVessel.transform.rotation;
    }

    /// <summary>
    /// Coloca el obstáculo
    /// </summary>
    void Update()
    {
        //que el punto de control no se meta por debajo del suelo
        if (pCoords.z - body.TerrainAltitude(pCoords.x, pCoords.y) <= 0)
        {
            pCoords.z = (float)body.TerrainAltitude(pCoords.x, pCoords.y);
        }

        if (pCoords.z > maxAlt)
        {
            pCoords.z = maxAlt;
        }

        //Como el origen del mundo se mueve con el buque, esto mantiene el punto de control en una posicion fija respecto al planeta.
        coords = body.GetWorldSurfacePosition(pCoords.x, pCoords.y, pCoords.z);

        this.transform.position = coords;
        this.transform.rotation = rot;
    }

    public void destroy()
    {
        Destroy(cube);
        Destroy(this);
    }

    /// <summary>
    /// Rota el obstáculo alrededor de los ejes y en la cantidad de grados especificados, cada vez que se llama.
    /// </summary>
    /// <param name="xAngle"></param>
    /// <param name="yAngle"></param>
    /// <param name="zAngle"></param>
    public void rotateOb(float xAngle, float yAngle, float zAngle)
    {
        rot *= Quaternion.Euler(xAngle, yAngle, zAngle);
    }

    /// <summary>
    /// Translada el obstaculo a lo largo del eje y en la distancia especificada, cada vez que se llama
    /// </summary>
    /// <param name="lat"></param>
    /// <param name="lon"></param>
    /// <param name="alt"></param>
    public void moveOb(float lat, float lon, float alt)
    {
        pCoords.x += lat;
        pCoords.y += lon;
        pCoords.z += alt;
    }

    public void scaleOb(float x, float y, float z)
    {
        float sx = cube.transform.localScale.x + x;
        float sy = cube.transform.localScale.y + y;
        float sz = cube.transform.localScale.z + z;

        if (sx < minScale)
        {
            sx = minScale;
        }
        else if (sx > maxScale)
        {
            sx = maxScale;
        }

        if (sy < minScale)
        {
            sy = minScale;
        }
        else if (sy > maxScale)
        {
            sy = maxScale;
        }
        if (sz < minScale)
        {
            sz = minScale;
        }
        else if (sz > maxScale)
        {
            sz = maxScale;
        }

        cube.transform.localScale = new Vector3((float)Math.Round(sx, 2), (float)Math.Round(sy, 2), (float)Math.Round(sz, 2));
    }

    //convertiría esto en una clase serializable
    [Serializable]
    public class ObsClon
    {
        public string body;
        public float pCoordsx;
        public float pCoordsy;
        public float pCoordsz;
        public float rotx;
        public float roty;
        public float rotz;
        public float rotw;
        public float scalex;
        public float scaley;
        public float scalez;
        public bool solid;
    }

    public ObsClon toClon()
    {
        ObsClon clon = new ObsClon();
        clon.body = body.name;
        clon.pCoordsx = pCoords.x;
        clon.pCoordsy = pCoords.y;
        clon.pCoordsz = pCoords.z;
        clon.rotx = rot.x;
        clon.roty = rot.y;
        clon.rotz = rot.z;
        clon.rotw = rot.w;
        clon.scalex = cube.transform.localScale.x;
        clon.scaley = cube.transform.localScale.y;
        clon.scalez = cube.transform.localScale.z;        
        clon.solid = Solid;
        return clon;
    }
}

