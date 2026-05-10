shader_type canvas_item;

// 由脚本传入两个 SubViewport 的渲染纹理
uniform sampler2D bg_texture;
uniform sampler2D game_texture;
uniform vec2 mouse_pos;
uniform float bg_fov : hint_range(0, 2.0) = 0.04;
uniform float game_fov : hint_range(0, 2.0) = 0.01;
// 游戏场景的"透明"背景色（设为场景里 ColorRect 的颜色）
uniform vec3 key_color : source_color = vec3(0.0, 0.0, 0.0);

vec2 distort_uv(vec2 uv, vec2 m, float fov) {
    float perspective = 1.0 - (uv.x * m.x + uv.y * m.y) * fov;
    vec2 p_uv = uv / perspective;
    p_uv = (p_uv + 1.0) * 0.5;
    return clamp(p_uv, vec2(0.0), vec2(1.0));
}

void fragment() {
    // 归一化坐标 (-1.0 到 1.0)
    vec2 uv = SCREEN_UV * 2.0 - 1.0;
    vec2 m = (mouse_pos * 2.0) - 1.0;
    
    // 分别采样两层（不同 fov）
    vec2 bg_uv = distort_uv(uv, m, bg_fov);
    vec2 game_uv = distort_uv(uv, m, game_fov);
    
    vec4 bg = texture(bg_texture, bg_uv);
    vec4 game = texture(game_texture, game_uv);
    
    // 色度键：检测 game 像素是否接近背景色
    float diff = distance(game.rgb, key_color);
    float alpha = smoothstep(0.01, 0.06, diff);
    
    // 合成
    vec3 result = game.rgb * alpha + bg.rgb * (1.0 - alpha);
    COLOR = vec4(result, 1.0);
}
