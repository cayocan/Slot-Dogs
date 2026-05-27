using UnityEngine;
using UnityEngine.UI;

public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 10f;

    private Image _image;
    private int _currentFrame;
    private float _timer;

    void Awake() => _image = GetComponent<Image>();

    void Update()
    {
        if (frames == null || frames.Length == 0 || _image == null)
            return;

        _timer += Time.deltaTime;
        if (_timer >= 1f / fps)
        {
            _timer = 0f;
            _currentFrame = (_currentFrame + 1) % frames.Length;
            _image.sprite = frames[_currentFrame];
        }
    }
}
