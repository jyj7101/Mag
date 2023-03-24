using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoingHack.Utils
{
    using BoingHack.Type;
    using System;
    using System.Runtime.InteropServices;

    public static class CommonExtension
    {
        internal static Vector3 ComputeTranslationalResults(Transform t, Vector3 src, Vector3 dst, BoingBones b)
        {
            if (!b.LockTranslationX && !b.LockTranslationY && !b.LockTranslationZ)
            {
                return dst;
            }
            else
            {
                Vector3 delta = dst - src;

                switch (b.TranslationLockSpace)
                {
                    case TransformLockSpace.Global:
                        if (b.LockTranslationX)
                            delta.x = 0.0f;
                        if (b.LockTranslationY)
                            delta.y = 0.0f;
                        if (b.LockTranslationZ)
                            delta.z = 0.0f;
                        break;

                    case TransformLockSpace.Local:
                        if (b.LockTranslationX)
                            delta -= Vector3.Project(delta, t.right);
                        if (b.LockTranslationY)
                            delta -= Vector3.Project(delta, t.up);
                        if (b.LockTranslationZ)
                            delta -= Vector3.Project(delta, t.forward);
                        break;
                }

                return src + delta;
            }
        }

        internal static Quaternion ComputeRotationalResults(Transform t, Quaternion src, Quaternion dst, BoingBones b)
        {
            if (!b.LockRotationX && !b.LockRotationY && !b.LockRotationZ)
            {
                return dst;
            }
            else
            {
                Quaternion rInv = Quaternion.Inverse(t.rotation);

                switch (b.RotationLockSpace)
                {
                    case TransformLockSpace.Local:
                        src = src * rInv;
                        dst = dst * rInv;
                        break;
                }

                Quaternion delta = dst * Quaternion.Inverse(src);
                Vector3 eulerAngles = delta.eulerAngles;
                if (b.LockRotationX)
                    eulerAngles.x = 0.0f;
                if (b.LockRotationY)
                    eulerAngles.y = 0.0f;
                if (b.LockRotationZ)
                    eulerAngles.z = 0.0f;
                delta.eulerAngles = eulerAngles;

                Quaternion res = delta * src;

                switch (b.RotationLockSpace)
                {
                    case TransformLockSpace.Local:
                        res = t.rotation * res;
                        break;
                }

                return res;
            }
        }
    }

    public class Codec
    {
        // Vector2 between 0.0 and 1.0
        // https://stackoverflow.com/questions/17638800/storing-two-float-values-in-a-single-float-variable
        //-----------------------------------------------------------------------------

        public static float PackSaturated(float a, float b)
        {
            const int precision = 4096;
            a = Mathf.Floor(a * (precision - 1));
            b = Mathf.Floor(b * (precision - 1));
            return a * precision + b;
        }

        public static float PackSaturated(Vector2 v)
        {
            return PackSaturated(v.x, v.y);
        }

        public static Vector2 UnpackSaturated(float f)
        {
            const int precision = 4096;
            return new Vector2(Mathf.Floor(f / precision), Mathf.Repeat(f, precision)) / (precision - 1);
        }

        //-----------------------------------------------------------------------------
        // end: Vector2 between 0.0 and 1.0


        // normals
        // https://knarkowicz.wordpress.com/2014/04/16/octahedron-normal-vector-encoding/
        //-----------------------------------------------------------------------------

        public static Vector2 OctWrap(Vector2 v)
        {
            return
              (Vector2.one - new Vector2(Mathf.Abs(v.y), Mathf.Abs(v.x)))
              * new Vector2(Mathf.Sign(v.x), Mathf.Sign(v.y));
        }

        public static float PackNormal(Vector3 n)
        {
            n /= (Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z));
            Vector2 n2 = n.z >= 0.0f ? new Vector2(n.x, n.y) : OctWrap(new Vector2(n.x, n.y));
            n2 = n2 * 0.5f + 0.5f * Vector2.one;
            return PackSaturated(n2);
        }

        public static Vector3 UnpackNormal(float f)
        {
            Vector2 v = UnpackSaturated(f);
            v = v * 2.0f - Vector2.one;
            Vector3 n = new Vector3(v.x, v.y, 1.0f - Mathf.Abs(v.x) - Mathf.Abs(v.y));
            float t = Mathf.Clamp01(-n.z);
            n.x += n.x >= 0.0f ? -t : t;
            n.y += n.y >= 0.0f ? -t : t;
            return n.normalized;
        }

        //-----------------------------------------------------------------------------
        // end: normals


        // colors
        //-----------------------------------------------------------------------------

        public static uint PackRgb(Color color)
        {
            return
                (((uint)(color.b * 255)) << 16)
              | (((uint)(color.g * 255)) << 8)
              | (((uint)(color.r * 255)) << 0);
        }

        public static Color UnpackRgb(uint i)
        {
            return
              new Color
              (
                ((i & 0x000000FF) >> 0) / 255.0f,
                ((i & 0x0000FF00) >> 8) / 255.0f,
                ((i & 0x00FF0000) >> 16) / 255.0f
              );
        }

        public static uint PackRgba(Color color)
        {
            return
                (((uint)(color.a * 255)) << 24)
              | (((uint)(color.b * 255)) << 16)
              | (((uint)(color.g * 255)) << 8)
              | (((uint)(color.r * 255)) << 0);
        }

        public static Color UnpackRgba(uint i)
        {
            return
              new Color
              (
                ((i & 0x000000FF) >> 0) / 255.0f,
                ((i & 0x0000FF00) >> 8) / 255.0f,
                ((i & 0x00FF0000) >> 16) / 255.0f,
                ((i & 0xFF000000) >> 24) / 255.0f
              );
        }

        //-----------------------------------------------------------------------------
        // end: colors


        // bits
        //-----------------------------------------------------------------------------

        public static uint Pack8888(uint x, uint y, uint z, uint w)
        {
            return
                ((x & 0xFF) << 24)
              | ((y & 0xFF) << 16)
              | ((z & 0xFF) << 8)
              | ((w & 0xFF) << 0);
        }

        public static void Unpack8888(uint i, out uint x, out uint y, out uint z, out uint w)
        {
            x = (i >> 24) & 0xFF;
            y = (i >> 16) & 0xFF;
            z = (i >> 8) & 0xFF;
            w = (i >> 0) & 0xFF;
        }

        //-----------------------------------------------------------------------------
        // end: bits


        // hash
        //-----------------------------------------------------------------------------

        public static readonly int FnvDefaultBasis = unchecked((int)2166136261);
        public static readonly int FnvPrime = 16777619;

        [StructLayout(LayoutKind.Explicit)]
        private struct IntFloat
        {
            [FieldOffset(0)]
            public int IntValue;
            [FieldOffset(0)]
            public float FloatValue;
        }
        private static int IntReinterpret(float f)
        {
            return (new IntFloat { FloatValue = f }).IntValue;
        }

        public static int HashConcat(int hash, int i)
        {
            return (hash ^ i) * FnvPrime;
        }

        public static int HashConcat(int hash, long i)
        {
            hash = HashConcat(hash, (int)(i & 0xFFFFFFFF));
            hash = HashConcat(hash, (int)(i >> 32));
            return hash;
        }

        public static int HashConcat(int hash, float f)
        {
            return HashConcat(hash, IntReinterpret(f));
        }

        public static int HashConcat(int hash, bool b)
        {
            return HashConcat(hash, b ? 1 : 0);
        }

        public static int HashConcat(int hash, params int[] ints)
        {
            foreach (int i in ints)
                hash = HashConcat(hash, i);
            return hash;
        }

        public static int HashConcat(int hash, params float[] floats)
        {
            foreach (float f in floats)
                hash = HashConcat(hash, f);
            return hash;
        }

        public static int HashConcat(int hash, Vector2 v)
        {
            return HashConcat(hash, v.x, v.y);
        }

        public static int HashConcat(int hash, Vector3 v)
        {
            return HashConcat(hash, v.x, v.y, v.z);
        }

        public static int HashConcat(int hash, Vector4 v)
        {
            return HashConcat(hash, v.x, v.y, v.z, v.w);
        }

        public static int HashConcat(int hash, Quaternion q)
        {
            return HashConcat(hash, q.x, q.y, q.z, q.w);
        }

        public static int HashConcat(int hash, Color c)
        {
            return HashConcat(hash, c.r, c.g, c.b, c.a);
        }

        public static int HashConcat(int hash, Transform t)
        {
            return HashConcat(hash, t.GetHashCode());
        }

        public static int Hash(int i)
        {
            return HashConcat(FnvDefaultBasis, i);
        }

        public static int Hash(long i)
        {
            return HashConcat(FnvDefaultBasis, i);
        }

        public static int Hash(float f)
        {
            return HashConcat(FnvDefaultBasis, f);
        }

        public static int Hash(bool b)
        {
            return HashConcat(FnvDefaultBasis, b);
        }

        public static int Hash(params int[] ints)
        {
            return HashConcat(FnvDefaultBasis, ints);
        }

        public static int Hash(params float[] floats)
        {
            return HashConcat(FnvDefaultBasis, floats);
        }

        public static int Hash(Vector2 v)
        {
            return HashConcat(FnvDefaultBasis, v);
        }

        public static int Hash(Vector3 v)
        {
            return HashConcat(FnvDefaultBasis, v);
        }

        public static int Hash(Vector4 v)
        {
            return HashConcat(FnvDefaultBasis, v);
        }

        public static int Hash(Quaternion q)
        {
            return HashConcat(FnvDefaultBasis, q);
        }

        public static int Hash(Color c)
        {
            return HashConcat(FnvDefaultBasis, c);
        }

        private static int HashTransformHierarchyRecurvsive(int hash, Transform t)
        {
            hash = HashConcat(hash, t);
            hash = HashConcat(hash, t.childCount);
            for (int i = 0; i < t.childCount; ++i)
            {
                hash = HashTransformHierarchyRecurvsive(hash, t.GetChild(i));
            }
            return hash;
        }

        public static int HashTransformHierarchy(Transform t)
        {
            return HashTransformHierarchyRecurvsive(FnvDefaultBasis, t);
        }

        //-----------------------------------------------------------------------------
        // end: hash
    }
    public class VectorUtil
    {
        public static readonly Vector3 Min = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        public static readonly Vector3 Max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

        public static Vector3 Rotate2D(Vector3 v, float angle)
        {
            Vector3 results = v;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            results.x = cos * v.x - sin * v.y;
            results.y = sin * v.x + cos * v.y;
            return results;
        }

        public static Vector4 NormalizeSafe(Vector4 v, Vector4 fallback)
        {
            return
              v.sqrMagnitude > MathUtil.Epsilon
              ? v.normalized
              : fallback;
        }

        // Returns a vector orthogonal to given vector.
        // If the given vector is a unit vector, the returned vector will also be a unit vector.
        public static Vector3 FindOrthogonal(Vector3 v)
        {
            if (v.x >= MathUtil.Sqrt3Inv)
                return new Vector3(v.y, -v.x, 0.0f);
            else
                return new Vector3(0.0f, v.z, -v.y);
        }

        // Yields two extra vectors that form an orthogonal basis with the given vector.
        // If the given vector is a unit vector, the returned vectors will also be unit vectors.
        public static void FormOrthogonalBasis(Vector3 v, out Vector3 a, out Vector3 b)
        {
            a = FindOrthogonal(v);
            b = Vector3.Cross(a, v);
        }

        // Both vectors must be unit vectors.
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t)
        {
            float dot = Vector3.Dot(a, b);

            if (dot > 0.99999f)
            {
                // singularity: two vectors point in the same direction
                return Vector3.Lerp(a, b, t);
            }
            else if (dot < -0.99999f)
            {
                // singularity: two vectors point in the opposite direction
                Vector3 axis = FindOrthogonal(a);
                return Quaternion.AngleAxis(180.0f * t, axis) * a;
            }

            float rad = MathUtil.AcosSafe(dot);
            return (Mathf.Sin((1.0f - t) * rad) * a + Mathf.Sin(t * rad) * b) / Mathf.Sin(rad);
        }

        public static Vector3 GetClosestPointOnSegment(Vector3 p, Vector3 segA, Vector3 segB)
        {
            Vector3 v = segB - segA;
            if (v.sqrMagnitude < MathUtil.Epsilon)
                return 0.5f * (segA + segB);

            float d = Mathf.Clamp01(Vector3.Dot(p - segA, v.normalized) / v.magnitude);
            return segA + d * v;
        }

        public static Vector3 TriLerp
        (
          ref Vector3 v000, ref Vector3 v001, ref Vector3 v010, ref Vector3 v011,
          ref Vector3 v100, ref Vector3 v101, ref Vector3 v110, ref Vector3 v111,
          float tx, float ty, float tz
        )
        {
            Vector3 lerpPosY00 = Vector3.Lerp(v000, v001, tx);
            Vector3 lerpPosY10 = Vector3.Lerp(v010, v011, tx);
            Vector3 lerpPosY01 = Vector3.Lerp(v100, v101, tx);
            Vector3 lerpPosY11 = Vector3.Lerp(v110, v111, tx);
            Vector3 lerpPosZ0 = Vector3.Lerp(lerpPosY00, lerpPosY10, ty);
            Vector3 lerpPosZ1 = Vector3.Lerp(lerpPosY01, lerpPosY11, ty);
            return Vector3.Lerp(lerpPosZ0, lerpPosZ1, tz);
        }

        public static Vector3 TriLerp
        (
          ref Vector3 v000, ref Vector3 v001, ref Vector3 v010, ref Vector3 v011,
          ref Vector3 v100, ref Vector3 v101, ref Vector3 v110, ref Vector3 v111,
          bool lerpX, bool lerpY, bool lerpZ,
          float tx, float ty, float tz
        )
        {
            Vector3 lerpPosY00 = lerpX ? Vector3.Lerp(v000, v001, tx) : v000;
            Vector3 lerpPosY10 = lerpX ? Vector3.Lerp(v010, v011, tx) : v010;
            Vector3 lerpPosY01 = lerpX ? Vector3.Lerp(v100, v101, tx) : v100;
            Vector3 lerpPosY11 = lerpX ? Vector3.Lerp(v110, v111, tx) : v110;
            Vector3 lerpPosZ0 = lerpY ? Vector3.Lerp(lerpPosY00, lerpPosY10, ty) : lerpPosY00;
            Vector3 lerpPosZ1 = lerpY ? Vector3.Lerp(lerpPosY01, lerpPosY11, ty) : lerpPosY01;
            return lerpZ ? Vector3.Lerp(lerpPosZ0, lerpPosZ1, tz) : lerpPosZ0;
        }

        public static Vector3 TriLerp
        (
          ref Vector3 min, ref Vector3 max,
          bool lerpX, bool lerpY, bool lerpZ,
          float tx, float ty, float tz
        )
        {
            Vector3 lerpPosY00 =
              lerpX
              ? Vector3.Lerp(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), tx)
              : new Vector3(min.x, min.y, min.z);

            Vector3 lerpPosY10 =
              lerpX
              ? Vector3.Lerp(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), tx)
              : new Vector3(min.x, max.y, min.z);

            Vector3 lerpPosY01 =
              lerpX
              ? Vector3.Lerp(new Vector3(min.x, min.y, max.z), new Vector3(max.x, min.y, max.z), tx)
              : new Vector3(min.x, min.y, max.z);

            Vector3 lerpPosY11 =
              lerpX
              ? Vector3.Lerp(new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z), tx)
              : new Vector3(min.x, max.y, max.z);

            Vector3 lerpPosZ0 =
              lerpY
              ? Vector3.Lerp(lerpPosY00, lerpPosY10, ty)
              : lerpPosY00;

            Vector3 lerpPosZ1 =
              lerpY
              ? Vector3.Lerp(lerpPosY01, lerpPosY11, ty)
              : lerpPosY01;

            return lerpZ ? Vector3.Lerp(lerpPosZ0, lerpPosZ1, tz) : lerpPosZ0;
        }

        public static Vector4 TriLerp
        (
          ref Vector4 v000, ref Vector4 v001, ref Vector4 v010, ref Vector4 v011,
          ref Vector4 v100, ref Vector4 v101, ref Vector4 v110, ref Vector4 v111,
          bool lerpX, bool lerpY, bool lerpZ,
          float tx, float ty, float tz
        )
        {
            Vector4 lerpPosY00 = lerpX ? Vector4.Lerp(v000, v001, tx) : v000;
            Vector4 lerpPosY10 = lerpX ? Vector4.Lerp(v010, v011, tx) : v010;
            Vector4 lerpPosY01 = lerpX ? Vector4.Lerp(v100, v101, tx) : v100;
            Vector4 lerpPosY11 = lerpX ? Vector4.Lerp(v110, v111, tx) : v110;
            Vector4 lerpPosZ0 = lerpY ? Vector4.Lerp(lerpPosY00, lerpPosY10, ty) : lerpPosY00;
            Vector4 lerpPosZ1 = lerpY ? Vector4.Lerp(lerpPosY01, lerpPosY11, ty) : lerpPosY01;
            return lerpZ ? Vector4.Lerp(lerpPosZ0, lerpPosZ1, tz) : lerpPosZ0;
        }

        public static Vector4 TriLerp
        (
          ref Vector4 min, ref Vector4 max,
          bool lerpX, bool lerpY, bool lerpZ,
          float tx, float ty, float tz
        )
        {
            Vector4 lerpPosY00 =
              lerpX
              ? Vector4.Lerp(new Vector4(min.x, min.y, min.z), new Vector4(max.x, min.y, min.z), tx)
              : new Vector4(min.x, min.y, min.z);

            Vector4 lerpPosY10 =
              lerpX
              ? Vector4.Lerp(new Vector4(min.x, max.y, min.z), new Vector4(max.x, max.y, min.z), tx)
              : new Vector4(min.x, max.y, min.z);

            Vector4 lerpPosY01 =
              lerpX
              ? Vector4.Lerp(new Vector4(min.x, min.y, max.z), new Vector4(max.x, min.y, max.z), tx)
              : new Vector4(min.x, min.y, max.z);

            Vector4 lerpPosY11 =
              lerpX
              ? Vector4.Lerp(new Vector4(min.x, max.y, max.z), new Vector4(max.x, max.y, max.z), tx)
              : new Vector4(min.x, max.y, max.z);

            Vector4 lerpPosZ0 =
              lerpY
              ? Vector4.Lerp(lerpPosY00, lerpPosY10, ty)
              : lerpPosY00;

            Vector4 lerpPosZ1 =
              lerpY
              ? Vector4.Lerp(lerpPosY01, lerpPosY11, ty)
              : lerpPosY01;

            return lerpZ ? Vector4.Lerp(lerpPosZ0, lerpPosZ1, tz) : lerpPosZ0;
        }

        public static Vector3 ClampLength(Vector3 v, float minLen, float maxLen)
        {
            float lenSqr = v.sqrMagnitude;
            if (lenSqr < MathUtil.Epsilon)
                return v;

            float len = Mathf.Sqrt(lenSqr);
            return v * (Mathf.Clamp(len, minLen, maxLen) / len);
        }

        public static float MinComponent(Vector3 v)
        {
            return Mathf.Min(v.x, Mathf.Min(v.y, v.z));
        }

        public static float MaxComponent(Vector3 v)
        {
            return Mathf.Max(v.x, Mathf.Max(v.y, v.z));
        }

        public static Vector3 ComponentWiseAbs(Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static Vector3 ComponentWiseMult(Vector3 a, Vector3 b)
        {
            return Vector3.Scale(a, b);
        }

        public static Vector3 ComponentWiseDiv(Vector3 num, Vector3 den)
        {
            return new Vector3(num.x / den.x, num.y / den.y, num.z / den.z);
        }

        public static Vector3 ComponentWiseDivSafe(Vector3 num, Vector3 den)
        {
            return
              new Vector3
              (
                num.x * MathUtil.InvSafe(den.x),
                num.y * MathUtil.InvSafe(den.y),
                num.z * MathUtil.InvSafe(den.z)
              );
        }

        public static Vector3 ClampBend(Vector3 vector, Vector3 reference, float maxBendAngle)
        {
            float vLenSqr = vector.sqrMagnitude;
            if (vLenSqr < MathUtil.Epsilon)
                return vector;

            float rLenSqr = reference.sqrMagnitude;
            if (rLenSqr < MathUtil.Epsilon)
                return vector;

            Vector3 vUnit = vector / Mathf.Sqrt(vLenSqr);
            Vector3 rUnit = reference / Mathf.Sqrt(rLenSqr);

            Vector3 cross = Vector3.Cross(rUnit, vUnit);
            float dot = Vector3.Dot(rUnit, vUnit);
            Vector3 axis =
              cross.sqrMagnitude > MathUtil.Epsilon
                ? cross.normalized
                : FindOrthogonal(rUnit);
            float angle = Mathf.Acos(Mathf.Clamp01(dot));

            if (angle <= maxBendAngle)
                return vector;

            Quaternion clampedBendRot = QuaternionUtil.AxisAngle(axis, maxBendAngle);
            Vector3 result = clampedBendRot * reference;
            result *= Mathf.Sqrt(vLenSqr) / Mathf.Sqrt(rLenSqr);

            return result;
        }
    }
    public class MathUtil
    {
        public static readonly float Pi = Mathf.PI;
        public static readonly float TwoPi = 2.0f * Mathf.PI;
        public static readonly float HalfPi = Mathf.PI / 2.0f;
        public static readonly float QuaterPi = Mathf.PI / 4.0f;
        public static readonly float SixthPi = Mathf.PI / 6.0f;

        public static readonly float Sqrt2 = Mathf.Sqrt(2.0f);
        public static readonly float Sqrt2Inv = 1.0f / Mathf.Sqrt(2.0f);
        public static readonly float Sqrt3 = Mathf.Sqrt(3.0f);
        public static readonly float Sqrt3Inv = 1.0f / Mathf.Sqrt(3.0f);

        public static readonly float Epsilon = 1.0e-6f;
        public static readonly float Rad2Deg = 180.0f / Mathf.PI;
        public static readonly float Deg2Rad = Mathf.PI / 180.0f;

        public static float AsinSafe(float x)
        {
            return Mathf.Asin(Mathf.Clamp(x, -1.0f, 1.0f));
        }

        public static float AcosSafe(float x)
        {
            return Mathf.Acos(Mathf.Clamp(x, -1.0f, 1.0f));
        }

        public static float InvSafe(float x)
        {
            return 1.0f / Mathf.Max(Epsilon, x);
        }

        public static float PointLineDist(Vector2 point, Vector2 linePos, Vector2 lineDir)
        {
            var delta = point - linePos;
            return (delta - Vector2.Dot(delta, lineDir) * lineDir).magnitude;
        }

        public static float PointSegmentDist(Vector2 point, Vector2 segmentPosA, Vector2 segmentPosB)
        {
            var segmentVec = segmentPosB - segmentPosA;
            float segmentDistInv = 1.0f / segmentVec.magnitude;
            var segmentDir = segmentVec * segmentDistInv;
            var delta = point - segmentPosA;
            float t = Vector2.Dot(delta, segmentDir) * segmentDistInv;
            var closest = segmentPosA + Mathf.Clamp(t, 0.0f, 1.0f) * segmentVec;
            return (closest - point).magnitude;
        }

        public static float Seek(float current, float target, float maxDelta)
        {
            float delta = target - current;
            delta = Mathf.Sign(delta) * Mathf.Min(maxDelta, Mathf.Abs(delta));
            return current + delta;
        }

        public static Vector2 Seek(Vector2 current, Vector2 target, float maxDelta)
        {
            Vector2 delta = target - current;
            float deltaMag = delta.magnitude;
            if (deltaMag < Epsilon)
                return target;

            delta = Mathf.Min(maxDelta, deltaMag) * delta.normalized;
            return current + delta;
        }

        public static float Remainder(float a, float b)
        {
            return a - (a / b) * b;
        }

        public static int Remainder(int a, int b)
        {
            return a - (a / b) * b;
        }

        public static float Modulo(float a, float b)
        {
            return Mathf.Repeat(a, b);
        }

        public static int Modulo(int a, int b)
        {
            int r = a % b;
            return r >= 0 ? r : r + b;
        }
    }

    public class QuaternionUtil
    {
        // basic stuff
        // ------------------------------------------------------------------------

        public static float Magnitude(Quaternion q)
        {
            return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        }

        public static float MagnitudeSqr(Quaternion q)
        {
            return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        }

        public static Quaternion Normalize(Quaternion q)
        {
            float magInv = 1.0f / Magnitude(q);
            return new Quaternion(magInv * q.x, magInv * q.y, magInv * q.z, magInv * q.w);
        }

        // axis must be normalized
        public static Quaternion AxisAngle(Vector3 axis, float angle)
        {
            float h = 0.5f * angle;
            float s = Mathf.Sin(h);
            float c = Mathf.Cos(h);

            return new Quaternion(s * axis.x, s * axis.y, s * axis.z, c);
        }

        public static Vector3 GetAxis(Quaternion q)
        {
            Vector3 v = new Vector3(q.x, q.y, q.z);
            float len = v.magnitude;
            if (len < MathUtil.Epsilon)
                return Vector3.left;

            return v / len;
        }

        public static float GetAngle(Quaternion q)
        {
            return 2.0f * Mathf.Acos(Mathf.Clamp(q.w, -1.0f, 1.0f));
        }

        public static Quaternion FromAngularVector(Vector3 v)
        {
            float len = v.magnitude;
            if (len < MathUtil.Epsilon)
                return Quaternion.identity;

            v /= len;

            float h = 0.5f * len;
            float s = Mathf.Sin(h);
            float c = Mathf.Cos(h);

            return new Quaternion(s * v.x, s * v.y, s * v.z, c);
        }

        public static Vector3 ToAngularVector(Quaternion q)
        {
            Vector3 axis = GetAxis(q);
            float angle = GetAngle(q);

            return angle * axis;
        }

        public static Quaternion Pow(Quaternion q, float exp)
        {
            Vector3 axis = GetAxis(q);
            float angle = GetAngle(q) * exp;
            return AxisAngle(axis, angle);
        }

        // v: derivative of q
        public static Quaternion Integrate(Quaternion q, Quaternion v, float dt)
        {
            return Pow(v, dt) * q;
        }

        // omega: angular velocity (direction is axis, magnitude is angle)
        // https://www.ashwinnarayan.com/post/how-to-integrate-quaternions/
        // https://gafferongames.com/post/physics_in_3d/
        public static Quaternion Integrate(Quaternion q, Vector3 omega, float dt)
        {
            omega *= 0.5f;
            Quaternion p = (new Quaternion(omega.x, omega.y, omega.z, 0.0f)) * q;
            return Normalize(new Quaternion(q.x + p.x * dt, q.y + p.y * dt, q.z + p.z * dt, q.w + p.w * dt));
        }

        public static Vector4 ToVector4(Quaternion q)
        {
            return new Vector4(q.x, q.y, q.z, q.w);
        }

        public static Quaternion FromVector4(Vector4 v, bool normalize = true)
        {
            if (normalize)
            {
                float magSqr = v.sqrMagnitude;
                if (magSqr < MathUtil.Epsilon)
                    return Quaternion.identity;

                v /= Mathf.Sqrt(magSqr);
            }

            return new Quaternion(v.x, v.y, v.z, v.w);
        }

        // ------------------------------------------------------------------------
        // end: basic stuff


        // swing-twist decomposition & interpolation
        // ------------------------------------------------------------------------

        public static void DecomposeSwingTwist
        (
          Quaternion q,
          Vector3 twistAxis,
          out Quaternion swing,
          out Quaternion twist
        )
        {
            Vector3 r = new Vector3(q.x, q.y, q.z); // (rotaiton axis) * cos(angle / 2)

            // singularity: rotation by 180 degree
            if (r.sqrMagnitude < MathUtil.Epsilon)
            {
                Vector3 rotatedTwistAxis = q * twistAxis;
                Vector3 swingAxis = Vector3.Cross(twistAxis, rotatedTwistAxis);

                if (swingAxis.sqrMagnitude > MathUtil.Epsilon)
                {
                    float swingAngle = Vector3.Angle(twistAxis, rotatedTwistAxis);
                    swing = Quaternion.AngleAxis(swingAngle, swingAxis);
                }
                else
                {
                    // more singularity: rotation axis parallel to twist axis
                    swing = Quaternion.identity; // no swing
                }

                // always twist 180 degree on singularity
                twist = Quaternion.AngleAxis(180.0f, twistAxis);
                return;
            }

            // formula & proof: 
            // http://www.euclideanspace.com/maths/geometry/rotations/for/decomposition/
            Vector3 p = Vector3.Project(r, twistAxis);
            twist = new Quaternion(p.x, p.y, p.z, q.w);
            twist = Normalize(twist);
            swing = q * Quaternion.Inverse(twist);
        }

        public enum SterpMode
        {
            // non-constant angular velocity, faster
            // use if interpolating across small angles or constant angular velocity is not important
            Nlerp,

            // constant angular velocity, slower
            // use if interpolating across large angles and constant angular velocity is important
            Slerp,
        };

        // same swing & twist parameters
        public static Quaternion Sterp
        (
          Quaternion a,
          Quaternion b,
          Vector3 twistAxis,
          float t,
          SterpMode mode = SterpMode.Slerp
        )
        {
            Quaternion swing;
            Quaternion twist;
            return Sterp(a, b, twistAxis, t, out swing, out twist, mode);
        }

        // same swing & twist parameters with individual interpolated swing & twist outputs
        public static Quaternion Sterp
        (
          Quaternion a,
          Quaternion b,
          Vector3 twistAxis,
          float t,
          out Quaternion swing,
          out Quaternion twist,
          SterpMode mode = SterpMode.Slerp
        )
        {
            return Sterp(a, b, twistAxis, t, t, out swing, out twist, mode);
        }

        // different swing & twist parameters
        public static Quaternion Sterp
        (
          Quaternion a,
          Quaternion b,
          Vector3 twistAxis,
          float tSwing,
          float tTwist,
          SterpMode mode = SterpMode.Slerp
        )
        {
            Quaternion swing;
            Quaternion twist;
            return Sterp(a, b, twistAxis, tSwing, tTwist, out swing, out twist, mode);
        }

        // master sterp function
        public static Quaternion Sterp
        (
          Quaternion a,
          Quaternion b,
          Vector3 twistAxis,
          float tSwing,
          float tTwist,
          out Quaternion swing,
          out Quaternion twist,
          SterpMode mode
        )
        {
            Quaternion q = b * Quaternion.Inverse(a);
            Quaternion swingFull;
            Quaternion twistFull;
            QuaternionUtil.DecomposeSwingTwist(q, twistAxis, out swingFull, out twistFull);

            switch (mode)
            {
                default:
                case SterpMode.Nlerp:
                    swing = Quaternion.Lerp(Quaternion.identity, swingFull, tSwing);
                    twist = Quaternion.Lerp(Quaternion.identity, twistFull, tTwist);
                    break;
                case SterpMode.Slerp:
                    swing = Quaternion.Slerp(Quaternion.identity, swingFull, tSwing);
                    twist = Quaternion.Slerp(Quaternion.identity, twistFull, tTwist);
                    break;
            }

            return twist * swing;
        }

        // ------------------------------------------------------------------------
        // end: swing-twist decomposition & interpolation
    }

    public class Collision
    {
        public static bool SphereSphere(Vector3 centerA, float radiusA, Vector3 centerB, float radiusB, out Vector3 push)
        {
            push = Vector3.zero;

            Vector3 vec = centerA - centerB;
            float dd = vec.sqrMagnitude;
            float r = radiusA + radiusB;

            if (dd >= r * r)
            {
                return false;
            }

            float d = Mathf.Sqrt(dd);

            push = VectorUtil.NormalizeSafe(vec, Vector3.zero) * (r - d);
            return true;
        }

        public static bool SphereSphereInverse(Vector3 centerA, float radiusA, Vector3 centerB, float radiusB, out Vector3 push)
        {
            push = Vector3.zero;

            Vector3 vec = centerB - centerA;
            float dd = vec.sqrMagnitude;
            float r = radiusB - radiusA;

            if (dd <= r * r)
            {
                return false;
            }

            float d = Mathf.Sqrt(dd);

            push = VectorUtil.NormalizeSafe(vec, Vector3.zero) * (d - r);
            return true;
        }

        public static bool SphereCapsule(Vector3 centerA, float radiusA, Vector3 headB, Vector3 tailB, float radiusB, out Vector3 push)
        {
            push = Vector3.zero;

            Vector3 segVec = tailB - headB;
            float segLenSqr = segVec.sqrMagnitude;
            if (segLenSqr < MathUtil.Epsilon)
                return SphereSphereInverse(centerA, radiusA, 0.5f * (headB + tailB), radiusB, out push);

            float segLenInv = 1.0f / Mathf.Sqrt(segLenSqr);
            Vector3 segDir = segVec * segLenInv;
            Vector3 headToA = centerA - headB;
            float t = Mathf.Clamp01(Vector3.Dot(headToA, segDir) * segLenInv);
            Vector3 closestB = Vector3.Lerp(headB, tailB, t);

            return SphereSphere(centerA, radiusA, closestB, radiusB, out push);
        }

        public static bool SphereCapsuleInverse(Vector3 centerA, float radiusA, Vector3 headB, Vector3 tailB, float radiusB, out Vector3 push)
        {
            push = Vector3.zero;

            Vector3 segVec = tailB - headB;
            float segLenSqr = segVec.sqrMagnitude;
            if (segLenSqr < MathUtil.Epsilon)
                return SphereSphereInverse(centerA, radiusA, 0.5f * (headB + tailB), radiusB, out push);

            float segLenInv = 1.0f / Mathf.Sqrt(segLenSqr);
            Vector3 segDir = segVec * segLenInv;
            Vector3 headToA = centerA - headB;
            float t = Mathf.Clamp01(Vector3.Dot(headToA, segDir) * segLenInv);
            Vector3 closestB = Vector3.Lerp(headB, tailB, t);

            return SphereSphereInverse(centerA, radiusA, closestB, radiusB, out push);
        }

        public static bool SphereBox(Vector3 centerOffsetA, float radiusA, Vector3 halfExtentB, out Vector3 push)
        {
            push = Vector3.zero;

            Vector3 closestOnB =
              new Vector3
              (
                Mathf.Clamp(centerOffsetA.x, -halfExtentB.x, halfExtentB.x),
                Mathf.Clamp(centerOffsetA.y, -halfExtentB.y, halfExtentB.y),
                Mathf.Clamp(centerOffsetA.z, -halfExtentB.z, halfExtentB.z)
              );

            Vector3 vec = centerOffsetA - closestOnB;
            float dd = vec.sqrMagnitude;

            if (dd > radiusA * radiusA)
            {
                return false;
            }

            int numInBoxAxes =
                ((centerOffsetA.x < -halfExtentB.x || centerOffsetA.x > halfExtentB.x) ? 0 : 1)
              + ((centerOffsetA.y < -halfExtentB.y || centerOffsetA.y > halfExtentB.y) ? 0 : 1)
              + ((centerOffsetA.z < -halfExtentB.z || centerOffsetA.z > halfExtentB.z) ? 0 : 1);

            switch (numInBoxAxes)
            {
                case 0: // hit corner
                case 1: // hit edge
                case 2: // hit face
                {
                    push = VectorUtil.NormalizeSafe(vec, Vector3.right) * (radiusA - Mathf.Sqrt(dd));
                }
                break;
                case 3: // inside
                {
                    Vector3 penetration =
                      new Vector3
                      (
                        halfExtentB.x - Mathf.Abs(centerOffsetA.x) + radiusA,
                        halfExtentB.y - Mathf.Abs(centerOffsetA.y) + radiusA,
                        halfExtentB.z - Mathf.Abs(centerOffsetA.z) + radiusA
                      );

                    if (penetration.x < penetration.y)
                    {
                        if (penetration.x < penetration.z)
                            push = new Vector3(Mathf.Sign(centerOffsetA.x) * penetration.x, 0.0f, 0.0f);
                        else
                            push = new Vector3(0.0f, 0.0f, Mathf.Sign(centerOffsetA.z) * penetration.z);
                    }
                    else
                    {
                        if (penetration.y < penetration.z)
                            push = new Vector3(0.0f, Mathf.Sign(centerOffsetA.y) * penetration.y, 0.0f);
                        else
                            push = new Vector3(0.0f, 0.0f, Mathf.Sign(centerOffsetA.z) * penetration.z);
                    }
                }
                break;
            }

            return true;
        }

        public static bool SphereBoxInverse(Vector3 centerOffsetA, float radiusA, Vector3 halfExtentB, out Vector3 push)
        {
            push = Vector3.zero;

            // TODO?
            return false;
        }
    }

    [Serializable]
    public struct Bits32
    {
        [SerializeField] private int m_bits;
        public int IntValue { get { return m_bits; } }

        public Bits32(int bits = 0) { m_bits = bits; }

        public void Clear() { m_bits = 0; }

        public void SetBit(int index, bool value)
        {
            if (value)
                m_bits |= (1 << index);
            else
                m_bits &= ~(1 << index);
        }

        public bool IsBitSet(int index)
        {
            return (m_bits & (1 << index)) != 0;
        }
    }


}
