shader_type canvas_item;

// 由脚本传入 SubViewport 的渲染纹理（替代 SCREEN_TEXTURE）
uniform sampler2D viewport_texture;
uniform vec2 mouse_pos;
uniform float fov : hint_range(0, 2.0) = 0.01;

void fragment() {
    // 归一化坐标 (-1.0 到 1.0)
    vec2 uv = SCREEN_UV * 2.0 - 1.0;
    
    // 计算鼠标偏移 (已由脚本归一化为 0~1，转为 -1.0~1.0)
    vec2 m = (mouse_pos * 2.0) - 1.0;
    
    // 模拟 3D 透视变形：基于鼠标位置修改 UV
    float perspective = 1.0 - (uv.x * m.x + uv.y * m.y) * fov;
    vec2 p_uv = uv / perspective;
    
    // 转回 0.0 到 1.0 范围
    p_uv = (p_uv + 1.0) * 0.5;
    
    // 钳位 UV 到 [0,1] 范围内，防止透视变形导致黑边
    p_uv = clamp(p_uv, vec2(0.0), vec2(1.0));
    COLOR = texture(viewport_texture, p_uv);
}