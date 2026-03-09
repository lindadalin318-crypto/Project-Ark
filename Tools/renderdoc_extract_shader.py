"""
RenderDoc Python API - Boost Trail Shader Extractor
用法：在 RenderDoc 的 Python Shell 中粘贴并运行此脚本
Window -> Python Shell -> 粘贴 -> 回车
"""

import renderdoc as rd
import os

# ============================================================
# 配置区：修改这里
# ============================================================
RDC_FILE = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\1.rdc"   # 你的 .rdc 文件路径
OUTPUT_DIR = r"F:\UnityProjects\ReferenceAssets\GGrenderdoc\output"  # 输出目录
# ============================================================

os.makedirs(OUTPUT_DIR, exist_ok=True)

def save_shader(shader_reflection, stage_name, draw_index):
    """保存 Shader 反汇编代码"""
    if shader_reflection is None:
        return
    output_path = os.path.join(OUTPUT_DIR, f"draw_{draw_index:04d}_{stage_name}.hlsl")
    with open(output_path, "w", encoding="utf-8") as f:
        # 写入 Shader 基本信息
        f.write(f"// Draw Call Index: {draw_index}\n")
        f.write(f"// Stage: {stage_name}\n")
        f.write(f"// Entry Point: {shader_reflection.entryPoint}\n\n")
        
        # 写入 Constant Buffers（包含所有参数）
        f.write("// ===== Constant Buffers (Parameters) =====\n")
        for cb in shader_reflection.constantBlocks:
            f.write(f"// cbuffer {cb.name} : register(b{cb.bindPoint})\n")
            for var in cb.variables:
                f.write(f"//   {var.type.descriptor.name} {var.name}\n")
        f.write("\n")
        
        # 写入纹理输入
        f.write("// ===== Texture Inputs =====\n")
        for res in shader_reflection.readOnlyResources:
            f.write(f"// Texture2D {res.name} : register(t{res.bindPoint})\n")
        f.write("\n")
        
        # 写入反汇编代码
        f.write("// ===== Disassembly =====\n")
        if hasattr(shader_reflection, 'rawBytes') and shader_reflection.rawBytes:
            f.write("// (Raw bytes available - use external decompiler)\n")
        f.write("// See HLSL disassembly below via GetDisassemblyText\n")
    
    print(f"  [OK] Saved: {output_path}")
    return output_path

def save_texture(controller, resource_id, name, draw_index, slot):
    """保存纹理为 PNG"""
    try:
        tex_data = controller.GetTextureData(resource_id, rd.Subresource(0, 0, 0))
        tex_info = controller.GetTexture(resource_id)
        
        output_path = os.path.join(OUTPUT_DIR, f"draw_{draw_index:04d}_tex_slot{slot}_{name}.png")
        
        # 使用 RenderDoc 内置保存
        save_data = rd.TextureSave()
        save_data.resourceId = resource_id
        save_data.destType = rd.FileType.PNG
        save_data.mip = 0
        save_data.slice.sliceIndex = 0
        controller.SaveTexture(save_data, output_path)
        
        print(f"  [OK] Texture saved: {output_path} ({tex_info.width}x{tex_info.height})")
    except Exception as e:
        print(f"  [WARN] Could not save texture {name}: {e}")

def is_boost_trail_draw(draw, controller):
    """
    判断一个 Draw Call 是否是 Boost Trail 相关的渲染
    策略：检查是否使用了半透明混合（Additive），且是粒子/Trail 渲染
    """
    try:
        state = controller.GetPipelineState()
        
        # 检查混合状态（Additive 混合是特效的标志）
        blend_state = state.GetFramebuffer()
        
        # 检查顶点数量（Trail 通常顶点数适中，不是 UI 也不是大地形）
        if draw.numIndices > 0 and draw.numIndices < 10000:
            return True
    except:
        pass
    return False

def extract_shader_disassembly(controller, draw_index):
    """提取当前 Draw Call 的 Shader 反汇编"""
    try:
        state = controller.GetPipelineState()
        
        # 获取 Pixel Shader
        ps = state.GetShader(rd.ShaderStage.Pixel)
        ps_reflection = state.GetShaderReflection(rd.ShaderStage.Pixel)
        ps_entry = state.GetShaderEntryPoint(rd.ShaderStage.Pixel)
        
        if ps == rd.ResourceId.Null():
            return None
        
        # 获取反汇编文本
        disasm = controller.DisassembleShader(ps, ps_reflection, "")
        
        output_path = os.path.join(OUTPUT_DIR, f"draw_{draw_index:04d}_PS_disasm.hlsl")
        with open(output_path, "w", encoding="utf-8") as f:
            f.write(f"// ===== Draw Call {draw_index} - Pixel Shader Disassembly =====\n\n")
            
            # Constant Buffers
            if ps_reflection:
                f.write("// ===== Constant Buffers =====\n")
                for cb in ps_reflection.constantBlocks:
                    f.write(f"cbuffer {cb.name} : register(b{cb.bindPoint}) {{\n")
                    for var in cb.variables:
                        f.write(f"    {var.type.descriptor.name} {var.name};\n")
                    f.write("}\n\n")
                
                f.write("// ===== Texture Inputs =====\n")
                for res in ps_reflection.readOnlyResources:
                    f.write(f"Texture2D {res.name} : register(t{res.bindPoint});\n")
                f.write("\n")
            
            f.write("// ===== Disassembly =====\n")
            if disasm:
                f.write(disasm)
            else:
                f.write("// No disassembly available\n")
        
        print(f"  [OK] PS Disassembly saved: {output_path}")
        return output_path
        
    except Exception as e:
        print(f"  [ERROR] Failed to extract shader: {e}")
        return None

def extract_textures_for_draw(controller, draw_index):
    """提取当前 Draw Call 绑定的所有纹理"""
    try:
        state = controller.GetPipelineState()
        
        # 获取 PS 绑定的纹理
        ps_resources = state.GetReadOnlyResources(rd.ShaderStage.Pixel)
        
        saved_count = 0
        for binding in ps_resources:
            for i, res in enumerate(binding.resources):
                if res.resourceId != rd.ResourceId.Null():
                    tex_info = controller.GetTexture(res.resourceId)
                    if tex_info is not None:
                        name = f"slot{i}_{tex_info.width}x{tex_info.height}"
                        save_texture(controller, res.resourceId, name, draw_index, i)
                        saved_count += 1
        
        return saved_count
    except Exception as e:
        print(f"  [ERROR] Failed to extract textures: {e}")
        return 0

def main():
    print("=" * 60)
    print("RenderDoc Boost Trail Shader Extractor")
    print("=" * 60)
    print(f"RDC File: {RDC_FILE}")
    print(f"Output Dir: {OUTPUT_DIR}")
    print()
    
    # 打开 .rdc 文件
    cap = rd.OpenCaptureFile()
    result = cap.OpenFile(RDC_FILE, "", None)
    
    if result != rd.ResultCode.Succeeded:
        print(f"[ERROR] Failed to open capture file: {result}")
        return
    
    print("[OK] Capture file opened successfully")
    
    # 创建 Replay Controller
    result, controller = cap.OpenCapture(rd.ReplayOptions(), None)
    
    if result != rd.ResultCode.Succeeded:
        print(f"[ERROR] Failed to create replay controller: {result}")
        cap.Shutdown()
        return
    
    print("[OK] Replay controller created")
    
    # 获取所有 Draw Calls（新版 API 用 GetRootActions，旧版用 GetDrawcalls）
    if hasattr(controller, 'GetRootActions'):
        draw_calls = controller.GetRootActions()
    else:
        draw_calls = controller.GetDrawcalls()
    
    # 展平所有嵌套的 action（RenderDoc 新版是树形结构）
    def flatten_actions(actions):
        result = []
        for a in actions:
            result.append(a)
            if hasattr(a, 'children') and a.children:
                result.extend(flatten_actions(a.children))
        return result
    
    draw_calls = flatten_actions(draw_calls)
    print(f"[INFO] Total draw calls: {len(draw_calls)}")
    print()
    
    # 策略：提取所有半透明 Draw Call（特效通常是半透明的）
    # 我们提取后半段的 Draw Call（特效通常在不透明物体之后渲染）
    total = len(draw_calls)
    start_idx = total // 2  # 从中间开始，特效在后半段
    
    print(f"[INFO] Scanning draw calls {start_idx} ~ {total} for transparent/VFX draws...")
    print()
    
    extracted_count = 0
    
    for i, draw in enumerate(draw_calls[start_idx:], start=start_idx):
        # 跳过非绘制调用（Clear、SetMarker 等）
        DrawFlag = rd.ActionFlags if hasattr(rd, 'ActionFlags') else rd.DrawFlags
        DrawcallBit = DrawFlag.Drawcall if hasattr(DrawFlag, 'Drawcall') else DrawFlag.Draw
        if not (draw.flags & DrawcallBit):
            continue
        
        # 移动到该 Draw Call
        controller.SetFrameEvent(draw.eventId, True)
        
        try:
            state = controller.GetPipelineState()
            ps = state.GetShader(rd.ShaderStage.Pixel)
            
            if ps == rd.ResourceId.Null():
                continue
            
            ps_reflection = state.GetShaderReflection(rd.ShaderStage.Pixel)
            
            # 检查是否有我们感兴趣的参数（_TintA, _TintB, _Fade, _Noise, _Pattern）
            is_flame_shader = False
            if ps_reflection:
                for cb in ps_reflection.constantBlocks:
                    for var in cb.variables:
                        if var.name.lower() in ['_tinta', '_tintb', '_fade', '_noise', '_pattern', '_shape']:
                            is_flame_shader = True
                            break
            
            if is_flame_shader:
                print(f"[FOUND] Draw #{i} (eventId={draw.eventId}) - Flame/Trail Shader detected!")
                extract_shader_disassembly(controller, i)
                tex_count = extract_textures_for_draw(controller, i)
                print(f"  Textures extracted: {tex_count}")
                extracted_count += 1
                print()
        
        except Exception as e:
            continue
    
    # 如果没找到精确匹配，提取所有半透明 Draw Call
    if extracted_count == 0:
        print("[INFO] No exact flame shader match found.")
        print("[INFO] Falling back: extracting ALL transparent draw calls in second half...")
        print()
        
        for i, draw in enumerate(draw_calls[start_idx:], start=start_idx):
            DrawFlag = rd.ActionFlags if hasattr(rd, 'ActionFlags') else rd.DrawFlags
            DrawcallBit = DrawFlag.Drawcall if hasattr(DrawFlag, 'Drawcall') else DrawFlag.Draw
            if not (draw.flags & DrawcallBit):
                continue
            
            controller.SetFrameEvent(draw.eventId, True)
            
            try:
                state = controller.GetPipelineState()
                ps = state.GetShader(rd.ShaderStage.Pixel)
                if ps == rd.ResourceId.Null():
                    continue
                
                # 检查混合状态
                fb = state.GetFramebuffer()
                blend = state.GetColorBlends()
                
                is_additive = False
                for b in blend:
                    if (b.enabled and 
                        b.colorBlend.source == rd.BlendMultiplier.One and
                        b.colorBlend.destination == rd.BlendMultiplier.One):
                        is_additive = True
                        break
                
                if is_additive:
                    print(f"[ADDITIVE] Draw #{i} (eventId={draw.eventId})")
                    extract_shader_disassembly(controller, i)
                    extract_textures_for_draw(controller, i)
                    extracted_count += 1
                    print()
                    
                    if extracted_count >= 20:  # 最多提取20个，避免太多
                        print("[INFO] Reached limit of 20 additive draws, stopping.")
                        break
            except:
                continue
    
    print("=" * 60)
    print(f"[DONE] Extracted {extracted_count} shader(s)")
    print(f"[DONE] Output saved to: {OUTPUT_DIR}")
    print("=" * 60)
    
    controller.Shutdown()
    cap.Shutdown()

# 运行
main()
