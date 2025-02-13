using System;
using System.Collections;
using System.Collections.Generic;
using Phantom.Environment.Scripts;
using TMPro;
using Unity.VisualScripting;
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
    public TextMeshPro tmp;
    public float originalHeight;
    public bool canTouchCurtain;
    CurtainScript curtain;
    private ParticleSystem _particleSystem;
    public AudioClip deathLoop;
    public AudioSource audioSource;

    private OVRSceneManager _ovrSceneManager;
    // Start is called before the first frame update
    void Start()
    {
        _colorLut = new OVRPassthroughColorLut(colorLutPng);
        playerCamera = GameObject.FindWithTag("MainCamera");
        audioSource = playerCamera.AddComponent<AudioSource>();
        _particleSystem = playerCamera.GetComponent<ParticleSystem>();
        originalHeight = playerCamera.transform.position.y;
        roomObjects = new List<GameObject>();
        roomLoaded = false;
        passthroughLayer = GameObject.FindWithTag("Player").GetComponentInChildren<OVRPassthroughLayer>();
        passthroughLayer.overlayType = OVROverlay.OverlayType.Overlay;
        StartCoroutine(GenerateEnvironment());
        StartCoroutine(StartSpider());
        StartCoroutine(WaitForService());
    }

    IEnumerator GenerateEnvironment()
    {
        GameObject ceiling = null;
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
                Vector3 positionFloor = sceneObj.transform.position;
                sceneObj.transform.position = new Vector3(positionFloor.x, positionFloor.y + 0.15f, positionFloor.z);
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
                Vector3 ceilingPosition = sceneObj.transform.position;
                Vector3 localPosition = sceneObj.transform.localPosition;
                sceneObj.transform.position = new Vector3(ceilingPosition.x, ceilingPosition.y - 0.12f, ceilingPosition.z);
                sceneObj.transform.localPosition = new Vector3(localPosition.x, localPosition.y - .15f, localPosition.z);
                MeshRenderer ceilingRenderer = sceneObj.GetComponent<MeshRenderer>();
                ceiling = sceneObj;
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
        bool doRotation = false;
        bool first = true;
        foreach (GameObject wall in walls)
        {
            GameObject redCurtainObj = Instantiate(redCurtainPrefab, new Vector3(0f, 0f, 0.5f), Quaternion.identity);
            redCurtainObj.transform.localScale = new Vector3(1f, 2f, 2f);
            redCurtainObj.transform.SetParent(wall.transform, true);
            break;
            if (doRotation)
            {
                Vector3 euler = redCurtainObj.transform.localRotation.eulerAngles;
                redCurtainObj.transform.localRotation = Quaternion.Euler(euler.x, euler.y, 90f);
                if (first)
                {
                    redCurtainObj.transform.localRotation = Quaternion.Euler(euler.x, 90f, 90f);

                }
                doRotation = false;
            }
            else doRotation = true;
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
        Vector3 position = _navMeshGenerator.RandomPointOnFloor(playerCamera.transform.position, 1f);
        position = new Vector3(position.x, position.y + 3f, position.z);
        activeSpider = Instantiate(spiderPrefab, position, Quaternion.identity);
        activeSpider.transform.localScale = new Vector3(spiderScale, spiderScale, spiderScale);
        //set up rig
        foreach (IKChain ikChain in activeSpider.GetComponentsInChildren<IKChain>())
        {
            ikChain.debugTarget = debugTarget.transform;
        }
    }

    IEnumerator InvertPassthrough()
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
    }
    IEnumerator RemovePassthrough()
    {
        float oldValue = 1f;
        float newValue = 0f;
        float someValue = 0f;
        int duration = 8;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            someValue = Mathf.Lerp(oldValue, newValue, t / duration);
            passthroughLayer.textureOpacity = someValue;
            //light.intensity = someValue;
            yield return null;
        }
        Debug.Log("Transfer occured");
    }
    IEnumerator WaitForService()
    {
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        audioSource.PlayOneShot(deathLoop);
        audioSource.loop = true;
        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Text") != null);
        tmp = GameObject.FindGameObjectWithTag("Text").GetComponent<TextMeshPro>();
        tmp.text = "Follow the spider.";
        yield return new WaitUntil(() => activeSpider != null);
        yield return new WaitForSeconds(5f);
        //StartCoroutine(InvertPassthrough());
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        //yield return new WaitForSeconds(5f);
        StartCoroutine(RemovePassthrough());
        yield return new WaitUntil(() => Vector3.Distance(playerCamera.transform.position, activeSpider.transform.position) < 2f);

        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        //TODO: Cover your ears.

        //tmp.text = "Cover your ears";
        //yield return new WaitForSeconds(3f);
        //_particleSystem.Play();
        //yield return new WaitForSeconds(10f);
        //_particleSystem.Stop();

        tmp.text = "Pet the spider.";

        SpiderInteractor _interactor = activeSpider.GetComponent<SpiderInteractor>();
        yield return new WaitForSeconds(5f);
        yield return new WaitUntil(() => _interactor.petted);
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        //TODO: Don't look up

        tmp.text = "Don't touch anything.";
        canTouchCurtain = true;
        curtain = GameObject.FindGameObjectWithTag("Curtain").GetComponent<CurtainScript>() ;
        int secondsToWait = 10;
        while (!curtain.isTouched)
        {
            yield return new WaitForSeconds(1f);
            secondsToWait--;
            if (secondsToWait == 0) break;
        }
        if (curtain.isTouched)
        {
            WhiteScreenEnd();
            yield break;
        }
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        tmp.text = "Kneel.";
        yield return new WaitUntil(() => playerCamera.transform.position.y < originalHeight);
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        IncreaseSpiderSize();
        tmp.text = "Kneel More.";
        while (true)
        {
            yield return new WaitForSeconds(5f);
            if (playerCamera.transform.position.y < originalHeight)
            {
                BlackScreenEnd();
            }
        }
    }
    private void Update()
    {
        if (canTouchCurtain && curtain != null && curtain.isTouched)
        {
            WhiteScreenEnd();
        }
    }
    private void WhiteScreenEnd()
    {
        curtain.isTouched = false;
        this.StopAllCoroutines();
        Application.Quit();
        //WHITE SCREEN CODE
    }
    private void BlackScreenEnd()
    {
        this.StopAllCoroutines();
        Application.Quit();
        //BLACK SCREEN CODE
    }

    public void IncreaseSpiderSize()
    {
        if (activeSpider == null)
        {
            Debug.LogError("Spider cannot increase size. Not found");
            return;
        }

        Vector3 scale = activeSpider.transform.localScale;
        activeSpider.transform.localScale = new Vector3(scale.x + 2, scale.y + 2, scale.z + 2);
    }
}
