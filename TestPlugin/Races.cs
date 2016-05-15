﻿using UnityEngine;
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
    /// Esta clase debería servir para comunicar el mod con el juego, como que no casque el mod al cambiar de escena, poner la compativilidad con cKan, barras de botones...
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
                else{
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
    private static int maxRaceWaypoints { get; } = 30; //cantidad máxima de puntos de control de una carrera, por si sirve para algo.
    public CelestialBody body;
    public Vector3 pCoords; //posición del marcador en lon lat alt
    public Quaternion rot;  //rotación marcador;

    //Intentando hacer un colisionador
    public BoxCollider boxCollider;

    //lineas del marcador
    public static Color colorStart = Color.white;
    public static Color colorCheckP = Color.yellow;
    public static Color colorFinish = Color.red;
    public static Color colorPasado = Color.clear;
    public static Color colorEdit = new Color(255, 0, 255);
    private static Vector3 sizePec { get; } = new Vector3(4f, 32f, 18f); //Grosor de la linea, ancho del rectángulo, alto del rectángulo
    private static Vector3 sizeMed { get; } = new Vector3(8F, 64f, 36f);
    public Vector3 size;
    private Color wpColor;
    public LineRenderer marcador;

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
        size = sizeMed;
        marcador = new GameObject().AddComponent<LineRenderer>();
        marcador.useWorldSpace = false;
        marcador.material = new Material(Shader.Find("KSP/Emissive/Diffuse"));
        marcador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        marcador.SetWidth(size.x, size.x);
        marcador.SetVertexCount(5);
        marcador.enabled = true;

        //colisionador
        boxCollider = new GameObject().AddComponent<BoxCollider>();
        boxCollider.gameObject.AddComponent<colision>();
        boxCollider.isTrigger = true;
        boxCollider.transform.localScale = new Vector3(size.y, size.x, size.z);
        boxCollider.enabled = false;
    }

    /// <summary>
    /// Coloca el colisionador y dibuja las lineas del punto de control
    /// </summary>
    void Update()
    {
        //Como el origen del mundo se mueve con el buque, esto mantiene el punto de control en una posicion fija respecto al planeta.
        Vector3 coords = body.GetWorldSurfacePosition(pCoords.x, pCoords.y, pCoords.z);

        //Coloca los vertices del rectángulo
        Vector3 si = new Vector3(-(size.y / 2), 0, size.z / 2);
        Vector3 sd = new Vector3(size.y / 2, 0, size.z / 2);
        Vector3 id = new Vector3(size.y / 2, 0, -(size.z / 2));
        Vector3 ii = new Vector3(-(size.y / 2), 0, -(size.z / 2));

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
    public CheckPointClon[] cpList;
}

[Serializable]
public class CheckPointClon
{
    public string body;  //¿Conmo convertir un string a un celestialbody?
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
    public List<CheckPoint> cpList = new List<CheckPoint>();
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
    public double tiempoIni = 0;
    public double tiempoTot = 0;
    public double tiempoAct = 0;

    //GUI
    public Rect guiWindow = new Rect();
    public string guiRaceName, guiRaceAuth;
    public Vector2 scrollRaceList = new Vector2(0, 0);
    ////rotación y translación para los puntos de control
    public float rotx, roty, rotz, trax, tray, traz = 0;
    ////Styles
    public float rotLabelWidth = 35f;
    public float editSliderWidth = 100f;
    public float nameLabelWidth = 38f;
    public float nameTextWidth = 150f;

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

        estadoAct = estados.LoadScreen;
        raceList = new List<RaceClon>(); //Lista de carreras disponibles en el directorio
        loadedTrack = new LoadedTrack();  //Carrera que se va a usar para correr o editar.
        tiempoIni = 0;
        tiempoTot = 0;
        tiempoAct = 0;
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
                prepCp(false, false);
                enCarrera = false;
                break;
            case estados.EditScreen:
                estadoAct = estados.EditScreen;
                //Cuando se edita un circuito el punto de control editable de forma predeterminada es el último
                //- Pero Vloos, si solo hay un punto de control, ese va a ser el primero, así que no podrá ser el editable.
                //- Sí, es el primero, pero también es el último. La condición para que sea editable es que sea el último. No "Esclusivamente el ultimo".
                //- Es un 50% último, 50% primero, así que tampoco...
                //- En realidad es totalmente último y totalmente primero. Y Tambien es el del medio. Está en un estado cuántico de ordinalidad.
                prepCp(false, false);
                cambiaEditCp(loadedTrack.cpList.Count - 1);
                break;
            case estados.RaceScreen:
                prepCp(true, true);

                //Aunque prepCP hace un monton de cosas, no colorea de colorCheckP el siguiente al de inicio, con esto se consigue, a la vez que se evita que se coloree si resulta que es tambien la meta
                if (loadedTrack.cpList.Count > 0)
                {
                    if (loadedTrack.cpList[1].tipoCp != CheckPoint.Types.FINISH)
                    {
                        loadedTrack.cpList[1].cpColor = CheckPoint.colorCheckP;
                    }
                }
                estadoAct = estados.RaceScreen;
                pActivo = 0;
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
                        if (GUILayout.Button(race.name + " by " + race.author + "\n" + race.bodyName))
                        {
                            newRaceTrack();
                            LoadRaceTrack(race);
                            prepCp(false, false);
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

                if (GUILayout.Button("New Checkpoint"))
                {
                    CheckPoint rwpCol = new GameObject().AddComponent<CheckPoint>();
                    if (loadedTrack.cpList.Count == 1)
                    {
                        rwpCol.tipoCp = CheckPoint.Types.START;
                    }
                    else
                    {
                        if (loadedTrack.cpList.Count > 2)
                        {
                            loadedTrack.cpList[loadedTrack.cpList.Count - 2].tipoCp = CheckPoint.Types.CHECKPOINT;
                        }
                        rwpCol.tipoCp = CheckPoint.Types.FINISH;
                    }
                    loadedTrack.cpList.Add(rwpCol);
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

                if (GUILayout.Button("Start Race!"))
                {
                    cambiaEstado(estados.RaceScreen);
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
                    }
                    GUILayout.Label(editionCp.ToString());
                    if (GUILayout.Button(">"))  //next
                    {
                        cambiaEditCp(editionCp + 1);
                    }
                    if (GUILayout.Button(">|")) //last
                    {
                        cambiaEditCp(loadedTrack.cpList.Count - 1);
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
                    GUILayout.Label("Lat", GUILayout.Width(rotLabelWidth));
                    trax = GUILayout.HorizontalSlider(trax, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Lon", GUILayout.Width(rotLabelWidth));
                    tray = GUILayout.HorizontalSlider(tray, -0.0001f, 0.0001f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Alt", GUILayout.Width(rotLabelWidth));
                    traz = GUILayout.HorizontalSlider(traz, -0.3f, 0.3f, GUILayout.Width(editSliderWidth));
                    GUILayout.EndHorizontal();

                    GUILayout.EndVertical();

                    if (Input.GetMouseButton(0))
                    {
                        loadedTrack.cpList[editionCp].rotateRwp(rotx, roty, rotz);
                        loadedTrack.cpList[editionCp].moveRwp(trax, tray, traz);
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        rotx = 0;
                        roty = 0;
                        rotz = 0;
                        trax = 0;
                        tray = 0;
                        traz = 0;
                    }
                }

                GUILayout.EndHorizontal();
                break;
            case estados.RaceScreen:
                GUILayout.Label(loadedTrack.name + "\nby " + loadedTrack.author);
                if (enCarrera)
                {
                    GUILayout.Label(tiempoAct.ToString("0.00")); //Esto tiene que ser de tamaño grande.
                    if (GUILayout.Button("Abort Race!")) //Solo visible durante la carrera
                    {
                        enCarrera = !enCarrera;
                        cambiaEstado(estados.RaceScreen);
                    }
                }
                else
                {
                    GUILayout.Label("Cross first checkpoint (white) to start race!");
                    if (GUILayout.Button("Back")) //Solo visible mientras no empieza la carrera
                    {
                        cambiaEstado(estados.LoadScreen);
                    }
                }

                break;
            case estados.EndScreen:
                GUILayout.Label(loadedTrack.name + "\nby " + loadedTrack.author);
                GUILayout.Label("Total time:\n" + tiempoTot.ToString("0.00"));
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
        editionCp = 0;
    }

    /// <summary>
    /// Hace una copia de la carrera cargada LoadedTrack y la guarda en un archivo binario
    /// </summary>
    public void SaveRaceTrack()
    {
        //Tomar los datos de la carrera cargada (no guardable) y transferirlos a la carrera guardable
        RaceClon raceClon = new RaceClon();
        raceClon.cpList = new CheckPointClon[loadedTrack.cpList.Count];
        for (int i = 0; i < loadedTrack.cpList.Count; i++)
        {
            CheckPointClon rwpClon = new CheckPointClon();
            rwpClon.body = loadedTrack.cpList[i].body.name;
            rwpClon.pCoordsX = loadedTrack.cpList[i].pCoords.x;
            rwpClon.pCoordsY = loadedTrack.cpList[i].pCoords.y;
            rwpClon.pCoordsZ = loadedTrack.cpList[i].pCoords.z;
            rwpClon.rotX = loadedTrack.cpList[i].rot.x;
            rwpClon.rotY = loadedTrack.cpList[i].rot.y;
            rwpClon.rotZ = loadedTrack.cpList[i].rot.z;
            rwpClon.rotW = loadedTrack.cpList[i].rot.w;
            raceClon.cpList[i] = rwpClon;
        }

        raceClon.name = loadedTrack.name;
        raceClon.author = loadedTrack.author;
        raceClon.bodyName = loadedTrack.bodyName;

        //Guardar la carrera guardable
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

        //Extraer cpList de raceclon y meterlos en loadedTrack
        for (int i = 0; i < raceClon.cpList.Length; i++)
        {
            CheckPointClon cpClon = raceClon.cpList[i];
            CheckPoint checkPoint = new GameObject().AddComponent<CheckPoint>();
            loadedTrack.cpList.Add(checkPoint);

            //asumimos que el buque está en el mismo body que el circuito y más tarde se filtran los circuitos
            checkPoint.body = FlightGlobals.ActiveVessel.mainBody;
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
                    tiempoIni = Planetarium.GetUniversalTime();
                    loadedTrack.cpList[pActivo].boxCollider.enabled = false;
                    loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorPasado;
                    break;
                case CheckPoint.Types.CHECKPOINT:
                    ScreenMessages.PostScreenMessage(tiempoAct.ToString("0.00"), 15);
                    loadedTrack.cpList[pActivo].boxCollider.enabled = false;
                    loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorPasado;
                    break;
                case CheckPoint.Types.FINISH:
                    loadedTrack.cpList[pActivo].boxCollider.enabled = false;
                    tiempoTot = tiempoAct;
                    enCarrera = false;
                    ScreenMessages.PostScreenMessage(tiempoTot.ToString("0.00"), 15);
                    cambiaEstado(estados.EndScreen);
                    break;
                default:
                    break;
            }
            pActivo++;
            if (pActivo < loadedTrack.cpList.Count - 1)
            {
                loadedTrack.cpList[pActivo].cpColor = CheckPoint.colorStart;

                if (pActivo + 1 < loadedTrack.cpList.Count - 1)
                {
                    loadedTrack.cpList[pActivo + 1].cpColor = CheckPoint.colorCheckP;
                }
            }
        }
        else
        {
            Debug.LogWarning("punto de control incorrecto");
        }
    }

    /// <summary>
    /// Prepara los checkpoints para la carrera (o no), activando (o no) los colisionadores
    /// </summary>
    /// <param name="collision">Activa la detección de colisiones en los checkpoints</param>
    /// <param name="correr">Colorear los checkpoints de una forma especial, para empezar a competir</param>
    public void prepCp(bool collision, bool correr)
    {
        if (loadedTrack.cpList.Count > 0)
        {
            for (int i = 0; i < loadedTrack.cpList.Count; i++)
            {
                loadedTrack.cpList[i].boxCollider.enabled = collision;
                loadedTrack.cpList[i].cpType = CheckPoint.Types.CHECKPOINT;
                loadedTrack.cpList[i].cpColor = (correr) ? CheckPoint.colorPasado : CheckPoint.colorCheckP;
            }
            loadedTrack.cpList[loadedTrack.cpList.Count - 1].tipoCp = CheckPoint.Types.FINISH;
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
            {

            }
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
        }
        else
        {
            editionCp = 0;
        }
    }

    /// <summary>
    /// Debuelve un clon del circuito que puede ser serializado
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    public RaceClon trackToclone(LoadedTrack track)
    {
        RaceClon raceClon = new RaceClon();
        raceClon.cpList = new CheckPointClon[loadedTrack.cpList.Count];
        for (int i = 0; i < loadedTrack.cpList.Count; i++)
        {
            CheckPointClon rwpClon = new CheckPointClon();
            rwpClon.body = loadedTrack.cpList[i].body.name;
            rwpClon.pCoordsX = loadedTrack.cpList[i].pCoords.x;
            rwpClon.pCoordsY = loadedTrack.cpList[i].pCoords.y;
            rwpClon.pCoordsZ = loadedTrack.cpList[i].pCoords.z;
            rwpClon.rotX = loadedTrack.cpList[i].rot.x;
            rwpClon.rotY = loadedTrack.cpList[i].rot.y;
            rwpClon.rotZ = loadedTrack.cpList[i].rot.z;
            rwpClon.rotW = loadedTrack.cpList[i].rot.w;
            raceClon.cpList[i] = rwpClon;
        }

        raceClon.name = loadedTrack.name;
        raceClon.author = loadedTrack.author;
        raceClon.bodyName = loadedTrack.bodyName;
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
}