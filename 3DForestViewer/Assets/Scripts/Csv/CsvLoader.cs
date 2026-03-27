
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

public static class CsvLoader
{
    /// <summary>
    /// ヘッダー行あり / カンマ区切り / クォート無し前提の CSV を List<T> に変換。
    /// [CsvColumn("HeaderName")] があればそれを優先。なければメンバー名で一致。
    /// </summary>
    public static List<T> Load<T>(string csvText, char delimiter = ',') where T : new()
    {
        if (string.IsNullOrWhiteSpace(csvText))
            return new List<T>();

        // 改行統一 & 空行削除
        var lines = csvText.Replace("\r\n", "\n").Replace("\r", "\n")
                           .Split('\n')
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .ToList();

        if (lines.Count < 2)
            return new List<T>();

        // ヘッダー（BOM除去）
        var headers = SplitSimple(lines[0], delimiter);
        if (headers.Count == 0) return new List<T>();
        headers[0] = headers[0].TrimStart('\uFEFF');

        // 反射でメンバー一覧取得
        var type = typeof(T);

        var members = new List<(string key, Type memberType, Action<T, object> set)>();

        // public field
        foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            var attr = f.GetCustomAttribute<CsvColumnAttribute>();
            var key = attr?.Name ?? f.Name;
            members.Add((key, f.FieldType, (obj, val) => f.SetValue(obj, val)));
        }

        // public property(set可能)
        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
        {
            var attr = p.GetCustomAttribute<CsvColumnAttribute>();
            var key = attr?.Name ?? p.Name;
            members.Add((key, p.PropertyType, (obj, val) => p.SetValue(obj, val)));
        }

        // key(列名) -> setter の辞書
        var memberMap = members.ToDictionary(m => m.key, m => m, StringComparer.OrdinalIgnoreCase);

        // ヘッダー順に setter を組み立て（高速化）
        var setters = new List<Action<T, string>>(headers.Count);
        foreach (var h in headers)
        {
            if (memberMap.TryGetValue(h, out var m))
            {
                setters.Add((obj, raw) =>
                {
                    var converted = ConvertValue(raw, m.memberType);
                    m.set(obj, converted);
                });
            }
            else
            {
                // クラスに無い列は無視
                setters.Add((obj, raw) => { });
            }
        }

        // データ行 → List<T>
        var result = new List<T>(lines.Count - 1);

        for (int i = 1; i < lines.Count; i++)
        {

            var cols = SplitSimple(lines[i], delimiter);

            // 足りない列は空で埋める（安全策）
            while (cols.Count < headers.Count) cols.Add(string.Empty);

            // 余分な列があっても無視したい場合は切り捨て（任意）
            if (cols.Count > headers.Count)
                cols = cols.Take(headers.Count).ToList();

            // ★ すべての列を必須：1つでも空/空白ならスキップ
            if (cols.Any(string.IsNullOrWhiteSpace))
                continue;

            var obj = new T();
            for (int c = 0; c < headers.Count; c++)
            {
                setters[c](obj, cols[c]);
            }

            result.Add(obj);
        }

        return result;
    }

    private static List<string> SplitSimple(string line, char delimiter)
        => line.Split(delimiter).Select(s => s.Trim()).ToList();

    private static object ConvertValue(string raw, Type targetType)
    {
        raw = raw?.Trim() ?? string.Empty;

        // Nullable<T>
        var underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
        {
            if (string.IsNullOrEmpty(raw)) return null;
            return ConvertValue(raw, underlying);
        }

        // 空なら default / null
        if (string.IsNullOrEmpty(raw))
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        var ci = CultureInfo.InvariantCulture;

        if (targetType == typeof(string)) return raw;

        if (targetType == typeof(int)) return int.Parse(raw, ci);
        if (targetType == typeof(long)) return long.Parse(raw, ci);
        if (targetType == typeof(ulong)) return ulong.Parse(raw, ci);

        if (targetType == typeof(float)) return float.Parse(raw, ci);
        if (targetType == typeof(double)) return double.Parse(raw, ci);
        if (targetType == typeof(decimal)) return decimal.Parse(raw, ci);

        if (targetType == typeof(bool))
        {
            if (raw == "1") return true;
            if (raw == "0") return false;
            return bool.Parse(raw);
        }

        if (targetType.IsEnum)
            return Enum.Parse(targetType, raw, ignoreCase: true);

        return Convert.ChangeType(raw, targetType, ci);
    }
}
