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
            else if (raceMod != this) Destroy(gameObject);

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
                if (raceMan.grot != null) raceMan.grot.Detach();
                if (raceMan.gofs != null) raceMan.gofs.Detach();
                raceMan.lastLoadedTrack = raceMan.loadedTrack.toClon();
                raceMan.cambiaEstado(RaceManager.estados.LoadScreen);
            }
            if (data.to != GameScenes.FLIGHT) GUIoff();
        }

        private void whatTheScene(GameScenes data)
        {
            if (data == GameScenes.FLIGHT)
            {
                if (raceMan.lastLoadedTrack.bodyName == FlightGlobals.ActiveVessel.mainBody.name)
                {
                    if (raceMan.grot != null) raceMan.grot.Detach();
                    if (raceMan.gofs != null) raceMan.gofs.Detach();
                    raceMan.LoadRaceTrack(raceMan.lastLoadedTrack); // Se supone que carga la ultima carrera que ha sido cargada cuando se vuelve a la escena de vuelo
                    raceMan.cambiaEstado(RaceManager.estados.LoadScreen);
                }
                else raceMan.newRaceTrack();
                GUIon();
            }
            else GUIoff();
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

public class RaceComponent : MonoBehaviour
{
    public CelestialBody body;
    private Vector3d coords; //coordenadas relativas al planeta
    public Quaternion rot;  //rotación relativa a la superficie
    public bool onMouse = false;
    public static int maxAlt = 50000;

    public Vector3 Coords
    {
        get
        {
            return coords;
        }

        set
        {
            coords = value;
        }
    }

    public void Start()
    {
        move();
        rotate();
    }

    /// <summary>
    /// Rota el obstáculo alrededor de los ejes y en la cantidad de grados especificados, cada vez que se llama.
    /// </summary>
    /// <param name="xAngle"></param>
    /// <param name="yAngle"></param>
    /// <param name="zAngle"></param>
    public void rotate(float xAngle, float yAngle, float zAngle)
    {
        rot *= Quaternion.Euler(xAngle, yAngle, zAngle);
        transform.rotation = rot;
    }

    public void rotate(Vector3 angles)
    {
        rotate(angles.x, angles.y, angles.z);
    }

    public void rotate()
    {
        transform.rotation = rotZero() * rot;
    }

    public void move()
    {
        transform.position = Coords + body.position;
    }

    [Serializable]
    public class Clon
    {
        public string body;
        public double posX, posY, posZ;
        public float rotX, rotY, rotZ, rotW;
    }

    /// <summary>
    /// Escupe una clase serializable que contiene los datos del obstáculo
    /// </summary>
    /// <returns></returns>
    public Clon toClon()
    {
        Clon clon = new Clon();
        clon.body = body.name;
        clon.posX = Coords.x;
        clon.posY = Coords.y;
        clon.posZ = Coords.z;
        clon.rotX = rot.x;
        clon.rotY = rot.y;
        clon.rotZ = rot.z;
        clon.rotW = rot.w;
        return clon;
    }

    /// <summary>
    /// toma los datos de un ObsClon
    /// </summary>
    /// <param name="clon"></param>
    public void fromClon(Clon clon)
    {
        Coords = new Vector3d(clon.posX, clon.posY, clon.posZ);
        rot = new QuaternionD(clon.rotX, clon.rotY, clon.rotZ, clon.rotW);
    }

    public Quaternion rotZero()
    {
        //http://answers.unity3d.com/questions/254130/how-do-i-rotate-an-object-towards-a-vector3-point.html
        Vector3 _direction = (body.position - transform.position).normalized;
        Quaternion _lookRotation = Quaternion.LookRotation(_direction);
        return _lookRotation;
    }

    public void resetRot()
    {
        rot = Quaternion.Inverse(rotZero()) * rotZero();
        rotate();
    }

    public Vector3d getBodyCoords()
    {
        return new Vector3d (body.GetLongitude(coords), body.GetLatitude(coords), body.GetAltitude(coords));
    }

    public void toFloor()
    {
        coords = body.GetWorldSurfacePosition(body.GetLatitude(coords), body.GetLongitude(coords), body.TerrainAltitude(body.GetLatitude(coords), body.GetLongitude(coords)));
        transform.position = coords;
    }
}

public class CheckPoint : RaceComponent
{
    public enum Types { START, CHECKPOINT, FINISH };
    public Types cpType;
    public GameObject cpBoxTrigger = GameObject.CreatePrimitive(PrimitiveType.Cube); //Esto detecta el buque pasar, para validar el punto de control
    private bool penalization;
    private int size;
    private Color wpColor;
    public Qub cuUp, cuDown, cuLe, cuRi;

    public static Color colorStart = Color.white;
    public static Color colorCheckP = Color.yellow;
    public static Color colorFinish = Color.red;
    public static Color colorPasado = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public static Color colorEdit = new Color(255, 0, 255);
    public static Material pasado = new Material(Shader.Find("Transparent/Diffuse"));
    public static Material listo = new Material(Shader.Find("KSP/Emissive/Diffuse"));
    public static Dictionary<int, Vector3> sizes = new Dictionary<int, Vector3>() {
            {0, new Vector3(1f, 16f, 9f)}, //Grosor de la linea, ancho del rectángulo, alto del rectángulo
            {1, new Vector3(2f, 32f, 18f)},
            {2, new Vector3(3f, 48f, 27f)},
            {3, new Vector3(4F, 64f, 36f)},
            {4, new Vector3(5F, 80f, 45f)}
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
            if (value == colorPasado)
            {
                cuUp.cubo.GetComponent<MeshRenderer>().material = pasado;
                cuDown.cubo.GetComponent<MeshRenderer>().material = pasado;
                cuRi.cubo.GetComponent<MeshRenderer>().material = pasado;
                cuLe.cubo.GetComponent<MeshRenderer>().material = pasado;
                cuUp.cubo.GetComponent<MeshRenderer>().material.color = wpColor;
                cuDown.cubo.GetComponent<MeshRenderer>().material.color = wpColor;
                cuRi.cubo.GetComponent<MeshRenderer>().material.color = wpColor;
                cuLe.cubo.GetComponent<MeshRenderer>().material.color = wpColor;
            }
            else
            {
                cuUp.cubo.GetComponent<MeshRenderer>().material = listo;
                cuDown.cubo.GetComponent<MeshRenderer>().material = listo;
                cuRi.cubo.GetComponent<MeshRenderer>().material = listo;
                cuLe.cubo.GetComponent<MeshRenderer>().material = listo;
                cuUp.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
                cuDown.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
                cuRi.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
                cuLe.cubo.GetComponent<MeshRenderer>().material.SetColor("_EmissiveColor", wpColor);
            }

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
                    cpColor = colorStart;
                    break;
                case Types.CHECKPOINT:
                    cpColor = colorCheckP;
                    break;
                case Types.FINISH:
                    cpColor = colorFinish;
                    break;
                default:
                    cpColor = colorPasado;
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

    public class Qub : MonoBehaviour
    {
        public GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);

        void Awake()
        {
            cubo.AddComponent<timePenalization>();
            cubo.gameObject.GetComponent<timePenalization>().enabled = false;
            cubo.transform.parent = transform;
            cubo.GetComponent<BoxCollider>().isTrigger = true;
            cubo.GetComponent<BoxCollider>().enabled = true;
            cubo.GetComponent<BoxCollider>().name = name + "cp-" + (Races.Races.raceMan.loadedTrack.cpList.Count).ToString();
        }

        void OnDestroy()
        {
            Destroy(cubo);
            Destroy(this);
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

    void Awake()
    {
        name = "[Races!]CP-";
        body = FlightGlobals.ActiveVessel.mainBody;
        transform.parent = body.transform;

        cpBoxTrigger.GetComponent<BoxCollider>().gameObject.AddComponent<colision>();
        cpBoxTrigger.transform.parent = transform;
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

    public void destroy()
    {
        Destroy(cuUp);
        Destroy(cuDown);
        Destroy(cuLe);
        Destroy(cuRi);
        Destroy(cpBoxTrigger);
        Destroy(this);
    }

    [Serializable]
    public class CheckPointClon
    {
        public int size;
        public Clon clon;
    }

    public CheckPointClon toCpClon()
    {
        CheckPointClon cpClon = new CheckPointClon();
        cpClon.size = Size;
        cpClon.clon = toClon();
        return cpClon;
    }

    public void fromCpClon(CheckPointClon cpClon)
    {
        Size = cpClon.size;
        fromClon(cpClon.clon);
    }
}

public class Obstacle : RaceComponent
{
    public static int maxScale = 100;
    public static int minScale = 1;
    public GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
    public BoxCollider cubeCol;
    private bool solid;
    public static Color colorNormal = new Color(0.75f, 0.75f, 0.75f);
    public static Color colorEdit = new Color(1f, 0f, 1f);
    private Color obColor;

    public bool Solid
    {
        get
        {
            return solid;
        }

        set
        {
            solid = value;
            cubeCol.isTrigger = !solid;
            cube.GetComponent<Renderer>().material = (!solid) ? new Material(Shader.Find("Transparent/Diffuse")) : null;
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

    public void scaleOb(float x, float y, float z)
    {
        float sx = cube.transform.localScale.x + x;
        float sy = cube.transform.localScale.y + y;
        float sz = cube.transform.localScale.z + z;

        if (sx < minScale) sx = minScale; else if (sx > maxScale) sx = maxScale;
        if (sy < minScale) sy = minScale; else if (sy > maxScale) sy = maxScale;
        if (sz < minScale) sz = minScale; else if (sz > maxScale) sz = maxScale;

        cube.transform.localScale = new Vector3((float)Math.Round(sx, 2), (float)Math.Round(sy, 2), (float)Math.Round(sz, 2));
    }

    void Awake()
    {
        name = "[Races!]OB-";
        body = FlightGlobals.ActiveVessel.mainBody;
        transform.parent = body.transform;

        cube.transform.parent = transform;
        cubeCol = cube.gameObject.GetComponent<BoxCollider>();
        ObColor = colorEdit;
        cubeCol.enabled = false;
        cubeCol.isTrigger = true;
        cubeCol.name = "[Races!]ob" + (Races.Races.raceMan.loadedTrack.obList.Count).ToString();
        cube.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Solid = false;
    }

    [Serializable]
    public class ObClon
    {
        public Clon clon;
        public float scalex, scaley, scalez;
        public bool solid;
    }

    /// <summary>
    /// Escupe una clase serializable que contiene los datos del obstáculo
    /// </summary>
    /// <returns></returns>
    public ObClon toObClon()
    {

        ObClon obClon = new ObClon();
        obClon.clon = toClon();
        obClon.scalex = cube.transform.localScale.x;
        obClon.scaley = cube.transform.localScale.y;
        obClon.scalez = cube.transform.localScale.z;
        obClon.solid = Solid;
        return obClon;
    }

    /// <summary>
    /// toma los datos de un ObsClon
    /// </summary>
    /// <param name="clon"></param>
    public void fromObClon(ObClon obClon)
    {
        fromClon(obClon.clon);
        cube.transform.localScale = new Vector3(obClon.scalex, obClon.scaley, obClon.scalez);
        Solid = obClon.solid;
    }
}

public class LoadedTrack
{
    public string bodyName, name, author, trackKey;
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

    [Serializable]
    public class RaceClon
    {
        public string bodyName, name, author;
        public int laps;
        public float lenght;
        public string key;
        public CheckPoint.CheckPointClon[] cpList;
        public Obstacle.ObClon[] obList;
    }

    public RaceClon toClon()
    {
        RaceClon clon = new RaceClon();

        clon.bodyName = bodyName;
        clon.name = name;
        clon.author = author;
        clon.laps = laps;
        clon.lenght = trackLength;
        clon.key = Races.Races.raceMan.genTrackKey();
        clon.cpList = new CheckPoint.CheckPointClon[cpList.Count];
        clon.obList = new Obstacle.ObClon[obList.Count];

        for (int i = 0; i < cpList.Count; i++)
        {
            clon.cpList[i] = cpList[i].toCpClon();
        }

        for (int i = 0; i < obList.Count; i++)
        {
            clon.obList[i] = obList[i].toObClon();
        }

        return clon;
    }

    public void fromClon(RaceClon clon)
    {
        bodyName = clon.bodyName;
        name = clon.name;
        author = clon.author;
        trackKey = clon.key;
        laps = clon.laps;
        foreach (CheckPoint.CheckPointClon cpClon in clon.cpList)
        {
            CheckPoint cp = new GameObject().AddComponent<CheckPoint>();
            cp.fromCpClon(cpClon);
            cp.rotate();
            cp.cpBoxTrigger.GetComponent<BoxCollider>().name = "[Races!]cp" + cpList.Count;
            cpList.Add(cp);
        }
        foreach (Obstacle.ObClon obClon in clon.obList)
        {
            Obstacle ob = new GameObject().AddComponent<Obstacle>();
            ob.fromObClon(obClon);
            ob.rotate();
            obList.Add(ob);
        }
    }
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
    public CheckPoint.CheckPointClon[] cpList;
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
    public List<LoadedTrack.RaceClon> raceList = new List<LoadedTrack.RaceClon>(); //Lista de carreras disponibles en el directorio
    public LoadedTrack loadedTrack = new LoadedTrack();  //Carrera que se va a usar para correr o editar.
    private int editionCp, editionOb = 0;
    public LoadedTrack.RaceClon lastLoadedTrack = new LoadedTrack.RaceClon(); //Esto valdrá (supongo) para cargar de nuevo un circuito al volver a la escena de vuelo
    public Dictionary<string, float> records = new Dictionary<string, float>() { { "0", 0 } };
    public EditorGizmos.GizmoRotate grot;
    public EditorGizmos.GizmoOffset gofs;

    //Carrera
    public bool enCarrera = false;
    public int pActivo, curLap;
    public double tiempoIni, tiempoTot, tiempoAct, penTime = 0d;

    //GUI
    public bool guiAct, appAct;
    public Rect guiBox = new Rect();
    public string guiRaceName, guiRaceAuth;
    public Vector2 scrollRaceList = Vector2.zero;
    public float trackLength;
    bool trackExist = false;
    public int size = 0;
    public float obScaleMinRatio = 0.1f;
    public float obScaleMaxRatio = 0.2f;
    ////Styles
    public float rotLabelWidth = 38f;
    public float editSliderWidth = 100f;
    public float nameLabelWidth = 38f;
    public float nameTextWidth = 150f;
    public float cardLabelWidth = 35f;
    public float obScaleInfoLabelWidth = 27;

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

            //Reactiva la penalización de tiempo al tocar el punto de control si el buque controlado se aleja demasiado del punto de control previamente tocado
            if (Vector3.Distance(FlightGlobals.ActiveVessel.CoM, loadedTrack.cpList[pActivo].Coords) > CheckPoint.sizes[loadedTrack.cpList[pActivo].Size].y && !loadedTrack.cpList[pActivo].Penalization)
            {
                loadedTrack.cpList[pActivo].Penalization = true;
            }
        }

        if (Input.GetMouseButtonUp(0) && (estadoAct == estados.EditScreen || estadoAct == estados.ObsScreen))
        {
            if (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.RightCommand))
            {
                if (estadoAct == estados.EditScreen) newCheckpoint(true);
                if (estadoAct == estados.ObsScreen) newObstacle(true);
            }
            else
            {
                try
                {
                    string obj = mouseRayHit().collider.name;
                    string type = obj.Substring(0, 10);
                    if (type == "[Races!]cp")
                    {
                        cambiaEstado(estados.EditScreen);
                        int num = int.Parse(obj.Substring(10));
                        cambiaEditCp(num);
                    }
                    if (type == "[Races!]ob")
                    {
                        cambiaEstado(estados.ObsScreen);
                        int num = int.Parse(obj.Substring(10));
                        cambiaEditOb(num);
                    }
                }
                catch (Exception)
                {

                }

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
                if (loadedTrack.obList.Count > 0)
                {
                    loadedTrack.obList[editionOb].ObColor = Obstacle.colorNormal;
                    loadedTrack.obList[editionOb].cubeCol.enabled = true;
                }
                if (grot != null) grot.Detach();
                if (gofs != null) gofs.Detach();
                editionOb = 0;
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
                    loadedTrack.obList[editionOb].cubeCol.enabled = true;
                }
                editionOb = 0;
                break;
            case estados.RaceScreen:
                prepCp(true);
                loadedTrack.trackKey = genTrackKey();
                loadedTrack.trackTime = (records.ContainsKey(loadedTrack.trackKey)) ? records[loadedTrack.trackKey] : 0;
                if (grot != null) grot.Detach();
                if (gofs != null) gofs.Detach();
                loadedTrack.cpList[0].cpBoxTrigger.GetComponent<BoxCollider>().enabled = true;
                estadoAct = estados.RaceScreen;
                pActivo = 0;
                curLap = 0;
                penTime = 0;
                if (loadedTrack.cpList[1].tipoCp != CheckPoint.Types.FINISH)
                {
                    loadedTrack.cpList[1].cpColor = CheckPoint.colorCheckP;
                }
                if (loadedTrack.obList.Count > 0)
                {
                    loadedTrack.obList[editionOb].ObColor = Obstacle.colorNormal;
                    loadedTrack.obList[editionOb].cubeCol.enabled = true;
                }
                break;
            case estados.EndScreen:
                estadoAct = estados.EndScreen;
                recordRecord();
                saveRecordFile();
                prepCp(false);
                break;
            case estados.ObsScreen:
                estadoAct = estados.ObsScreen;
                if (grot != null) grot.Detach();
                if (gofs != null) gofs.Detach();
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
        GUILayout.Label(loadedTrack.trackKey);
        if (GUILayout.Button("key"))
        {
            loadedTrack.trackKey = genTrackKey();
        }
        switch (estadoAct)
        {
            case estados.LoadScreen:
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical();
                GUILayout.Label(raceList.Count.ToString() + " Race Tracks Loaded");

                scrollRaceList = GUILayout.BeginScrollView(scrollRaceList, GUILayout.Height(250), GUILayout.Width(180));

                foreach (LoadedTrack.RaceClon race in raceList)
                {
                    if (race.bodyName == FlightGlobals.ActiveVessel.mainBody.name)
                    {
                        string names = ((race.name + " by " + race.author).Length > 20) ? race.name + "\nby " + race.author : race.name + " by " + race.author;
                        if (GUILayout.Button(names + "\n" + ((race.laps == 0) ? "Sprint, " : (race.laps + " Laps, ")) + race.lenght.ToString("0.00") + " meters", GUILayout.MaxWidth(170)))
                        {
                            newRaceTrack();
                            LoadRaceTrack(race);
                            prepCp(false);
                            trackLength = race.lenght;
                            loadedTrack.trackTime = (records.ContainsKey(loadedTrack.trackKey)) ? records[loadedTrack.trackKey] : 0;
                        }
                    }
                    else GUILayout.Label(race.name + " by " + race.author + "\n" + race.bodyName);
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
                    Vector3d pCoords = loadedTrack.cpList[0].getBodyCoords();
                    GUILayout.Label(loadedTrack.name + " by " + loadedTrack.author + "\n" + loadedTrack.cpList.Count + " Checkpoints\n" + ((loadedTrack.laps == 0) ? "Sprint" : (loadedTrack.laps + " Laps")) + trackLength.ToString("0.00") + " Meters\nBest time: " + tiempo(loadedTrack.trackTime));
                    GUILayout.Label("Starting point:\n" + "Latitude: " + pCoords.x + "\nLongitude: " + pCoords.y + "\nAltitude: " + pCoords.z + "\nDistance: " + Vector3.Distance(loadedTrack.cpList[0].Coords, FlightGlobals.ActiveVessel.CoM).ToString("0.00"));
                    if (loadedTrack.cpList.Count > 1 && GUILayout.Button("Start Race")) cambiaEstado(estados.RaceScreen);
                }

                if (loadedTrack.cpList.Count > 0 || loadedTrack.obList.Count > 0)
                {
                    if (GUILayout.Button("Edit Race Track")) cambiaEstado(estados.EditScreen);
                    if (GUILayout.Button("Clear Race Track")) newRaceTrack();
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                break;
            case estados.EditScreen:
                GUILayout.Label("Race Track Editor");

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUI.SetNextControlName("TrackNAme");
                GUILayout.Label("Name", GUILayout.Width(nameLabelWidth));
                loadedTrack.name = GUILayout.TextField(loadedTrack.name, GUILayout.Width(nameTextWidth));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Author", GUILayout.Width(nameLabelWidth));
                loadedTrack.author = GUILayout.TextField(loadedTrack.author, GUILayout.Width(nameTextWidth));
                GUILayout.EndHorizontal();
                GUILayout.Label("Laps");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Sprint")) loadedTrack.laps = 0;
                if (GUILayout.Button("-") && loadedTrack.laps > 1) loadedTrack.laps--;
                GUILayout.Label((loadedTrack.laps == 0) ? "Sprint" : loadedTrack.laps.ToString());
                if (GUILayout.Button("+")) loadedTrack.laps++;

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Length");
                GUILayout.Label(trackLength.ToString());
                GUILayout.EndHorizontal();

                if (GUILayout.Button("New Checkpoint here")) newCheckpoint(false);
                GUILayout.Label("RCtrl + LMB new checkpoint on cursor");

                if (loadedTrack.cpList.Count > 0 && GUILayout.Button("Remove Checkpoint"))
                {
                    loadedTrack.cpList[editionCp].destroy();
                    loadedTrack.cpList.RemoveAt(editionCp);
                    cambiaEditCp(editionCp);
                }

                if (GUILayout.Button("Edit Obstacles")) cambiaEstado(estados.ObsScreen);
                saveDialog();
                if (GUILayout.Button("New Race Track")) newRaceTrack();
                if (loadedTrack.cpList.Count > 1 && GUILayout.Button("Start Race!")) cambiaEstado(estados.RaceScreen);
                if (GUILayout.Button("Load Racetrack")) cambiaEstado(estados.LoadScreen);

                GUILayout.EndVertical();

                if (loadedTrack.cpList.Count > 0)
                {
                    GUILayout.BeginVertical();

                    GUILayout.Label("Select Checkpoint");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("|<")) cambiaEditCp(0); //First
                    if (GUILayout.Button("<")) cambiaEditCp(editionCp - 1); //previous
                    GUILayout.Label(editionCp.ToString());
                    if (GUILayout.Button(">")) cambiaEditCp(editionCp + 1);  //next
                    if (GUILayout.Button(">|")) cambiaEditCp(loadedTrack.cpList.Count - 1); //last

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Size");
                    if (GUILayout.Button("-") && size > 0) loadedTrack.cpList[editionCp].Size = --size;
                    GUILayout.Label(size.ToString());
                    if (GUILayout.Button("+") && size < CheckPoint.sizes.Count - 1) loadedTrack.cpList[editionCp].Size = ++size;
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Rotate"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        grot = EditorGizmos.GizmoRotate.Attach(loadedTrack.cpList[editionCp].transform, loadedTrack.cpList[editionCp].transform.position, cpRot, cpRot);
                    }

                    if (GUILayout.Button("Reset Rotation"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        loadedTrack.cpList[editionCp].resetRot();
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Translate");
                    bool abs = GUILayout.Button("Absolute");
                    bool rel = GUILayout.Button("Relative");

                    if (abs || rel)
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        gofs = EditorGizmos.GizmoOffset.Attach(loadedTrack.cpList[editionCp].transform, cpTran, cpTrEnd);
                        if (abs)
                        {
                            Vector3 _direction = (loadedTrack.cpList[editionCp].body.position - transform.position).normalized;
                            gofs.transform.rotation = Quaternion.LookRotation(_direction);
                        }
                        else gofs.transform.rotation = loadedTrack.cpList[editionCp].transform.rotation;
                    }
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("Send to floor"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        loadedTrack.cpList[editionCp].toFloor();
                    }
                    if (GUILayout.Button("Hide gizmo"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                break;
            case estados.RaceScreen:
                GUILayout.Label(loadedTrack.name + " by " + loadedTrack.author);
                if (enCarrera)
                {
                    if (loadedTrack.laps > 1) GUILayout.Label("Lap " + curLap + "/" + loadedTrack.laps);
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
                    GUILayout.Label("Best time: " + tiempo(loadedTrack.trackTime));
                    if (loadedTrack.laps > 0) GUILayout.Label(loadedTrack.laps + " Laps");
                    GUILayout.Label("Cross first checkpoint (white) to start race!");
                    if (GUILayout.Button("Edit Track")) cambiaEstado(estados.EditScreen);
                    if (GUILayout.Button("Load Track")) cambiaEstado(estados.LoadScreen);
                }

                break;
            case estados.EndScreen:
                GUILayout.Label(loadedTrack.name + " by " + loadedTrack.author);
                GUILayout.Label("Time: " + tiempo((float)tiempoAct));
                GUILayout.Label("Time penalty: " + tiempo((float)penTime));
                GUILayout.Label("Total time: " + tiempo((float)tiempoTot));
                GUILayout.Label("Best Time: " + tiempo(loadedTrack.trackTime));
                if (GUILayout.Button("Restart Race")) cambiaEstado(estados.RaceScreen);
                if (GUILayout.Button("Edit Race Track")) cambiaEstado(estados.EditScreen);
                if (GUILayout.Button("Load Track")) cambiaEstado(estados.LoadScreen);
                break;
            case estados.ObsScreen:
                GUILayout.Label("Obstacle Editor");

                GUILayout.BeginHorizontal();

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUI.SetNextControlName("TrackNAme");
                GUILayout.Label("Name", GUILayout.Width(nameLabelWidth));
                loadedTrack.name = GUILayout.TextField(loadedTrack.name, GUILayout.Width(nameTextWidth));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("Author", GUILayout.Width(nameLabelWidth));
                loadedTrack.author = GUILayout.TextField(loadedTrack.author, GUILayout.Width(nameTextWidth));
                GUILayout.EndHorizontal();
                GUILayout.Label("Laps");
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("1")) loadedTrack.laps = 1;
                if (GUILayout.Button("-") && loadedTrack.laps > 1) loadedTrack.laps--;
                GUILayout.Label(loadedTrack.laps.ToString());
                if (GUILayout.Button("+")) loadedTrack.laps++;

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Length");
                GUILayout.Label(trackLength.ToString());
                GUILayout.EndHorizontal();

                if (GUILayout.Button("New Obstacle here")) newObstacle(false);
                GUILayout.Label("RCtrl + LMB new obstacle on cursor");

                if (loadedTrack.obList.Count > 0 && GUILayout.Button("Remove Obstacle"))
                {
                    Destroy(loadedTrack.obList[editionOb]);
                    loadedTrack.obList.RemoveAt(editionOb);
                    cambiaEditOb(editionOb);
                }

                if (GUILayout.Button("Edit Checkpoints")) cambiaEstado(estados.EditScreen);

                saveDialog();

                if (loadedTrack.obList.Count > 0 && GUILayout.Button("Clear Obstacles"))
                {
                    foreach (Obstacle obs in loadedTrack.obList)
                    {
                        Destroy(obs);
                    }
                    loadedTrack.obList.Clear();
                }

                if (loadedTrack.cpList.Count > 1 && GUILayout.Button("Start Race!")) cambiaEstado(estados.RaceScreen);
                if (GUILayout.Button("Load Racetrack")) cambiaEstado(estados.LoadScreen);

                GUILayout.EndVertical();

                if (loadedTrack.obList.Count > 0)
                {
                    GUILayout.BeginVertical();

                    GUILayout.Label("Select Obstacle");
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("|<")) cambiaEditOb(0); //First
                    if (GUILayout.Button("<")) cambiaEditOb(editionOb - 1); //Previous
                    GUILayout.Label(editionOb.ToString());
                    if (GUILayout.Button(">")) cambiaEditOb(editionOb + 1); //Next
                    if (GUILayout.Button(">|")) cambiaEditOb(loadedTrack.obList.Count - 1); //Last
                    GUILayout.EndHorizontal();

                    loadedTrack.obList[editionOb].Solid = GUILayout.Toggle(loadedTrack.obList[editionOb].Solid, "Solid thing");

                    if (GUILayout.Button("Rotate"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        grot = EditorGizmos.GizmoRotate.Attach(loadedTrack.obList[editionOb].transform, loadedTrack.obList[editionOb].transform.position, obRot, obRotado);
                    }

                    if (GUILayout.Button("Reset Rotation"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        loadedTrack.obList[editionOb].resetRot();
                    }

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Translate");
                    bool abs = GUILayout.Button("Absolute");
                    bool rel = GUILayout.Button("Relative");
                    GUILayout.EndHorizontal();

                    if (abs || rel)
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        gofs = EditorGizmos.GizmoOffset.Attach(loadedTrack.obList[editionOb].transform, obTran, obTrEnd);
                        if (abs)
                        {
                            Vector3 _direction = (loadedTrack.obList[editionOb].body.position - transform.position).normalized;
                            gofs.transform.rotation = Quaternion.LookRotation(_direction);
                        }
                        else gofs.transform.rotation = loadedTrack.obList[editionOb].transform.rotation;
                    }

                    if (GUILayout.Button("Send to floor"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                        loadedTrack.obList[editionOb].toFloor();
                    }

                    if (GUILayout.Button("Hide gizmo"))
                    {
                        if (grot != null) grot.Detach();
                        if (gofs != null) gofs.Detach();
                    }

                    GUILayout.Label("Scale");

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("X", GUILayout.Width(15));
                    if (GUILayout.Button("|-")) loadedTrack.obList[editionOb].scaleOb(-Obstacle.maxScale, 0, 0);
                    if (GUILayout.RepeatButton("--")) loadedTrack.obList[editionOb].scaleOb(-obScaleMaxRatio, 0, 0);
                    if (GUILayout.Button("-")) loadedTrack.obList[editionOb].scaleOb(-obScaleMinRatio, 0, 0);
                    GUILayout.Label(loadedTrack.obList[editionOb].cube.transform.localScale.x.ToString(), GUILayout.Width(obScaleInfoLabelWidth));
                    if (GUILayout.Button("+")) loadedTrack.obList[editionOb].scaleOb(obScaleMinRatio, 0, 0);
                    if (GUILayout.RepeatButton("++")) loadedTrack.obList[editionOb].scaleOb(obScaleMaxRatio, 0, 0);
                    if (GUILayout.Button("+|")) loadedTrack.obList[editionOb].scaleOb(Obstacle.maxScale, 0, 0);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Y", GUILayout.Width(15));
                    if (GUILayout.Button("|-")) loadedTrack.obList[editionOb].scaleOb(0, -Obstacle.maxScale, 0);
                    if (GUILayout.RepeatButton("--")) loadedTrack.obList[editionOb].scaleOb(0, -obScaleMaxRatio, 0);
                    if (GUILayout.Button("-")) loadedTrack.obList[editionOb].scaleOb(0, -obScaleMinRatio, 0);
                    GUILayout.Label(loadedTrack.obList[editionOb].cube.transform.localScale.y.ToString(), GUILayout.Width(obScaleInfoLabelWidth));
                    if (GUILayout.Button("+")) loadedTrack.obList[editionOb].scaleOb(0, obScaleMinRatio, 0);
                    if (GUILayout.RepeatButton("++")) loadedTrack.obList[editionOb].scaleOb(0, obScaleMaxRatio, 0);
                    if (GUILayout.Button("+|")) loadedTrack.obList[editionOb].scaleOb(0, Obstacle.maxScale, 0);
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Z", GUILayout.Width(15));
                    if (GUILayout.Button("|-")) loadedTrack.obList[editionOb].scaleOb(0, 0, -Obstacle.maxScale);
                    if (GUILayout.RepeatButton("--")) loadedTrack.obList[editionOb].scaleOb(0, 0, -obScaleMaxRatio);
                    if (GUILayout.Button("-")) loadedTrack.obList[editionOb].scaleOb(0, 0, -obScaleMinRatio);
                    GUILayout.Label(loadedTrack.obList[editionOb].cube.transform.localScale.z.ToString(), GUILayout.Width(obScaleInfoLabelWidth));
                    if (GUILayout.Button("+")) loadedTrack.obList[editionOb].scaleOb(0, 0, obScaleMinRatio);
                    if (GUILayout.RepeatButton("++")) loadedTrack.obList[editionOb].scaleOb(0, 0, obScaleMaxRatio);
                    if (GUILayout.Button("+|")) loadedTrack.obList[editionOb].scaleOb(0, 0, Obstacle.maxScale);
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();
                }
                GUILayout.EndHorizontal();
                break;
            default:
                break;
        }
        GUI.DragWindow();
    }

    /// <summary>
    /// llamado cuando se suelta el chirimbolo de translación de obstáculos
    /// </summary>
    /// <param name="arg1"></param>
    private void obTrEnd(Vector3 arg1)
    {
        Obstacle ob = loadedTrack.obList[editionOb];
        loadedTrack.obList[editionOb].Coords = arg1 - loadedTrack.obList[editionOb].body.position;
        loadedTrack.obList[editionOb].move();
    }

    /// <summary>
    /// llamado mientras se agarra el chirimbolo de translación de obstáculos
    /// </summary>
    /// <param name="arg1"></param>
    private void obTran(Vector3 arg1)
    {
        loadedTrack.obList[editionOb].transform.position = gofs.transform.position;
    }

    /// <summary>
    /// llamado cuando se suelta el chirimbolo de rotación de obstáculos
    /// </summary>
    /// <param name="arg1"></param>
    private void obRotado(Quaternion arg1)
    {
        Obstacle ob = loadedTrack.obList[editionOb];
        ob.rot = Quaternion.Inverse(ob.rotZero()) * (arg1 * grot.HostRot0);
        ob.rotate();
    }

    /// <summary>
    /// llamado cuando mientras se agarra el chirimbolo de rotación de obstáculos
    /// </summary>
    /// <param name="arg1"></param>
    private void obRot(Quaternion arg1)
    {
        grot.useAngleSnap = !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand));
        Obstacle ob = loadedTrack.obList[editionOb];
        ob.rot = Quaternion.Inverse(ob.rotZero()) * (arg1 * grot.HostRot0);
        ob.rotate();
    }

    /// <summary>
    /// llamado cuando se suelta el chirimbolo de translación de puntos de control
    /// </summary>
    /// <param name="arg1"></param>
    private void cpTrEnd(Vector3 arg1)
    {
        CheckPoint cp = loadedTrack.cpList[editionCp];
        cp.Coords = gofs.transform.position - cp.body.position;
        cp.move();
    }

    /// <summary>
    /// llamado mientras se agarra el chirimbolo de translación de puntos de control
    /// </summary>
    /// <param name="arg1"></param>
    private void cpTran(Vector3 arg1)
    {
        gofs.useGrid = !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand));
        CheckPoint cp = loadedTrack.cpList[editionCp];
        cp.Coords = gofs.transform.position - cp.body.position;
        cp.move();
    }

    /// <summary>
    /// llamado mientras se agarra el chirimbolo de rotación de puntos de control
    /// </summary>
    /// <param name="arg1"></param>
    private void cpRot(Quaternion arg1)
    {
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)) grot.useAngleSnap = false; else grot.useAngleSnap = true;
        CheckPoint cp = loadedTrack.cpList[editionCp];
        cp.rot = Quaternion.Inverse(cp.rotZero()) * (arg1 * grot.HostRot0);
        cp.rotate();
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
                foreach (CheckPoint cp in loadedTrack.cpList)
                {
                    cp.destroy();
                }
                loadedTrack.cpList.Clear();
            }

            if (loadedTrack.obList.Count > 0)
            {
                foreach (Obstacle obs in loadedTrack.obList)
                {
                    Destroy(obs);
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
        trackExist = false;
    }

    /// <summary>
    /// pone un nuevo punto de control en la posición del buque on en la posición del cursor
    /// </summary>
    /// <param name="enCursor"></param>
    public void newCheckpoint(bool enCursor)
    {
        if (grot != null) grot.Detach();
        if (gofs != null) gofs.Detach();
        CheckPoint cp = new GameObject().AddComponent<CheckPoint>();
        cp.onMouse = enCursor;
        cp.name += loadedTrack.cpList.Count;
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
        cp.cpBoxTrigger.GetComponent<BoxCollider>().name = cp.name;
        if (enCursor)
        {
            try
            {
                Vector3d mousePos = mouseRayHit().point;
                cp.Coords = mousePos - cp.body.position;
            }
            catch (Exception) { }
            cp.resetRot();
        }
        else
        {
            cp.Coords = FlightGlobals.ActiveVessel.GetTransform().position - cp.body.position;
            cp.rot = Quaternion.Inverse(cp.rotZero()) * FlightGlobals.ActiveVessel.GetTransform().rotation;
        }
        Debug.Log(cp.name);
        loadedTrack.cpList.Add(cp);
        cambiaEditCp(loadedTrack.cpList.Count - 1);
    }

    /// <summary>
    /// pone un nuevo obstáculo en la posición del buche o en la posición del cursor
    /// </summary>
    /// <param name="enCursor"></param>
    public void newObstacle(bool enCursor)
    {
        Obstacle obs = new GameObject().AddComponent<Obstacle>();
        obs.cube.transform.localScale = new Vector3(Obstacle.maxScale / 2, Obstacle.maxScale / 2, Obstacle.maxScale / 2);
        if (enCursor)
        {
            Vector3d mousePos = mouseRayHit().point;
            obs.Coords = mousePos;
        }
        obs.rot = obs.rotZero();
        obs.rotate();
        loadedTrack.obList.Add(obs);
        cambiaEditOb(loadedTrack.obList.Count - 1);
    }

    /// <summary>
    /// Code by ChrisW -> http://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use
    /// </summary>
    protected virtual bool IsFileLocked(FileInfo file)
    {
        FileStream stream = null;

        try
        {
            stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return true;
        }
        finally
        {
            if (stream != null)
                stream.Close();
        }

        //file is not locked
        return false;
    }

    /// <summary>
    /// Un trozo de interfaz que muestra la confirmación de guardado de circuito cuando el nombre del circuito ya existe
    /// </summary>
    public void saveDialog()
    {
        GUI.SetNextControlName("SaveButton");
        if (loadedTrack.cpList.Count > 0 && GUILayout.Button("Save Race Track"))
        {
            GUI.FocusControl("SaveButton");
            if (raceList.FindAll(x => x.name == loadedTrack.name).Count == 0)
            {
                SaveRaceTrack();
                raceList.Clear();
                GetRacetrackList();
            }
            else trackExist = true;
        }

        if (trackExist)
        {
            if (GUI.changed && GUI.GetNameOfFocusedControl() == "TrackNAme") trackExist = false;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Already exist");
            if (GUILayout.Button("Overwrite"))
            {
                FileInfo file = new FileInfo(Races.Races.RaceTrackFolder + RemoveSpecialCharacters(loadedTrack.name) + Races.Races.RaceTrackFileExtension);
                if (!IsFileLocked(file))
                {
                    SaveRaceTrack();
                    raceList.Clear();
                    GetRacetrackList();
                    trackExist = false;
                }
                else
                {
                    Debug.LogWarning("[Races!] Busy");
                }
            }
            if (GUILayout.Button("Cancel")) trackExist = false;
            GUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Hace una copia de la carrera cargada LoadedTrack y la guarda en un archivo binario
    /// </summary>
    public void SaveRaceTrack()
    {
        //Tomar los datos de la carrera cargada (no guardable) y transferirlos a la carrera guardable
        LoadedTrack.RaceClon raceClon = loadedTrack.toClon();
        if (!Directory.Exists(Races.Races.RaceTrackFolder))
        {
            Directory.CreateDirectory(Races.Races.RaceTrackFolder);
        }
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            string filename = RemoveSpecialCharacters(raceClon.name);
            FileStream file = File.Create(Races.Races.RaceTrackFolder + filename + Races.Races.RaceTrackFileExtension);
            bf.Serialize(file, raceClon);
            file.Close();
            Debug.Log("[Races!] Saved: " + raceClon.name + " - " + raceClon.author);
        }
        catch (Exception)
        {
            Debug.LogError("[Races!] Something went wrong saving the track file");
        }
    }

    /// <summary>
    /// Coge una carrera de la lista de carreras y la mete en LoadedTrack 
    /// </summary>
    public void LoadRaceTrack(LoadedTrack.RaceClon raceClon)
    {
        loadedTrack = new LoadedTrack();
        loadedTrack.fromClon(raceClon);
        lastLoadedTrack = raceClon;         //Esto supongo que valdrá para volver a cargar el circuito cuando se revierte el vuelo
        Debug.Log("[Races!] Race track loaded: " + loadedTrack.name + " by " + loadedTrack.author);
    }

    /// <summary>
    /// Escanea el directorio de circuitos en busca de circuitos y llena una lista de circuitos con los circuitos encontrados
    /// Como cambia la forma de guardar los circuitos, esto convierte los viejos. La conversión la quitaré en la siguiente versión...
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
        Debug.Log("[Races!] " + fileInfo.Length + " Race Tracks found");

        foreach (var file in fileInfo)
        {
            try
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fileStream = File.Open(file.FullName, FileMode.Open);
                var stream = bf.Deserialize(fileStream);
                LoadedTrack.RaceClon race = stream as LoadedTrack.RaceClon;

                if (race != null)
                {
                    raceList.Add(race);
                }
            }
            catch (Exception)
            {
                Debug.LogError("[Races!] Something went wrong");
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
                    if (loadedTrack.laps > 0 && curLap != 0)
                    {
                        ScreenMessages.PostScreenMessage("Lap " + curLap + " Checkpoint " + pActivo + "\n" + tiempo((float)tiempoAct), 15);
                    }
                    if (loadedTrack.laps > 0)
                    {
                        curLap++;
                    }
                    if (loadedTrack.laps > 0 && curLap == loadedTrack.laps)
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

            if (loadedTrack.laps == 0)
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
        if (grot != null) grot.Detach();
        if (gofs != null) gofs.Detach();
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
    /// Cambia el color de los obstáculos para indicar el que se está editando
    /// </summary>
    /// <param name="num"></param>
    public void cambiaEditOb(int num)
    {
        if (grot != null) grot.Detach();
        if (gofs != null) gofs.Detach();
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
            if (editionOb <= loadedTrack.obList.Count - 1)
            {
                loadedTrack.obList[editionOb].ObColor = Obstacle.colorNormal;
            }
            loadedTrack.obList[editionOb].cubeCol.enabled = true;
            editionOb = num;
            loadedTrack.obList[editionOb].ObColor = Obstacle.colorEdit;
            loadedTrack.obList[editionOb].cubeCol.enabled = false;
        }
        else
        {
            editionOb = 0;
        }
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
        try
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Create(Races.Races.RaceTrackFolder + "records.dat");
            bf.Serialize(file, saveThis);
            file.Close();
        }
        catch (Exception)
        {
            Debug.LogError("[Races!] Something went wrong saving records.dat");
        }
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
        data.cpList = new CheckPoint.CheckPointClon[loadedTrack.cpList.Count];
        for (int i = 0; i < loadedTrack.cpList.Count; i++)
        {
            data.cpList[i] = loadedTrack.cpList[i].toCpClon();
        }
        data.laps = loadedTrack.laps;
        return loadedTrack.trackKey = MD5Hash(data);
    }

    /// <summary>
    /// Desde el cursor del ratón proyecta un rayo que toca cosas.
    /// </summary>
    /// <returns>información sobre las cosas que toca</returns>
    public RaycastHit mouseRayHit()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        // Casts the ray and get the first game object hit
        Physics.Raycast(ray, out hit);
        return hit;
    }
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