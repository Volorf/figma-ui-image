using UnityEngine;

namespace Volorf.FigmaUIImage
{
    public class FigmaUIImageData
    {
        Texture _texture;
        float _width;
        float _height;
        float _scale;
        string _lastUpdate;

        public FigmaUIImageData(Texture t, float s, string lastUpdate)
        {
            this._texture = t;
            this._width = t.width;
            this._height = t.height;
            this._scale = s;
            this._lastUpdate = lastUpdate;
        }

        public Texture GetTexture() => _texture;
        public float GetWidth() => _width;
        public float GetHeight() => _height;
        public float GetRatio() => _width / _height;
        public float GetScale() => _scale;
        public float GetOriginalWidth() => _width / _scale;
        public float GetOriginalHeight() => _height / _scale;
    }
}