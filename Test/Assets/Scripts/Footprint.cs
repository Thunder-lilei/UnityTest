using UnityEngine;

public class Footprint : MonoBehaviour
{
    public float lifetime = 3f;
    private Material mat;
    private float timer = 0f;

    void Start()
    {
        mat = GetComponentInChildren<Renderer>().material;
        Color c = mat.color;
        c.a = 1f;
        mat.color = c;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float alpha = 1f - (timer / lifetime);
        Color c = mat.color;
        c.a = alpha;
        mat.color = c;

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}