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
[ $fail -eq 0 ] && echo "check-practices: ok"
exit $fail
