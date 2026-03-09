"""
RenderDoc Diagnose Script - List ALL Draw Calls
Usage: paste in RenderDoc Python Shell
"""
import renderdoc as rd
import os

RDC_FILE = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\1.rdc"
OUTPUT_DIR = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\output"
os.makedirs(OUTPUT_DIR, exist_ok=True)

def flatten_actions(actions):
    result = []
    for a in actions:
        result.append(a)
        if hasattr(a, 'children') and a.children:
            result.extend(flatten_actions(a.children))
    return result

def get_draw_flag():
    if hasattr(rd, 'ActionFlags'):
        f = rd.ActionFlags
        return getattr(f, 'Drawcall', getattr(f, 'Draw', None))
    else:
        f = rd.DrawFlags
        return getattr(f, 'Drawcall', getattr(f, 'Draw', None))

def main():
    print("Opening capture...")
    cap = rd.OpenCaptureFile()
    result = cap.OpenFile(RDC_FILE, "", None)
    if result != rd.ResultCode.Succeeded:
        print(f"[ERROR] {result}")
        return

    result, controller = cap.OpenCapture(rd.ReplayOptions(), None)
    if result != rd.ResultCode.Succeeded:
        print(f"[ERROR] {result}")
        cap.Shutdown()
        return

    print("[OK] Controller ready")

    # 获取所有 actions
    if hasattr(controller, 'GetRootActions'):
        root = controller.GetRootActions()
    else:
        root = controller.GetDrawcalls()

    all_actions = flatten_actions(root)
    draw_flag_bit = get_draw_flag()
    print(f"[INFO] Total actions: {len(all_actions)}")

    # 过滤出真正的 Draw Call
    draws = []
    for a in all_actions:
        if draw_flag_bit is not None:
            if a.flags & draw_flag_bit:
                draws.append(a)
        else:
            # 无法判断，全部加入
            draws.append(a)

    print(f"[INFO] Draw calls: {len(draws)}")

    # 输出到文件
    out_path = os.path.join(OUTPUT_DIR, "all_drawcalls.txt")
    with open(out_path, "w", encoding="utf-8") as f:
        f.write(f"Total actions: {len(all_actions)}\n")
        f.write(f"Draw calls: {len(draws)}\n\n")
        f.write(f"{'Idx':>5} {'EventId':>8} {'Name':<40} {'Indices':>8} {'Verts':>8}  PS_CBuffers\n")
        f.write("-" * 120 + "\n")

        for idx, draw in enumerate(draws):
            # 移动到该 draw
            try:
                controller.SetFrameEvent(draw.eventId, True)
                state = controller.GetPipelineState()
                ps = state.GetShader(rd.ShaderStage.Pixel)

                cb_info = ""
                if ps != rd.ResourceId.Null():
                    refl = state.GetShaderReflection(rd.ShaderStage.Pixel)
                    if refl:
                        cb_names = []
                        for cb in refl.constantBlocks:
                            var_names = [v.name for v in cb.variables]
                            cb_names.append(f"{cb.name}[{','.join(var_names[:5])}{'...' if len(var_names)>5 else ''}]")
                        cb_info = " | ".join(cb_names)
                    else:
                        cb_info = "(no reflection)"
                else:
                    cb_info = "(no PS)"

                name = draw.name if hasattr(draw, 'name') else str(draw.eventId)
                num_indices = draw.numIndices if hasattr(draw, 'numIndices') else 0
                num_verts = draw.numIndices if hasattr(draw, 'numIndices') else 0

                line = f"{idx:>5} {draw.eventId:>8} {name:<40} {num_indices:>8} {num_verts:>8}  {cb_info}"
                f.write(line + "\n")

                # 每50个打印一次进度
                if idx % 50 == 0:
                    print(f"  Progress: {idx}/{len(draws)} ...")

            except Exception as e:
                f.write(f"{idx:>5} {draw.eventId:>8} {'(error)':<40}  ERROR: {e}\n")

    print(f"\n[DONE] Saved to: {out_path}")
    print(f"[DONE] Total draw calls written: {len(draws)}")

    controller.Shutdown()
    cap.Shutdown()

main()
