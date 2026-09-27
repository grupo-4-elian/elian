using System;
using System.Collections.Generic;
using UnityEngine;

// Animacion por cuadros sin Animator Controller: cada clip es una lista de
// sprites con su velocidad. Lo usan los personajes y objetos del Nivel 2.
public class FrameAnimator : MonoBehaviour
{
    [Serializable]
    public class Clip
    {
        public string name;
        public Sprite[] frames;
        public float fps = 10f;
        public bool loop = true;

        public float Length => frames == null || fps <= 0f ? 0f : frames.Length / fps;
    }

    [SerializeField] private SpriteRenderer target;
    [SerializeField] private List<Clip> clips = new List<Clip>();
    [SerializeField] private string playOnStart = "idle";

    private Clip current;
    private float time;
    private int lastFrame = -1;

    public string Current => current != null ? current.name : null;
    public bool IsPlaying { get; private set; }
    public int FrameIndex => lastFrame;

    // Se dispara cuando termina un clip que no se repite.
    public event Action<string> Finished;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (current == null && !string.IsNullOrEmpty(playOnStart))
            Play(playOnStart);
    }

    public bool Has(string clipName)
    {
        return Find(clipName) != null;
    }

    public float LengthOf(string clipName)
    {
        Clip c = Find(clipName);
        return c != null ? c.Length : 0f;
    }

    public void Play(string clipName, bool restart = false)
    {
        Clip c = Find(clipName);
        if (c == null || c.frames == null || c.frames.Length == 0)
            return;

        if (c == current && IsPlaying && !restart)
            return;

        current = c;
        time = 0f;
        lastFrame = -1;
        IsPlaying = true;
        ApplyFrame(0);
    }

    public void SetClips(List<Clip> newClips, SpriteRenderer renderer)
    {
        clips = newClips;
        target = renderer;
    }

    private void Update()
    {
        if (current == null || !IsPlaying)
            return;

        time += Time.deltaTime;
        int frame = Mathf.FloorToInt(time * current.fps);

        if (frame >= current.frames.Length)
        {
            if (current.loop)
            {
                time %= current.Length;
                frame = Mathf.FloorToInt(time * current.fps) % current.frames.Length;
            }
            else
            {
                ApplyFrame(current.frames.Length - 1);
                IsPlaying = false;
                Finished?.Invoke(current.name);
                return;
            }
        }

        ApplyFrame(frame);
    }

    private void ApplyFrame(int frame)
    {
        if (frame == lastFrame || target == null)
            return;

        lastFrame = frame;
        target.sprite = current.frames[Mathf.Clamp(frame, 0, current.frames.Length - 1)];
    }

    private Clip Find(string clipName)
    {
        foreach (Clip c in clips)
        {
            if (c.name == clipName)
                return c;
        }
        return null;
    }
}
