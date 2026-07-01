using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using FixMath;

namespace FixMath
{
    /// <summary>
    /// 定点数拓展方法
    /// </summary>
    public static class FixedExtern
    {
        public static Fixed64 ToFixed(this int x) => new Fixed64(x);
        public static Fixed64 ToFixed(this long x) => new Fixed64(x);
        public static Fixed64 ToFixed(this float x) => new Fixed64(x);
    }

    [System.Serializable]
    /// <summary>
    /// 定点数运算库
    /// </summary>
    public struct Fixed64 : IEquatable<Fixed64>
    {
        #region 内部结构定义
        /// <summary>
        /// 小数占用位数
        /// </summary>
        public const int Fix_FractionalBits = 16;
        /// <summary>
        /// 缩放因子
        /// </summary>
        private const long Scale = 1L << Fix_FractionalBits;
        /// <summary>
        /// 缩放因子平方
        /// </summary>
        private const long ScaleSquare = Scale * Scale;

        /// <summary>
        /// 最大值
        /// </summary>
        public static readonly Fixed64 MaxValue = new Fixed64((1L << (63 - Fix_FractionalBits)) - 1, true);

        /// <summary>
        /// 零值
        /// </summary>
        public static readonly Fixed64 Zero = new Fixed64(0);

        /// <summary>
        /// 一值
        /// </summary>
        public static readonly Fixed64 One = new Fixed64(1);

        /// <summary>
        /// pi值
        /// </summary>
        public static readonly Fixed64 pi = 3.14159265358979323846f.ToFixed();

        /// <summary>
        /// 原始数据
        /// </summary>
        public long m_Bits;
        #endregion

        #region 构造函数
        public Fixed64(int x) => m_Bits = (long)x * Scale;
        public Fixed64(long x) => m_Bits = x * Scale;
        public Fixed64(float x) => m_Bits = (long)Math.Round(x * Scale);
        public Fixed64(long bit, bool isRaw) => m_Bits = bit;// 初始化构造
        #endregion

        /// <summary>
        /// 获取原始数据
        /// </summary>
        /// <returns></returns>
        public long GetValue() => m_Bits;

        /// <summary>
        /// 设置原始数据
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        public Fixed64 SetValue(long x)
        {
            m_Bits = x;
            return this;
        }

        // 插值算法
        public static Fixed64 Lerp(Fixed64 a, Fixed64 b, float t) => a + (b - a) * new Fixed64(t);
        public static Fixed64 Lerp(Fixed64 a, Fixed64 b, Fixed64 t) => a + (b - a) * t;



        // 旋转插值(考虑2d旋转)
        public static Fixed64 RotationLerp(Fixed64 a, Fixed64 b, Fixed64 time)
        {
            a = a.Mod(360.ToFixed());
            b = b.Mod(360.ToFixed());

            var offset1 = b - a;
            var offset2 = b - (a + 360.ToFixed());
            return a + time * (offset1.Abs() > offset2.Abs() ? offset2 : offset1);
        }

        // 取模
        public Fixed64 Mod(Fixed64 x)
        {
            var remainder = this - x * Fixed64.Floor(this / x);
            return remainder < Fixed64.Zero ? remainder + x : remainder;
        }

        //向下取整
        //public static Fixed64 Floor(Fixed64 value)=>new Fixed64(value.m_Bits & ~(Scale - 1),true);
        //public static Fixed64 Floor(Fixed64 value) => new Fixed64(value.m_Bits / Scale * Scale, true);
        public static Fixed64 Floor(Fixed64 value)
        {
            long bits = value.m_Bits;
            long Mask = Scale - 1;
            if (bits >= 0)
            {
                return new Fixed64(bits & ~Mask, true);
            }
            else
            {
                return new Fixed64((bits+Scale) & ~Mask, true);
            }
        }

        // 绝对值
        public Fixed64 Abs() => Fixed64.Abs(this);
        public static Fixed64 Abs(Fixed64 x) => new Fixed64(Math.Abs(x.m_Bits), true);

        // 平方根
        public Fixed64 Sqrt() => Fixed64.Sqrt(this);
        public static Fixed64 Sqrt(Fixed64 x)
        {
            if (x < Zero) return Zero; // 负数没有平方根，返回零
            long temp1 = x.m_Bits * Scale;
            return new Fixed64((long)Math.Round(Math.Sqrt(temp1)), true);
        }

        // 幂运算
        public static Fixed64 Pow(Fixed64 p1, Fixed64 p2)
        {
            float value = (float)Math.Pow(p1.ToFloat(), p2.ToFloat());
            return new Fixed64(value);
        }

        // 范围限制
        public static Fixed64 Clamp(Fixed64 value, Fixed64 min, Fixed64 max) => value < min ? min : value > max ? max : value;

        #region 运算符重载
        // 加法重载
        public static Fixed64 operator +(Fixed64 a, Fixed64 b) => new Fixed64(a.m_Bits + b.m_Bits, true);
        public static Fixed64 operator +(Fixed64 a, int b) => new Fixed64(a.m_Bits + (long)b * Scale, true);
        public static Fixed64 operator +(int a, Fixed64 b) => a + b;
        public static Fixed64 operator +(Fixed64 a, long b) => new Fixed64(a.m_Bits + b * Scale, true);
        public static Fixed64 operator +(long a, Fixed64 b) => a + b;
        public static Fixed64 operator +(Fixed64 a, float b) => new Fixed64(a.m_Bits + (long)Math.Round(b * Scale), true);
        public static Fixed64 operator +(float a, Fixed64 b) => a + b;

        // 减法重载
        public static Fixed64 operator -(Fixed64 p1, Fixed64 p2) => new Fixed64(p1.m_Bits - p2.m_Bits, true);
        public static Fixed64 operator -(int p1, Fixed64 p2) => new Fixed64((long)p1 * Scale - p2.m_Bits, true);
        public static Fixed64 operator -(Fixed64 p1, int p2) => new Fixed64(p1.m_Bits - (long)p2 * Scale, true);
        public static Fixed64 operator -(long p1, Fixed64 p2) => new Fixed64(p1 * Scale - p2.m_Bits, true);
        public static Fixed64 operator -(Fixed64 p2, long p1) => new Fixed64(p2.m_Bits - p1 * Scale, true);
        public static Fixed64 operator -(float p1, Fixed64 p2) => new Fixed64((long)Math.Round(p1 * Scale) - p2.m_Bits, true);
        public static Fixed64 operator -(Fixed64 p2, float p1) => new Fixed64(p2.m_Bits - (long)Math.Round(p1 * Scale), true);
        public static Fixed64 operator -(Fixed64 p1) => new Fixed64(-p1.m_Bits, true);

        // 乘法重载
        public static Fixed64 operator *(Fixed64 a, Fixed64 b) => new Fixed64((a.m_Bits * b.m_Bits) >> Fix_FractionalBits, true);
        public static Fixed64 operator *(Fixed64 a, int b) => new Fixed64(a.m_Bits * (long)b, true);
        public static Fixed64 operator *(int a, Fixed64 b) => b * a;
        public static Fixed64 operator *(Fixed64 a, long b) => new Fixed64(a.m_Bits * b, true);
        public static Fixed64 operator *(long a, Fixed64 b) => b * a;
        public static Fixed64 operator *(Fixed64 a, float b) => new Fixed64((long)Math.Round(a.m_Bits * b), true);
        public static Fixed64 operator *(float a, Fixed64 b) => b * a;

        // 除法重载
        public static Fixed64 operator /(Fixed64 a, Fixed64 b)
        {
            if (b == Zero)
            {
                Debug.LogError("Division by zero in Fixed64 division.");
                return Zero; // 抛出异常
            }
            return new Fixed64((a.m_Bits * Scale) / b.m_Bits, true);
        }
        public static Fixed64 operator /(int a, Fixed64 b)
        {
            if (b == Zero)
            {
                Debug.LogError("Division by zero in Fixed64 division.");
                return Zero; // 抛出异常
            }
            return new Fixed64((long)a * ScaleSquare / b.m_Bits, true);
        }
        public static Fixed64 operator /(Fixed64 a, int b)
        {
            if (b == 0)
            {
                Debug.LogError("Division by zero in Fixed64 division.");
                return Zero; // 抛出异常
            }
            return new Fixed64(a.m_Bits / (long)b, true);
        }
        public static Fixed64 operator /(long a, Fixed64 b)
        {
            if (b == Zero)
            {
                Debug.LogError("Division by zero in Fixed64 division.");
                return Zero; // 抛出异常
            }
            return new Fixed64(a * ScaleSquare / b.m_Bits, true);
        }
        public static Fixed64 operator /(Fixed64 a, long b)
        {
            if (b == 0)
            {
                Debug.LogError("Division by zero in Fixed64 division.");
                return Zero; // 抛出异常
            }
            return new Fixed64(a.m_Bits / b, true);
        }
        public static Fixed64 operator /(float a, Fixed64 b)
        {
            if (b == Zero)
            {
                Debug.LogError("Division by zero in Fixed64 division.");
                return Zero; // 抛出异常
            }
            return new Fixed64((long)Math.Round(a * ScaleSquare) / b.m_Bits, true);
        }
        public static Fixed64 operator /(Fixed64 a, float b)
        {
            if (Math.Abs(b) < 1e-6f)
            {
                Debug.LogError("Division by zero in Fixed64 division.");
                return Zero; // 抛出异常
            }
            return new Fixed64((long)Math.Round(a.m_Bits / b), true);
        }
        /// <summary>
        /// 取模运算 待修改
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Fixed64 operator %(Fixed64 a, Fixed64 b)
        {
            if (b == Zero)
            {
                Debug.LogError("Division by zero in Fixed64 modulus.");
                return Zero; // 抛出异常
            }
            long remainder = a.m_Bits % b.m_Bits;
            return new Fixed64(remainder < 0 ? remainder + Math.Abs(b.m_Bits) : remainder, true);
        }

        // 比较运算符重载
        public static bool operator >(Fixed64 a, Fixed64 b) => a.m_Bits > b.m_Bits;
        public static bool operator <(Fixed64 a, Fixed64 b) => a.m_Bits < b.m_Bits;
        public static bool operator >=(Fixed64 a, Fixed64 b) => a.m_Bits >= b.m_Bits;
        public static bool operator <=(Fixed64 a, Fixed64 b) => a.m_Bits <= b.m_Bits;
        public static bool operator ==(Fixed64 a, Fixed64 b) => a.m_Bits == b.m_Bits;
        public static bool operator !=(Fixed64 a, Fixed64 b) => a.m_Bits != b.m_Bits;

        // 其他类型比较
        public static bool operator >(Fixed64 a, float b) => a > b.ToFixed();
        public static bool operator <(Fixed64 a, float b) => a < b.ToFixed();
        public static bool operator >=(Fixed64 a, float b) => a >= b.ToFixed();
        public static bool operator <=(Fixed64 a, float b) => a <= b.ToFixed();
        public static bool operator !=(Fixed64 a, float b) => a != b.ToFixed();
        public static bool operator ==(Fixed64 a, float b) => a == b.ToFixed();

        // 重写Equals和GetHashCode
        public bool Equals(Fixed64 other) => this == other;
        public override bool Equals(object obj)
        {
            return obj is Fixed64 other && this == other;
        }
        public override int GetHashCode() => m_Bits.GetHashCode();

        // 工具函数
        public static Fixed64 Min(Fixed64 a, Fixed64 b) => a < b ? a : b;
        public static Fixed64 Max(Fixed64 a, Fixed64 b) => a > b ? a : b;
        public static Fixed64 Precision() => new Fixed64(1L, true); // 最小精度为1/65536

        //类型转换
        public float ToFloat() => (float)m_Bits / Scale;
        public int ToInt() => (int)(m_Bits / Scale);
        public override string ToString() => ToFloat().ToString("F6"); // 保留6位小数的字符串表示

        #endregion
    }

    [System.Serializable]
    public struct FixedVector2 : IEquatable<FixedVector2>
    {
        public Fixed64 x;
        public Fixed64 y;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public FixedVector2(Fixed64 x, Fixed64 y)
        {
            this.x = x;
            this.y = y;
        }
        public FixedVector2(Vector2 vector)
        {
            this.x = vector.x.ToFixed();
            this.y = vector.y.ToFixed();
        }

        public FixedVector2(float a, float b) : this(a.ToFixed(), b.ToFixed()) { }
        public FixedVector2(int a, int b) : this(a.ToFixed(), b.ToFixed()) { }

        public static readonly FixedVector2 Zero = new FixedVector2(Fixed64.Zero, Fixed64.Zero);
        public static readonly FixedVector2 One = new FixedVector2(Fixed64.One, Fixed64.One);
        public static readonly FixedVector2 Up = new FixedVector2(Fixed64.Zero, Fixed64.One);
        public static readonly FixedVector2 Right = new FixedVector2(Fixed64.One, Fixed64.Zero);
        public static readonly FixedVector2 Down = new FixedVector2(Fixed64.Zero, -Fixed64.One);
        public static readonly FixedVector2 Left = new FixedVector2(-Fixed64.One, Fixed64.Zero);

        /// <summary>
        /// 向量的长度（模），通过勾股定理计算得到
        /// </summary>
        public Fixed64 Magnitude => Fixed64.Sqrt(x * x + y * y);
        /// <summary>
        /// 向量的平方长度，避免了平方根运算，适用于只需要比较长度大小的情况
        /// </summary>
        public Fixed64 SqrMagnitude => x * x + y * y;

        // 归一化
        public FixedVector2 Normalize
        {
            get
            {
                var mag = Magnitude;
                return mag > Fixed64.Zero ? this / mag : Zero;
            }
        }
        #region 运算重载
        // 运算符重载
        public static FixedVector2 operator +(FixedVector2 a, FixedVector2 b) => new FixedVector2(a.x + b.x, a.y + b.y);
        public static FixedVector2 operator -(FixedVector2 a, FixedVector2 b) => new FixedVector2(a.x - b.x, a.y - b.y);
        public static FixedVector2 operator -(FixedVector2 a) => new FixedVector2(-a.x, -a.y);
        public static FixedVector2 operator *(FixedVector2 a, Fixed64 b) => new FixedVector2(a.x * b, a.y * b);
        public static FixedVector2 operator *(Fixed64 a, FixedVector2 b) => b * a;
        public static FixedVector2 operator /(FixedVector2 a, Fixed64 b) => new FixedVector2(a.x / b, a.y / b);

        // 比较运算符重载
        public static bool operator ==(FixedVector2 a, FixedVector2 b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(FixedVector2 a, FixedVector2 b) => !(a == b);
        #endregion

        #region 基础函数
        // 向量运算
        public static Fixed64 Dot(FixedVector2 a, FixedVector2 b) => a.x * b.x + a.y * b.y;// 点积
        public static Fixed64 Cross(FixedVector2 a, FixedVector2 b) => a.x * b.y - a.y * b.x;// 叉积
        public static FixedVector2 Lerp(FixedVector2 a, FixedVector2 b, Fixed64 t) => a + (b - a) * t;// 线性插值
        #endregion
        // 旋转向量(绕原点)
        public FixedVector2 Rotate(Fixed64 angle)
        {
            Fixed64 rad = angle * Fixed64.pi / 180.ToFixed();
            Fixed64 cos = FixedMath.Cos(rad,false);
            Fixed64 sin = FixedMath.Sin(rad,false);
            return new FixedVector2(x * cos - y * sin, x * sin + y * cos);

        }

        /// <summary>
        /// 点到线段的距离
        /// </summary>
        public static FixedVector2 ClosetPointToSegment(FixedVector2 p, FixedVector2 a, FixedVector2 b)
        {
            FixedVector2 ab = b - a;
            FixedVector2 ap = p - a;
            Fixed64 t = Dot(ab, ap) / Dot(ab, ab);
            t = Fixed64.Clamp(t, Fixed64.Zero, Fixed64.One);
            return a + (b - a) * t;

        }

        // 类型转换
        public Vector2 ToVector2() => new Vector2(x.ToFloat(), y.ToFloat());
        public Vector3 ToVector3() => new Vector3(x.ToFloat(), y.ToFloat(), 0f);
        public bool Equals(FixedVector2 other) => this == other;
        public override bool Equals(object obj) => obj is FixedVector2 other && Equals(obj);
        public override int GetHashCode() => HashCode.Combine(x, y);
        public override string ToString() => $"{x},{y}";

        // 隐式转换
        public static implicit operator UnityEngine.Vector2(FixedVector2 v) => v.ToVector2();
        public static implicit operator FixedVector2(UnityEngine.Vector2 v) => new FixedVector2(v.x, v.y);
    }

}
