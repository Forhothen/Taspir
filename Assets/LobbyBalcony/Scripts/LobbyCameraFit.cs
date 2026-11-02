using UnityEngine;

/// <summary>Keeps the entire painted composition visible at any Game view aspect ratio.</summary>
[ExecuteAlways, RequireComponent(typeof(Camera))]
public sealed class LobbyCameraFit : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float compositionWidth = 13.333333f;
    [SerializeField, Min(0.01f)] private float compositionHeight = 10f;
    private Camera view;

    private void OnEnable() => Fit();
    private void LateUpdate() => Fit();

    public void Fit()
    {
        if (view == null) view = GetComponent<Camera>();
        view.orthographic = true;
        view.orthographicSize = Mathf.Max(compositionHeight * 0.5f,
            compositionWidth * 0.5f / Mathf.Max(view.aspect, 0.01f));
    }
}
