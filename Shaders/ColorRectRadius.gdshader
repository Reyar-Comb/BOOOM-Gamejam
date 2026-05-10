shader_type canvas_item;

// 请在编辑器中将 ColorRect 的 Size 填入这里，或者通过脚本同步
uniform vec2 size = vec2(200.0, 100.0);
// 这里的半径单位是“像素”，例如 20.0 就是 20px 的圆角
uniform float radius_px : hint_range(0.0, 500.0) = 20.0;

// 圆角矩形 SDF 函数（像素空间版）
float sdRoundedBox(vec2 p, vec2 b, float r) {
    vec2 q = abs(p) - b + r;
    return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
}

void fragment() {
    // 1. 将 UV 映射到像素空间，并以中心为原点
    // p 的范围现在是 [-(width/2), -(height/2)] 到 [(width/2), (height/2)]
    vec2 p = (UV - 0.5) * size;
    
    // 2. 计算半宽半高
    vec2 half_size = size * 0.5;
    
    // 3. 计算到边缘的像素距离
    float d = sdRoundedBox(p, half_size, radius_px);
    
    // 4. 抗锯齿：在像素空间中，fade 值通常固定为 1.0 左右
    float mask = 1.0 - smoothstep(-1.0, 1.0, d);
    
    // 5. 应用遮罩
    COLOR.a *= mask;
}