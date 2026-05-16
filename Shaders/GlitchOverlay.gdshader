shader_type canvas_item;
render_mode blend_mix;

// =====================================================
// GlitchOverlay.gdshader — 轻微动态 Glitch 覆盖效果
// 应用于 PPTScene 的 SlideDisplay，不影响文字阅读。
// 效果：RGB色散 + 水平撕裂 + 随机块位移
// 所有参数均已导出，可在 Inspector 中调节。
// =====================================================

// ---- 主控 ----
uniform float intensity : hint_range(0.0, 1.0) = 0.12;

// ---- RGB 色散 ----
uniform float rgb_split : hint_range(0.0, 0.02) = 0.005;

// ---- 水平撕裂 ----
uniform float tear_amount : hint_range(0.0, 0.06) = 0.014;
uniform float tear_threshold : hint_range(0.7, 1.0) = 0.94;
uniform float tear_width : hint_range(0.001, 0.04) = 0.008;

// ---- 随机块位移 ----
uniform float block_amount : hint_range(0.0, 0.04) = 0.007;
uniform float block_size : hint_range(2.0, 30.0) = 12.0;
uniform float block_threshold : hint_range(0.7, 1.0) = 0.95;

// ---- 时序 ----
uniform float speed : hint_range(0.1, 5.0) = 1.0;
uniform float noise_scale : hint_range(1.0, 60.0) = 25.0;

// ---- 可选色调偏移（增强 glitch 观感） ----
uniform float color_shift : hint_range(0.0, 0.15) = 0.03;

// =====================================================
// 哈希/噪声函数
// =====================================================

float hash(float n) {
    return fract(sin(n) * 43758.5453123);
}

float hash2(vec2 p) {
    float h = dot(p, vec2(127.1, 311.7));
    return fract(sin(h) * 43758.5453123);
}

float noise(vec2 uv) {
    vec2 i = floor(uv);
    vec2 f = fract(uv);
    f = f * f * (3.0 - 2.0 * f);
    float a = hash2(i);
    float b = hash2(i + vec2(1.0, 0.0));
    float c = hash2(i + vec2(0.0, 1.0));
    float d = hash2(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

// =====================================================

void fragment() {
    vec2 uv = UV;

    // 时间变量，带动画
    float t = TIME * speed;

    // ---- 噪声采样 ----
    // 行级噪声（用于撕裂判断）
    float row_noise = hash(floor(uv.y * noise_scale) + floor(t * 3.7));

    // 块级噪声（用于块位移判断）
    vec2 block_uv = floor(uv * block_size) / block_size;
    float block_noise = hash2(block_uv * noise_scale + vec2(t * 0.7, t * 0.3));

    // ---- RGB 色散 ----
    // 水平方向偏移 R 和 B 通道
    float rgb_offset = rgb_split * intensity * (row_noise * 2.0 - 1.0);

    // 额外的大幅抖动（偶尔出现）
    float spike = step(0.97, row_noise) * hash(floor(t * 10.0)) * 3.0;
    rgb_offset += rgb_split * intensity * spike;

    float r = texture(TEXTURE, uv + vec2(rgb_offset, 0.0)).r;
    float g = texture(TEXTURE, uv).g;
    float b = texture(TEXTURE, uv - vec2(rgb_offset, 0.0)).b;

    vec4 color = vec4(r, g, b, 1.0);

    // ---- 水平撕裂 ----
    float tear_trigger = step(tear_threshold, row_noise);
    float tear_strength = tear_amount * intensity * tear_trigger;
    // 撕裂影响的行很窄
    float tear_line = 1.0 - smoothstep(0.0, tear_width, abs(row_noise - tear_threshold));
    float tear_offset = tear_strength * (hash(floor(uv.y * noise_scale) + t) * 2.0 - 1.0);

    if (tear_line > 0.01) {
        // 撕裂处采样偏移的纹理
        vec4 torn = texture(TEXTURE, uv + vec2(tear_offset, 0.0));
        // 撕裂线上加重色散
        torn.r = texture(TEXTURE, uv + vec2(tear_offset + rgb_offset * 2.0, 0.0)).r;
        torn.b = texture(TEXTURE, uv - vec2(tear_offset + rgb_offset * 2.0, 0.0)).b;
        color = mix(color, torn, tear_line * 0.7);
    }

    // ---- 随机块位移 ----
    float block_trigger = step(block_threshold, block_noise);
    float block_offset = block_amount * intensity * block_trigger * (hash2(block_uv + t * 0.5) * 2.0 - 1.0);

    if (abs(block_offset) > 0.0001) {
        vec4 displaced = texture(TEXTURE, uv + vec2(block_offset, 0.0));
        color = mix(color, displaced, 0.55);
    }

    // ---- 色调偏移 ----
    // 给画面轻微的色彩漂移
    float tint = color_shift * intensity * (noise(vec2(uv.y * 2.0, t * 0.3)) - 0.5);
    color.r += tint;
    color.b -= tint;

    // ---- 保持 Alpha ----
    color.a = texture(TEXTURE, uv).a;
    COLOR = color;
}
