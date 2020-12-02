using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClearablePiece : MonoBehaviour
{
    protected GamePiece piece;
    // Start is called before the first frame update

    private void Awake()
    {
        piece = GetComponent<GamePiece>();
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void Clear()
    {
        Destroy(gameObject);
    }
}
