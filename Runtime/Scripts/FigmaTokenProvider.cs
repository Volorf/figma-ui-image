using System;
using UnityEngine;
using Volorf.FigmaUIImage;

[RequireComponent(typeof(FigmaUIImage))]
public class FigmaTokenProvider : MonoBehaviour
{
    [SerializeField] FigmaTokenAsset _tokenAsset;

    void Awake()
    {
        
    }
}
