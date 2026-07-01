using FixMath;
using System;

public static class FixedMath
{
    // 三角函数表的大小，覆盖0到360度，每5度一个值
    private const int SinTableCount = 72;
    private static readonly Fixed64[] sinTab;

    // 静态构造函数，初始化三角函数表
    static FixedMath()
    {
        sinTab = new Fixed64[SinTableCount];
        for (int i = 0; i < SinTableCount; i++)
        {
            // 计算角度对应的弧度
            float angle = i * 5.0f; // 每5度一个值
            float sin = (float)Math.Sin(angle * Math.PI / 180.0f);
            // 将sin值转换为Fixed64类型并存储在表中
            sinTab[i] = sin.ToFixed();
        }
    }

    /// <summary>
    /// 弧度计算正弦值，使用预计算的三角函数表进行插值处理
    /// </summary>
    /// <param name="rad"></param>
    /// <returns></returns>
    public static Fixed64 Sin(Fixed64 a, bool isUseAngle = true)
    {
        Fixed64 angle = a;
        if (!isUseAngle)
        {
            angle = a * 180.ToFixed() / Fixed64.pi; // 将弧度转换为角度
        }
        // 将弧度转换为角度(0~360°)
        angle = angle.Mod(360.ToFixed());
        if (angle >= 360.ToFixed()) angle = Fixed64.Zero;

        Fixed64 step = 5.ToFixed(); // 每5度一个值
        Fixed64 index = angle / step; // 计算索引
        int indexint = index.ToInt(); // 获取整数部分作为索引
                                      // 边界保护
        if (indexint >= SinTableCount) indexint = SinTableCount - 1;

        Fixed64 t = index - indexint.ToFixed(); // 获取小数部分作为插值参数
        // 插值处理
        int nextIndex = (indexint + 1) % SinTableCount; // 下一个索引
        return Fixed64.Lerp(sinTab[indexint], sinTab[nextIndex], t); // 线性插值
    }
    public static Fixed64 Cos(Fixed64 a, bool isUseAngle = true)
    {
        if(isUseAngle)return Sin(a + 90.ToFixed(), true);
        return Sin(a + Fixed64.pi / 2.ToFixed(), false);
    }

    /// <summary>
    /// 计算一个数的幂，使用快速幂算法进行计算，效率较高
    /// </summary>
    public static Fixed64 Pow(Fixed64 a, int count)
    {
        if (count < 0)
        {
            a = Fixed64.One / a;
            count = -count;
        }
        Fixed64 result = Fixed64.One;
        Fixed64 baseValue = a;
        while (count > 0)
        {
            if ((count & 1) == 1) // 如果当前位是1，则乘以a
            {
                result = (result * baseValue) / Fixed64.One;
            }
            baseValue = (baseValue * baseValue) / Fixed64.One; // 平方a

            count >>= 1; // 右移一位，处理下一位
        }
        return result;
    }

    public static FixedVector2 ClampTarget(FixedVector2 point, FixedVector2 followPos, Fixed64 maxRange)
    {
        FixedVector2 delta= point - followPos;
        Fixed64 distance = delta.SqrMagnitude;
        if (distance > maxRange*maxRange)
        {
            delta = delta.Normalize * maxRange;
        }
        return followPos + delta;
    }
}