using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorPiece : MonoBehaviour
{

    public Color[] availableColors = { Color.red, new Color(255,118,0,255), Color.yellow, Color.green, Color.blue, new Color(135, 0, 195, 255) };

    public int numColors
    {
        get { return availableColors.Length; }
    }

    private SpriteRenderer sprite;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetColor(int thisIndex)
    {
        sprite.color = availableColors[thisIndex];
    }
}
