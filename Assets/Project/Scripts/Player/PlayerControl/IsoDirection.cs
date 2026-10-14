using UnityEngine;

/// <summary>
/// 8 hướng, sắp xếp theo góc (0° = Đông, ngược chiều kim đồng hồ, mỗi bước 45°).
/// Thứ tự này cho phép cast thẳng từ "sector" góc sang enum.
/// </summary>
public enum Dir8 { E = 0, NE = 1, N = 2, NW = 3, W = 4, SW = 5, S = 6, SE = 7 }

public static class IsoDirection
{
    /// <summary>Hậu tố animation, khớp với tên clip: Idle_Down, Walk_Up_Side, ...</summary>
    public static readonly string[] AnimSuffixes = { "Down", "Down_Side", "Side", "Up_Side", "Up" };

    public static Dir8 FromVector(Vector2 v)
    {
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        int sector = Mathf.RoundToInt(angle / 45f) % 8;
        return (Dir8)sector;
    }

    public static Vector2 ToVector(Dir8 d)
    {
        float rad = (int)d * 45f * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    public static bool IsDiagonal(Dir8 d) => ((int)d & 1) == 1;

    /// <summary>
    /// Map 8 hướng -> 5 animation + cờ lật. flip = true nghĩa là hướng về bên TRÁI
    /// (giả định art gốc của "Side" quay sang phải).
    /// </summary>
    public static (int suffixIndex, bool flip) ToAnim(Dir8 d) => d switch
    {
        Dir8.S => (0, false),
        Dir8.SE => (1, false),
        Dir8.E => (2, false),
        Dir8.NE => (3, false),
        Dir8.N => (4, false),
        Dir8.NW => (3, true),
        Dir8.W => (2, true),
        Dir8.SW => (1, true),
        _ => (0, false),
    };

    /// <summary>
    /// Chuyển hướng màn hình sang vận tốc isometric.
    /// yScale = 0.5 cho tile 2:1 -> đi chéo sẽ chạy song song cạnh tile.
    /// </summary>
    public static Vector2 ToIso(Vector2 dir, float yScale) => new Vector2(dir.x, dir.y * yScale);
}