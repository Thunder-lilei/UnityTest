using UnityEngine;

public class Footprint : MonoBehaviour
{
    public float lifetime = 2f;
    private Material mat;
    private float timer = 0f;

    void Start()
    {
        mat = GetComponentInChildren<Renderer>().material;
        Color c = mat.GetColor("_BaseColor");
        c.a = 1f;
        mat.SetColor("_BaseColor", c);
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = 1f - (timer / lifetime);
        Color c = mat.GetColor("_BaseColor");
        c.a = alpha;
        mat.SetColor("_BaseColor", c);

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}