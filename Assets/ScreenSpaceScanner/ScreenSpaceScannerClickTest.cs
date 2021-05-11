using UnityEngine;

public class ScreenSpaceScannerClickTest : MonoBehaviour
{
    public float speed;
    private float _scanDistance;
    private static readonly int ScanDistance = Shader.PropertyToID("_ScanDistance");
    private static readonly int WorldSpaceScannerPos = Shader.PropertyToID("_WorldSpaceScannerPos");

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100)) {
                Shader.SetGlobalVector(WorldSpaceScannerPos, hit.point);
                _scanDistance = 0f;
            }
        }

        _scanDistance += speed * Time.deltaTime;
        Shader.SetGlobalFloat(ScanDistance, _scanDistance);
    }
}
