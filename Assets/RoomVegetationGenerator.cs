using System;
using System.Collections;
using System.Collections.Generic;
using Phantom.Environment.Scripts;
using UnityEngine;

public class RoomVegetationGenerator : MonoBehaviour
{
    public Terrain terrain;

    public GameObject room;

    public OVRPassthroughLayer passthroughLayer;
    private OVRPassthroughColorLut _colorLut;
    public Texture2D colorLutPng;

    public Light light;

    public GameObject sceneRoot;

    public GameObject spiderPrefab;

    public GameObject activeSpider;

    public GameObject playerCamera;

    private NavMeshGenerator _navMeshGenerator;
    public GameObject debugTarget;
    public SceneDataLoader loader;
    public float spiderScale;
    private OVRSceneRoom _roomData;
    public Material swampMaterial;
    public Material zigZag;
    public List<GameObject> roomObjects;
    bool roomLoaded;
    public GameObject redCurtainPrefab;

    private OVRSceneManager _ovrSceneManager;
    // Start is called before the first frame update
    void Start()
    {
        _colorLut = new OVRPassthroughColorLut(colorLutPng);
        playerCamera = GameObject.FindWithTag("MainCamera");
        roomObjects = new List<GameObject>();
        roomLoaded = false;
        passthroughLayer = GameObject.FindWithTag("Player").GetComponentInChildren<OVRPassthroughLayer>();
        StartCoroutine(StartSequence());
        StartCoroutine(GenerateEnvironment());
        StartCoroutine(StartSpider());
    }

    IEnumerator GenerateEnvironment()
    {
        List<GameObject> walls =
            new List<GameObject>();
        while (_ovrSceneManager == null)
        {
            _ovrSceneManager = loader.GetSceneManager();
            yield return null;
        }

        while (_roomData == null)
        {
            _roomData = FindObjectOfType<OVRSceneRoom>();
            yield return null;
        }
        _ovrSceneManager.SceneModelLoadedSuccessfully += SetRoomLoaded;
        yield return new WaitUntil(() => roomLoaded);
        foreach (GameObject sceneObj in GameObject.FindGameObjectsWithTag("GlobalMesh"))
        {
            if (sceneObj.GetComponent<OVRSemanticClassification>().Labels[0].Equals("GLOBAL_MESH"))
            {
                MeshRenderer volumeRenderer = sceneObj.GetComponent<MeshRenderer>();
                volumeRenderer.material = swampMaterial;
                volumeRenderer.enabled = true;
            }
            else if (sceneObj.GetComponent<OVRSemanticClassification>().Labels[0].Equals("FLOOR"))
            {
                Vector3 position = sceneObj.transform.position;
                sceneObj.transform.position = new Vector3(position.x, position.y + 0.12f, position.z);
                MeshRenderer floorRenderer = sceneObj.GetComponent<MeshRenderer>();
                floorRenderer.material = swampMaterial;
                floorRenderer.enabled = true;
            }
            else if (sceneObj.GetComponent<OVRSemanticClassification>().Labels[0].Equals("CEILING"))
            {
                Vector3 position = sceneObj.transform.position;
                sceneObj.transform.position = new Vector3(position.x, position.y - 0.2f, position.z);
                //Vector3 rot = sceneObj.transform.rotation.eulerAngles;
                //rot = new Vector3(rot.x + 180f, rot.y, rot.z);
                MeshRenderer ceilingRenderer = sceneObj.GetComponent<MeshRenderer>();
                ceilingRenderer.material = zigZag;
                ceilingRenderer.enabled = true;
            }
            else
            {
                walls.Add(sceneObj);
            }
            roomObjects.Add(sceneObj);
        }
        int i = roomObjects.Count;
        if (walls.Count > 0) SetWalls(walls);
    }
    private void SetWalls(List<GameObject> walls)
    {
        foreach (GameObject wall in walls)
        {
            Instantiate(redCurtainPrefab);
            redCurtainPrefab.transform.position = wall.transform.position;
        }
    }
    private void SetRoomLoaded()
    {
        roomLoaded = true;
    }

    IEnumerator StartSpider()
    {
        while (_navMeshGenerator == null)
        {
            _navMeshGenerator = FindObjectOfType<NavMeshGenerator>();
            yield return null;
        }
        Vector3 position = _navMeshGenerator.RandomPointOnFloor(playerCamera.transform.position, 3f);
        position = new Vector3(position.x, position.y + 5f, position.z);
        activeSpider = Instantiate(spiderPrefab, position, Quaternion.identity);
        activeSpider.transform.localScale = new Vector3(spiderScale, spiderScale, spiderScale);
        //set up rig
        foreach (IKChain ikChain in activeSpider.GetComponentsInChildren<IKChain>())
        {
            ikChain.debugTarget = debugTarget.transform;
        }
    }

    IEnumerator StartSequence()
    {
        float oldValue = 0f;
        float newValue = 1f;
        float someValue = 0f;
        int duration = 5;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            someValue = Mathf.Lerp(oldValue, newValue, t / duration);
            passthroughLayer.SetColorLut(_colorLut, someValue);
            yield return null;
        }
        Debug.Log("Invert occured");
        oldValue = 1f;
        newValue = 0f;
        someValue = 0f;
        duration = 5;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            someValue = Mathf.Lerp(oldValue, newValue, t / duration);
            passthroughLayer.textureOpacity = someValue;
            //light.intensity = someValue;
            yield return null;
        }
        Debug.Log("Transfer occured");
    }

    public void IncreaseSpiderSize()
    {
        if (activeSpider == null)
        {
            Debug.LogError("Spider cannot increase size. Not found");
            return;
        }

        Vector3 scale = activeSpider.transform.localScale;
        activeSpider.transform.localScale = new Vector3(scale.x + 1, scale.y + 1, scale.z + 1);
    }
}
