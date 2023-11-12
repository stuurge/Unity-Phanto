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

    private OVRSceneManager _ovrSceneManager;
    // Start is called before the first frame update
    void Start()
    {
        _colorLut = new OVRPassthroughColorLut(colorLutPng);
        playerCamera = GameObject.FindWithTag("MainCamera");
        passthroughLayer = GameObject.FindWithTag("Player").GetComponentInChildren<OVRPassthroughLayer>();
        StartCoroutine(StartSequence());
        StartCoroutine(GenerateEnvironment());
        StartCoroutine(StartSpider());
    }

    IEnumerator GenerateEnvironment()
    {
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

        MeshRenderer renderer = _roomData.Floor.gameObject.AddComponent<MeshRenderer>();
        renderer.material = swampMaterial;
        renderer.enabled = true;
        foreach(Component comp in _roomData.Floor.GetComponents<Component>())
        {
            Debug.LogError(comp.GetType());
        }
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
