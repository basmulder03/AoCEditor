#!/usr/bin/env bash
# Greps for project rules the compiler cannot enforce. Exit 1 on findings.
cd "$(dirname "$0")/.." || exit 2
root=${CHECK_ROOT:-src}
fail=0
check() { # description, pattern, extra grep args...
  local d=$1 p=$2; shift 2
  local out; out=$(grep -rnE "$p" "$root" --include='*.cs' --include='*.axaml' "$@" 2>/dev/null)
  [ -n "$out" ] && { echo "## $d"; echo "$out"; fail=1; }
}
check "HttpClient outside the AoC gateway" 'new HttpClient\(' --exclude-dir=Gateway
check "AoC URL outside the AoC gateway" 'adventofcode\.com' --exclude-dir=Gateway
check "Blocking on tasks" '\.Result\b|\.Wait\(\)|GetAwaiter\(\)\.GetResult\(\)'
out=$(grep -rnE 'async void' "$root" --include='*.cs' | grep -v 'EventArgs')
[ -n "$out" ] && { echo "## async void (event handlers only)"; echo "$out"; fail=1; }
check "Reflection that breaks AoT" 'Activator\.CreateInstance|Assembly\.Load|Type\.GetType\(|MakeGenericType|JsonSerializer\.(De)?[Ss]erialize<|ReflectionBinding'
check "ADR or issue references in comments" '//.*(ADR[ -]?[0-9]+|#[0-9]{1,4}\b)'
check "Swallowed exceptions" 'catch\s*(\([^)]*\))?\s*\{\s*\}'
check "Logging calls or log parameters named like secrets" '\b_?(log|logger)\.[A-Za-z]+\(.*(token|cookie|secret|password|session)|partial void [A-Za-z]+\(.*(token|cookie|secret|password|session)' -i
check "Optional logger or NullLogger fallback (take a required ILogger<T>)" 'ILogger(<[^>]*>)?\? |NullLogger(<[^>]*>)?\.Instance'
# One top-level type per file, named after the file. Top level = least-indented declaration in the file.
mods='((public|internal|private|protected|static|sealed|abstract|partial|readonly|unsafe|file|ref|new)[ \t]+)*'
out=$(for r in ${CHECK_ROOT:-src tests}; do find "$r" -name '*.cs' ! -name '*.g.cs' ! -name '*.feature.cs' ! -path '*/obj/*' ! -path '*/bin/*' 2>/dev/null; done | sort | while read -r f; do
  awk -v file="$f" -v mods="$mods" '
    function indent(l) { match(l, /^[ \t]*/); return RLENGTH }
    {
      line = $0; name = ""
      if (match(line, "^[ \t]*" mods "(class|struct|interface|enum|record([ \t]+(class|struct))?)[ \t]+[A-Za-z_][A-Za-z_0-9]*")) {
        t = substr(line, RSTART, RLENGTH); sub(/^.*[ \t]/, "", t); name = t
      } else if (match(line, "^[ \t]*" mods "delegate[ \t]+[^(=]*\\(")) {
        t = substr(line, RSTART, RLENGTH - 1); sub(/<[^>]*>[ \t]*$/, "", t); sub(/[ \t]+$/, "", t); sub(/^.*[ \t]/, "", t); name = t
      }
      if (name != "") { n++; nm[n] = name; ind[n] = indent(line); if (min == "" || ind[n] < min) min = ind[n] }
    }
    END {
      base = file; sub(/^.*\//, "", base); sub(/\.cs$/, "", base); sub(/\.axaml$/, "", base)
      for (i = 1; i <= n; i++) if (ind[i] == min && !(nm[i] in seen)) { seen[nm[i]] = 1; names = names (names ? ", " : "") nm[i]; c++ }
      if (c > 1) print file ": " c " top-level types (" names ")"
      else if (c == 1 && names != base) print file ": type " names " does not match the file name"
    }' "$f"
done)
[ -n "$out" ] && { echo "## One top-level type per file, named after the type"; echo "$out"; fail=1; }
[ $fail -eq 0 ] && echo "check-practices: ok"
exit $fail
