"""
RenderDoc Targeted Shader Extractor v4
Fixes:
  - UsedDescriptor: use .descriptor.resourceId (not .resourceId directly)
  - PipeState: no GetConstantBufferId → use GetConstantBuffer or skip
  - Texture fallback: enumerate all resources via controller.GetResources()
"""
import renderdoc as rd
import os

RDC_FILE = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\3.rdc"
OUTPUT_DIR = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\output\targeted_v7"

# Priority targets from 5.rdc list analysis:
# eid=805  (8 textures! highest priority - matches 4.rdc eid_801)
# eid=1181 (4tex+3cb - matches 4.rdc eid_1050, Boost noise layer)
# eid=1252 (5 textures - NEW! not seen before)
# eid=1247 (4tex - Trail main candidate)
# eid=1725 (4tex - Trail main candidate #2)
# eid=21   (6tex - background layers, cross-RDC consistency check)
# eid=877  (3tex+2cb - ship body candidate, first of large cluster)
# eid=1108 (3tex+2cb - VFX candidate)
# eid=1149 (3tex+2cb - VFX candidate)
# eid=1785 (2tex+2cb - new VFX candidate)
TARGET_EVENT_IDS = [
    21,    # 6tex+1cb - background layers (EID identical to 4.rdc/5.rdc!)
    805,   # 8tex+1cb - RT binding pass (matches 4.rdc eid_801, 5.rdc eid_805)
    877,   # 3tex+2cb - ship body (EID identical to 5.rdc!)
    1076,  # 4tex+3cb - Boost noise layer (matches 4.rdc eid_1050, 5.rdc eid_1181)
    1121,  # 4tex+1cb - Trail color blend candidate
    1126,  # 5tex+1cb - double Boost energy layer (matches 5.rdc eid_1252!)
    1598,  # 4tex+1cb - Trail main effect (matches 1.rdc eid_1596, 4.rdc eid_1571, 5.rdc eid_1725!)
    1964,  # 2tex+2cb - global energy field candidate (matches 5.rdc eid_1785?)
    1080,  # 3tex+2cb - Trail particle segment (first of cluster 1080~1087)
]

os.makedirs(OUTPUT_DIR, exist_ok=True)

def flatten_actions(actions):
    result = []
    for a in actions:
        result.append(a)
        if hasattr(a, 'children') and a.children:
            result.extend(flatten_actions(a.children))
    return result

def save_texture(controller, res_id, slot_name, out_dir):
    """Save a texture resource to PNG."""
    try:
        texsave = rd.TextureSave()
        texsave.resourceId = res_id
        texsave.destType = rd.FileType.PNG
        texsave.mip = 0
        texsave.slice.sliceIndex = 0
        out_path = os.path.join(out_dir, f"{slot_name}.png")
        controller.SaveTexture(texsave, out_path)
        if os.path.exists(out_path):
            size = os.path.getsize(out_path)
            return f"OK ({size} bytes)"
        else:
            return "(file not created)"
    except Exception as e:
        return f"(save failed: {e})"

def get_bound_textures(controller, state, lines):
    """
    Get bound PS textures. Returns list of (slot_name, resourceId).
    Tries multiple approaches.
    """
    bound = []

    # --- Approach 1: UsedDescriptor.descriptor.resourceId ---
    try:
        descriptors = state.GetReadOnlyResources(rd.ShaderStage.Pixel)
        lines.append(f"  [A1] GetReadOnlyResources: {len(descriptors)} items\n")
        for i, used in enumerate(descriptors):
            res_id = None
            # Try .descriptor.resourceId (the Descriptor sub-object)
            if hasattr(used, 'descriptor'):
                d = used.descriptor
                d_attrs = [a for a in dir(d) if not a.startswith('_')]
                lines.append(f"    [{i}] descriptor attrs: {d_attrs}\n")
                if hasattr(d, 'resourceId'):
                    res_id = d.resourceId
                elif hasattr(d, 'resource'):
                    res_id = d.resource
                elif hasattr(d, 'view') and hasattr(d.view, 'resourceId'):
                    res_id = d.view.resourceId
            # Also check .access for slot info
            slot_idx = i
            if hasattr(used, 'access') and hasattr(used.access, 'index'):
                slot_idx = used.access.index

            if res_id and res_id != rd.ResourceId.Null():
                slot_name = f"tex_slot{slot_idx}"
                lines.append(f"    [{i}] -> {slot_name} res={res_id}\n")
                bound.append((slot_name, res_id))
        if bound:
            return bound
    except Exception as e:
        lines.append(f"  [A1] failed: {e}\n")

    # --- Approach 2: Match by resource name from reflection ---
    # Get all resources in the capture, find ones matching shader texture names
    try:
        refl = state.GetShaderReflection(rd.ShaderStage.Pixel)
        if refl and refl.readOnlyResources:
            tex_names = [r.name for r in refl.readOnlyResources]
            lines.append(f"  [A2] Looking for textures by name: {tex_names}\n")

            all_resources = controller.GetResources()
            lines.append(f"  [A2] Total resources in capture: {len(all_resources)}\n")

            # Build name->resourceId map
            name_map = {}
            for res in all_resources:
                name_map[res.name] = res.resourceId

            for tex_name in tex_names:
                if tex_name in name_map:
                    res_id = name_map[tex_name]
                    lines.append(f"  [A2] Found '{tex_name}' -> {res_id}\n")
                    bound.append((tex_name, res_id))
                else:
                    lines.append(f"  [A2] NOT FOUND: '{tex_name}'\n")
            if bound:
                return bound
    except Exception as e:
        lines.append(f"  [A2] failed: {e}\n")

    # --- Approach 3: Dump all resource names for debugging ---
    try:
        all_resources = controller.GetResources()
        lines.append(f"  [A3-DEBUG] All resource names (first 50):\n")
        for res in all_resources[:50]:
            lines.append(f"    '{res.name}' id={res.resourceId} type={res.type}\n")
    except Exception as e:
        lines.append(f"  [A3] failed: {e}\n")

    return bound

def read_cbuffer_values(controller, state, lines):
    """Try to read actual cbuffer values at runtime."""
    try:
        refl = state.GetShaderReflection(rd.ShaderStage.Pixel)
        if not refl or not refl.constantBlocks:
            lines.append("  (no constant blocks)\n")
            return

        pipe_obj = state.GetGraphicsPipelineObject()
        shader_id = state.GetShader(rd.ShaderStage.Pixel)

        for cb_idx, cb in enumerate(refl.constantBlocks):
            lines.append(f"  CB[{cb_idx}] '{cb.name}':\n")

            # Try GetCBufferVariableContents with different signatures
            try:
                # Newer API: needs (pipeline, shader, stage, entryPoint, cbIndex, bufferId, offset, length)
                # First get the buffer resourceId via GetConstantBuffer
                cb_res = state.GetConstantBuffer(rd.ShaderStage.Pixel, cb_idx, 0)
                buf_id = cb_res.resourceId if hasattr(cb_res, 'resourceId') else rd.ResourceId.Null()
                lines.append(f"    buffer resourceId={buf_id}\n")

                vars_data = controller.GetCBufferVariableContents(
                    pipe_obj, shader_id, rd.ShaderStage.Pixel,
                    refl.entryPoint, cb_idx, buf_id, 0, 0
                )
                for v in vars_data[:20]:
                    val = getattr(v, 'value', None)
                    lines.append(f"    {v.name} = {val}\n")
            except Exception as e1:
                lines.append(f"    (GetCBufferVariableContents failed: {e1})\n")
                # Fallback: just show reflection info
                for v in cb.variables:
                    lines.append(f"    {v.name}: {v.type.name} @offset={v.byteOffset}\n")
    except Exception as e:
        lines.append(f"  [CBuffer] failed: {e}\n")

def extract_draw(controller, draw, out_dir):
    eid = draw.eventId
    draw_dir = os.path.join(out_dir, f"eid_{eid}")
    os.makedirs(draw_dir, exist_ok=True)

    lines = [f"=== EventId {eid} ===\n"]

    try:
        controller.SetFrameEvent(eid, True)
        state = controller.GetPipelineState()

        # --- Pixel Shader ---
        ps_id = state.GetShader(rd.ShaderStage.Pixel)
        if ps_id == rd.ResourceId.Null():
            lines.append("[PS] No pixel shader\n")
        else:
            lines.append(f"[PS] id={ps_id}\n")
            refl = state.GetShaderReflection(rd.ShaderStage.Pixel)

            if refl:
                lines.append(f"[PS] Entry: {refl.entryPoint}\n")
                lines.append(f"[PS] CBuffers ({len(refl.constantBlocks)}):\n")
                for cb in refl.constantBlocks:
                    lines.append(f"  CB '{cb.name}' bind={cb.fixedBindNumber} ({len(cb.variables)} vars)\n")
                lines.append(f"[PS] Textures ({len(refl.readOnlyResources)}):\n")
                for r in refl.readOnlyResources:
                    lines.append(f"  '{r.name}' bind={r.fixedBindNumber}\n")

            # Disassembly
            try:
                disasm = controller.DisassembleShader(ps_id, refl, "")
                if disasm:
                    with open(os.path.join(draw_dir, "ps_disasm.txt"), "w", encoding="utf-8") as f:
                        f.write(disasm)
                    lines.append(f"[PS] Disasm saved ({len(disasm)} chars)\n")
            except Exception as e:
                lines.append(f"[PS] Disasm error: {e}\n")

        # --- Textures ---
        lines.append("\n[Textures]\n")
        bound_textures = get_bound_textures(controller, state, lines)
        for slot_name, res_id in bound_textures:
            result = save_texture(controller, res_id, slot_name, draw_dir)
            lines.append(f"  SAVED {slot_name}: {result}\n")
        if not bound_textures:
            lines.append("  (no textures extracted)\n")

        # --- CBuffer Values ---
        lines.append("\n[CBuffer Values]\n")
        read_cbuffer_values(controller, state, lines)

    except Exception as e:
        lines.append(f"[FATAL] {e}\n")
        import traceback
        lines.append(traceback.format_exc())

    with open(os.path.join(draw_dir, "report.txt"), "w", encoding="utf-8") as f:
        f.writelines(lines)

    return lines

def main():
    print(f"Opening: {RDC_FILE}")
    cap = rd.OpenCaptureFile()
    result = cap.OpenFile(RDC_FILE, "", None)
    if result != rd.ResultCode.Succeeded:
        print(f"[ERROR] Open failed: {result}")
        return

    result, controller = cap.OpenCapture(rd.ReplayOptions(), None)
    if result != rd.ResultCode.Succeeded:
        print(f"[ERROR] Replay failed: {result}")
        cap.Shutdown()
        return

    print("[OK] Controller ready")

    if hasattr(controller, 'GetRootActions'):
        root = controller.GetRootActions()
    else:
        root = controller.GetDrawcalls()

    all_actions = flatten_actions(root)
    eid_map = {a.eventId: a for a in all_actions}
    print(f"[INFO] Total actions: {len(all_actions)}")

    # List mode: if TARGET_EVENT_IDS is empty, just list all draw calls (no extraction)
    target_ids = list(TARGET_EVENT_IDS)
    if not target_ids:
        print("[LIST MODE] TARGET_EVENT_IDS is empty, listing all draw calls with PS...")
        list_lines = []
        for a in all_actions:
            if not (a.flags & rd.ActionFlags.Drawcall):
                continue
            try:
                controller.SetFrameEvent(a.eventId, True)
                state = controller.GetPipelineState()
                ps_id = state.GetShader(rd.ShaderStage.Pixel)
                if ps_id == rd.ResourceId.Null():
                    continue
                refl = state.GetShaderReflection(rd.ShaderStage.Pixel)
                tex_count = len(refl.readOnlyResources) if refl else 0
                cb_count = len(refl.constantBlocks) if refl else 0
                entry = refl.entryPoint if refl else "?"
                line = f"eid={a.eventId:5d}  textures={tex_count}  cbuffers={cb_count}  entry={entry}"
                print(line)
                list_lines.append(line + "\n")
            except Exception as e:
                list_lines.append(f"eid={a.eventId:5d}  ERROR: {e}\n")

        list_path = os.path.join(OUTPUT_DIR, "all_drawcalls.txt")
        os.makedirs(OUTPUT_DIR, exist_ok=True)
        with open(list_path, "w", encoding="utf-8") as f:
            f.writelines(list_lines)
        print(f"\n[DONE] List saved to: {list_path}")
        print("Set TARGET_EVENT_IDS to the EIDs you want to extract, then re-run.")
        controller.Shutdown()
        cap.Shutdown()
        return

    for eid in target_ids:
        if eid in eid_map:
            print(f"  Extracting eid={eid} ...")
            lines = extract_draw(controller, eid_map[eid], OUTPUT_DIR)
            for l in lines[:8]:
                print("   ", l.rstrip())
        else:
            print(f"  [SKIP] eid={eid} not found")

    print(f"\n[DONE] Output: {OUTPUT_DIR}")
    controller.Shutdown()
    cap.Shutdown()

main()
