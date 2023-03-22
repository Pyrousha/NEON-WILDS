using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static float Clamp(float _val, float _min, float _max)
    {
        return Mathf.Min(Mathf.Max(_val, _min), _max);
    }

    public static float Lerp360(float _from, float _to, float _t)
    {
        //Bound 0-360
        while (_from > 360)
            _from -= 360;
        while (_from < 0)
            _from += 360;

        while (_to > 360)
            _to -= 360;
        while (_to < 0)
            _to += 360;

        //Don't cross over 0
        if (Mathf.Abs(_from - _to) <= 180)
            return Mathf.Lerp(_from, _to, _t);

        //Cross over 0
        if (_from > _to)
        {
            _from -= 360;
            return Mathf.Lerp(_from, _to, _t);
        }
        if (_to > _from)
        {
            _to -= 360;
            return Mathf.Lerp(_from, _to, _t);
        }

        return _from;
    }
}
