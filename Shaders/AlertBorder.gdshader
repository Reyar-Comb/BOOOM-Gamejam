shader_type canvas_item;
render_mode blend_mix;

// ==============================
// AlertBorder shader
// 低血量/警告状态下的闪烁红色边框
// ==============================

// 边框宽度（归一化 UV 比例，0~0.5）
uniform float border_width : hint_range(0.0, 0.5) = 0.06;
// 边框羽化/柔边范围
uniform float border_softness : hint_range(0.0, 0.3) = 0.04;
// 光晕宽度（边框内侧扩散范围）
uniform float glow_width : hint_range(0.0, 0.5) = 0.12;
// 光晕羽化
uniform float glow_softness : hint_range(0.0, 0.3) = 0.08;

// 闪烁速度
uniform float blink_speed : hint_range(0.5, 10.0) = 2.5;
// 闪烁透明度范围
uniform float min_alpha : hint_range(0.0, 1.0) = 0.12;
uniform float max_alpha : hint_range(0.0, 1.0) = 0.7;

// 边框颜色
uniform vec4 border_color : source_color = vec4(0.9, 0.05, 0.05, 1.0);
// 光晕颜色
uniform vec4 glow_color : source_color = vec4(1.0, 0.15, 0.1, 1.0);

// 圆角半径（归一化 UV，0 = 直角，越大越圆）
uniform float corner_radius : hint_range(0.0, 0.5) = 0.02;


void fragment()
{
	// ---- 计算到边缘的最近距离 ----
	// 考虑圆角的距离：用一个简单的方法，同时考虑水平和垂直方向
	vec2 half_size = vec2(0.5);
	vec2 center = vec2(0.5);

	// 先计算普通矩形距离
	float dist_x = min(UV.x, 1.0 - UV.x);
	float dist_y = min(UV.y, 1.0 - UV.y);

	// 角区域：离角越近，用对角线距离补偿
	vec2 corner_dist = vec2(
		max(0.0, corner_radius - dist_x),
		max(0.0, corner_radius - dist_y)
	);
	float corner_fix = length(corner_dist);

	// 最终边缘距离（取较小的那个方向，再加上圆角修正）
	float dist = min(dist_x, dist_y);
	dist = dist + corner_radius - corner_fix;

	// ---- 边框遮罩 ----
	float border_mask = 1.0 - smoothstep(
		border_width - border_softness,
		border_width + border_softness,
		dist
	);

	// ---- 光晕遮罩（边框内侧） ----
	float glow_mask = 1.0 - smoothstep(
		border_width + glow_width - glow_softness,
		border_width + glow_width + glow_softness,
		dist
	);
	glow_mask -= border_mask; // 排除边框本身，只保留内侧光晕
	glow_mask = clamp(glow_mask, 0.0, 1.0);

	// ---- 闪烁动画 ----
	// 使用两个不同频率的正弦波叠加，制造"心跳"般的不规则闪烁
	float wave1 = 0.5 + 0.5 * sin(TIME * blink_speed * 3.14159);
	float wave2 = 0.5 + 0.5 * sin(TIME * blink_speed * 1.7 * 3.14159 + 1.2);
	float blink = mix(wave1, wave2, 0.35);

	float current_alpha = mix(min_alpha, max_alpha, blink);

	// ---- 合成颜色 ----
	vec4 col = vec4(0.0);

	// 光晕层
	col = mix(col, glow_color, glow_mask * current_alpha * 0.6);

	// 边框层（更实）
	col = mix(col, border_color, border_mask * current_alpha);

	// 光晕层只影响 RGB，alpha 走边框遮罩
	float final_alpha = (border_mask + glow_mask * 0.5) * current_alpha;

	COLOR = vec4(col.rgb, final_alpha);
}
