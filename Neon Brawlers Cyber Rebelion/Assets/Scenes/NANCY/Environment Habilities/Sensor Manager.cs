using UnityEngine;
using UnityEngine.Events;

public class SensorManager : MonoBehaviour
{
    [SerializeField] Sensor sensor1;
    [SerializeField] Sensor sensor2;

    public UnityEvent SensorsActive;

    public void ActivatedSensorsSucceesfully()
    {
        if (sensor1.isActive && sensor2.isActive)
        {
            SensorsActive?.Invoke();
        }
    }
}
