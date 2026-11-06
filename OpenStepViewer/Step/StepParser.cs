using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenStepViewer.Step
{
    // ISO 10303-21 实体图：#id = TYPE(args);
    public enum ArgKind { List, Typed, Ref, Str, Enum, Num, Null, Star, Complex }

    public sealed class Arg
    {
        public ArgKind Kind;
        public string Str;              // Str / Enum / Complex raw
        public double Num;
        public int Ref;
        public string Type;             // Typed
        public List<Arg> Items = new List<Arg>();

        public Arg this[int i] => Items[i];
        public int Count => Items.Count;

        public static readonly Arg Null = new Arg { Kind = ArgKind.Null };

        public override string ToString() =>
            Kind switch
            {
                ArgKind.List => "(" + string.Join(",", Items) + ")",
                ArgKind.Typed => Type + "(" + string.Join(",", Items) + ")",
                ArgKind.Ref => "#" + Ref,
                ArgKind.Str => "'" + Str + "'",
                ArgKind.Enum => "." + Str + ".",
                ArgKind.Num => Num.ToString("R", CultureInfo.InvariantCulture),
                ArgKind.Null => "$",
                ArgKind.Star => "*",
                _ => Str
            };
    }

    public sealed class StepEntity
    {
        public int Id;
        public string Type;             // 大写；Complex 为 "|A|B|C|" 形式
        public string RawArgs;          // 原始参数串
        Arg _parsed;
        public Arg Args
        {
            get
            {
                if (_parsed == null)
                {
                    var root = StepParser.ParseArgs(RawArgs);
                    _parsed = new Arg { Kind = ArgKind.Typed, Type = Type, Items = root.Items };
                }
                return _parsed;
            }
        }
    }

    public sealed class StepFile
    {
        public Dictionary<int, StepEntity> Entities = new Dictionary<int, StepEntity>();
        public string Schema = "";

        public StepEntity Get(int id) => Entities.TryGetValue(id, out var e) ? e : null;

        public IEnumerable<StepEntity> ByType(params string[] types)
        {
            var set = new HashSet<string>(types, StringComparer.OrdinalIgnoreCase);
            foreach (var e in Entities.Values)
                if (set.Contains(e.Type)) yield return e;
        }
    }

    public static class StepParser
    {
        // 将整个文件切分为实体（按括号深度 0 且不在字符串内的 ';' 分段）
        public static StepFile Parse(string text)
        {
            var file = new StepFile();
            bool inHeader = false;
            int i = 0, n = text.Length;
            long guard = 0;

            while (i < n)
            {
                if (++guard > 50_000_000) throw new Exception($"解析死循环保护触发 @ i={i}, 实体数={file.Entities.Count}, c='{text[i]}'");
                char c = text[i];
                if (c == '\'') { i = SkipString(text, i); continue; }
                if (c == '/')
                {
                    if (i + 1 < n && text[i + 1] == '*') { i = SkipBlockComment(text, i); continue; }
                    i++; continue;
                }
                if (c == 'D' && MatchWord(text, i, "DATA") && AtLineStart(text, i)) { i += 4; continue; }
                if (c == 'H' && MatchWord(text, i, "HEADER") && AtLineStart(text, i)) { inHeader = true; i += 6; continue; }
                if (c == 'E' && (MatchWord(text, i, "ENDSEC") || MatchWord(text, i, "END-ISO-10303-21")) && AtLineStart(text, i))
                {
                    inHeader = false;
                    while (i < n && text[i] != ';') i++;
                    i++; continue;
                }
                if (c == '#' && !inHeader)
                {
                    // 解析 "#id ="
                    int j = i + 1;
                    while (j < n && text[j] >= '0' && text[j] <= '9') j++;
                    if (j == i + 1) { i++; continue; }
                    int id = int.Parse(text.Substring(i + 1, j - i - 1), CultureInfo.InvariantCulture);
                    while (j < n && (text[j] == ' ' || text[j] == '\r' || text[j] == '\n' || text[j] == '\t')) j++;
                    if (j >= n || text[j] != '=') { i = j; continue; }
                    j++;
                    while (j < n && char.IsWhiteSpace(text[j])) j++;

                    // 实体类型 + 参数，直到顶层 ';'
                    int start = j, depth = 0;
                    bool inStr = false;
                    int typeEnd = -1;
                    string type = null;

                    while (j < n)
                    {
                        char ch = text[j];
                        if (inStr) { if (ch == '\'') { if (j + 1 < n && text[j + 1] == '\'') { j += 2; continue; } inStr = false; } j++; continue; }
                        if (ch == '\'') { inStr = true; j++; continue; }
                        if (ch == '/' && j + 1 < n && text[j + 1] == '*') { j = SkipBlockComment(text, j); continue; }
                        if (ch == '(')
                        {
                            if (type == null && depth == 0) { type = text.Substring(start, j - start).Trim(); typeEnd = j; }
                            depth++;
                        }
                        else if (ch == ')') depth--;
                        else if (ch == ';' && depth == 0) break;
                        j++;
                    }

                    var ent = new StepEntity { Id = id };
                    if (string.IsNullOrEmpty(type))
                    {
                        // complex entity: #n=(A(...)B(...)) 或 (A B C(...))
                        string raw = text.Substring(start, Math.Max(0, j - start)).Trim();
                        if (raw.StartsWith("(")) raw = raw.Substring(1, raw.Length - 2).TrimEnd();
                        if (raw.EndsWith(")")) raw = raw.Substring(0, raw.Length - 1).TrimEnd();
                        var cargs = ParseArgs(raw);
                        var names = new List<string>();
                        CollectTypes(cargs, names);
                        ent.Type = "|" + string.Join("|", names) + "|";
                        ent.RawArgs = raw;
                    }
                    else
                    {
                        ent.Type = type.Trim().ToUpperInvariant();
                        int aStart = typeEnd + 1;
                        // 约定：RawArgs 不含外层括号
                        ent.RawArgs = text.Substring(aStart, Math.Max(0, j - aStart));
                    }

                    file.Entities[id] = ent;
                    i = j + 1;
                    continue;
                }
                i++;
            }
            return file;
        }

        static bool AtLineStart(string t, int i) => i == 0 || t[i - 1] == '\n' || t[i - 1] == '\r';
        static bool MatchWord(string t, int i, string w) =>
            i + w.Length <= t.Length && string.CompareOrdinal(t, i, w, 0, w.Length) == 0;

        static void CollectTypes(Arg a, List<string> names)
        {
            if (a == null) return;
            if (a.Kind == ArgKind.Typed) { names.Add(a.Type); foreach (var it in a.Items) CollectTypes(it, names); }
            else if (a.Kind == ArgKind.List) foreach (var it in a.Items) CollectTypes(it, names);
        }

        static int SkipString(string t, int i)
        {
            i++;
            while (i < t.Length)
            {
                if (t[i] == '\'') { if (i + 1 < t.Length && t[i + 1] == '\'') { i += 2; continue; } return i + 1; }
                i++;
            }
            return i;
        }

        static int SkipBlockComment(string t, int i)
        {
            int e = t.IndexOf("*/", i + 2, StringComparison.Ordinal);
            return e < 0 ? t.Length : e + 2;
        }

        // ---- 参数串 → AST ----
        // 约定：raw 不含最外层括号，如 "a, b, TYPE(...), (1,2)"
        public static Arg ParseArgs(string raw)
        {
            var items = new List<Arg>();
            int i = 0;
            while (i < raw.Length)
            {
                SkipWs(raw, ref i);
                if (i >= raw.Length) break;
                items.Add(ParseValue(raw, ref i));
                SkipWs(raw, ref i);
                if (i < raw.Length && raw[i] == ',') i++;
            }
            return new Arg { Kind = ArgKind.List, Items = items };
        }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') i++;
                else if (c == '/' && i + 1 < s.Length && s[i + 1] == '*') { i = SkipBlockComment(s, i); }
                else break;
            }
        }

        static Arg ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            char c = s[i];
            switch (c)
            {
                case '(': { var a = new Arg { Kind = ArgKind.List }; i++; while (i < s.Length) { SkipWs(s, ref i); if (i < s.Length && s[i] == ')') { i++; break; } a.Items.Add(ParseValue(s, ref i)); SkipWs(s, ref i); if (i < s.Length && s[i] == ',') i++; } return a; }
                case '\'': { var a = new Arg { Kind = ArgKind.Str }; i++; var sb = new System.Text.StringBuilder(); while (i < s.Length) { if (s[i] == '\'') { if (i + 1 < s.Length && s[i + 1] == '\'') { sb.Append('\''); i += 2; continue; } i++; break; } sb.Append(s[i]); i++; } a.Str = sb.ToString(); return a; }
                case '#': { i++; int j = i; while (j < s.Length && s[j] >= '0' && s[j] <= '9') j++; int id = int.Parse(s.Substring(i, j - i), CultureInfo.InvariantCulture); i = j; return new Arg { Kind = ArgKind.Ref, Ref = id }; }
                case '$': i++; return Arg.Null;
                case '*': i++; return new Arg { Kind = ArgKind.Star };
                case '.': { i++; int j = s.IndexOf('.', i); if (j < 0) { return new Arg { Kind = ArgKind.Str, Str = s.Substring(i) }; } var a = new Arg { Kind = ArgKind.Enum, Str = s.Substring(i, j - i).ToUpperInvariant() }; i = j + 1; return a; }
            }
            if (char.IsDigit(c) || c == '-' || c == '+')
            {
                int j = i + 1;
                while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '.' || s[j] == 'e' || s[j] == 'E' || s[j] == '-' || s[j] == '+')) j++;
                var a = new Arg { Kind = ArgKind.Num, Num = double.Parse(s.Substring(i, j - i), CultureInfo.InvariantCulture) };
                i = j;
                return a;
            }
            // Typed: NAME( ... )
            int k = i;
            while (k < s.Length && (char.IsLetterOrDigit(s[k]) || s[k] == '_' || s[k] == '!')) k++;
            string type = s.Substring(i, k - i).Trim();
            if (k == i) { i++; return new Arg { Kind = ArgKind.Str, Str = type }; } // 防御：未知 token 强制前进
            i = k;
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '(')
            {
                int depth = 0, start = i;
                bool inStr = false;
                for (; i < s.Length; i++)
                {
                    char ch = s[i];
                    if (inStr) { if (ch == '\'') { if (i + 1 < s.Length && s[i + 1] == '\'') { i++; continue; } inStr = false; } continue; }
                    if (ch == '\'') inStr = true;
                    else if (ch == '(') depth++;
                    else if (ch == ')') { depth--; if (depth == 0) { i++; break; } }
                }
                int innerEnd = Math.Min(i, s.Length);
                int innerLen = innerEnd - start - 2;
                string inner = innerLen > 0 ? s.Substring(start + 1, innerLen) : "";
                return new Arg { Kind = ArgKind.Typed, Type = type.ToUpperInvariant(), Items = ParseArgs(inner).Items };
            }
            return new Arg { Kind = ArgKind.Str, Str = type };
        }

        // 常用取值助手
        public static int AsRef(Arg a) => a.Kind == ArgKind.Ref ? a.Ref : -1;
        public static double AsNum(Arg a) => a.Kind == ArgKind.Num ? a.Num : 0.0;
        public static bool AsBool(Arg a) => a.Kind == ArgKind.Enum && a.Str == "T";
        public static Arg RefArg(Arg a, StepFile f) => a.Kind == ArgKind.Ref ? f.Get(a.Ref)?.Args : null;
    }
}
