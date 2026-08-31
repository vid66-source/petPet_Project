#!/usr/bin/env bash
# Low-token setup/config verification for petpet-mentor lesson reviews.
# Checks file/folder/config EXISTENCE only (compile correctness still needs
# the Unity Console / the student's own report — this is a cheap pre-check).
#
# Usage: bash Docs/tools/verify_lesson.sh <lesson-number, e.g. 00 or 01>
#
# When writing a new lesson README, add its case block here at the same time
# (same PR/commit), listing the concrete files/config lines that lesson
# introduces. Keeps future reviews a single cheap call instead of ad-hoc
# Read/find/grep exploration.

set -uo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PASS=0; FAIL=0
ok()    { echo "OK   $1"; PASS=$((PASS+1)); }
bad()   { echo "FAIL $1"; FAIL=$((FAIL+1)); }
check() { eval "$2" >/dev/null 2>&1 && ok "$1" || bad "$1"; }
has_file() { find "$1" -iname "$2" 2>/dev/null | grep -q .; }

lesson="${1:-}"

case "$lesson" in
  00)
    check "cinemachine package"            "grep -q com.unity.cinemachine '$ROOT/Packages/manifest.json'"
    check "input system package"           "grep -q com.unity.inputsystem '$ROOT/Packages/manifest.json'"
    check "active input = New only"        "grep -q 'activeInputHandler: 1' '$ROOT/ProjectSettings/ProjectSettings.asset'"
    check "Bootstrap.unity exists"         "[ -f '$ROOT/Assets/Scenes/Bootstrap.unity' ]"
    check "Level_Arena.unity exists"       "[ -f '$ROOT/Assets/Scenes/Level_Arena.unity' ]"
    check "no leftover SampleScene"        "[ ! -f '$ROOT/Assets/Scenes/SampleScene.unity' ]"
    check "build settings: has Bootstrap"    "grep -q 'Bootstrap.unity' '$ROOT/ProjectSettings/EditorBuildSettings.asset'"
    check "build settings: has Level_Arena"  "grep -q 'Level_Arena.unity' '$ROOT/ProjectSettings/EditorBuildSettings.asset'"
    check "Bootstrap is scene index 0" \
      "[ \"\$(grep -n '\.unity' '$ROOT/ProjectSettings/EditorBuildSettings.asset' | grep -m1 Bootstrap | cut -d: -f1)\" -lt \"\$(grep -n '\.unity' '$ROOT/ProjectSettings/EditorBuildSettings.asset' | grep -m1 Level_Arena | cut -d: -f1)\" ]"
    check "CodeBase/Infrastructure/AssetManagement (spelling)" "[ -d '$ROOT/Assets/CodeBase/Infrastructure/AssetManagement' ]"
    check "CodeBase/Services/PersistentProgress (spelling)"    "[ -d '$ROOT/Assets/CodeBase/Services/PersistentProgress' ]"
    ;;
  01)
    for f in IState.cs GameStateMachine.cs ICoroutineRunner.cs SceneLoader.cs Game.cs \
             GameBootstrapper.cs BootstrapState.cs LoadLevelState.cs GameLoopState.cs LoadingCurtain.cs; do
      check "$f exists somewhere in CodeBase" "has_file '$ROOT/Assets/CodeBase' '$f'"
    done
    check "no FindObjectOfType in Infrastructure" "! grep -rq 'FindObjectOfType' '$ROOT/Assets/CodeBase/Infrastructure'"
    check "no public static fields in Infrastructure" "! grep -rq 'public static' '$ROOT/Assets/CodeBase/Infrastructure'"
    ;;
  *)
    echo "No checks defined for lesson '$lesson' yet."
    echo "Add a case block above when Docs/Lessons/${lesson}_*.md is written."
    exit 2
    ;;
esac

echo "----"
echo "$PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ]
