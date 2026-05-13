# GGReplica Shader Spike Report

> Goal: determine whether selected DevXUnity shaders can be directly or partially used by Project Ark GGReplica.  
> Date: 2026-05-13 15:36  
> Status: Complete — all three imported DevX shader trial files are L0 reference-only.

## 1. Grades

| Grade | Meaning | Decision |
|---|---|---|
| L0 | Imports but does not compile | Reference only |
| L1 | Compiles but visual output is clearly wrong | Rebuild in Project Ark shader |
| L2 | Main visual works, minor parameters broken | Partial adoption |
| L3 | Works almost directly | Adopt under Project Ark naming and ownership |

## 2. Candidates

| Shader | Path | Compile result | Visual result | Grade | Decision |
|---|---|---|---|---|---|
| PlayerShipHighlight | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/CLG_PlayerShipHighlight.shader` | FAIL: parse error at line 36; source contains DevX `!!! Allow restore shader as UnityLab format` marker and disassembled `Program` blocks | Not rendered | L0 | Reference only; rebuild additive highlight shader with `_Tint`, `_Intensity`, `_Smooth`, `Blend SrcAlpha One` |
| Lit_PlayerLightSourceColored | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/Lit_PlayerLightSourceColored.shader` | FAIL: parse error at line 53; source contains DevX restore marker in multiple passes | Not rendered | L0 | Reference only; reuse property intent (`_Tint_Color`, `_Intensity`, normal/height fields) but rebuild in Project Ark URP 2D shader |
| PlayerLQTrail | `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/PlayerLQTrail.shader` | FAIL: parse error at line 35; source contains DevX restore marker in multiple passes | Not rendered | L0 | Reference only; rebuild boost trail shader using exposed color/noise intent rather than direct adoption |

## 3. Evidence

Unity Console after importing curated shader trial files reported three shader parse errors:

```text
Shader error in '': Parse error: syntax error, unexpected $undefined, expecting TOK_SETTEXTURE or '}' at line 36
Shader error in '': Parse error: syntax error, unexpected $undefined, expecting TOK_SETTEXTURE or '}' at line 35
Shader error in '': Parse error: syntax error, unexpected $undefined, expecting TOK_SETTEXTURE or '}' at line 53
```

Source inspection links those line numbers to invalid DevX restore markers:

```text
CLG_PlayerShipHighlight.shader:36      !!! Allow restore shader as UnityLab format ...
PlayerLQTrail.shader:35                !!! Allow restore shader as UnityLab format ...
Lit_PlayerLightSourceColored.shader:53 !!! Allow restore shader as UnityLab format ...
```

Because the shaders fail at parse stage, no trial materials were created in `Assets/_Art/Ship/GGReplica/Materials/DevX_Trial/` for this pass.

## 4. Final decision

- Highlight layer shader: L0, reference only.
- Liquid layer shader: L0, reference only.
- Boost trail shader: L0, reference only.
- Fallback implementation needed: yes.

## 5. Follow-up for GGReplica

- Keep imported DevX shader files under `Assets/_Art/Ship/GGReplica/Shaders/DevX_Trial/` as reference artifacts only.
- Do not assign these shaders to `Ship_GGReplica.prefab` materials.
- Build Project Ark-owned replacement shaders under `Assets/_Art/Ship/GGReplica/Shaders/ProjectArk_Rebuilt/` if the replica needs additive highlight, liquid tint, or LQ trail effects.
- Candidate rebuild properties to preserve:
  - Highlight: `_Tint`, `_Intensity`, `_Smooth`, additive blend.
  - Liquid/tint: `_Tint_Color`, `_Intensity`, optional normal/height response.
  - Trail: main color, edge color, noise tiling/mutation/smooth edge.
