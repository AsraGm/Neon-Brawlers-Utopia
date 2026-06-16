using System.Collections;
using UnityEngine;

public class Sensor : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int timeActive;
    [SerializeField] private SensorManager manager;

    [Header("Visual")]
    [SerializeField] private Material matOff;
    [SerializeField] private Material matPrendido;

    private MeshRenderer sensorRenderer;
    public bool isActive { get; private set; }

    private void Start()
    {
        sensorRenderer = GetComponent<MeshRenderer>();
        sensorRenderer.sharedMaterial = matOff;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(Activate());
            manager.ActivatedSensorsSucceesfully();
        }
    }

    private IEnumerator Activate()
    {
        isActive = true;
        sensorRenderer.sharedMaterial = matPrendido;

        yield return new WaitForSeconds(timeActive);

        sensorRenderer.sharedMaterial = matOff;
        isActive = false;
    }
}
