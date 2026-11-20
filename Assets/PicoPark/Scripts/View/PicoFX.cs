using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public class Particle
    {
        public Transform tr;
        public SpriteRenderer sr;
        public Vector2 vel;
        public float life;
        public float maxLife;
        public float gravity;
        public float spin;
        public float drag;
        public bool alive;
    }

    /// <summary>Squishy little square particles + screen shake. All procedural.</summary>
    public class PicoFX : MonoBehaviour
    {
        public Material spriteMat;
        private List<Particle> pool = new List<Particle>();
        private Transform poolRoot;
        private float trauma;
        private Vector3 camBase;

        public void Init(Material mat)
        {
            spriteMat = mat;
            poolRoot = new GameObject("Particles").transform;
            poolRoot.SetParent(transform, false);
        }

        private Particle Get()
        {
            for (int i = 0; i < pool.Count; i++)
                if (!pool[i].alive) return pool[i];
            GameObject go = new GameObject("p");
            go.transform.SetParent(poolRoot, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PicoArt.S("px");
            sr.material = spriteMat;
            sr.sortingOrder = GameRenderer.FXSortingOrder;
            Particle p = new Particle();
            p.tr = go.transform;
            p.sr = sr;
            pool.Add(p);
            return p;
        }

        public void Burst(Vector2 pos, Color color, int count, float speed, float life, float gravity, float size)
        {
            for (int i = 0; i < count; i++)
            {
                Particle p = Get();
                p.alive = true;
                p.tr.position = new Vector3(pos.x, pos.y, 0f);
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float sp = speed * Random.Range(0.35f, 1f);
                p.vel = new Vector2(Mathf.Cos(ang) * sp, Mathf.Abs(Mathf.Sin(ang)) * sp * 0.9f + speed * 0.25f);
                p.life = life * Random.Range(0.7f, 1.15f);
                p.maxLife = p.life;
                p.gravity = gravity;
                p.drag = 2.2f;
                p.spin = Random.Range(-360f, 360f);
                p.sr.color = color;
                p.sr.enabled = true;
                float s = size * Random.Range(0.6f, 1.2f);
                p.tr.localScale = new Vector3(s, s, 1f);
                p.tr.rotation = Quaternion.identity;
            }
        }

        public void Ring(Vector2 pos, Color color)
        {
            Particle p = Get();
            p.alive = true;
            p.sr.sprite = PicoArt.S("ring");
            p.tr.position = new Vector3(pos.x, pos.y, 0f);
            p.vel = Vector2.zero;
            p.life = 0.45f;
            p.maxLife = 0.45f;
            p.gravity = 0f;
            p.drag = 0f;
            p.spin = 0f;
            p.sr.color = color;
            p.sr.enabled = true;
            p.tr.localScale = new Vector3(0.6f, 0.6f, 1f);
        }

        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < pool.Count; i++)
            {
                Particle p = pool[i];
                if (!p.alive) continue;
                p.life -= dt;
                if (p.life <= 0f)
                {
                    p.alive = false;
                    p.sr.enabled = false;
                    if (p.sr.sprite != PicoArt.S("px")) p.sr.sprite = PicoArt.S("px");
                    continue;
                }
                p.vel.y -= p.gravity * dt;
                p.vel *= Mathf.Clamp01(1f - p.drag * dt);
                Vector3 pos = p.tr.position;
                pos.x += p.vel.x * dt;
                pos.y += p.vel.y * dt;
                p.tr.position = pos;
                p.tr.Rotate(0f, 0f, p.spin * dt);
                float k = p.life / p.maxLife;
                Color c = p.sr.color;
                c.a = Mathf.Clamp01(k * 1.4f);
                p.sr.color = c;
                if (p.sr.sprite == PicoArt.S("ring"))
                {
                    float s = Mathf.Lerp(6f, 1.2f, k);
                    p.tr.localScale = new Vector3(s, s, 1f);
                }
                else
                {
                    float s = Mathf.Clamp01(k * 1.2f) * p.tr.localScale.x / Mathf.Max(0.001f, p.tr.localScale.x);
                    p.tr.localScale = new Vector3(p.tr.localScale.x * 0.97f, p.tr.localScale.y * 0.97f, 1f);
                }
            }

            if (trauma > 0f)
            {
                Camera cam = Camera.main;
                PicoCamera pcam = cam != null ? cam.GetComponent<PicoCamera>() : null;
                float t = trauma * trauma;
                Vector3 offset = new Vector3(
                    Random.Range(-1f, 1f) * 0.32f * t,
                    Random.Range(-1f, 1f) * 0.32f * t, 0f);

                if (pcam != null)
                {
                    pcam.shakeOffset = offset;
                }
                else if (cam != null)
                {
                    if (camBase == Vector3.zero) camBase = cam.transform.position;
                    cam.transform.position = camBase + offset;
                }

                trauma = Mathf.Max(0f, trauma - dt * 1.6f);
                if (trauma <= 0f)
                {
                    if (pcam != null) pcam.shakeOffset = Vector3.zero;
                    else if (cam != null) cam.transform.position = camBase;
                }
            }
            else
            {
                camBase = Vector3.zero;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    PicoCamera pcam = cam.GetComponent<PicoCamera>();
                    if (pcam != null) pcam.shakeOffset = Vector3.zero;
                }
            }
        }
    }
}
