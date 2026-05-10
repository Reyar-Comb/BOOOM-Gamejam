shader_type canvas_item;

// === 参数 ===
// 光点颜色
uniform vec4 dot_color : source_color = vec4(0.6, 1.0, 0.8, 1.0);
// 流动速度
uniform float speed : hint_range(0.0, 10.0) = 2.0;
// 走完一轮后的停顿时间（秒）
uniform float pause_duration : hint_range(0.0, 5.0) = 1.0;
// 光点大小（沿线条比例）
uniform float dot_size : hint_range(0.005, 0.5) = 0.02;
// 拖尾长度（沿线条比例）
uniform float trail_length : hint_range(0.0, 0.5) = 0.12;
// 发光强度
uniform float intensity : hint_range(1.0, 30.0) = 8.0;

void fragment() {
    // 走动耗时 = 1/speed 秒走完一圈
    float move_time = 1.0 / max(speed, 0.001);
    float cycle_time = move_time + pause_duration;
    float t = mod(TIME, cycle_time);
    
    float mask = 0.0;
    
    // 走动阶段才计算光点，停顿阶段保持 mask=0（完全隐藏）
    if (t < move_time) {
        float dot_pos = t / move_time;
        
        // 当前片段到光点的距离（沿 UV.x 增大方向，不绕回）
        // diff < 0：光点后方（已走过，拖尾区域）
        // diff > 0：光点前方（未到达）
        float diff = UV.x - dot_pos;
        
        // 光点本身（高斯尖峰）
        float dot = exp(-diff * diff / (dot_size * dot_size));
        
        // 拖尾（光点后方指数衰减）
        float trail = 0.0;
        if (diff < 0.0 && trail_length > 0.0) {
            trail = exp(diff / trail_length) * 0.4;
        }
        
        mask = max(dot, trail);
    }
    
    COLOR = vec4(dot_color.rgb * intensity * mask, mask);
}