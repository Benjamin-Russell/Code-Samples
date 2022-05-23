using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

// Class for quick implementation of easing functions inspired by https://easings.net/
// Also helpful https://www.desmos.com/calculator (Some behaviors are marked with desmos formula)

// The purpose of this class is for the flexible interpolation of any value to another state.
// First an Easing is created with start and end values. Then it can be sampled.
// If the easing is in a transition, it will return the interpolated value based on its chosen behavior.
// If it has not begun, it will return the start value, and if it finished the transition, it will continue to return the end value. 

public class Easing
{
    public enum EasingBehavior
    {
        NULL = -1,
        LINEAR,         // Returns timeFactor
        START_VALUE,    // Returns _startValue
        END_VALUE,      // Returns _endValue
        CURVE,          // Use supplied animation curve

        QUAD_IN,
        QUAD_OUT,
        QUAD_IN_OUT,    // x * x

        CUBIC_IN,
        CUBIC_OUT,
        CUBIC_IN_OUT,   // x * x * x

        TRIG_IN,        // f\left(x\right)\ =\ 1-\cos\left(\frac{x\pi}{2}\right)
        TRIG_OUT,       // f\left(x\right)\ =\sin\left(\frac{x\pi}{2}\right)
        TRIG_IN_OUT,    // f\left(x\right)\ =\frac{\left(\cos\left(x\pi\right)-1\right)}{-2} 

        EXPO_IN,
        EXPO_OUT,
        EXPO_IN_OUT,    // Exponential

        BOUNCE_IN,
        BOUNCE_OUT,
        BOUNCE_IN_OUT,  // Like a ball

        BACK_IN,
        BACK_OUT,
        BACK_IN_OUT,    // Wind-up [Beyond 0-1]

        ELASTIC_IN,
        ELASTIC_OUT,
        ELASTIC_IN_OUT, // Rubber Band [Beyond 0-1]

        //---------
        NUM_EASINGS
    }

    public enum LoopBehavior
    { 
        NO_LOOP = 0,
        RESET,
        PING_PONG,
        PING_PONG_ONCE,
    }

    public enum PlayState
    { 
        UNPLAYED = 0,   // Sample() == _startValue
        IS_PLAYING,     // Sample() == [calculated]
        FINISHED,       // Sample() == _endValue
    }

    public EasingBehavior _easingType = EasingBehavior.NULL;
    public LoopBehavior _loopType = LoopBehavior.NO_LOOP;
    public AnimationCurve _animationCurve = null;
    public bool _timeScaleRelative = true;
    public bool _paused = false;

    public PlayState _PlayState { get { return _playState; } }
    private PlayState _playState = PlayState.UNPLAYED;

    private float _startTime = float.NegativeInfinity;
    public float _duration = 1f;
    public float _startValue = 0f;
    public float _endValue = 1f;

    public Easing(EasingBehavior type, LoopBehavior loopType, float duration = float.NegativeInfinity)
    {
        _easingType = type;
        _loopType = loopType;

        if (!float.IsNegativeInfinity(duration))
        {
            // Optionally set duration
            _duration = duration;
        }
    }

    public Easing(AnimationCurve curve, LoopBehavior loopType, float duration = float.NegativeInfinity)
    {
        _animationCurve = curve;
        _easingType = EasingBehavior.CURVE;
        _loopType = loopType;

        if (!float.IsNegativeInfinity(duration))
        {
            // Optionally set duration
            _duration = duration;
        }
    }


    // Get the current value, must be called from Update()
    public float Sample()
    {
        if (_paused)
        {
            if (_timeScaleRelative)
            {
                _startTime += Time.deltaTime;
            }
            else
            {
                _startTime += Time.unscaledDeltaTime;
            }
        }

        // Return value based on state of easing
        if (_playState == PlayState.UNPLAYED)
        {
            return _startValue;
        }
        else if (_playState == PlayState.IS_PLAYING)
        {
            float timeFactor = (GetCurrentTime() - _startTime) / _duration;

            if (timeFactor > 1f)
            {
                if (_loopType == LoopBehavior.NO_LOOP)
                {
                    // Easing finished
                    _playState = PlayState.FINISHED;
                    return _endValue;
                }
                else
                {
                    // Calculate new transition
                    while (timeFactor > 1f)
                    {
                        _startTime += _duration;
                        timeFactor = (GetCurrentTime() - _startTime) / _duration;

                        switch (_loopType)
                        {
                            case LoopBehavior.RESET:

                                // No change, reset back to start value
                                break;

                            case LoopBehavior.PING_PONG:

                                // Alternate start and endpoints
                                float tmp = _endValue;
                                _endValue = _startValue;
                                _startValue = tmp;
                                break;

                            case LoopBehavior.PING_PONG_ONCE:

                                // Only loop this time
                                _loopType = LoopBehavior.NO_LOOP;
                                goto case LoopBehavior.PING_PONG;
                        }
                    }
                }
            }

            // Calculate current value
            return GetValueAtTime(timeFactor);
        }

        Assert.IsTrue(_playState == PlayState.FINISHED);
        return _endValue;
    }


    #region State Management

    // Easing marks the start time for calculations
    public void Begin(float startValue, float endValue, float duration = float.NegativeInfinity)
    {
        _playState = PlayState.IS_PLAYING;
        _startTime = GetCurrentTime();
        _startValue = startValue;
        _endValue = endValue;

        if (!float.IsNegativeInfinity(duration))
        {
            // Optionally set duration
            _duration = duration;
        }

        if (_easingType == EasingBehavior.NULL)
        {
            Debug.LogError("Easing type not yet assigned!");
        }
    }

    // Begin Easing without values, so raw output can be applied to a Vector3.Lerp(), for instance
    public void Begin(float duration = float.NegativeInfinity)
    {
        Begin(0f, 1f, duration);
    }

    public void Reset()
    {
        _playState = PlayState.UNPLAYED;
        _startTime = float.NegativeInfinity;
    }

    #endregion

    #region Public Queries

    // Perform easing calculation
    public float GetValueAtTime(float t)
    {
        float value = t;

        switch (_easingType)
        {
            case EasingBehavior.LINEAR:
                // Value is linear by default
                break;

            case EasingBehavior.START_VALUE:
                value = 0f;
                break;

            case EasingBehavior.END_VALUE:
                value = 1f;
                break;

            case EasingBehavior.CURVE:

                if (_animationCurve != null)
                {
                    value = _animationCurve.Evaluate(t);
                }
                else
                {
                    Debug.LogError("Easing's animation curve is null!");
                }
                break;

            case EasingBehavior.QUAD_IN:
                value = t * t;
                break;

            case EasingBehavior.QUAD_OUT:
                value = 1f - ((1f - t) * (1f - t));
                break;

            case EasingBehavior.QUAD_IN_OUT:

                if (t < 0.5f)
                {
                    value = 2f * t * t;
                }
                else
                {
                    value = 1f - (Mathf.Pow(-2f * t + 2f, 2f) / 2f);
                }
                break;

            case EasingBehavior.CUBIC_IN:
                value = t * t * t;
                break;

            case EasingBehavior.CUBIC_OUT:
                value = 1f - Mathf.Pow(1f - t, 3f);
                break;

            case EasingBehavior.CUBIC_IN_OUT:

                if (t < 0.5f)
                {
                    value = 4f * t * t * t;
                }
                else
                {
                    value = 1f - (Mathf.Pow(-2f * t + 2f, 3f) / 2f);
                }
                break;

            case EasingBehavior.TRIG_IN:
                value = 1f - Mathf.Cos(t * Mathf.PI / 2f);
                break;

            case EasingBehavior.TRIG_OUT:
                value = Mathf.Sin(t * Mathf.PI / 2f);
                break;

            case EasingBehavior.TRIG_IN_OUT:
                value = (Mathf.Cos(Mathf.PI * t) - 1f) / -2f;
                break;

            case EasingBehavior.EXPO_IN:

                if (t > 0f)
                {
                    value = Mathf.Pow(2f, (10f * t) - 10f);
                }
                break;

            case EasingBehavior.EXPO_OUT:

                if (t < 1f)
                {
                    value = 1f - Mathf.Pow(2f, -10f * t);
                }
                break;

            case EasingBehavior.EXPO_IN_OUT:

                if (t != 0f
                    && t != 1f)
                {
                    if (t < 0.5f)
                    {
                        value = Mathf.Pow(2f, (20f * t) - 10f) / 2f;
                    }
                    else
                    {
                        value = (2f - Mathf.Pow(2f, (-20f * t) + 10f)) / 2f;
                    }
                }
                break;

            case EasingBehavior.BOUNCE_IN:
                value = 1f - CalculateBounceOut(1f - t);
                break;

            case EasingBehavior.BOUNCE_OUT:
                value = CalculateBounceOut(t);
                break;

            case EasingBehavior.BOUNCE_IN_OUT:

                if (t < 0.5f)
                {
                    value = (1f - CalculateBounceOut(1f - (2f * t))) / 2f;
                }
                else
                {
                    value = (1f + CalculateBounceOut((2f * t) - 1f)) / 2f;
                }
                break;

            case EasingBehavior.BACK_IN:
                value = (2.70158f * t * t * t) - (1.70158f * t * t);
                break;

            case EasingBehavior.BACK_OUT:
                value = 1f + (2.70158f * Mathf.Pow(t - 1f, 3f)) + (1.70158f * Mathf.Pow(t - 1f, 2f));
                break;

            case EasingBehavior.BACK_IN_OUT:

                if (t < 0.5f)
                {
                    value = (Mathf.Pow(2f * t, 2f) * ((2.595f + 1f) * 2f * t - 2.595f)) / 2f;
                }
                else
                {
                    value = (Mathf.Pow(2f * t - 2f, 2f) * ((2.595f + 1f) * (t * 2f - 2f) + 2.595f) + 2f) / 2f;
                }
                break;

            case EasingBehavior.ELASTIC_IN:

                if (t != 0f
                    && t != 1f)
                {
                    value = -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((t * 10f - 10.75f) * ((2f * Mathf.PI) / 3f));
                }
                break;

            case EasingBehavior.ELASTIC_OUT:

                if (t != 0f
                    && t != 1f)
                {
                    value = Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * ((2f * Mathf.PI) / 3f)) + 1f;
                }
                break;

            case EasingBehavior.ELASTIC_IN_OUT:

                if (t != 0f
                    && t != 1f)
                {
                    if (t < 0.5f)
                    {
                        value = -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * ((2f * Mathf.PI) / 4.5f))) / 2f;
                    }
                    else
                    {
                        value = (Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * ((2f * Mathf.PI) / 4.5f))) / 2f + 1f;
                    }
                }
                break;

            default:
                Debug.LogWarning("Easing not defined: " + _easingType.ToString());
                break;
        }

        return Mathf.LerpUnclamped(_startValue, _endValue, value);
    }

    #endregion

    #region Internal Calculations

    private float GetCurrentTime()
    {
        if (_timeScaleRelative)
            return Time.time;
        else
            return Time.unscaledTime;
    }

    private float CalculateBounceOut(float timeFactor)
    {
        float n1 = 7.5625f;
        float d1 = 2.75f;

        if (timeFactor < 1f / d1)
        {
            return n1 * timeFactor * timeFactor;
        }
        else if (timeFactor < 2f / d1)
        {
            return n1 * (timeFactor -= 1.5f / d1) * timeFactor + 0.75f;
        }
        else if (timeFactor < 2.5f / d1)
        {
            return n1 * (timeFactor -= 2.25f / d1) * timeFactor + 0.9375f;
        }
        else
        {
            return n1 * (timeFactor -= 2.625f / d1) * timeFactor + 0.984375f;
        }
    }

    #endregion
}