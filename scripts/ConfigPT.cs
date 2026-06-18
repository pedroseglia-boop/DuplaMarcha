using UnityEngine;

public class ConfigPT : MonoBehaviour
{
    public OVRPassthroughLayer passthroughLayer;

    void Start()
    {
        passthroughLayer = GetComponent<OVRPassthroughLayer>();

        if (passthroughLayer == null)
        {
            passthroughLayer = gameObject.AddComponent<OVRPassthroughLayer>();
        }

        Debug.Log("Passthrough configurado!");
    }
}