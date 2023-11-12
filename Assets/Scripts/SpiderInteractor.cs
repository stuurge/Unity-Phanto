using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpiderInteractor : MonoBehaviour
{
    // Start is called before the first frame update
    public ParticleSystem particleSystem;
    public bool petted;
    public RoomVegetationGenerator roomVegetationGenerator;
    void Start()
    {
        particleSystem = GetComponentInChildren<ParticleSystem>();
        roomVegetationGenerator = GameObject.FindGameObjectWithTag("Manager").GetComponent<RoomVegetationGenerator>() ;
    }
    private void OnTriggerEnter(Collider other)
    {
        particleSystem.Play();
        petted = true;

    }
    // Update is called once per frame
    void Update()
    {
        if (this.transform.position.y < -3)
        {
            this.transform.position = new Vector3(transform.position.x, 1f, transform.position.z);
        }
    }
}
