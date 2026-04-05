using UnityEngine;

/// <summary>
/// Functionality extensions on the Vector class
/// </summary>
public static class VectorExtensions
{
    /// <summary>
    /// Returns a copy of this color with modified values for the given components
    /// </summary>
    /// <param name="original">this specifies the class to apply the extension method to</param>
    /// <param name="r">The new r value</param>
    /// <param name="g">The new g value</param>
    /// <param name="b">The new b value</param>
    /// <param name="a">The new a value</param>
    /// <returns>The modified color</returns>
    [JetBrains.Annotations.MustUseReturnValue("A modified copy of the vector is returned, you need to assign it.")]
    public static Color With(this Color original, float? r = null, float? g = null, float? b = null, float? a = null) =>
        new(r ?? original.r, g ?? original.g, b ?? original.b, a ?? original.a);

    /// <summary>
    /// Returns a copy of this vector with modified values for the given components
    /// </summary>
    /// <param name="original">this specifies the class to apply the extension method to</param>
    /// <param name="x">The new x value</param>
    /// <param name="y">The new y value</param>
    /// <param name="z">The new z value</param>
    /// <returns>The modified vector</returns>
    [JetBrains.Annotations.MustUseReturnValue("A modified copy of the vector is returned, you need to assign it.")]
    public static Vector3 With(this Vector3 original, float? x = null, float? y = null, float? z = null) => new(x ?? original.x, y ?? original.y, z ?? original.z);

    /// <summary>
    /// Adds the given components to this vector
    /// </summary>
    /// <param name="v">this specifies the class to apply the extension method to</param>
    /// <param name="x">The x value to add</param>
    /// <param name="y">The y value to add</param>
    /// <param name="z">The z value to add</param>
    /// <returns>Self for chaining</returns>
    [JetBrains.Annotations.MustUseReturnValue("A modified copy of the vector is returned, you need to assign it.")]
    public static Vector3 Add(this Vector3 v, float? x = null, float? y = null, float? z = null)
    {
        if (x.HasValue) v.x += x.Value;
        if (y.HasValue) v.y += y.Value;
        if (z.HasValue) v.z += z.Value;
        return v;
    }

    /// <summary>
    /// Returns the the given vector <paramref name="v"/> rotated by the given amount of <paramref name="degrees"/>
    /// </summary>
    public static Vector2 Rotate(this Vector2 v, float degrees)
    {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        v.x = cos * tx - sin * ty;
        v.y = sin * tx + cos * ty;
        return v;
    }

    public static Vector3 Div(this Vector3 a, Vector3 b)
    {
        var res = new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
        if (float.IsNaN(res.x)) res.x = 0f;
        if (float.IsNaN(res.y)) res.y = 0f;
        if (float.IsNaN(res.z)) res.z = 0f;
        return res;
    }
    public static Vector3 Mul(this Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);

    /// <summary>
    /// Returns the squared distance between this and the other vector
    /// </summary>
    public static float SqrDist(this Vector3 a, Vector3 b) => (a - b).sqrMagnitude;

    /// <summary>
    /// Returns the x and y coordinates as <see cref="Vector2">
    /// </summary>
    public static Vector2 XY(this Vector3 v) => new(v.x, v.y);

    /// <summary>
    /// Returns the x and z coordinates as <see cref="Vector2">
    /// </summary>
    public static Vector2 XZ(this Vector3 v) => new(v.x, v.z);

    /// <summary>
    /// Returns the x and y coordinates as x and z coordinates of a <see cref="Vector3">, with a new y defaulting to 0
    /// </summary>
    public static Vector3 X0Y(this Vector2 v, float newY = 0f) => new(v.x, newY, v.y);

    public static Vector3 XYX(this Vector2 v) => new(v.x, v.y, v.x);
}

/// <summary>
/// Class containing the vector equivalent operations of <see cref="Mathf"/>
/// </summary>
public static class MathV
{
    public static Vector3 Abs(Vector3 v) => new(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    public static Vector3 Sqrt(Vector3 v) => new(Mathf.Sqrt(v.x), Mathf.Sqrt(v.y), Mathf.Sqrt(v.z));

    public static Vector3 Sqr(Vector3 v) => new(v.x * v.x, v.y * v.y, v.z * v.z);

    public static Vector3 Round(Vector3 v) => new(Mathf.Round(v.x), Mathf.Round(v.y), Mathf.Round(v.z));

    public static Vector3Int RoundToInt(Vector3 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));

    public static Vector3 Min(Vector3 a, Vector3 b) => new(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));

    public static Vector3 Max(Vector3 a, Vector3 b) => new(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));

    public static Vector3 Max(Vector3 a, float b) => new(Mathf.Max(a.x, b), Mathf.Max(a.y, b), Mathf.Max(a.z, b));

    public static float Max(Vector3 v) => Mathf.Max(v.x, Mathf.Max(v.y, v.z));

    public static Vector2 Clamp(Vector2 v, Vector2 min, Vector2 max) => new(Mathf.Clamp(v.x, min.x, max.x), Mathf.Clamp(v.y, min.y, max.y));
    public static Vector3 Clamp(Vector3 v, Vector3 min, Vector3 max) => new(Mathf.Clamp(v.x, min.x, max.x), Mathf.Clamp(v.y, min.y, max.y), Mathf.Clamp(v.z, min.z, max.z));

    public static Vector3 Random(Vector3 range) => new(UnityEngine.Random.Range(0f, range.x), UnityEngine.Random.Range(0f, range.y), UnityEngine.Random.Range(0f, range.z));


    /// <summary>Returns the given two floats in ascending order</summary>
    public static (float, float) MinMax(float a, float b) => a <= b ? (a, b) : (b, a);

    public static float Sqr(float v) => v * v;


    /// <summary>
    /// Quaternion equivalent for Vector3.SmoothDamp
    /// </summary>
    public static Quaternion QuaternionSmoothDamp(Quaternion current, Quaternion target, ref float currentAngularSpeed, float smoothTime)
    {
        var delta = Quaternion.Angle(current, target);
        if (delta > 0.0f)
        {
            var t = Mathf.SmoothDampAngle(delta, 0.0f, ref currentAngularSpeed, smoothTime);
            t = 1.0f - t / delta;
            return Quaternion.Slerp(current, target, t);
        }
        return target;
    }

    /// <summary>
    /// Decomposes a vector into a normalized direction and its magnitude
    /// </summary>
    /// <param name="vector">The vector to decompose</param>
    /// <returns>(vector.normalized, vector.magnitude) equivalent</returns>
    public static (Vector3, float) Decompose(Vector3 vector)
    {
        var mag = vector.magnitude;
        if (mag > Vector3.kEpsilon)
            return (vector / mag, mag);
        else
            return (Vector3.zero, 0f);
    }
}
